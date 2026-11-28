namespace Glacier.Mobile.Tests;

using System;
using System.Diagnostics;
using Glacier.Graphics;
using Glacier.Mobile.Core;
using Glacier.Mobile.Input;
using Glacier.Mobile.Interop;
using Glacier.Mobile.Rendering;
using Glacier.Mobile.UI;
using Glacier.Mobile.UI.Controls;
using Glacier.Mobile.UI.Layouts;
using Glacier.Mobile.UI.Navigation;
using Glacier.Plot.Core;
using Glacier.Plot.Figures;
using Glacier.Plot.Plottables;
using Glacier.Plot.Rendering;
using Glacier.Polaris;
using Glacier.Polaris.Data;
using SkiaSharp;
using Xunit;

public class M4HardeningTests
{
    private sealed class DisposableSpyPlottable : IPlottable, IDisposable
    {
        public string? Label { get; set; }
        public PlotStyle Style { get; set; } = new();
        public int DisposeCount { get; private set; }

        public AxisLimits GetLimits() => new(0, 1, 0, 1);
        public void Render(IGraphicsCanvas canvas, CoordinateConverter converter, PlotTheme theme) { }
        public void Dispose() => DisposeCount++;
    }

    [Fact]
    public void PlotView_RepeatedRenderSameSize_ReusesCanvasWithoutLohChurn()
    {
        using var compositor = new HeadlessCompositor(400, 300);
        var figure = new Figure { Title = "Steady" };
        figure.PlotSignal(new float[] { 1f, 2f, 3f, 4f, 5f }, label: "S");

        using var plotView = new PlotView(figure) { Width = 380f, Height = 250f };

        // Warmup frames initialize cached LinearFramebuffer + CpuGraphicsCanvas (with 4 MB GlyphAtlas)
        for (int i = 0; i < 5; i++)
        {
            compositor.BeginFrame();
            compositor.RenderTree(plotView, 400f, 300f);
            compositor.EndFrame();
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        const int steadyFrames = 5;
        for (int i = 0; i < steadyFrames; i++)
        {
            compositor.BeginFrame();
            compositor.RenderTree(plotView, 400f, 300f);
            compositor.EndFrame();
        }
        long after = GC.GetAllocatedBytesForCurrentThread();

        // Previously allocated >4.19 MB/frame (GlyphAtlas LOH) + NativeMemory LinearFramebuffer;
        // remaining ~39 KB/frame is Glacier.Plot/Glacier.Graphics path rendering (strictly < 85 KB LOH threshold).
        long bytesPerFrame = (after - before) / steadyFrames;
        Assert.True(bytesPerFrame < 85_000, $"Expected cached PlotView render to avoid LOH churn (<85,000 B/frame), got {bytesPerFrame} B/frame.");

        // Resize reallocates cleanly
        plotView.Width = 320f;
        plotView.Height = 180f;
        compositor.BeginFrame();
        compositor.RenderTree(plotView, 400f, 300f);
        compositor.EndFrame();

        // Dispose makes subsequent Render a safe no-op
        plotView.Dispose();
        compositor.BeginFrame();
        compositor.RenderTree(plotView, 400f, 300f);
        compositor.EndFrame();
        Assert.Equal(12, compositor.RenderedFrameCount);
    }

    [Fact]
    public void PlotView_UnpremulPixelRendering_PreservesStraightAlphaColors()
    {
        var figure = new Figure
        {
            ShowLegend = false,
            Theme = new PlotTheme
            {
                FigureBackground = new Rgba32(200, 100, 50, 128),
                DataBackground = new Rgba32(200, 100, 50, 128)
            }
        };
        figure.XAxis.ShowTicks = false;
        figure.XAxis.ShowGrid = false;
        figure.YAxis.ShowTicks = false;
        figure.YAxis.ShowGrid = false;

        using var plotView = new PlotView(figure)
        {
            BackgroundColor = Color4.Transparent,
            Width = 100f,
            Height = 100f
        };
        plotView.Measure(100f, 100f);
        plotView.Arrange(0f, 0f, 100f, 100f);

        using var bitmap = new SKBitmap(100, 100, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(0, 0, 0, 255)); // opaque black backdrop

        plotView.Render(canvas);
        canvas.Flush();

        // Pixel (5, 5) is in the figure margin (straight RGBA(200, 100, 50, 128) over black).
        // Under SKAlphaType.Unpremul, Skia scales RGB by 128/255 (~100, 50, 25);
        // under the old Premul bug, RGB would stay unscaled (~200, 100, 50).
        SKColor pixel = bitmap.GetPixel(5, 5);
        Assert.InRange(pixel.Red, 95, 106);
        Assert.InRange(pixel.Green, 46, 55);
        Assert.InRange(pixel.Blue, 22, 29);
        Assert.Equal(255, pixel.Alpha);
    }

    [Fact]
    public void PlotView_FigureOwnership_DisposesOwnedButPreservesExternal()
    {
        var externalFig = new Figure();
        var externalSpy = new DisposableSpyPlottable();
        externalFig.AddPlottable(externalSpy);

        using (var externalPlotView = new PlotView(externalFig))
        {
            Assert.Same(externalFig, externalPlotView.Figure);
        }
        Assert.Equal(0, externalSpy.DisposeCount);

        var ownedPlotView = new PlotView();
        var ownedSpy = new DisposableSpyPlottable();
        ownedPlotView.Figure.AddPlottable(ownedSpy);
        ownedPlotView.Dispose();
        Assert.Equal(1, ownedSpy.DisposeCount);

        externalFig.Dispose();
        Assert.Equal(1, externalSpy.DisposeCount);
    }

    [Fact]
    public void TelemetryView_BindDataFrame_UsesAsFloatMemoryAndDisposesReplacedFigures()
    {
        using var compositor = new HeadlessCompositor(400, 400);
        using var telemetry = new TelemetryView("Telemetry");

        using var f32 = new Float32Series("Value", 4);
        f32[0] = 10f; f32[1] = 20f; f32[2] = 30f; f32[3] = 40f;
        using var df1 = new DataFrame([f32]);

        telemetry.BindDataFrame(df1, "Value");
        var firstFigure = ((PlotView)telemetry.Children[2]).Figure;
        var spy = new DisposableSpyPlottable();
        firstFigure.AddPlottable(spy);

        // Second bind with Float64Series replaces and disposes firstFigure
        using var f64 = new Float64Series("Value", 3);
        f64[0] = 1.5; f64[1] = 2.5; f64[2] = 3.5;
        using var df2 = new DataFrame([f64]);

        telemetry.BindDataFrame(df2, "Value");
        Assert.Equal(1, spy.DisposeCount);

        var secondFigure = ((PlotView)telemetry.Children[2]).Figure;
        var spy2 = new DisposableSpyPlottable();
        secondFigure.AddPlottable(spy2);

        // Missing column falls back cleanly without replacing figure
        telemetry.BindDataFrame(df2, "NonExistent");
        Assert.Equal(0, spy2.DisposeCount);

        compositor.BeginFrame();
        compositor.RenderTree(telemetry, 400f, 400f);
        compositor.EndFrame();

        telemetry.Dispose();
        Assert.Equal(1, spy2.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => telemetry.BindDataFrame(df1, "Value"));
    }

    [Fact]
    public void Button_And_Label_RepeatedRender_UsesThreadStaticScratchCache()
    {
        using var compositor = new HeadlessCompositor(393, 200);
        var root = new Column(spacing: 8f)
        {
            new Header("Header"),
            new Label("Body Text"),
            new Button("Tap")
        };

        for (int i = 0; i < 5; i++)
        {
            compositor.BeginFrame();
            compositor.RenderTree(root, 393f, 200f);
            compositor.EndFrame();
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        const int steadyFrames = 10;
        for (int i = 0; i < steadyFrames; i++)
        {
            compositor.BeginFrame();
            compositor.RenderTree(root, 393f, 200f);
            compositor.EndFrame();
        }
        long after = GC.GetAllocatedBytesForCurrentThread();

        long bytesPerFrame = (after - before) / steadyFrames;
        Assert.True(bytesPerFrame < 2048, $"Expected pooled SKPaint/SKRoundRect (<2048 B/frame), got {bytesPerFrame} B/frame.");
    }

    private sealed class BubblingTestApp : MobileApplication
    {
        public ScrollView Scroll { get; private set; } = null!;
        public Button ActionButton { get; private set; } = null!;
        public PlotView Chart { get; private set; } = null!;
        public int Clicks { get; private set; }

        protected override MobileView Build()
        {
            ActionButton = new Button("Action", () => Clicks++) { Width = 200f, Height = 44f };
            Chart = new PlotView { Width = 300f, Height = 200f };
            Chart.Figure.PlotSignal(new float[] { 1f, 3f, 2f, 5f }, label: "V");

            var col = new Column(spacing: 12f)
            {
                ActionButton,
                Chart,
                new Label("Tail") { Width = 300f, Height = 900f }
            };

            Scroll = new ScrollView { Content = col };
            return new Screen("Home") { Content = Scroll };
        }
    }

    [Fact]
    public void ScrollView_BubblesDragFromChildButton_DiscriminatesTap_AndRespectsPlotViewPan()
    {
        using var app = new BubblingTestApp();
        app.Start();
        app.Step(1f / 120f);

        long t0 = 1_000_000L;
        long dt = Stopwatch.Frequency / 120;

        // 1. Tap on Button inside ScrollView triggers OnClick and leaves ScrollOffsetY == 0
        float bx = app.ActionButton.X + 20f;
        float by = app.ActionButton.Y + 20f;
        app.DispatchTouch(new TouchEvent(t0, 0, bx, by, TouchPhase.Began));
        app.DispatchTouch(new TouchEvent(t0 + dt, 0, bx, by, TouchPhase.Ended));
        Assert.Equal(1, app.Clicks);
        Assert.Equal(0f, app.Scroll.ScrollOffsetY);

        // 2. Drag starting on Button bubbles to ScrollView and cancels Tap
        app.DispatchTouch(new TouchEvent(t0 + 2 * dt, 0, bx, by, TouchPhase.Began));
        app.DispatchTouch(new TouchEvent(t0 + 3 * dt, 0, bx, by - 60f, TouchPhase.Moved));
        app.DispatchTouch(new TouchEvent(t0 + 4 * dt, 0, bx, by - 120f, TouchPhase.Moved));
        app.DispatchTouch(new TouchEvent(t0 + 5 * dt, 0, bx, by - 120f, TouchPhase.Ended));
        Assert.Equal(1, app.Clicks);
        Assert.Equal(120f, app.Scroll.ScrollOffsetY, 1);

        // 3. Drag starting inside PlotView pans PlotView without scrolling outer ScrollView
        float scrollBefore = app.Scroll.ScrollOffsetY;
        float px = app.Chart.X + 50f;
        float py = app.Chart.Y - scrollBefore + 50f;
        app.DispatchTouch(new TouchEvent(t0 + 6 * dt, 0, px, py, TouchPhase.Began));
        app.DispatchTouch(new TouchEvent(t0 + 7 * dt, 0, px + 40f, py - 40f, TouchPhase.Moved));
        app.DispatchTouch(new TouchEvent(t0 + 8 * dt, 0, px + 40f, py - 40f, TouchPhase.Ended));
        Assert.Equal(scrollBefore, app.Scroll.ScrollOffsetY, 3);
    }

    [Fact]
    public void PinchGestureRecognizer_SingleFingerLift_BothFingersAndCancelled_ResumeOnReTouch()
    {
        // Case A: Lift finger 1, keep finger 0, re-place finger 2
        var pinch = new PinchGestureRecognizer();
        pinch.ProcessTouch(new TouchEvent(0, 100f, 100f, TouchPhase.Began));
        pinch.ProcessTouch(new TouchEvent(1, 150f, 100f, TouchPhase.Began));
        pinch.ProcessTouch(new TouchEvent(1, 150f, 100f, TouchPhase.Ended));
        Assert.Equal(GestureState.Ended, pinch.State);

        pinch.ProcessTouch(new TouchEvent(2, 150f, 100f, TouchPhase.Began));
        Assert.Equal(GestureState.Began, pinch.State);
        pinch.ProcessTouch(new TouchEvent(2, 220f, 100f, TouchPhase.Moved));
        Assert.Equal(GestureState.Changed, pinch.State);
        Assert.Equal(2.4f, pinch.Scale, 2);

        // Case B: Lift finger 0 (while pointer 2 is touch1 -> promoted to touch0), re-place pointer 3
        pinch.ProcessTouch(new TouchEvent(0, 100f, 100f, TouchPhase.Ended));
        Assert.Equal(GestureState.Ended, pinch.State);

        pinch.ProcessTouch(new TouchEvent(3, 120f, 100f, TouchPhase.Began));
        Assert.Equal(GestureState.Began, pinch.State); // initial distance = |220 - 120| = 100
        pinch.ProcessTouch(new TouchEvent(3, 20f, 100f, TouchPhase.Moved)); // distance = 200 -> scale 2.0
        Assert.Equal(GestureState.Changed, pinch.State);
        Assert.Equal(2.0f, pinch.Scale, 2);

        // Case C: Cancelled on one finger preserves surviving finger
        pinch.ProcessTouch(new TouchEvent(3, 20f, 100f, TouchPhase.Cancelled));
        Assert.Equal(GestureState.Cancelled, pinch.State);
        pinch.ProcessTouch(new TouchEvent(4, 170f, 100f, TouchPhase.Began));
        Assert.Equal(GestureState.Began, pinch.State);
    }

    [Fact]
    public void TouchRingBuffer_WrapAroundBatchDrain_ClearAndCapacityOne()
    {
        Assert.Throws<ArgumentException>(() => new TouchRingBuffer(1));

        var ring = new TouchRingBuffer(8);

        // Enqueue 6, drain 4 -> tail=4, head=6
        for (int i = 0; i < 6; i++)
        {
            Assert.True(ring.TryEnqueue(new TouchSnapshot(100 + i, i, i, i, TouchPhase.Moved)));
        }

        Span<TouchSnapshot> firstBatch = stackalloc TouchSnapshot[4];
        Assert.Equal(4, ring.Drain(firstBatch));
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(i, firstBatch[i].PointerId);
        }

        // Enqueue 6 more -> wraps around indices 6,7,0,1,2,3 (head=12, tail=4, Count=8)
        for (int i = 6; i < 12; i++)
        {
            Assert.True(ring.TryEnqueue(new TouchSnapshot(100 + i, i, i, i, TouchPhase.Moved)));
        }
        Assert.False(ring.TryEnqueue(new TouchSnapshot(999, 99, 0, 0, TouchPhase.Moved)));
        Assert.Equal(8, ring.Count);

        // Batch Drain across wrap boundary [4..11]
        Span<TouchSnapshot> wrapBatch = stackalloc TouchSnapshot[8];
        int drained = ring.Drain(wrapBatch);
        Assert.Equal(8, drained);
        Assert.Equal(0, ring.Count);
        for (int i = 0; i < 8; i++)
        {
            Assert.Equal(4 + i, wrapBatch[i].PointerId);
            Assert.Equal(104 + i, wrapBatch[i].Timestamp);
        }

        // Clear syncs CachedRemote and permits full refill
        Assert.True(ring.TryEnqueue(new TouchSnapshot(1, 1, 0, 0, TouchPhase.Began)));
        ring.Clear();
        Assert.Equal(0, ring.Count);
        Assert.False(ring.TryDequeue(out _));

        for (int i = 0; i < 8; i++)
        {
            Assert.True(ring.TryEnqueue(new TouchSnapshot(200 + i, 50 + i, 0, 0, TouchPhase.Began)));
        }
        Assert.Equal(8, ring.Drain(wrapBatch));
        Assert.Equal(57, wrapBatch[7].PointerId);
    }

    [Fact]
    public void ScrollView_UpdatePhysics_GuardsNonPositiveDt_UpdatesBatch_AndHitTestsScrolledOffset()
    {
        var labelTop = new Label("Top") { Width = 300f, Height = 100f };
        var labelMid = new Label("Mid") { Width = 300f, Height = 100f };
        var labelBot = new Label("Bot") { Width = 300f, Height = 500f };

        var scroll = new ScrollView
        {
            Width = 300f,
            Height = 200f,
            Content = new Column(spacing: 0f) { labelTop, labelMid, labelBot }
        };

        scroll.Measure(300f, 200f);
        scroll.Arrange(0f, 0f, 300f, 200f);

        long t0 = 1_000_000L;
        long dt = Stopwatch.Frequency / 120;
        scroll.ProcessTouch(new TouchEvent(t0, 0, 50f, 150f, TouchPhase.Began));
        scroll.ProcessTouch(new TouchEvent(t0 + dt, 0, 50f, 50f, TouchPhase.Moved));
        scroll.ProcessTouch(new TouchEvent(t0 + 2 * dt, 0, 50f, 50f, TouchPhase.Ended));

        float offsetBefore = scroll.ScrollOffsetY;
        Assert.Equal(100f, offsetBefore, 1);

        // Non-positive dt must be a no-op
        scroll.UpdatePhysics(0f);
        scroll.UpdatePhysics(-0.016f);
        Assert.Equal(offsetBefore, scroll.ScrollOffsetY);

        // HitTest at screen Y=50 when scrolled by 100 hits labelMid (y=[100..200))
        var hit = scroll.HitTest(50f, 50f);
        Assert.Same(labelMid, hit);

        // Batch SIMD damping across 16 scroll lanes
        Span<float> batch = stackalloc float[16];
        for (int i = 0; i < 15; i++) batch[i] = 100f;
        batch[15] = 0.0005f;
        ScrollView.UpdatePhysicsBatch(batch, 1f / 120f, 0.5f);
        Assert.Equal(50f, batch[0], 2);
        Assert.Equal(0f, batch[15]);
    }
}

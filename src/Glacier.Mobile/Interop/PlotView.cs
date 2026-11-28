namespace Glacier.Mobile.Interop;

using System;
using Glacier.Mobile.Input;
using Glacier.Mobile.UI;
using Glacier.Plot.Figures;
using SkiaSharp;

/// <summary>
/// Viewport component hosting a Glacier.Plot Figure with cached framebuffer/canvas rendering.
/// Supports touch gestures for interactive pan and pinch-to-zoom.
/// </summary>
public sealed class PlotView : MobileView, IDisposable
{
    private readonly PanGestureRecognizer _panRecognizer;
    private readonly PinchGestureRecognizer _pinchRecognizer;
    private Figure _figure;
    private bool _ownsFigure;
    private Glacier.Graphics.Raster.LinearFramebuffer? _framebuffer;
    private Glacier.Graphics.CpuGraphicsCanvas? _figureCanvas;
    private int _cachedWidth;
    private int _cachedHeight;
    private bool _disposed;

    public Figure Figure
    {
        get => _figure;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (!ReferenceEquals(_figure, value))
            {
                if (_ownsFigure)
                {
                    _figure.Dispose();
                }
                _figure = value;
                _ownsFigure = false;
            }
        }
    }

    public PlotView(Figure? figure = null)
    {
        if (figure == null)
        {
            _figure = new Figure();
            _ownsFigure = true;
        }
        else
        {
            _figure = figure;
            _ownsFigure = false;
        }

        BackgroundColor = Color4.CardBackground;
        Padding = new Thickness(8f);

        _panRecognizer = new PanGestureRecognizer();
        _panRecognizer.StateChanged += OnPan;
        GestureRecognizers.Add(_panRecognizer);

        _pinchRecognizer = new PinchGestureRecognizer();
        _pinchRecognizer.StateChanged += OnPinch;
        GestureRecognizers.Add(_pinchRecognizer);
    }

    internal void SetOwnedFigure(Figure figure)
    {
        ArgumentNullException.ThrowIfNull(figure);
        if (!ReferenceEquals(_figure, figure))
        {
            if (_ownsFigure)
            {
                _figure.Dispose();
            }
            _figure = figure;
        }
        _ownsFigure = true;
    }

    private void OnPan(PanGestureRecognizer pan)
    {
        if (pan.State == GestureState.Changed)
        {
            // Pan interaction: Shift axis limits incrementally (avoiding cumulative Translation drift)
            float dx = -pan.DeltaX * 0.01f;
            float dy = pan.DeltaY * 0.01f;
            Figure.XAxis.Pan(dx);
            Figure.YAxis.Pan(dy);
        }
    }

    private void OnPinch(PinchGestureRecognizer pinch)
    {
        if (pinch.State == GestureState.Changed && MathF.Abs(pinch.DeltaScale - 1.0f) > 1e-5f)
        {
            // Pinch interaction: Zoom axis limits incrementally using frame-to-frame DeltaScale
            Figure.XAxis.Zoom(pinch.DeltaScale);
            Figure.YAxis.Zoom(pinch.DeltaScale);
        }
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        float w = !float.IsNaN(Width) ? Width : availableWidth;
        float h = !float.IsNaN(Height) ? Height : MathF.Min(availableHeight, 300f);
        ActualWidth = w;
        ActualHeight = h;
    }

    public override unsafe void Render(SKCanvas canvas)
    {
        if (!IsVisible || _disposed) return;
        base.Render(canvas);

        int w = (int)MathF.Max(1f, ActualWidth);
        int h = (int)MathF.Max(1f, ActualHeight);

        if (_framebuffer == null || _figureCanvas == null || _cachedWidth != w || _cachedHeight != h)
        {
            _figureCanvas?.Dispose();
            _figureCanvas = null;
            _framebuffer?.Dispose();
            _framebuffer = null;

            _framebuffer = new Glacier.Graphics.Raster.LinearFramebuffer(w, h);
            _figureCanvas = new Glacier.Graphics.CpuGraphicsCanvas(_framebuffer);
            _cachedWidth = w;
            _cachedHeight = h;
        }

        canvas.Save();
        canvas.ClipRect(new SKRect(X, Y, X + ActualWidth, Y + ActualHeight));
        canvas.Translate(X, Y);

        _figure.Render(_figureCanvas, w, h);

        fixed (byte* p = _framebuffer.AsByteSpan())
        {
            var info = new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            using var image = SKImage.FromPixels(info, (IntPtr)p, _framebuffer.Stride);
            if (image != null)
            {
                canvas.DrawImage(image, 0, 0);
            }
        }

        canvas.Restore();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _figureCanvas?.Dispose();
            _figureCanvas = null;
            _framebuffer?.Dispose();
            _framebuffer = null;
            if (_ownsFigure)
            {
                _figure.Dispose();
                _ownsFigure = false;
            }
            _disposed = true;
        }
    }
}

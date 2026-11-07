namespace Glacier.Mobile.Tests;

using System;
using Glacier.Mobile.Core;
using Glacier.Mobile.Input;
using Glacier.Mobile.Rendering;
using Glacier.Mobile.UI;
using Glacier.Mobile.UI.Controls;
using Glacier.Mobile.UI.Layouts;
using Glacier.Mobile.UI.Navigation;
using Xunit;

public class TestMobileApp : MobileApplication
{
    public bool Built { get; private set; }
    public Button TestButton { get; private set; } = null!;

    public TestMobileApp(DisplayMetrics? metrics = null) : base(metrics) { }

    protected override MobileView Build()
    {
        Built = true;
        TestButton = new Button("Tap Me");
        return new Screen("Home")
        {
            Content = new Column
            {
                new Header("Test App"),
                TestButton
            }
        };
    }
}

public class ApplicationLifecycleTests
{
    [Fact]
    public void MobileApplication_Lifecycle_StartStepPauseResumeStop()
    {
        using var app = new TestMobileApp();
        Assert.False(app.IsRunning);

        app.Start();
        Assert.True(app.Built);
        Assert.True(app.IsRunning);
        Assert.NotNull(app.RootView);

        // Simulate 5 frames at 120Hz (8.33ms per frame)
        for (int i = 0; i < 5; i++)
        {
            app.Step(1.0f / 120.0f);
        }
        Assert.Equal(5, app.FrameCount);

        app.Pause();
        Assert.False(app.IsRunning);

        app.Resume();
        Assert.True(app.IsRunning);

        app.Stop();
        Assert.False(app.IsRunning);
    }

    [Fact]
    public void MobileApplication_DispatchesTouchToHitView()
    {
        using var app = new TestMobileApp();
        app.Start();

        bool buttonClicked = false;
        app.TestButton.OnClick = () => buttonClicked = true;

        // Render once to measure and arrange layout
        app.Step(1.0f / 120.0f);

        // Dispatch touch directly to button coordinates
        float touchX = app.TestButton.X + 10f;
        float touchY = app.TestButton.Y + 10f;

        app.DispatchTouch(new TouchEvent(0, touchX, touchY, TouchPhase.Began));
        app.DispatchTouch(new TouchEvent(0, touchX, touchY, TouchPhase.Ended));

        Assert.True(buttonClicked);
    }

    private sealed class ScrollPhysicsTestApp : MobileApplication
    {
        public ScrollView Scroll { get; private set; } = null!;

        protected override MobileView Build()
        {
            var col = new Column(spacing: 0f);
            for (int i = 0; i < 20; i++)
            {
                col.Add(new Label($"Row {i}") { Width = 350f, Height = 100f });
            }

            Scroll = new ScrollView { Content = col };
            return new NavigationStack
            {
                new Screen("ScrollPage") { Content = Scroll }
            };
        }
    }

    [Fact]
    public void MobileApplication_Step_TicksScrollViewInertialPhysicsAndBubblesFromLabels()
    {
        using var app = new ScrollPhysicsTestApp();
        app.Start();
        app.Step(1f / 120f);

        Assert.True(app.Scroll.MaxScrollY > 500f);

        long t0 = 1_000_000L;
        long dtTicks = System.Diagnostics.Stopwatch.Frequency / 120; // ~8.33ms

        // Drag upward on a child Label inside ScrollView: touches bubble up to ScrollView
        app.DispatchTouch(new TouchEvent(t0, 0, 100f, 400f, TouchPhase.Began));
        app.DispatchTouch(new TouchEvent(t0 + dtTicks, 0, 100f, 350f, TouchPhase.Moved));
        app.DispatchTouch(new TouchEvent(t0 + 2 * dtTicks, 0, 100f, 300f, TouchPhase.Moved));
        app.DispatchTouch(new TouchEvent(t0 + 3 * dtTicks, 0, 100f, 300f, TouchPhase.Ended));

        float offsetAtRelease = app.Scroll.ScrollOffsetY;
        Assert.Equal(100f, offsetAtRelease, 1);

        // Step frames: kinetic inertia should continue scrolling past 100f and decay cleanly
        for (int i = 0; i < 10; i++)
        {
            app.Step(1f / 120f);
        }

        Assert.True(app.Scroll.ScrollOffsetY > offsetAtRelease);
    }
}

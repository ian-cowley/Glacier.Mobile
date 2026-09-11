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
}

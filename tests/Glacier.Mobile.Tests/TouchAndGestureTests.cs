namespace Glacier.Mobile.Tests;

using System;
using Glacier.Mobile.Input;
using Xunit;

public class TouchAndGestureTests
{
    [Fact]
    public void TouchEvent_And_Snapshot_PreserveCoordinates()
    {
        var touch = new TouchEvent(1, 150.5f, 250.5f, TouchPhase.Began);
        Assert.Equal(1, touch.PointerId);
        Assert.Equal(150.5f, touch.X);
        Assert.Equal(250.5f, touch.Y);
        Assert.Equal(TouchPhase.Began, touch.Phase);

        var snapshot = new TouchSnapshot(in touch);
        Assert.Equal(touch.Timestamp, snapshot.Timestamp);
        Assert.Equal(1, snapshot.PointerId);
        Assert.Equal(150.5f, snapshot.X);
        Assert.Equal(250.5f, snapshot.Y);
        Assert.Equal(TouchPhase.Began, snapshot.Phase);
    }

    [Fact]
    public void GestureKernels_ApplyInertiaDamping_DecaysAndClampsEpsilon()
    {
        Span<float> velocities = stackalloc float[8]
        {
            100f, -50f, 20f, -10f, 0.005f, -0.0005f, 0.0001f, 0f
        };

        GestureKernels.ApplyInertiaDamping(velocities, 0.5f);

        Assert.Equal(50f, velocities[0], 2);
        Assert.Equal(-25f, velocities[1], 2);
        Assert.Equal(10f, velocities[2], 2);
        Assert.Equal(-5f, velocities[3], 2);
        // Subnormal values (< 0.001f) clamped to zero
        Assert.Equal(0f, velocities[5]);
        Assert.Equal(0f, velocities[6]);
        Assert.Equal(0f, velocities[7]);
    }

    [Fact]
    public void PanGestureRecognizer_TracksTranslationAndVelocity()
    {
        var pan = new PanGestureRecognizer();
        Assert.Equal(GestureState.Possible, pan.State);

        // 1. Began
        pan.ProcessTouch(new TouchEvent(0, 100f, 100f, TouchPhase.Began));
        Assert.Equal(GestureState.Began, pan.State);
        Assert.Equal(0f, pan.TranslationX);
        Assert.Equal(0f, pan.TranslationY);

        // 2. Moved
        pan.ProcessTouch(new TouchEvent(0, 150f, 80f, TouchPhase.Moved));
        Assert.Equal(GestureState.Changed, pan.State);
        Assert.Equal(50f, pan.TranslationX);
        Assert.Equal(-20f, pan.TranslationY);

        // 3. Ended
        pan.ProcessTouch(new TouchEvent(0, 160f, 75f, TouchPhase.Ended));
        Assert.Equal(GestureState.Ended, pan.State);
        Assert.Equal(60f, pan.TranslationX);
        Assert.Equal(-25f, pan.TranslationY);
    }

    [Fact]
    public void PinchGestureRecognizer_TracksScale()
    {
        var pinch = new PinchGestureRecognizer();

        // Pointer 0 begins at (100, 100)
        pinch.ProcessTouch(new TouchEvent(0, 100f, 100f, TouchPhase.Began));
        Assert.Equal(GestureState.Possible, pinch.State);

        // Pointer 1 begins at (200, 100) -> initial distance = 100
        pinch.ProcessTouch(new TouchEvent(1, 200f, 100f, TouchPhase.Began));
        Assert.Equal(GestureState.Began, pinch.State);
        Assert.Equal(1.0f, pinch.Scale);

        // Pointer 1 moves to (300, 100) -> distance = 200 -> Scale = 2.0
        pinch.ProcessTouch(new TouchEvent(1, 300f, 100f, TouchPhase.Moved));
        Assert.Equal(GestureState.Changed, pinch.State);
        Assert.Equal(2.0f, pinch.Scale, 2);

        // Pointer 1 ends
        pinch.ProcessTouch(new TouchEvent(1, 300f, 100f, TouchPhase.Ended));
        Assert.Equal(GestureState.Ended, pinch.State);
    }

    [Fact]
    public void TapGestureRecognizer_InvokesTapped()
    {
        var tap = new TapGestureRecognizer();
        bool wasTapped = false;
        tap.Tapped += () => wasTapped = true;

        tap.ProcessTouch(new TouchEvent(0, 50f, 50f, TouchPhase.Began));
        tap.ProcessTouch(new TouchEvent(0, 52f, 51f, TouchPhase.Ended));

        Assert.True(wasTapped);
        Assert.Equal(GestureState.Ended, tap.State);
    }

    [Fact]
    public void TouchDispatcher_DistributesToRegisteredRecognizers()
    {
        var dispatcher = new TouchDispatcher();
        var tap = new TapGestureRecognizer();
        bool tapped = false;
        tap.Tapped += () => tapped = true;

        dispatcher.RegisterRecognizer(tap);

        dispatcher.Dispatch(new TouchEvent(0, 10f, 10f, TouchPhase.Began));
        dispatcher.Dispatch(new TouchEvent(0, 10f, 10f, TouchPhase.Ended));

        Assert.True(tapped);
    }
}

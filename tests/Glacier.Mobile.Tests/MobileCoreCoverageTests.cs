namespace Glacier.Mobile.Tests;

using System;
using Glacier.Mobile.Input;
using Glacier.Mobile.Rendering;
using Glacier.Mobile.UI;
using Glacier.Mobile.UI.Controls;
using Glacier.Mobile.UI.Navigation;
using Xunit;

public class MobileCoreCoverageTests
{
    [Fact]
    public void NavigationStack_PushPopMeasureArrangeHitTest_WorkCorrectly()
    {
        var stack = new NavigationStack();
        Assert.Equal(0, stack.Depth);
        Assert.Null(stack.CurrentPage);
        Assert.Null(stack.Pop());

        var home = new MobilePage("Home") { Content = new Label("Welcome") };
        var detail = new MobilePage("Detail") { Content = new Button("Back") };

        stack.Add(home);
        stack.Push(detail);
        Assert.Equal(2, stack.Depth);
        Assert.Same(detail, stack.CurrentPage);
        Assert.Same(stack, detail.Parent);

        stack.Measure(400f, 800f);
        stack.Arrange(0f, 0f, 400f, 800f);
        Assert.Equal(400f, detail.ActualWidth);
        Assert.Equal(800f, detail.ActualHeight);

        var hit = stack.HitTest(10f, 10f);
        Assert.NotNull(hit);

        var popped = stack.Pop();
        Assert.Same(detail, popped);
        Assert.Null(detail.Parent);
        Assert.Same(home, stack.CurrentPage);
    }

    [Fact]
    public void PointerCaptureTable_DirectAndOverflowSlots_WorkCorrectly()
    {
        var table = new PointerCaptureTable();
        var v1 = new Label("One");
        var v2 = new Label("Two");

        Assert.Throws<ArgumentNullException>(() => table.Capture(0, null!));

        table.Capture(3, v1);
        table.Capture(42, v2); // overflow (>31)

        Assert.Same(v1, table.GetCaptured(3));
        Assert.Same(v2, table.GetCaptured(42));
        Assert.Null(table.GetCaptured(99));

        table.Release(3);
        Assert.Null(table.GetCaptured(3));
        Assert.Same(v2, table.GetCaptured(42));

        table.Clear();
        Assert.Null(table.GetCaptured(42));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    [InlineData(3)]
    public void TouchRingBuffer_NonPowerOfTwoCapacity_ThrowsArgumentException(int capacity)
    {
        Assert.Throws<ArgumentException>(() => new TouchRingBuffer(capacity));
    }

    [Fact]
    public void TouchRingBuffer_EnqueueDequeueDrainClear_AndSnapshotEquality()
    {
        var ring = new TouchRingBuffer(4);
        Assert.Equal(4, ring.Capacity);
        Assert.Equal(0, ring.Count);
        Assert.False(ring.TryDequeue(out _));

        var s1 = new TouchSnapshot(100, 1, 10f, 20f, TouchPhase.Began);
        var s2 = new TouchSnapshot(100, 1, 10f, 20f, TouchPhase.Began);
        var s3 = new TouchSnapshot(200, 1, 15f, 25f, TouchPhase.Moved);
        Assert.True(s1 == s2);
        Assert.True(s1 != s3);
        Assert.Equal(s1.GetHashCode(), s2.GetHashCode());

        Assert.True(ring.TryEnqueue(s1));
        Assert.True(ring.TryEnqueue(s3));
        Assert.True(ring.TryEnqueue(s1));
        Assert.True(ring.TryEnqueue(s3));
        Assert.False(ring.TryEnqueue(s1)); // full
        Assert.Equal(4, ring.Count);

        Span<TouchSnapshot> drained = stackalloc TouchSnapshot[2];
        int count = ring.Drain(drained);
        Assert.Equal(2, count);
        Assert.Equal(s1, drained[0]);
        Assert.Equal(s3, drained[1]);
        Assert.Equal(2, ring.Count);

        ring.Clear();
        Assert.Equal(0, ring.Count);
        Assert.False(ring.TryDequeue(out _));
    }

    [Fact]
    public void DisplayMetrics_Thickness_AndColor4_ComputePropertiesCorrectly()
    {
        var phone = DisplayMetrics.DefaultPhone;
        Assert.Equal(phone.Width * phone.ScaleFactor, phone.PhysicalWidth);
        Assert.Equal(phone.Height * phone.ScaleFactor, phone.PhysicalHeight);
        Assert.InRange(phone.FrameIntervalSeconds, 0.008f, 0.009f);

        var t = new Thickness(10f, 5f);
        Assert.Equal(20f, t.Horizontal);
        Assert.Equal(10f, t.Vertical);

        var sk = Color4.GlacierBlue.ToSKColor();
        Assert.Equal(50, sk.Red);
        Assert.Equal(180, sk.Green);
        Assert.Equal(255, sk.Blue);
    }
}

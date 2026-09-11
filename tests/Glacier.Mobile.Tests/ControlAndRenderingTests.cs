namespace Glacier.Mobile.Tests;

using System;
using Glacier.Mobile.Rendering;
using Glacier.Mobile.UI;
using Glacier.Mobile.UI.Controls;
using Glacier.Mobile.UI.Layouts;
using Glacier.Mobile.UI.Navigation;
using Xunit;

public class ControlAndRenderingTests
{
    [Fact]
    public void Label_MeasuresAndRendersWithoutExceptions()
    {
        using var compositor = new HeadlessCompositor(400, 200);
        var label = new Label("Hello Glacier.Mobile") { FontSize = 18f, IsBold = true };

        compositor.BeginFrame();
        compositor.RenderTree(label, 400f, 200f);
        compositor.EndFrame();

        Assert.True(label.ActualWidth > 0f);
        Assert.True(label.ActualHeight > 0f);
        Assert.Equal(1, compositor.RenderedFrameCount);

        byte[] png = compositor.EncodeToPng();
        Assert.NotEmpty(png);
    }

    [Fact]
    public void Button_OnClick_TriggersOnTap()
    {
        bool clicked = false;
        var btn = new Button("Click Me", onClick: () => clicked = true)
        {
            Width = 120f,
            Height = 44f
        };

        btn.Arrange(10f, 10f, 120f, 44f);

        // Process tap on button
        btn.ProcessTouch(new Glacier.Mobile.Input.TouchEvent(0, 50f, 20f, Glacier.Mobile.Input.TouchPhase.Began));
        btn.ProcessTouch(new Glacier.Mobile.Input.TouchEvent(0, 50f, 20f, Glacier.Mobile.Input.TouchPhase.Ended));

        Assert.True(clicked);
    }

    [Fact]
    public void HeadlessCompositor_RendersCompleteScreenHierarchy()
    {
        using var compositor = new HeadlessCompositor(393, 852);

        var screen = new Screen("Dashboard")
        {
            Content = new Column(spacing: 12f)
            {
                new Header("Glacier.Mobile Dashboard"),
                new Label("Zero-Allocation Native AOT Runtime"),
                new Button("Action Button")
            }
        };

        for (int frame = 0; frame < 10; frame++)
        {
            compositor.BeginFrame();
            compositor.RenderTree(screen, 393f, 852f);
            compositor.EndFrame();
        }

        Assert.Equal(10, compositor.RenderedFrameCount);
    }
}

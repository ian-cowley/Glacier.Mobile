namespace Glacier.Mobile.UI.Navigation;

using System;
using Glacier.Mobile.UI;
using SkiaSharp;

/// <summary>
/// Top-level mobile screen container representing an individual application view.
/// </summary>
public class MobilePage : MobileView
{
    public string Title { get; set; } = string.Empty;
    public MobileView? Content { get; set; }

    public MobilePage(string title = "")
    {
        Title = title;
        BackgroundColor = Color4.DarkBackground;
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        ActualWidth = availableWidth;
        ActualHeight = availableHeight;
        Content?.Measure(availableWidth, availableHeight);
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
        Content?.Arrange(X, Y, ActualWidth, ActualHeight);
    }

    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        base.Render(canvas);
        Content?.Render(canvas);
    }

    public override MobileView? HitTest(float px, float py)
    {
        if (!IsVisible || !IsEnabled) return null;
        var hit = Content?.HitTest(px, py);
        return hit ?? base.HitTest(px, py);
    }
}

/// <summary>
/// Alias for MobilePage supporting declarative syntax: new Screen("Dashboard") { Content = ... }
/// </summary>
public sealed class Screen : MobilePage
{
    public Screen(string title = "") : base(title) { }
}

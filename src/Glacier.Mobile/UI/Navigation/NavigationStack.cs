namespace Glacier.Mobile.UI.Navigation;

using System;
using System.Collections;
using System.Collections.Generic;
using Glacier.Mobile.UI;
using SkiaSharp;

/// <summary>
/// Push/pop navigation container managing hierarchical page transitions.
/// Supports declarative syntax: new NavigationStack { new Screen("Home") { ... } }
/// </summary>
public sealed class NavigationStack : MobileView, IEnumerable<MobilePage>
{
    private readonly Stack<MobilePage> _pages = new();

    public MobilePage? CurrentPage => _pages.Count > 0 ? _pages.Peek() : null;
    public int Depth => _pages.Count;

    public void Add(MobilePage page)
    {
        Push(page);
    }

    public void Push(MobilePage page)
    {
        page.Parent = this;
        _pages.Push(page);
    }

    public MobilePage? Pop()
    {
        if (_pages.Count > 0)
        {
            var page = _pages.Pop();
            page.Parent = null;
            return page;
        }
        return null;
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        ActualWidth = availableWidth;
        ActualHeight = availableHeight;
        CurrentPage?.Measure(availableWidth, availableHeight);
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
        CurrentPage?.Arrange(X, Y, ActualWidth, ActualHeight);
    }

    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        base.Render(canvas);
        CurrentPage?.Render(canvas);
    }

    public override MobileView? HitTest(float px, float py)
    {
        if (!IsVisible || !IsEnabled) return null;
        var hit = CurrentPage?.HitTest(px, py);
        return hit ?? base.HitTest(px, py);
    }

    public IEnumerator<MobilePage> GetEnumerator() => _pages.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

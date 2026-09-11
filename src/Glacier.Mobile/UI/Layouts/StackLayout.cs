namespace Glacier.Mobile.UI.Layouts;

using System;
using System.Collections;
using System.Collections.Generic;
using Glacier.Mobile.Input;
using SkiaSharp;

public enum StackOrientation : byte
{
    Vertical,
    Horizontal
}

/// <summary>
/// High-throughput linear layout container for organizing children sequentially.
/// </summary>
public class StackLayout : MobileView, IEnumerable<MobileView>
{
    protected readonly List<MobileView> _children = new();

    public StackOrientation Orientation { get; set; } = StackOrientation.Vertical;
    public float Spacing { get; set; } = 8f;
    public IReadOnlyList<MobileView> Children => _children;

    public void Add(MobileView child)
    {
        child.Parent = this;
        _children.Add(child);
    }

    public bool Remove(MobileView child)
    {
        if (_children.Remove(child))
        {
            child.Parent = null;
            return true;
        }
        return false;
    }

    public void Clear()
    {
        foreach (var c in _children) c.Parent = null;
        _children.Clear();
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        float availW = MathF.Max(0f, availableWidth - Margin.Horizontal - Padding.Horizontal);
        float availH = MathF.Max(0f, availableHeight - Margin.Vertical - Padding.Vertical);

        float totalW = 0f;
        float totalH = 0f;

        for (int i = 0; i < _children.Count; i++)
        {
            var child = _children[i];
            if (!child.IsVisible) continue;

            child.Measure(availW, availH);

            if (Orientation == StackOrientation.Vertical)
            {
                totalH += child.ActualHeight + (i > 0 ? Spacing : 0f);
                totalW = MathF.Max(totalW, child.ActualWidth);
            }
            else
            {
                totalW += child.ActualWidth + (i > 0 ? Spacing : 0f);
                totalH = MathF.Max(totalH, child.ActualHeight);
            }
        }

        ActualWidth = !float.IsNaN(Width) ? Width : totalW + Padding.Horizontal;
        ActualHeight = !float.IsNaN(Height) ? Height : totalH + Padding.Vertical;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        float curX = X + Padding.Left;
        float curY = Y + Padding.Top;
        float innerW = MathF.Max(0f, ActualWidth - Padding.Horizontal);
        float innerH = MathF.Max(0f, ActualHeight - Padding.Vertical);

        for (int i = 0; i < _children.Count; i++)
        {
            var child = _children[i];
            if (!child.IsVisible) continue;

            if (Orientation == StackOrientation.Vertical)
            {
                float childW = !float.IsNaN(child.Width) ? child.Width : innerW;
                child.Arrange(curX, curY, childW, child.ActualHeight);
                curY += child.ActualHeight + Spacing;
            }
            else
            {
                float childH = !float.IsNaN(child.Height) ? child.Height : innerH;
                child.Arrange(curX, curY, child.ActualWidth, childH);
                curX += child.ActualWidth + Spacing;
            }
        }
    }

    public override void Render(SKCanvas canvas)
    {
        base.Render(canvas);
        for (int i = 0; i < _children.Count; i++)
        {
            _children[i].Render(canvas);
        }
    }

    public override MobileView? HitTest(float px, float py)
    {
        if (!IsVisible || !IsEnabled) return null;
        for (int i = _children.Count - 1; i >= 0; i--)
        {
            var hit = _children[i].HitTest(px, py);
            if (hit != null) return hit;
        }
        return base.HitTest(px, py);
    }

    public IEnumerator<MobileView> GetEnumerator() => _children.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

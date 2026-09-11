namespace Glacier.Mobile.UI;

using System;
using System.Collections.Generic;
using Glacier.Mobile.Input;
using SkiaSharp;

/// <summary>
/// Base class for all high-performance retained-mode visual elements in Glacier.Mobile.
/// Provides zero-allocation layout measurement, hit-testing, and GPU-accelerated Skia rendering.
/// </summary>
public abstract class MobileView
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; } = float.NaN;
    public float Height { get; set; } = float.NaN;
    public float ActualWidth { get; protected set; }
    public float ActualHeight { get; protected set; }

    public Thickness Margin { get; set; } = Thickness.Zero;
    public Thickness Padding { get; set; } = Thickness.Zero;
    public Color4 BackgroundColor { get; set; } = Color4.Transparent;
    public bool IsVisible { get; set; } = true;
    public bool IsEnabled { get; set; } = true;

    public MobileView? Parent { get; internal set; }
    public List<IGestureRecognizer> GestureRecognizers { get; } = new();

    public virtual void Measure(float availableWidth, float availableHeight)
    {
        float measuredW = !float.IsNaN(Width) ? Width : availableWidth - Margin.Horizontal;
        float measuredH = !float.IsNaN(Height) ? Height : availableHeight - Margin.Vertical;
        ActualWidth = MathF.Max(0f, measuredW);
        ActualHeight = MathF.Max(0f, measuredH);
    }

    public virtual void Arrange(float x, float y, float width, float height)
    {
        X = x + Margin.Left;
        Y = y + Margin.Top;
        if (!float.IsNaN(Width))
            ActualWidth = MathF.Max(0f, width - Margin.Horizontal);
        if (!float.IsNaN(Height))
            ActualHeight = MathF.Max(0f, height - Margin.Vertical);
    }

    public virtual void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;

        if (BackgroundColor.A > 0)
        {
            using var paint = new SKPaint { Color = BackgroundColor.ToSKColor(), Style = SKPaintStyle.Fill };
            canvas.DrawRect(X, Y, ActualWidth, ActualHeight, paint);
        }
    }

    public virtual MobileView? HitTest(float px, float py)
    {
        if (!IsVisible || !IsEnabled) return null;
        if (px >= X && px <= X + ActualWidth && py >= Y && py <= Y + ActualHeight)
        {
            return this;
        }
        return null;
    }

    public virtual void ProcessTouch(in TouchEvent touch)
    {
        for (int i = 0; i < GestureRecognizers.Count; i++)
        {
            GestureRecognizers[i].ProcessTouch(in touch);
        }
    }
}

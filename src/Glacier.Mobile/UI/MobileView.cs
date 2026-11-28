namespace Glacier.Mobile.UI;

using System;
using System.Collections.Generic;
using Glacier.Mobile.Input;
using SkiaSharp;

/// <summary>
/// Base class for all retained-mode visual elements in Glacier.Mobile.
/// Provides layout measurement, hit-testing, touch bubbling, and SkiaSharp rendering.
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
        ActualWidth = !float.IsNaN(Width) ? MathF.Max(0f, Width) : MathF.Max(0f, width - Margin.Horizontal);
        ActualHeight = !float.IsNaN(Height) ? MathF.Max(0f, Height) : MathF.Max(0f, height - Margin.Vertical);
    }

    public virtual void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;

        if (BackgroundColor.A > 0)
        {
            var paint = SkiaScratchCache.AcquireFillPaint(BackgroundColor.ToSKColor(), isAntialias: false);
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

    public virtual void Update(float dt)
    {
    }

    public virtual void ProcessTouch(in TouchEvent touch)
    {
        bool consumed = false;
        for (int i = 0; i < GestureRecognizers.Count; i++)
        {
            var recognizer = GestureRecognizers[i];
            recognizer.ProcessTouch(in touch);
            if (recognizer is PanGestureRecognizer or PinchGestureRecognizer)
            {
                if (recognizer.State == GestureState.Began || recognizer.State == GestureState.Changed)
                {
                    consumed = true;
                }
            }
        }

        if (!consumed)
        {
            Parent?.ProcessTouch(in touch);
        }
    }
}

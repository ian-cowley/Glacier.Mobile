namespace Glacier.Mobile.Interop;

using System;
using Glacier.Mobile.Input;
using Glacier.Mobile.UI;
using Glacier.Plot.Figures;
using SkiaSharp;

/// <summary>
/// Native mobile viewport component hosting a high-performance Glacier.Plot Figure.
/// Supports touch gestures for interactive pan and pinch-to-zoom on mobile devices.
/// </summary>
public sealed class PlotView : MobileView
{
    private readonly PanGestureRecognizer _panRecognizer;
    private readonly PinchGestureRecognizer _pinchRecognizer;

    public Figure Figure { get; set; }

    public PlotView(Figure? figure = null)
    {
        Figure = figure ?? new Figure();
        BackgroundColor = Color4.CardBackground;
        Padding = new Thickness(8f);

        _panRecognizer = new PanGestureRecognizer();
        _panRecognizer.StateChanged += OnPan;
        GestureRecognizers.Add(_panRecognizer);

        _pinchRecognizer = new PinchGestureRecognizer();
        _pinchRecognizer.StateChanged += OnPinch;
        GestureRecognizers.Add(_pinchRecognizer);
    }

    private void OnPan(PanGestureRecognizer pan)
    {
        if (pan.State == GestureState.Changed)
        {
            // Pan interaction: Shift axis limits
            float dx = pan.TranslationX * 0.01f;
            float dy = pan.TranslationY * 0.01f;
            Figure.XAxis.Pan(dx);
            Figure.YAxis.Pan(dy);
        }
    }

    private void OnPinch(PinchGestureRecognizer pinch)
    {
        if (pinch.State == GestureState.Changed)
        {
            // Pinch interaction: Zoom axis limits
            Figure.XAxis.Zoom(pinch.Scale);
            Figure.YAxis.Zoom(pinch.Scale);
        }
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        float w = !float.IsNaN(Width) ? Width : availableWidth;
        float h = !float.IsNaN(Height) ? Height : MathF.Min(availableHeight, 300f);
        ActualWidth = w;
        ActualHeight = h;
    }

    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        base.Render(canvas);

        canvas.Save();
        canvas.ClipRect(new SKRect(X, Y, X + ActualWidth, Y + ActualHeight));
        canvas.Translate(X, Y);

        Figure.Render(canvas, (int)ActualWidth, (int)ActualHeight);

        canvas.Restore();
    }
}

namespace Glacier.Mobile.UI.Controls;

using System;
using Glacier.Mobile.Input;
using SkiaSharp;

/// <summary>
/// Touch-responsive mobile button supporting click callbacks and rounded corners.
/// </summary>
public sealed class Button : MobileView
{
    private readonly TapGestureRecognizer _tapRecognizer;

    public string Text { get; set; } = string.Empty;
    public float FontSize { get; set; } = 15f;
    public Color4 TextColor { get; set; } = Color4.White;
    public float CornerRadius { get; set; } = 8f;
    public Action? OnClick { get; set; }

    public Button(string text = "Button", Action? onClick = null)
    {
        Text = text;
        OnClick = onClick;
        BackgroundColor = Color4.GlacierBlue;
        Padding = new Thickness(16f, 10f);

        _tapRecognizer = new TapGestureRecognizer();
        _tapRecognizer.Tapped += () => OnClick?.Invoke();
        GestureRecognizers.Add(_tapRecognizer);
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        using var paint = new SKPaint
        {
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
            TextSize = FontSize,
            IsAntialias = true
        };
        float textW = paint.MeasureText(Text);

        float measuredW = !float.IsNaN(Width) ? Width : textW + Padding.Horizontal + Margin.Horizontal;
        float measuredH = !float.IsNaN(Height) ? Height : FontSize + Padding.Vertical + Margin.Vertical + 4f;

        ActualWidth = MathF.Min(availableWidth, measuredW);
        ActualHeight = MathF.Min(availableHeight, measuredH);
    }

    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;

        // Draw rounded button background
        using var bgPaint = new SKPaint
        {
            Color = BackgroundColor.ToSKColor(),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        var rect = new SKRoundRect(new SKRect(X, Y, X + ActualWidth, Y + ActualHeight), CornerRadius);
        canvas.DrawRoundRect(rect, bgPaint);

        // Draw button label
        if (!string.IsNullOrEmpty(Text))
        {
            using var textPaint = new SKPaint
            {
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
                TextSize = FontSize,
                Color = TextColor.ToSKColor(),
                IsAntialias = true
            };

            float textW = textPaint.MeasureText(Text);
            float textX = X + (ActualWidth - textW) * 0.5f;
            float textY = Y + (ActualHeight + FontSize * 0.75f) * 0.5f;

            canvas.DrawText(Text, textX, textY, textPaint);
        }
    }
}

namespace Glacier.Mobile.UI.Controls;

using System;
using SkiaSharp;

public enum TextAlignment : byte
{
    Start,
    Center,
    End
}

/// <summary>
/// Fast native text label rendered with GPU Skia fonts.
/// </summary>
public class Label : MobileView
{
    private string _text;
    public string Text
    {
        get => _text;
        set => _text = value ?? string.Empty;
    }

    public float FontSize { get; set; } = 16f;
    public Color4 TextColor { get; set; } = Color4.White;
    public bool IsBold { get; set; } = false;
    public TextAlignment Alignment { get; set; } = TextAlignment.Start;

    public Label(string text = "")
    {
        _text = text ?? string.Empty;
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        using var paint = new SKPaint
        {
            Typeface = SKTypeface.FromFamilyName("Arial", IsBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
            TextSize = FontSize,
            IsAntialias = true
        };
        var textBounds = new SKRect();
        paint.MeasureText(Text, ref textBounds);

        float measuredW = !float.IsNaN(Width) ? Width : textBounds.Width + Margin.Horizontal + Padding.Horizontal;
        float measuredH = !float.IsNaN(Height) ? Height : FontSize * 1.3f + Margin.Vertical + Padding.Vertical;

        ActualWidth = MathF.Min(availableWidth, measuredW);
        ActualHeight = MathF.Min(availableHeight, measuredH);
    }

    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible || string.IsNullOrEmpty(Text)) return;
        base.Render(canvas);

        using var paint = new SKPaint
        {
            Typeface = SKTypeface.FromFamilyName("Arial", IsBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
            TextSize = FontSize,
            Color = TextColor.ToSKColor(),
            IsAntialias = true
        };

        float textX = X + Padding.Left;
        if (Alignment == TextAlignment.Center)
        {
            float w = paint.MeasureText(Text);
            textX = X + (ActualWidth - w) * 0.5f;
        }
        else if (Alignment == TextAlignment.End)
        {
            float w = paint.MeasureText(Text);
            textX = X + ActualWidth - Padding.Right - w;
        }

        float textY = Y + Padding.Top + FontSize;
        canvas.DrawText(Text, textX, textY, paint);
    }
}

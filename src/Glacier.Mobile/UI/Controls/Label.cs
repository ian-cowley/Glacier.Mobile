namespace Glacier.Mobile.UI.Controls;

using System;
using System.IO;
using SkiaSharp;

public enum TextAlignment : byte
{
    Start,
    Center,
    End
}

/// <summary>
/// Text label rendered with SkiaSharp fonts.
/// </summary>
public class Label : MobileView
{
    internal static readonly SKTypeface RegularTypeface = ResolveTypeface(SKFontStyleWeight.Normal);

    internal static readonly SKTypeface SemiBoldTypeface = ResolveTypeface(SKFontStyleWeight.SemiBold);

    internal static readonly SKTypeface BoldTypeface = ResolveTypeface(SKFontStyleWeight.Bold);

    private static SKTypeface ResolveTypeface(SKFontStyleWeight weight)
    {
        bool bold = weight >= SKFontStyleWeight.SemiBold;
        string[] families = ["Arial", "DejaVu Sans", "Liberation Sans", "Noto Sans", "Segoe UI"];
        foreach (string family in families)
        {
            var tf = SKTypeface.FromFamilyName(family, weight, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
            if (tf != null)
            {
                if (tf.GlyphCount > 0)
                {
                    return tf;
                }
                tf.Dispose();
            }
        }

        // Fallback for SkiaSharp.NativeAssets.Linux.NoDependencies (no libfontconfig): load system TTF directly
        string[] candidatePaths = bold
            ? [
                "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
                "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",
                "/usr/share/fonts/TTF/DejaVuSans-Bold.ttf",
                "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
                "/System/Library/Fonts/Supplemental/Arial Bold.ttf"
              ]
            : [
                "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
                "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
                "/usr/share/fonts/TTF/DejaVuSans.ttf",
                "/System/Library/Fonts/Supplemental/Arial.ttf"
              ];

        foreach (string path in candidatePaths)
        {
            if (File.Exists(path))
            {
                var fileTf = SKTypeface.FromFile(path);
                if (fileTf != null)
                {
                    if (fileTf.GlyphCount > 0)
                    {
                        return fileTf;
                    }
                    fileTf.Dispose();
                }
            }
        }

        return SKTypeface.Default;
    }

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
        var paint = SkiaScratchCache.AcquireTextPaint(
            IsBold ? BoldTypeface : RegularTypeface,
            FontSize,
            TextColor.ToSKColor(),
            isAntialias: true);
        var textBounds = new SKRect();
        paint.MeasureText(Text, ref textBounds);

        float textWidth = textBounds.Width;
        if (textWidth <= 0f && !string.IsNullOrEmpty(Text))
        {
            textWidth = Text.Length * FontSize * 0.55f;
        }

        float measuredW = !float.IsNaN(Width) ? Width : textWidth + Margin.Horizontal + Padding.Horizontal;
        float measuredH = !float.IsNaN(Height) ? Height : FontSize * 1.3f + Margin.Vertical + Padding.Vertical;

        ActualWidth = MathF.Min(availableWidth, measuredW);
        ActualHeight = MathF.Min(availableHeight, measuredH);
    }

    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible || string.IsNullOrEmpty(Text)) return;
        base.Render(canvas);

        var paint = SkiaScratchCache.AcquireTextPaint(
            IsBold ? BoldTypeface : RegularTypeface,
            FontSize,
            TextColor.ToSKColor(),
            isAntialias: true);

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

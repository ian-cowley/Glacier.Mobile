namespace Glacier.Mobile.UI;

using System;
using SkiaSharp;

/// <summary>
/// Thread-local scratch cache of SkiaSharp paint and round-rect objects to eliminate per-frame native allocations.
/// </summary>
internal static class SkiaScratchCache
{
    [ThreadStatic]
    private static SKPaint? t_fillPaint;

    [ThreadStatic]
    private static SKPaint? t_textPaint;

    [ThreadStatic]
    private static SKRoundRect? t_roundRect;

    public static SKPaint AcquireFillPaint(SKColor color, bool isAntialias = false)
    {
        var paint = t_fillPaint ??= new SKPaint();
        paint.Color = color;
        paint.Style = SKPaintStyle.Fill;
        paint.IsAntialias = isAntialias;
        return paint;
    }

    public static SKPaint AcquireTextPaint(SKTypeface typeface, float textSize, SKColor color, bool isAntialias = true)
    {
        var paint = t_textPaint ??= new SKPaint();
        paint.Typeface = typeface;
        paint.TextSize = textSize;
        paint.Color = color;
        paint.Style = SKPaintStyle.Fill;
        paint.IsAntialias = isAntialias;
        return paint;
    }

    public static SKRoundRect AcquireRoundRect(float left, float top, float right, float bottom, float radius)
    {
        var rect = t_roundRect ??= new SKRoundRect();
        rect.SetRect(new SKRect(left, top, right, bottom), radius, radius);
        return rect;
    }
}

namespace Glacier.Mobile.Rendering;

using System;
using System.IO;
using Glacier.Mobile.UI;
using SkiaSharp;

/// <summary>
/// Deterministic headless software compositor for CI test runners, snapshot verification, and benchmarks.
/// </summary>
public sealed class HeadlessCompositor : ICompositor
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private bool _disposed;

    public int Width { get; }
    public int Height { get; }
    public long RenderedFrameCount { get; private set; }

    public HeadlessCompositor(int width = 393, int height = 852)
    {
        Width = width;
        Height = height;
        _bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        _canvas = new SKCanvas(_bitmap);
    }

    public void BeginFrame()
    {
        _canvas.Clear(SKColors.Transparent);
    }

    public void RenderTree(MobileView root, float width, float height)
    {
        root.Measure(width, height);
        root.Arrange(0f, 0f, width, height);
        root.Render(_canvas);
        RenderedFrameCount++;
    }

    public void EndFrame()
    {
        _canvas.Flush();
    }

    public byte[] EncodeToPng()
    {
        using var image = SKImage.FromBitmap(_bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _canvas.Dispose();
            _bitmap.Dispose();
            _disposed = true;
        }
    }
}

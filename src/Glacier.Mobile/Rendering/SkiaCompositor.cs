namespace Glacier.Mobile.Rendering;

using System;
using Glacier.Mobile.UI;
using SkiaSharp;

/// <summary>
/// GPU-accelerated SkiaSharp compositor targeting Metal (iOS) and Vulkan (Android).
/// </summary>
public sealed class SkiaCompositor : ICompositor
{
    private readonly SKCanvas _canvas;
    private readonly bool _ownsCanvas;
    private bool _disposed;

    public SkiaCompositor(SKCanvas canvas, bool ownsCanvas = false)
    {
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        _ownsCanvas = ownsCanvas;
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
    }

    public void EndFrame()
    {
        _canvas.Flush();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_ownsCanvas)
            {
                _canvas.Dispose();
            }
            _disposed = true;
        }
    }
}

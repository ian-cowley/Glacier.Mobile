namespace Glacier.Mobile.Rendering;

using System;
using Glacier.Mobile.UI;
using SkiaSharp;

/// <summary>
/// GPU-accelerated SkiaSharp compositor targeting Metal (iOS) and Vulkan (Android).
/// Supports rendering either via an IMobileSwapchain HAL or directly to an external canvas.
/// </summary>
public sealed class SkiaCompositor : ICompositor
{
    private readonly SKCanvas? _externalCanvas;
    private readonly bool _ownsCanvas;
    private IHardwareFrame? _currentFrame;
    private bool _disposed;

    public IMobileSwapchain? Swapchain { get; }

    public SkiaCompositor(SKCanvas canvas, bool ownsCanvas = false)
    {
        _externalCanvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        _ownsCanvas = ownsCanvas;
        Swapchain = null;
    }

    public SkiaCompositor(IMobileSwapchain swapchain)
    {
        Swapchain = swapchain ?? throw new ArgumentNullException(nameof(swapchain));
        _externalCanvas = null;
        _ownsCanvas = false;
    }

    public void BeginFrame()
    {
        if (Swapchain != null)
        {
            _currentFrame = Swapchain.AcquireNextFrame();
            _currentFrame.Canvas.Clear(SKColors.Transparent);
        }
        else if (_externalCanvas != null)
        {
            _externalCanvas.Clear(SKColors.Transparent);
        }
    }

    public void RenderTree(MobileView root, float width, float height)
    {
        var canvas = _currentFrame?.Canvas ?? _externalCanvas;
        if (canvas == null) return;

        root.Measure(width, height);
        root.Arrange(0f, 0f, width, height);
        root.Render(canvas);
    }

    public void EndFrame()
    {
        if (Swapchain != null && _currentFrame != null)
        {
            Swapchain.Present(_currentFrame);
            _currentFrame.Dispose();
            _currentFrame = null;
        }
        else if (_externalCanvas != null)
        {
            _externalCanvas.Flush();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _currentFrame?.Dispose();
            _currentFrame = null;

            if (_ownsCanvas && _externalCanvas != null)
            {
                _externalCanvas.Dispose();
            }

            Swapchain?.Dispose();
            _disposed = true;
        }
    }
}

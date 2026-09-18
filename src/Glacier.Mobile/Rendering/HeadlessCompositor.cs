namespace Glacier.Mobile.Rendering;

using System;
using System.IO;
using Glacier.Mobile.UI;
using SkiaSharp;

/// <summary>
/// Deterministic headless software compositor for CI test runners, snapshot verification, and benchmarks.
/// Utilizes a HeadlessSwapchain to decouple rendering from desktop canvas.
/// </summary>
public sealed class HeadlessCompositor : ICompositor
{
    private readonly HeadlessSwapchain _swapchain;
    private IHardwareFrame? _currentFrame;
    private bool _disposed;

    public IMobileSwapchain? Swapchain => _swapchain;
    public int Width => _swapchain.Width;
    public int Height => _swapchain.Height;
    public long RenderedFrameCount { get; private set; }

    public HeadlessCompositor(int width = 393, int height = 852)
    {
        _swapchain = new HeadlessSwapchain(width, height);
    }

    public HeadlessCompositor(HeadlessSwapchain swapchain)
    {
        _swapchain = swapchain ?? throw new ArgumentNullException(nameof(swapchain));
    }

    public void BeginFrame()
    {
        _currentFrame = _swapchain.AcquireNextFrame();
        _currentFrame.Canvas.Clear(SKColors.Transparent);
    }

    public void RenderTree(MobileView root, float width, float height)
    {
        if (_currentFrame == null) return;
        root.Measure(width, height);
        root.Arrange(0f, 0f, width, height);
        root.Render(_currentFrame.Canvas);
        RenderedFrameCount++;
    }

    public void EndFrame()
    {
        if (_currentFrame != null)
        {
            _swapchain.Present(_currentFrame);
            _currentFrame.Dispose();
            _currentFrame = null;
        }
    }

    public byte[] EncodeToPng()
    {
        using var image = SKImage.FromBitmap(_swapchain.GetCurrentFrontBitmap());
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _currentFrame?.Dispose();
            _currentFrame = null;
            _swapchain.Dispose();
            _disposed = true;
        }
    }
}

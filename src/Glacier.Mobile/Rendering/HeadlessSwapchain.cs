namespace Glacier.Mobile.Rendering;

using System;
using SkiaSharp;

/// <summary>
/// Headless software swapchain decoupling rendering from desktop canvas,
/// providing double-buffered off-screen frames for deterministic CI test runners and benchmarks.
/// </summary>
public sealed class HeadlessSwapchain : IMobileSwapchain
{
    private readonly SKBitmap[] _buffers;
    private readonly SKCanvas[] _canvases;
    private int _bufferIndex;
    private long _frameIndex;
    private bool _disposed;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public float DensityScale { get; private set; }
    public SwapchainFormat Format => SwapchainFormat.Rgba8888;

    public HeadlessSwapchain(int width = 393, int height = 852, float densityScale = 1.0f, int bufferCount = 2)
    {
        if (bufferCount <= 0) throw new ArgumentOutOfRangeException(nameof(bufferCount));
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        DensityScale = densityScale > 0f ? densityScale : 1.0f;

        _buffers = new SKBitmap[bufferCount];
        _canvases = new SKCanvas[bufferCount];
        for (int i = 0; i < bufferCount; i++)
        {
            _buffers[i] = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            _canvases[i] = new SKCanvas(_buffers[i]);
        }
    }

    public void Resize(int width, int height, float densityScale)
    {
        if (width == Width && height == Height && MathF.Abs(densityScale - DensityScale) < 0.001f)
            return;

        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        DensityScale = densityScale > 0f ? densityScale : 1.0f;

        for (int i = 0; i < _buffers.Length; i++)
        {
            _canvases[i]?.Dispose();
            _buffers[i]?.Dispose();
            _buffers[i] = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            _canvases[i] = new SKCanvas(_buffers[i]);
        }
    }

    public IHardwareFrame AcquireNextFrame()
    {
        int idx = _bufferIndex;
        _bufferIndex = (_bufferIndex + 1) % _buffers.Length;
        _frameIndex++;
        return new HeadlessHardwareFrame(_canvases[idx], _frameIndex);
    }

    public void Present(IHardwareFrame frame)
    {
        if (frame is HeadlessHardwareFrame hf)
        {
            hf.Canvas.Flush();
        }
    }

    public SKBitmap GetCurrentFrontBitmap()
    {
        int frontIdx = (_bufferIndex + _buffers.Length - 1) % _buffers.Length;
        return _buffers[frontIdx];
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            for (int i = 0; i < _buffers.Length; i++)
            {
                _canvases[i]?.Dispose();
                _buffers[i]?.Dispose();
            }
            _disposed = true;
        }
    }

    private sealed class HeadlessHardwareFrame : IHardwareFrame
    {
        public SKCanvas Canvas { get; }
        public long FrameIndex { get; }

        public HeadlessHardwareFrame(SKCanvas canvas, long frameIndex)
        {
            Canvas = canvas;
            FrameIndex = frameIndex;
        }

        public void Dispose()
        {
            // Canvas lifecycle managed by swapchain buffers
        }
    }
}

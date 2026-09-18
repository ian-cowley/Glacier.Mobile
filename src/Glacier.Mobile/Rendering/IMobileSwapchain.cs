namespace Glacier.Mobile.Rendering;

using System;
using SkiaSharp;

/// <summary>
/// Pixel format for hardware mobile swapchains.
/// </summary>
public enum SwapchainFormat
{
    Rgba8888,
    Bgra8888,
    Rgb565
}

/// <summary>
/// Disposable frame lease acquired from an active mobile swapchain.
/// </summary>
public interface IHardwareFrame : IDisposable
{
    SKCanvas Canvas { get; }
    long FrameIndex { get; }
}

/// <summary>
/// Hardware Abstraction Layer (HAL) swapchain interface decoupling Glacier.Mobile rendering
/// from underlying platform windowing systems (Vulkan on Android, Metal on iOS, or Headless off-screen).
/// </summary>
public interface IMobileSwapchain : IDisposable
{
    int Width { get; }
    int Height { get; }
    float DensityScale { get; }
    SwapchainFormat Format { get; }

    void Resize(int width, int height, float densityScale);
    IHardwareFrame AcquireNextFrame();
    void Present(IHardwareFrame frame);
}

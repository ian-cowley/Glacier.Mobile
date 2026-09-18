namespace Glacier.Mobile.Rendering;

using System;
using Glacier.Mobile.UI;

/// <summary>
/// Hardware GPU / software compositor contract for Glacier.Mobile.
/// Decouples rendering from desktop canvas via IMobileSwapchain HAL.
/// </summary>
public interface ICompositor : IDisposable
{
    IMobileSwapchain? Swapchain { get; }
    void BeginFrame();
    void EndFrame();
    void RenderTree(MobileView root, float width, float height);
}

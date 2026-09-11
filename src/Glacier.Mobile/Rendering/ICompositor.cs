namespace Glacier.Mobile.Rendering;

using System;
using Glacier.Mobile.UI;
using SkiaSharp;

/// <summary>
/// Hardware GPU / software compositor contract for Glacier.Mobile.
/// </summary>
public interface ICompositor : IDisposable
{
    void BeginFrame();
    void EndFrame();
    void RenderTree(MobileView root, float width, float height);
}

namespace Glacier.Mobile.UI;

using System.Runtime.InteropServices;
using SkiaSharp;

/// <summary>
/// Fast unmanaged 32-bit RGBA color representation.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Color4(byte R, byte G, byte B, byte A = 255)
{
    public static readonly Color4 Transparent = new(0, 0, 0, 0);
    public static readonly Color4 Black = new(0, 0, 0, 255);
    public static readonly Color4 White = new(255, 255, 255, 255);
    public static readonly Color4 GlacierBlue = new(50, 180, 255, 255);
    public static readonly Color4 DarkBackground = new(18, 22, 28, 255);
    public static readonly Color4 CardBackground = new(28, 34, 44, 255);
    public static readonly Color4 AccentPurple = new(140, 80, 255, 255);

    public SKColor ToSKColor() => new(R, G, B, A);
}

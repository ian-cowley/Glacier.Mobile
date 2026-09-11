namespace Glacier.Mobile.Rendering;

/// <summary>
/// Physical and logical screen specifications for mobile devices.
/// </summary>
public record DisplayMetrics
{
    public float Width { get; init; } = 393f; // iPhone 16 / Pixel 8 logical width
    public float Height { get; init; } = 852f; // iPhone 16 / Pixel 8 logical height
    public float ScaleFactor { get; init; } = 3.0f; // Retina @3x
    public int TargetRefreshRateHz { get; init; } = 120; // ProMotion / Smooth Display
    public float FrameIntervalSeconds => 1.0f / TargetRefreshRateHz;

    public float PhysicalWidth => Width * ScaleFactor;
    public float PhysicalHeight => Height * ScaleFactor;

    public static readonly DisplayMetrics DefaultPhone = new();
    public static readonly DisplayMetrics DefaultTablet = new() { Width = 834f, Height = 1194f, ScaleFactor = 2.0f };
}

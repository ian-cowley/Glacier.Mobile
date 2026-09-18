namespace Glacier.Mobile.Input;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Transient, zero-allocation stack-only touch event for high-frequency 120Hz input dispatch.
/// Aligned to 24-byte natural alignment (8-byte boundary) without Pack=1.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly ref struct TouchEvent
{
    public readonly long Timestamp;   // 8 bytes (offset 0)
    public readonly float X;          // 4 bytes (offset 8)
    public readonly float Y;          // 4 bytes (offset 12)
    public readonly int PointerId;    // 4 bytes (offset 16)
    public readonly TouchPhase Phase; // 1 byte  (offset 20)
    private readonly byte _pad0;      // 1 byte  (offset 21)
    private readonly ushort _pad1;    // 2 bytes (offset 22) - Total 24 bytes

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TouchEvent(int id, float x, float y, TouchPhase phase)
    {
        Timestamp = Stopwatch.GetTimestamp();
        X = x;
        Y = y;
        PointerId = id;
        Phase = phase;
        _pad0 = 0;
        _pad1 = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TouchEvent(long timestamp, int id, float x, float y, TouchPhase phase)
    {
        Timestamp = timestamp;
        X = x;
        Y = y;
        PointerId = id;
        Phase = phase;
        _pad0 = 0;
        _pad1 = 0;
    }
}

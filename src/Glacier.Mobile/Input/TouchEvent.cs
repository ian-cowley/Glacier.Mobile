namespace Glacier.Mobile.Input;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Transient, zero-allocation stack-only touch event for high-frequency 120Hz input dispatch.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly ref struct TouchEvent
{
    public readonly long Timestamp;
    public readonly int PointerId;
    public readonly float X;
    public readonly float Y;
    public readonly TouchPhase Phase;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TouchEvent(int id, float x, float y, TouchPhase phase)
    {
        Timestamp = Stopwatch.GetTimestamp();
        PointerId = id;
        X = x;
        Y = y;
        Phase = phase;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TouchEvent(long timestamp, int id, float x, float y, TouchPhase phase)
    {
        Timestamp = timestamp;
        PointerId = id;
        X = x;
        Y = y;
        Phase = phase;
    }
}

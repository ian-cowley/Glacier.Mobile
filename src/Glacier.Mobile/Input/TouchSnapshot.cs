namespace Glacier.Mobile.Input;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Value snapshot struct of TouchEvent suitable for class fields, event queues, and temporal smoothing.
/// Stores durable pointer coordinates without violating C# ref struct field restrictions.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct TouchSnapshot
{
    public readonly long Timestamp;
    public readonly int PointerId;
    public readonly float X;
    public readonly float Y;
    public readonly TouchPhase Phase;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TouchSnapshot(long timestamp, int id, float x, float y, TouchPhase phase)
    {
        Timestamp = timestamp;
        PointerId = id;
        X = x;
        Y = y;
        Phase = phase;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TouchSnapshot(in TouchEvent e)
    {
        Timestamp = e.Timestamp;
        PointerId = e.PointerId;
        X = e.X;
        Y = e.Y;
        Phase = e.Phase;
    }
}

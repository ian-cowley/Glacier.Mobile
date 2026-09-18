namespace Glacier.Mobile.Input;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Value snapshot struct of TouchEvent suitable for class fields, event queues, and temporal smoothing.
/// Aligned to 24-byte natural alignment (8-byte boundary) for zero-penalty SIMD and PCIe transfer.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct TouchSnapshot : IEquatable<TouchSnapshot>
{
    public readonly long Timestamp;   // 8 bytes (offset 0)
    public readonly float X;          // 4 bytes (offset 8)
    public readonly float Y;          // 4 bytes (offset 12)
    public readonly int PointerId;    // 4 bytes (offset 16)
    public readonly TouchPhase Phase; // 1 byte  (offset 20)
    private readonly byte _pad0;      // 1 byte  (offset 21)
    private readonly ushort _pad1;    // 2 bytes (offset 22) - Total 24 bytes

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TouchSnapshot(long timestamp, int id, float x, float y, TouchPhase phase)
    {
        Timestamp = timestamp;
        X = x;
        Y = y;
        PointerId = id;
        Phase = phase;
        _pad0 = 0;
        _pad1 = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TouchSnapshot(in TouchEvent e)
    {
        Timestamp = e.Timestamp;
        X = e.X;
        Y = e.Y;
        PointerId = e.PointerId;
        Phase = e.Phase;
        _pad0 = 0;
        _pad1 = 0;
    }

    public bool Equals(TouchSnapshot other) =>
        Timestamp == other.Timestamp &&
        X.Equals(other.X) &&
        Y.Equals(other.Y) &&
        PointerId == other.PointerId &&
        Phase == other.Phase;

    public override bool Equals(object? obj) => obj is TouchSnapshot other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Timestamp, X, Y, PointerId, (byte)Phase);
    public static bool operator ==(TouchSnapshot left, TouchSnapshot right) => left.Equals(right);
    public static bool operator !=(TouchSnapshot left, TouchSnapshot right) => !left.Equals(right);
}

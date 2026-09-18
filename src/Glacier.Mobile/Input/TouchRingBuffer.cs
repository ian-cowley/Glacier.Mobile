namespace Glacier.Mobile.Input;

using System;
using System.Runtime.CompilerServices;
using System.Threading;

/// <summary>
/// High-frequency lock-free Single Producer Single Consumer (SPSC) ring buffer for TouchSnapshot.
/// Enables 120Hz/240Hz zero-allocation touch queuing between native OS thread and frame step loop.
/// </summary>
public sealed class TouchRingBuffer
{
    private readonly TouchSnapshot[] _buffer;
    private readonly int _mask;
    private long _head; // Producer write cursor
    private long _tail; // Consumer read cursor

    public int Capacity => _buffer.Length;

    public int Count
    {
        get
        {
            long head = Volatile.Read(ref _head);
            long tail = Volatile.Read(ref _tail);
            long diff = head - tail;
            return diff > 0 ? (int)diff : 0;
        }
    }

    public TouchRingBuffer(int capacity = 1024)
    {
        if (capacity <= 0 || (capacity & (capacity - 1)) != 0)
        {
            throw new ArgumentException("Capacity must be a positive power of two.", nameof(capacity));
        }
        _buffer = new TouchSnapshot[capacity];
        _mask = capacity - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryEnqueue(in TouchSnapshot snapshot)
    {
        long head = Volatile.Read(ref _head);
        long tail = Volatile.Read(ref _tail);

        if (head - tail >= _buffer.Length)
        {
            return false; // Buffer full
        }

        _buffer[head & _mask] = snapshot;
        Volatile.Write(ref _head, head + 1);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDequeue(out TouchSnapshot snapshot)
    {
        long tail = Volatile.Read(ref _tail);
        long head = Volatile.Read(ref _head);

        if (tail >= head)
        {
            snapshot = default;
            return false; // Buffer empty
        }

        snapshot = _buffer[tail & _mask];
        Volatile.Write(ref _tail, tail + 1);
        return true;
    }

    public int Drain(Span<TouchSnapshot> destination)
    {
        int count = 0;
        while (count < destination.Length && TryDequeue(out var snapshot))
        {
            destination[count++] = snapshot;
        }
        return count;
    }

    public void Clear()
    {
        long head = Volatile.Read(ref _head);
        Volatile.Write(ref _tail, head);
    }
}

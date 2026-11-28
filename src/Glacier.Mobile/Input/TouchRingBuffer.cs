namespace Glacier.Mobile.Input;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
/// High-frequency lock-free Single Producer Single Consumer (SPSC) ring buffer for TouchSnapshot.
/// Enables 120Hz/240Hz zero-allocation touch queuing between native OS thread and frame step loop.
/// </summary>
public sealed class TouchRingBuffer
{
    [StructLayout(LayoutKind.Explicit, Size = 128)]
    private struct PaddedCursor
    {
        [FieldOffset(64)]
        public long Value;

        [FieldOffset(72)]
        public long CachedRemote;
    }

    private readonly TouchSnapshot[] _buffer;
    private readonly int _mask;
    private PaddedCursor _head; // Producer-owned write cursor (CachedRemote caches _tail.Value)
    private PaddedCursor _tail; // Consumer-owned read cursor (CachedRemote caches _head.Value)

    public int Capacity => _buffer.Length;

    public int Count
    {
        get
        {
            long head = Volatile.Read(ref _head.Value);
            long tail = Volatile.Read(ref _tail.Value);
            long diff = head - tail;
            return diff > 0 ? (int)Math.Min(diff, _buffer.Length) : 0;
        }
    }

    public TouchRingBuffer(int capacity = 1024)
    {
        if (capacity < 2 || (capacity & (capacity - 1)) != 0)
        {
            throw new ArgumentException("Capacity must be a power of two greater than or equal to 2.", nameof(capacity));
        }
        _buffer = new TouchSnapshot[capacity];
        _mask = capacity - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryEnqueue(in TouchSnapshot snapshot)
    {
        long head = _head.Value;
        if (head - _head.CachedRemote >= _buffer.Length)
        {
            _head.CachedRemote = Volatile.Read(ref _tail.Value);
            if (head - _head.CachedRemote >= _buffer.Length)
            {
                return false; // Buffer full
            }
        }

        _buffer[head & _mask] = snapshot;
        Volatile.Write(ref _head.Value, head + 1);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDequeue(out TouchSnapshot snapshot)
    {
        long tail = _tail.Value;
        if (_tail.CachedRemote - tail <= 0)
        {
            _tail.CachedRemote = Volatile.Read(ref _head.Value);
            if (_tail.CachedRemote - tail <= 0)
            {
                snapshot = default;
                return false; // Buffer empty
            }
        }

        snapshot = _buffer[tail & _mask];
        Volatile.Write(ref _tail.Value, tail + 1);
        return true;
    }

    public int Drain(Span<TouchSnapshot> destination)
    {
        if (destination.IsEmpty) return 0;

        long tail = _tail.Value;
        long availableLong = _tail.CachedRemote - tail;
        if (destination.Length > availableLong)
        {
            _tail.CachedRemote = Volatile.Read(ref _head.Value);
            availableLong = _tail.CachedRemote - tail;
        }

        int available = availableLong > int.MaxValue ? int.MaxValue : (int)availableLong;
        int toRead = Math.Min(destination.Length, available);
        if (toRead <= 0) return 0;

        int startIndex = (int)(tail & _mask);
        int chunk1 = Math.Min(toRead, _buffer.Length - startIndex);
        int chunk2 = toRead - chunk1;

        _buffer.AsSpan(startIndex, chunk1).CopyTo(destination[..chunk1]);
        if (chunk2 > 0)
        {
            _buffer.AsSpan(0, chunk2).CopyTo(destination[chunk1..toRead]);
        }

        Volatile.Write(ref _tail.Value, tail + toRead);
        return toRead;
    }

    public void Clear()
    {
        long head = Volatile.Read(ref _head.Value);
        _tail.CachedRemote = head;
        Volatile.Write(ref _tail.Value, head);
    }
}

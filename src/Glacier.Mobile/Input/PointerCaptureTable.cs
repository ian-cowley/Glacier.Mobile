namespace Glacier.Mobile.Input;

using System;
using System.Collections.Generic;
using Glacier.Mobile.UI;

/// <summary>
/// Fast O(1) table maintaining active pointer captures for multi-touch interaction tracking.
/// Binds pointers to views during gesture sequences (Began -> Moved -> Ended/Cancelled).
/// </summary>
public sealed class PointerCaptureTable
{
    private readonly MobileView?[] _directSlots = new MobileView?[32];
    private readonly Dictionary<int, MobileView> _overflow = new();

    public void Capture(int pointerId, MobileView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        if ((uint)pointerId < (uint)_directSlots.Length)
        {
            _directSlots[pointerId] = view;
        }
        else
        {
            _overflow[pointerId] = view;
        }
    }

    public MobileView? GetCaptured(int pointerId)
    {
        if ((uint)pointerId < (uint)_directSlots.Length)
        {
            return _directSlots[pointerId];
        }
        _overflow.TryGetValue(pointerId, out var view);
        return view;
    }

    public void Release(int pointerId)
    {
        if ((uint)pointerId < (uint)_directSlots.Length)
        {
            _directSlots[pointerId] = null;
        }
        else
        {
            _overflow.Remove(pointerId);
        }
    }

    public void Clear()
    {
        Array.Clear(_directSlots);
        _overflow.Clear();
    }
}

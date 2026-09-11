namespace Glacier.Mobile.Input;

using System;
using System.Collections.Generic;

/// <summary>
/// High-throughput zero-allocation touch input router distributing OS touch events to views.
/// </summary>
public sealed class TouchDispatcher
{
    private readonly List<IGestureRecognizer> _recognizers = new();

    public void RegisterRecognizer(IGestureRecognizer recognizer)
    {
        if (!_recognizers.Contains(recognizer))
        {
            _recognizers.Add(recognizer);
        }
    }

    public void UnregisterRecognizer(IGestureRecognizer recognizer)
    {
        _recognizers.Remove(recognizer);
    }

    /// <summary>
    /// Dispatches a zero-allocation touch event across all registered gesture recognizers.
    /// </summary>
    public void Dispatch(in TouchEvent touch)
    {
        for (int i = 0; i < _recognizers.Count; i++)
        {
            _recognizers[i].ProcessTouch(in touch);
        }
    }

    public void Reset()
    {
        for (int i = 0; i < _recognizers.Count; i++)
        {
            _recognizers[i].Reset();
        }
    }
}

namespace Glacier.Mobile.Input;

using System;
using System.Diagnostics;

/// <summary>
/// Fast discrete tap and double-tap gesture recognizer.
/// </summary>
public sealed class TapGestureRecognizer : IGestureRecognizer
{
    private const float MaxTapMovementSq = 100f; // 10px tolerance
    private TouchSnapshot _startTouch;
    private bool _tracking;

    public int NumberOfTapsRequired { get; set; } = 1;
    public GestureState State { get; private set; } = GestureState.Possible;
    public event Action? Tapped;

    public void ProcessTouch(in TouchEvent touch)
    {
        switch (touch.Phase)
        {
            case TouchPhase.Began:
                _startTouch = new TouchSnapshot(in touch);
                _tracking = true;
                State = GestureState.Possible;
                break;

            case TouchPhase.Moved:
                if (_tracking)
                {
                    float dx = touch.X - _startTouch.X;
                    float dy = touch.Y - _startTouch.Y;
                    if (dx * dx + dy * dy > MaxTapMovementSq)
                    {
                        _tracking = false;
                        State = GestureState.Failed;
                    }
                }
                break;

            case TouchPhase.Ended:
                if (_tracking)
                {
                    double tapDuration = (touch.Timestamp - _startTouch.Timestamp) / (double)Stopwatch.Frequency;
                    if (tapDuration < 0.5) // Max 500ms press
                    {
                        State = GestureState.Ended;
                        Tapped?.Invoke();
                    }
                    else
                    {
                        State = GestureState.Failed;
                    }
                    _tracking = false;
                }
                break;

            case TouchPhase.Cancelled:
                Reset();
                break;
        }
    }

    public void Reset()
    {
        _tracking = false;
        State = GestureState.Possible;
    }
}

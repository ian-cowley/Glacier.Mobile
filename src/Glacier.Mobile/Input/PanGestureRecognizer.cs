namespace Glacier.Mobile.Input;

using System;
using System.Diagnostics;

/// <summary>
/// Continuous panning / dragging gesture recognizer with velocity and translation tracking.
/// </summary>
public sealed class PanGestureRecognizer : IGestureRecognizer
{
    private TouchSnapshot _startTouch;
    private TouchSnapshot _lastTouch;
    private bool _hasActivePointer;
    private int _trackedPointerId = -1;

    public float TranslationX { get; private set; }
    public float TranslationY { get; private set; }
    public float VelocityX { get; private set; }
    public float VelocityY { get; private set; }
    public GestureState State { get; private set; } = GestureState.Possible;

    public event Action<PanGestureRecognizer>? StateChanged;

    public void ProcessTouch(in TouchEvent touch)
    {
        switch (touch.Phase)
        {
            case TouchPhase.Began:
                if (!_hasActivePointer)
                {
                    _trackedPointerId = touch.PointerId;
                    _hasActivePointer = true;
                    _startTouch = new TouchSnapshot(in touch);
                    _lastTouch = _startTouch;
                    TranslationX = 0f;
                    TranslationY = 0f;
                    VelocityX = 0f;
                    VelocityY = 0f;
                    State = GestureState.Began;
                    StateChanged?.Invoke(this);
                }
                break;

            case TouchPhase.Moved:
                if (_hasActivePointer && touch.PointerId == _trackedPointerId)
                {
                    TranslationX = touch.X - _startTouch.X;
                    TranslationY = touch.Y - _startTouch.Y;

                    double dtSeconds = (touch.Timestamp - _lastTouch.Timestamp) / (double)Stopwatch.Frequency;
                    if (dtSeconds > 0.0001)
                    {
                        VelocityX = (float)((touch.X - _lastTouch.X) / dtSeconds);
                        VelocityY = (float)((touch.Y - _lastTouch.Y) / dtSeconds);
                    }

                    _lastTouch = new TouchSnapshot(in touch);
                    State = GestureState.Changed;
                    StateChanged?.Invoke(this);
                }
                break;

            case TouchPhase.Ended:
                if (_hasActivePointer && touch.PointerId == _trackedPointerId)
                {
                    TranslationX = touch.X - _startTouch.X;
                    TranslationY = touch.Y - _startTouch.Y;
                    _hasActivePointer = false;
                    _trackedPointerId = -1;
                    State = GestureState.Ended;
                    StateChanged?.Invoke(this);
                }
                break;

            case TouchPhase.Cancelled:
                if (_hasActivePointer && touch.PointerId == _trackedPointerId)
                {
                    _hasActivePointer = false;
                    _trackedPointerId = -1;
                    State = GestureState.Cancelled;
                    StateChanged?.Invoke(this);
                }
                break;
        }
    }

    public void Reset()
    {
        _hasActivePointer = false;
        _trackedPointerId = -1;
        State = GestureState.Possible;
    }
}

namespace Glacier.Mobile.Input;

using System;

/// <summary>
/// Multi-touch pinch-to-zoom gesture recognizer tracking scale factor and pinch center.
/// </summary>
public sealed class PinchGestureRecognizer : IGestureRecognizer
{
    private TouchSnapshot _touch0;
    private TouchSnapshot _touch1;
    private bool _hasTouch0;
    private bool _hasTouch1;
    private float _initialDistance;

    public float Scale { get; private set; } = 1.0f;
    public float FocusX { get; private set; }
    public float FocusY { get; private set; }
    public GestureState State { get; private set; } = GestureState.Possible;

    public event Action<PinchGestureRecognizer>? StateChanged;

    public void ProcessTouch(in TouchEvent touch)
    {
        switch (touch.Phase)
        {
            case TouchPhase.Began:
                if (!_hasTouch0)
                {
                    _touch0 = new TouchSnapshot(in touch);
                    _hasTouch0 = true;
                }
                else if (!_hasTouch1 && touch.PointerId != _touch0.PointerId)
                {
                    _touch1 = new TouchSnapshot(in touch);
                    _hasTouch1 = true;

                    float dx = _touch1.X - _touch0.X;
                    float dy = _touch1.Y - _touch0.Y;
                    _initialDistance = MathF.Max(1.0f, MathF.Sqrt(dx * dx + dy * dy));
                    Scale = 1.0f;
                    FocusX = (_touch0.X + _touch1.X) * 0.5f;
                    FocusY = (_touch0.Y + _touch1.Y) * 0.5f;

                    State = GestureState.Began;
                    StateChanged?.Invoke(this);
                }
                break;

            case TouchPhase.Moved:
                if (touch.PointerId == _touch0.PointerId) _touch0 = new TouchSnapshot(in touch);
                else if (touch.PointerId == _touch1.PointerId) _touch1 = new TouchSnapshot(in touch);

                if (_hasTouch0 && _hasTouch1 && _initialDistance > 0f)
                {
                    float dx = _touch1.X - _touch0.X;
                    float dy = _touch1.Y - _touch0.Y;
                    float currentDist = MathF.Sqrt(dx * dx + dy * dy);

                    Scale = currentDist / _initialDistance;
                    FocusX = (_touch0.X + _touch1.X) * 0.5f;
                    FocusY = (_touch0.Y + _touch1.Y) * 0.5f;

                    State = GestureState.Changed;
                    StateChanged?.Invoke(this);
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Cancelled:
                if (touch.PointerId == _touch0.PointerId || touch.PointerId == _touch1.PointerId)
                {
                    if (State == GestureState.Began || State == GestureState.Changed)
                    {
                        State = touch.Phase == TouchPhase.Ended ? GestureState.Ended : GestureState.Cancelled;
                        StateChanged?.Invoke(this);
                    }
                    _hasTouch0 = false;
                    _hasTouch1 = false;
                    _initialDistance = 0f;
                }
                break;
        }
    }

    public void Reset()
    {
        _hasTouch0 = false;
        _hasTouch1 = false;
        _initialDistance = 0f;
        Scale = 1.0f;
        State = GestureState.Possible;
    }
}

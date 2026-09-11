namespace Glacier.Mobile.Input;

/// <summary>
/// Lifecycle state of a gesture recognizer.
/// </summary>
public enum GestureState : byte
{
    Possible,
    Began,
    Changed,
    Ended,
    Cancelled,
    Failed
}

/// <summary>
/// Contract for touch gesture recognizers with zero-heap event processing.
/// </summary>
public interface IGestureRecognizer
{
    GestureState State { get; }
    void ProcessTouch(in TouchEvent touch);
    void Reset();
}

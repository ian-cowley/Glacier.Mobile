namespace Glacier.Mobile.Input;

/// <summary>
/// Lifecycle phase of an active touch pointer contact.
/// </summary>
public enum TouchPhase : byte
{
    Began,
    Moved,
    Stationary,
    Ended,
    Cancelled
}

namespace FreeSeat.Core.Sessions.Models;

/// <summary>
/// Represents the type of session change event.
/// </summary>
public enum SessionChangeReason
{
    /// <summary>
    /// Unknown change reason.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A session was created.
    /// </summary>
    SessionLogon = 1,

    /// <summary>
    /// A session was logged off.
    /// </summary>
    SessionLogoff = 2,

    /// <summary>
    /// A session was locked.
    /// </summary>
    SessionLock = 3,

    /// <summary>
    /// A session was unlocked.
    /// </summary>
    SessionUnlock = 4,

    /// <summary>
    /// A session was connected (remote).
    /// </summary>
    SessionRemoteControl = 5,

    /// <summary>
    /// A session was disconnected (remote).
    /// </summary>
    SessionRemoteDisconnect = 6,

    /// <summary>
    /// Session state changed.
    /// </summary>
    SessionStateChange = 7,
}

/// <summary>
/// Event arguments for session change notifications.
/// </summary>
/// <param name="Reason">The reason for the session change.</param>
/// <param name="SessionId">The session ID that changed.</param>
/// <param name="Timestamp">The time the change occurred.</param>
/// <param name="SessionInfo">Optional session information if available.</param>
public record SessionChangedEventArgs(
    SessionChangeReason Reason,
    int SessionId,
    DateTime Timestamp,
    SessionInfo? SessionInfo = null
);
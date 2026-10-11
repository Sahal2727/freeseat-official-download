using System.Collections.Generic;

using System.ComponentModel;

namespace FreeSeat.Core.Sessions.Models;

/// <summary>
/// Represents the type of session event for the event system.
/// </summary>
public enum SessionEventType
{
    /// <summary> A new session was detected. </summary>
    [Description("Session Detected")]
    SessionDetected = 1,

    /// <summary> A session was connected (remote or console). </summary>
    [Description("Session Connected")]
    SessionConnected = 2,

    /// <summary> A session was disconnected. </summary>
    [Description("Session Disconnected")]
    SessionDisconnected = 3,

    /// <summary> A session was locked. </summary>
    [Description("Session Locked")]
    SessionLocked = 4,

    /// <summary> A session was unlocked. </summary>
    [Description("Session Unlocked")]
    SessionUnlocked = 5,

    /// <summary> A session was logged off. </summary>
    [Description("Session Logged Off")]
    SessionLoggedOff = 6,

    /// <summary> A session was removed/no longer tracked. </summary>
    [Description("Session Removed")]
    SessionRemoved = 7,

    /// <summary> A session's state changed. </summary>
    [Description("State Changed")]
    SessionStateChanged = 8,

    /// <summary> A process was launched in a session. </summary>
    [Description("Process Launched")]
    ProcessLaunched = 9,

    /// <summary> A process launch failed. </summary>
    [Description("Launch Failed")]
    ProcessLaunchFailed = 10,
}

/// <summary>
/// Event arguments for session lifecycle events.
/// </summary>
public record SessionEventArgs(
    SessionEventType EventType,
    int SessionId,
    SessionLifecycleState? PreviousState = null,
    SessionLifecycleState? NewState = null,
    SessionInfo? SessionInfo = null,
    DateTime? Timestamp = null,
    string? Details = null
)
{
    public string TimeString => (Timestamp ?? DateTime.UtcNow).ToLocalTime().ToString("HH:mm:ss");
    public string DisplayString => $"{TimeString} - Session {SessionId} - {Details}";

    public static SessionEventArgs SessionDetected(int sessionId, SessionInfo? info = null)
        => new(SessionEventType.SessionDetected, sessionId, NewState: info?.State.ToLifecycleState() ?? SessionLifecycleState.Detected, SessionInfo: info, Timestamp: DateTime.UtcNow, Details: info != null ? $"Session detected: {info.DisplayName}" : $"Session detected: {sessionId}");

    public static SessionEventArgs SessionConnected(int sessionId, SessionLifecycleState newState, SessionInfo? info = null)
        => new(SessionEventType.SessionConnected, sessionId, NewState: newState, SessionInfo: info, Timestamp: DateTime.UtcNow, Details: $"Session connected: {newState.GetDescription()}");

    public static SessionEventArgs SessionDisconnected(int sessionId, SessionLifecycleState previousState, SessionInfo? info = null)
        => new(SessionEventType.SessionDisconnected, sessionId, PreviousState: previousState, NewState: SessionLifecycleState.Disconnected, SessionInfo: info, Timestamp: DateTime.UtcNow, Details: $"Session disconnected from {previousState.GetDescription()}");

    public static SessionEventArgs SessionLocked(int sessionId, SessionLifecycleState previousState, SessionInfo? info = null)
        => new(SessionEventType.SessionLocked, sessionId, PreviousState: previousState, NewState: SessionLifecycleState.Locked, SessionInfo: info, Timestamp: DateTime.UtcNow, Details: "Session locked");

    public static SessionEventArgs SessionUnlocked(int sessionId, SessionLifecycleState previousState, SessionInfo? info = null)
        => new(SessionEventType.SessionUnlocked, sessionId, PreviousState: previousState, NewState: SessionLifecycleState.Unlocked, SessionInfo: info, Timestamp: DateTime.UtcNow, Details: "Session unlocked");

    public static SessionEventArgs SessionLoggedOff(int sessionId, SessionLifecycleState previousState, SessionInfo? info = null)
        => new(SessionEventType.SessionLoggedOff, sessionId, PreviousState: previousState, NewState: SessionLifecycleState.LoggedOff, SessionInfo: info, Timestamp: DateTime.UtcNow, Details: "Session logged off");

    public static SessionEventArgs SessionRemoved(int sessionId, SessionInfo? info = null)
        => new(SessionEventType.SessionRemoved, sessionId, SessionInfo: info, Timestamp: DateTime.UtcNow, Details: "Session removed from tracking");

    public static SessionEventArgs SessionStateChanged(int sessionId, SessionLifecycleState previousState, SessionLifecycleState newState, SessionInfo? info = null)
        => new(SessionEventType.SessionStateChanged, sessionId, PreviousState: previousState, NewState: newState, SessionInfo: info, Timestamp: DateTime.UtcNow, Details: $"State changed: {previousState.GetDescription()} -> {newState.GetDescription()}");

    public static SessionEventArgs ProcessLaunched(int sessionId, int processId, string executable)
        => new(SessionEventType.ProcessLaunched, sessionId, Timestamp: DateTime.UtcNow, Details: $"Launched {executable} (PID: {processId})");

    public static SessionEventArgs ProcessLaunchFailed(int sessionId, string executable, string error)
        => new(SessionEventType.ProcessLaunchFailed, sessionId, Timestamp: DateTime.UtcNow, Details: $"Failed to launch {executable}: {error}");
}
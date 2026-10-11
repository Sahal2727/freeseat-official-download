using System.ComponentModel;

namespace FreeSeat.Core.Sessions.Models;

/// <summary>
/// Represents the lifecycle state of a FreeSeat-managed session.
/// This maps Windows session states to FreeSeat's managed lifecycle.
/// </summary>
public enum SessionLifecycleState
{
    /// <summary>
    /// The session state is unknown or not yet determined.
    /// </summary>
    [Description("Unknown")]
    Unknown = 0,

    /// <summary>
    /// Session has been detected but not yet fully characterized.
    /// </summary>
    [Description("Detected")]
    Detected = 1,

    /// <summary>
    /// Session is active and available for management.
    /// User is logged on and session is connected.
    /// </summary>
    [Description("Active")]
    Active = 2,

    /// <summary>
    /// Session is locked (workstation locked).
    /// </summary>
    [Description("Locked")]
    Locked = 3,

    /// <summary>
    /// Session was locked and is now unlocked.
    /// </summary>
    [Description("Unlocked")]
    Unlocked = 4,

    /// <summary>
    /// Session is disconnected (user logged on but not connected to client).
    /// </summary>
    [Description("Disconnected")]
    Disconnected = 5,

    /// <summary>
    /// Session is in the process of logging on.
    /// </summary>
    [Description("Logging On")]
    LoggingOn = 6,

    /// <summary>
    /// Session is in the process of logging off.
    /// </summary>
    [Description("Logging Off")]
    LoggingOff = 7,

    /// <summary>
    /// Session has been logged off and is no longer available.
    /// </summary>
    [Description("Logged Off")]
    LoggedOff = 8,

    /// <summary>
    /// Session is unavailable (e.g., access denied, session terminated).
    /// </summary>
    [Description("Unavailable")]
    Unavailable = 9,
}

/// <summary>
/// Extension methods for SessionLifecycleState.
/// </summary>
public static class SessionLifecycleStateExtensions
{
    /// <summary>
    /// Maps a Windows SessionState to FreeSeat's SessionLifecycleState.
    /// </summary>
    /// <param name="state">The Windows session state.</param>
    /// <returns>The corresponding lifecycle state.</returns>
    public static SessionLifecycleState ToLifecycleState(this SessionState state)
    {
        return state switch
        {
            SessionState.Active => SessionLifecycleState.Active,
            SessionState.Connected => SessionLifecycleState.Detected,
            SessionState.Disconnected => SessionLifecycleState.Disconnected,
            SessionState.Idle => SessionLifecycleState.Active,
            SessionState.Logon => SessionLifecycleState.LoggingOn,
            SessionState.Logoff => SessionLifecycleState.LoggingOff,
            SessionState.Locked => SessionLifecycleState.Locked,
            SessionState.Unlocked => SessionLifecycleState.Unlocked,
            SessionState.RemoteControl => SessionLifecycleState.Active,
            SessionState.Shadow => SessionLifecycleState.Active,
            _ => SessionLifecycleState.Unknown,
        };
    }

    /// <summary>
    /// Maps a SessionChangeReason to the expected lifecycle transition.
    /// </summary>
    /// <param name="reason">The session change reason.</param>
    /// <returns>The expected new lifecycle state, or null if no transition.</returns>
    public static SessionLifecycleState? GetExpectedLifecycleState(this SessionChangeReason reason)
    {
        return reason switch
        {
            SessionChangeReason.SessionLogon => SessionLifecycleState.Active,
            SessionChangeReason.SessionLogoff => SessionLifecycleState.LoggedOff,
            SessionChangeReason.SessionLock => SessionLifecycleState.Locked,
            SessionChangeReason.SessionUnlock => SessionLifecycleState.Unlocked,
            SessionChangeReason.SessionRemoteControl => SessionLifecycleState.Active,
            SessionChangeReason.SessionRemoteDisconnect => SessionLifecycleState.Disconnected,
            SessionChangeReason.SessionStateChange => null, // Requires querying actual state
            _ => null,
        };
    }

    /// <summary>
    /// Gets a human-readable description of the lifecycle state.
    /// </summary>
    public static string GetDescription(this SessionLifecycleState state)
    {
        return state switch
        {
            SessionLifecycleState.Unknown => "Unknown",
            SessionLifecycleState.Detected => "Detected",
            SessionLifecycleState.Active => "Active",
            SessionLifecycleState.Locked => "Locked",
            SessionLifecycleState.Unlocked => "Unlocked",
            SessionLifecycleState.Disconnected => "Disconnected",
            SessionLifecycleState.LoggingOn => "Logging On",
            SessionLifecycleState.LoggingOff => "Logging Off",
            SessionLifecycleState.LoggedOff => "Logged Off",
            SessionLifecycleState.Unavailable => "Unavailable",
            _ => "Unknown",
        };
    }

    /// <summary>
    /// Determines if the session is in a state where process launching is possible.
    /// </summary>
    public static bool CanLaunchProcess(this SessionLifecycleState state)
    {
        return state is SessionLifecycleState.Active or SessionLifecycleState.Unlocked or SessionLifecycleState.Detected;
    }
}
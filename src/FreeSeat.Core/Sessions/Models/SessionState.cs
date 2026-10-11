using System.ComponentModel;

namespace FreeSeat.Core.Sessions.Models;

/// <summary>
/// Represents the state of a Windows session.
/// </summary>
public enum SessionState
{
    /// <summary>
    /// The session state is unknown.
    /// </summary>
    [Description("Unknown")]
    Unknown = 0,

    /// <summary>
    /// The session is active (user is logged on and the session is connected).
    /// </summary>
    [Description("Active")]
    Active = 1,

    /// <summary>
    /// The session is connected but not active (e.g., locked workstation).
    /// </summary>
    [Description("Connected")]
    Connected = 2,

    /// <summary>
    /// The session is disconnected (user is logged on but session is not connected to a client).
    /// </summary>
    [Description("Disconnected")]
    Disconnected = 3,

    /// <summary>
    /// The session is idle.
    /// </summary>
    [Description("Idle")]
    Idle = 4,

    /// <summary>
    /// The session is in the logon process.
    /// </summary>
    [Description("Logging On")]
    Logon = 5,

    /// <summary>
    /// The session is in the logoff process.
    /// </summary>
    [Description("Logging Off")]
    Logoff = 6,

    /// <summary>
    /// The session is locked.
    /// </summary>
    [Description("Locked")]
    Locked = 7,

    /// <summary>
    /// The session is unlocked.
    /// </summary>
    [Description("Unlocked")]
    Unlocked = 8,

    /// <summary>
    /// The session is in a remote control state.
    /// </summary>
    [Description("Remote Control")]
    RemoteControl = 9,

    /// <summary>
    /// The session is in a shadow state.
    /// </summary>
    [Description("Shadow")]
    Shadow = 10,
}
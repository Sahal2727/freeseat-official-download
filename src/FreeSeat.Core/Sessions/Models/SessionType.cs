using System.ComponentModel;

namespace FreeSeat.Core.Sessions.Models;

/// <summary>
/// Represents the type of a Windows session.
/// </summary>
public enum SessionType
{
    /// <summary>
    /// The session type is unknown.
    /// </summary>
    [Description("Unknown")]
    Unknown = 0,

    /// <summary>
    /// Console session (local physical session).
    /// </summary>
    [Description("Console")]
    Console = 1,

    /// <summary>
    /// Remote Desktop Services session.
    /// </summary>
    [Description("Remote")]
    Remote = 2,

    /// <summary>
    /// Session 0 (services, non-interactive).
    /// </summary>
    [Description("Services (Session 0)")]
    Services = 3,

    /// <summary>
    /// Fast user switching session.
    /// </summary>
    [Description("Fast User Switching")]
    FastUserSwitching = 4,
}
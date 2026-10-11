namespace FreeSeat.Core.Sessions.Models;

/// <summary>
/// Represents information about a Windows session.
/// </summary>
/// <param name="SessionId">The unique session identifier.</param>
/// <param name="UserName">The user name associated with the session.</param>
/// <param name="DomainName">The domain name of the user.</param>
/// <param name="State">The current state of the session.</param>
/// <param name="Type">The type of the session.</param>
/// <param name="ClientName">The name of the client device (if remote).</param>
/// <param name="ConnectTime">The time the session was connected.</param>
/// <param name="DisconnectTime">The time the session was disconnected.</param>
/// <param name="LastInputTime">The time of the last user input.</param>
/// <param name="IdleTime">The idle time of the session.</param>
/// <param name="SessionFlags">Additional session flags.</param>
/// <param name="ProcessId">The process ID of the session leader (winlogon).</param>
public record SessionInfo(
    int SessionId,
    string? UserName,
    string? DomainName,
    SessionState State,
    SessionType Type,
    string? ClientName = null,
    DateTime? ConnectTime = null,
    DateTime? DisconnectTime = null,
    DateTime? LastInputTime = null,
    TimeSpan? IdleTime = null,
    uint SessionFlags = 0,
    int ProcessId = 0
)
{
    /// <summary>
    /// Gets the fully qualified user name (domain\user or user@domain).
    /// </summary>
    public string? FullyQualifiedUserName =>
        !string.IsNullOrEmpty(DomainName) && !string.IsNullOrEmpty(UserName)
            ? $"{DomainName}\\{UserName}"
            : UserName;

    /// <summary>
    /// Gets a value indicating whether the session is currently active.
    /// </summary>
    public bool IsActive => State == SessionState.Active;

    /// <summary>
    /// Gets a value indicating whether the session is a console session.
    /// </summary>
    public bool IsConsole => Type == SessionType.Console;

    /// <summary>
    /// Gets a value indicating whether the session is a remote session.
    /// </summary>
    public bool IsRemote => Type == SessionType.Remote;

    /// <summary>
    /// Gets a display name for the session.
    /// </summary>
    public string DisplayName =>
        !string.IsNullOrEmpty(FullyQualifiedUserName)
            ? $"{FullyQualifiedUserName} (Session {SessionId})"
            : $"Session {SessionId}";

    /// <summary>
    /// Gets a description of the session state for display.
    /// </summary>
    public string StateDescription => State switch
    {
        SessionState.Active => "Active",
        SessionState.Connected => "Connected",
        SessionState.Disconnected => "Disconnected",
        SessionState.Idle => "Idle",
        SessionState.Logon => "Logging On",
        SessionState.Logoff => "Logging Off",
        SessionState.Locked => "Locked",
        SessionState.Unlocked => "Unlocked",
        SessionState.RemoteControl => "Remote Control",
        SessionState.Shadow => "Shadow",
        _ => "Unknown",
    };

    /// <summary>
    /// Gets a description of the session type for display.
    /// </summary>
    public string TypeDescription => Type switch
    {
        SessionType.Console => "Console",
        SessionType.Remote => "Remote",
        SessionType.Services => "Services (Session 0)",
        SessionType.FastUserSwitching => "Fast User Switching",
        _ => "Unknown",
    };
}
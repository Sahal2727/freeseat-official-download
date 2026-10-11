namespace FreeSeat.Core.Sessions.Models;

/// <summary>
/// Represents information about a process and its associated session.
/// </summary>
/// <param name="ProcessId">The process ID.</param>
/// <param name="ProcessName">The process name.</param>
/// <param name="SessionId">The session ID the process belongs to.</param>
/// <param name="UserName">The user name running the process.</param>
/// <param name="DomainName">The domain name of the user.</param>
/// <param name="StartTime">The process start time.</param>
/// <param name="MemoryUsage">The memory usage in bytes.</param>
public record ProcessSessionInfo(
    int ProcessId,
    string ProcessName,
    int SessionId,
    string? UserName = null,
    string? DomainName = null,
    DateTime? StartTime = null,
    long MemoryUsage = 0
)
{
    /// <summary>
    /// Gets the fully qualified user name.
    /// </summary>
    public string? FullyQualifiedUserName =>
        !string.IsNullOrEmpty(DomainName) && !string.IsNullOrEmpty(UserName)
            ? $"{DomainName}\\{UserName}"
            : UserName;

    /// <summary>
    /// Gets a display name for the process.
    /// </summary>
    public string DisplayName => $"{ProcessName} (PID: {ProcessId}, Session: {SessionId})";
}
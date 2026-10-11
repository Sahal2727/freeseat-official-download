namespace FreeSeat.Core.Sessions.Models;

/// <summary>
/// Represents the identity of a session user for authorization purposes.
/// Contains only non-sensitive identifying information.
/// </summary>
/// <param name="SessionId">The Windows session ID.</param>
/// <param name="UserName">The user name.</param>
/// <param name="DomainName">The domain name (optional).</param>
/// <param name="SessionType">The type of session.</param>
public record SessionIdentity(
    int SessionId,
    string? UserName,
    string? DomainName,
    SessionType SessionType
)
{
    /// <summary>
    /// Gets the fully qualified user name (domain\user or user).
    /// </summary>
    public string? FullyQualifiedUserName =>
        !string.IsNullOrEmpty(DomainName) && !string.IsNullOrEmpty(UserName)
            ? $"{DomainName}\\{UserName}"
            : UserName;

    /// <summary>
    /// Gets a display name for the session identity.
    /// </summary>
    public string DisplayName =>
        !string.IsNullOrEmpty(FullyQualifiedUserName)
            ? $"{FullyQualifiedUserName} (Session {SessionId})"
            : $"Session {SessionId}";

    /// <summary>
    /// Determines if this identity matches another for authorization purposes.
    /// </summary>
    public bool Matches(SessionIdentity? other)
    {
        if (other == null) return false;
        return SessionId == other.SessionId &&
               string.Equals(UserName, other.UserName, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(DomainName, other.DomainName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a SessionIdentity from a SessionInfo.
    /// </summary>
    public static SessionIdentity FromSessionInfo(SessionInfo info)
    {
        return new SessionIdentity(
            info.SessionId,
            info.UserName,
            info.DomainName,
            info.Type
        );
    }
}
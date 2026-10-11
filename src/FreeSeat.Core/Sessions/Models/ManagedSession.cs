using System.Collections.Generic;

namespace FreeSeat.Core.Sessions.Models;

/// <summary>
/// Represents a FreeSeat-managed session with lifecycle tracking and metadata.
/// This is the primary model used by the Session Manager for tracking sessions.
/// </summary>
/// <param name="SessionId">The Windows session ID.</param>
/// <param name="Identity">The session identity (user, domain, type).</param>
/// <param name="State">The current lifecycle state.</param>
/// <param name="ClientName">The client device name (for remote sessions).</param>
/// <param name="ConnectedAt">When the session was first connected.</param>
/// <param name="LastSeen">The last time the session was observed.</param>
/// <param name="IsManaged">Whether FreeSeat is actively managing this session.</param>
/// <param name="IsCurrent">Whether this is the current process session.</param>
/// <param name="DisconnectedAt">When the session was disconnected (if applicable).</param>
/// <param name="LockedAt">When the session was last locked (if applicable).</param>
/// <param name="UnlockedAt">When the session was last unlocked (if applicable).</param>
public record ManagedSession(
    int SessionId,
    SessionIdentity Identity,
    SessionLifecycleState State,
    string? ClientName = null,
    DateTime? ConnectedAt = null,
    DateTime? LastSeen = null,
    bool IsManaged = false,
    bool IsCurrent = false,
    DateTime? DisconnectedAt = null,
    DateTime? LockedAt = null,
    DateTime? UnlockedAt = null
)
{
    /// <summary>
    /// Gets the fully qualified user name.
    /// </summary>
    public string? FullyQualifiedUserName => Identity.FullyQualifiedUserName;

    /// <summary>
    /// Gets the session type.
    /// </summary>
    public SessionType SessionType => Identity.SessionType;

    /// <summary>
    /// Gets a display name for the session.
    /// </summary>
    public string DisplayName => Identity.DisplayName;

    /// <summary>
    /// Gets a value indicating whether the session is in an active, usable state.
    /// </summary>
    public bool IsActive => State is SessionLifecycleState.Active or SessionLifecycleState.Unlocked or SessionLifecycleState.Detected;

    /// <summary>
    /// Gets a value indicating whether process launching is possible in this session.
    /// </summary>
    public bool CanLaunchProcess => State.CanLaunchProcess();

    /// <summary>
    /// Gets the time since the session was last seen.
    /// </summary>
    public TimeSpan? TimeSinceLastSeen => LastSeen.HasValue ? DateTime.UtcNow - LastSeen.Value : null;

    /// <summary>
    /// Creates a ManagedSession from a SessionInfo (initial detection).
    /// </summary>
    public static ManagedSession FromSessionInfo(SessionInfo info, bool isCurrent = false)
    {
        var identity = SessionIdentity.FromSessionInfo(info);
        var lifecycleState = info.State.ToLifecycleState();
        var now = DateTime.UtcNow;

        return new ManagedSession(
            SessionId: info.SessionId,
            Identity: identity,
            State: lifecycleState,
            ClientName: info.ClientName,
            ConnectedAt: info.ConnectTime?.ToUniversalTime() ?? now,
            LastSeen: now,
            IsManaged: false,
            IsCurrent: isCurrent
        );
    }

    /// <summary>
    /// Creates a new ManagedSession with updated state and timestamp.
    /// </summary>
    public ManagedSession WithState(SessionLifecycleState newState, DateTime? timestamp = null)
    {
        var now = timestamp ?? DateTime.UtcNow;
        return this with
        {
            State = newState,
            LastSeen = now,
            DisconnectedAt = newState == SessionLifecycleState.Disconnected ? now : DisconnectedAt,
            LockedAt = newState == SessionLifecycleState.Locked ? now : LockedAt,
            UnlockedAt = newState == SessionLifecycleState.Unlocked ? now : UnlockedAt,
        };
    }

    /// <summary>
    /// Creates a new ManagedSession with updated LastSeen timestamp.
    /// </summary>
    public ManagedSession WithUpdatedTimestamp(DateTime? timestamp = null)
    {
        return this with { LastSeen = timestamp ?? DateTime.UtcNow };
    }

    /// <summary>
    /// Creates a new ManagedSession marking it as managed/unmanaged.
    /// </summary>
    public ManagedSession WithManaged(bool managed)
    {
        return this with { IsManaged = managed };
    }
}
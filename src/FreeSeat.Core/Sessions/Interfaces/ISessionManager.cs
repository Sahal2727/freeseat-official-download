using FreeSeat.Core.Sessions.Models;

namespace FreeSeat.Core.Sessions.Interfaces;

/// <summary>
/// Manages Windows sessions for FreeSeat.
/// Provides session discovery, tracking, lifecycle management, and safe process launching.
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Gets the current session ID of the calling process.
    /// </summary>
    int CurrentSessionId { get; }

    /// <summary>
    /// Gets the current managed session information.
    /// </summary>
    ManagedSession? CurrentSession { get; }

    /// <summary>
    /// Gets all currently tracked sessions.
    /// </summary>
    IReadOnlyList<ManagedSession> Sessions { get; }

    /// <summary>
    /// Gets a value indicating whether session monitoring is active.
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Gets recent session events.
    /// </summary>
    IReadOnlyList<SessionEventArgs> RecentEvents { get; }

    /// <summary>
    /// Gets a session by ID.
    /// </summary>
    ManagedSession? GetSession(int sessionId);

    /// <summary>
    /// Gets the lifecycle state of a specific session.
    /// </summary>
    SessionLifecycleState GetSessionState(int sessionId);

    /// <summary>
    /// Starts monitoring for session changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops monitoring for session changes.
    /// </summary>
    Task StopMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces a reconciliation of tracked sessions with actual Windows sessions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReconcileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a process can be launched in the specified session.
    /// </summary>
    /// <param name="sessionId">The target session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if launch is possible, false otherwise.</returns>
    Task<bool> CanLaunchProcessAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Launches a process in the specified session.
    /// Only works for authorized sessions where Windows permits the operation.
    /// </summary>
    /// <param name="request">The launch request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The launch result.</returns>
    Task<SessionLaunchResult> LaunchProcessAsync(SessionLaunchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a session lifecycle event occurs.
    /// </summary>
    event EventHandler<SessionEventArgs> SessionEvent;

    /// <summary>
    /// Event raised when the session collection changes.
    /// </summary>
    event EventHandler<SessionCollectionChangedEventArgs> SessionsChanged;
}

/// <summary>
/// Event arguments for session collection changes.
/// </summary>
/// <param name="Added">Sessions that were added.</param>
/// <param name="Removed">Sessions that were removed.</param>
/// <param name="Changed">Sessions that changed.</param>
public record SessionCollectionChangedEventArgs(
    IReadOnlyList<ManagedSession> Added,
    IReadOnlyList<ManagedSession> Removed,
    IReadOnlyList<ManagedSession> Changed
);
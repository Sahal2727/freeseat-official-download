using FreeSeat.Core.Sessions.Models;

namespace FreeSeat.Core.Sessions.Interfaces;

/// <summary>
/// Provides read-only access to Windows session information.
/// This interface is designed for diagnostic and research purposes only.
/// No session modifications are performed.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Gets the current session ID of the calling process.
    /// </summary>
    int CurrentSessionId { get; }

    /// <summary>
    /// Gets the current session information.
    /// </summary>
    SessionInfo? CurrentSession { get; }

    /// <summary>
    /// Enumerates all sessions on the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of session information.</returns>
    Task<IReadOnlyList<SessionInfo>> EnumerateSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets session information for a specific session ID.
    /// </summary>
    /// <param name="sessionId">The session ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session information, or null if not found.</returns>
    Task<SessionInfo?> GetSessionAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active console session ID.
    /// </summary>
    /// <returns>The active console session ID, or -1 if not available.</returns>
    int GetActiveConsoleSessionId();

    /// <summary>
    /// Enumerates processes and their associated sessions.
    /// This is a read-only diagnostic operation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of process session information.</returns>
    Task<IReadOnlyList<ProcessSessionInfo>> EnumerateProcessSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts monitoring for session changes.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Stops monitoring for session changes.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Gets a value indicating whether session monitoring is active.
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Event raised when a session changes.
    /// </summary>
    event EventHandler<SessionChangedEventArgs>? SessionChanged;
}
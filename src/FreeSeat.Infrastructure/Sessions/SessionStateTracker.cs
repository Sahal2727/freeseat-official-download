using FreeSeat.Core.Sessions.Interfaces;
using FreeSeat.Core.Sessions.Models;
using Microsoft.Extensions.Logging;

namespace FreeSeat.Infrastructure.Sessions;

/// <summary>
/// Tracks the state of Windows sessions and maintains a history of session events.
/// </summary>
internal sealed class SessionStateTracker : IDisposable
{
    private readonly ILogger<SessionStateTracker> _logger;
    private readonly object _lock = new();
    private readonly Dictionary<int, ManagedSession> _sessions = new();
    private readonly List<SessionEventArgs> _eventHistory = new();
    private readonly int _maxEventHistory;
    private bool _disposed;

    public SessionStateTracker(ILogger<SessionStateTracker> logger, int maxEventHistory = 100)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxEventHistory = maxEventHistory;
    }

    /// <summary>
    /// Gets all tracked sessions.
    /// </summary>
    public IReadOnlyList<ManagedSession> Sessions
    {
        get
        {
            lock (_lock)
            {
                return _sessions.Values.OrderBy(s => s.SessionId).ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Gets recent session events.
    /// </summary>
    public IReadOnlyList<SessionEventArgs> RecentEvents
    {
        get
        {
            lock (_lock)
            {
                return _eventHistory.OrderByDescending(e => e.Timestamp ?? DateTime.MinValue).ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Gets a session by ID.
    /// </summary>
    public ManagedSession? GetSession(int sessionId)
    {
        lock (_lock)
        {
            return _sessions.TryGetValue(sessionId, out var session) ? session : null;
        }
    }

    /// <summary>
    /// Gets the lifecycle state of a session.
    /// </summary>
    public SessionLifecycleState GetSessionState(int sessionId)
    {
        lock (_lock)
        {
            return _sessions.TryGetValue(sessionId, out var session) ? session.State : SessionLifecycleState.Unknown;
        }
    }

    /// <summary>
    /// Updates or adds a session from SessionInfo.
    /// </summary>
    public SessionCollectionChangedEventArgs UpdateSession(SessionInfo info, bool isCurrent = false)
    {
        lock (_lock)
        {
            var sessionId = info.SessionId;
            var previousSession = _sessions.GetValueOrDefault(sessionId);
            var newSession = ManagedSession.FromSessionInfo(info, isCurrent);

            var added = new List<ManagedSession>();
            var removed = new List<ManagedSession>();
            var changed = new List<ManagedSession>();
            var events = new List<SessionEventArgs>();

            if (previousSession == null)
            {
                // New session detected
                _sessions[sessionId] = newSession;
                added.Add(newSession);
                events.Add(SessionEventArgs.SessionDetected(sessionId, info));
                _logger.LogInformation("Session detected: {SessionId} - {DisplayName}", sessionId, newSession.DisplayName);
            }
            else
            {
                // Session exists - check for state changes
                var previousState = previousSession.State;
                var newState = newSession.State;

                if (previousState != newState)
                {
                    var updatedSession = previousSession.WithState(newState).WithUpdatedTimestamp();
                    if (newState == SessionLifecycleState.Locked)
                    {
                        updatedSession = updatedSession with { LockedAt = DateTime.UtcNow };
                    }
                    else if (newState == SessionLifecycleState.Unlocked)
                    {
                        updatedSession = updatedSession with { UnlockedAt = DateTime.UtcNow };
                    }
                    else if (newState == SessionLifecycleState.Disconnected)
                    {
                        updatedSession = updatedSession with { DisconnectedAt = DateTime.UtcNow };
                    }

                    _sessions[sessionId] = updatedSession;
                    changed.Add(updatedSession);

                    // Generate appropriate event
                    SessionEventArgs? stateEvent = newState switch
                    {
                        SessionLifecycleState.Locked => SessionEventArgs.SessionLocked(sessionId, previousState, info),
                        SessionLifecycleState.Unlocked => SessionEventArgs.SessionUnlocked(sessionId, previousState, info),
                        SessionLifecycleState.Disconnected => SessionEventArgs.SessionDisconnected(sessionId, previousState, info),
                        SessionLifecycleState.LoggedOff => SessionEventArgs.SessionLoggedOff(sessionId, previousState, info),
                        SessionLifecycleState.Active when previousState == SessionLifecycleState.Disconnected => SessionEventArgs.SessionConnected(sessionId, newState, info),
                        _ => SessionEventArgs.SessionStateChanged(sessionId, previousState, newState, info)
                    };

                    if (stateEvent != null)
                    {
                        events.Add(stateEvent);
                    }

                    _logger.LogInformation("Session {SessionId} state changed: {Previous} -> {Current}", sessionId, previousState.GetDescription(), newState.GetDescription());
                }
                else
                {
                    // Just update timestamp
                    _sessions[sessionId] = previousSession.WithUpdatedTimestamp();
                }
            }

            // Add events to history
            foreach (var evt in events)
            {
                AddEvent(evt);
            }

            return new SessionCollectionChangedEventArgs(added, removed, changed);
        }
    }

    /// <summary>
    /// Removes a session from tracking.
    /// </summary>
    public SessionCollectionChangedEventArgs RemoveSession(int sessionId)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                _sessions.Remove(sessionId);
                var evt = SessionEventArgs.SessionRemoved(sessionId, null);
                AddEvent(evt);
                _logger.LogInformation("Session removed: {SessionId}", sessionId);
                return new SessionCollectionChangedEventArgs(Array.Empty<ManagedSession>(), new[] { session }, Array.Empty<ManagedSession>());
            }
            return new SessionCollectionChangedEventArgs(Array.Empty<ManagedSession>(), Array.Empty<ManagedSession>(), Array.Empty<ManagedSession>());
        }
    }

    /// <summary>
    /// Reconciles tracked sessions with actual Windows sessions.
    /// </summary>
    public SessionSnapshotDiff Reconcile(IEnumerable<ManagedSession> actualSessions, int currentSessionId)
    {
        lock (_lock)
        {
            var currentSnapshot = SessionSnapshot.Create(_sessions.Values, currentSessionId);
            var actualSnapshot = SessionSnapshot.Create(actualSessions, currentSessionId);
            var diff = currentSnapshot.CompareTo(actualSnapshot);

            // Apply additions
            foreach (var added in diff.Added)
            {
                _sessions[added.SessionId] = added;
                AddEvent(SessionEventArgs.SessionDetected(added.SessionId, null)); // No SessionInfo available
                _logger.LogInformation("Reconciliation: Session added: {SessionId}", added.SessionId);
            }

            // Apply removals
            foreach (var removed in diff.Removed)
            {
                _sessions.Remove(removed.SessionId);
                AddEvent(SessionEventArgs.SessionRemoved(removed.SessionId, null));
                _logger.LogInformation("Reconciliation: Session removed: {SessionId}", removed.SessionId);
            }

            // Apply changes
            foreach (var change in diff.Changed)
            {
                _sessions[change.Current.SessionId] = change.Current;
                if (change.StateChanged)
                {
                    AddEvent(SessionEventArgs.SessionStateChanged(change.SessionId, change.PreviousState, change.CurrentState, null));
                    _logger.LogInformation("Reconciliation: Session {SessionId} state changed: {Previous} -> {Current}", change.SessionId, change.PreviousState.GetDescription(), change.CurrentState.GetDescription());
                }
            }

            return diff;
        }
    }

    /// <summary>
    /// Marks a session as managed by FreeSeat.
    /// </summary>
    public void SetManaged(int sessionId, bool managed)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                _sessions[sessionId] = session.WithManaged(managed);
            }
        }
    }

    /// <summary>
    /// Sets the current session ID.
    /// </summary>
    public void SetCurrentSession(int sessionId)
    {
        lock (_lock)
        {
            foreach (var kvp in _sessions)
            {
                _sessions[kvp.Key] = kvp.Value with { IsCurrent = kvp.Key == sessionId };
            }
        }
    }

    private void AddEvent(SessionEventArgs evt)
    {
        _eventHistory.Add(evt);
        if (_eventHistory.Count > _maxEventHistory)
        {
            _eventHistory.RemoveAt(0);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        lock (_lock)
        {
            _sessions.Clear();
            _eventHistory.Clear();
        }
    }
}

using System.Collections.Generic;

namespace FreeSeat.Core.Sessions.Models;

/// <summary>
/// Represents a point-in-time snapshot of all tracked sessions.
/// Used for reconciliation and state comparison.
/// </summary>
/// <param name="Timestamp">When the snapshot was taken.</param>
/// <param name="Sessions">The sessions in this snapshot.</param>
/// <param name="CurrentSessionId">The current process session ID at snapshot time.</param>
public record SessionSnapshot(
    DateTime Timestamp,
    IReadOnlyList<ManagedSession> Sessions,
    int CurrentSessionId
)
{
    /// <summary>
    /// Creates a snapshot from a collection of managed sessions.
    /// </summary>
    public static SessionSnapshot Create(IEnumerable<ManagedSession> sessions, int currentSessionId)
    {
        return new SessionSnapshot(
            Timestamp: DateTime.UtcNow,
            Sessions: sessions.ToList().AsReadOnly(),
            CurrentSessionId: currentSessionId
        );
    }

    /// <summary>
    /// Gets a session by ID from the snapshot.
    /// </summary>
    public ManagedSession? GetSession(int sessionId)
    {
        return Sessions.FirstOrDefault(s => s.SessionId == sessionId);
    }

    /// <summary>
    /// Compares this snapshot with another and returns the differences.
    /// </summary>
    public SessionSnapshotDiff CompareTo(SessionSnapshot other)
    {
        var added = other.Sessions.Where(s => !Sessions.Any(x => x.SessionId == s.SessionId)).ToList();
        var removed = Sessions.Where(s => !other.Sessions.Any(x => x.SessionId == s.SessionId)).ToList();
        var changed = new List<SessionChange>();

        foreach (var current in Sessions)
        {
            var otherSession = other.GetSession(current.SessionId);
            if (otherSession != null && !current.Equals(otherSession))
            {
                changed.Add(new SessionChange(current, otherSession));
            }
        }

        return new SessionSnapshotDiff(added, removed, changed);
    }
}

/// <summary>
/// Represents a change between two session snapshots.
/// </summary>
/// <param name="Previous">The previous session state.</param>
/// <param name="Current">The current session state.</param>
public record SessionChange(
    ManagedSession Previous,
    ManagedSession Current
)
{
    /// <summary>
    /// Gets the session ID.
    /// </summary>
    public int SessionId => Current.SessionId;

    /// <summary>
    /// Gets the previous lifecycle state.
    /// </summary>
    public SessionLifecycleState PreviousState => Previous.State;

    /// <summary>
    /// Gets the current lifecycle state.
    /// </summary>
    public SessionLifecycleState CurrentState => Current.State;

    /// <summary>
    /// Gets a value indicating whether the state changed.
    /// </summary>
    public bool StateChanged => Previous.State != Current.State;

    /// <summary>
    /// Gets a value indicating whether the session was added.
    /// </summary>
    public bool IsAdded => false; // Handled by diff

    /// <summary>
    /// Gets a value indicating whether the session was removed.
    /// </summary>
    public bool IsRemoved => false; // Handled by diff
}

/// <summary>
/// Represents the differences between two session snapshots.
/// </summary>
/// <param name="Added">Sessions that were added.</param>
/// <param name="Removed">Sessions that were removed.</param>
/// <param name="Changed">Sessions that changed state.</param>
public record SessionSnapshotDiff(
    IReadOnlyList<ManagedSession> Added,
    IReadOnlyList<ManagedSession> Removed,
    IReadOnlyList<SessionChange> Changed
)
{
    /// <summary>
    /// Gets a value indicating whether there are any differences.
    /// </summary>
    public bool HasChanges => Added.Count > 0 || Removed.Count > 0 || Changed.Count > 0;

    /// <summary>
    /// Gets the total number of changes.
    /// </summary>
    public int TotalChanges => Added.Count + Removed.Count + Changed.Count;
}
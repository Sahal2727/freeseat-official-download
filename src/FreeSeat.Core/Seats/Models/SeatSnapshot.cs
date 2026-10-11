using System.Collections.Immutable;
using FreeSeat.Core.Hardware.Models;

namespace FreeSeat.Core.Seats.Models;

/// <summary>
/// A point-in-time snapshot of the complete seat configuration state.
/// Used for backup/restore and change tracking.
/// </summary>
public sealed record SeatSnapshot
{
    /// <summary>
    /// Timestamp when this snapshot was taken.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// The seat configuration at this point in time.
    /// </summary>
    public SeatConfiguration Configuration { get; init; } = SeatConfiguration.Empty;

    /// <summary>
    /// The hardware snapshot at this point in time (for reference).
    /// </summary>
    public HardwareSnapshot? HardwareSnapshot { get; init; }

    /// <summary>
    /// Creates a new snapshot.
    /// </summary>
    public static SeatSnapshot Create(SeatConfiguration configuration, HardwareSnapshot? hardwareSnapshot = null)
        => new()
        {
            Timestamp = DateTime.UtcNow,
            Configuration = configuration,
            HardwareSnapshot = hardwareSnapshot
        };

    /// <summary>
    /// Compares this snapshot with another and returns the differences.
    /// </summary>
    public SeatSnapshotDiff CompareTo(SeatSnapshot other)
    {
        var added = other.Configuration.Seats
            .Where(s => !Configuration.Seats.Any(c => c.Id.Value == s.Id.Value))
            .ToImmutableArray();

        var removed = Configuration.Seats
            .Where(s => !other.Configuration.Seats.Any(c => c.Id.Value == s.Id.Value))
            .ToImmutableArray();

        var changed = new List<SeatChange>();
        foreach (var current in Configuration.Seats)
        {
            var otherSeat = other.Configuration.GetSeat(current.Id);
            if (otherSeat != null && !current.Equals(otherSeat))
            {
                changed.Add(new SeatChange(current, otherSeat));
            }
        }

        return new SeatSnapshotDiff(added, removed, changed.ToImmutableArray());
    }
}

/// <summary>
/// Represents a change between two seat snapshots.
/// </summary>
public sealed record SeatChange(
    Seat Previous,
    Seat Current
)
{
    public SeatId SeatId => Current.Id;
    public bool NameChanged => Previous.Name != Current.Name;
    public bool EnabledChanged => Previous.IsEnabled != Current.IsEnabled;
    public bool AssignmentChanged => !Previous.Assignment.Equals(Current.Assignment);
    public bool StatusChanged => Previous.Status != Current.Status;
}

/// <summary>
/// Represents the differences between two seat snapshots.
/// </summary>
public sealed record SeatSnapshotDiff(
    ImmutableArray<Seat> Added,
    ImmutableArray<Seat> Removed,
    ImmutableArray<SeatChange> Changed
)
{
    public bool HasChanges => Added.Length > 0 || Removed.Length > 0 || Changed.Length > 0;
    public int TotalChanges => Added.Length + Removed.Length + Changed.Length;
}
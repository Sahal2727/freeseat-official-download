using System.Collections.Immutable;

namespace FreeSeat.Core.Seats.Models;

/// <summary>
/// Represents the complete seat configuration for FreeSeat.
/// </summary>
public sealed record SeatConfiguration
{
    /// <summary>
    /// Configuration schema version for migration support.
    /// </summary>
    public int Version { get; init; } = 1;

    /// <summary>
    /// All configured seats.
    /// </summary>
    public ImmutableArray<Seat> Seats { get; init; } = ImmutableArray<Seat>.Empty;

    /// <summary>
    /// Timestamp when this configuration was last saved.
    /// </summary>
    public DateTime SavedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the number of configured seats.
    /// </summary>
    public int Count => Seats.Length;

    /// <summary>
    /// Gets all enabled seats.
    /// </summary>
    public IEnumerable<Seat> EnabledSeats => Seats.Where(s => s.IsEnabled);

    /// <summary>
    /// Gets all seats that are ready for use.
    /// </summary>
    public IEnumerable<Seat> ReadySeats => Seats.Where(s => s.IsReady);

    /// <summary>
    /// Gets a seat by its ID.
    /// </summary>
    public Seat? GetSeat(SeatId id) => Seats.FirstOrDefault(s => s.Id.Value == id.Value);

    /// <summary>
    /// Creates a copy with the specified seats.
    /// </summary>
    public SeatConfiguration WithSeats(IEnumerable<Seat> seats) => this with
    {
        Seats = seats.ToImmutableArray(),
        SavedAt = DateTime.UtcNow
    };

    /// <summary>
    /// Creates a copy with a single seat updated.
    /// </summary>
    public SeatConfiguration WithSeat(Seat seat)
    {
        var list = Seats.ToList();
        var index = list.FindIndex(s => s.Id.Value == seat.Id.Value);
        if (index >= 0)
            list[index] = seat;
        else
            list.Add(seat);
        return this with { Seats = list.ToImmutableArray(), SavedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Creates a copy with a seat removed.
    /// </summary>
    public SeatConfiguration WithoutSeat(SeatId id)
    {
        var list = Seats.Where(s => s.Id.Value != id.Value).ToList();
        return this with { Seats = list.ToImmutableArray(), SavedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Compares this configuration with another and returns the differences.
    /// </summary>
    public SeatSnapshotDiff CompareTo(SeatConfiguration other)
    {
        var snapshot1 = SeatSnapshot.Create(this);
        var snapshot2 = SeatSnapshot.Create(other);
        return snapshot1.CompareTo(snapshot2);
    }

    /// <summary>
    /// Creates a default configuration with Seat 1 and Seat 2.
    /// </summary>
    public static SeatConfiguration CreateDefault()
    {
        var (seat1, seat2) = Seat.CreateDefaultPair();
        return new SeatConfiguration
        {
            Version = 1,
            Seats = [seat1, seat2],
            SavedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an empty configuration.
    /// </summary>
    public static SeatConfiguration Empty => new()
    {
        Version = 1,
        Seats = ImmutableArray<Seat>.Empty,
        SavedAt = DateTime.UtcNow
    };
}
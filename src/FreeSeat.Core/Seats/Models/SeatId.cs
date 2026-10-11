namespace FreeSeat.Core.Seats.Models;

/// <summary>
/// Represents a stable, unique identifier for a Seat.
/// Uses a GUID to ensure stability across application restarts.
/// </summary>
public readonly record struct SeatId
{
    public Guid Value { get; }

    public SeatId(Guid value)
    {
        Value = value;
    }

    public static SeatId New() => new(Guid.NewGuid());

    public static SeatId Empty => new(Guid.Empty);

    public static SeatId Parse(string input) => new(Guid.Parse(input));

    public static bool TryParse(string input, out SeatId seatId)
    {
        if (Guid.TryParse(input, out var guid))
        {
            seatId = new SeatId(guid);
            return true;
        }
        seatId = Empty;
        return false;
    }

    public override string ToString() => Value.ToString("D");

    public static implicit operator Guid(SeatId seatId) => seatId.Value;

    public static explicit operator SeatId(Guid guid) => new(guid);
}
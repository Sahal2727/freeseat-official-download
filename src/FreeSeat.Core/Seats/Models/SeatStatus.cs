namespace FreeSeat.Core.Seats.Models;

/// <summary>
/// Represents the current status of a Seat configuration.
/// </summary>
public enum SeatStatus
{
    /// <summary>
    /// Seat is not configured (missing required assignments).
    /// </summary>
    Unconfigured = 0,

    /// <summary>
    /// Seat is fully configured and ready for use.
    /// All required assignments exist and are available.
    /// </summary>
    Ready = 1,

    /// <summary>
    /// One or more required assignments are missing.
    /// </summary>
    Incomplete = 2,

    /// <summary>
    /// A configured device is currently disconnected.
    /// </summary>
    DeviceUnavailable = 3,

    /// <summary>
    /// Configured Windows session is unavailable.
    /// </summary>
    SessionUnavailable = 4,

    /// <summary>
    /// A device/session is assigned to multiple Seats (conflict).
    /// </summary>
    Conflict = 5,

    /// <summary>
    /// Seat has been disabled by the user.
    /// </summary>
    Disabled = 6,
}

/// <summary>
/// Extension methods for SeatStatus.
/// </summary>
public static class SeatStatusExtensions
{
    public static string GetDescription(this SeatStatus status)
    {
        return status switch
        {
            SeatStatus.Unconfigured => "Not Configured",
            SeatStatus.Ready => "Ready",
            SeatStatus.Incomplete => "Incomplete",
            SeatStatus.DeviceUnavailable => "Device Unavailable",
            SeatStatus.SessionUnavailable => "Session Unavailable",
            SeatStatus.Conflict => "Conflict",
            SeatStatus.Disabled => "Disabled",
            _ => "Unknown",
        };
    }

    public static bool IsUsable(this SeatStatus status) => status == SeatStatus.Ready;
}
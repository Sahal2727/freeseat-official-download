using System.ComponentModel;
using System.Reflection;

namespace FreeSeat.Core.Seats.Models;

/// <summary>
/// Represents the lifecycle state of a FreeSeat-managed seat.
/// This is different from Windows session state - it represents the configuration state.
/// </summary>
public enum SeatLifecycleState
{
    /// <summary>
    /// The seat configuration state is unknown.
    /// </summary>
    [Description("Unknown")]
    Unknown = 0,

    /// <summary>
    /// Seat is not configured (missing required assignments).
    /// </summary>
    [Description("Not Configured")]
    Unconfigured = 1,

    /// <summary>
    /// Seat is fully configured and ready for use.
    /// All required assignments exist and are available.
    /// </summary>
    [Description("Ready")]
    Ready = 2,

    /// <summary>
    /// One or more required assignments are missing.
    /// </summary>
    [Description("Incomplete")]
    Incomplete = 3,

    /// <summary>
    /// A configured device is currently disconnected.
    /// </summary>
    [Description("Device Unavailable")]
    DeviceUnavailable = 4,

    /// <summary>
    /// Configured Windows session is unavailable.
    /// </summary>
    [Description("Session Unavailable")]
    SessionUnavailable = 5,

    /// <summary>
    /// A device/session is assigned to multiple seats (conflict).
    /// </summary>
    [Description("Conflict")]
    Conflict = 6,

    /// <summary>
    /// Seat has been disabled by the user.
    /// </summary>
    [Description("Disabled")]
    Disabled = 7,
}

/// <summary>
/// Extension methods for SeatLifecycleState.
/// </summary>
public static class SeatLifecycleStateExtensions
{
    public static string GetDescription(this SeatLifecycleState state)
    {
        var field = state.GetType().GetField(state.ToString());
        if (field == null) return state.ToString();
        var attr = field.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? state.ToString();
    }
}
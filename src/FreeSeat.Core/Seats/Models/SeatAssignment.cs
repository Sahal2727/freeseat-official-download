using FreeSeat.Core.Hardware.Models;

namespace FreeSeat.Core.Seats.Models;

/// <summary>
/// Represents the assignment of hardware devices and a Windows session to a Seat.
/// </summary>
public sealed record SeatAssignment
{
    /// <summary>
    /// The persistent ID of the assigned monitor (from HardwareDevice.Id).
    /// </summary>
    public string? MonitorId { get; init; }

    /// <summary>
    /// The persistent ID of the assigned keyboard (from HardwareDevice.Id).
    /// </summary>
    public string? KeyboardId { get; init; }

    /// <summary>
    /// The persistent ID of the assigned mouse (from HardwareDevice.Id).
    /// </summary>
    public string? MouseId { get; init; }

    /// <summary>
    /// The Windows session ID assigned to this Seat.
    /// </summary>
    public int? SessionId { get; init; }

    /// <summary>
    /// The optional application path to launch when this seat starts.
    /// </summary>
    public string? ApplicationPath { get; init; }

    /// <summary>
    /// Gets a value indicating whether all required assignments are present.
    /// </summary>
    public bool IsComplete => !string.IsNullOrEmpty(MonitorId)
        && !string.IsNullOrEmpty(KeyboardId)
        && !string.IsNullOrEmpty(MouseId)
        && SessionId.HasValue;

    /// <summary>
    /// Gets a value indicating whether any assignments are present.
    /// </summary>
    public bool HasAnyAssignment => !string.IsNullOrEmpty(MonitorId)
        || !string.IsNullOrEmpty(KeyboardId)
        || !string.IsNullOrEmpty(MouseId)
        || SessionId.HasValue;

    /// <summary>
    /// Creates a copy with the specified monitor ID.
    /// </summary>
    public SeatAssignment WithMonitor(string? monitorId) => this with { MonitorId = monitorId };

    /// <summary>
    /// Creates a copy with the specified keyboard ID.
    /// </summary>
    public SeatAssignment WithKeyboard(string? keyboardId) => this with { KeyboardId = keyboardId };

    /// <summary>
    /// Creates a copy with the specified mouse ID.
    /// </summary>
    public SeatAssignment WithMouse(string? mouseId) => this with { MouseId = mouseId };

    /// <summary>
    /// Creates a copy with the specified session ID.
    /// </summary>
    public SeatAssignment WithSession(int? sessionId) => this with { SessionId = sessionId };

    /// <summary>
    /// Creates a copy with the specified application path.
    /// </summary>
    public SeatAssignment WithApplication(string? applicationPath) => this with { ApplicationPath = applicationPath };

    /// <summary>
    /// Creates a copy with all assignments cleared.
    /// </summary>
    public SeatAssignment Clear() => new();

    /// <summary>
    /// Gets the IDs of all assigned devices.
    /// </summary>
    public IEnumerable<string> GetAssignedDeviceIds()
    {
        if (!string.IsNullOrEmpty(MonitorId)) yield return MonitorId!;
        if (!string.IsNullOrEmpty(KeyboardId)) yield return KeyboardId!;
        if (!string.IsNullOrEmpty(MouseId)) yield return MouseId!;
    }
}
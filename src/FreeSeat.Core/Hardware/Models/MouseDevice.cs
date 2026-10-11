using System.Collections.Immutable;

namespace FreeSeat.Core.Hardware.Models;

/// <summary>
/// Represents a physical mouse/pointing device.
/// </summary>
public sealed record MouseDevice : HardwareDevice
{
    public override DeviceType DeviceType => DeviceType.Mouse;

    /// <summary>
    /// Number of buttons.
    /// </summary>
    public int? NumberOfButtons { get; init; }

    /// <summary>
    /// Whether the mouse has a scroll wheel.
    /// </summary>
    public bool? HasScrollWheel { get; init; }

    /// <summary>
    /// Whether the mouse is wireless.
    /// </summary>
    public bool? IsWireless { get; init; }

    /// <summary>
    /// DPI/CPI if detectable.
    /// </summary>
    public int? Dpi { get; init; }

    /// <summary>
    /// Device interface path for Raw Input API.
    /// </summary>
    public string? DeviceInterfacePath { get; init; }

    /// <summary>
    /// HID usage page and usage (e.g., "0x01/0x02" for mouse).
    /// </summary>
    public string? HidUsage { get; init; }

    /// <summary>
    /// Creates a copy with updated connection status.
    /// </summary>
    public override HardwareDevice WithConnectionStatus(ConnectionStatus status)
    {
        return this with { ConnectionStatus = status, LastSeen = DateTime.UtcNow };
    }
}
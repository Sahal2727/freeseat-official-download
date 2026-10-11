using System.Collections.Immutable;

namespace FreeSeat.Core.Hardware.Models;

/// <summary>
/// Represents a physical keyboard device.
/// </summary>
public sealed record KeyboardDevice : HardwareDevice
{
    public override DeviceType DeviceType => DeviceType.Keyboard;

    /// <summary>
    /// Keyboard layout (e.g., "US", "DE", "FR").
    /// </summary>
    public string? Layout { get; init; }

    /// <summary>
    /// Number of keys if detectable.
    /// </summary>
    public int? NumberOfKeys { get; init; }

    /// <summary>
    /// Whether the keyboard has multimedia keys.
    /// </summary>
    public bool? HasMultimediaKeys { get; init; }

    /// <summary>
    /// Whether the keyboard is wireless.
    /// </summary>
    public bool? IsWireless { get; init; }

    /// <summary>
    /// Device interface path for Raw Input API.
    /// </summary>
    public string? DeviceInterfacePath { get; init; }

    /// <summary>
    /// HID usage page and usage (e.g., "0x01/0x06" for keyboard).
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
using System.Collections.Generic;
using System.Collections.Immutable;

namespace FreeSeat.Core.Hardware.Models;

/// <summary>
/// Base class for all hardware devices.
/// </summary>
public abstract record HardwareDevice
{
    /// <summary>
    /// Stable unique identifier for this device instance.
    /// This should be a persistent identifier like Windows device instance ID or device interface path.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The type of this device.
    /// </summary>
    public abstract DeviceType DeviceType { get; }

    /// <summary>
    /// Human-readable friendly name for display purposes.
    /// </summary>
    public string? FriendlyName { get; init; }

    /// <summary>
    /// Device manufacturer.
    /// </summary>
    public string? Manufacturer { get; init; }

    /// <summary>
    /// Device model name.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Windows device instance identifier (e.g., "HID\\VID_046D&PID_C31C\\7&12345678&0&0000").
    /// This is the primary stable identifier for PnP devices.
    /// </summary>
    public string? DeviceInstanceId { get; init; }

    /// <summary>
    /// Hardware ID(s) as reported by Windows.
    /// </summary>
    public ImmutableArray<string> HardwareIds { get; init; } = ImmutableArray<string>.Empty;

    /// <summary>
    /// Vendor ID (VID) for USB/HID devices.
    /// </summary>
    public string? VendorId { get; init; }

    /// <summary>
    /// Product ID (PID) for USB/HID devices.
    /// </summary>
    public string? ProductId { get; init; }

    /// <summary>
    /// Current connection status.
    /// </summary>
    public ConnectionStatus ConnectionStatus { get; init; } = ConnectionStatus.Unknown;

    /// <summary>
    /// Timestamp when this device was last seen/updated.
    /// </summary>
    public DateTime LastSeen { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata not covered by standard properties.
    /// </summary>
    public ImmutableDictionary<string, string> AdditionalMetadata { get; init; } = ImmutableDictionary<string, string>.Empty;

    /// <summary>
    /// Creates a copy with updated connection status.
    /// </summary>
    public abstract HardwareDevice WithConnectionStatus(ConnectionStatus status);
}
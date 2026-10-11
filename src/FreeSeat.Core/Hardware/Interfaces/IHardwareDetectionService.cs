using System.Collections.Generic;
using FreeSeat.Core.Hardware.Models;

namespace FreeSeat.Core.Hardware.Interfaces;

/// <summary>
/// Service for detecting and monitoring hardware devices.
/// </summary>
public interface IHardwareDetectionService
{
    /// <summary>
    /// Gets the current hardware snapshot.
    /// </summary>
    HardwareSnapshot CurrentSnapshot { get; }

    /// <summary>
    /// Event raised when hardware changes are detected.
    /// </summary>
    event EventHandler<HardwareChangedEventArgs>? HardwareChanged;

    /// <summary>
    /// Initializes the hardware detection service.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a full hardware detection and updates the snapshot.
    /// </summary>
    Task<HardwareSnapshot> DetectHardwareAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes only the specified device types.
    /// </summary>
    Task RefreshAsync(IEnumerable<DeviceType>? deviceTypes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the hardware detection service and releases resources.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Event arguments for hardware change notifications.
/// </summary>
public sealed class HardwareChangedEventArgs : EventArgs
{
    /// <summary>
    /// The type of device that changed.
    /// </summary>
    public DeviceType DeviceType { get; init; }

    /// <summary>
    /// The identifier of the device that changed.
    /// </summary>
    public string DeviceId { get; init; } = string.Empty;

    /// <summary>
    /// The type of change.
    /// </summary>
    public HardwareChangeType ChangeType { get; init; }

    /// <summary>
    /// The device before the change (null for added devices).
    /// </summary>
    public HardwareDevice? OldDevice { get; init; }

    /// <summary>
    /// The device after the change (null for removed devices).
    /// </summary>
    public HardwareDevice? NewDevice { get; init; }

    /// <summary>
    /// Timestamp of the change.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Type of hardware change.
/// </summary>
public enum HardwareChangeType
{
    Added = 0,
    Removed = 1,
    Updated = 2
}
using System.Collections.Generic;
using FreeSeat.Core.Hardware.Models;

namespace FreeSeat.Core.Hardware.Interfaces;

/// <summary>
/// Service for detecting and monitoring display devices and display configuration.
/// Read-only - does not modify display settings.
/// </summary>
public interface IDisplayDetectionService
{
    /// <summary>
    /// Gets the current display snapshot.
    /// </summary>
    DisplaySnapshot CurrentSnapshot { get; }

    /// <summary>
    /// Event raised when display configuration changes are detected.
    /// </summary>
    event EventHandler<DisplayChangedEventArgs>? DisplayChanged;

    /// <summary>
    /// Initializes the display detection service.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a full display detection and updates the snapshot.
    /// </summary>
    Task<DisplaySnapshot> DetectDisplaysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes display detection.
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the display detection service and releases resources.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Snapshot of current display configuration.
/// </summary>
public sealed record DisplaySnapshot
{
    /// <summary>
    /// All detected display devices.
    /// </summary>
    public IReadOnlyList<MonitorDevice> Monitors { get; init; } = Array.Empty<MonitorDevice>();

    /// <summary>
    /// Current Windows display topology.
    /// </summary>
    public DisplayTopology Topology { get; init; } = DisplayTopology.Unknown;

    /// <summary>
    /// Number of active displays.
    /// </summary>
    public int ActiveDisplayCount { get; init; }

    /// <summary>
    /// Primary monitor ID.
    /// </summary>
    public string? PrimaryMonitorId { get; init; }

    /// <summary>
    /// Timestamp of the snapshot.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Windows display topology.
/// </summary>
public enum DisplayTopology
{
    Unknown = 0,
    Internal = 1,       // Single internal display (laptop)
    External = 2,       // Single external display
    Clone = 3,          // Duplicate displays
    Extend = 4,         // Extended desktop
    Extended = 4        // Alias for Extend
}

/// <summary>
/// Event arguments for display change notifications.
/// </summary>
public sealed class DisplayChangedEventArgs : EventArgs
{
    /// <summary>
    /// The identifier of the monitor that changed.
    /// </summary>
    public string MonitorId { get; init; } = string.Empty;

    /// <summary>
    /// The type of change.
    /// </summary>
    public DisplayChangeType ChangeType { get; init; }

    /// <summary>
    /// The monitor before the change (null for added monitors).
    /// </summary>
    public MonitorDevice? OldMonitor { get; init; }

    /// <summary>
    /// The monitor after the change (null for removed monitors).
    /// </summary>
    public MonitorDevice? NewMonitor { get; init; }

    /// <summary>
    /// Timestamp of the change.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Type of display change.
/// </summary>
public enum DisplayChangeType
{
    Added = 0,
    Removed = 1,
    Updated = 2,
    TopologyChanged = 3
}
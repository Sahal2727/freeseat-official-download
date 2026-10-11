using FreeSeat.Core.Hardware.Models;
using FreeSeat.Core.Seats.Models;
using FreeSeat.Core.Sessions.Models;
using System.Collections.Immutable;

namespace FreeSeat.Core.Runtime;

/// <summary>
/// Represents the runtime state of a seat.
/// Distinguishes between configured, available, supported, active, and enforced states.
/// </summary>
public sealed record SeatRuntime
{
    public SeatId Id { get; init; } = SeatId.Empty;
    public string Name { get; init; } = string.Empty;
    public SeatRuntimeState State { get; init; } = SeatRuntimeState.Unknown;
    public bool IsEnabled { get; init; }
    public string? ConfiguredMonitorId { get; init; }
    public MonitorDevice? ActiveMonitor { get; init; }
    public string? ConfiguredKeyboardId { get; init; }
    public KeyboardDevice? ActiveKeyboard { get; init; }
    public string? ConfiguredMouseId { get; init; }
    public MouseDevice? ActiveMouse { get; init; }
    public int? ConfiguredSessionId { get; init; }
    public ManagedSession? ActiveSession { get; init; }
    public ImmutableArray<SeatValidationIssue> Issues { get; init; } = ImmutableArray<SeatValidationIssue>.Empty;
    public bool HasTrueIsolation { get; init; } = false;
    public DateTime LastReconciled { get; init; } = DateTime.UtcNow;

    public string StatusSummary => State switch
    {
        SeatRuntimeState.Ready => "Ready",
        SeatRuntimeState.PartiallyReady => "Partially Ready",
        SeatRuntimeState.Degraded => "Degraded",
        SeatRuntimeState.Unavailable => "Unavailable",
        SeatRuntimeState.Error => "Error: " + string.Join("; ", Issues.Select(i => i.Message)),
        SeatRuntimeState.Initializing => "Initializing...",
        SeatRuntimeState.Stopping => "Stopping...",
        SeatRuntimeState.Stopped => "Stopped",
        _ => "Unknown"
    };

    public bool CanStart => State is SeatRuntimeState.Ready or SeatRuntimeState.PartiallyReady or SeatRuntimeState.Degraded or SeatRuntimeState.Stopped;
}

/// <summary>
/// Runtime state of a seat.
/// </summary>
public enum SeatRuntimeState
{
    Unknown = 0,
    Initializing = 1,
    Ready = 2,
    PartiallyReady = 3,
    Degraded = 4,
    Unavailable = 5,
    Error = 6,
    Starting = 7,
    Running = 8,
    Stopping = 9,
    Stopped = 10,
}

/// <summary>
/// Global runtime status for all seats.
/// </summary>
public sealed record RuntimeStatus
{
    public RuntimeSystemState SystemState { get; init; } = RuntimeSystemState.Unknown;
    public ImmutableArray<SeatRuntime> Seats { get; init; } = ImmutableArray<SeatRuntime>.Empty;
    public HardwareRuntimeStatus Hardware { get; init; } = new();
    public SessionRuntimeStatus Sessions { get; init; } = new();
    public DisplayRuntimeStatus Displays { get; init; } = new();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public string Summary => SystemState switch
    {
        RuntimeSystemState.Ready => "Ready",
        RuntimeSystemState.PartiallyReady => "Partially Ready",
        RuntimeSystemState.Degraded => "Degraded",
        RuntimeSystemState.Error => "Error",
        RuntimeSystemState.Initializing => "Initializing...",
        RuntimeSystemState.Stopped => "Stopped",
        _ => "Unknown"
    };
}

/// <summary>
/// Overall system runtime state.
/// </summary>
public enum RuntimeSystemState
{
    Unknown = 0,
    Initializing = 1,
    Ready = 2,
    PartiallyReady = 3,
    Degraded = 4,
    Error = 5,
    Stopped = 6,
}

/// <summary>
/// Hardware runtime status.
/// </summary>
public sealed record HardwareRuntimeStatus
{
    public int MonitorCount { get; init; }
    public int KeyboardCount { get; init; }
    public int MouseCount { get; init; }
    public bool IsMonitoring { get; init; }
    public DateTime LastUpdate { get; init; }
}

/// <summary>
/// Session runtime status.
/// </summary>
public sealed record SessionRuntimeStatus
{
    public int TotalSessions { get; init; }
    public int ActiveSessions { get; init; }
    public int CurrentSessionId { get; init; }
    public bool IsMonitoring { get; init; }
}

/// <summary>
/// Display runtime status.
/// </summary>
public sealed record DisplayRuntimeStatus
{
    public int DetectedMonitors { get; init; }
    public string Topology { get; init; } = "Unknown";
    public bool IsMonitoring { get; init; }
}
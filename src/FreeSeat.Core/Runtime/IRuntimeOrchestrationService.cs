using FreeSeat.Core.Seats.Models;
using System.Collections.Immutable;

namespace FreeSeat.Core.Runtime;

/// <summary>
/// Service for orchestrating seat runtime operations.
/// Coordinates hardware detection, session management, and seat configuration.
/// </summary>
public interface IRuntimeOrchestrationService
{
    /// <summary>
    /// Gets the current global runtime status.
    /// </summary>
    RuntimeStatus CurrentStatus { get; }

    /// <summary>
    /// Event raised when runtime status changes.
    /// </summary>
    event EventHandler<RuntimeStatusChangedEventArgs>? StatusChanged;

    /// <summary>
    /// Event raised when a seat's runtime state changes.
    /// </summary>
    event EventHandler<SeatRuntimeChangedEventArgs>? SeatStateChanged;

    /// <summary>
    /// Initializes the runtime orchestration service.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a full reconciliation of configured state vs actual state.
    /// </summary>
    Task<RuntimeStatus> ReconcileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a seat if it's ready.
    /// </summary>
    Task<SeatStartResult> StartSeatAsync(SeatId seatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a seat gracefully.
    /// </summary>
    Task<SeatStopResult> StopSeatAsync(SeatId seatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restarts a seat.
    /// </summary>
    Task<SeatStartResult> RestartSeatAsync(SeatId seatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the runtime state for a specific seat.
    /// </summary>
    SeatRuntime? GetSeatRuntime(SeatId seatId);

    /// <summary>
    /// Gets all seat runtimes.
    /// </summary>
    ImmutableArray<SeatRuntime> GetAllSeatRuntimes();

    /// <summary>
    /// Stops the orchestration service.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Event arguments for runtime status changes.
/// </summary>
public sealed class RuntimeStatusChangedEventArgs : EventArgs
{
    public RuntimeStatus PreviousStatus { get; init; } = new();
    public RuntimeStatus CurrentStatus { get; init; } = new();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event arguments for seat runtime state changes.
/// </summary>
public sealed class SeatRuntimeChangedEventArgs : EventArgs
{
    public SeatId SeatId { get; init; } = SeatId.Empty;
    public SeatRuntimeState PreviousState { get; init; }
    public SeatRuntimeState CurrentState { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Result of starting a seat.
/// </summary>
public sealed record SeatStartResult
{
    public SeatId SeatId { get; init; } = SeatId.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public SeatRuntimeState NewState { get; init; }

    public static SeatStartResult SuccessResult(SeatId seatId, SeatRuntimeState newState)
        => new() { SeatId = seatId, Success = true, NewState = newState };

    public static SeatStartResult FailureResult(SeatId seatId, string errorMessage)
        => new() { SeatId = seatId, Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Result of stopping a seat.
/// </summary>
public sealed record SeatStopResult
{
    public SeatId SeatId { get; init; } = SeatId.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static SeatStopResult SuccessResult(SeatId seatId)
        => new() { SeatId = seatId, Success = true };

    public static SeatStopResult FailureResult(SeatId seatId, string errorMessage)
        => new() { SeatId = seatId, Success = false, ErrorMessage = errorMessage };
}
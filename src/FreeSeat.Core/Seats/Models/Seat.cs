using FreeSeat.Core.Hardware.Models;

namespace FreeSeat.Core.Seats.Models;

/// <summary>
/// Represents a Seat - a configured workstation with assigned hardware and session.
/// </summary>
public sealed record Seat
{
    /// <summary>
    /// Stable unique identifier for this Seat.
    /// </summary>
    public SeatId Id { get; init; } = SeatId.New();

    /// <summary>
    /// User-friendly display name for this Seat.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Whether this Seat is enabled.
    /// Disabled seats are not considered for use.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// The hardware and session assignments for this Seat.
    /// </summary>
    public SeatAssignment Assignment { get; init; } = new();

    /// <summary>
    /// The last validation result for this Seat.
    /// </summary>
    public SeatValidationResult? ValidationResult { get; init; }

    /// <summary>
    /// Timestamp when this Seat was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the current status of this Seat.
    /// </summary>
    public SeatStatus Status => ValidationResult?.Status ?? (IsEnabled ? SeatStatus.Unconfigured : SeatStatus.Disabled);

    /// <summary>
    /// Gets a value indicating whether this Seat is ready for use.
    /// </summary>
    public bool IsReady => Status.IsUsable();

    /// <summary>
    /// Creates a copy with the specified name.
    /// </summary>
    public Seat WithName(string name) => this with { Name = name, UpdatedAt = DateTime.UtcNow };

    /// <summary>
    /// Creates a copy with the specified enabled state.
    /// </summary>
    public Seat WithEnabled(bool enabled) => this with { IsEnabled = enabled, UpdatedAt = DateTime.UtcNow };

    /// <summary>
    /// Creates a copy with the specified assignment.
    /// </summary>
    public Seat WithAssignment(SeatAssignment assignment) => this with { Assignment = assignment, UpdatedAt = DateTime.UtcNow };

    /// <summary>
    /// Creates a copy with the specified validation result.
    /// </summary>
    public Seat WithValidation(SeatValidationResult? result) => this with { ValidationResult = result };

    /// <summary>
    /// Creates a copy with the monitor assignment updated.
    /// </summary>
    public Seat WithMonitor(string? monitorId) => this with { Assignment = Assignment.WithMonitor(monitorId), UpdatedAt = DateTime.UtcNow };

    /// <summary>
    /// Creates a copy with the keyboard assignment updated.
    /// </summary>
    public Seat WithKeyboard(string? keyboardId) => this with { Assignment = Assignment.WithKeyboard(keyboardId), UpdatedAt = DateTime.UtcNow };

    /// <summary>
    /// Creates a copy with the mouse assignment updated.
    /// </summary>
    public Seat WithMouse(string? mouseId) => this with { Assignment = Assignment.WithMouse(mouseId), UpdatedAt = DateTime.UtcNow };

    /// <summary>
    /// Creates a copy with the session assignment updated.
    /// </summary>
    public Seat WithSession(int? sessionId) => this with { Assignment = Assignment.WithSession(sessionId), UpdatedAt = DateTime.UtcNow };

    /// <summary>
    /// Creates a copy with the application path updated.
    /// </summary>
    public Seat WithApplication(string? applicationPath) => this with { Assignment = Assignment.WithApplication(applicationPath), UpdatedAt = DateTime.UtcNow };

    /// <summary>
    /// Creates a copy with all assignments cleared.
    /// </summary>
    public Seat ClearAssignments() => this with { Assignment = Assignment.Clear(), UpdatedAt = DateTime.UtcNow };

    /// <summary>
    /// Creates a new Seat with default settings.
    /// </summary>
    public static Seat CreateDefault(string name) => new()
    {
        Name = name,
        IsEnabled = true
    };

    /// <summary>
    /// Creates Seat 1 and Seat 2 with default names.
    /// </summary>
    public static (Seat, Seat) CreateDefaultPair() =>
    (
        CreateDefault("Seat 1"),
        CreateDefault("Seat 2")
    );
}
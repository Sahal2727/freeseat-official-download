using System.Collections.Immutable;
using FreeSeat.Core.Seats.Models;

namespace FreeSeat.Core.Seats.Interfaces;

/// <summary>
/// Event arguments for seat configuration changes.
/// </summary>
public sealed record SeatConfigurationChangedEventArgs
{
    public SeatChangeType ChangeType { get; init; }
    public SeatId SeatId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Types of seat configuration changes.
/// </summary>
public enum SeatChangeType
{
    SeatCreated = 0,
    SeatRemoved = 1,
    SeatRenamed = 2,
    SeatEnabled = 3,
    SeatDisabled = 4,
    AssignmentChanged = 5,
    SeatReset = 6,
    ConfigurationImported = 7,
    ConfigurationRestored = 8,
}

/// <summary>
/// Event arguments for seat validation changes.
/// </summary>
public sealed record SeatValidationChangedEventArgs
{
    public SeatId SeatId { get; init; }
    public SeatValidationResult? ValidationResult { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Result of importing a seat configuration.
/// </summary>
public sealed record ImportResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public SeatConfiguration? Configuration { get; init; }
    public ImmutableArray<SeatValidationIssue> ValidationIssues { get; init; } = ImmutableArray<SeatValidationIssue>.Empty;

    public static ImportResult SuccessResult(SeatConfiguration config) => new()
    {
        Success = true,
        Configuration = config
    };

    public static ImportResult Failure(string error, ImmutableArray<SeatValidationIssue> issues = default) => new()
    {
        Success = false,
        ErrorMessage = error,
        ValidationIssues = issues.IsDefault ? ImmutableArray<SeatValidationIssue>.Empty : issues
    };
}

/// <summary>
/// Result of restoring a backup.
/// </summary>
public sealed record RestoreResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public SeatSnapshotDiff? Diff { get; init; }

    public static RestoreResult SuccessResult(SeatSnapshotDiff diff) => new()
    {
        Success = true,
        Diff = diff
    };

    public static RestoreResult Failure(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };
}
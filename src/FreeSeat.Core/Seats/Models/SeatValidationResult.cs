using System.Collections.Generic;
using System.Collections.Immutable;

namespace FreeSeat.Core.Seats.Models;

/// <summary>
/// Represents the result of validating a Seat configuration.
/// </summary>
public sealed record SeatValidationResult
{
    public SeatStatus Status { get; init; }
    public ImmutableArray<SeatValidationIssue> Issues { get; init; } = ImmutableArray<SeatValidationIssue>.Empty;
    public DateTime ValidatedAt { get; init; } = DateTime.UtcNow;
    public bool IsValid => Status.IsUsable();

    public static SeatValidationResult Success() => new()
    {
        Status = SeatStatus.Ready,
        Issues = ImmutableArray<SeatValidationIssue>.Empty
    };

    public static SeatValidationResult Failure(IEnumerable<SeatValidationIssue> issues)
    {
        var issuesArray = issues.ToImmutableArray();
        var status = DetermineStatus(issuesArray);
        return new SeatValidationResult
        {
            Status = status,
            Issues = issuesArray
        };
    }

    public static SeatValidationResult Failure(SeatValidationIssue issue)
        => Failure(new[] { issue });

    private static SeatStatus DetermineStatus(ImmutableArray<SeatValidationIssue> issues)
    {
        if (issues.Any(i => i.Type == ValidationIssueType.Conflict))
            return SeatStatus.Conflict;
        if (issues.Any(i => i.Type == ValidationIssueType.SessionUnavailable))
            return SeatStatus.SessionUnavailable;
        if (issues.Any(i => i.Type == ValidationIssueType.DeviceUnavailable))
            return SeatStatus.DeviceUnavailable;
        if (issues.Any(i => i.Type == ValidationIssueType.MissingRequired))
            return SeatStatus.Incomplete;
        return SeatStatus.Unconfigured;
    }
}
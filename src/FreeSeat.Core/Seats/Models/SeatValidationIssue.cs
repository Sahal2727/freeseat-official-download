using FreeSeat.Core.Hardware.Models;

namespace FreeSeat.Core.Seats.Models;

/// <summary>
/// Represents a specific validation issue for a Seat.
/// </summary>
public sealed record SeatValidationIssue
{
    public ValidationIssueType Type { get; init; }
    public DeviceType? DeviceType { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? AffectedId { get; init; }

    public static SeatValidationIssue MissingRequired(DeviceType deviceType)
        => new()
        {
            Type = ValidationIssueType.MissingRequired,
            DeviceType = deviceType,
            Message = $"Required {deviceType.GetDescription().ToLower()} is not assigned"
        };

    public static SeatValidationIssue DeviceUnavailable(DeviceType deviceType, string deviceId)
        => new()
        {
            Type = ValidationIssueType.DeviceUnavailable,
            DeviceType = deviceType,
            AffectedId = deviceId,
            Message = $"Assigned {deviceType.GetDescription().ToLower()} ({deviceId}) is currently disconnected"
        };

    public static SeatValidationIssue SessionUnavailable(int sessionId)
        => new()
        {
            Type = ValidationIssueType.SessionUnavailable,
            AffectedId = sessionId.ToString(),
            Message = $"Assigned session {sessionId} is unavailable"
        };

    public static SeatValidationIssue Conflict(DeviceType deviceType, string deviceId, IEnumerable<string> seatNames)
        => new()
        {
            Type = ValidationIssueType.Conflict,
            DeviceType = deviceType,
            AffectedId = deviceId,
            Message = $"{deviceType.GetDescription()} ({deviceId}) is assigned to multiple seats: {string.Join(", ", seatNames)}"
        };

    public static SeatValidationIssue Invalid(string message)
        => new()
        {
            Type = ValidationIssueType.Invalid,
            Message = message
        };
}

/// <summary>
/// Types of validation issues that can occur.
/// </summary>
public enum ValidationIssueType
{
    MissingRequired = 0,
    DeviceUnavailable = 1,
    SessionUnavailable = 2,
    Conflict = 3,
    Invalid = 4,
}
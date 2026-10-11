using FreeSeat.Core.Hardware.Models;
using FreeSeat.Core.Seats.Models;
using FreeSeat.Core.Sessions.Models;

namespace FreeSeat.Core.Seats.Interfaces;

/// <summary>
/// Service for managing Seat configurations.
/// Handles seat creation, device/session assignment, validation, and persistence.
/// </summary>
public interface ISeatConfigurationService
{
    SeatConfiguration Configuration { get; }
    event EventHandler<SeatConfigurationChangedEventArgs>? ConfigurationChanged;
    event EventHandler<SeatValidationChangedEventArgs>? SeatValidationChanged;

    Seat? GetSeat(SeatId seatId);
    IReadOnlyList<Seat> GetAllSeats();
    IReadOnlyList<Seat> GetEnabledSeats();
    IReadOnlyList<Seat> GetReadySeats();

    Seat CreateSeat(string name);
    Task<bool> RenameSeatAsync(SeatId seatId, string newName, CancellationToken cancellationToken = default);
    Task<bool> SetSeatEnabledAsync(SeatId seatId, bool enabled, CancellationToken cancellationToken = default);
    Task<bool> AssignMonitorAsync(SeatId seatId, string? monitorId, CancellationToken cancellationToken = default);
    Task<bool> AssignKeyboardAsync(SeatId seatId, string? keyboardId, CancellationToken cancellationToken = default);
    Task<bool> AssignMouseAsync(SeatId seatId, string? mouseId, CancellationToken cancellationToken = default);
    Task<bool> AssignSessionAsync(SeatId seatId, int? sessionId, CancellationToken cancellationToken = default);
    Task<bool> ResetSeatAsync(SeatId seatId, CancellationToken cancellationToken = default);

    Task<SeatValidationResult> ValidateSeatAsync(SeatId seatId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SeatValidationResult>> ValidateAllAsync(CancellationToken cancellationToken = default);

    Task<string> ExportAsync(CancellationToken cancellationToken = default);
    Task<ImportResult> ImportAsync(string json, CancellationToken cancellationToken = default);
    Task<SeatSnapshot> CreateBackupAsync(CancellationToken cancellationToken = default);
    Task<RestoreResult> RestoreBackupAsync(SeatSnapshot snapshot, CancellationToken cancellationToken = default);

    IReadOnlyList<MonitorDevice> GetAvailableMonitors();
    IReadOnlyList<KeyboardDevice> GetAvailableKeyboards();
    IReadOnlyList<MouseDevice> GetAvailableMice();
    IReadOnlyList<ManagedSession> GetAvailableSessions();

    bool IsDeviceAssignedElsewhere(string deviceId, SeatId excludeSeatId);
    bool IsSessionAssignedElsewhere(int sessionId, SeatId excludeSeatId);
}
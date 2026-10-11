using System.Collections.Immutable;
using System.Text.Json;
using FreeSeat.Core.Hardware.Interfaces;
using FreeSeat.Core.Hardware.Models;
using FreeSeat.Core.Seats.Interfaces;
using FreeSeat.Core.Seats.Models;
using FreeSeat.Core.Sessions.Interfaces;
using FreeSeat.Core.Sessions.Models;
using FreeSeat.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace FreeSeat.Infrastructure.Seats;

internal sealed class SeatConfigurationService : ISeatConfigurationService, IDisposable
{
    private readonly ILogger<SeatConfigurationService> _logger;
    private readonly IHardwareDetectionService _hardwareService;
    private readonly ISessionManager _sessionManager;
    private readonly ISettingsService _settingsService;
    private readonly object _lock = new();
    private readonly JsonSerializerOptions _jsonOptions;
    private SeatConfiguration _configuration;
    private bool _disposed;

    public SeatConfiguration Configuration => _configuration;

    public event EventHandler<SeatConfigurationChangedEventArgs>? ConfigurationChanged;
    public event EventHandler<SeatValidationChangedEventArgs>? SeatValidationChanged;

    public SeatConfigurationService(
        ILogger<SeatConfigurationService> logger,
        IHardwareDetectionService hardwareService,
        ISessionManager sessionManager,
        ISettingsService settingsService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hardwareService = hardwareService ?? throw new ArgumentNullException(nameof(hardwareService));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        _configuration = SeatConfiguration.CreateDefault();
        LoadConfiguration();
        SubscribeToEvents();
    }

    private void LoadConfiguration()
    {
        try
        {
            var json = _settingsService.Settings.SeatConfiguration.SeatConfiguration;
            if (!string.IsNullOrWhiteSpace(json))
            {
                var loaded = JsonSerializer.Deserialize<SeatConfiguration>(json, _jsonOptions);
                if (loaded != null && loaded.Version <= 1)
                {
                    _configuration = loaded with { SavedAt = DateTime.UtcNow };
                    _logger.LogInformation("Loaded seat configuration with {Count} seats", _configuration.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load seat configuration, using defaults");
            _configuration = SeatConfiguration.CreateDefault();
        }
    }

    private void SaveConfiguration()
    {
        try
        {
            var json = JsonSerializer.Serialize(_configuration, _jsonOptions);
            _settingsService.Settings.SeatConfiguration.SeatConfiguration = json;
            _ = _settingsService.SaveAsync();
            _logger.LogDebug("Saved seat configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save seat configuration");
        }
    }

    private void SubscribeToEvents()
    {
        _hardwareService.HardwareChanged += OnHardwareChanged;
        _sessionManager.SessionEvent += OnSessionEvent;
        _sessionManager.SessionsChanged += OnSessionsChanged;
    }

    private void OnHardwareChanged(object? sender, HardwareChangedEventArgs e)
    {
        lock (_lock)
        {
            var updated = UpdateDeviceAvailability(e);
            if (updated)
            {
                _ = ValidateAllAsync();
            }
        }
    }

    private void OnSessionEvent(object? sender, SessionEventArgs e)
    {
        lock (_lock)
        {
            _ = ValidateAllAsync();
        }
    }

    private void OnSessionsChanged(object? sender, SessionCollectionChangedEventArgs e)
    {
        lock (_lock)
        {
            _ = ValidateAllAsync();
        }
    }

    private bool UpdateDeviceAvailability(HardwareChangedEventArgs e)
    {
        var changed = false;
        foreach (var seat in _configuration.Seats)
        {
            var assignment = seat.Assignment;
            var needsValidation = false;

            if (!string.IsNullOrEmpty(assignment.MonitorId))
            {
                var monitor = _hardwareService.CurrentSnapshot.Monitors.FirstOrDefault(m => m.Id == assignment.MonitorId);
                if (monitor?.ConnectionStatus != ConnectionStatus.Connected)
                    needsValidation = true;
            }

            if (!string.IsNullOrEmpty(assignment.KeyboardId))
            {
                var keyboard = _hardwareService.CurrentSnapshot.Keyboards.FirstOrDefault(k => k.Id == assignment.KeyboardId);
                if (keyboard?.ConnectionStatus != ConnectionStatus.Connected)
                    needsValidation = true;
            }

            if (!string.IsNullOrEmpty(assignment.MouseId))
            {
                var mouse = _hardwareService.CurrentSnapshot.Mice.FirstOrDefault(m => m.Id == assignment.MouseId);
                if (mouse?.ConnectionStatus != ConnectionStatus.Connected)
                    needsValidation = true;
            }

            if (needsValidation)
            {
                _ = ValidateSeatAsync(seat.Id);
                changed = true;
            }
        }
        return changed;
    }

    private void RaiseConfigurationChanged(SeatChangeType type, SeatId seatId)
    {
        ConfigurationChanged?.Invoke(this, new SeatConfigurationChangedEventArgs
        {
            ChangeType = type,
            SeatId = seatId
        });
    }

    private void RaiseValidationChanged(SeatId seatId, SeatValidationResult result)
    {
        SeatValidationChanged?.Invoke(this, new SeatValidationChangedEventArgs
        {
            SeatId = seatId,
            ValidationResult = result
        });
    }

    public Seat? GetSeat(SeatId seatId)
    {
        lock (_lock) { return _configuration.GetSeat(seatId); }
    }

    public IReadOnlyList<Seat> GetAllSeats()
    {
        lock (_lock) { return _configuration.Seats.ToList(); }
    }

    public IReadOnlyList<Seat> GetEnabledSeats()
    {
        lock (_lock) { return _configuration.EnabledSeats.ToList(); }
    }

    public IReadOnlyList<Seat> GetReadySeats()
    {
        lock (_lock) { return _configuration.ReadySeats.ToList(); }
    }

    public Seat CreateSeat(string name)
    {
        lock (_lock)
        {
            var seat = Seat.CreateDefault(name);
            _configuration = _configuration.WithSeat(seat);
            SaveConfiguration();
            RaiseConfigurationChanged(SeatChangeType.SeatCreated, seat.Id);
            _logger.LogInformation("Created seat {SeatId}: {Name}", seat.Id, seat.Name);
            return seat;
        }
    }

    public Task<bool> RenameSeatAsync(SeatId seatId, string newName, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var seat = _configuration.GetSeat(seatId);
            if (seat == null) return Task.FromResult(false);
            _configuration = _configuration.WithSeat(seat.WithName(newName));
            SaveConfiguration();
            RaiseConfigurationChanged(SeatChangeType.SeatRenamed, seatId);
            _logger.LogInformation("Renamed seat {SeatId} to {Name}", seatId, newName);
            return Task.FromResult(true);
        }
    }

    public Task<bool> SetSeatEnabledAsync(SeatId seatId, bool enabled, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var seat = _configuration.GetSeat(seatId);
            if (seat == null) return Task.FromResult(false);
            _configuration = _configuration.WithSeat(seat.WithEnabled(enabled));
            SaveConfiguration();
            _ = ValidateSeatAsync(seatId);
            RaiseConfigurationChanged(enabled ? SeatChangeType.SeatEnabled : SeatChangeType.SeatDisabled, seatId);
            _logger.LogInformation("{Action} seat {SeatId}", enabled ? "Enabled" : "Disabled", seatId);
            return Task.FromResult(true);
        }
    }

    public Task<bool> AssignMonitorAsync(SeatId seatId, string? monitorId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var seat = _configuration.GetSeat(seatId);
            if (seat == null) return Task.FromResult(false);

            if (monitorId != null && IsDeviceAssignedElsewhere(monitorId, seatId))
            {
                _logger.LogWarning("Monitor {MonitorId} already assigned to another seat", monitorId);
                return Task.FromResult(false);
            }

            _configuration = _configuration.WithSeat(seat.WithMonitor(monitorId));
            SaveConfiguration();
            _ = ValidateSeatAsync(seatId);
            RaiseConfigurationChanged(SeatChangeType.AssignmentChanged, seatId);
            _logger.LogInformation("Assigned monitor {MonitorId} to seat {SeatId}", monitorId, seatId);
            return Task.FromResult(true);
        }
    }

    public Task<bool> AssignKeyboardAsync(SeatId seatId, string? keyboardId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var seat = _configuration.GetSeat(seatId);
            if (seat == null) return Task.FromResult(false);

            if (keyboardId != null && IsDeviceAssignedElsewhere(keyboardId, seatId))
            {
                _logger.LogWarning("Keyboard {KeyboardId} already assigned to another seat", keyboardId);
                return Task.FromResult(false);
            }

            _configuration = _configuration.WithSeat(seat.WithKeyboard(keyboardId));
            SaveConfiguration();
            _ = ValidateSeatAsync(seatId);
            RaiseConfigurationChanged(SeatChangeType.AssignmentChanged, seatId);
            _logger.LogInformation("Assigned keyboard {KeyboardId} to seat {SeatId}", keyboardId, seatId);
            return Task.FromResult(true);
        }
    }

    public Task<bool> AssignMouseAsync(SeatId seatId, string? mouseId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var seat = _configuration.GetSeat(seatId);
            if (seat == null) return Task.FromResult(false);

            if (mouseId != null && IsDeviceAssignedElsewhere(mouseId, seatId))
            {
                _logger.LogWarning("Mouse {MouseId} already assigned to another seat", mouseId);
                return Task.FromResult(false);
            }

            _configuration = _configuration.WithSeat(seat.WithMouse(mouseId));
            SaveConfiguration();
            _ = ValidateSeatAsync(seatId);
            RaiseConfigurationChanged(SeatChangeType.AssignmentChanged, seatId);
            _logger.LogInformation("Assigned mouse {MouseId} to seat {SeatId}", mouseId, seatId);
            return Task.FromResult(true);
        }
    }

    public Task<bool> AssignSessionAsync(SeatId seatId, int? sessionId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var seat = _configuration.GetSeat(seatId);
            if (seat == null) return Task.FromResult(false);

            if (sessionId.HasValue && IsSessionAssignedElsewhere(sessionId.Value, seatId))
            {
                _logger.LogWarning("Session {SessionId} already assigned to another seat", sessionId);
                return Task.FromResult(false);
            }

            _configuration = _configuration.WithSeat(seat.WithSession(sessionId));
            SaveConfiguration();
            _ = ValidateSeatAsync(seatId);
            RaiseConfigurationChanged(SeatChangeType.AssignmentChanged, seatId);
            _logger.LogInformation("Assigned session {SessionId} to seat {SeatId}", sessionId, seatId);
            return Task.FromResult(true);
        }
    }

    public Task<bool> ResetSeatAsync(SeatId seatId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var seat = _configuration.GetSeat(seatId);
            if (seat == null) return Task.FromResult(false);
            _configuration = _configuration.WithSeat(seat.ClearAssignments());
            SaveConfiguration();
            _ = ValidateSeatAsync(seatId);
            RaiseConfigurationChanged(SeatChangeType.SeatReset, seatId);
            _logger.LogInformation("Reset seat {SeatId}", seatId);
            return Task.FromResult(true);
        }
    }

    public Task<SeatValidationResult> ValidateSeatAsync(SeatId seatId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var seat = _configuration.GetSeat(seatId);
            if (seat == null)
                return Task.FromResult(SeatValidationResult.Failure(SeatValidationIssue.Invalid("Seat not found")));

            var result = PerformValidation(seat);
            _configuration = _configuration.WithSeat(seat.WithValidation(result));
            RaiseValidationChanged(seatId, result);
            return Task.FromResult(result);
        }
    }

    private SeatValidationResult PerformValidation(Seat seat)
    {
        if (!seat.IsEnabled)
            return SeatValidationResult.Failure(SeatValidationIssue.Invalid("Seat is disabled"));

        var issues = new List<SeatValidationIssue>();
        var assignment = seat.Assignment;

        if (string.IsNullOrEmpty(assignment.MonitorId))
            issues.Add(SeatValidationIssue.MissingRequired(DeviceType.Monitor));
        else if (!_hardwareService.CurrentSnapshot.Monitors.Any(m => m.Id == assignment.MonitorId))
            issues.Add(SeatValidationIssue.DeviceUnavailable(DeviceType.Monitor, assignment.MonitorId!));
        else
        {
            var monitor = _hardwareService.CurrentSnapshot.Monitors.First(m => m.Id == assignment.MonitorId!);
            if (monitor.ConnectionStatus != ConnectionStatus.Connected)
                issues.Add(SeatValidationIssue.DeviceUnavailable(DeviceType.Monitor, assignment.MonitorId!));
        }

        if (string.IsNullOrEmpty(assignment.KeyboardId))
            issues.Add(SeatValidationIssue.MissingRequired(DeviceType.Keyboard));
        else if (!_hardwareService.CurrentSnapshot.Keyboards.Any(k => k.Id == assignment.KeyboardId))
            issues.Add(SeatValidationIssue.DeviceUnavailable(DeviceType.Keyboard, assignment.KeyboardId!));
        else
        {
            var keyboard = _hardwareService.CurrentSnapshot.Keyboards.First(k => k.Id == assignment.KeyboardId!);
            if (keyboard.ConnectionStatus != ConnectionStatus.Connected)
                issues.Add(SeatValidationIssue.DeviceUnavailable(DeviceType.Keyboard, assignment.KeyboardId!));
        }

        if (string.IsNullOrEmpty(assignment.MouseId))
            issues.Add(SeatValidationIssue.MissingRequired(DeviceType.Mouse));
        else if (!_hardwareService.CurrentSnapshot.Mice.Any(m => m.Id == assignment.MouseId))
            issues.Add(SeatValidationIssue.DeviceUnavailable(DeviceType.Mouse, assignment.MouseId!));
        else
        {
            var mouse = _hardwareService.CurrentSnapshot.Mice.First(m => m.Id == assignment.MouseId!);
            if (mouse.ConnectionStatus != ConnectionStatus.Connected)
                issues.Add(SeatValidationIssue.DeviceUnavailable(DeviceType.Mouse, assignment.MouseId!));
        }

        if (!assignment.SessionId.HasValue)
            issues.Add(SeatValidationIssue.MissingRequired(DeviceType.Unknown));
        else
        {
            var session = _sessionManager.GetSession(assignment.SessionId.Value);
            if (session == null)
                issues.Add(SeatValidationIssue.SessionUnavailable(assignment.SessionId.Value));
            else if (session.State is not (SessionLifecycleState.Active or SessionLifecycleState.Unlocked or SessionLifecycleState.Detected))
                issues.Add(SeatValidationIssue.SessionUnavailable(assignment.SessionId.Value));
        }

        if (!string.IsNullOrEmpty(assignment.MonitorId) && IsDeviceAssignedElsewhere(assignment.MonitorId, seat.Id))
            issues.Add(SeatValidationIssue.Conflict(DeviceType.Monitor, assignment.MonitorId!, GetSeatsUsingDevice(assignment.MonitorId, seat.Id)));

        if (!string.IsNullOrEmpty(assignment.KeyboardId) && IsDeviceAssignedElsewhere(assignment.KeyboardId, seat.Id))
            issues.Add(SeatValidationIssue.Conflict(DeviceType.Keyboard, assignment.KeyboardId!, GetSeatsUsingDevice(assignment.KeyboardId, seat.Id)));

        if (!string.IsNullOrEmpty(assignment.MouseId) && IsDeviceAssignedElsewhere(assignment.MouseId, seat.Id))
            issues.Add(SeatValidationIssue.Conflict(DeviceType.Mouse, assignment.MouseId!, GetSeatsUsingDevice(assignment.MouseId, seat.Id)));

        if (assignment.SessionId.HasValue && IsSessionAssignedElsewhere(assignment.SessionId.Value, seat.Id))
            issues.Add(SeatValidationIssue.Conflict(DeviceType.Unknown, assignment.SessionId.Value.ToString(), GetSeatsUsingSession(assignment.SessionId.Value)));

        var result = issues.Count == 0
            ? SeatValidationResult.Success()
            : SeatValidationResult.Failure(issues);

        return result;
    }

    private IEnumerable<string> GetSeatsUsingDevice(string deviceId, SeatId excludeSeatId)
    {
        return _configuration.Seats
            .Where(s => s.Id.Value != excludeSeatId.Value)
            .Where(s => s.Assignment.MonitorId == deviceId
                || s.Assignment.KeyboardId == deviceId
                || s.Assignment.MouseId == deviceId)
            .Select(s => s.Name);
    }

    private IEnumerable<string> GetSeatsUsingSession(int sessionId)
    {
        return _configuration.Seats
            .Where(s => s.Assignment.SessionId == sessionId)
            .Select(s => s.Name);
    }

    public Task<IReadOnlyList<SeatValidationResult>> ValidateAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<SeatValidationResult>();
        foreach (var seat in _configuration.Seats)
        {
            results.Add(ValidateSeatAsync(seat.Id, cancellationToken).GetAwaiter().GetResult());
        }
        return Task.FromResult((IReadOnlyList<SeatValidationResult>)results);
    }

    public Task<string> ExportAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(JsonSerializer.Serialize(_configuration, _jsonOptions));
        }
    }

    public async Task<ImportResult> ImportAsync(string json, CancellationToken cancellationToken = default)
    {
        try
        {
            var imported = JsonSerializer.Deserialize<SeatConfiguration>(json, _jsonOptions);
            if (imported == null)
                return ImportResult.Failure("Failed to parse configuration JSON");

            if (imported.Version > 1)
                return ImportResult.Failure($"Configuration version {imported.Version} is not supported");

            var validationIssues = new List<SeatValidationIssue>();
            foreach (var seat in imported.Seats)
            {
                if (imported.Seats.Count(s => s.Id.Value == seat.Id.Value) > 1)
                    validationIssues.Add(SeatValidationIssue.Invalid($"Duplicate seat ID: {seat.Id}"));
            }

            if (validationIssues.Count > 0)
                return ImportResult.Failure("Validation failed", validationIssues.ToImmutableArray());

            lock (_lock)
            {
                _configuration = imported with { SavedAt = DateTime.UtcNow };
                SaveConfiguration();
                RaiseConfigurationChanged(SeatChangeType.ConfigurationImported, SeatId.Empty);
                _logger.LogInformation("Imported seat configuration with {Count} seats", imported.Count);
            }

            await ValidateAllAsync(cancellationToken);
            return ImportResult.SuccessResult(imported);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to import seat configuration");
            return ImportResult.Failure($"Invalid JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing seat configuration");
            return ImportResult.Failure($"Import failed: {ex.Message}");
        }
    }

    public Task<SeatSnapshot> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var snapshot = SeatSnapshot.Create(_configuration, _hardwareService.CurrentSnapshot);
            _logger.LogDebug("Created seat configuration backup");
            return Task.FromResult(snapshot);
        }
    }

    public Task<RestoreResult> RestoreBackupAsync(SeatSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_lock)
            {
                var diff = _configuration.CompareTo(snapshot.Configuration);
                _configuration = snapshot.Configuration with { SavedAt = DateTime.UtcNow };
                SaveConfiguration();
                RaiseConfigurationChanged(SeatChangeType.ConfigurationRestored, SeatId.Empty);
                _logger.LogInformation("Restored seat configuration from backup");
                return Task.FromResult(RestoreResult.SuccessResult(diff));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore seat configuration backup");
            return Task.FromResult(RestoreResult.Failure($"Restore failed: {ex.Message}"));
        }
    }

    public IReadOnlyList<MonitorDevice> GetAvailableMonitors()
    {
        return _hardwareService.CurrentSnapshot.Monitors
            .Where(m => m.ConnectionStatus == ConnectionStatus.Connected)
            .ToList();
    }

    public IReadOnlyList<KeyboardDevice> GetAvailableKeyboards()
    {
        return _hardwareService.CurrentSnapshot.Keyboards
            .Where(k => k.ConnectionStatus == ConnectionStatus.Connected)
            .ToList();
    }

    public IReadOnlyList<MouseDevice> GetAvailableMice()
    {
        return _hardwareService.CurrentSnapshot.Mice
            .Where(m => m.ConnectionStatus == ConnectionStatus.Connected)
            .ToList();
    }

    public IReadOnlyList<ManagedSession> GetAvailableSessions()
    {
        return _sessionManager.Sessions
            .Where(s => s.CanLaunchProcess)
            .ToList();
    }

    public bool IsDeviceAssignedElsewhere(string deviceId, SeatId excludeSeatId)
    {
        return _configuration.Seats
            .Where(s => s.Id.Value != excludeSeatId.Value)
            .Any(s => s.Assignment.MonitorId == deviceId
                || s.Assignment.KeyboardId == deviceId
                || s.Assignment.MouseId == deviceId);
    }

    public bool IsSessionAssignedElsewhere(int sessionId, SeatId excludeSeatId)
    {
        return _configuration.Seats
            .Where(s => s.Id.Value != excludeSeatId.Value)
            .Any(s => s.Assignment.SessionId == sessionId);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _hardwareService.HardwareChanged -= OnHardwareChanged;
        _sessionManager.SessionEvent -= OnSessionEvent;
        _sessionManager.SessionsChanged -= OnSessionsChanged;
    }
}

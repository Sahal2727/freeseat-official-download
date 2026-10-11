using FreeSeat.Core.Hardware.Interfaces;
using FreeSeat.Core.Hardware.Models;
using FreeSeat.Core.Runtime;
using FreeSeat.Core.Seats.Interfaces;
using FreeSeat.Core.Seats.Models;
using FreeSeat.Core.Sessions.Interfaces;
using FreeSeat.Core.Sessions.Models;
using FreeSeat.Infrastructure.Configuration;
using FreeSeat.Infrastructure.Sessions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;

namespace FreeSeat.Infrastructure.Runtime;

/// <summary>
/// Internal class for tracking running process information.
/// </summary>
internal sealed class ProcessRuntimeInfo
{
    public int ProcessId { get; init; }
    public SeatId SeatId { get; init; } = SeatId.Empty;
    public DateTime StartTime { get; init; }
    public DateTime? ExitTime { get; set; }
    public bool IsRunning { get; set; }
}

/// <summary>
/// Orchestrates seat runtime operations, coordinating hardware detection, 
/// session management, seat configuration, and process launching.
/// </summary>
internal sealed class RuntimeOrchestrationService : IRuntimeOrchestrationService, IDisposable
{
    private readonly ILogger<RuntimeOrchestrationService> _logger;
    private readonly ISeatConfigurationService _seatConfigService;
    private readonly IHardwareDetectionService _hardwareService;
    private readonly IDisplayDetectionService _displayService;
    private readonly ISessionManager _sessionManager;
    private readonly SessionLauncher _launcher;
    private readonly IOptionsMonitor<SessionManagerSettings> _settingsMonitor;
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<SeatId, SeatRuntime> _seatRuntimes = new();
    private readonly ConcurrentDictionary<int, ProcessRuntimeInfo> _runningProcesses = new();
    private int _reconciliationDepth;
    private Timer? _reconciliationTimer;
    private Timer? _processMonitorTimer;
    private bool _disposed;
    private bool _initialized;

    /// <inheritdoc />
    public RuntimeStatus CurrentStatus
    {
        get
        {
            lock (_lock)
            {
                return ComputeCurrentStatus();
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<RuntimeStatusChangedEventArgs>? StatusChanged;

    /// <inheritdoc />
    public event EventHandler<SeatRuntimeChangedEventArgs>? SeatStateChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeOrchestrationService"/> class.
    /// </summary>
    public RuntimeOrchestrationService(
        ILogger<RuntimeOrchestrationService> logger,
        ISeatConfigurationService seatConfigService,
        IHardwareDetectionService hardwareService,
        IDisplayDetectionService displayService,
        ISessionManager sessionManager,
        SessionLauncher launcher,
        IOptionsMonitor<SessionManagerSettings> settingsMonitor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _seatConfigService = seatConfigService ?? throw new ArgumentNullException(nameof(seatConfigService));
        _hardwareService = hardwareService ?? throw new ArgumentNullException(nameof(hardwareService));
        _displayService = displayService ?? throw new ArgumentNullException(nameof(displayService));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _settingsMonitor = settingsMonitor ?? throw new ArgumentNullException(nameof(settingsMonitor));

        _seatConfigService.ConfigurationChanged += OnConfigurationChanged;
        _seatConfigService.SeatValidationChanged += OnSeatValidationChanged;
        _hardwareService.HardwareChanged += OnHardwareChanged;
        _displayService.DisplayChanged += OnDisplayChanged;
        _sessionManager.SessionEvent += OnSessionEvent;
        _sessionManager.SessionsChanged += OnSessionsChanged;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_initialized) return;
            _initialized = true;
        }

        _logger.LogInformation("Initializing runtime orchestration service");
        InitializeSeatRuntimes();
        StartTimers();
        await ReconcileAsync(cancellationToken);
        _logger.LogInformation("Runtime orchestration service initialized");
    }

    /// <inheritdoc />
    public async Task<RuntimeStatus> ReconcileAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting runtime reconciliation");

        var hardwareSnapshot = _hardwareService.CurrentSnapshot;
        var displaySnapshot = _displayService.CurrentSnapshot;
        var sessions = _sessionManager.Sessions;
        var seatConfigs = _seatConfigService.Configuration.Seats;

        var seatRuntimes = new List<SeatRuntime>();

        foreach (var seatConfig in seatConfigs.Where(s => s.IsEnabled))
        {
            Interlocked.Increment(ref _reconciliationDepth);
            try
            {
                var runtime = await ReconcileSeatAsync(seatConfig, hardwareSnapshot, displaySnapshot, sessions, cancellationToken);
                seatRuntimes.Add(runtime);
                _seatRuntimes[seatConfig.Id] = runtime;
            }
            finally
            {
                Interlocked.Decrement(ref _reconciliationDepth);
            }
        }

        var status = ComputeCurrentStatus(seatRuntimes.ToImmutableArray());

        lock (_lock)
        {
            _seatRuntimes.Clear();
            foreach (var rt in seatRuntimes)
            {
                _seatRuntimes[rt.Id] = rt;
            }
        }

        RaiseStatusChanged(status);
        return status;
    }

    /// <inheritdoc />
    public async Task<SeatStartResult> StartSeatAsync(SeatId seatId, CancellationToken cancellationToken = default)
    {
        var seatConfig = _seatConfigService.Configuration.Seats.FirstOrDefault(s => s.Id == seatId);
        if (seatConfig == null)
        {
            return SeatStartResult.FailureResult(seatId, "Seat configuration not found");
        }

        var validation = await _seatConfigService.ValidateSeatAsync(seatId, cancellationToken);
        if (!validation.IsValid)
        {
            var errorMsg = string.Join("; ", validation.Issues.Select(i => i.Message));
            return SeatStartResult.FailureResult(seatId, $"Seat validation failed: {errorMsg}");
        }

        var runtime = _seatRuntimes.TryGetValue(seatId, out var rt) ? rt : null;
        if (runtime?.State == SeatRuntimeState.Running)
        {
            return SeatStartResult.FailureResult(seatId, "Seat already running");
        }

        var appPath = seatConfig.Assignment.ApplicationPath;
        if (string.IsNullOrWhiteSpace(appPath))
        {
            return SeatStartResult.FailureResult(seatId, "No application configured for this seat");
        }

        if (!File.Exists(appPath))
        {
            return SeatStartResult.FailureResult(seatId, $"Application not found: {appPath}");
        }

        // Get the session ID for launching
        var sessionId = runtime?.ActiveSession?.SessionId ?? seatConfig.Assignment.SessionId ?? 1;

        var launchRequest = new SessionLaunchRequest(
            SessionId: sessionId,
            ExecutablePath: appPath,
            Arguments: null,
            WorkingDirectory: null,
            EnvironmentVariables: null,
            RunAsCurrentUser: true);

        var result = await _launcher.LaunchAsync(launchRequest, cancellationToken);

        if (result.Success && result.ProcessId.HasValue)
        {
            _runningProcesses[result.ProcessId.Value] = new ProcessRuntimeInfo
            {
                ProcessId = result.ProcessId.Value,
                SeatId = seatId,
                StartTime = DateTime.UtcNow,
                IsRunning = true
            };
            _ = SafeReconcileAsync();
        }

        return result.Success
            ? SeatStartResult.SuccessResult(seatId, SeatRuntimeState.Running)
            : SeatStartResult.FailureResult(seatId, result.ErrorMessage ?? "Launch failed");
    }

    /// <inheritdoc />
    public async Task<SeatStopResult> StopSeatAsync(SeatId seatId, CancellationToken cancellationToken = default)
    {
        var runtime = _seatRuntimes.TryGetValue(seatId, out var rt) ? rt : null;
        
        // Find the process for this seat
        var processEntry = _runningProcesses.FirstOrDefault(kvp => kvp.Value.SeatId == seatId && kvp.Value.IsRunning);
        if (processEntry.Value == null)
        {
            return SeatStopResult.FailureResult(seatId, "Seat not running");
        }

        try
        {
            var process = Process.GetProcessById(processEntry.Key);
            if (!process.HasExited)
            {
                process.Kill(true);
                await process.WaitForExitAsync(cancellationToken);
            }
            
            _runningProcesses.TryRemove(processEntry.Key, out _);
            _ = SafeReconcileAsync();
            
            return SeatStopResult.SuccessResult(seatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping seat {SeatId}", seatId);
            return SeatStopResult.FailureResult(seatId, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<SeatStartResult> RestartSeatAsync(SeatId seatId, CancellationToken cancellationToken = default)
    {
        await StopSeatAsync(seatId, cancellationToken);
        await Task.Delay(500, cancellationToken);
        return await StartSeatAsync(seatId, cancellationToken);
    }

    /// <inheritdoc />
    public SeatRuntime? GetSeatRuntime(SeatId seatId)
    {
        _seatRuntimes.TryGetValue(seatId, out var runtime);
        return runtime;
    }

    /// <inheritdoc />
    public ImmutableArray<SeatRuntime> GetAllSeatRuntimes()
    {
        return _seatRuntimes.Values.ToImmutableArray();
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping runtime orchestration service");
        
        _reconciliationTimer?.Dispose();
        _processMonitorTimer?.Dispose();

        // Stop all running seats
        foreach (var seatId in _seatRuntimes.Keys)
        {
            await StopSeatAsync(seatId, cancellationToken);
        }

        _seatConfigService.ConfigurationChanged -= OnConfigurationChanged;
        _seatConfigService.SeatValidationChanged -= OnSeatValidationChanged;
        _hardwareService.HardwareChanged -= OnHardwareChanged;
        _displayService.DisplayChanged -= OnDisplayChanged;
        _sessionManager.SessionEvent -= OnSessionEvent;
        _sessionManager.SessionsChanged -= OnSessionsChanged;

        _disposed = true;
        _logger.LogInformation("Runtime orchestration service stopped");
    }
private async Task<SeatRuntime> ReconcileSeatAsync(
        Seat seatConfig,
        HardwareSnapshot hardwareSnapshot,
        DisplaySnapshot displaySnapshot,
        IReadOnlyList<ManagedSession> sessions,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Reconciling seat {SeatId}", seatConfig.Id);

        var validation = await _seatConfigService.ValidateSeatAsync(seatConfig.Id, cancellationToken);
        var issues = validation.Issues.ToImmutableArray();

        var monitor = GetMonitorDevice(seatConfig, hardwareSnapshot);
        var keyboard = GetKeyboardDevice(seatConfig, hardwareSnapshot);
        var mouse = GetMouseDevice(seatConfig, hardwareSnapshot);
        var session = GetManagedSession(seatConfig, sessions);

        var runtimeState = DetermineSeatRuntimeState(seatConfig, issues);

        var previousRuntime = _seatRuntimes.TryGetValue(seatConfig.Id, out var prev) ? prev : null;
        var runtime = new SeatRuntime
        {
            Id = seatConfig.Id,
            Name = seatConfig.Name,
            State = runtimeState,
            IsEnabled = seatConfig.IsEnabled,
            ConfiguredMonitorId = seatConfig.Assignment.MonitorId,
            ActiveMonitor = monitor,
            ConfiguredKeyboardId = seatConfig.Assignment.KeyboardId,
            ActiveKeyboard = keyboard,
            ConfiguredMouseId = seatConfig.Assignment.MouseId,
            ActiveMouse = mouse,
            ConfiguredSessionId = seatConfig.Assignment.SessionId,
            ActiveSession = session,
            Issues = issues,
            LastReconciled = DateTime.UtcNow
        };

        if (previousRuntime?.State != runtime.State)
        {
            SeatStateChanged?.Invoke(this, new SeatRuntimeChangedEventArgs
            {
                SeatId = seatConfig.Id,
                PreviousState = previousRuntime?.State ?? SeatRuntimeState.Unknown,
                CurrentState = runtime.State,
                Timestamp = DateTime.UtcNow
            });
        }

        return runtime;
    }

    private MonitorDevice? GetMonitorDevice(Seat seatConfig, HardwareSnapshot hardwareSnapshot)
    {
        if (seatConfig.Assignment.MonitorId == null) return null;
        return hardwareSnapshot.Monitors.FirstOrDefault(m => m.DeviceInstanceId == seatConfig.Assignment.MonitorId);
    }

    private KeyboardDevice? GetKeyboardDevice(Seat seatConfig, HardwareSnapshot hardwareSnapshot)
    {
        if (seatConfig.Assignment.KeyboardId == null) return null;
        return hardwareSnapshot.Keyboards.FirstOrDefault(k => k.DeviceInstanceId == seatConfig.Assignment.KeyboardId);
    }

    private MouseDevice? GetMouseDevice(Seat seatConfig, HardwareSnapshot hardwareSnapshot)
    {
        if (seatConfig.Assignment.MouseId == null) return null;
        return hardwareSnapshot.Mice.FirstOrDefault(m => m.DeviceInstanceId == seatConfig.Assignment.MouseId);
    }

    private ManagedSession? GetManagedSession(Seat seatConfig, IReadOnlyList<ManagedSession> sessions)
    {
        if (seatConfig.Assignment.SessionId == null) return null;
        return sessions.FirstOrDefault(s => s.SessionId == seatConfig.Assignment.SessionId);
    }

    private SeatRuntimeState DetermineSeatRuntimeState(Seat seatConfig, ImmutableArray<SeatValidationIssue> issues)
    {
        if (!seatConfig.IsEnabled) return SeatRuntimeState.Stopped;

        var hasErrors = issues.Any(i => i.Type == ValidationIssueType.Conflict || i.Type == ValidationIssueType.Invalid);
        var hasWarnings = issues.Any(i => i.Type == ValidationIssueType.DeviceUnavailable || i.Type == ValidationIssueType.SessionUnavailable || i.Type == ValidationIssueType.MissingRequired);

        if (hasErrors) return SeatRuntimeState.Error;
        if (hasWarnings) return SeatRuntimeState.Degraded;

        var assignment = seatConfig.Assignment;
        var isFullyConfigured = assignment.IsComplete;

        if (!isFullyConfigured) return SeatRuntimeState.PartiallyReady;

        var hasRunningProcess = _runningProcesses.Values.Any(p => p.SeatId == seatConfig.Id && p.IsRunning);

        if (hasRunningProcess) return SeatRuntimeState.Running;

        return SeatRuntimeState.Ready;
    }

    private void InitializeSeatRuntimes()
    {
        var seatConfigs = _seatConfigService.Configuration.Seats;
        foreach (var seatConfig in seatConfigs.Where(s => s.IsEnabled))
        {
            var runtime = new SeatRuntime
            {
                Id = seatConfig.Id,
                Name = seatConfig.Name,
                State = SeatRuntimeState.Initializing,
                IsEnabled = seatConfig.IsEnabled,
                Issues = ImmutableArray<SeatValidationIssue>.Empty,
                LastReconciled = DateTime.UtcNow
            };
            _seatRuntimes[seatConfig.Id] = runtime;
        }
    }
private void StartTimers()
    {
        var settings = _settingsMonitor.CurrentValue;
        _reconciliationTimer = new Timer(async _ => await SafeReconcileAsync(), null, TimeSpan.FromSeconds(settings.ReconciliationIntervalSeconds), TimeSpan.FromSeconds(settings.ReconciliationIntervalSeconds));
        _processMonitorTimer = new Timer(MonitorProcesses, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    private async Task SafeReconcileAsync()
    {
        try
        {
            await ReconcileAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during runtime reconciliation");
        }
    }

    private void MonitorProcesses(object? state)
    {
        foreach (var kvp in _runningProcesses)
        {
            var processInfo = kvp.Value;
            try
            {
                var process = Process.GetProcessById(kvp.Key);
                if (process.HasExited)
                {
                    processInfo.IsRunning = false;
                    processInfo.ExitTime = DateTime.UtcNow;
                    _logger.LogInformation("Process {ProcessId} for seat {SeatId} exited", kvp.Key, processInfo.SeatId);
                    _ = SafeReconcileAsync();
                }
                else
                {
                    processInfo.IsRunning = true;
                }
            }
            catch (ArgumentException)
            {
                processInfo.IsRunning = false;
                processInfo.ExitTime = DateTime.UtcNow;
                _runningProcesses.TryRemove(kvp.Key, out _);
                _ = SafeReconcileAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error monitoring process {ProcessId}", kvp.Key);
            }
        }
    }

    private void OnConfigurationChanged(object? sender, SeatConfigurationChangedEventArgs e)
    {
        _logger.LogDebug("Configuration changed, triggering reconciliation");
        _ = SafeReconcileAsync();
    }

    private void OnSeatValidationChanged(object? sender, SeatValidationChangedEventArgs e)
    {
        if (Volatile.Read(ref _reconciliationDepth) > 0)
        {
            _logger.LogDebug("Skipping reconciliation for validation event raised during reconciliation of seat {SeatId}", e.SeatId);
            return;
        }

        _logger.LogDebug("Seat validation changed for seat {SeatId}", e.SeatId);
        _ = SafeReconcileAsync();
    }

    private void OnHardwareChanged(object? sender, HardwareChangedEventArgs e)
    {
        _logger.LogDebug("Hardware changed, triggering reconciliation");
        _ = SafeReconcileAsync();
    }

    private void OnDisplayChanged(object? sender, DisplayChangedEventArgs e)
    {
        _logger.LogDebug("Display changed, triggering reconciliation");
        _ = SafeReconcileAsync();
    }

    private void OnSessionEvent(object? sender, SessionEventArgs e)
    {
        _logger.LogDebug("Session event: {EventType} for session {SessionId}", e.EventType, e.SessionId);
        _ = SafeReconcileAsync();
    }

    private void OnSessionsChanged(object? sender, SessionCollectionChangedEventArgs e)
    {
        _logger.LogDebug("Sessions changed, triggering reconciliation");
        _ = SafeReconcileAsync();
    }

    private RuntimeStatus ComputeCurrentStatus(ImmutableArray<SeatRuntime> seatRuntimes)
    {
        var totalSeats = seatRuntimes.Length;
        var runningSeats = seatRuntimes.Count(s => s.State == SeatRuntimeState.Running);
        var readySeats = seatRuntimes.Count(s => s.State == SeatRuntimeState.Ready);
        var degradedSeats = seatRuntimes.Count(s => s.State == SeatRuntimeState.Degraded);
        var errorSeats = seatRuntimes.Count(s => s.State == SeatRuntimeState.Error);
        var stoppedSeats = seatRuntimes.Count(s => s.State == SeatRuntimeState.Stopped);
        var initializingSeats = seatRuntimes.Count(s => s.State == SeatRuntimeState.Initializing);
        var partiallyReadySeats = seatRuntimes.Count(s => s.State == SeatRuntimeState.PartiallyReady);

        var overallState = errorSeats > 0 ? RuntimeSystemState.Error :
            degradedSeats > 0 ? RuntimeSystemState.Degraded :
            runningSeats > 0 ? RuntimeSystemState.Ready :
            readySeats > 0 ? RuntimeSystemState.Ready :
            stoppedSeats > 0 ? RuntimeSystemState.Stopped :
            initializingSeats > 0 ? RuntimeSystemState.Initializing :
            RuntimeSystemState.Unknown;

        return new RuntimeStatus
        {
            SystemState = overallState,
            Seats = seatRuntimes,
            Timestamp = DateTime.UtcNow
        };
    }

    private RuntimeStatus ComputeCurrentStatus()
    {
        return ComputeCurrentStatus(_seatRuntimes.Values.ToImmutableArray());
    }

    private void RaiseStatusChanged(RuntimeStatus status)
    {
        StatusChanged?.Invoke(this, new RuntimeStatusChangedEventArgs
        {
            CurrentStatus = status,
            Timestamp = DateTime.UtcNow
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _reconciliationTimer?.Dispose();
        _processMonitorTimer?.Dispose();

        _seatConfigService.ConfigurationChanged -= OnConfigurationChanged;
        _seatConfigService.SeatValidationChanged -= OnSeatValidationChanged;
        _hardwareService.HardwareChanged -= OnHardwareChanged;
        _displayService.DisplayChanged -= OnDisplayChanged;
        _sessionManager.SessionEvent -= OnSessionEvent;
        _sessionManager.SessionsChanged -= OnSessionsChanged;

        foreach (var kvp in _runningProcesses)
        {
            try
            {
                var process = Process.GetProcessById(kvp.Key);
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreeSeat.Core;
using FreeSeat.Core.Runtime;
using FreeSeat.Core.Seats.Interfaces;
using FreeSeat.Core.Seats.Models;
using FreeSeat.Core.Sessions.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Collections.Immutable;
using FreeSeat.Core.Hardware.Models;
using System.Windows;

namespace FreeSeat.App.ViewModels;

/// <summary>
/// View model for the Dashboard page with real runtime integration.
/// </summary>
public sealed partial class DashboardViewModel : ViewModelBase
{
    private readonly IRuntimeOrchestrationService _runtimeService;
    private readonly ISeatConfigurationService _seatService;
    private readonly ILogger<DashboardViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<SeatRuntimeViewModel> _seatRuntimes = new();

    [ObservableProperty]
    private RuntimeSystemState _systemState = RuntimeSystemState.Unknown;

    [ObservableProperty]
    private string _systemStatusSummary = "Unknown";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _lastReconciledText = "Never";

    [ObservableProperty]
    private int _totalSeats;

    [ObservableProperty]
    private int _runningSeats;

    [ObservableProperty]
    private int _readySeats;

    [ObservableProperty]
    private int _stoppedSeats;

    [ObservableProperty]
    private int _errorSeats;

    public DashboardViewModel(
        IRuntimeOrchestrationService runtimeService,
        ISeatConfigurationService seatService,
        ILogger<DashboardViewModel> logger)
    {
        _runtimeService = runtimeService ?? throw new ArgumentNullException(nameof(runtimeService));
        _seatService = seatService ?? throw new ArgumentNullException(nameof(seatService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Title = "Dashboard";

        _runtimeService.StatusChanged += OnStatusChanged;
        _runtimeService.SeatStateChanged += OnSeatStateChanged;

        UpdateFromRuntimeStatus(_runtimeService.CurrentStatus);

        
    }

    private async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            await _runtimeService.InitializeAsync();
            UpdateFromRuntimeStatus(_runtimeService.CurrentStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize runtime service");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnStatusChanged(object? sender, RuntimeStatusChangedEventArgs e)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            UpdateFromRuntimeStatus(e.CurrentStatus);
        });
    }

    private void OnSeatStateChanged(object? sender, SeatRuntimeChangedEventArgs e)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            var seatVm = SeatRuntimes.FirstOrDefault(s => s.Id == e.SeatId);
            if (seatVm != null)
            {
                seatVm.UpdateFromRuntime(e.CurrentState);
            }
            UpdateSummaryCounts();
        });
    }

    private void UpdateFromRuntimeStatus(RuntimeStatus status)
    {
        SystemState = status.SystemState;
        SystemStatusSummary = status.Summary;
        
        LastReconciledText = status.Timestamp == DateTime.MinValue 
            ? "Never" 
            : $"Last reconciled: {status.Timestamp:yyyy-MM-dd HH:mm:ss}";

        foreach (var seatRuntime in status.Seats)
        {
            var existing = SeatRuntimes.FirstOrDefault(s => s.Id == seatRuntime.Id);
            if (existing != null)
            {
                existing.UpdateFromRuntime(seatRuntime);
            }
            else
            {
                SeatRuntimes.Add(new SeatRuntimeViewModel(seatRuntime));
            }
        }

        var toRemove = SeatRuntimes.Where(s => !status.Seats.Any(rs => rs.Id == s.Id)).ToList();
        foreach (var seat in toRemove)
        {
            SeatRuntimes.Remove(seat);
        }

        UpdateSummaryCounts();
    }

    private void UpdateSummaryCounts()
    {
        TotalSeats = SeatRuntimes.Count;
        RunningSeats = SeatRuntimes.Count(s => s.State == SeatRuntimeState.Running);
        ReadySeats = SeatRuntimes.Count(s => s.State == SeatRuntimeState.Ready);
        StoppedSeats = SeatRuntimes.Count(s => s.State == SeatRuntimeState.Stopped);
        ErrorSeats = SeatRuntimes.Count(s => s.State == SeatRuntimeState.Error);
    }

    [RelayCommand]
    private async Task ReconcileAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            await _runtimeService.ReconcileAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reconciliation failed");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public override void Dispose()
    {
        _runtimeService.StatusChanged -= OnStatusChanged;
        _runtimeService.SeatStateChanged -= OnSeatStateChanged;
        base.Dispose();
    }
}

/// <summary>
/// View model for a seat's runtime state on the dashboard.
/// </summary>
public sealed partial class SeatRuntimeViewModel : ObservableObject
{
    public SeatId Id { get; }
    public string Name { get; }
    public SeatRuntimeState State { get; private set; }
    public bool IsEnabled { get; }
    
    public string? ConfiguredMonitorId { get; private set; }
    public MonitorDevice? ActiveMonitor { get; private set; }
    public string? ConfiguredKeyboardId { get; private set; }
    public KeyboardDevice? ActiveKeyboard { get; private set; }
    public string? ConfiguredMouseId { get; private set; }
    public MouseDevice? ActiveMouse { get; private set; }
    public int? ConfiguredSessionId { get; private set; }
    public ManagedSession? ActiveSession { get; private set; }
    
    public ImmutableArray<SeatValidationIssue> Issues { get; private set; }
    public DateTime LastReconciled { get; private set; }
    public bool HasTrueIsolation { get; private set; }

    public string StateDisplay => GetStateDisplay(State);
    public string StateColor => GetStateColor(State);
    public string IsolationStatus => HasTrueIsolation ? "Enforced" : "Not Enforced";
    public string IsolationColor => HasTrueIsolation ? "#4CAF50" : "#F44336";
    
    public bool CanStart => State is SeatRuntimeState.Ready or SeatRuntimeState.PartiallyReady or SeatRuntimeState.Degraded or SeatRuntimeState.Stopped;
    public bool CanStop => State == SeatRuntimeState.Running;
    public bool CanRestart => State == SeatRuntimeState.Running;

    public string? MonitorDisplay => ActiveMonitor?.FriendlyName ?? ConfiguredMonitorId ?? "Not assigned";
    public string? KeyboardDisplay => ActiveKeyboard?.FriendlyName ?? ConfiguredKeyboardId ?? "Not assigned";
    public string? MouseDisplay => ActiveMouse?.FriendlyName ?? ConfiguredMouseId ?? "Not assigned";
    public string? SessionDisplay => ActiveSession?.DisplayName ?? (ConfiguredSessionId.HasValue ? $"Session {ConfiguredSessionId.Value}" : "Not assigned");
    public int? ProcessId { get; private set; }
    public bool IsRunning => State == SeatRuntimeState.Running;

    public SeatRuntimeViewModel(SeatRuntime runtime)
    {
        Id = runtime.Id;
        Name = runtime.Name;
        State = runtime.State;
        IsEnabled = runtime.IsEnabled;
        ConfiguredMonitorId = runtime.ConfiguredMonitorId;
        ActiveMonitor = runtime.ActiveMonitor;
        ConfiguredKeyboardId = runtime.ConfiguredKeyboardId;
        ActiveKeyboard = runtime.ActiveKeyboard;
        ConfiguredMouseId = runtime.ConfiguredMouseId;
        ActiveMouse = runtime.ActiveMouse;
        ConfiguredSessionId = runtime.ConfiguredSessionId;
        ActiveSession = runtime.ActiveSession;
        Issues = runtime.Issues;
        LastReconciled = runtime.LastReconciled;
        HasTrueIsolation = runtime.HasTrueIsolation;
    }

    public void UpdateFromRuntime(SeatRuntimeState newState)
    {
        State = newState;
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(StateDisplay));
        OnPropertyChanged(nameof(StateColor));
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanStop));
        OnPropertyChanged(nameof(CanRestart));
        OnPropertyChanged(nameof(IsRunning));
    }

    public void UpdateFromRuntime(SeatRuntime runtime)
    {
        State = runtime.State;
        ActiveMonitor = runtime.ActiveMonitor;
        ActiveKeyboard = runtime.ActiveKeyboard;
        ActiveMouse = runtime.ActiveMouse;
        ActiveSession = runtime.ActiveSession;
        Issues = runtime.Issues;
        LastReconciled = runtime.LastReconciled;
        HasTrueIsolation = runtime.HasTrueIsolation;

        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(StateDisplay));
        OnPropertyChanged(nameof(StateColor));
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanStop));
        OnPropertyChanged(nameof(CanRestart));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(MonitorDisplay));
        OnPropertyChanged(nameof(KeyboardDisplay));
        OnPropertyChanged(nameof(MouseDisplay));
        OnPropertyChanged(nameof(SessionDisplay));
        OnPropertyChanged(nameof(IsolationStatus));
        OnPropertyChanged(nameof(IsolationColor));
    }

    private static string GetStateDisplay(SeatRuntimeState state)
    {
        return state switch
        {
            SeatRuntimeState.Ready => "Ready",
            SeatRuntimeState.PartiallyReady => "Partially Ready",
            SeatRuntimeState.Degraded => "Degraded",
            SeatRuntimeState.Unavailable => "Unavailable",
            SeatRuntimeState.Error => "Error",
            SeatRuntimeState.Initializing => "Initializing...",
            SeatRuntimeState.Starting => "Starting...",
            SeatRuntimeState.Running => "Running",
            SeatRuntimeState.Stopping => "Stopping...",
            SeatRuntimeState.Stopped => "Stopped",
            _ => "Unknown"
        };
    }

    private static string GetStateColor(SeatRuntimeState state)
    {
        return state switch
        {
            SeatRuntimeState.Ready => "#4CAF50",
            SeatRuntimeState.PartiallyReady => "#FF9800",
            SeatRuntimeState.Degraded => "#FF9800",
            SeatRuntimeState.Unavailable => "#F44336",
            SeatRuntimeState.Error => "#F44336",
            SeatRuntimeState.Initializing => "#2196F3",
            SeatRuntimeState.Starting => "#2196F3",
            SeatRuntimeState.Running => "#4CAF50",
            SeatRuntimeState.Stopping => "#FF9800",
            SeatRuntimeState.Stopped => "#9E9E9E",
            _ => "#9E9E9E"
        };
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreeSeat.App.ViewModels;
using FreeSeat.Core.Sessions.Interfaces;
using FreeSeat.Core.Sessions.Models;
using FreeSeat.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace FreeSeat.App.ViewModels;

public sealed partial class SessionViewModel : ViewModelBase
{
    private readonly ISessionManager _sessionManager;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SessionViewModel> _logger;

    [ObservableProperty] private int _currentSessionId = -1;
    [ObservableProperty] private ManagedSession? _currentSession;
    [ObservableProperty] private ObservableCollection<ManagedSession> _sessions = new();
    [ObservableProperty] private ObservableCollection<SessionEventArgs> _sessionEvents = new();
    [ObservableProperty] private bool _isMonitoring;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private bool _developerMode;
    [ObservableProperty] private int _selectedSessionId = -1;
    [ObservableProperty] private string _launchStatus = "Ready";

    public SessionViewModel(ISessionManager sessionManager, ISettingsService settingsService, ILogger<SessionViewModel> logger)
    {
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        CurrentSessionId = _sessionManager.CurrentSessionId;
        CurrentSession = _sessionManager.CurrentSession;
        DeveloperMode = _settingsService.Settings.SessionManager.DeveloperMode;
        _sessionManager.SessionEvent += OnSessionEvent;
        _sessionManager.SessionsChanged += OnSessionsChanged;
    }

    [RelayCommand] private async Task RefreshAsync()
    {
        if (IsLoading) return;
        IsLoading = true; StatusMessage = "Refreshing...";
        try { await _sessionManager.ReconcileAsync(); Sessions.Clear(); foreach (var s in _sessionManager.Sessions) Sessions.Add(s); SessionEvents.Clear(); foreach (var e in _sessionManager.RecentEvents.Take(50)) SessionEvents.Add(e); CurrentSessionId = _sessionManager.CurrentSessionId; CurrentSession = _sessionManager.CurrentSession; IsMonitoring = _sessionManager.IsMonitoring; StatusMessage = $"Found {Sessions.Count} session(s), {SessionEvents.Count} event(s)"; }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; _logger.LogError(ex, "Error refreshing sessions"); }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task ToggleMonitoringAsync()
    {
        try { if (IsMonitoring) { await _sessionManager.StopMonitoringAsync(); IsMonitoring = false; StatusMessage = "Monitoring stopped"; } else { await _sessionManager.StartMonitoringAsync(); IsMonitoring = true; StatusMessage = "Monitoring started"; } }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; _logger.LogError(ex, "Error toggling monitoring"); }
    }

    [RelayCommand] private async Task ReconcileAsync()
    {
        try { StatusMessage = "Reconciling..."; await _sessionManager.ReconcileAsync(); await RefreshAsync(); StatusMessage = "Reconciliation complete"; }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; _logger.LogError(ex, "Error during reconciliation"); }
    }

    [RelayCommand] private async Task TestLaunchAsync()
    {
        if (!DeveloperMode) { LaunchStatus = "Developer Mode is disabled"; return; }
        if (SelectedSessionId <= 0) { LaunchStatus = "Please select a target session"; return; }
        try { LaunchStatus = "Launching notepad.exe..."; var r = SessionLaunchRequest.CreateTestLaunch(SelectedSessionId); var result = await _sessionManager.LaunchProcessAsync(r); LaunchStatus = result.Success ? $"Success! Launched PID {result.ProcessId} in session {result.SessionId}" : $"Failed: {result.ErrorMessage} (Error: {result.ErrorCode})"; }
        catch (Exception ex) { LaunchStatus = $"Error: {ex.Message}"; _logger.LogError(ex, "Error during test launch"); }
    }

    partial void OnDeveloperModeChanged(bool value)
    {
        _settingsService.Settings.SessionManager.DeveloperMode = value;
        _ = SaveDeveloperModeAsync();
    }

    [RelayCommand] private void ToggleDeveloperMode()
    {
        DeveloperMode = !DeveloperMode;
        StatusMessage = DeveloperMode ? "Developer Mode enabled" : "Developer Mode disabled";
    }

    private async Task SaveDeveloperModeAsync()
    {
        try
        {
            await _settingsService.SaveAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Developer Mode setting");
            StatusMessage = "Could not save Developer Mode setting.";
        }
    }

    private void OnSessionEvent(object? sender, SessionEventArgs evt) => System.Windows.Application.Current?.Dispatcher.Invoke(() => { SessionEvents.Insert(0, evt); while (SessionEvents.Count > 100) SessionEvents.RemoveAt(SessionEvents.Count - 1); });
    private void OnSessionsChanged(object? sender, SessionCollectionChangedEventArgs evt) => System.Windows.Application.Current?.Dispatcher.Invoke(() => { Sessions.Clear(); foreach (var s in _sessionManager.Sessions) Sessions.Add(s); CurrentSession = _sessionManager.CurrentSession; });

    public override void Dispose() { _sessionManager.SessionEvent -= OnSessionEvent; _sessionManager.SessionsChanged -= OnSessionsChanged; base.Dispose(); }
}

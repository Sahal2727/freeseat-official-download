using FreeSeat.Core.Sessions.Interfaces;
using FreeSeat.Core.Sessions.Models;
using FreeSeat.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;

namespace FreeSeat.Infrastructure.Sessions;

/// <summary>
/// Main session manager implementation.
/// Coordinates session monitoring, tracking, recovery, and process launching.
/// </summary>
internal sealed class SessionManager : ISessionManager, IDisposable
{
    private readonly ILogger<SessionManager> _logger;
    private readonly ISessionService _sessionService;
    private readonly SessionStateTracker _stateTracker;
    private readonly SessionLauncher _launcher;
    private readonly SessionRecoveryService _recoveryService;
    private readonly SessionManagerSettings _settings;
    private readonly object _lock = new();
    private bool _disposed;
    private bool _monitoringStarted;

    public int CurrentSessionId => _sessionService.CurrentSessionId;
    public ManagedSession? CurrentSession => _stateTracker.GetSession(CurrentSessionId);
    public IReadOnlyList<ManagedSession> Sessions => _stateTracker.Sessions;
    public bool IsMonitoring => _monitoringStarted;
    public IReadOnlyList<SessionEventArgs> RecentEvents => _stateTracker.RecentEvents;

    public event EventHandler<SessionEventArgs>? SessionEvent;
    public event EventHandler<SessionCollectionChangedEventArgs>? SessionsChanged;

    public SessionManager(
        ILogger<SessionManager> logger,
        ISessionService sessionService,
        SessionStateTracker stateTracker,
        SessionLauncher launcher,
        SessionRecoveryService recoveryService,
        SessionManagerSettings settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _stateTracker = stateTracker ?? throw new ArgumentNullException(nameof(stateTracker));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _recoveryService = recoveryService ?? throw new ArgumentNullException(nameof(recoveryService));
        _settings = settings ?? new SessionManagerSettings();

        // Subscribe to session service events
        _sessionService.SessionChanged += OnSessionServiceEvent;
    }

    public ManagedSession? GetSession(int sessionId)
    {
        return _stateTracker.GetSession(sessionId);
    }

    public SessionLifecycleState GetSessionState(int sessionId)
    {
        return _stateTracker.GetSessionState(sessionId);
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_monitoringStarted || _disposed) return;
            _monitoringStarted = true;
        }

        _logger.LogInformation("Starting session monitoring");

        // Initial enumeration
        await RefreshSessionsAsync(cancellationToken);

        // Start WTS notifications
        _sessionService.StartMonitoring();

        // Start reconciliation service
        if (_settings.MonitoringEnabled)
        {
            _recoveryService.Start();
        }

        _logger.LogInformation("Session monitoring started");
    }

    public Task StopMonitoringAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_monitoringStarted || _disposed) return Task.CompletedTask;
            _monitoringStarted = false;
        }

        _logger.LogInformation("Stopping session monitoring");

        _sessionService.StopMonitoring();
        _recoveryService.Stop();

        _logger.LogInformation("Session monitoring stopped");
        return Task.CompletedTask;
    }

    public async Task ReconcileAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Manual reconciliation requested");
        await _recoveryService.ReconcileAsync(cancellationToken);
    }

    public async Task<bool> CanLaunchProcessAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        var session = _stateTracker.GetSession(sessionId);
        if (session == null)
        {
            // Try to get session info from service
            var info = await _sessionService.GetSessionAsync(sessionId, cancellationToken);
            if (info == null) return false;
            return info.State.ToLifecycleState().CanLaunchProcess();
        }
        return session.CanLaunchProcess;
    }

    public async Task<SessionLaunchResult> LaunchProcessAsync(SessionLaunchRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Launch requested for session {SessionId}: {Executable}", request.SessionId, request.ExecutablePath);

        // Validate session exists and can launch
        var canLaunch = await CanLaunchProcessAsync(request.SessionId, cancellationToken);
        if (!canLaunch)
        {
            var error = $"Cannot launch in session {request.SessionId}: session not found or not in launchable state";
            _logger.LogWarning(error);
            var failResult = SessionLaunchResult.FailureResult(request.SessionId, 0, error);
            RaiseSessionEvent(SessionEventArgs.ProcessLaunchFailed(request.SessionId, Path.GetFileName(request.ExecutablePath), error));
            return failResult;
        }

        // Perform launch
        var result = await _launcher.LaunchAsync(request, cancellationToken);

        // Raise event
        if (result.Success)
        {
            RaiseSessionEvent(SessionEventArgs.ProcessLaunched(request.SessionId, result.ProcessId!.Value, Path.GetFileName(request.ExecutablePath)));
        }
        else
        {
            RaiseSessionEvent(SessionEventArgs.ProcessLaunchFailed(request.SessionId, Path.GetFileName(request.ExecutablePath), result.ErrorMessage ?? "Unknown error"));
        }

        return result;
    }

    private async Task RefreshSessionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var sessions = await _sessionService.EnumerateSessionsAsync(cancellationToken);
            var currentSessionId = _sessionService.CurrentSessionId;

            foreach (var session in sessions)
            {
                var isCurrent = session.SessionId == currentSessionId;
                var diff = _stateTracker.UpdateSession(session, isCurrent);
                RaiseSessionsChanged(diff);
            }

            _stateTracker.SetCurrentSession(currentSessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh sessions");
        }
    }

    private void OnSessionServiceEvent(object? sender, SessionChangedEventArgs e)
    {
        if (_disposed) return;

        try
        {
            // Get updated session info
            var sessionInfo = _sessionService.GetSessionAsync(e.SessionId).GetAwaiter().GetResult();
            
            if (sessionInfo != null)
            {
                var isCurrent = sessionInfo.SessionId == CurrentSessionId;
                var diff = _stateTracker.UpdateSession(sessionInfo, isCurrent);
                RaiseSessionsChanged(diff);
            }
            else
            {
                // Session might have been removed
                var diff = _stateTracker.RemoveSession(e.SessionId);
                if (diff.Removed.Count > 0)
                {
                    RaiseSessionsChanged(diff);
                }
            }

            // Also raise the raw session event
            var lifecycleState = e.Reason.GetExpectedLifecycleState() ?? SessionLifecycleState.Unknown;
            SessionEventArgs? sessionEvent = e.Reason switch
            {
                SessionChangeReason.SessionLogon => SessionEventArgs.SessionConnected(e.SessionId, lifecycleState, sessionInfo),
                SessionChangeReason.SessionLogoff => SessionEventArgs.SessionLoggedOff(e.SessionId, lifecycleState, sessionInfo),
                SessionChangeReason.SessionLock => SessionEventArgs.SessionLocked(e.SessionId, lifecycleState, sessionInfo),
                SessionChangeReason.SessionUnlock => SessionEventArgs.SessionUnlocked(e.SessionId, lifecycleState, sessionInfo),
                SessionChangeReason.SessionRemoteDisconnect => SessionEventArgs.SessionDisconnected(e.SessionId, lifecycleState, sessionInfo),
                _ => SessionEventArgs.SessionStateChanged(e.SessionId, lifecycleState, lifecycleState, sessionInfo)
            };

            if (sessionEvent != null)
            {
                RaiseSessionEvent(sessionEvent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing session change event for session {SessionId}", e.SessionId);
        }
    }

    private void RaiseSessionEvent(SessionEventArgs evt)
    {
        SessionEvent?.Invoke(this, evt);
    }

    private void RaiseSessionsChanged(SessionCollectionChangedEventArgs evt)
    {
        if (evt.Added.Count > 0 || evt.Removed.Count > 0 || evt.Changed.Count > 0)
        {
            SessionsChanged?.Invoke(this, evt);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        StopMonitoringAsync().GetAwaiter().GetResult();
        _sessionService.SessionChanged -= OnSessionServiceEvent;
        _stateTracker.Dispose();
        _launcher.Dispose();
        _recoveryService.Dispose();
    }
}

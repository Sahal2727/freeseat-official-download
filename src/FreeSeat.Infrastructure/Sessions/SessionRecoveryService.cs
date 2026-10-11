using FreeSeat.Core.Sessions.Interfaces;
using FreeSeat.Core.Sessions.Models;
using Microsoft.Extensions.Logging;

namespace FreeSeat.Infrastructure.Sessions;

/// <summary>
/// Provides recovery mechanisms for session management.
/// Handles missed notifications and state synchronization.
/// </summary>
internal sealed class SessionRecoveryService : IDisposable
{
    private readonly ILogger<SessionRecoveryService> _logger;
    private readonly ISessionService _sessionService;
    private readonly SessionStateTracker _stateTracker;
    private readonly Timer? _reconciliationTimer;
    private readonly int _reconciliationIntervalSeconds;
    private bool _disposed;
    private bool _started;

    public SessionRecoveryService(
        ILogger<SessionRecoveryService> logger,
        ISessionService sessionService,
        SessionStateTracker stateTracker,
        int reconciliationIntervalSeconds = 300)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _stateTracker = stateTracker ?? throw new ArgumentNullException(nameof(stateTracker));
        _reconciliationIntervalSeconds = Math.Max(60, reconciliationIntervalSeconds); // Minimum 1 minute

        _reconciliationTimer = new Timer(OnReconciliationTimer, null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// Starts the reconciliation timer.
    /// </summary>
    public void Start()
    {
        if (_started || _disposed) return;
        _started = true;
        
        var dueTime = TimeSpan.FromSeconds(_reconciliationIntervalSeconds);
        var period = TimeSpan.FromSeconds(_reconciliationIntervalSeconds);
        _reconciliationTimer?.Change(dueTime, period);
        
        _logger.LogInformation("Session recovery service started with {Interval}s reconciliation interval", _reconciliationIntervalSeconds);
    }

    /// <summary>
    /// Stops the reconciliation timer.
    /// </summary>
    public void Stop()
    {
        if (!_started || _disposed) return;
        _started = false;
        _reconciliationTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _logger.LogInformation("Session recovery service stopped");
    }

    /// <summary>
    /// Forces an immediate reconciliation.
    /// </summary>
    public async Task<SessionSnapshotDiff> ReconcileAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Manual reconciliation triggered");
        return await PerformReconciliationAsync(cancellationToken);
    }

    private async void OnReconciliationTimer(object? state)
    {
        if (_disposed) return;
        
        try
        {
            await PerformReconciliationAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled reconciliation");
        }
    }

    private async Task<SessionSnapshotDiff> PerformReconciliationAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get actual sessions from Windows
            var actualSessions = await _sessionService.EnumerateSessionsAsync(cancellationToken);
            var currentSessionId = _sessionService.CurrentSessionId;

            // Convert to ManagedSession for comparison
            var actualManaged = actualSessions.Select(s => ManagedSession.FromSessionInfo(s, s.SessionId == currentSessionId)).ToList();

            // Reconcile
            var diff = _stateTracker.Reconcile(actualManaged, currentSessionId);

            if (diff.HasChanges)
            {
                _logger.LogInformation("Reconciliation completed: {Added} added, {Removed} removed, {Changed} changed",
                    diff.Added.Count, diff.Removed.Count, diff.Changed.Count);
            }
            else
            {
                _logger.LogDebug("Reconciliation completed: no changes");
            }

            // Update current session tracking
            _stateTracker.SetCurrentSession(currentSessionId);

            return diff;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reconciliation failed");
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _reconciliationTimer?.Dispose();
    }
}

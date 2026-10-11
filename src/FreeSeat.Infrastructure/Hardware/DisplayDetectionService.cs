using System.Collections.Immutable;
using System.Management;
using FreeSeat.Core.Hardware.Interfaces;
using FreeSeat.Core.Hardware.Models;
using Microsoft.Extensions.Logging;

namespace FreeSeat.Infrastructure.Hardware;

internal sealed class DisplayDetectionService : IDisplayDetectionService, IDisposable
{
    private readonly ILogger _logger;
    private DisplaySnapshot _currentSnapshot = new();
    private readonly object _lock = new();
    private Timer? _monitorTimer;
    private bool _disposed;
    private readonly int _pollIntervalMs = 5000;

    public DisplaySnapshot CurrentSnapshot
    {
        get
        {
            lock (_lock)
            {
                return _currentSnapshot;
            }
        }
    }

    public event EventHandler<DisplayChangedEventArgs>? DisplayChanged;

    public DisplayDetectionService(ILogger<DisplayDetectionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing display detection service...");
        DetectDisplaysInternal(cancellationToken);
        StartMonitoring();
        _logger.LogInformation("Display detection service initialized");
        return Task.CompletedTask;
    }

    public Task<DisplaySnapshot> DetectDisplaysAsync(CancellationToken cancellationToken = default)
    {
        DetectDisplaysInternal(cancellationToken);
        lock (_lock)
        {
            return Task.FromResult(_currentSnapshot);
        }
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        DetectDisplaysInternal(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        StopMonitoring();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopMonitoring();
    }

    private void StopMonitoring()
    {
        _monitorTimer?.Dispose();
        _monitorTimer = null;
    }

    private void DetectDisplaysInternal(CancellationToken cancellationToken = default)
    {
        // Basic implementation - in production this would use WMI/SetupAPI
        var displays = new List<MonitorDevice>();
        
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DesktopMonitor");
            foreach (ManagementObject obj in searcher.Get())
            {
                var deviceId = obj["DeviceID"]?.ToString() ?? string.Empty;
                var name = obj["Name"]?.ToString() ?? "Unknown Monitor";
                
                if (!string.IsNullOrEmpty(deviceId))
                {
                    displays.Add(new MonitorDevice
                    {
                        Id = deviceId,
                        DeviceInstanceId = deviceId,
                        FriendlyName = name,
                        IsPrimary = obj["Availability"]?.ToString() == "3", // Running/Full Power
                        ConnectionStatus = ConnectionStatus.Connected
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enumerate displays via WMI");
        }

        var snapshot = new DisplaySnapshot
        {
            Timestamp = DateTime.UtcNow,
            Monitors = displays.ToImmutableArray()
        };

        lock (_lock)
        {
            var previous = _currentSnapshot;
            _currentSnapshot = snapshot;
            
            // Fire event if changed
            if (!DisplaysEqual(previous, snapshot))
            {
                DisplayChanged?.Invoke(this, new DisplayChangedEventArgs
                {
                    MonitorId = "Configuration",
                    ChangeType = DisplayChangeType.TopologyChanged,
                    NewMonitor = null,
                    OldMonitor = null,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }

    private void StartMonitoring()
    {
        _monitorTimer = new Timer(_ => DetectDisplaysInternal(), null, TimeSpan.FromMilliseconds(_pollIntervalMs), TimeSpan.FromMilliseconds(_pollIntervalMs));
    }

    private static bool DisplaysEqual(DisplaySnapshot a, DisplaySnapshot b)
    {
        if (a.Monitors.Count != b.Monitors.Count) return false;
        for (int i = 0; i < a.Monitors.Count; i++)
        {
            if (a.Monitors[i].DeviceInstanceId != b.Monitors[i].DeviceInstanceId) return false;
        }
        return true;
    }
}

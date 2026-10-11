using FreeSeat.Core.Hardware.Interfaces;
using FreeSeat.Core.Hardware.Models;
using Microsoft.Extensions.Logging;

namespace FreeSeat.Infrastructure.Hardware;

internal sealed class HardwareDetectionService : IHardwareDetectionService
{
    private readonly MonitorDetector _monitorDetector;
    private readonly HidDeviceDetector _hidDetector;
    private readonly GpuDetector _gpuDetector;
    private readonly CpuDetector _cpuDetector;
    private readonly MemoryDetector _memoryDetector;
    private readonly StorageDetector _storageDetector;
    private readonly ILogger<HardwareDetectionService> _logger;

    private HardwareSnapshot _currentSnapshot = HardwareSnapshot.Empty;
    private readonly object _snapshotLock = new();
    private CancellationTokenSource? _monitoringCts;
    private Task? _monitoringTask;

    public event EventHandler<HardwareChangedEventArgs>? HardwareChanged;

    public HardwareSnapshot CurrentSnapshot
    {
        get
        {
            lock (_snapshotLock)
            {
                return _currentSnapshot;
            }
        }
    }

    public HardwareDetectionService(
        MonitorDetector monitorDetector,
        HidDeviceDetector hidDetector,
        GpuDetector gpuDetector,
        CpuDetector cpuDetector,
        MemoryDetector memoryDetector,
        StorageDetector storageDetector,
        ILogger<HardwareDetectionService> logger)
    {
        _monitorDetector = monitorDetector;
        _hidDetector = hidDetector;
        _gpuDetector = gpuDetector;
        _cpuDetector = cpuDetector;
        _memoryDetector = memoryDetector;
        _storageDetector = storageDetector;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting hardware detection...");
        await DetectHardwareAsync(cancellationToken);
        StartMonitoring();
        _logger.LogInformation("Hardware detection initialized");
    }

    public Task<HardwareSnapshot> DetectHardwareAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Detecting hardware...");

        var monitors = _monitorDetector.Detect(cancellationToken);
        var (keyboards, mice) = _hidDetector.Detect(cancellationToken);
        var gpus = _gpuDetector.Detect(cancellationToken);
        var cpu = _cpuDetector.Detect(cancellationToken);
        var memory = _memoryDetector.Detect(cancellationToken);
        var storage = _storageDetector.Detect(cancellationToken);

        var newSnapshot = new HardwareSnapshot
        {
            Monitors = monitors,
            Keyboards = keyboards,
            Mice = mice,
            Gpus = gpus,
            Cpu = cpu,
            Memory = memory,
            Storage = storage
        };

        lock (_snapshotLock)
        {
            var oldSnapshot = _currentSnapshot;
            _currentSnapshot = newSnapshot;
            DetectChanges(oldSnapshot, newSnapshot);
        }

        _logger.LogInformation("Hardware detection completed: {MonitorCount} monitors, {KeyboardCount} keyboards, {MouseCount} mice, {GpuCount} GPUs",
            monitors.Length, keyboards.Length, mice.Length, gpus.Length);

        return Task.FromResult(newSnapshot);
    }

    public async Task RefreshAsync(IEnumerable<DeviceType>? deviceTypes = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Refreshing hardware...");
        await DetectHardwareAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _monitoringCts?.Cancel();
        if (_monitoringTask != null)
        {
            try
            {
                await _monitoringTask;
            }
            catch (OperationCanceledException)
            {
            }
        }
        _monitoringCts?.Dispose();
        _monitoringCts = null;
        _monitoringTask = null;
    }

    private void StartMonitoring()
    {
        _monitoringCts = new CancellationTokenSource();
        _monitoringTask = Task.Run(async () =>
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            while (!_monitoringCts.Token.IsCancellationRequested)
            {
                try
                {
                    await timer.WaitForNextTickAsync(_monitoringCts.Token);
                    await DetectHardwareAsync(_monitoringCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in hardware monitoring");
                }
            }
        }, _monitoringCts.Token);
    }

    private void DetectChanges(HardwareSnapshot oldSnapshot, HardwareSnapshot newSnapshot)
    {
        var oldDevices = oldSnapshot.AllDevices.ToDictionary(d => d.Id);
        var newDevices = newSnapshot.AllDevices.ToDictionary(d => d.Id);

        foreach (var (id, newDevice) in newDevices)
        {
            if (!oldDevices.TryGetValue(id, out var oldDevice))
            {
                OnHardwareChanged(new HardwareChangedEventArgs
                {
                    DeviceType = newDevice.DeviceType,
                    DeviceId = id,
                    ChangeType = HardwareChangeType.Added,
                    NewDevice = newDevice
                });
            }
            else if (HasSignificantChange(oldDevice, newDevice))
            {
                OnHardwareChanged(new HardwareChangedEventArgs
                {
                    DeviceType = newDevice.DeviceType,
                    DeviceId = id,
                    ChangeType = HardwareChangeType.Updated,
                    OldDevice = oldDevice,
                    NewDevice = newDevice
                });
            }
        }

        foreach (var (id, oldDevice) in oldDevices)
        {
            if (!newDevices.ContainsKey(id))
            {
                var disconnectedDevice = oldDevice.WithConnectionStatus(ConnectionStatus.Disconnected);
                OnHardwareChanged(new HardwareChangedEventArgs
                {
                    DeviceType = oldDevice.DeviceType,
                    DeviceId = id,
                    ChangeType = HardwareChangeType.Removed,
                    OldDevice = oldDevice,
                    NewDevice = disconnectedDevice
                });
            }
        }
    }

    private bool HasSignificantChange(HardwareDevice oldDevice, HardwareDevice newDevice)
    {
        if (oldDevice.ConnectionStatus != newDevice.ConnectionStatus)
            return true;
        
        if (oldDevice.FriendlyName != newDevice.FriendlyName)
            return true;

        return newDevice switch
        {
            MonitorDevice newMon when oldDevice is MonitorDevice oldMon =>
                newMon.Width != oldMon.Width ||
                newMon.Height != oldMon.Height ||
                newMon.RefreshRate != oldMon.RefreshRate ||
                newMon.IsPrimary != oldMon.IsPrimary,
            _ => false
        };
    }

    private void OnHardwareChanged(HardwareChangedEventArgs args)
    {
        try
        {
            HardwareChanged?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error raising HardwareChanged event");
        }
    }
}
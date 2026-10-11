using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreeSeat.App.ViewModels;
using FreeSeat.Core.Hardware.Interfaces;
using FreeSeat.Core.Hardware.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;

namespace FreeSeat.App.ViewModels;

/// <summary>
/// Hardware view model for displaying detected hardware.
/// </summary>
public sealed partial class HardwareViewModel : ViewModelBase
{
    private readonly IHardwareDetectionService _hardwareService;
    private readonly ILogger<HardwareViewModel> _logger;

    public HardwareViewModel(IHardwareDetectionService hardwareService, ILogger<HardwareViewModel> logger)
    {
        _hardwareService = hardwareService;
        _logger = logger;
        Title = "Hardware";

        _hardwareService.HardwareChanged += OnHardwareChanged;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        
        // Initialize with current snapshot
        UpdateFromSnapshot(_hardwareService.CurrentSnapshot);
    }

    [ObservableProperty]
    private ImmutableArray<MonitorDevice> _monitors = ImmutableArray<MonitorDevice>.Empty;

    [ObservableProperty]
    private ImmutableArray<KeyboardDevice> _keyboards = ImmutableArray<KeyboardDevice>.Empty;

    [ObservableProperty]
    private ImmutableArray<MouseDevice> _mice = ImmutableArray<MouseDevice>.Empty;

    [ObservableProperty]
    private ImmutableArray<GpuDevice> _gpus = ImmutableArray<GpuDevice>.Empty;

    [ObservableProperty]
    private CpuInfo? _cpu;

    [ObservableProperty]
    private MemoryInfo? _memory;

    [ObservableProperty]
    private ImmutableArray<StorageInfo> _storage = ImmutableArray<StorageInfo>.Empty;

    public IAsyncRelayCommand RefreshCommand { get; }

    private async Task RefreshAsync()
    {
        IsBusy = true;
        BusyMessage = "Detecting hardware...";
        
        try
        {
            await _hardwareService.RefreshAsync(cancellationToken: default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh hardware");
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    private void OnHardwareChanged(object? sender, HardwareChangedEventArgs args)
    {
        // Update on UI thread
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            UpdateFromSnapshot(_hardwareService.CurrentSnapshot);
        });
    }

    private void UpdateFromSnapshot(HardwareSnapshot snapshot)
    {
        Monitors = snapshot.Monitors;
        Keyboards = snapshot.Keyboards;
        Mice = snapshot.Mice;
        Gpus = snapshot.Gpus;
        Cpu = snapshot.Cpu;
        Memory = snapshot.Memory;
        Storage = snapshot.Storage;
    }

    public override string ToString() => "Hardware Detection";
}
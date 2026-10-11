using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreeSeat.App.ViewModels;
using FreeSeat.Core;
using FreeSeat.Core.Diagnostics;
using FreeSeat.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace FreeSeat.App.ViewModels;

/// <summary>
/// Diagnostics view model - read-only system information.
/// </summary>
public sealed partial class DiagnosticsViewModel : ViewModelBase
{
    private readonly IDiagnosticsService _diagnosticsService;
    private readonly ILogger<DiagnosticsViewModel> _logger;

    public DiagnosticsViewModel(IDiagnosticsService diagnosticsService, ILogger<DiagnosticsViewModel> logger)
    {
        _diagnosticsService = diagnosticsService;
        _logger = logger;
        Title = "Diagnostics";
        
        DiagnosticItems = new ObservableCollection<DiagnosticItem>();
        LoadDiagnosticsCommand = new AsyncRelayCommand(LoadDiagnosticsAsync);
        CopyDiagnosticsCommand = new RelayCommand(CopyDiagnosticsToClipboard);
    }

    public ObservableCollection<DiagnosticItem> DiagnosticItems { get; }
    public IAsyncRelayCommand LoadDiagnosticsCommand { get; }
    public IRelayCommand CopyDiagnosticsCommand { get; }

    public string AppVersion => AppInfo.Version;

    private async Task LoadDiagnosticsAsync()
    {
        IsBusy = true;
        BusyMessage = "Collecting system diagnostics...";
        
        try
        {
            var diagnostics = await _diagnosticsService.CollectDiagnosticsAsync();
            
            DiagnosticItems.Clear();
            DiagnosticItems.Add(new DiagnosticItem("Operating System", diagnostics.OperatingSystem));
            DiagnosticItems.Add(new DiagnosticItem("Architecture", diagnostics.Architecture));
            DiagnosticItems.Add(new DiagnosticItem("CPU", diagnostics.Cpu));
            DiagnosticItems.Add(new DiagnosticItem("RAM", diagnostics.Ram));
            DiagnosticItems.Add(new DiagnosticItem("GPU", diagnostics.Gpu));
            DiagnosticItems.Add(new DiagnosticItem("Free Disk Space", diagnostics.FreeDiskSpace));
            DiagnosticItems.Add(new DiagnosticItem("Application Version", diagnostics.ApplicationVersion));
            
            _fullDiagnosticsText = diagnostics.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load diagnostics");
            DiagnosticItems.Add(new DiagnosticItem("Error", "Failed to collect diagnostics"));
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    private string _fullDiagnosticsText = string.Empty;

    private void CopyDiagnosticsToClipboard()
    {
        try
        {
            if (!string.IsNullOrEmpty(_fullDiagnosticsText))
            {
                System.Windows.Clipboard.SetText(_fullDiagnosticsText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy diagnostics to clipboard");
        }
    }
}

public sealed class DiagnosticItem
{
    public string Label { get; }
    public string Value { get; }

    public DiagnosticItem(string label, string value)
    {
        Label = label;
        Value = value;
    }
}
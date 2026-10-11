using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreeSeat.Core.Hardware.Models;
using FreeSeat.Core.Seats.Interfaces;
using FreeSeat.Core.Seats.Models;
using FreeSeat.Core.Sessions.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FreeSeat.App.ViewModels;

/// <summary>
/// View model for the Seat Configuration page.
/// </summary>
public sealed partial class SeatsViewModel : ViewModelBase
{
    private readonly ISeatConfigurationService _seatService;
    private readonly ILogger<SeatsViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<SeatViewModel> _seats = new();

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _exportJson = string.Empty;

    [ObservableProperty]
    private bool _showExportDialog;

    [ObservableProperty]
    private string _importJson = string.Empty;

    [ObservableProperty]
    private bool _showImportDialog;

    [ObservableProperty]
    private string _importResultMessage = string.Empty;

    [ObservableProperty]
    private bool _hasBackup;

    private SeatSnapshot? _lastBackup;

    public SeatsViewModel(ISeatConfigurationService seatService, ILogger<SeatsViewModel> logger)
    {
        _seatService = seatService ?? throw new ArgumentNullException(nameof(seatService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Title = "Seat Configuration";

        // Subscribe to events
        _seatService.ConfigurationChanged += OnConfigurationChanged;
        _seatService.SeatValidationChanged += OnSeatValidationChanged;

        // Initialize seats from service
        InitializeSeats();

        // Load initial validation
        _ = ValidateAllAsync();
    }

    private void InitializeSeats()
    {
        Seats.Clear();
        foreach (var seat in _seatService.GetAllSeats())
        {
            var seatVm = new SeatViewModel(seat, _seatService);
            seatVm.PropertyChanged += OnSeatPropertyChanged;
            Seats.Add(seatVm);
        }
    }

    private void OnConfigurationChanged(object? sender, SeatConfigurationChangedEventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            switch (e.ChangeType)
            {
                case SeatChangeType.SeatCreated:
                    var newSeat = _seatService.GetSeat(e.SeatId);
                    if (newSeat != null)
                    {
                        var seatVm = new SeatViewModel(newSeat, _seatService);
                        seatVm.PropertyChanged += OnSeatPropertyChanged;
                        Seats.Add(seatVm);
                    }
                    break;

                case SeatChangeType.SeatRemoved:
                    var removedSeat = Seats.FirstOrDefault(s => s.Id == e.SeatId);
                    if (removedSeat != null)
                    {
                        removedSeat.PropertyChanged -= OnSeatPropertyChanged;
                        Seats.Remove(removedSeat);
                    }
                    break;

                case SeatChangeType.SeatRenamed:
                case SeatChangeType.AssignmentChanged:
                case SeatChangeType.SeatEnabled:
                case SeatChangeType.SeatDisabled:
                case SeatChangeType.SeatReset:
                    var updatedSeat = Seats.FirstOrDefault(s => s.Id == e.SeatId);
                    if (updatedSeat != null)
                    {
                        var serviceSeat = _seatService.GetSeat(e.SeatId);
                        if (serviceSeat != null)
                        {
                            updatedSeat.UpdateFromService(serviceSeat);
                        }
                    }
                    break;

                case SeatChangeType.ConfigurationImported:
                case SeatChangeType.ConfigurationRestored:
                    // Full refresh
                    RefreshSeats();
                    break;
            }

            StatusMessage = $"Configuration updated: {e.ChangeType}";
        });
    }

    private void OnSeatValidationChanged(object? sender, SeatValidationChangedEventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            var seatVm = Seats.FirstOrDefault(s => s.Id == e.SeatId);
            if (seatVm != null && e.ValidationResult != null)
            {
                seatVm.UpdateValidation(e.ValidationResult);
            }
        });
    }

    private void OnSeatPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Could track changes here if needed
    }

    private void RefreshSeats()
    {
        Seats.Clear();
        foreach (var seat in _seatService.GetAllSeats())
        {
            var seatVm = new SeatViewModel(seat, _seatService);
            seatVm.PropertyChanged += OnSeatPropertyChanged;
            Seats.Add(seatVm);
        }
    }

    private async Task ValidateAllAsync()
    {
        IsLoading = true;
        StatusMessage = "Validating seats...";
        try
        {
            await _seatService.ValidateAllAsync();
            StatusMessage = "Validation complete";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed");
            StatusMessage = $"Validation error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        StatusMessage = "Refreshing...";
        try
        {
            await _seatService.ValidateAllAsync();
            RefreshSeats();
            StatusMessage = "Refreshed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh failed");
            StatusMessage = $"Refresh error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddSeatAsync()
    {
        var seat = _seatService.CreateSeat($"Seat {Seats.Count + 1}");
        StatusMessage = $"Created seat: {seat.Name}";
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Exporting configuration...";
            ExportJson = await _seatService.ExportAsync();
            ShowExportDialog = true;
            StatusMessage = "Export complete";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export failed");
            StatusMessage = $"Export error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        if (string.IsNullOrWhiteSpace(ImportJson))
        {
            StatusMessage = "No JSON provided for import";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Importing configuration...";
            var result = await _seatService.ImportAsync(ImportJson);

            if (result.Success)
            {
                ImportResultMessage = "Import successful";
                ShowImportDialog = false;
                ImportJson = string.Empty;
                RefreshSeats();
                StatusMessage = "Import successful";
            }
            else
            {
                ImportResultMessage = $"Import failed: {result.ErrorMessage}";
                if (result.ValidationIssues.IsDefault == false && result.ValidationIssues.Length > 0)
                {
                    ImportResultMessage += "\n\nIssues:\n" + string.Join("\n", result.ValidationIssues.Select(i => $"- {i.Message}"));
                }
                StatusMessage = "Import failed";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import failed");
            ImportResultMessage = $"Import error: {ex.Message}";
            StatusMessage = "Import error";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Creating backup...";
            _lastBackup = await _seatService.CreateBackupAsync();
            HasBackup = true;
            ExportJson = await _seatService.ExportAsync();
            ShowExportDialog = true;
            StatusMessage = $"Backup snapshot created at {_lastBackup.Timestamp:yyyy-MM-dd HH:mm:ss}. It is available to restore until FreeSeat closes.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup failed");
            StatusMessage = $"Backup error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenImport() => ShowImportDialog = true;

    [RelayCommand]
    private void CloseImport() => ShowImportDialog = false;

    [RelayCommand]
    private void CloseExport() => ShowExportDialog = false;

    [RelayCommand]
    private async Task RestoreLastBackupAsync()
    {
        if (_lastBackup is null)
        {
            StatusMessage = "Create a backup snapshot before restoring.";
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _seatService.RestoreBackupAsync(_lastBackup);
            StatusMessage = result.Success
                ? $"Last backup restored. {result.Diff?.TotalChanges ?? 0} change(s) applied."
                : $"Restore failed: {result.ErrorMessage}";
            if (result.Success)
                RefreshSeats();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup restore failed");
            StatusMessage = $"Restore error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public override void Dispose()
    {
        _seatService.ConfigurationChanged -= OnConfigurationChanged;
        _seatService.SeatValidationChanged -= OnSeatValidationChanged;
        foreach (var seat in Seats)
        {
            seat.PropertyChanged -= OnSeatPropertyChanged;
        }
        base.Dispose();
    }
}

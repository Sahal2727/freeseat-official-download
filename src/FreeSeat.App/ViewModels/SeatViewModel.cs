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
/// View model for a single seat in the UI.
/// </summary>
public sealed partial class SeatViewModel : ObservableObject
{
    private readonly ISeatConfigurationService _seatService;
    private bool _isUpdatingFromService;

    public SeatViewModel(Seat seat, ISeatConfigurationService seatService)
    {
        _seatService = seatService;
        UpdateFromService(seat);
    }

    [ObservableProperty]
    private SeatId _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private string _monitorDisplay = "Not assigned";

    [ObservableProperty]
    private string _keyboardDisplay = "Not assigned";

    [ObservableProperty]
    private string _mouseDisplay = "Not assigned";

    [ObservableProperty]
    private string _sessionDisplay = "Not assigned";

    [ObservableProperty]
    private string _statusDisplay = "Not configured";

    [ObservableProperty]
    private bool _hasConflicts;

    [ObservableProperty]
    private bool _hasUnavailableDevices;

    [ObservableProperty]
    private SeatStatus _status;

    [ObservableProperty]
    private string _monitorId = string.Empty;

    [ObservableProperty]
    private string _keyboardId = string.Empty;

    [ObservableProperty]
    private string _mouseId = string.Empty;

    [ObservableProperty]
    private int? _sessionId;

    public IReadOnlyList<MonitorDevice> AvailableMonitors => _seatService.GetAvailableMonitors();
    public IReadOnlyList<KeyboardDevice> AvailableKeyboards => _seatService.GetAvailableKeyboards();
    public IReadOnlyList<MouseDevice> AvailableMice => _seatService.GetAvailableMice();
    public IReadOnlyList<ManagedSession> AvailableSessions => _seatService.GetAvailableSessions();

    public void UpdateFromService(Seat seat)
    {
        _isUpdatingFromService = true;
        try
        {
            Id = seat.Id;
            Name = seat.Name;
            IsEnabled = seat.IsEnabled;

            var assignment = seat.Assignment;
            MonitorId = assignment.MonitorId ?? string.Empty;
            KeyboardId = assignment.KeyboardId ?? string.Empty;
            MouseId = assignment.MouseId ?? string.Empty;
            SessionId = assignment.SessionId;

            UpdateDisplayStrings();
            UpdateValidationState(seat.ValidationResult);
        }
        finally
        {
            _isUpdatingFromService = false;
        }
    }

    public void UpdateValidation(SeatValidationResult result)
    {
        UpdateValidationState(result);
    }

    private void UpdateValidationState(SeatValidationResult? result)
    {
        if (result == null)
        {
            StatusDisplay = "Not validated";
            Status = SeatStatus.Unconfigured;
            HasConflicts = false;
            HasUnavailableDevices = false;
            return;
        }

        Status = result.Status;
        HasConflicts = result.Issues.Any(i => i.Type == ValidationIssueType.Conflict);
        HasUnavailableDevices = result.Issues.Any(i => i.Type == ValidationIssueType.DeviceUnavailable || i.Type == ValidationIssueType.SessionUnavailable);

        StatusDisplay = result.Status.GetDescription();
        if (!result.IsValid)
        {
            var firstIssue = result.Issues.FirstOrDefault();
            if (firstIssue != null)
            {
                StatusDisplay += $": {firstIssue.Message}";
            }
        }
    }

    private void UpdateDisplayStrings()
    {
        var monitors = AvailableMonitors;
        var keyboards = AvailableKeyboards;
        var mice = AvailableMice;
        var sessions = AvailableSessions;

        MonitorDisplay = string.IsNullOrEmpty(MonitorId) ? "Not assigned" : 
            monitors.FirstOrDefault(m => m.Id == MonitorId)?.FriendlyName ?? MonitorId;

        KeyboardDisplay = string.IsNullOrEmpty(KeyboardId) ? "Not assigned" :
            keyboards.FirstOrDefault(k => k.Id == KeyboardId)?.FriendlyName ?? KeyboardId;

        MouseDisplay = string.IsNullOrEmpty(MouseId) ? "Not assigned" :
            mice.FirstOrDefault(m => m.Id == MouseId)?.FriendlyName ?? MouseId;

        SessionDisplay = SessionId.HasValue ?
            sessions.FirstOrDefault(s => s.SessionId == SessionId.Value)?.DisplayName ?? $"Session {SessionId.Value}" :
            "Not assigned";
    }

    partial void OnMonitorIdChanged(string value)
    {
        if (IsEnabled && !_isUpdatingFromService)
        {
            _ = PersistAssignmentAsync(() => _seatService.AssignMonitorAsync(Id, string.IsNullOrEmpty(value) ? null : value));
        }
        UpdateDisplayStrings();
    }

    partial void OnKeyboardIdChanged(string value)
    {
        if (IsEnabled && !_isUpdatingFromService)
        {
            _ = PersistAssignmentAsync(() => _seatService.AssignKeyboardAsync(Id, string.IsNullOrEmpty(value) ? null : value));
        }
        UpdateDisplayStrings();
    }

    partial void OnMouseIdChanged(string value)
    {
        if (IsEnabled && !_isUpdatingFromService)
        {
            _ = PersistAssignmentAsync(() => _seatService.AssignMouseAsync(Id, string.IsNullOrEmpty(value) ? null : value));
        }
        UpdateDisplayStrings();
    }

    partial void OnSessionIdChanged(int? value)
    {
        if (IsEnabled && !_isUpdatingFromService)
        {
            _ = PersistAssignmentAsync(() => _seatService.AssignSessionAsync(Id, value));
        }
        UpdateDisplayStrings();
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (!_isUpdatingFromService)
            _ = PersistAssignmentAsync(() => _seatService.SetSeatEnabledAsync(Id, value));
    }

    private async Task PersistAssignmentAsync(Func<Task<bool>> update)
    {
        try
        {
            if (!await update())
                StatusDisplay = "The assignment could not be saved. Refresh and try again.";
        }
        catch (Exception ex)
        {
            StatusDisplay = $"Could not save the assignment: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RenameAsync(string? newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return;

        await _seatService.RenameSeatAsync(Id, newName.Trim());
    }

    [RelayCommand]
    private async Task ResetAsync()
    {
        await _seatService.ResetSeatAsync(Id);
    }

    [RelayCommand]
    private async Task ValidateAsync()
    {
        await _seatService.ValidateSeatAsync(Id);
    }
}

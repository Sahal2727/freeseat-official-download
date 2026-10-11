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
/// Settings view model with placeholders for future configuration.
/// </summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SettingsViewModel> _logger;
    private bool _isLoadingSettings;

    public SettingsViewModel(ISettingsService settingsService, ILogger<SettingsViewModel> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
        Title = "Settings";
        
        // Initialize settings categories
        SettingsCategories = new[]
        {
            new SettingsCategoryViewModel
            {
                Name = "General",
                Icon = "🌐",
                Description = "Language, updates, and privacy settings",
                IsImplemented = true,
                PlaceholderText = "General settings are available."
            },
            new SettingsCategoryViewModel
            {
                Name = "Appearance",
                Icon = "🎨",
                Description = "Theme, accent color, and visual preferences",
                IsImplemented = true,
                PlaceholderText = "Appearance settings are available."
            },
            new SettingsCategoryViewModel
            {
                Name = "Startup",
                Icon = "🚀",
                Description = "Launch behavior and Windows startup",
                IsImplemented = true,
                PlaceholderText = "Startup settings are available."
            },
            new SettingsCategoryViewModel
            {
                Name = "Seat Configuration",
                Icon = "🪑",
                Description = "Configure monitors, input devices, and user sessions for each seat",
                IsImplemented = false,
                PlaceholderText = "Seat configuration will be implemented in a future milestone."
            },
            new SettingsCategoryViewModel
            {
                Name = "Diagnostics",
                Icon = "🔍",
                Description = "Data collection and diagnostic preferences",
                IsImplemented = false,
                PlaceholderText = "Diagnostics settings will be implemented in a future milestone."
            }
        };
        
        // Initialize combo box items
        Languages = new ObservableCollection<string> { "en-US", "de-DE", "fr-FR", "es-ES", "ja-JP", "zh-CN" };
        Themes = new ObservableCollection<AppTheme> { AppTheme.Light, AppTheme.Dark, AppTheme.System };
        
        // Load current settings
        LoadSettings();
        ThemeManager.Apply(Theme);
    }

    public SettingsCategoryViewModel[] SettingsCategories { get; }
    
    public ObservableCollection<string> Languages { get; }
    public ObservableCollection<AppTheme> Themes { get; }
    
    // General settings
    [ObservableProperty]
    private string _language = "en-US";
    
    [ObservableProperty]
    private bool _checkForUpdates = true;
    
    [ObservableProperty]
    private bool _sendAnonymousUsageData = false;
    
    // Appearance settings
    [ObservableProperty]
    private AppTheme _theme = AppTheme.Dark;
    
    [ObservableProperty]
    private bool _useAccentColor = true;
    
    // Startup settings
    [ObservableProperty]
    private bool _startMinimized = false;
    
    [ObservableProperty]
    private bool _startWithWindows = false;

    partial void OnLanguageChanged(string value) => SaveSettings();
    partial void OnCheckForUpdatesChanged(bool value) => SaveSettings();
    partial void OnSendAnonymousUsageDataChanged(bool value) => SaveSettings();
    partial void OnThemeChanged(AppTheme value)
    {
        ThemeManager.Apply(value);
        SaveSettings();
    }
    partial void OnUseAccentColorChanged(bool value) => SaveSettings();
    partial void OnStartMinimizedChanged(bool value) => SaveSettings();
    partial void OnStartWithWindowsChanged(bool value) => SaveSettings();

    private void LoadSettings()
    {
        _isLoadingSettings = true;
        try
        {
            var settings = _settingsService.Settings;
            Language = settings.General.Language;
            CheckForUpdates = settings.General.CheckForUpdates;
            SendAnonymousUsageData = settings.General.SendAnonymousUsageData;
            Theme = settings.Appearance.Theme;
            UseAccentColor = settings.Appearance.UseAccentColor;
            StartMinimized = settings.Startup.StartMinimized;
            StartWithWindows = settings.Startup.StartWithWindows;
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private async void SaveSettings()
    {
        if (_isLoadingSettings)
            return;

        var settings = _settingsService.Settings;
        settings.General.Language = Language;
        settings.General.CheckForUpdates = CheckForUpdates;
        settings.General.SendAnonymousUsageData = SendAnonymousUsageData;
        settings.Appearance.Theme = Theme;
        settings.Appearance.UseAccentColor = UseAccentColor;
        settings.Startup.StartMinimized = StartMinimized;
        settings.Startup.StartWithWindows = StartWithWindows;
        
        try
        {
            await _settingsService.SaveAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
        }
    }
}

public sealed class SettingsCategoryViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsImplemented { get; set; }
    public string PlaceholderText { get; set; } = string.Empty;
}

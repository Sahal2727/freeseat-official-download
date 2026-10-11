using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreeSeat.App.ViewModels;
using FreeSeat.Core;
using FreeSeat.Core.Diagnostics;
using FreeSeat.Core.Sessions.Interfaces;
using FreeSeat.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System;

namespace FreeSeat.App.ViewModels;

/// <summary>
/// Main window view model with navigation.
/// </summary>
public sealed partial class MainViewModel : ViewModelBase
{
    private readonly IDiagnosticsService _diagnosticsService;
    private readonly ISettingsService _settingsService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;

    private ViewModelBase? _currentViewModel;

    public MainViewModel(
        IDiagnosticsService diagnosticsService,
        ISettingsService settingsService,
        ISessionService sessionService,
        ILogger<MainViewModel> logger,
        IServiceProvider serviceProvider)
    {
        _diagnosticsService = diagnosticsService;
        _settingsService = settingsService;
        _sessionService = sessionService;
        _logger = logger;
        _serviceProvider = serviceProvider;

        Title = $"{AppInfo.Name} - {AppInfo.Tagline}";

        // Navigation items - ViewModels will be resolved lazily when navigated to
        NavigationItems = new ObservableCollection<NavigationItem>
        {
            new("Dashboard", "\uE80F", typeof(DashboardViewModel)),
            new("Hardware", "\uE8B7", typeof(HardwareViewModel)),
            new("Sessions", "\uE823", typeof(SessionViewModel)),
            new("Seats", "\uE716", typeof(SeatsViewModel)),
            new("Diagnostics", "\uE721", typeof(DiagnosticsViewModel)),
            new("Settings", "\uE713", typeof(SettingsViewModel)),
            new("About", "\uE946", typeof(AboutViewModel))
        };

        // Don't resolve initial ViewModel here - defer until window is loaded
        // This avoids stack overflow during MainWindow construction
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    [RelayCommand]
    private void Navigate(NavigationItem? item)
    {
        if (item == null) return;

        foreach (var navItem in NavigationItems)
        {
            navItem.IsSelected = navItem == item;
        }

        // Resolve ViewModel on demand
        CurrentViewModel = (ViewModelBase)_serviceProvider.GetRequiredService(item.ViewModelType);
        _logger.LogInformation("Navigated to {Page}", item.Name);
    }

    // Call this when the window is loaded to set initial view
    public void InitializeDefaultView()
    {
        if (CurrentViewModel == null && NavigationItems.Count > 0)
        {
            var firstItem = NavigationItems[0];
            CurrentViewModel = (ViewModelBase)_serviceProvider.GetRequiredService(firstItem.ViewModelType);
            firstItem.IsSelected = true;
        }
    }

    public override string ToString() => $"{AppInfo.Name} v{AppInfo.Version}";

    public sealed partial class NavigationItem : ObservableObject
    {
        public string Name { get; }
        public string Icon { get; }
        public Type ViewModelType { get; }

        [ObservableProperty]
        private bool _isSelected;

        public NavigationItem(string name, string icon, Type viewModelType)
        {
            Name = name;
            Icon = icon;
            ViewModelType = viewModelType;
        }
    }
}

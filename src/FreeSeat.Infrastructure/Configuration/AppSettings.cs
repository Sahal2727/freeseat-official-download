namespace FreeSeat.Infrastructure.Configuration;

/// <summary>
/// Application settings that can be persisted.
/// </summary>
public sealed class AppSettings
{
    public GeneralSettings General { get; set; } = new();
    public AppearanceSettings Appearance { get; set; } = new();
    public StartupSettings Startup { get; set; } = new();
    public SessionManagerSettings SessionManager { get; set; } = new();
    public SeatConfigurationSettings SeatConfiguration { get; set; } = new();
}

public sealed class GeneralSettings
{
    public string Language { get; set; } = "en-US";
    public bool CheckForUpdates { get; set; } = true;
    public bool SendAnonymousUsageData { get; set; } = false;
}

public sealed class AppearanceSettings
{
    public AppTheme Theme { get; set; } = AppTheme.Dark;
    public bool UseAccentColor { get; set; } = true;
}

public sealed class StartupSettings
{
    public bool StartMinimized { get; set; } = false;
    public bool StartWithWindows { get; set; } = false;
}

/// <summary>
/// Settings for the Session Manager.
/// </summary>
public sealed class SessionManagerSettings
{
    /// <summary>
    /// Whether session monitoring is enabled.
    /// </summary>
    public bool MonitoringEnabled { get; set; } = true;

    /// <summary>
    /// Reconciliation interval in seconds (default: 300 = 5 minutes).
    /// </summary>
    public int ReconciliationIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Maximum number of session events to keep in history.
    /// </summary>
    public int MaxEventHistory { get; set; } = 100;

    /// <summary>
    /// Whether developer diagnostics mode is enabled.
    /// When enabled, the Test Launch feature is available.
    /// </summary>
    public bool DeveloperMode { get; set; } = false;
}

/// <summary>
/// Settings for Seat Configuration.
/// </summary>
public sealed class SeatConfigurationSettings
{
    /// <summary>
    /// JSON-serialized seat configuration.
    /// </summary>
    public string SeatConfiguration { get; set; } = string.Empty;
}

public enum AppTheme
{
    Light,
    Dark,
    System
}

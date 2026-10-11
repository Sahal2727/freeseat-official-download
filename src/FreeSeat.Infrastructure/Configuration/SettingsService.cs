using System.IO;
using System.Text.Json;
using FreeSeat.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace FreeSeat.Infrastructure.Configuration;

/// <summary>
/// JSON-based settings persistence service.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly string _settingsPath;
    private AppSettings _settings = new();

    public AppSettings Settings => _settings;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var settingsDir = Path.Combine(appDataPath, "FreeSeat");
        Directory.CreateDirectory(settingsDir);
        _settingsPath = Path.Combine(settingsDir, "settings.json");
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath, cancellationToken);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                _settings = settings ?? new AppSettings();
                _logger.LogInformation("Settings loaded from {Path}", _settingsPath);
            }
            else
            {
                _settings = new AppSettings();
                _logger.LogInformation("No settings file found, using defaults");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings, using defaults");
            _settings = new AppSettings();
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_settings, options);
            await File.WriteAllTextAsync(_settingsPath, json, cancellationToken);
            _logger.LogInformation("Settings saved to {Path}", _settingsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            throw;
        }
    }

    public void ResetToDefaults()
    {
        _settings = new AppSettings();
        _logger.LogInformation("Settings reset to defaults");
    }
}
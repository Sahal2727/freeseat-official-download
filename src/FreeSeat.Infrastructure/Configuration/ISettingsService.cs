namespace FreeSeat.Infrastructure.Configuration;

/// <summary>
/// Service for managing application settings persistence.
/// </summary>
public interface ISettingsService
{
    AppSettings Settings { get; }
    Task LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    void ResetToDefaults();
}
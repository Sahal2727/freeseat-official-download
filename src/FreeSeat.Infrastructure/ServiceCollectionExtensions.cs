using FreeSeat.Core.Diagnostics;
using FreeSeat.Core.Hardware.Interfaces;
using FreeSeat.Core.Runtime;
using FreeSeat.Core.Seats.Interfaces;
using FreeSeat.Core.Sessions.Interfaces;
using FreeSeat.Infrastructure.Configuration;
using FreeSeat.Infrastructure.Hardware;
using FreeSeat.Infrastructure.Runtime;
using FreeSeat.Infrastructure.Seats;
using FreeSeat.Infrastructure.Sessions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FreeSeat.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDiagnosticsService, DiagnosticsService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ISessionService, SessionService>();

        services.AddSingleton<IDisplayDetectionService, DisplayDetectionService>();
        services.AddSingleton<IRuntimeOrchestrationService, RuntimeOrchestrationService>();

        // Session Manager services
        services.AddSingleton<SessionStateTracker>();
        services.AddSingleton<SessionLauncher>();
        services.AddSingleton<SessionRecoveryService>();
        services.AddSingleton<ISessionManager, SessionManager>();

        // Seat Configuration services
        services.AddSingleton<ISeatConfigurationService, SeatConfigurationService>();

        // Settings - bind from AppSettings using post-configuration
        services.Configure<SessionManagerSettings>(settings => 
            settings.MonitoringEnabled = true);
        services.Configure<SeatConfigurationSettings>(settings => 
            { });

        // Register settings objects for DI injection
        services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<SessionManagerSettings>>().Value);
        services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<SeatConfigurationSettings>>().Value);

        services.AddHardwareDetection();
        services.AddHostedService<RuntimeInitializer>();

        // Settings loaded on startup
        services.AddHostedService<SettingsInitializer>();

        return services;
    }

    public static IServiceCollection AddHardwareDetection(this IServiceCollection services)
    {
        // Register hardware detector components (required by HardwareDetectionService)
        services.AddSingleton<MonitorDetector>();
        services.AddSingleton<HidDeviceDetector>();
        services.AddSingleton<GpuDetector>();
        services.AddSingleton<CpuDetector>();
        services.AddSingleton<MemoryDetector>();
        services.AddSingleton<StorageDetector>();

        services.AddSingleton<IHardwareDetectionService, HardwareDetectionService>();
        services.AddHostedService<HardwareDetectionInitializer>();
        return services;
    }
}

internal sealed class SettingsInitializer : IHostedService
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SettingsInitializer> _logger;

    public SettingsInitializer(ISettingsService settingsService, ILogger<SettingsInitializer> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _settingsService.LoadAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _settingsService.SaveAsync(cancellationToken);
    }
}

internal sealed class HardwareDetectionInitializer : IHostedService
{
    private readonly IHardwareDetectionService _hardwareService;
    private readonly ILogger<HardwareDetectionInitializer> _logger;

    public HardwareDetectionInitializer(IHardwareDetectionService hardwareService, ILogger<HardwareDetectionInitializer> logger)
    {
        _hardwareService = hardwareService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing hardware detection...");
        // Don't await - run in background to avoid blocking host startup
        _ = Task.Run(async () =>
        {
            try
            {
                await _hardwareService.InitializeAsync(cancellationToken);
                _logger.LogInformation("Hardware detection initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hardware detection initialization failed");
            }
        }, cancellationToken);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping hardware detection...");
        await _hardwareService.StopAsync(cancellationToken);
        _logger.LogInformation("Hardware detection stopped");
    }
}

internal sealed class RuntimeInitializer : IHostedService
{
    private readonly IRuntimeOrchestrationService _runtimeService;
    private readonly IDisplayDetectionService _displayService;
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<RuntimeInitializer> _logger;

    public RuntimeInitializer(
        IRuntimeOrchestrationService runtimeService,
        IDisplayDetectionService displayService,
        ISessionManager sessionManager,
        ILogger<RuntimeInitializer> logger)
    {
        _runtimeService = runtimeService;
        _displayService = displayService;
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _displayService.InitializeAsync(cancellationToken);
            await _sessionManager.StartMonitoringAsync(cancellationToken);
            await _runtimeService.InitializeAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Runtime initialization was incomplete; FreeSeat will remain available for configuration and diagnostics");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _runtimeService.StopAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop runtime orchestration cleanly");
        }

        try
        {
            await _sessionManager.StopMonitoringAsync(cancellationToken);
            await _displayService.StopAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop one or more monitoring services cleanly");
        }
    }
}

using FreeSeat.App.ViewModels;
using FreeSeat.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FreeSeat.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    public App()
    {
        // Handle unhandled exceptions on the UI thread
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        
        // Handle unhandled exceptions on non-UI threads
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        StartupLog.Write($"App.DispatcherUnhandledException: {e.Exception}");
        // An unhandled UI exception must not be silently swallowed. WPF can
        // otherwise keep rendering a broken view and repeatedly raise the same
        // layout exception, leaving a responsive but unusable blank window.
        e.Handled = false;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        StartupLog.Write($"App.CurrentDomain_UnhandledException: {e.ExceptionObject}");
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        StartupLog.Write($"App.TaskScheduler_UnobservedTaskException: {e.Exception}");
        e.SetObserved();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        StartupLog.Write("App.OnStartup: Building host...");
        _host = CreateHostBuilder().Build();
        StartupLog.Write("App.OnStartup: Host built (service provider ready)");
        
        // Create and show MainWindow from DI container
        try
        {
            StartupLog.Write("App.OnStartup: Getting MainWindow from DI...");
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            
            MainWindow = mainWindow;
            StartupLog.Write("App.OnStartup: MainWindow set as Application.MainWindow");
            
            // Must explicitly show the window when setting MainWindow manually
            mainWindow.Show();
            LogWindowState(mainWindow, "Show returned");
            mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => LogWindowState(mainWindow, "Dispatcher idle")));
            StartupLog.Write("App.OnStartup: MainWindow.Show() called");
        }
        catch (Exception ex)
        {
            StartupLog.Write($"App.OnStartup: ERROR creating/showing MainWindow: {ex}");
            throw;
        }
        
        // Start hosted services in background (non-blocking)
        _ = Task.Run(async () =>
        {
            try
            {
                StartupLog.Write("App.OnStartup: Starting hosted services...");
                await _host!.StartAsync();
                var theme = _host.Services.GetRequiredService<FreeSeat.Infrastructure.Configuration.ISettingsService>().Settings.Appearance.Theme;
                await Dispatcher.InvokeAsync(() => ThemeManager.Apply(theme));
                StartupLog.Write("App.OnStartup: Hosted services started");
            }
            catch (Exception ex)
            {
                StartupLog.Write($"App.OnStartup: Hosted services failed: {ex}");
            }
        });
        
        // Call base.OnStartup LAST - this starts the message pump
        StartupLog.Write("App.OnStartup: Calling base.OnStartup...");
        base.OnStartup(e);
        
        StartupLog.Write("App.OnStartup: base.OnStartup returned");
    }

    private static void LogWindowState(Window window, string stage)
    {
        StartupLog.Write(
            $"Window.{stage}: IsVisible={window.IsVisible}, Visibility={window.Visibility}, State={window.WindowState}, " +
            $"Left={window.Left:0.##}, Top={window.Top:0.##}, Width={window.Width:0.##}, Height={window.Height:0.##}, " +
            $"ActualWidth={window.ActualWidth:0.##}, ActualHeight={window.ActualHeight:0.##}, Opacity={window.Opacity:0.##}, ShowInTaskbar={window.ShowInTaskbar}");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        StartupLog.Write($"App.OnExit: Application exiting (ApplicationExitCode={e.ApplicationExitCode})");
        var host = _host;
        if (host is not null)
        {
            try
            {
                StartupLog.Write("App.OnExit: Host StopAsync starting");
                using var shutdownTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                // OnExit runs on the WPF dispatcher. Wait for host shutdown on a
                // thread-pool context so async hosted-service continuations cannot
                // deadlock against the dispatcher being shut down.
                Task.Run(() => host.StopAsync(shutdownTimeout.Token)).GetAwaiter().GetResult();
                StartupLog.Write("App.OnExit: Host StopAsync completed");
            }
            catch (Exception ex)
            {
                StartupLog.Write($"App.OnExit: Host StopAsync failed: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                host.Dispose();
                StartupLog.Write("App.OnExit: Host disposed");
            }
        }        base.OnExit(e);
    }

    private static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddInfrastructure();
                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<HardwareViewModel>();
                services.AddTransient<SeatsViewModel>();
                services.AddTransient<DiagnosticsViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<AboutViewModel>();
                services.AddTransient<SessionViewModel>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddDebug();
                logging.AddConsole();
            });
    }
    
    public static T GetService<T>() where T : notnull
    {
        return ((App)Current)._host!.Services.GetRequiredService<T>();
    }
}

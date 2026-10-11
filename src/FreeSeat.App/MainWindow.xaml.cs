using FreeSeat.App.ViewModels;
using System.Windows;

namespace FreeSeat.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        StartupLog.Write("MainWindow: Constructor start");
        InitializeComponent();
        SourceInitialized += (_, _) => LogWindowState("SourceInitialized");
        ContentRendered += (_, _) => LogWindowState("ContentRendered");
        Activated += (_, _) => LogWindowState("Activated");
        IsVisibleChanged += (_, _) => LogWindowState("IsVisibleChanged");
        StateChanged += (_, _) => LogWindowState("StateChanged");
        StartupLog.Write("MainWindow: InitializeComponent complete");
        
        try
        {
            StartupLog.Write("MainWindow: Getting MainViewModel from DI...");
            DataContext = App.GetService<MainViewModel>();
            StartupLog.Write("MainWindow: DataContext set successfully");
        }
        catch (Exception ex)
        {
            StartupLog.Write($"MainWindow: ERROR setting DataContext: {ex}");
            throw;
        }
        
        StartupLog.Write("MainWindow: Constructor complete");
    }

    private void LogWindowState(string stage)
    {
        StartupLog.Write(
            $"Window.{stage}: IsVisible={IsVisible}, Visibility={Visibility}, State={WindowState}, " +
            $"Left={Left:0.##}, Top={Top:0.##}, Width={Width:0.##}, Height={Height:0.##}, " +
            $"ActualWidth={ActualWidth:0.##}, ActualHeight={ActualHeight:0.##}, Opacity={Opacity:0.##}, ShowInTaskbar={ShowInTaskbar}");
    }

    private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        StartupLog.Write("MainWindow: Window_Loaded, initializing default view");
        StartupLog.Write($"MainWindow: Application.Windows.Count={Application.Current?.Windows.Count}, Application.MainWindow={Application.Current?.MainWindow?.GetHashCode()}");
        if (DataContext is MainViewModel vm)
        {
            vm.InitializeDefaultView();
            StartupLog.Write("MainWindow: Default view initialized");
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        StartupLog.Write("MainWindow: Window_Closing");
    }

    private void Window_Closed(object sender, System.EventArgs e)
    {
        StartupLog.Write("MainWindow: Window_Closed");
    }
}

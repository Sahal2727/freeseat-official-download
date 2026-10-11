using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreeSeat.App.ViewModels;
using FreeSeat.Core;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
namespace FreeSeat.App.ViewModels;
public sealed partial class AboutViewModel : ViewModelBase {
    private readonly ILogger<AboutViewModel> _logger;
    public AboutViewModel(ILogger<AboutViewModel> logger) {
        _logger = logger;
        Title = "About";
    }
    public string AppName => AppInfo.Name;
    public string Tagline => AppInfo.Tagline;
    public string Creator => AppInfo.Creator;
    public string Version => $"Version {AppInfo.Version}";
    public string Copyright => AppInfo.Copyright;
    public string Description => AppInfo.Description;
    public string LicenseType => "Free and open-source software (MIT License)";
    public string NetVersion => GetNetVersion();
    public string RuntimeVersion => GetRuntimeVersion();
    [RelayCommand]
    private void OpenGitHub() { OpenUrl(AppInfo.GitHubUrl); }
    [RelayCommand]
    private void OpenDocumentation() { OpenUrl(AppInfo.DocumentationUrl); }
    [RelayCommand]
    private void OpenLicense() { OpenUrl(AppInfo.LicenseUrl); }
    private void OpenUrl(string url) {
        try {
            if (url.StartsWith("TODO:", StringComparison.OrdinalIgnoreCase)) {
                System.Windows.MessageBox.Show(
                    "This link is a placeholder and will be implemented in a future release.",
                    "Coming Soon",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to open URL: {Url}", url); }
    }
    private static string GetNetVersion() {
        try {
            var assembly = Assembly.GetEntryAssembly();
            var targetFramework = assembly?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
            if (!string.IsNullOrEmpty(targetFramework)) {
                return targetFramework.Replace(".NETCoreApp,Version=v", ".NET ");
            }
        }
        catch { }
        return Environment.Version.ToString();
    }
    private static string GetRuntimeVersion() {
        var arch = Environment.Is64BitProcess ? "x64" : "x86";
        return $"{Environment.Version} ({arch})";
    }
}
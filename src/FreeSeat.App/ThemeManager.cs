using FreeSeat.Infrastructure.Configuration;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;

namespace FreeSeat.App;

/// <summary>
/// Applies the user's selected palette to the shared brushes used by every view.
/// </summary>
internal static class ThemeManager
{
    private static readonly IReadOnlyDictionary<string, string> DarkPalette = new Dictionary<string, string>
    {
        ["PrimaryColor"] = "#168BFF",
        ["PrimaryHoverColor"] = "#3A9BFF",
        ["PrimaryPressedColor"] = "#0870DC",
        ["BackgroundColor"] = "#0B1420",
        ["SurfaceColor"] = "#111D2B",
        ["SurfaceHoverColor"] = "#182738",
        ["SurfacePressedColor"] = "#203146",
        ["TextPrimaryColor"] = "#F4F7FB",
        ["TextSecondaryColor"] = "#B7C4D2",
        ["TextDisabledColor"] = "#8593A5",
        ["TextOnPrimaryColor"] = "#FFFFFF",
        ["BorderColor"] = "#26384A",
        ["BorderHoverColor"] = "#405A74",
        ["SuccessColor"] = "#34D399",
        ["WarningColor"] = "#FBBF24",
        ["ErrorColor"] = "#FB7185",
        ["InfoColor"] = "#38BDF8",
        ["NavigationSelectedColor"] = "#15304E",
        ["LogoStandColor"] = "#FFFFFF"
    };

    private static readonly IReadOnlyDictionary<string, string> LightPalette = new Dictionary<string, string>
    {
        ["PrimaryColor"] = "#0078D4",
        ["PrimaryHoverColor"] = "#106EBE",
        ["PrimaryPressedColor"] = "#005A9E",
        ["BackgroundColor"] = "#FFFFFF",
        ["SurfaceColor"] = "#F3F2F1",
        ["SurfaceHoverColor"] = "#EDEBE9",
        ["SurfacePressedColor"] = "#E1DFDD",
        ["TextPrimaryColor"] = "#323130",
        ["TextSecondaryColor"] = "#605E5C",
        ["TextDisabledColor"] = "#76716E",
        ["TextOnPrimaryColor"] = "#FFFFFF",
        ["BorderColor"] = "#D2D0CE",
        ["BorderHoverColor"] = "#8A8886",
        ["SuccessColor"] = "#107C10",
        ["WarningColor"] = "#B46900",
        ["ErrorColor"] = "#D13438",
        ["InfoColor"] = "#0078D4",
        ["NavigationSelectedColor"] = "#E6F2FC",
        ["LogoStandColor"] = "#000000"
    };

    public static void Apply(AppTheme theme)
    {
        var application = Application.Current;
        if (application is null)
            return;

        var palette = application.Resources.MergedDictionaries
            .FirstOrDefault(dictionary => dictionary.Contains("BackgroundColor"));
        if (palette is null)
            return;

        var useDarkPalette = theme == AppTheme.Dark ||
            (theme == AppTheme.System && IsWindowsUsingDarkApps());
        var colors = useDarkPalette ? DarkPalette : LightPalette;

        foreach (var (key, value) in colors)
        {
            var color = (Color)ColorConverter.ConvertFromString(value);
            palette[key] = color;
            application.Resources[key] = color;

            var brushKey = key.Replace("Color", "Brush", StringComparison.Ordinal);
            var brush = palette[brushKey] as SolidColorBrush;
            if (brush is null || brush.IsFrozen)
            {
                brush = new SolidColorBrush(color);
            }
            else
            {
                brush.Color = color;
            }

            palette[brushKey] = brush;
            application.Resources[brushKey] = brush;
        }

        application.Resources["FreeSeatLogoSource"] = application.Resources[
            useDarkPalette ? "FreeSeatDarkLogoSource" : "FreeSeatLightLogoSource"];
    }

    private static bool IsWindowsUsingDarkApps()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int useLightTheme && useLightTheme == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

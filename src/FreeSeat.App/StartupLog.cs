using System.Diagnostics;
using System.IO;

namespace FreeSeat.App;

/// <summary>
/// Best-effort startup diagnostics. Logging must never prevent the UI from opening.
/// </summary>
internal static class StartupLog
{
    private static readonly object SyncRoot = new();

    internal static string LogPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FreeSeat",
        "Logs",
        "startup.log");

    internal static void Write(string message) => WriteTo(LogPath, message);

    internal static void WriteTo(string logPath, string message)
    {
        try
        {
            var directory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            lock (SyncRoot)
            {
                File.AppendAllText(logPath, $"[{DateTimeOffset.Now:O}] {message}{Environment.NewLine}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"FreeSeat startup log write failed: {ex}");
        }
    }
}

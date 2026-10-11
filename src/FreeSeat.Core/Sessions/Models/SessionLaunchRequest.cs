using System.Collections.Generic;
using System.IO;

namespace FreeSeat.Core.Sessions.Models;

/// <summary>
/// Represents a request to launch a process in a specific Windows session.
/// This is a safe, validated request object - no shell commands or arbitrary execution.
/// </summary>
/// <param name="SessionId">The target Windows session ID.</param>
/// <param name="ExecutablePath">The full path to the executable to launch.</param>
/// <param name="Arguments">Command line arguments (optional).</param>
/// <param name="WorkingDirectory">The working directory (optional, defaults to executable directory).</param>
/// <param name="EnvironmentVariables">Additional environment variables (optional).</param>
/// <param name="RunAsCurrentUser">Whether to run as the session's current user (requires token).</param>
public record SessionLaunchRequest(
    int SessionId,
    string ExecutablePath,
    string? Arguments = null,
    string? WorkingDirectory = null,
    IReadOnlyDictionary<string, string>? EnvironmentVariables = null,
    bool RunAsCurrentUser = true
)
{
    /// <summary>
    /// Validates the launch request.
    /// </summary>
    /// <returns>True if valid, false otherwise.</returns>
    public bool IsValid(out string? errorMessage)
    {
        if (SessionId <= 0)
        {
            errorMessage = "Invalid session ID.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ExecutablePath))
        {
            errorMessage = "Executable path is required.";
            return false;
        }

        if (!File.Exists(ExecutablePath))
        {
            errorMessage = $"Executable not found: {ExecutablePath}";
            return false;
        }

        // Only allow .exe files for safety
        var extension = Path.GetExtension(ExecutablePath);
        if (!string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "Only .exe files are allowed for launch.";
            return false;
        }

        // Prevent system directory launches unless explicitly allowed
        var systemDirs = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            @"C:\Windows\System32",
            @"C:\Windows\SysWOW64"
        };

        var fullPath = Path.GetFullPath(ExecutablePath);
        if (systemDirs.Any(d => fullPath.StartsWith(d, StringComparison.OrdinalIgnoreCase)))
        {
            // Allow notepad.exe and calc.exe as developer test tools
            var fileName = Path.GetFileName(fullPath);
            if (!string.Equals(fileName, "notepad.exe", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(fileName, "calc.exe", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Launching system executables is not allowed.";
                return false;
            }
        }

        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Creates a developer test launch request for notepad.exe.
    /// </summary>
    public static SessionLaunchRequest CreateTestLaunch(int sessionId)
    {
        var notepadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "notepad.exe");
        return new SessionLaunchRequest(
            SessionId: sessionId,
            ExecutablePath: notepadPath,
            Arguments: null,
            WorkingDirectory: null,
            EnvironmentVariables: null,
            RunAsCurrentUser: true
        );
    }
}
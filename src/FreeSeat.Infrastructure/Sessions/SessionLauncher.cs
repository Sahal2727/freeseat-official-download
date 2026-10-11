using FreeSeat.Core.Sessions.Interfaces;
using FreeSeat.Core.Sessions.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace FreeSeat.Infrastructure.Sessions;

/// <summary>
/// Handles safe process launching in specific Windows sessions.
/// Uses documented Windows mechanisms (WTSQueryUserToken, CreateProcessAsUser, etc.).
/// </summary>
internal sealed class SessionLauncher : IDisposable
{
    private readonly ILogger<SessionLauncher> _logger;
    private bool _disposed;

    public SessionLauncher(ILogger<SessionLauncher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Attempts to launch a process in the specified session.
    /// </summary>
    public async Task<SessionLaunchResult> LaunchAsync(SessionLaunchRequest request, CancellationToken cancellationToken = default)
    {
        var validation = request.IsValid(out var errorMessage);
        if (!validation)
        {
            _logger.LogWarning("Launch validation failed for session {SessionId}: {Error}", request.SessionId, errorMessage);
            return SessionLaunchResult.FailureResult(request.SessionId, 0, errorMessage!);
        }

        return await Task.Run(() => LaunchInternal(request, cancellationToken), cancellationToken);
    }

    private SessionLaunchResult LaunchInternal(SessionLaunchRequest request, CancellationToken cancellationToken)
    {
        IntPtr userToken = IntPtr.Zero;
        IntPtr primaryToken = IntPtr.Zero;
        IntPtr environmentBlock = IntPtr.Zero;
        NativeMethods.SECURITY_ATTRIBUTES saProcess = default;
        NativeMethods.SECURITY_ATTRIBUTES saThread = default;
        NativeMethods.STARTUPINFOW si = default;
        NativeMethods.PROCESS_INFORMATION pi = default;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Step 1: Get the user token for the target session
            _logger.LogDebug("Querying user token for session {SessionId}", request.SessionId);
            if (!NativeMethods.WTSQueryUserToken(request.SessionId, out userToken))
            {
                var errorCode = Marshal.GetLastWin32Error();
                _logger.LogWarning("WTSQueryUserToken failed for session {SessionId}: {ErrorCode}", request.SessionId, errorCode);
                return SessionLaunchResult.FailureResult(request.SessionId, errorCode, $"Failed to get user token for session {request.SessionId} (Error: {errorCode})");
            }

            _logger.LogDebug("Got user token for session {SessionId}", request.SessionId);

            // Step 2: Duplicate the token to get a primary token with required access
            saProcess.nLength = Marshal.SizeOf<NativeMethods.SECURITY_ATTRIBUTES>();
            saThread.nLength = Marshal.SizeOf<NativeMethods.SECURITY_ATTRIBUTES>();

            var desiredAccess = NativeMethods.TOKEN_ASSIGN_PRIMARY | NativeMethods.TOKEN_DUPLICATE | NativeMethods.TOKEN_QUERY | NativeMethods.TOKEN_IMPERSONATE;
            
            if (!NativeMethods.DuplicateTokenEx(
                userToken,
                desiredAccess,
                ref saProcess,
                NativeMethods.SECURITY_IMPERSONATION,
                NativeMethods.TOKEN_TYPE_PRIMARY,
                out primaryToken))
            {
                var errorCode = Marshal.GetLastWin32Error();
                _logger.LogWarning("DuplicateTokenEx failed for session {SessionId}: {ErrorCode}", request.SessionId, errorCode);
                return SessionLaunchResult.FailureResult(request.SessionId, errorCode, $"Failed to duplicate token for session {request.SessionId} (Error: {errorCode})");
            }

            _logger.LogDebug("Duplicated primary token for session {SessionId}", request.SessionId);

            // Step 3: Create environment block for the user
            if (!NativeMethods.CreateEnvironmentBlock(out environmentBlock, primaryToken, false))
            {
                var errorCode = Marshal.GetLastWin32Error();
                _logger.LogWarning("CreateEnvironmentBlock failed for session {SessionId}: {ErrorCode}", request.SessionId, errorCode);
                // Continue without environment block - not fatal
            }

            // Step 4: Prepare startup info
            si.cb = Marshal.SizeOf<NativeMethods.STARTUPINFOW>();
            si.lpDesktop = "winsta0\\default"; // Default desktop

            // Step 5: Prepare command line
            string commandLine = BuildCommandLine(request.ExecutablePath, request.Arguments);
            string workingDirectory = request.WorkingDirectory ?? Path.GetDirectoryName(request.ExecutablePath) ?? Environment.SystemDirectory;

            // Step 6: Create the process
            uint creationFlags = NativeMethods.CREATE_UNICODE_ENVIRONMENT | NativeMethods.NORMAL_PRIORITY_CLASS;
            
            if (environmentBlock != IntPtr.Zero)
            {
                creationFlags |= NativeMethods.CREATE_UNICODE_ENVIRONMENT;
            }

            _logger.LogInformation("Launching process in session {SessionId}: {Executable} {Arguments}", 
                request.SessionId, request.ExecutablePath, request.Arguments ?? "");

            bool success = NativeMethods.CreateProcessAsUserW(
                primaryToken,
                request.ExecutablePath,
                commandLine,
                ref saProcess,
                ref saThread,
                false, // bInheritHandles
                creationFlags,
                environmentBlock,
                workingDirectory,
                ref si,
                out pi);

            if (!success)
            {
                var errorCode = Marshal.GetLastWin32Error();
                _logger.LogWarning("CreateProcessAsUserW failed for session {SessionId}: {ErrorCode}", request.SessionId, errorCode);
                return SessionLaunchResult.FailureResult(request.SessionId, errorCode, $"Failed to create process in session {request.SessionId} (Error: {errorCode})");
            }

            _logger.LogInformation("Successfully launched process {ProcessId} in session {SessionId}", pi.dwProcessId, request.SessionId);

            // Clean up handles
            if (pi.hProcess != IntPtr.Zero) NativeMethods.CloseHandle(pi.hProcess);
            if (pi.hThread != IntPtr.Zero) NativeMethods.CloseHandle(pi.hThread);

            return SessionLaunchResult.SuccessResult(pi.dwProcessId, request.SessionId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Launch cancelled for session {SessionId}", request.SessionId);
            return SessionLaunchResult.FailureResult(request.SessionId, 0, "Launch was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error launching process in session {SessionId}", request.SessionId);
            return SessionLaunchResult.FromException(request.SessionId, ex);
        }
        finally
        {
            // Cleanup
            if (userToken != IntPtr.Zero) NativeMethods.CloseHandle(userToken);
            if (primaryToken != IntPtr.Zero) NativeMethods.CloseHandle(primaryToken);
            if (environmentBlock != IntPtr.Zero) NativeMethods.DestroyEnvironmentBlock(environmentBlock);
            if (pi.hProcess != IntPtr.Zero) NativeMethods.CloseHandle(pi.hProcess);
            if (pi.hThread != IntPtr.Zero) NativeMethods.CloseHandle(pi.hThread);
        }
    }

    private static string BuildCommandLine(string executablePath, string? arguments)
    {
        var quotedExe = $"\"{executablePath}\"";
        return string.IsNullOrWhiteSpace(arguments) ? quotedExe : $"{quotedExe} {arguments}";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}

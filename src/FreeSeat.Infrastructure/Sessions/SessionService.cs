using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FreeSeat.Core.Sessions.Interfaces;
using FreeSeat.Core.Sessions.Models;
using Microsoft.Extensions.Logging;

namespace FreeSeat.Infrastructure.Sessions;

/// <summary>
/// Read-only implementation of ISessionService using Windows Terminal Services APIs.
/// This service is for diagnostic and research purposes only - no session modifications are performed.
/// </summary>
public sealed class SessionService : ISessionService, IDisposable
{
    private readonly ILogger<SessionService> _logger;
    private readonly CancellationTokenSource _monitoringCts = new();
    private readonly Task _monitoringTask;
    private readonly object _lock = new();
    private bool _disposed;
    private bool _monitoringStarted;
    private IntPtr _sessionNotificationHandle = IntPtr.Zero;

    public event EventHandler<SessionChangedEventArgs>? SessionChanged;

    public int CurrentSessionId { get; private set; } = -1;
    public SessionInfo? CurrentSession { get; private set; }
    public bool IsMonitoring => _monitoringStarted && !_monitoringCts.Token.IsCancellationRequested;

    public SessionService(ILogger<SessionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Get current session ID
        try
        {
            CurrentSessionId = NativeMethods.WTSGetActiveConsoleSessionId();
            _logger.LogDebug("Current console session ID: {SessionId}", CurrentSessionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get active console session ID");
            CurrentSessionId = GetCurrentProcessSessionId();
        }

        _monitoringTask = Task.Run(MonitorSessionsAsync, _monitoringCts.Token);
    }

    public async Task<IReadOnlyList<SessionInfo>> EnumerateSessionsAsync(CancellationToken cancellationToken = default)
    {
        var sessions = new List<SessionInfo>();
        
        try
        {
            var serverHandle = IntPtr.Zero; // WTS_CURRENT_SERVER_HANDLE
            
            // Try WTSEnumerateSessionsEx first (Windows Vista+)
            int level = 1;
            int filter = 0;
            IntPtr ppSessionInfo = IntPtr.Zero;
            int count = 0;
            
            bool success = NativeMethods.WTSEnumerateSessionsExW(serverHandle, ref level, filter, out ppSessionInfo, out count);
            
            if (!success)
            {
                // Fallback to WTSEnumerateSessions
                _logger.LogDebug("WTSEnumerateSessionsEx failed, falling back to WTSEnumerateSessions");
                success = NativeMethods.WTSEnumerateSessionsW(serverHandle, 0, 1, out ppSessionInfo, out count);
            }
            
            if (success && ppSessionInfo != IntPtr.Zero && count > 0)
            {
                try
                {
                    int structSize = Marshal.SizeOf<NativeMethods.WTS_SESSION_INFO_1>();
                    IntPtr currentPtr = ppSessionInfo;
                    
                    for (int i = 0; i < count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        var sessionInfo = Marshal.PtrToStructure<NativeMethods.WTS_SESSION_INFO_1>(currentPtr);
                        var session = await GetSessionDetailsAsync(sessionInfo.SessionId, cancellationToken);
                        if (session != null)
                        {
                            sessions.Add(session);
                        }
                        
                        currentPtr = IntPtr.Add(currentPtr, structSize);
                    }
                }
                finally
                {
                    NativeMethods.WTSFreeMemory(ppSessionInfo);
                }
            }
            else
            {
                _logger.LogWarning("Failed to enumerate sessions. Error: {Error}", Marshal.GetLastWin32Error());
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enumerating sessions");
        }
        
        return sessions.AsReadOnly();
    }

    public async Task<SessionInfo?> GetSessionAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        return await GetSessionDetailsAsync(sessionId, cancellationToken);
    }

    public int GetActiveConsoleSessionId()
    {
        try
        {
            return NativeMethods.WTSGetActiveConsoleSessionId();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get active console session ID");
            return -1;
        }
    }
public Task<IReadOnlyList<ProcessSessionInfo>> EnumerateProcessSessionsAsync(CancellationToken cancellationToken = default)
    {
        var processSessions = new List<ProcessSessionInfo>();
        
        try
        {
            var processes = Process.GetProcesses();
            int processed = 0;
            const int maxProcesses = 1000;
            
            foreach (var process in processes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                if (processed >= maxProcesses)
                {
                    _logger.LogDebug("Reached process limit ({MaxProcesses}), stopping enumeration", maxProcesses);
                    break;
                }
                
                try
                {
                    string processName;
                    int processId;
                    int sessionId;
                    string? userName = null;
                    string? domainName = null;
                    DateTime? startTime = null;
                    long memoryUsage = 0;
                    
                    try { processName = process.ProcessName; } catch { processName = "Unknown"; }
                    try { processId = process.Id; } catch { continue; }
                    
                    try
                    {
                        NativeMethods.ProcessIdToSessionId((uint)processId, out uint sid);
                        sessionId = (int)sid;
                    }
                    catch
                    {
                        sessionId = -1;
                    }
                    
                    try
                    {
                        startTime = process.StartTime;
                        memoryUsage = process.WorkingSet64;
                    }
                    catch { }
                    
                    try
                    {
                        using (var searcher = new System.Management.ManagementObjectSearcher(
                            "SELECT UserName, Domain FROM Win32_Process WHERE ProcessId = " + processId))
                        {
                            foreach (var obj in searcher.Get())
                            {
                                userName = obj["UserName"]?.ToString();
                                domainName = obj["Domain"]?.ToString();
                                break;
                            }
                        }
                    }
                    catch { }
                    
                    processSessions.Add(new ProcessSessionInfo(
                        processId,
                        processName,
                        sessionId,
                        userName,
                        domainName,
                        startTime,
                        memoryUsage
                    ));
                    
                    processed++;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to get info for process");
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate process sessions");
        }
        
        return Task.FromResult<IReadOnlyList<ProcessSessionInfo>>(processSessions.AsReadOnly());
    }

    public void StartMonitoring()
    {
        lock (_lock) { _monitoringStarted = true; }
    }

    public void StopMonitoring()
    {
        lock (_lock) { _monitoringStarted = false; }
    }
private Task<SessionInfo?> GetSessionDetailsAsync(int sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var serverHandle = IntPtr.Zero;
            string? userName = null;
            string? domainName = null;
            string? clientName = null;
            SessionState state = SessionState.Unknown;
            SessionType type = SessionType.Unknown;
            DateTime? connectTime = null;
            DateTime? disconnectTime = null;
            DateTime? lastInputTime = null;
            TimeSpan? idleTime = null;
            int processId = 0;
            
            if (QuerySessionString(serverHandle, sessionId, NativeMethods.WTSUserName, out string? uname))
                userName = uname;
            
            if (QuerySessionString(serverHandle, sessionId, NativeMethods.WTSDomainName, out string? dname))
                domainName = dname;
            
            if (QuerySessionString(serverHandle, sessionId, NativeMethods.WTSClientName, out string? cname))
                clientName = cname;
            
            if (QuerySessionInt(serverHandle, sessionId, NativeMethods.WTSConnectState, out int connectState))
            {
                state = MapConnectionState(connectState);
            }
            
            type = DetermineSessionType(sessionId, state, clientName);
            
            // WTSConnectTime and WTSDisconnectTime are not available in the current NativeMethods
            // These would require additional P/Invoke signatures
            // if (QuerySessionTime(serverHandle, sessionId, NativeMethods.WTSConnectTime, out DateTime? ctime))
            //     connectTime = ctime;
            
            // if (QuerySessionTime(serverHandle, sessionId, NativeMethods.WTSDisconnectTime, out DateTime? dtime))
            //     disconnectTime = dtime;
            
            if (QuerySessionTime(serverHandle, sessionId, NativeMethods.WTSIdleTime, out DateTime? itime))
            {
                lastInputTime = itime;
                if (itime.HasValue)
                    idleTime = DateTime.Now - itime.Value;
            }
            
            if (QuerySessionTime(serverHandle, sessionId, NativeMethods.WTSLogonTime, out DateTime? ltime))
            {
            }
            
            processId = GetSessionProcessId(sessionId);
            
            return Task.FromResult<SessionInfo?>(new SessionInfo(
                sessionId,
                userName,
                domainName,
                state,
                type,
                clientName,
                connectTime,
                disconnectTime,
                lastInputTime,
                idleTime,
                0,
                processId
            ));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get session info for {SessionId}", sessionId);
            return Task.FromResult<SessionInfo?>(null);
        }
    }

    private bool QuerySessionString(IntPtr serverHandle, int sessionId, int infoClass, out string? result)
    {
        result = null;
        try
        {
            if (NativeMethods.WTSQuerySessionInformationW(serverHandle, sessionId, infoClass, out IntPtr ppBuffer, out int bytesReturned))
            {
                if (ppBuffer != IntPtr.Zero && bytesReturned > 0)
                {
                    result = Marshal.PtrToStringUni(ppBuffer);
                    NativeMethods.WTSFreeMemory(ppBuffer);
                    return true;
                }
            }
        }
        catch { }
        return false;
    }

    private bool QuerySessionInt(IntPtr serverHandle, int sessionId, int infoClass, out int result)
    {
        result = 0;
        try
        {
            if (NativeMethods.WTSQuerySessionInformationW(serverHandle, sessionId, infoClass, out IntPtr ppBuffer, out int bytesReturned))
            {
                if (ppBuffer != IntPtr.Zero && bytesReturned >= 4)
                {
                    result = Marshal.ReadInt32(ppBuffer);
                    NativeMethods.WTSFreeMemory(ppBuffer);
                    return true;
                }
            }
        }
        catch { }
        return false;
    }

    private bool QuerySessionTime(IntPtr serverHandle, int sessionId, int infoClass, out DateTime? result)
    {
        result = null;
        try
        {
            if (NativeMethods.WTSQuerySessionInformationW(serverHandle, sessionId, infoClass, out IntPtr ppBuffer, out int bytesReturned))
            {
                if (ppBuffer != IntPtr.Zero && bytesReturned >= 8)
                {
                    long fileTime = Marshal.ReadInt64(ppBuffer);
                    NativeMethods.WTSFreeMemory(ppBuffer);
                    if (fileTime != 0 && NativeMethods.FileTimeToSystemTime(ref fileTime, out NativeMethods.SYSTEMTIME st))
                    {
                        result = new DateTime(st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds, DateTimeKind.Utc);
                        return true;
                    }
                }
            }
        }
        catch { }
        return false;
    }
private SessionState MapConnectionState(int state)
    {
        return state switch
        {
            NativeMethods.WTSActive => SessionState.Active,
            NativeMethods.WTSConnected => SessionState.Connected,
            // WTSConnectQuery - no direct mapping, use Connected
            NativeMethods.WTSConnectQuery => SessionState.Connected,
            NativeMethods.WTSShadow => SessionState.Shadow,
            NativeMethods.WTSDisconnected => SessionState.Disconnected,
            NativeMethods.WTSIdle => SessionState.Idle,
            // WTSListen - no direct mapping, use Disconnected
            NativeMethods.WTSListen => SessionState.Disconnected,
            // WTSReset - no direct mapping, use Unknown
            NativeMethods.WTSReset => SessionState.Unknown,
            // WTSDown - no direct mapping, use Unknown
            NativeMethods.WTSDown => SessionState.Unknown,
            // WTSInit - no direct mapping, use Unknown
            NativeMethods.WTSInit => SessionState.Unknown,
            _ => SessionState.Unknown
        };
    }

    private SessionType DetermineSessionType(int sessionId, SessionState state, string? clientName)
    {
        if (sessionId == 0)
            return SessionType.Services;
        if (sessionId == GetActiveConsoleSessionId() && state == SessionState.Active)
            return SessionType.Console;
        if (!string.IsNullOrEmpty(clientName))
            return SessionType.Remote;
        if (state == SessionState.Disconnected && !string.IsNullOrEmpty(clientName))
            return SessionType.Remote;
        if (state == SessionState.Active || state == SessionState.Connected || state == SessionState.Disconnected)
        {
            return SessionType.Console;
        }
        return SessionType.Unknown;
    }

    private int GetSessionProcessId(int sessionId)
    {
        try
        {
            var processes = Process.GetProcessesByName("winlogon");
            foreach (var p in processes)
            {
                try
                {
                    NativeMethods.ProcessIdToSessionId((uint)p.Id, out uint sid);
                    if (sid == sessionId)
                        return p.Id;
                }
                catch { }
            }
        }
        catch { }
        return 0;
    }

    private int GetCurrentProcessSessionId()
    {
        try { NativeMethods.ProcessIdToSessionId((uint)Environment.ProcessId, out uint sid); return (int)sid; }
        catch { return -1; }
    }
private bool TryRegisterSessionNotifications()
    {
        try
        {
            _sessionNotificationHandle = CreateNotificationWindow();
            if (_sessionNotificationHandle != IntPtr.Zero)
            {
                bool success = NativeMethods.WTSRegisterSessionNotification(_sessionNotificationHandle, NativeMethods.NOTIFY_FOR_ALL_SESSIONS);
                if (success)
                {
                    _logger.LogDebug("Registered for session notifications");
                    return true;
                }
                else
                {
                    _logger.LogWarning("WTSRegisterSessionNotification failed: {Error}", Marshal.GetLastWin32Error());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register session notifications");
        }
        return false;
    }

    private IntPtr CreateNotificationWindow()
    {
        const int HWND_MESSAGE = -3;
        
        var wc = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc = SessionNotificationWndProc,
            hInstance = GetModuleHandle(null),
            lpszClassName = "FreeSeat_SessionNotify_" + Guid.NewGuid().ToString("N")
        };
        
        if (RegisterClassEx(ref wc) != 0)
        {
            var hwnd = CreateWindowEx(
                0, wc.lpszClassName, "FreeSeat Session Notify",
                0, 0, 0, 0, 0,
                (IntPtr)HWND_MESSAGE, IntPtr.Zero,
                GetModuleHandle(null), IntPtr.Zero);
            
            if (hwnd != IntPtr.Zero)
            {
                SetWindowLongPtr(hwnd, -8, GCHandle.ToIntPtr(GCHandle.Alloc(this)));
                return hwnd;
            }
        }
        return IntPtr.Zero;
    }

    private void DestroyNotificationWindow()
    {
        if (_sessionNotificationHandle != IntPtr.Zero)
        {
            NativeMethods.WTSUnRegisterSessionNotification(_sessionNotificationHandle);
            DestroyWindow(_sessionNotificationHandle);
            _sessionNotificationHandle = IntPtr.Zero;
        }
    }

    private static IntPtr SessionNotificationWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == 0x02B1) // WM_WTSSESSION_CHANGE
        {
            try
            {
                var gch = GCHandle.FromIntPtr(GetWindowLongPtr(hWnd, -8));
                if (gch.IsAllocated && gch.Target is SessionService service)
                {
                    int sessionId = lParam.ToInt32();
                    SessionChangeReason reason = (SessionChangeReason)wParam.ToInt32();
                    
                    service.OnSessionChanged(reason, sessionId);
                }
            }
            catch { }
        }
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private void OnSessionChanged(SessionChangeReason reason, int sessionId)
    {
        if (!_monitoringStarted) return;
        
        _logger.LogInformation("Session change: {Reason} for session {SessionId}", reason, sessionId);
        
        var args = new SessionChangedEventArgs(reason, sessionId, DateTime.UtcNow, null);
        SessionChanged?.Invoke(this, args);
    }

    private Task MonitorSessionsAsync()
    {
        while (!_monitoringCts.Token.IsCancellationRequested)
        {
            try
            {
                _monitoringCts.Token.WaitHandle.WaitOne(1000);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _monitoringCts.Cancel();
        DestroyNotificationWindow();
        
        try
        {
            _monitoringTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch { }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public WndProcDelegate lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }
    
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);
    
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle,
        string lpClassName,
        string lpWindowName,
        uint dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam);
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
}

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace FreeSeat.Infrastructure.Sessions;

/// <summary>
/// Native Windows API methods for session management.
/// All methods are read-only and safe for diagnostic use.
/// </summary>
internal static class NativeMethods
{
    // Constants
    public const int WTS_CURRENT_SERVER_HANDLE = 0;
    public const int WTS_CURRENT_SESSION = -1;

    // WTS_INFO_CLASS values
    public const int WTSInitialProgram = 0;
    public const int WTSApplicationName = 1;
    public const int WTSWorkingDirectory = 2;
    public const int WTSOEMId = 3;
    public const int WTSSessionId = 4;
    public const int WTSUserName = 5;
    public const int WTSWinStationName = 6;
    public const int WTSDomainName = 7;
    public const int WTSConnectState = 8;
    public const int WTSClientBuildNumber = 9;
    public const int WTSClientName = 10;
    public const int WTSClientDirectory = 11;
    public const int WTSClientProductId = 12;
    public const int WTSClientHardwareId = 13;
    public const int WTSClientAddress = 14;
    public const int WTSClientDisplay = 15;
    public const int WTSClientProtocolType = 16;
    public const int WTSIdleTime = 17;
    public const int WTSLogonTime = 18;
    public const int WTSIncomingBytes = 19;
    public const int WTSOutgoingBytes = 20;
    public const int WTSIncomingFrames = 21;
    public const int WTSOutgoingFrames = 22;
    public const int WTSClientInfo = 23;
    public const int WTSSessionInfo = 24;
    public const int WTSSessionInfoEx = 25;
    public const int WTSConfigInfo = 25;

    // WTS_CONNECTSTATE_CLASS values
    public const int WTSActive = 0;
    public const int WTSConnected = 1;
    public const int WTSConnectQuery = 2;
    public const int WTSShadow = 3;
    public const int WTSDisconnected = 4;
    public const int WTSIdle = 5;
    public const int WTSListen = 6;
    public const int WTSReset = 7;
    public const int WTSDown = 8;
    public const int WTSInit = 9;

    // Session notification flags
    public const uint NOTIFY_FOR_THIS_SESSION = 0;
    public const uint NOTIFY_FOR_ALL_SESSIONS = 1;

    // Session notification messages
    public const int WTS_SESSION_LOGON = 0x5;
    public const int WTS_SESSION_LOGOFF = 0x6;
    public const int WTS_SESSION_LOCK = 0x7;
    public const int WTS_SESSION_UNLOCK = 0x8;
    public const int WTS_SESSION_REMOTE_CONTROL = 0x9;
    public const int WTS_CONSOLE_CONNECT = 0x1;
    public const int WTS_CONSOLE_DISCONNECT = 0x2;
    public const int WTS_REMOTE_CONNECT = 0x3;
    public const int WTS_REMOTE_DISCONNECT = 0x4;

    // Process creation flags
    public const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    public const uint CREATE_NO_WINDOW = 0x08000000;
    public const uint CREATE_NEW_CONSOLE = 0x00000010;
    public const uint CREATE_DEFAULT_ERROR_MODE = 0x04000000;
    public const uint NORMAL_PRIORITY_CLASS = 0x00000020;

    // Token access rights
    public const uint TOKEN_ASSIGN_PRIMARY = 0x0001;
    public const uint TOKEN_DUPLICATE = 0x0002;
    public const uint TOKEN_IMPERSONATE = 0x0004;
    public const uint TOKEN_QUERY = 0x0008;
    public const uint TOKEN_QUERY_SOURCE = 0x0010;
    public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    public const uint TOKEN_ADJUST_GROUPS = 0x0040;
    public const uint TOKEN_ADJUST_DEFAULT = 0x0080;
    public const uint TOKEN_ADJUST_SESSIONID = 0x0100;
    public const uint TOKEN_READ = 0x00020000 | TOKEN_QUERY;
    public const uint TOKEN_ALL_ACCESS = 0x000F0000 | TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE | TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID;

    // Structures
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WTS_SESSION_INFO_1
    {
        public uint ExecEnvId;
        public int SessionId;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pSessionName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pHostName;
        public int State;
        public uint SessionFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WTS_CLIENT_ADDRESS
    {
        public uint AddressFamily;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] Address;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WTS_CLIENT_DISPLAY
    {
        public int HorizontalResolution;
        public int VerticalResolution;
        public int ColorDepth;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public int bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct STARTUPINFOW
    {
        public int cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    // Process/Session APIs
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern int ProcessIdToSessionId(uint dwProcessId, out uint pSessionId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern int WTSGetActiveConsoleSessionId();

    // Session enumeration
    [DllImport("wtsapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool WTSEnumerateSessionsExW(
        IntPtr hServer,
        ref int pLevel,
        int Filter,
        out IntPtr ppSessionInfo,
        out int pCount);

    [DllImport("wtsapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool WTSEnumerateSessionsW(
        IntPtr hServer,
        int Reserved,
        int Version,
        out IntPtr ppSessionInfo,
        out int pCount);

    // Session information queries
    [DllImport("wtsapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool WTSQuerySessionInformationW(
        IntPtr hServer,
        int SessionId,
        int WTSInfoClass,
        out IntPtr ppBuffer,
        out int pBytesReturned);

    // Session notifications
    [DllImport("wtsapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool WTSRegisterSessionNotification(IntPtr hWnd, uint dwFlags);

    [DllImport("wtsapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool WTSUnRegisterSessionNotification(IntPtr hWnd);

    // Token APIs
    [DllImport("wtsapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool WTSQueryUserToken(int SessionId, out IntPtr phToken);

    // Memory management
    [DllImport("wtsapi32.dll", SetLastError = true)]
    public static extern void WTSFreeMemory(IntPtr pMemory);

    // User environment APIs
    [DllImport("userenv.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

    [DllImport("userenv.dll", SetLastError = true)]
    public static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

    // Process creation
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool CreateProcessAsUserW(
        IntPtr hToken,
        string? lpApplicationName,
        string? lpCommandLine,
        ref SECURITY_ATTRIBUTES lpProcessAttributes,
        ref SECURITY_ATTRIBUTES lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string? lpCurrentDirectory,
        ref STARTUPINFOW lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool CreateProcessWithTokenW(
        IntPtr hToken,
        uint dwLogonFlags,
        string? lpApplicationName,
        string? lpCommandLine,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string? lpCurrentDirectory,
        ref STARTUPINFOW lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    // Handle management
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool DuplicateTokenEx(
        IntPtr hExistingToken,
        uint dwDesiredAccess,
        ref SECURITY_ATTRIBUTES lpTokenAttributes,
        int ImpersonationLevel,
        int TokenType,
        out IntPtr phNewToken);

    // Time conversion
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool FileTimeToSystemTime(ref long lpFileTime, out SYSTEMTIME lpSystemTime);

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEMTIME
    {
        public ushort wYear;
        public ushort wMonth;
        public ushort wDayOfWeek;
        public ushort wDay;
        public ushort wHour;
        public ushort wMinute;
        public ushort wSecond;
        public ushort wMilliseconds;
    }

    // Security constants
    public const int SECURITY_IMPERSONATION = 2;
    public const int TOKEN_TYPE_PRIMARY = 1;
    public const int TOKEN_TYPE_IMPERSONATION = 2;
}

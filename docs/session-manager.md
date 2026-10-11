# Session Manager Architecture

## Overview

This document describes the FreeSeat Session Manager architecture, which provides a clean, safe abstraction for Windows session management. The Session Manager is designed to:

1. Discover and track Windows sessions
2. Monitor session lifecycle changes
3. Provide a safe process launching mechanism
4. Recover from missed notifications via reconciliation
5. Prepare the foundation for multi-seat management

## Architecture

```
+-------------------------------------------------------------+
¦                      ISessionManager                        ¦
¦  (FreeSeat.Core.Sessions.Interfaces)                        ¦
+-------------------------------------------------------------+
                            ¦
        +-------------------+-------------------+
        ?                   ?                   ?
+---------------+   +---------------+   +---------------+
¦SessionService ¦   ¦SessionState   ¦   ¦SessionLauncher¦
¦(Infrastructure)¦   ¦Tracker        ¦   ¦(Infrastructure)¦
+---------------+   +---------------+   +---------------+
        ¦                   ¦                   ¦
        ?                   ?                   ?
+-------------------------------------------------------------+
¦                    WTSAPI32 / advapi32                       ¦
¦         (Windows Terminal Services APIs)                    ¦
+-------------------------------------------------------------+
```

### Core Components

| Component | Location | Responsibility |
|-----------|----------|----------------|
| `ISessionManager` | Core/Interfaces | Public interface |
| `SessionManager` | Infrastructure/Sessions | Main coordinator |
| `SessionService` | Infrastructure/Sessions | WTS session enumeration & notifications |
| `SessionStateTracker` | Infrastructure/Sessions | State tracking & event history |
| `SessionLauncher` | Infrastructure/Sessions | Safe process launching |
| `SessionRecoveryService` | Infrastructure/Sessions | Reconciliation & recovery |

### Data Flow

```
Windows Session Changes
         ¦
         ?
WTS Notifications (WM_WTSSESSION_CHANGE)
         ¦
         ?
SessionService.SessionChanged event
         ¦
         ?
SessionManager.OnSessionServiceEvent()
         ¦
         +-------------------------------------+
         ?                  ?                  ?
SessionStateTracker   SessionManager     SessionManager
.UpdateSession()      .RaiseSessionsChanged() .RaiseSessionEvent()
         ¦                  ¦                  ¦
         ?                  ?                  ?
   Internal State    SessionsChanged      SessionEvent
   (ManagedSession)    Event (UI)         (UI/Logging)
```

## Session Lifecycle

### State Mapping

The Session Manager maps Windows session states (`SessionState`) to FreeSeat\'s managed lifecycle states (`SessionLifecycleState`):

| Windows State | FreeSeat Lifecycle | Description |
|---------------|-------------------|-------------|
| Unknown | Unknown | State not determined |
| Active | Active | User logged on, connected |
| Connected | Detected | Connected but not active |
| Disconnected | Disconnected | Logged on, no client |
| Idle | Active | Idle but connected |
| Logon | LoggingOn | In logon process |
| Logoff | LoggingOff | In logoff process |
| Locked | Locked | Workstation locked |
| Unlocked | Unlocked | Just unlocked |
| RemoteControl | Active | Remote control session |
| Shadow | Active | Shadow session |

### Valid Transitions

```
Detected -> Active
Active <-> Locked
Active <-> Disconnected
Active -> LoggingOff -> LoggedOff
LoggedOff -> (removed)
Unknown -> Detected
Any -> Unavailable (on error)
```

### Launch Eligibility

A session can launch processes when in state:
- `Active`
- `Unlocked`
- `Detected`

## Session Events

The Session Manager raises strongly-typed events for all lifecycle changes:

| Event Type | When Raised |
|------------|-------------|
| `SessionDetected` | New session discovered |
| `SessionConnected` | Session connected (remote/console) |
| `SessionDisconnected` | Session disconnected |
| `SessionLocked` | Workstation locked |
| `SessionUnlocked` | Workstation unlocked |
| `SessionLoggedOff` | User logged off |
| `SessionRemoved` | Session no longer tracked |
| `SessionStateChanged` | Generic state change |
| `ProcessLaunched` | Process successfully launched |
| `ProcessLaunchFailed` | Process launch failed |

Each event contains:
- Session ID
- Previous state (if applicable)
- New state (if applicable)
- Timestamp
- Human-readable details

## Reconciliation

### Purpose

Windows notifications can occasionally be missed or arrive during application startup. The reconciliation mechanism periodically synchronizes the internal state with actual Windows sessions.

### Algorithm

1. Enumerate actual Windows sessions via `WTSEnumerateSessionsExW`
2. Convert to `ManagedSession` for comparison
3. Compare with tracked sessions using `SessionSnapshot.CompareTo()`
4. Detect additions, removals, and changes
5. Update internal state
6. Raise appropriate events

### Configuration

- **Interval**: 300 seconds (5 minutes) default, minimum 60 seconds
- **Enabled**: Via `SessionManagerSettings.MonitoringEnabled`
- **Manual Trigger**: `ISessionManager.ReconcileAsync()`

### Safety

- Never deletes sessions without verification
- Uses stable Session IDs for matching
- Preserves event history
- Logs all reconciliation actions

## Process Launching

### Security Model

The Session Manager implements a **safe, authorized-only** process launching mechanism:

```
SessionLaunchRequest (validated)
         ¦
         ?
WTSQueryUserToken(sessionId)
         ¦
         ?
DuplicateTokenEx(TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_QUERY | TOKEN_IMPERSONATE)
         ¦
         ?
CreateEnvironmentBlock()
         ¦
         ?
CreateProcessAsUserW()
         ¦
         ?
SessionLaunchResult
```

### Validation Rules

1. **Executable must exist** and be a `.exe` file
2. **System executables blocked** except `notepad.exe` and `calc.exe` (developer test tools)
3. **Session must exist** and be in launchable state
4. **No shell commands** - direct executable execution only
5. **No arbitrary arguments** - controlled via `Arguments` property

### Supported Scenarios

- Launching into **current user\'s session** (always works)
- Launching into **other user\'s session** (requires appropriate privileges)
- **Developer test launch** (notepad.exe) when Developer Mode enabled

### Explicitly NOT Supported

- Stealing another user\'s token
- Bypassing authentication
- Creating unauthorized sessions
- Accessing passwords/credentials
- Impersonating without authorization
- Modifying security settings
- Launching hidden processes
- Privilege escalation

## Developer Mode

### Purpose

Provides a safe diagnostic action for testing process launching during development.

### Behavior

- **Default**: OFF
- **Setting**: `SessionManagerSettings.DeveloperMode` (persisted in settings.json)
- **UI**: Toggle button in Sessions view
- **Action**: "Test Launch" button launches `notepad.exe` in selected session
- **Safety**: Clearly labeled "This launches a test process only. No session configuration is changed."

### Usage

1. Enable Developer Mode in Sessions view
2. Select target session from dropdown
3. Click "Test Launch (notepad.exe)"
4. Observe result in Launch Status

## Settings

### SessionManagerSettings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MonitoringEnabled` | bool | true | Enable session monitoring |
| `ReconciliationIntervalSeconds` | int | 300 | Reconciliation interval (min 60) |
| `MaxEventHistory` | int | 100 | Maximum events to retain |
| `DeveloperMode` | bool | false | Enable test launch diagnostics |

### Persistence

Stored in `%LOCALAPPDATA%\FreeSeat\settings.json`:

```json
{
  "SessionManager": {
    "MonitoringEnabled": true,
    "ReconciliationIntervalSeconds": 300,
    "MaxEventHistory": 100,
    "DeveloperMode": false
  }
}
```

## Error Handling

### Strategy

- **Never crash** - all exceptions caught and logged
- **Structured failures** - `SessionLaunchResult` with error code/message
- **No silent retries** - privileged operations fail fast
- **Graceful degradation** - monitoring continues after individual failures

### Common Errors

| Scenario | Error Code | Message |
|----------|------------|---------|
| Invalid session ID | 0 | "Invalid session ID" |
| Executable not found | 0 | "Executable not found: <path>" |
| Not an .exe file | 0 | "Only .exe files are allowed for launch" |
| System executable blocked | 0 | "Launching system executables is not allowed" |
| Token query failed | Win32 error | "Failed to get user token for session X" |
| Token duplicate failed | Win32 error | "Failed to duplicate token for session X" |
| Process creation failed | Win32 error | "Failed to create process in session X" |
| Access denied | 5 | "Access denied" |
| Session not launchable | 0 | "Session not found or not in launchable state" |

## Logging

### Structured Events

| Event | Level | Context |
|-------|-------|---------|
| Session discovered | Information | SessionId, DisplayName |
| Session connected | Information | SessionId, State |
| Session disconnected | Information | SessionId, PreviousState |
| Session locked | Information | SessionId |
| Session unlocked | Information | SessionId |
| Session logged off | Information | SessionId |
| Session removed | Information | SessionId |
| Launch attempted | Information | SessionId, Executable |
| Launch succeeded | Information | SessionId, ProcessId, Executable |
| Launch failed | Warning | SessionId, Executable, ErrorCode, ErrorMessage |
| Reconciliation completed | Information | Added, Removed, Changed counts |
| Reconciliation failed | Error | Exception details |

### Privacy

- **Never logs**: Passwords, tokens, credentials, access token contents
- **Minimal personal info**: Only username/domain for session identification
- **Local only**: No telemetry or remote logging

## API Reference

### ISessionManager

```csharp
public interface ISessionManager
{
    int CurrentSessionId { get; }
    ManagedSession? CurrentSession { get; }
    IReadOnlyList<ManagedSession> Sessions { get; }
    bool IsMonitoring { get; }
    IReadOnlyList<SessionEventArgs> RecentEvents { get; }
    
    ManagedSession? GetSession(int sessionId);
    SessionLifecycleState GetSessionState(int sessionId);
    
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);
    Task StopMonitoringAsync(CancellationToken cancellationToken = default);
    Task ReconcileAsync(CancellationToken cancellationToken = default);
    Task<bool> CanLaunchProcessAsync(int sessionId, CancellationToken cancellationToken = default);
    Task<SessionLaunchResult> LaunchProcessAsync(SessionLaunchRequest request, CancellationToken cancellationToken = default);
    
    event EventHandler<SessionEventArgs> SessionEvent;
    event EventHandler<SessionCollectionChangedEventArgs> SessionsChanged;
}
```

### SessionLaunchRequest

```csharp
public record SessionLaunchRequest(
    int SessionId,
    string ExecutablePath,
    string? Arguments = null,
    string? WorkingDirectory = null,
    IReadOnlyDictionary<string, string>? EnvironmentVariables = null,
    bool RunAsCurrentUser = true
);
```

### SessionLaunchResult

```csharp
public record SessionLaunchResult(
    bool Success,
    int? ProcessId,
    int SessionId,
    int? ErrorCode = null,
    string? ErrorMessage = null,
    DateTime? Timestamp = null
);
```

## Limitations

### Current Limitations

1. **Read-only session management** - cannot create/terminate sessions
2. **Launch restrictions** - cannot launch into all sessions (Windows security)
3. **No input isolation** - keyboard/mouse not yet routed per session
4. **No display assignment** - monitors not yet assigned per session
4. **Single-user launch** - cannot launch as different user without credentials

### Future Extensions (Milestone 4+)

- Seat-to-session mapping
- Input device assignment
- Display ownership
- Independent desktop creation
- Audio isolation

## Testing

### Unit Tests (Planned)

- `ManagedSession` state transitions
- `SessionLifecycleState` mapping
- `SessionLaunchRequest` validation
- `SessionLaunchResult` creation
- `SessionStateTracker` reconciliation logic
- `SessionRecoveryService` timer behavior

### Integration Tests (Manual)

- Session enumeration on multi-user system
- Session lock/unlock notifications
- Test launch in current session
- Test launch in other session (requires privileges)
- Reconciliation after missed notifications

## Related Documents

- [Architecture](architecture.md) - Overall system architecture
- [Roadmap](roadmap.md) - Project roadmap
- [Windows Session Research](windows-session-research.md) - WTS API research
- [Input Architecture Research](input-architecture-research.md) - Input routing research

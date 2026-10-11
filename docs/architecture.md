# FreeSeat Architecture

## Overview

FreeSeat follows a clean architecture pattern with clear separation of concerns across four main projects:

```
FreeSeat/
+-- FreeSeat.App/           # Presentation Layer (WPF + MVVM)
+-- FreeSeat.Core/          # Domain Layer (Business Logic)
+-- FreeSeat.Infrastructure/# Infrastructure Layer (External Services)
+-- FreeSeat.Setup/         # Installer (Inno Setup)
```

---

## Project Structure

### FreeSeat.App (WPF Application)

**Purpose:** User interface and presentation logic

**Key Components:**
- **Views/** - XAML-based UI views (Dashboard, Seats, Diagnostics, Settings, About, Sessions)
- **ViewModels/** - MVVM view models using CommunityToolkit.Mvvm
- **Styles/** - Theme system (Colors.xaml, Controls.xaml) supporting light/dark modes
- **Converters/** - XAML value converters
- **App.xaml.cs** - Application entry point, DI container configuration

**Dependencies:** FreeSeat.Core, FreeSeat.Infrastructure

---

### FreeSeat.Core (Domain Logic)

**Purpose:** Core business logic, domain models, and interfaces

**Key Components:**
- **AppInfo.cs** - Application metadata (name, version, creator, branding)
- **Diagnostics/** - System diagnostics and compatibility checking
  - `IDiagnosticsService` - Interface for diagnostics collection
  - `DiagnosticsService` - WMI-based implementation
  - `SystemDiagnostics` - Diagnostic data model
  - `CompatibilityResult` - System compatibility check results
- **Hardware/** - Hardware detection models and interfaces
  - `IHardwareDetectionService` - Interface for hardware enumeration
  - Models: `CpuInfo`, `GpuDevice`, `MonitorDevice`, `KeyboardDevice`, `MouseDevice`, `MemoryInfo`, `StorageInfo`, `HardwareSnapshot`, `DeviceType`, `ConnectionStatus`
- **Sessions/** - Session management models and interfaces
  - **Interfaces:** `ISessionService`, `ISessionManager`
  - **Models:** 
    - `SessionInfo` - Windows session information
    - `SessionState` - Windows session states
    - `SessionType` - Session types (Console, Remote, Services)
    - `SessionChangedEventArgs` - Session change notifications
    - `ProcessSessionInfo` - Process-to-session mapping
    - `ManagedSession` - FreeSeat-managed session with lifecycle
    - `SessionIdentity` - User/session identification
    - `SessionLifecycleState` - FreeSeat session lifecycle states
    - `SessionLaunchRequest` - Validated process launch request
    - `SessionLaunchResult` - Structured launch result
    - `SessionEventArgs` - Strongly-typed session events
    - `SessionSnapshot` - Point-in-time session state snapshot
- **Seats/** - Seat configuration models and interfaces
  - **Interfaces:** `ISeatConfigurationService`
  - **Models:**
    - `Seat` - Logical seat with assignments and lifecycle
    - `SeatId` - Stable GUID-based seat identifier
    - `SeatAssignment` - Device/session assignments for a seat
    - `SeatConfiguration` - Complete seat configuration
    - `SeatStatus` - Seat configuration status enum
    - `SeatValidationIssue` - Validation issue details
    - `SeatValidationResult` - Validation result with issues
    - `SeatLifecycleState` - Seat lifecycle state enum
    - `SeatSnapshot` - Point-in-time backup
    - `SeatConfigurationChangedEventArgs` - Configuration change events
    - `SeatValidationChangedEventArgs` - Validation change events

**Dependencies:** None (pure .NET)

---

### FreeSeat.Infrastructure (External Services)

**Purpose:** Implementation of external concerns (persistence, system integration)

**Key Components:**
- **Configuration/** - Settings management
  - `ISettingsService` - Interface for settings persistence
  - `SettingsService` - JSON file-based implementation
  - `AppSettings` - Settings data model (General, Appearance, Startup, SessionManager, SeatConfiguration)
- **Hardware/** - Hardware detection implementations
  - `HardwareDetectionService` - Main hardware detection coordinator
  - `CpuDetector`, `GpuDetector`, `MonitorDetector`, `HidDeviceDetector`, `MemoryDetector`, `StorageDetector`
- **Sessions/** - Session management implementations
  - `SessionService` - WTS-based session enumeration and monitoring
  - `SessionManager` - Main session manager coordinator
  - `SessionStateTracker` - Session state tracking and event history
  - `SessionLauncher` - Safe process launching in specific sessions
  - `SessionRecoveryService` - Periodic reconciliation and recovery
  - `NativeMethods` - P/Invoke signatures for WTSAPI32, advapi32, userenv, kernel32
- **Seats/** - Seat configuration implementations
  - `SeatConfigurationService` - Main seat configuration service
- **ServiceCollectionExtensions.cs** - DI registration helpers

**Dependencies:** FreeSeat.Core, Newtonsoft.Json, Microsoft.Extensions.Logging, Microsoft.Extensions.Hosting, Microsoft.Extensions.Options

---

### FreeSeat.Setup (Installer)

**Purpose:** User-facing Windows installer

**Technology:** Inno Setup 6

**Key Files:**
- `FreeSeat.iss` - Inno Setup script
- Custom wizard images (branding)

**Features:**
- Modern wizard-style UI
- System compatibility checks (read-only)
- Progress tracking with responsive UI
- Start Menu + optional Desktop shortcuts
- Uninstaller entry
- Admin elevation for Program Files installation

---

## Data Flow

```
User Action (UI)
    ¦
    ?
ViewModel (Commands/Properties)
    ¦
    ?
Domain Service (Core/Infrastructure)
    ¦
    ?
External Systems (WMI, File System, Registry, WTSAPI32)
    ¦
    ?
Result ? ViewModel ? View (Data Binding)
```

---

## Dependency Injection

Configured in `App.xaml.cs`:

```csharp
services.AddInfrastructure();  // Core + Infrastructure services
services.AddSingleton<MainWindow>();
services.AddSingleton<MainViewModel>();
services.AddTransient<DashboardViewModel>();
services.AddTransient<SeatsViewModel>();
services.AddTransient<DiagnosticsViewModel>();
services.AddTransient<SettingsViewModel>();
services.AddTransient<AboutViewModel>();
services.AddTransient<HardwareViewModel>();
services.AddTransient<SessionViewModel>();
```

**Lifetime Management:**
- Singleton: MainWindow, MainViewModel, DiagnosticsService, SettingsService, SessionService, SessionStateTracker, SessionLauncher, SessionRecoveryService, SessionManager, SeatConfigurationService, HardwareDetectionService
- Transient: Page ViewModels (created on navigation)

---

## Session Manager Architecture

The Session Manager provides a clean abstraction for Windows session management:

### Component Hierarchy

```
ISessionManager (Core/Interfaces)
    ¦
    ?
SessionManager (Infrastructure/Sessions)
    ¦
    +-- SessionService (Infrastructure/Sessions) --? WTSAPI32 (Session enumeration, notifications)
    +-- SessionStateTracker (Infrastructure/Sessions) --? ManagedSession state & event history
    +-- SessionLauncher (Infrastructure/Sessions) --? advapi32/userenv (Process launching)
    +-- SessionRecoveryService (Infrastructure/Sessions) --? Periodic reconciliation
```

### Key Design Principles

1. **Read-only monitoring** - SessionService only observes, never modifies
2. **Core/Infrastructure separation** - Models and interfaces in Core, implementations in Infrastructure
3. **Safe process launching** - Validated requests, authorized-only, structured failures
4. **Reconciliation** - Periodic sync with actual Windows sessions
5. **Developer Mode** - Off by default, enables test launch only

### Session Lifecycle

```
Windows SessionState  ?  SessionLifecycleState  ?  ManagedSession.State
     ¦                        ¦                        ¦
     ?                        ?                        ?
Unknown                 Unknown                 Unknown
Active                  Active                  Active (CanLaunch)
Connected               Detected                Detected
Disconnected            Disconnected            Disconnected
Idle                    Active                  Active (CanLaunch)
Logon                   LoggingOn               LoggingOn
Logoff                  LoggingOff              LoggingOff
Locked                  Locked                  Locked
Unlocked                Unlocked                Unlocked (CanLaunch)
RemoteControl           Active                  Active
Shadow                  Active                  Active
```

### Process Launch Security

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
SessionLaunchResult (Success/ProcessId or Failure/ErrorCode/ErrorMessage)
```

**Validation Rules:**
- Executable must exist and be .exe
- System executables blocked (except notepad.exe, calc.exe for dev testing)
- Session must exist and be in launchable state
- No shell commands, direct execution only

---

## Seat Configuration Architecture

The Seat Configuration system provides a clean abstraction for mapping hardware devices and Windows sessions to logical "seats".

### Component Hierarchy

```
ISeatConfigurationService (Core/Interfaces)
    ¦
    ?
SeatConfigurationService (Infrastructure/Seats)
    ¦
    +-- HardwareDetectionService (Infrastructure/Hardware) --? WMI/WinRT (Device enumeration)
    +-- SessionManager (Infrastructure/Sessions) --? ISessionManager (Session enumeration)
    +-- AppSettings (JSON) --? Persistent storage
```

### Key Design Principles

1. **Configuration only** - Records intent, no actual isolation/routing
2. **Core/Infrastructure separation** - Models and interfaces in Core, implementations in Infrastructure
3. **Stable IDs** - GUID-based SeatIds and persistent device IDs
4. **Validation engine** - Real-time validation with conflict detection
5. **Developer Mode** - Off by default, enables test launch only

### Seat Lifecycle

```
Unconfigured ? Incomplete ? Ready
    ¦            ¦            ¦
    ?            ?            ?
Disabled   DeviceUnavail  SessionUnavail
              ¦               ¦
              +---------------+
                      ?
                    Conflict
                      ¦
                      ?
                   Disabled
```

### Device/Session Discovery Integration

**Hardware Integration:**

```csharp
// Available devices for assignment
var monitors = seatService.GetAvailableMonitors();
var keyboards = seatService.GetAvailableKeyboards();
var mice = seatService.GetAvailableMice();
```

**Session Integration:**

```csharp
// Available sessions for assignment
var sessions = seatService.GetAvailableSessions();
```

### Validation Engine

A seat is `Ready` only when:
1. Seat is enabled
2. Monitor assigned and connected
3. Keyboard assigned and connected
4. Mouse assigned and connected
5. Session assigned and in launchable state (Active, Unlocked, Detected)
6. No conflicts with other seats

### Conflict Detection

Detects when a device/session is assigned to multiple enabled seats.

### Disconnection Handling

When a configured device disconnects:
- Assignment is **preserved** (not deleted)
- Seat status changes to `DeviceUnavailable`
- Reconnection with same ID automatically restores availability
- Different ID = treated as new device (old assignment marked unavailable)

---

## Diagnostics System

**IDiagnosticsService** provides:
1. `CollectDiagnosticsAsync()` - Full system snapshot
2. `CheckCompatibilityAsync()` - Read-only compatibility checks

**Checks Performed:**
- Windows Version (10/11 detection)
- Architecture (x64 required)
- CPU (cores, model)
- RAM (total available)
- Disk Space (system drive)
- GPU (dedicated, VRAM, driver)

**Output:** `SystemDiagnostics` record with formatted `ToString()` for clipboard

---

## Settings Persistence

**Location:** `%LOCALAPPDATA%\FreeSeat\settings.json`

**Schema:**
```json
{
  "General": {
    "Language": "en-US",
    "CheckForUpdates": true,
    "SendAnonymousUsageData": false
  },
  "Appearance": {
    "Theme": "System",
    "UseAccentColor": true
  },
  "Startup": {
    "StartMinimized": false,
    "StartWithWindows": false
  },
  "SessionManager": {
    "MonitoringEnabled": true,
    "ReconciliationIntervalSeconds": 300,
    "MaxEventHistory": 100,
    "DeveloperMode": false
  },
  "SeatConfiguration": {
    "SeatConfiguration": "{\"Version\":1,\"Seats\":[...],...}"
  }
}
```

**Loading:** On app startup via `SettingsInitializer` (IHostedService)
**Saving:** On app shutdown + individual property changes

---

## Versioning

**Central Version:** `Directory.Build.props`
- Version: 0.1.0
- AssemblyVersion: 0.1.0.0
- FileVersion: 0.1.0.0
- InformationalVersion: 0.1.0

**Increment Strategy:**
- Milestone releases: 0.1.0 ? 0.2.0 ? 0.3.0...
- Patch fixes: 0.1.0 ? 0.1.1
- 1.0.0 = First stable multi-seat release

---

## Build Pipeline

```
dotnet restore
    ¦
    ?
dotnet build FreeSeat.sln -c Release
    ¦
    +-- FreeSeat.Core.dll
    +-- FreeSeat.Infrastructure.dll
    +-- FreeSeat.App.exe + dependencies
            ¦
            ?
dotnet publish FreeSeat.App -c Release -r win-x64 -o dist
            ¦
            ?
iscc FreeSeat.iss (Inno Setup)
            ¦
            ?
dist/FreeSeat-Setup.exe
```

---

## Testing Strategy

**Unit Tests** (FreeSeat.Tests):
- AppInfo version/constants
- Diagnostics generation
- Settings serialization
- Compatibility check logic
- Seat models (12 tests)
- Seat assignments (6 tests)
- Seat validation (8 tests)
- Seat configuration (6 tests)
- Session tests (existing)
- Settings tests (existing)
- Installer tests (existing)

**Total: 48 tests**

**Integration Tests:** (Future)
- Installer verification
- Settings persistence round-trip
- Session enumeration
- Process launching
- Seat configuration round-trip

**Manual Testing:**
- Installer UI flow
- Theme switching
- Navigation
- Diagnostics copy
- Session monitoring
- Test launch (Developer Mode)
- Seat configuration UI

---

## Future Architecture Considerations

### Milestone 5+ Additions

- **Input Isolation** - Keyboard/mouse filtering per seat
- **Display Ownership** - Monitor assignment per session
- **Audio Isolation** - Per-seat audio endpoints
- **GPU Partitioning** - Time-slice or physical GPU per seat

### Security Boundaries

- Core: No external dependencies, pure logic
- Infrastructure: File system, registry, WMI, WTSAPI32 (read-only + authorized launch)
- App: UI only, no direct system access
- Setup: Admin only for installation, not runtime

---

## Coding Standards

- **Nullable Reference Types:** Enabled project-wide
- **Implicit Usings:** Enabled
- **Modern C#:** Records, pattern matching, primary constructors
- **Naming:** PascalCase for public, camelCase for private
- **Async:** All I/O operations async with CancellationToken
- **Logging:** Microsoft.Extensions.Logging throughout
- **Error Handling:** Try/catch with logging, no silent failures

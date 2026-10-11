# Seat Configuration Architecture

## Overview

This document describes the FreeSeat Seat Configuration architecture, which provides a clean abstraction for mapping hardware devices and Windows sessions to logical "seats".

A **Seat** represents a future independent workstation that will be fully isolated in later milestones. Currently, the configuration only records the relationships between:

- Monitors
- Keyboards
- Mice
- Windows Sessions

And logical seat identifiers. **No actual isolation or routing is performed** - this milestone is configuration only.

## Architecture

```
+-------------------------------------------------------------+
¦                    ISeatConfigurationService                 ¦
¦  (FreeSeat.Core.Seats.Interfaces)                            ¦
+-------------------------------------------------------------+
                            ¦
        +-------------------+-------------------+
        ?                   ?                   ?
+---------------+   +---------------+   +---------------+
¦SeatConfigura- ¦   ¦ Hardware      ¦   ¦ SessionManager¦
¦tionService    ¦   ¦DetectionService¦   ¦(ISessionMgr)  ¦
¦(Infrastructure)¦   ¦(Infrastructure)¦   ¦(Infrastructure)¦
+---------------+   +---------------+   +---------------+
        ¦                   ¦                   ¦
        ?                   ?                   ?
+-------------------------------------------------------------+
¦                    AppSettings (JSON)                        ¦
¦         Persistent seat configuration storage                ¦
+-------------------------------------------------------------+
```

### Core Components

| Component | Location | Responsibility |
|-----------|----------|----------------|
| `ISeatConfigurationService` | Core/Interfaces | Public interface |
| `SeatConfigurationService` | Infrastructure/Seats | Main implementation |
| `Seat` | Core/Seats/Models | Seat domain model |
| `SeatAssignment` | Core/Seats/Models | Device/session assignments |
| `SeatValidationResult` | Core/Seats/Models | Validation results |
| `SeatConfiguration` | Core/Seats/Models | Complete configuration |
| `SeatStatus` | Core/Seats/Models | Seat status enum |
| `SeatValidationIssue` | Core/Seats/Models | Validation issue details |
| `SeatLifecycleState` | Core/Seats/Models | Seat lifecycle enum |
| `SeatSnapshot` | Core/Seats/Models | Point-in-time backup |
| `SeatViewModel` | App/ViewModels | UI view model |
| `SeatsViewModel` | App/ViewModels | Main UI coordinator |

### Data Flow

```
Hardware Detection          Session Manager
     ¦                          ¦
     ?                          ?
HardwareSnapshot          ManagedSessions
     ¦                          ¦
     +--------------------------+
                 ?
        SeatConfigurationService
                 ¦
         +---------------+
         ?               ?
    Validation      Assignment
    Engine          Management
         ¦               ¦
         +---------------+
                 ?
        AppSettings (JSON)
                 ¦
                 ?
        SeatViewModel / UI
```

## Seat Model

### Seat

A **Seat** represents a configured workstation with:

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `SeatId` | Stable GUID identifier |
| `Name` | `string` | User-friendly display name |
| `IsEnabled` | `bool` | Whether the seat is active |
| `Assignment` | `SeatAssignment` | Device/session mappings |
| `ValidationResult` | `SeatValidationResult?` | Current validation state |
| `UpdatedAt` | `DateTime` | Last modification timestamp |

### SeatId

A **SeatId** is a stable GUID-based identifier that persists across application restarts:

```csharp
var seatId = SeatId.New();        // Generate new
var seatId = SeatId.Parse(guid);  // Parse from string
var success = SeatId.TryParse(str, out var id); // Safe parse
```

### SeatAssignment

Records the relationship between a seat and its devices/session:

| Property | Type | Description |
|----------|------|-------------|
| `MonitorId` | `string?` | Persistent monitor device ID |
| `KeyboardId` | `string?` | Persistent keyboard device ID |
| `MouseId` | `string?` | Persistent mouse device ID |
| `SessionId` | `int?` | Windows session ID |

**Validation Properties:**

- `IsComplete` - All four assignments present
- `HasAnyAssignment` - At least one assignment present

### SeatStatus

Represents the overall configuration state of a seat:

| Status | Description |
|--------|-------------|
| `Unconfigured` | Seat has no configuration |
| `Ready` | Fully configured and ready |
| `Incomplete` | Missing required assignments |
| `DeviceUnavailable` | Configured device disconnected |
| `SessionUnavailable` | Configured session unavailable |
| `Conflict` | Device/session assigned to multiple seats |
| `Disabled` | Seat explicitly disabled |

### SeatValidationResult

Contains the outcome of seat validation:

| Property | Type | Description |
|----------|------|-------------|
| `Status` | `SeatStatus` | Overall validation status |
| `Issues` | `ImmutableArray<SeatValidationIssue>` | Specific validation issues |
| `ValidatedAt` | `DateTime` | When validation was performed |
| `IsValid` | `bool` | True if status is `Ready` |

### SeatValidationIssue

Specific validation problems:

| Issue Type | Description |
|------------|-------------|
| `MissingRequired` | Required assignment missing |
| `DeviceUnavailable` | Assigned device currently disconnected |
| `SessionUnavailable` | Assigned session unavailable |
| `Conflict` | Device/session assigned to multiple seats |
| `Invalid` | Generic invalid configuration |

## Configuration Persistence

### Storage

Seat configurations are persisted in the existing `AppSettings` JSON file:

```json
{
  "General": { ... },
  "Appearance": { ... },
  "Startup": { ... },
  "SessionManager": { ... },
  "SeatConfiguration": {
    "SeatConfiguration": "{\"Version\":1,\"Seats\":[...],...}"
  }
}
```

The configuration is stored as a JSON string within the `SeatConfigurationSettings` class.

### Versioning

Configuration includes a version number for future migrations:

```csharp
public int Version { get; init; } = 1;
```

Current version is 1. Future versions will implement migration logic.

### Import/Export

**Export:**

```csharp
string json = await seatService.ExportAsync();
```

**Import:**

```csharp
var result = await seatService.ImportAsync(json);
// result.Success, result.Configuration, result.ValidationIssues
```

**Import Safety:**

- Imported JSON is validated before application
- Configuration version is checked (currently v1 only)
- Duplicate seat IDs are rejected
- Validation issues are reported but don't block import
- Backup is recommended before import (manual)

### Backup/Restore

```csharp
// Create backup
var snapshot = await seatService.CreateBackupAsync();

// Restore from backup
var result = await seatService.RestoreBackupAsync(snapshot);
// result.Diff shows what changed
```

## Device/Session Discovery Integration

### Hardware Integration

Reuses Milestone 1's `IHardwareDetectionService`:

```csharp
// Available devices for assignment
var monitors = seatService.GetAvailableMonitors();
var keyboards = seatService.GetAvailableKeyboards();
var mice = seatService.GetAvailableMice();
```

Device availability is tracked via `ConnectionStatus`:
- `Connected` - Available for assignment
- `Disconnected` - Shows as unavailable in UI

### Session Integration

Reuses Milestone 3's `ISessionManager`:

```csharp
// Available sessions for assignment
var sessions = seatService.GetAvailableSessions();
```

Only sessions that `CanLaunchProcess` are shown (Active, Unlocked, Detected).

## Validation Engine

### Validation Rules

A seat is `Ready` only when:
1. Seat is enabled
2. Monitor assigned and connected
3. Keyboard assigned and connected
4. Mouse assigned and connected
5. Session assigned and in launchable state (Active, Unlocked, Detected)
6. No conflicts with other seats

### Conflict Detection

Detects when a device/session is assigned to multiple enabled seats:

```csharp
if (seat1.MonitorId == seat2.MonitorId && seat1.IsEnabled && seat2.IsEnabled)
    // Conflict detected!
```

### Disconnection Handling

When a configured device disconnects:
- Assignment is **preserved** (not deleted)
- Seat status changes to `DeviceUnavailable`
- Reconnection with same ID automatically restores availability
- Different ID = treated as new device (old assignment marked unavailable)

## UI Implementation

### SeatsView

The main configuration UI (`SeatsView.xaml`) provides:

- **Toolbar**: Add Seat, Refresh, Export, Import, Create Backup
- **Seats List**: Each seat shows:
  - Seat number badge (green=ready, red=conflict)
  - Name (editable via Rename)
  - Enable/Disable toggle
  - Monitor/Keyboard/Mouse/Session dropdowns
  - Status display with warnings
  - Validate/Rename/Reset buttons
- **Dialogs**: Export (read-only JSON), Import (paste JSON)
- **Legend**: Visual key for status indicators

### SeatViewModel

Per-seat UI logic in `SeatViewModel`:

- Reactive property updates via `partial void OnPropertyChanged`
- Automatic service calls on assignment changes
- Real-time validation updates via `SeatValidationChanged` event
- Live device/session availability from services

### Event System

```csharp
// Configuration changes
public event EventHandler<SeatConfigurationChangedEventArgs> ConfigurationChanged;

// Validation updates
public event EventHandler<SeatValidationChangedEventArgs> SeatValidationChanged;
```

Hardware/session changes automatically trigger re-validation.

## Limitations (By Design)

> **This milestone is configuration only.**

The following are **NOT** implemented:

- ? Keyboard input isolation
- ? Mouse input isolation
- ? Raw Input interception
- ? Monitor ownership enforcement
- ? Windows session creation
- ? Automatic user login
- ? Input hooks or hooks
- ? Kernel drivers
- ? DLL injection
- ? System DLL modification
- ? termsrv.dll modification
- ? Windows licensing bypass

The configuration records **intent** only. Actual isolation is deferred to Milestone 5+.

## Security

- Configuration stored in user's AppData (%LOCALAPPDATA%\FreeSeat)
- No credentials, tokens, or secrets stored
- Import validates JSON structure before application
- No arbitrary code execution from imported JSON
- No registry or system file modifications
- Standard user privileges sufficient

## Performance

- Configuration loaded once at startup
- Device/session lists cached from existing services
- Validation runs on-demand and on hardware/session changes
- No background polling loops
- UI updates via event-driven architecture

## Testing

### Unit Tests (48 tests)

| Test Class | Tests | Coverage |
|------------|-------|----------|
| `SeatModelTests` | 12 | Seat creation, immutability, assignments |
| `SeatAssignmentTests` | 6 | Assignment properties, completeness |
| `SeatValidationTests` | 8 | Validation logic, issue types, descriptions |
| `SeatConfigurationTests` | 6 | Configuration CRUD, comparison, defaults |
| `SeatAssignmentTests` | 6 | Assignment operations |
| `SeatValidationTests` | 8 | Validation logic, issues |
| `SeatConfigurationTests` | 6 | Configuration management |

**Total: 48 tests** (31 new seat tests + 17 existing)

## Related Documents

- [Architecture](architecture.md) - Overall system architecture
- [Roadmap](roadmap.md) - Project roadmap
- [Session Manager](session-manager.md) - Session management
- [Windows Session Research](windows-session-research.md) - WTS API research
- [Input Architecture Research](input-architecture-research.md) - Input routing research

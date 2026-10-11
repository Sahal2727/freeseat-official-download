# Runtime Orchestration Architecture

## Overview

This document describes the runtime orchestration architecture implemented in FreeSeat Milestone 7. The runtime orchestration service coordinates hardware detection, session management, and seat configuration to provide a unified runtime view of all seats.

## Architecture

### Core Components

```
IRuntimeOrchestrationService
    ├── InitializeAsync() - Initialize all dependent services
    ├── ReconcileAsync() - Reconcile configured vs actual state
    ├── StartSeatAsync() - Validate and start a seat
    ├── StopSeatAsync() - Gracefully stop a seat
    ├── RestartSeatAsync() - Restart a seat
    ├── GetSeatRuntime() - Get runtime state for a seat
    ├── GetAllSeatRuntimes() - Get all seat runtimes
    └── StopAsync() - Stop the orchestration service
```

### State Model

#### SeatRuntime
Represents the runtime state of a single seat:
- **Id/Name** - Seat identification
- **State** - Current runtime state (Ready, PartiallyReady, Degraded, Error, etc.)
- **Configured vs Active Devices** - Distinguishes between configured assignments and actually available devices
- **Issues** - Validation issues preventing ready state
- **HasTrueIsolation** - Always false on consumer Windows 11 (documented limitation)

#### SeatRuntimeState Enum
```
Unknown → Initializing → Ready ↔ PartiallyReady ↔ Degraded ↔ Error
                ↓
            Stopped ↔ Stopping → Running
```

#### RuntimeStatus
Global runtime status aggregating all seats:
- **SystemState** - Overall system health
- **Seats** - All seat runtimes
- **Hardware/Session/Display Status** - Aggregated status from dependent services

### Reconciliation Engine

The core reconciliation logic runs periodically (every 30 seconds) and on-demand:

1. **Load Configuration** - Get seat assignments from SeatConfigurationService
2. **Query Hardware** - Get current hardware snapshot from HardwareDetectionService
3. **Query Sessions** - Get active sessions from SessionManager
4. **Query Displays** - Get display topology from DisplayDetectionService
5. **Match Assignments** - Match configured devices to actual hardware
6. **Generate Issues** - Create validation issues for unmatched assignments
7. **Determine States** - Calculate seat and system states
8. **Publish Events** - Raise StatusChanged and SeatStateChanged events

### Seat State Determination

| Condition | State |
|-----------|-------|
| Seat disabled | Stopped |
| Has error issues | Error |
| Has warning issues | Degraded |
| All assignments matched | Ready |
| Some assignments unmatched | PartiallyReady |

### System State Determination

| Condition | SystemState |
|-----------|-------------|
| Any seat Error | Error |
| Any seat Degraded | Degraded |
| All enabled seats Ready | Ready |
| Mixed states | PartiallyReady |
| No enabled seats | Ready |

### Event Handling

The orchestrator subscribes to events from all dependent services:
- **ConfigurationChanged** - Seat configuration modified
- **SeatValidationChanged** - Validation results updated
- **HardwareChanged** - Device added/removed/updated
- **DisplayChanged** - Monitor topology changed
- **SessionEvent** - Session logon/logoff/lock/unlock
- **SessionsChanged** - Session list changed

All events trigger an automatic reconciliation.

### Seat Start/Stop Operations

- **StartSeatAsync** - Validates seat configuration, reconciles state, returns success/failure
- **StopSeatAsync** - Graceful stop (no process termination in current implementation)
- **RestartSeatAsync** - Stop + brief delay + Start

### Safety Constraints

- No process termination or injection
- No automatic session switching
- No input interception
- Read-only hardware/display queries
- User-initiated operations only
- Transparent error reporting

### Limitations (Documented)

1. **No True Input Isolation** - Milestone 5 decision: PARTIALLY
2. **No True Display Isolation** - Milestone 6 decision: PARTIALLY
3. **No GPU Partitioning** - Requires Windows Server + enterprise GPU
3. **No Process Launch Integration** - Requires explicit user action

### Integration Points

- **SeatConfigurationService** - Source of truth for seat assignments
- **HardwareDetectionService** - Hardware enumeration and monitoring
- **DisplayDetectionService** - Monitor topology and EDID
- **SessionManager** - Session enumeration and process launching
- **SettingsService** - Persistence of configuration

### Testing

- 48 unit tests passing
- Covers reconciliation logic, state determination, event handling
- Integration tests for seat start/stop operations

### Future Enhancements

- Full SessionLauncher integration for process launching
- Real-time device monitoring with faster polling
- Enhanced UI with real-time status updates
- Configuration validation with auto-repair suggestions

---

*Document generated as part of FreeSeat Milestone 7 - Runtime Orchestration*
*Last Updated: 2026*
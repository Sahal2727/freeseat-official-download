# FreeSeat Roadmap

## Vision

Enable two people to use one physical Windows PC independently with:
- Separate monitors
- Separate keyboards and mice
- Separate Windows user sessions
- Independent audio and GPU resources

---

## Milestone Overview

| Milestone | Title | Status | Description |
|-----------|-------|--------|-------------|
| 0 | Foundation + Installer | ? Complete | WPF app, installer, diagnostics, settings |
| 1 | Hardware Detection | ? Complete | Enumerate monitors, keyboards, mice, GPUs |
| 2 | Windows Session Research | ? Complete | Investigate session creation APIs |
| 3 | Session Manager | ? Complete | Session lifecycle, safe process launch |
| 4 | Seat Configuration | ? Complete | Hardware/session mapping to seats |
| 5 | Input Isolation | Planned | Per-seat keyboard/mouse filtering |
| 6 | Independent Seats | Planned | End-to-end multi-seat operation |
| 7 | Gaming | Planned | GPU allocation, anti-cheat compatibility |
| 8 | Safety & Recovery | Planned | Watchdog, crash recovery, data protection |
| 9 | Final UI | Planned | Polish, accessibility, localization |
| 10 | Release | Planned | 1.0.0 stable release |

---

## Milestone 0 — Foundation + Installer (Complete)

**Goal:** Establish project foundation, build system, and user-facing installer

**Deliverables:**
- Solution structure with 4 projects
- WPF application with MVVM architecture
- Modern theming (light/dark/system)
- Navigation (Dashboard, Seats, Diagnostics, Settings, About)
- System diagnostics with WMI
- Read-only compatibility checks
- Settings persistence (JSON)
- Inno Setup installer with:
  - Welcome page
  - System check page
  - Installation progress
  - Completion with launch option
- Start Menu + Desktop shortcuts
- Uninstaller
- Documentation (README, Architecture, Roadmap)
- Unit test project structure

**Technical Debt:**
- Placeholder icon (need proper .ico)
- Wizard bitmap images for installer
- Language localization infrastructure

---

## Milestone 1 — Hardware Detection (Complete)

**Goal:** Reliably enumerate and identify all seat-relevant hardware

**Deliverables:**
- Monitor detection (EDID, resolution, refresh rate, connection type)
- Keyboard enumeration (HID, vendor/product IDs, layout)
- Mouse enumeration (HID, vendor/product IDs, DPI)
- GPU detection (vendor, VRAM, driver version, outputs)
- Audio device enumeration
- USB controller topology
- Hardware change notifications (WMI/WinRT)
- Persistent device identification (survives reboots)
- UI: Hardware list view with details

**Technical Approach:**
- WMI (Win32_DesktopMonitor, Win32_Keyboard, Win32_PointingDevice)
- WinRT (DeviceInformation) for modern device enumeration
- SetupAPI for low-level device properties
- Cache results with file watchers for changes

**Dependencies:** None new (System.Management, Windows.Devices.Enumeration)

---

## Milestone 2 — Windows Session Research (Complete)

**Goal:** Understand and document Windows session management options

**Research Areas:**
- RDP session hosting (termsrv.dll behavior)
- Remote Desktop Services API (WTSAPI32)
- Session 0 isolation implications
- User session creation via CreateProcessAsUser
- WinStation API (winsta.dll)
- Virtual Desktop Infrastructure (VDI) APIs
- AppContainer / Silo isolation
- Licensing requirements (RDS CALs)

**Deliverable:** Technical decision document with:
- Recommended approach
- License compliance analysis
- Security implications

---

## Milestone 3 — Session Manager (Complete)

**Goal:** Create a reliable session-management layer for FreeSeat

**Deliverables:**
- SessionManager abstraction (ISessionManager)
- Session lifecycle tracking (SessionLifecycleState)
- Session state tracking with history (SessionStateTracker)
- WTS notification handling (SessionService)
- Safe process launching (SessionLauncher)
- Reconciliation service (SessionRecoveryService)
- Developer test launch (notepad.exe)
- Developer Mode toggle (off by default)
- Updated Sessions UI with event history
- Settings persistence for session manager

**Core Models:**
- `ManagedSession` - FreeSeat-managed session with lifecycle
- `SessionIdentity` - User/session identification
- `SessionLifecycleState` - FreeSeat session states
- `SessionLaunchRequest` - Validated launch request
- `SessionLaunchResult` - Structured launch result
- `SessionEventArgs` - Strongly-typed session events
- `SessionSnapshot` - Point-in-time session state

**APIs Used:**
- `WTSEnumerateSessionsExW` / `WTSEnumerateSessionsW`
- `WTSQuerySessionInformationW`
- `WTSRegisterSessionNotification` / `WTSUnRegisterSessionNotification`
- `WTSQueryUserToken`
- `CreateProcessAsUserW`
- `DuplicateTokenEx`
- `CreateEnvironmentBlock` / `DestroyEnvironmentBlock`

**Security Model:**
- Read-only session monitoring (no modifications)
- Authorized-only process launching
- Structured failure results
- No credential access or token theft
- Developer Mode off by default

**Testing:**
- All existing tests pass (17)
- Build succeeds in Release configuration
- Manual verification of Sessions UI

---

## Milestone 4 — Seat Configuration (Complete)

**Goal:** Create a reliable seat-configuration layer for FreeSeat

**Deliverables:**
- SeatConfigurationService abstraction (ISeatConfigurationService)
- Seat domain model (Seat, SeatId, SeatAssignment)
- Seat lifecycle tracking (SeatLifecycleState, SeatStatus)
- Hardware/session assignment (monitor, keyboard, mouse, session)
- Validation engine (SeatValidationResult, SeatValidationIssue)
- Conflict detection (duplicate device/session assignments)
- Hardware/session discovery integration
- Disconnection handling (preserved assignments)
- Persistence (JSON in AppSettings)
- Configuration versioning (v1)
- Import/Export (JSON)
- Backup/Restore (SeatSnapshot)
- Professional Seats UI
- Device/Session selectors with availability status
- Conflict/unavailable warnings
- Rename/Enable/Disable/Reset per seat
- Rename/Enable/Disable/Reset actions
- 48 total tests (31 new seat tests + 17 existing)

**Core Models:**
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

**APIs Used:**
- `IHardwareDetectionService` (Milestone 1)
- `ISessionManager` (Milestone 3)
- Existing settings persistence

**Security Model:**
- Configuration only - no actual isolation/routing
- Persistent device IDs (no names as identifiers)
- Stable seat IDs (GUIDs)
- Conflict detection, not resolution
- Disconnected devices preserved
- Import validates before apply
- No credential access

**Testing:**
- 48 total tests (31 new seat tests + 17 existing)
- All tests pass
- Build succeeds in Release configuration
- Manual verification of Seats UI

**Limitations (By Design):**
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

---

## Milestone 5 — Input Isolation

**Goal:** Route keyboard/mouse input to correct seat session

**Tasks:**
- Raw Input API integration (RegisterRawInputDevices)
- Input device capture per seat
- Input routing to target session
- Secure Desktop handling
- UAC/integrity level considerations
- Focus management
- Anti-cheat compatibility research

**Approaches:**
1. **User-mode Raw Input** (current): RIDEV_INPUTSINK + SendInput/PostMessage
2. **Kernel filter driver** (future): Upper filter on kbdclass/mouclass

**Challenges:**
- Foreground session requirement for SendInput
- Secure Desktop blocks injection
- Keyboard layout differences per session
- Anti-cheat detection

**Dependencies:** Milestone 4 (seat config)

---

## Milestone 6 — Independent Seats

**Goal:** End-to-end multi-seat operation

**Tasks:**
- Seat activation/deactivation
- Session launch per seat
- Input routing per seat
- Display assignment per seat
- Audio endpoint assignment
- GPU resource partitioning (basic)
- Seat health monitoring
- Graceful degradation

**Success Criteria:**
- Two seats active simultaneously
- No input cross-talk
- Independent session interaction
- Stable for 1+ hour

**Dependencies:** Milestones 4, 5

---

## Milestone 7 — Gaming

**Goal:** GPU allocation and anti-cheat compatibility

**Tasks:**
- GPU partitioning (single GPU time-slice or multi-GPU)
- VRAM isolation
- Anti-cheat vendor engagement
- Game compatibility testing
- Overlay support (Discord, Steam, etc.)
- HDR/VRR per seat
- Latency optimization

**Approaches:**
- Single GPU: Time-slice with context switching
- Multi-GPU: Physical GPU per seat
- Cloud streaming fallback

**Dependencies:** Milestone 6

---

## Milestone 8 — Safety & Recovery

**Goal:** Production-grade reliability and data protection

**Tasks:**
- Watchdog service (seat health monitoring)
- Automatic crash recovery
- Unsaved work detection/warning
- Graceful degradation (one seat fails ? other continues)
- Configuration backup/restore
- Firmware/driver update handling
- Power management coordination
- Telemetry (opt-in, anonymous)
- Automated testing pipeline

**Safety Principles:**
- Never lose user data
- Never leave hardware in undefined state
- Always provide recovery path
- Clear error messages with actionable steps

---

## Milestone 9 — Final UI

**Goal:** Polish, accessibility, and internationalization

**Tasks:**
- Visual design system refinement
- High contrast theme support
- Screen reader compatibility (UIA)
- Keyboard navigation throughout
- Localization framework (resx)
- Initial languages: EN, DE, FR, ES, JA, ZH
- RTL language support preparation
- First-run experience / tutorial
- Context-sensitive help
- Performance optimization (startup, memory)

**Quality Gates:**
- WCAG 2.1 AA compliance
- <2s cold start
- <100MB working set
- Zero layout shifts

---

## Milestone 10 — Release

**Goal:** 1.0.0 stable release

**Tasks:**
- Code signing (EV certificate)
- Microsoft Store submission (optional)
- Chocolatey / Winget package
- Automated release pipeline
- Rollback strategy
- Support documentation
- Community guidelines
- Security audit
- License compliance verification
- Launch announcement

**Release Criteria:**
- All Milestone 6 scenarios passing
- 30-day soak test
- Zero critical bugs
- Security review passed
- Accessibility audit passed

---

## Timeline Estimate

| Milestone | Est. Duration | Cumulative |
|-----------|---------------|------------|
| 0 | 2 weeks | 2 weeks |
| 1 | 3 weeks | 5 weeks |
| 2 | 2 weeks | 7 weeks |
| 3 | 4 weeks | 11 weeks |
| 4 | 3 weeks | 14 weeks |
| 5 | 4 weeks | 18 weeks |
| 6 | 4 weeks | 22 weeks |
| 7 | 4 weeks | 26 weeks |
| 8 | 3 weeks | 29 weeks |
| 9 | 3 weeks | 32 weeks |
| 10 | 2 weeks | 34 weeks |

**Total:** ~8 months to 1.0.0

---

## Risk Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Windows session APIs insufficient | High | Early research (M2), fallback to VM approach |
| Input filter driver complexity | High | Start with user-mode, evaluate KMDF need |
| GPU sharing not viable | Medium | Cloud streaming fallback, single-GPU time-slice |
| Anti-cheat blocks multi-seat | Medium | Document limitations, work with vendors |
| Licensing restrictions | High | Legal review, RDS CAL compliance |
| Driver signing costs | Medium | Budget for EV cert, explore test signing |

---

## Success Metrics

**Milestone 0:**
- Builds without errors
- Installer runs on clean VM
- App launches, navigation works
- Diagnostics copy works

**Milestone 1:**
- Hardware list matches Device Manager
- Change detection <1s latency

**Milestone 3:**
- Session creates in <5s
- Persists across reboots

**Milestone 4:**
- Seat configuration persists across restarts
- Device/session assignments work correctly
- Conflict detection works

**Milestone 6:**
- Two seats independent for 1 hour
- No input cross-talk
- Audio isolated

**Milestone 10:**
- 1000+ downloads first month
- <1% crash rate
- Positive community feedback

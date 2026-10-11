# FreeSeat 1.0.0 Release Notes

## Release Date: October 2026

---

## Overview

FreeSeat 1.0.0 is the first stable release of FreeSeat — a free, open-source Windows application for configuring and monitoring multi-seat setups on a single Windows 11 PC.

**Version:** 1.0.0  
**Target Platform:** Windows 11 (21H2 / Build 22000+), x64  
**License:** MIT  
**Runtime:** .NET 9.0 Desktop Runtime

---

## Major Functionality Delivered

### 🎯 Core Features

| Area | Features |
|------|----------|
| **Seat Configuration** | Two-seat model with persistent JSON config, device assignment (monitor/keyboard/mouse/session), conflict detection, state machine (Unconfigured→Incomplete→Ready→Active), backup/restore/import/export |
| **Hardware Detection** | Monitor (EDID, resolution, refresh), Keyboard/Mouse (HID vendor/product IDs), GPU, Audio devices, Hardware change notifications via WMI |
| **Session Management** | WTSAPI32 session enumeration, state tracking (Active/Connected/Disconnected/Locked), session change notifications, process-to-session mapping, safe process launch in target session (Developer Mode) |
| **Runtime Orchestration** | Seat lifecycle management, periodic state reconciliation, event history |
| **User Interface** | WPF/MVVM dashboard, seats/sessions/hardware/diagnostics/settings views, dark/light/system theme, toast notifications, system tray integration |
| **Installer** | Inno Setup professional installer, admin install to Program Files, Desktop/Start Menu shortcuts, start-with-Windows option, working uninstaller |
---

## User Interface

- **Dashboard** — Two-seat overview cards with state badges and quick actions
- **Seats View** — Expandable seat configuration panels with device selectors, real-time validation, conflict warnings
- **Sessions View** — Live session list with state, user, process count; Developer Mode test launch
- **Hardware View** — Categorized inventory (Monitors, Keyboards, Mice, GPU, Audio) with refresh
- **Diagnostics View** — System info, compatibility checks, copy-to-clipboard report
- **Settings View** — General (theme, language, tray), Notifications (enable, categories), Advanced (developer mode, log level, refresh interval), Backup/Restore
- **About View** — Version, credits, links (placeholder URLs marked TODO)

---

## Runtime

- **RuntimeOrchestrationService** — Central coordinator for seat lifecycle
- **SeatRuntime** — Per-seat runtime state with validation
- **Reconciliation** — Periodic sync between desired config and actual system state
- **Event System** — SeatServiceEvents for UI binding and logging

---

## Hardware Detection

- **DisplayDetectionService** — WMI-based (Win32_DesktopMonitor, WmiMonitorID)
- **Input Detection** — WMI-based (Win32_Keyboard, Win32_PointingDevice)
- **GPU/Audio** — WMI-based (Win32_VideoController, Win32_SoundDevice)
- **Change Notifications** — WMI event subscriptions for connect/disconnect

> **Note:** Uses WMI only. SetupAPI/DXGI detection is a known technical debt item.

---

## Installer

- **Inno Setup 6** script (`FreeSeat.iss`)
- **Output:** `dist/FreeSeat-Setup.exe` (~2.9 MB)
- **Icon:** Official two-monitor icon (`FreeSeat-monitor-icon.ico`)
- **Branding:** Official installer banner (`FreeSeat-Installer.png`)
- **Default Path:** `{autopf}\FreeSeat` → `C:\Program Files\FreeSeat\`
- **Shortcuts:** Desktop (optional, default ON), Start Menu (always)
- **Start with Windows:** Registry Run entry (optional, default ON)
- **Launch on Finish:** Optional
- **Uninstaller:** Full removal of app files, shortcuts, registry entries; preserves user config in AppData

---

## Branding

- **Application Icon:** `FreeSeat-monitor-icon.ico` (two-monitor design)
- **Installer Banner:** `FreeSeat-Installer.png`
- **About Logo:** `FreeSeat.png`
- All assets from `assets/branding/` — official, unchanged

---

## Security

- **No kernel drivers, no global hooks, no DLL injection**
- **No input interception, no keystroke capture, no mouse capture**
- **No system DLL modification, no Defender disabling, no licensing bypass**
- **No credential collection, no telemetry, no network communication**
- **Standard user execution** (admin only for installer and Developer Mode session launch)
- **Read-only system diagnostics** (WMI, WTSAPI32)
- **Settings stored in user AppData** (%APPDATA%\FreeSeat)

---

## Known Limitations

> **CRITICAL:** TRUE PHYSICAL KEYBOARD/MOUSE/DISPLAY ISOLATION IS NOT ENFORCED ON NORMAL CONSUMER WINDOWS.

FreeSeat provides **configuration and monitoring only**. On Windows 11 Home/Pro:
- All keyboards/mice control the active desktop simultaneously
- Windows controls which session appears on which monitor
- Audio, GPU, and desktop are shared

See [docs/known-limitations.md](known-limitations.md) for full details.

---

## Testing

- **48/48 unit tests passing** (Release configuration)
- **Clean build:** 0 errors, 0 warnings (Release)
- **Test Coverage:** AppInfo, Diagnostics, Hardware Models, Seat Models, Session Models, Configuration, Installer Config

---

## What's NOT in This Release

| Feature | Status |
|---------|--------|
| True input isolation | Not feasible on consumer Windows without kernel driver |
| Display ownership enforcement | Not possible in user-mode |
| Audio isolation | Not exposed via user-mode APIs |
| GPU partitioning | Requires Windows Server / vGPU |
| Independent desktops | Requires multi-session Windows Server |
| Automated hardware pairing | Manual assignment only |
| Multi-language | English only |
| SetupAPI/DXGI display detection | WMI-only (technical debt) |

---

## Migration / Upgrade

This is the initial 1.0.0 release. No migration needed.

Fresh install only. Configuration from any pre-release versions is not compatible.

---

## Verification

**SHA-256:** `37c565c84b2f1b85df103ff576bcaf71799b5e465faa833b6b9a9caad39ff72d  FreeSeat-Setup.exe`

Verify with:
```powershell
certutil -hashfile FreeSeat-Setup.exe SHA256
```

---

## Links (Placeholders)

- **GitHub:** https://github.com/freeseat/freeseat (TODO)
- **Documentation:** https://docs.freeseat.app (TODO)
- **License:** https://github.com/freeseat/freeseat/blob/main/LICENSE (TODO)

---

*FreeSeat 1.0.0 — Two seats. One PC.*
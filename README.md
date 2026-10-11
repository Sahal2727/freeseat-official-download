# FreeSeat

Two seats. One PC.

**Version 1.0.0**

Created by Sahal

© 2026 Sahal

Free and open-source software (MIT License).

---

## What is FreeSeat?

FreeSeat is a free, open-source Windows application for configuring and monitoring multi-seat setups on a single Windows 11 PC. It helps you assign monitors, keyboards, mice, and Windows sessions to two independent "seats."

**Seat 1:**
- Monitor 1
- Keyboard 1
- Mouse 1
- Windows user/session 1

**Seat 2:**
- Monitor 2
- Keyboard 2
- Mouse 2
- Windows user/session 2

---

## Features

- **Seat Configuration** — Two-seat model with persistent JSON config, device assignment (monitor/keyboard/mouse/session), conflict detection, state machine
- **Hardware Detection** — Monitor (EDID, resolution, refresh), Keyboard/Mouse (HID vendor/product IDs), GPU, Audio devices, Hardware change notifications
- **Session Management** — Windows session enumeration, state tracking, session change notifications, process-to-session mapping, safe process launch (Developer Mode)
- **Runtime Orchestration** — Seat lifecycle management, periodic state reconciliation, event history
- **User Interface** — WPF/MVVM dashboard, seats/sessions/hardware/diagnostics/settings views, dark/light/system theme, toast notifications, system tray
- **Professional Installer** — Single-file Inno Setup installer with a branded wizard, per-user install, Start Menu shortcut, optional start-with-Windows setting, working uninstaller, and a desktop-shortcut checkbox enabled by default
- **Backup & Restore** — Configuration export/import, timestamped backups, restore from backup

---

## System Requirements

| Requirement | Minimum |
|-------------|---------|
| Operating System | Windows 11 (21H2 / Build 22000 or later), 64-bit |
| Processor | x64 (Intel/AMD) |
| RAM | 8 GB (16 GB recommended for two active sessions) |
| Storage | 500 MB available space |
| Graphics | GPU with at least 2 display outputs |
| Input Devices | 2 keyboards, 2 mice (USB or Bluetooth) |
| Displays | 2 monitors (or 1 monitor + 1 TV/projector) |
| .NET Runtime | .NET 9.0 Desktop Runtime (included in installer) |
| Privileges | No administrator access required for per-user installation |

## Build the Windows installer

With the .NET 9 SDK and Inno Setup 6 installed, run this from the repository root:

```powershell
.\build-tools\Build-Setup.ps1
```

The script publishes the self-contained one-file app and packages it into `artifacts\FreeSeat-Setup\FreeSeat-Setup.exe`. The setup wizard installs FreeSeat for the current Windows user and starts the app when installation finishes. “Create a desktop shortcut” is enabled by default and can be turned off on the additional-options page.

---

## Installation

1. Download `FreeSeat-Setup.exe` from the releases page
2. Verify SHA-256 checksum:
   ```powershell
   certutil -hashfile FreeSeat-Setup.exe SHA256
   ```
3. Run `FreeSeat-Setup.exe` as Administrator
4. Choose options: Desktop Shortcut (default ON), Start with Windows (default ON)
4. Click **Install**
5. Optionally launch FreeSeat on completion

**Default install location:** `C:\Program Files\FreeSeat\`

---

## Known Limitations

> **CRITICAL:** TRUE PHYSICAL KEYBOARD/MOUSE/DISPLAY ISOLATION IS NOT ENFORCED ON NORMAL CONSUMER WINDOWS.

FreeSeat provides **configuration and monitoring only**. On Windows 11 Home/Pro:
- All keyboards/mice control the active desktop simultaneously
- Windows controls which session appears on which monitor
- Audio, GPU, and desktop are shared

For true multi-seat isolation, you need Windows Server with RDS CALs, commercial multi-seat software (e.g., ASTER), virtualization, or Linux.

See [docs/known-limitations.md](docs/known-limitations.md) for full details.

---

## Documentation

- [User Guide](docs/user-guide.md) — Complete usage guide
- [FAQ](docs/faq.md) — Frequently asked questions
- [Troubleshooting](docs/troubleshooting.md) — Common issues and fixes
- [Known Limitations](docs/known-limitations.md) — Honest limitation documentation
- [Release Notes](docs/release-notes-1.0.0.md) — This release details
- [Security Audit](docs/security-audit-1.0.0.md) — Security review
- [License Compliance](docs/license-compliance-1.0.0.md) — Dependency licenses
- [Rollback Strategy](docs/rollback-strategy.md) — How to roll back
---

## Quick Start

1. **Install** FreeSeat via the installer
2. **Launch** FreeSeat from Start Menu or Desktop
3. **Configure Seats** — Go to **Seats** page, assign Monitor, Keyboard, Mouse, Session to each seat
4. **Resolve Conflicts** — Ensure no device/session assigned to both seats
5. **Start Seats** — When both seats show **Ready**, click **Start**

---

## Project Structure

```
FreeSeat/
+-- src/
|   +-- FreeSeat.App/           # WPF Application
|   |   +-- Views/              # XAML Views
|   |   +-- ViewModels/         # MVVM ViewModels
|   |   +-- Styles/             # Themes and Control Styles
|   |   +-- Converters/         # Value Converters
|   |   +-- Services/           # Application Services
|   +-- FreeSeat.Core/          # Core Domain Logic
|   |   +-- Diagnostics/        # System Diagnostics
|   |   +-- Hardware/           # Hardware Models/Interfaces
|   |   +-- Sessions/           # Session Models/Interfaces
|   |   +-- Seats/              # Seat Configuration Models/Interfaces
|   |   +-- Runtime/            # Runtime Orchestration
|   +-- FreeSeat.Infrastructure/# Infrastructure Services
|   |   +-- Configuration/      # Settings Persistence
|   |   +-- Hardware/           # Hardware Detection
|   |   +-- Sessions/           # Session Management
|   |   +-- Runtime/            # Runtime Services
|   +-- FreeSeat.Setup/         # Inno Setup Installer
+-- tests/
|   +-- FreeSeat.Tests/         # Unit Tests (48 passing)
+-- docs/                       # Documentation
+-- assets/                     # Icons, Images
+-- dist/                       # Build Output (FreeSeat-Setup.exe, SHA256SUMS.txt)
```

### Key Technologies

- **.NET 9** — Modern .NET runtime
- **WPF** — Windows Presentation Foundation for UI
- **CommunityToolkit.Mvvm** — MVVM framework with source generators
- **Microsoft.Extensions.DependencyInjection** — Dependency Injection
- **System.Management** — WMI for system diagnostics
- **Inno Setup** — Professional Windows installer

### Design Principles

- **Clean Architecture** — Separation of concerns
- **MVVM Pattern** — Testable, maintainable UI code
- **Nullable Reference Types** — Compile-time null safety
- **Modern C#** — Latest language features
- **Security First** — No admin exploits, no system modifications

---

## Milestone Progress

| Milestone | Title | Status |
|-----------|-------|--------|
| 0 | Foundation + Installer | ✅ Complete |
| 1 | Hardware Detection | ✅ Complete |
| 2 | Windows Session Research | ✅ Complete |
| 3 | Session Manager | ✅ Complete |
| 4 | Seat Configuration | ✅ Complete |
| 5 | Input Isolation | ❌ Not feasible on consumer Windows |
| 6 | Independent Seats | ❌ Requires Windows Server |
| 7 | Gaming | ❌ Not applicable |
| 8 | Safety & Recovery | ✅ Config backup/restore complete |
| 9 | Final UI | ✅ Complete |
| 10 | Release | ✅ **1.0.0 Released** |

See [docs/roadmap.md](docs/roadmap.md) for detailed milestone information.

---

## Testing

- **48/48 unit tests passing** (Release configuration)
- **Clean build:** 0 errors, 0 warnings (Release)
- **Test Coverage:** AppInfo, Diagnostics, Hardware Models, Seat Models, Session Models, Configuration, Installer Config

Run tests:
```bash
dotnet test -c Release
```

---

## Security

FreeSeat follows strict security principles:

✅ **Does NOT:**
- Download unknown executables
- Execute arbitrary scripts
- Modify Windows security settings
- Disable antivirus or Windows Defender
- Modify system DLLs (including termsrv.dll)
- Bypass Windows licensing
- Modify Remote Desktop licensing
- Install kernel drivers
- Collect or transmit personal data
- Use global hooks, DLL injection, or input interception

✅ **Does:**
- Run as standard user (no admin required for app)
- Use read-only system diagnostics (WMI, WTSAPI32)
- Store settings in user AppData folder
- Request admin only for installer (standard practice)
- Hardcode test executable (notepad.exe) in Developer Mode

See [docs/security-audit-1.0.0.md](docs/security-audit-1.0.0.md) for full audit.

---

## License

MIT License — See [LICENSE](LICENSE) file.

---

## Credits

**Creator:** Sahal  
**Copyright:** © 2026 Sahal  
**License:** MIT License

---

## Links

- **GitHub:** https://github.com/freeseat/freeseat (TODO)
- **Documentation:** https://docs.freeseat.app (TODO)
- **License:** https://github.com/freeseat/freeseat/blob/main/LICENSE (TODO)

---

## Verification

**SHA-256:** `37c565c84b2f1b85df103ff576bcaf71799b5e465faa833b6b9a9caad39ff72d  FreeSeat-Setup.exe`

Verify with:
```powershell
certutil -hashfile FreeSeat-Setup.exe SHA256
```

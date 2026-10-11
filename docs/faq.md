# Frequently Asked Questions

## General

### What is FreeSeat?

FreeSeat is a free, open-source Windows application for configuring and monitoring multi-seat setups on a single Windows 11 PC. It helps you assign monitors, keyboards, mice, and Windows sessions to two independent "seats."

### Is FreeSeat really free?

Yes. FreeSeat is licensed under the MIT License. You can use, modify, and distribute it freely, including for commercial purposes.

### What version of Windows do I need?

Windows 11 (21H2 / Build 22000 or later), 64-bit. Windows 10 is not supported.

### Does FreeSeat work on Windows Server?

FreeSeat targets Windows 11 consumer editions. It may run on Windows Server but is not tested or supported there.

---

## Installation

### Do I need administrator rights to install?

Yes. The installer requires Administrator privileges to write to `C:\Program Files\FreeSeat\` and create Start Menu shortcuts.

### Do I need administrator rights to run FreeSeat?

**No.** The application runs as a standard user. Administrator is only required for:
- Installation/uninstallation
- Developer Mode session launch (optional)

### Where is FreeSeat installed?

Default: `C:\Program Files\FreeSeat\`

User data (config, logs): `%APPDATA%\FreeSeat\` and `%LOCALAPPDATA%\FreeSeat\`

### Can I install FreeSeat without the installer?

The installer is the recommended method. You can also run the published `FreeSeat.App.exe` directly from the `dist/` folder, but you'll need to manually install the .NET 9.0 Desktop Runtime.
---

## Multi-Seat Concepts

### What is a "seat"?

A seat is a complete user workspace: one monitor + one keyboard + one mouse + one Windows session.

### Can I have more than two seats?

FreeSeat 1.0.0 supports exactly two seats. This is a design decision for the initial release.

### What hardware do I need for two seats?

- 2 monitors (or 1 monitor + 1 TV/projector)
- 2 keyboards (USB or Bluetooth)
- 2 mice (USB or Bluetooth)
- A GPU with at least 2 video outputs
- Sufficient RAM (16 GB recommended)

### Can I use a laptop screen + external monitor?

Yes, if your laptop has a discrete GPU or supports dual-display output. The laptop screen counts as one monitor.

---

## Configuration

### Why does my seat show "Conflict"?

A conflict means the same hardware device or Windows session is assigned to both seats. Each device/session can only belong to one seat at a time. Resolve by assigning different devices to each seat.

### Why does my seat show "DeviceUnavailable"?

A previously assigned device (monitor, keyboard, or mouse) is no longer detected by Windows. Reconnect the device or assign a different one.

### Why does my seat show "SessionUnavailable"?

The assigned Windows session is not currently available (e.g., user logged off). Log the user back in or assign a different session.

### How do I know which keyboard is which?

Go to **Hardware → Keyboards**. Press keys on a physical keyboard — the corresponding entry may highlight (device-dependent). You can also check vendor/product IDs.

### Can I assign the same monitor to both seats?

No. This creates a conflict. Each seat needs its own display.

### Can I use one keyboard for both seats?

No. This creates a conflict. Each seat needs its own keyboard for independent operation.
---

## Sessions

### What is a Windows session?

A Windows session represents a logged-in user's environment. Each user account that logs in creates a session (Session 0 is the system session; Session 1+ are user sessions).

### Can FreeSeat create new Windows sessions?

No. FreeSeat can only enumerate and launch processes in existing sessions. Creating new interactive sessions requires Windows Server with RDS or third-party tools.

### What is "Developer Mode"?

Developer Mode enables the **Launch Test Process** button in the Sessions view. It launches `notepad.exe` in a target session using standard Windows APIs (`WTSQueryUserToken` + `CreateProcessAsUser`). This is for testing/verification only.

### Why does session launch require Administrator?

`CreateProcessAsUser` with another user's token requires `SeAssignPrimaryTokenPrivilege` and `SeIncreaseQuotaPrivilege`, which are only granted to Administrators by default.

---

## Known Limitations (Critical)

### Does FreeSeat actually isolate keyboards and mice?

**No.** On standard Windows 11 Home/Pro, all keyboards and mice control the active desktop simultaneously. FreeSeat **configures** which keyboard belongs to which seat, but Windows does not enforce this routing.

### Does FreeSeat assign monitors to specific sessions?

**No.** Windows controls which session appears on which monitor. FreeSeat records your intent but cannot enforce display ownership.

### Can two people use the PC at the same time independently?

**Not with true isolation on consumer Windows.** Both users will share the same desktop, see each other's cursors, and receive each other's keystrokes. FreeSeat provides the configuration framework; true isolation requires Windows Server RDS or specialized multi-seat software.

### Why doesn't FreeSeat implement true isolation?

True input isolation on Windows requires:
- Kernel-mode drivers (input filtering)
- Windows Server with Remote Desktop Services
- Or specialized commercial solutions (ASTER, etc.)

FreeSeat explicitly avoids kernel drivers, system hooks, DLL injection, and security bypasses. It operates entirely within user-mode, documented Windows APIs.

---

## Troubleshooting

### FreeSeat won't start

1. Ensure .NET 9.0 Desktop Runtime is installed (included in installer).
2. Check `%LOCALAPPDATA%\FreeSeat\Logs\` for error logs.
3. Run `FreeSeat.App.exe` from command line to see console output.

### Hardware not detected

1. Click **Refresh** on the Hardware page.
2. Ensure devices are properly connected and recognized by Windows (check Device Manager).
3. Restart FreeSeat.

### Notifications not appearing

1. Check **Settings → Notifications → Enable Notifications**.
2. Check Windows Settings → System → Notifications → FreeSeat is allowed.
3. Focus Assist / Do Not Disturb may suppress notifications.

### Installer fails

1. Run as Administrator.
2. Ensure Windows 11 64-bit.
2. Check available disk space.
3. Temporarily disable antivirus during install.

---

## Development & Contributing

### Can I contribute?

Yes! FreeSeat is open source. See the repository for contribution guidelines.

### What technology stack?

- .NET 9 / WPF / CommunityToolkit.Mvvm
- System.Management (WMI) for hardware detection
- WTSAPI32 (P/Invoke) for session management
- Inno Setup for installer

### How do I build from source?

```bash
dotnet restore
dotnet build -c Release
```

### How do I run tests?

```bash
dotnet test -c Release
```

---

## Licensing & Legal

### What license is FreeSeat under?

MIT License. See `LICENSE` file.

### Can I use FreeSeat commercially?

Yes, the MIT License permits commercial use.

### Is there a warranty?

No. The software is provided "AS IS" without warranty of any kind.

---

*FreeSeat 1.0.0 — Two seats. One PC.*

### Do assignments persist after reboot?

Yes. All seat configurations are saved to `%APPDATA%\FreeSeat\seats.json` and restored on startup.
# FreeSeat User Guide

## Version 1.0.0

---

## Table of Contents

1. [What is FreeSeat?](#what-is-freeseat)
2. [System Requirements](#system-requirements)
3. [Installation](#installation)
4. [First Launch](#first-launch)
5. [Configuring Seats](#configuring-seats)
6. [Assigning Hardware](#assigning-hardware)
7. [Sessions](#sessions)
8. [Starting and Stopping Seats](#starting-and-stopping-seats)
9. [Settings](#settings)
10. [Notifications](#notifications)
11. [Diagnostics](#diagnostics)
---

## Installation

### Download

1. Download `FreeSeat-Setup.exe` from the official release page.
2. Verify the SHA-256 checksum matches the one published in `SHA256SUMS.txt`:
   ```powershell
   certutil -hashfile FreeSeat-Setup.exe SHA256
   ```

### Install

1. Run `FreeSeat-Setup.exe` as Administrator (required for system-wide installation).
2. Review and accept the license agreement (MIT License).
3. Choose installation options:
   - **Desktop Shortcut** (enabled by default) — creates a shortcut on your desktop
   - **Start with Windows** (enabled by default) — launches FreeSeat minimized at login
4. Click **Install**.
5. Wait for the progress bar to complete.
6. Optionally check **Launch FreeSeat** on the final page.
7. Click **Finish**.

### Default Install Location

```
C:\Program Files\FreeSeat\
```

---

## First Launch

When you launch FreeSeat for the first time:

1. The **Dashboard** appears showing two default seats: "Seat 1" and "Seat 2".
2. Each seat shows its current state: **Unconfigured**, **Incomplete**, **Ready**, or an error state.
3. The sidebar navigation provides access to:
   - **Dashboard** — Overview of both seats
   - **Seats** — Detailed seat configuration
   - **Sessions** — Windows session monitoring
   - **Hardware** — Detected hardware inventory
   - **Diagnostics** — System information and compatibility
   - **Settings** — Application preferences
   - **About** — Version, credits, and links

---

## Configuring Seats

### Opening Seat Configuration

Navigate to **Seats** in the sidebar to access the seat configuration view.

### Seat States

| State | Meaning |
|-------|---------|
| **Unconfigured** | No hardware or session assigned |
| **Incomplete** | Some but not all required components assigned |
| **Ready** | Monitor, keyboard, mouse, and session all assigned — no conflicts |
| **DeviceUnavailable** | Previously assigned hardware no longer detected |
| **SessionUnavailable** | Assigned session not currently available |
| **Conflict** | Hardware or session assigned to multiple seats |
| **Disabled** | Seat manually disabled by user |
---

## Assigning Hardware

### Hardware Detection

FreeSeat automatically detects hardware on startup and when devices are connected/disconnected. Navigate to **Hardware** to see the current inventory:

- **Monitors** — EDID name, resolution, refresh rate, connection type
- **Keyboards** — HID vendor/product ID, device name, connection type
- **Mice** — HID vendor/product ID, device name, DPI (if reported)
- **GPU** — Vendor, model, VRAM, driver version
- **Audio** — Playback and recording devices

### Refreshing Hardware

Click **Refresh** on the Hardware page to force a new detection cycle.

### Identifying Devices

If you're unsure which keyboard/mouse is which:

1. Go to **Hardware** → **Keyboards** (or Mice).
2. Press keys or move the physical device.
3. The corresponding entry will highlight or show activity (if supported by the device).

---

## Sessions

### Session Monitoring

Navigate to **Sessions** to view all Windows sessions on the machine:

- **Session ID** — Unique Windows session identifier
- **User** — Account name
- **State** — Active, Connected, Disconnected, Locked, etc.
- **Processes** — Number of processes running in the session

### Session Launch (Developer Mode)

> **Requires Administrator privileges.**

FreeSeat includes a Developer Mode for testing session launches:

1. Enable **Developer Mode** in **Settings** → **Advanced**.
2. Go to **Sessions** and click **Launch Test Process** next to a session.
3. This launches `notepad.exe` in the target session using `WTSQueryUserToken` + `CreateProcessAsUser`.

This demonstrates the underlying session launch capability but is **not** the primary multi-seat mechanism.

---

## Starting and Stopping Seats

### Starting a Seat

When a seat reaches **Ready** state (all four components assigned, no conflicts):

---

## Settings

Access via **Settings** in the sidebar.

### General

- **Theme** — Light, Dark, or System (follows Windows)
- **Language** — English (more languages planned)
- **Start Minimized** — Launch to system tray instead of window
- **Minimize to Tray** — Close button minimizes to tray

### Notifications

- **Enable Notifications** — Toast notifications for seat state changes
- **Notify on Hardware Change** — Alert when devices connect/disconnect
- **Notify on Session Change** — Alert when sessions change state
- **Notify on Conflict** — Alert when assignment conflicts detected

### Advanced

- **Developer Mode** — Enables test session launch (requires admin)
- **Log Level** — Debug, Information, Warning, Error
- **Auto-Refresh Interval** — Hardware detection refresh rate (default: 30s)

### Backup & Restore

- **Export Configuration** — Save seat config to a JSON file
- **Import Configuration** — Load seat config from a JSON file
- **Create Backup** — Timestamped backup of current configuration
- **Restore Backup** — Select from available backups

---

## Notifications

FreeSeat uses Windows 10/11 toast notifications for:

- Seat state changes (Ready → Active, Conflict detected, etc.)
- Hardware connect/disconnect events
- Session state changes
- Configuration conflicts

Notifications appear in the Windows Action Center. Click a notification to open FreeSeat to the relevant page.

---

## Diagnostics

Navigate to **Diagnostics** for system information and troubleshooting:

### System Information

- OS version and build
- CPU, RAM, GPU details
- Disk space
- .NET runtime version

### Compatibility Checks

- Windows version compatibility
- Multi-session capability
- Administrator privileges
- Display configuration

### Export Diagnostics

Click **Copy to Clipboard** to copy a full diagnostics report for support requests.

---

## Uninstalling

### Via Windows Settings

1. Open **Settings** → **Apps** → **Installed Apps**.
2. Find **FreeSeat**.
3. Click **...** → **Uninstall**.
4. Confirm.

### Via Start Menu

1. Open Start Menu → **FreeSeat** folder.
2. Click **Uninstall FreeSeat**.
---

## Known Limitations

> **CRITICAL: TRUE PHYSICAL KEYBOARD/MOUSE/DISPLAY ISOLATION IS NOT ENFORCED ON NORMAL CONSUMER WINDOWS.**

FreeSeat configures and monitors seat assignments, but on standard Windows 11 Home/Pro:

| Capability | Status |
|------------|--------|
| Monitor assignment (which display shows which session) | ❌ Not enforced |
| Keyboard routing (keys only go to assigned session) | ❌ Not enforced |
| Mouse routing (cursor only moves on assigned display) | ❌ Not enforced |
| Audio isolation | ❌ Not enforced |
| GPU resource allocation per seat | ❌ Not enforced |
| Independent desktop per seat | ❌ Not enforced |

**What FreeSeat DOES provide:**
- Persistent seat configuration (which devices belong to which seat)
- Real-time hardware detection and monitoring
- Windows session enumeration and state tracking
- Conflict detection (prevents accidental double-assignment)
- Configuration backup/restore/import/export
- Session launch capability (via standard Windows APIs)

**What FreeSeat does NOT do:**
- Intercept or route keyboard/mouse input
- Modify Windows display ownership
- Create virtual displays or desktops
- Install kernel drivers or system hooks
- Bypass Windows security or licensing

For true multi-seat isolation, you need:
- Windows Server with Remote Desktop Services (RDS) CALs, OR
- Specialized hardware (e.g., ASTER, VMware, dedicated multi-seat solutions), OR
- Linux with multi-seat X11/Wayland configuration

---

## Getting Help

- **Documentation:** See other guides in the `docs/` folder
- **Diagnostics:** Use the Diagnostics page to generate a system report
- **Logs:** Check `%LOCALAPPDATA%\FreeSeat\Logs\` for detailed logs

---

*FreeSeat 1.0.0 — Two seats. One PC.*
3. Confirm.

### What Gets Removed

- Application files from `C:\Program Files\FreeSeat\`
- Start Menu shortcuts
- Desktop shortcut (if created)
- Uninstaller entry
- Registry run entry (if "Start with Windows" was enabled)

### What Is Preserved

- User configuration in `%APPDATA%\FreeSeat\` (seats.json, settings.json, backups)
- Logs in `%LOCALAPPDATA%\FreeSeat\Logs\`

To completely remove all data, delete these folders manually after uninstall.
1. The seat card shows a **Start** button.
2. Click **Start** to initiate the seat runtime.
3. FreeSeat will:
   - Validate all assignments are still current
   - Record the seat as "Active" in its runtime state
   - Monitor the assigned session for changes

### Stopping a Seat

1. Click **Stop** on an active seat card.
2. The seat returns to **Ready** state (assignments preserved).

### Seat Persistence

All seat configurations (assignments, names, enabled/disabled state) are automatically saved to:

```
%APPDATA%\FreeSeat\seats.json
```

Configurations survive reboots and application restarts.

### Configuring a Seat

1. Click on a seat card to expand its configuration panel.
2. For each component, click the dropdown and select a device:
   - **Monitor** — Select from detected displays
   - **Keyboard** — Select from detected HID keyboards
   - **Mouse** — Select from detected HID mice
   - **Session** — Select from available Windows sessions
3. Changes are saved automatically.
4. The seat state updates in real-time based on your selections.

### Conflict Detection

FreeSeat automatically detects and warns about conflicts:

- **Duplicate Monitor** — Same display assigned to multiple seats
- **Duplicate Keyboard** — Same keyboard assigned to multiple seats
- **Duplicate Mouse** — Same mouse assigned to multiple seats
- **Duplicate Session** — Same Windows session assigned to multiple seats

Conflicted seats show a **Conflict** state with details in the validation panel.
12. [Uninstalling](#uninstalling)
13. [Known Limitations](#known-limitations)

---

## What is FreeSeat?

FreeSeat is a Windows application that helps you configure and manage **multi-seat** setups on a single Windows 11 PC. A "seat" represents a complete user workspace consisting of:

- **One monitor** (display)
- **One keyboard**
- **One mouse**
- **One Windows user session**

With FreeSeat, you can configure two independent seats on one physical machine, allowing two people to work simultaneously — each with their own display, input devices, and Windows session.

> **Important:** FreeSeat provides **configuration and monitoring** for multi-seat setups. True hardware-level input isolation (where Keyboard A only controls Seat A and Keyboard B only controls Seat B) requires additional Windows features not available on consumer editions of Windows. See [Known Limitations](#known-limitations) for details.

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
| Privileges | Administrator required for installation only |
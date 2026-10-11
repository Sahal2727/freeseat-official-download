# Troubleshooting Guide

## Version 1.0.0

---

## Quick Diagnostics

Before diving into specific issues, run the built-in diagnostics:

1. Open FreeSeat → **Diagnostics**
2. Click **Copy to Clipboard**
3. Paste into a text editor to review
4. Include this output when reporting issues

---

## Installation Issues

### Installer fails with "Access Denied"

**Cause:** Not running as Administrator.

**Fix:** Right-click `FreeSeat-Setup.exe` → **Run as Administrator**.

### Installer fails with "This app can't run on your PC"

**Cause:** 32-bit Windows or unsupported version.

**Fix:** FreeSeat requires Windows 11 64-bit (Build 22000+). Check `winver`.

### Installer completes but shortcuts missing

**Cause:** "Desktop Shortcut" or "Start Menu" options were unchecked during install.

**Fix:** Re-run installer, enable both options, or create shortcuts manually:
- Desktop: Right-click `FreeSeat.exe` → **Send to** → **Desktop (create shortcut)**
- Start Menu: Pin `FreeSeat.exe` from `C:\Program Files\FreeSeat\`

### "FreeSeat-Setup.exe is not a valid Win32 application"

---

## Application Won't Start

### Nothing happens when clicking FreeSeat

**Possible causes:**
1. .NET 9.0 Desktop Runtime not installed
2. Application crashed silently
3. Antivirus blocking execution

**Steps:**
1. Open Command Prompt in `C:\Program Files\FreeSeat\`
2. Run: `FreeSeat.exe`
3. Observe console output for errors
4. Check `%LOCALAPPDATA%\FreeSeat\Logs\` for log files

### "The application to execute does not exist" / "FreeSeat.App.dll not found"

**Cause:** Incomplete installation or files deleted.

**Fix:** Reinstall FreeSeat.

### Application crashes immediately on startup

**Check:**
1. Windows Event Viewer → Windows Logs → Application → Look for "FreeSeat" or ".NET Runtime" errors
2. `%LOCALAPPDATA%\FreeSeat\Logs\` for startup logs
3. Ensure Windows 11 21H2+ (Build 22000+)
4. Try repairing .NET 9.0 Desktop Runtime via "Installed Apps"

---

## Hardware Detection Issues

### No monitors detected

**Check:**
1. Both monitors powered on and connected
2. Windows Settings → System → Display → Both monitors shown
3. GPU drivers up to date
4. Click **Refresh** on Hardware page

### No keyboards/mice detected

**Check:**
1. Devices connected and working in Windows
2. Device Manager → Keyboards / Mice → No yellow warnings
3. For Bluetooth: paired and connected
4. Click **Refresh** on Hardware page
5. Some HID devices may not enumerate via WMI — this is a known limitation

### Hardware list doesn't update after plugging/unplugging

**Fix:**
1. Click **Refresh** on Hardware page
2. Wait up to 30 seconds (auto-refresh interval)
3. Restart FreeSeat if needed
4. Check **Settings → Advanced → Auto-Refresh Interval**

### Wrong monitor/keyboard/mouse identified

**Cause:** WMI enumeration order may not match physical layout.

**Workaround:**
1. Use **Hardware** page to note vendor/product IDs
2. Physically label devices
3. Assign carefully in **Seats** configuration
**Cause:** Corrupted download.

**Fix:** Re-download and verify SHA-256 checksum:
```powershell
certutil -hashfile FreeSeat-Setup.exe SHA256
```
Compare with `SHA256SUMS.txt` from the release page.
---

## Session Issues

### "Launch Test Process" button missing

**Cause:** Developer Mode not enabled.

**Fix:** Settings → Advanced → Enable **Developer Mode** → Restart FreeSeat.

### "Launch Test Process" fails with "Access Denied"

**Cause:** FreeSeat not running as Administrator.

**Fix:** 
1. Close FreeSeat
2. Right-click FreeSeat shortcut → **Run as Administrator**
3. Developer Mode session launch requires admin privileges

### Session launch works but notepad doesn't appear

**Cause:** Session 0 (System) or disconnected session.

**Fix:** Launch into an **Active** user session (Session 1+), not Session 0.

### Can't see other user's session

**Cause:** Fast User Switching not used, or only one user logged in.

**Fix:** 
1. Create second user account
2. Press Win+L → Sign in as other user (don't sign out first)
3. Both sessions now exist simultaneously

---

## Notification Issues

### No toast notifications appear

**Check:**
1. FreeSeat **Settings → Notifications → Enable Notifications** = ON
2. Windows Settings → System → Notifications → FreeSeat = ON
3. Windows Settings → System → Focus Assist → Off or "Priority only" with FreeSeat allowed
4. Not running in "Do Not Disturb" mode

### Notifications appear but clicking does nothing

**Cause:** Known limitation in current version.

**Workaround:** Open FreeSeat manually and navigate to the relevant page.

---

## Settings & Persistence Issues

### Settings not saved

**Check:**
1. `%APPDATA%\FreeSeat\settings.json` exists and is writable
2. Not running from read-only location
3. Antivirus not blocking writes to AppData

### Seat configuration lost after reboot

**Check:**
1. `%APPDATA%\FreeSeat\seats.json` exists
2. File not being deleted by cleanup tools
3. Write permissions on `%APPDATA%\FreeSeat\`

### Theme not applying

**Fix:** 
1. Settings → General → Theme → Select desired option
2. Restart FreeSeat if needed
3. "System" theme follows Windows Settings → Personalization → Colors → Choose your mode

---

## Performance Issues

### High CPU usage

---

## Uninstall Issues

### Uninstaller won't run

**Fix:**
1. Run as Administrator
2. Or use Windows Settings → Apps → Installed Apps → FreeSeat → Uninstall
3. If corrupted, manually delete `C:\Program Files\FreeSeat\` and remove Start Menu shortcuts

### "FreeSeat is still running" during uninstall

**Fix:** 
1. Close FreeSeat from system tray (right-click tray icon → Exit)
2. Check Task Manager for `FreeSeat.exe` or `FreeSeat.App.exe` processes
3. End task, then retry uninstall

### User data not removed after uninstall

**By design:** Uninstaller preserves `%APPDATA%\FreeSeat\` and `%LOCALAPPDATA%\FreeSeat\` to retain user configuration.

**To fully remove:**
```powershell
Remove-Item "$env:APPDATA\FreeSeat" -Recurse -Force
Remove-Item "$env:LOCALAPPDATA\FreeSeat" -Recurse -Force
```

---

## Log Files

### Location

```
%LOCALAPPDATA%\FreeSeat\Logs\
```

### Log Levels (Settings → Advanced)

| Level | Use Case |
|-------|----------|
| **Debug** | Development, detailed troubleshooting (verbose) |
| **Information** | Normal operation (default) |
| **Warning** | Only warnings and errors |
| **Error** | Only errors |

### Sharing Logs for Support

1. Zip the entire `Logs` folder
2. Include Diagnostics export (Diagnostics page → Copy to Clipboard)
3. Describe steps to reproduce the issue

---

## Common Error Codes

| Error | Meaning | Action |
|-------|---------|--------|
| `WTSAPI_ERROR` | Session API failure | Check admin rights, Windows version |
| `WMI_QUERY_FAILED` | Hardware detection failed | Run as admin once, check WMI service |
| `CONFIG_SAVE_FAILED` | Cannot write seats.json | Check AppData permissions |
| `SESSION_LAUNCH_FAILED` | CreateProcessAsUser failed | Requires admin, valid user token |

---

## Getting More Help

1. **Check Diagnostics** — Built-in system report
2. **Review Logs** — `%LOCALAPPDATA%\FreeSeat\Logs\`
3. **Search Issues** — GitHub Issues (if repository is public)
4. **Create Issue** — Include diagnostics, logs, steps to reproduce

---

*FreeSeat 1.0.0 — Two seats. One PC.*
**Check:**
1. **Settings → Advanced → Auto-Refresh Interval** — increase from default 30s
2. Hardware detection runs on interval; reduce frequency if needed
3. Check logs for repeated errors causing retry loops

### High memory usage

**Normal:** ~100-200 MB for WPF application.

**If excessive:**
1. Restart FreeSeat
2. Check for memory leaks in logs
3. Report with diagnostics if persistent

### Slow UI response

**Check:**
1. Debug logging enabled? (Settings → Advanced → Log Level = Debug)
2. Switch to **Information** or **Warning** log level
3. Large number of connected HID devices?
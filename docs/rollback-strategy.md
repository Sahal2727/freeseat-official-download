# Rollback Strategy — FreeSeat 1.0.0

## Purpose

This document describes how to roll back a problematic FreeSeat 1.0.0 release. It covers uninstallation, configuration backup, version rollback, and data considerations.

---

## When to Roll Back

Consider rollback if 1.0.0 exhibits:
- Application crashes on startup
- Installer failures on supported systems
- Data corruption or loss
- Security regression
- Critical functionality regression

---

## Rollback Procedures

### 1. User-Level Rollback (Uninstall Current Version)

**For end users experiencing issues:**

#### Via Windows Settings (Recommended)
1. Open **Settings** → **Apps** → **Installed Apps**
2. Find **FreeSeat**
3. Click **...** → **Uninstall**
4. Confirm uninstallation

#### Via Start Menu
1. Open Start Menu → **FreeSeat** folder
2. Click **Uninstall FreeSeat**
3. Confirm

#### What Gets Removed
- Application files: `C:\Program Files\FreeSeat\`
- Start Menu shortcuts
- Desktop shortcut (if created)
- Uninstaller entry
- Registry Run entry (if "Start with Windows" enabled)

#### What Is Preserved (By Design)
- User configuration: `%APPDATA%\FreeSeat\seats.json`
- User settings: `%APPDATA%\FreeSeat\settings.json`
- Backups: `%APPDATA%\FreeSeat\Backups\`
- Logs: `%LOCALAPPDATA%\FreeSeat\Logs\`

#### Complete Removal (If Needed)
```powershell
# Run as the affected user (not admin)
Remove-Item "$env:APPDATA\FreeSeat" -Recurse -Force
Remove-Item "$env:LOCALAPPDATA\FreeSeat" -Recurse -Force
---

### 2. Administrator Rollback (System-Wide)

**For IT administrators deploying via managed environment:**

#### Silent Uninstall
```cmd
"C:\Program Files\FreeSeat\unins000.exe" /SILENT /NORESTART
```

#### Group Policy / SCCM / Intune
- Deploy uninstall command above
- Or use Windows "Apps & Features" enterprise management

#### Cleanup Machine-Wide Artifacts
```powershell
# Run as SYSTEM or admin
Remove-Item "C:\Program Files\FreeSeat" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "C:\ProgramData\Microsoft\Windows\Start Menu\Programs\FreeSeat" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "C:\Users\Public\Desktop\FreeSeat.lnk" -Force -ErrorAction SilentlyContinue
# Note: User-specific AppData must be cleaned per-user
```

---

### 3. Configuration Backup & Restore

#### Automatic Backups
FreeSeat creates timestamped backups on each configuration change:
```
%APPDATA%\FreeSeat\Backups\seats-<timestamp>.json
%APPDATA%\FreeSeat\Backups\settings-<timestamp>.json
```

#### Manual Backup (Before Upgrade/Rollback)
```powershell
$backupDir = "$env:APPDATA\FreeSeat\ManualBackup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-Item -ItemType Directory -Path $backupDir | Out-Null
Copy-Item "$env:APPDATA\FreeSeat\seats.json" $backupDir
Copy-Item "$env:APPDATA\FreeSeat\settings.json" $backupDir
Write-Host "Backed up to: $backupDir"
```

#### Restore from Backup
1. Open FreeSeat → **Settings** → **Backup & Restore**
2. Click **Restore Backup**
3. Select desired backup from list
4. Confirm

#### Manual Restore
```powershell
# Replace with actual backup path
$backup = "$env:APPDATA\FreeSeat\ManualBackup-20261008-120000"
Copy-Item "$backup\seats.json" "$env:APPDATA\FreeSeat\seats.json" -Force
Copy-Item "$backup\settings.json" "$env:APPDATA\FreeSeat\settings.json" -Force
# Restart FreeSeat
```

---

### 4. Version Rollback (Reinstall Previous Version)

#### If Previous Installer Available
1. Uninstall current version (see Section 1)
2. Run previous version's installer (e.g., `FreeSeat-Setup-0.9.0.exe`)
3. Configuration in `%APPDATA%\FreeSeat\` is compatible with 0.9.0+

#### If No Previous Installer
1. Uninstall current version
2. Download previous release from GitHub Releases (if published)
3. Or build from source at previous tag:
   ```bash
   git checkout v0.9.0  # or whatever previous tag
   dotnet build -c Release
   # Run installer or copy dist/ files manually
   ```

#### Configuration Compatibility
- **Forward compatible:** 1.0.0 config works with future versions
- **Backward compatible:** 1.0.0 config works with 0.9.0+ (seat config v1)
- **Breaking:** Pre-0.9.0 configs not supported (different schema)

---

### 5. Release Artifact Identification

#### Current Release (1.0.0)
| Artifact | Location | SHA-256 |
|----------|----------|---------|
| Installer | `dist/FreeSeat-Setup.exe` | `37c565c84b2f1b85df103ff576bcaf71799b5e465faa833b6b9a9caad39ff72d` |
| Checksums | `dist/SHA256SUMS.txt` | N/A |

#### Previous Releases
| Version | Tag | Installer | Notes |
|---------|-----|-----------|-------|
| 0.9.0 | v0.9.0 | `FreeSeat-Setup-0.9.0.exe` | Pre-release |
| 0.8.0 | v0.8.0 | `FreeSeat-Setup-0.8.0.exe` | Pre-release |

---

## Data Considerations

### User Data Locations

| Data Type | Location | Backup Strategy |
|-----------|----------|-----------------|
| Seat Configuration | `%APPDATA%\FreeSeat\seats.json` | Auto-backup on change + manual export |
| Application Settings | `%APPDATA%\FreeSeat\settings.json` | Auto-backup on change |
| Backups | `%APPDATA%\FreeSeat\Backups\` | Timestamped, retained indefinitely |
| Logs | `%LOCALAPPDATA%\FreeSeat\Logs\` | Rotating, not backed up |

### Data Migration

- **No migration needed for rollback** — Config schema v1 stable since 0.9.0
- **Forward migration** — Newer versions read v1 config, may add fields
- **Downgrade risk** — Newer config fields ignored by older versions (safe)

### Data Retention Policy

| Artifact | Retention |
|----------|-----------|
| Current config (seats.json, settings.json) | Until user deletes |
| Auto-backups | Indefinite (manual cleanup) |
| Manual exports | User-controlled |
| Logs | 30 days (rolling) |

---

## Rollback Decision Matrix

| Scenario | Action |
|----------|--------|
| Installer fails on clean machine | Report bug; try previous installer |
| App crashes on startup | Uninstall; restore config from backup; try previous version |
| Seat config corrupted | Restore from `%APPDATA%\FreeSeat\Backups\` |
| Settings cause issues | Reset via Settings → Backup & Restore → Restore Defaults |
| Security issue discovered | Immediate uninstall; await patched release |
| Performance regression | Uninstall; reinstall previous version |

---

## Communication

### For Users
- Check GitHub Releases for rollback announcements
- Previous installers remain available on GitHub Releases
- Documentation updated with known issues

### For Developers
- Tag problematic release as `v1.0.0-rollback` (if needed)
- Create hotfix branch from last known good tag
- Increment patch version: `1.0.1`

---

## Testing Rollback

Before declaring release ready, verify:
1. ✅ Clean install of 1.0.0
2. ✅ Configure seats, create backups
3. ✅ Uninstall 1.0.0
4. ✅ Verify config preserved in AppData
5. ✅ Install previous version (0.9.0)
6. ✅ Verify previous version reads config
7. ✅ Reinstall 1.0.0
8. ✅ Verify 1.0.0 reads config

---

## Emergency Contacts

**Support contact not configured yet.**

---

*FreeSeat 1.0.0 — Two seats. One PC.*
```
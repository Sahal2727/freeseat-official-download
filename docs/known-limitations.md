# Known Limitations

## Version 1.0.0

---

## Critical Limitation — Read This First

> **TRUE PHYSICAL KEYBOARD/MOUSE/DISPLAY ISOLATION IS NOT ENFORCED ON NORMAL CONSUMER WINDOWS.**

This is the single most important thing to understand about FreeSeat 1.0.0.

**FreeSeat is a configuration and monitoring tool.** It records which hardware devices you intend to belong to which seat, detects conflicts, monitors Windows sessions, and provides a framework for multi-seat management. **It does not — and cannot on consumer Windows — enforce hardware-level isolation.**

---

## What "Not Enforced" Means in Practice

| Scenario | What Happens |
|----------|--------------|
| User A types on Keyboard A | Keystrokes go to the **currently active desktop** (could be User B's session) |
| User B moves Mouse B | Cursor moves on the **currently active desktop** (could be User A's session) |
| Both monitors connected | Windows decides which session shows on which monitor (mirror/extend), not FreeSeat |
| User A plays audio | Audio plays through **system default device** (shared) |
| GPU renders for both | Single GPU context, no resource partitioning |

**Two users sitting at two seats will see each other's cursors, receive each other's keystrokes, and share the same desktop environment.**
---

## Capability Matrix

| Capability | Consumer Windows 11 | Windows Server + RDS | FreeSeat 1.0.0 |
|------------|---------------------|----------------------|----------------|
| Monitor assignment | ❌ | ✅ (RDP) | ❌ Config only |
| Keyboard routing | ❌ | ✅ (RDP) | ❌ Config only |
| Mouse routing | ❌ | ✅ (RDP) | ❌ Config only |
| Audio isolation | ❌ | ✅ (RDP) | ❌ Config only |
| GPU partitioning | ❌ | Partial (vGPU) | ❌ |
| Independent desktops | ❌ | ✅ (Multi-session) | ❌ |
| Session enumeration | ✅ | ✅ | ✅ |
| Session launch | ✅ (Admin) | ✅ | ✅ |
| Hardware detection | ✅ | ✅ | ✅ |
| Conflict detection | N/A | N/A | ✅ |
| Config persistence | N/A | N/A | ✅ |

---

## Why Can't FreeSeat Enforce Isolation?

### Windows Architecture

On consumer Windows (Home/Pro):
- **Single interactive session** — Only one user can be "active" at a time (Fast User Switching suspends the other)
- **Input subsystem** — All keyboards/mice feed into a single input stream; no per-device routing
- **Display subsystem** — One desktop spans all monitors; no per-session display ownership
- **Security model** — User-mode apps cannot intercept or filter input without kernel drivers

### FreeSeat's Design Constraints

FreeSeat explicitly **rejects** the following approaches:

| Approach | Why Rejected |
|----------|--------------|
| Kernel-mode input filter driver | Requires WHQL signing, kernel debugging, high risk |
| Global Windows hooks (`SetWindowsHookEx`) | Captures all input — privacy/security violation |
| DLL injection into explorer.exe | Unstable, flagged by antivirus, unsupported |
| Raw Input API (`RegisterRawInputDevices`) | Receives all input, cannot suppress/route per-session |
| Modifying `termsrv.dll` | Illegal (bypasses licensing), breaks Windows Update |
| Disabling Windows Defender | Security violation, unacceptable |
| RDP wrapper / RDS patch | Licensing violation, unsupported |

**FreeSeat operates entirely within documented, user-mode Windows APIs.** This is a deliberate security and stability choice.
---

## What FreeSeat Actually Does

### ✅ Implemented and Working

1. **Seat Configuration Model**
   - Two seats with persistent JSON configuration
   - Device assignment (monitor, keyboard, mouse, session)
   - Conflict detection (duplicate assignments)
   - State machine (Unconfigured → Incomplete → Ready → Active)

2. **Hardware Detection**
   - Monitor enumeration via WMI (EDID, resolution, refresh rate)
   - Keyboard/mouse enumeration via WMI (HID vendor/product IDs)
   - GPU, audio device enumeration
   - Hardware change notifications (WMI events)

3. **Windows Session Management**
   - Session enumeration via WTSAPI32
   - Session state tracking (Active, Connected, Disconnected, Locked, etc.)
   - Session change notifications
   - Process-to-session mapping
   - Safe process launch in target session (`WTSQueryUserToken` + `CreateProcessAsUser`)

4. **Runtime Orchestration**
   - Seat lifecycle management
   - State reconciliation (periodic sync)
   - Event history and monitoring

5. **User Interface**
   - Dashboard with seat overview
   - Detailed seat configuration UI
   - Sessions monitoring view
   - Hardware inventory view
   - Diagnostics and compatibility checks
   - Settings with backup/restore/import/export
   - Toast notifications

6. **Installer**
   - Inno Setup professional installer
   - Admin install to `Program Files`
   - Desktop/Start Menu shortcuts
   - Uninstaller
   - Start-with-Windows option

---

## What FreeSeat Does NOT Do

### ❌ Not Implemented (By Design)

| Feature | Reason |
|---------|--------|
| Input interception/routing | Requires kernel driver or global hooks |
| Display ownership assignment | Not possible in user-mode on consumer Windows |
| Audio stream isolation | Requires Windows Audio Session API per-session, not exposed |
| GPU resource partitioning | Requires vGPU / Windows Server |
| Virtual desktop creation | Not exposed to user-mode apps |
| Automatic session switching | Would disrupt user workflow |
| Game/Application-specific profiles | Out of scope for 1.0.0 |
| Network multi-seat | Out of scope |

### ❌ Not Implemented (Technical Debt)

| Feature | Status |
|---------|--------|
| SetupAPI / DXGI display detection | WMI-only currently |
| Bluetooth device battery status | Not exposed via WMI reliably |
| High-DPI scaling per monitor | WPF handles globally |
| Multi-language support | English only in 1.0.0 |
| Automated hardware pairing wizard | Manual assignment only |
---

## Workarounds for True Multi-Seat

If you need **actual** independent simultaneous use:

### Option 1: Windows Server + RDS CALs (Official Microsoft Way)
- Install Windows Server 2019/2022
- Add Remote Desktop Services role
- Purchase RDS CALs per user/device
- Connect via RDP from thin clients or second PC
- **Cost:** Windows Server license + CALs

### Option 2: Commercial Multi-Seat Software
- **ASTER** (ibik.ru) — Multiple seats on one Windows
- **BeTwin** — Similar concept
- **Cost:** Per-seat licenses

### Option 3: Virtualization
- Run two VMs (Hyper-V, VMware, VirtualBox)
- Pass through GPU (SR-IOV / vGPU) or use software rendering
- Assign USB devices to each VM
- **Cost:** Hardware with IOMMU/VT-d, powerful CPU/GPU

### Option 4: Linux Multi-Seat
- Ubuntu/Fedora with `loginctl` seat management
- `systemd` handles seat assignment natively
- X11 or Wayland with `seatd`
- **Cost:** Free, but requires Linux expertise

### Option 5: Two Physical PCs
- KVM switch for shared peripherals
- Synergy / Mouse Without Borders for keyboard/mouse sharing
- **Cost:** Second PC

---

## Honest Marketing Statement

> **FreeSeat 1.0.0 does not enable two people to independently use one Windows 11 PC simultaneously with true hardware isolation.**
>
> It provides a professional configuration framework, hardware detection, session monitoring, and conflict detection for multi-seat setups. True isolation requires Windows Server with RDS, commercial multi-seat software, virtualization, or Linux.
>
> We are transparent about this limitation because misleading users would violate our security-first principles and the trust of the open-source community.

---

## Future Roadmap

| Milestone | Planned Capability |
|-----------|-------------------|
| M5: Input Isolation | Research only — not feasible on consumer Windows without kernel driver |
| M6: Independent Seats | Requires Windows Server or virtualization |
| M7: Gaming | Not applicable without true isolation |
| M8: Safety & Recovery | Configuration backup/restore (done), runtime safety (partial) |
| M9: Final UI | Complete in 1.0.0 |
| M10: Release | **You are here** |

---

## Reporting Issues Related to Limitations

**Do not file bugs** for:
- "Keyboard A controls Seat B" — This is expected behavior
- "Both monitors show same desktop" — This is expected behavior
- "Can't hear separate audio" — This is expected behavior

**Do file bugs** for:
- Configuration not saving correctly
- Hardware not detected when Windows sees it
- Session enumeration missing active sessions
- Installer failures
- Application crashes
- UI bugs in configuration flow

---

*FreeSeat 1.0.0 — Two seats. One PC. (Configuration only on consumer Windows)*
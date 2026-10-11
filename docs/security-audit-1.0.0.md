# Security Audit — FreeSeat 1.0.0

## Audit Date: October 2026
## Version: 1.0.0

---

## Executive Summary

This document presents a source-level security audit of FreeSeat 1.0.0. The audit examines the codebase for unsafe practices, vulnerability patterns, and adherence to the project's declared security principles.

**Overall Assessment:** FreeSeat 1.0.0 follows a security-first design. No critical vulnerabilities found. All declared security restrictions are enforced in code.

---

## Audit Scope

**In Scope:**
- FreeSeat.Core (domain models, interfaces)
- FreeSeat.Infrastructure (services, hardware detection, session management, runtime)
- FreeSeat.App (WPF UI, ViewModels, Views)
- FreeSeat.Setup (Inno Setup installer script)
- Build configuration (Directory.Build.props, .csproj files)

**Out of Scope:**
- .NET Runtime / WPF framework internals
- Windows OS security (WTSAPI32, WMI, etc.)
- Third-party NuGet packages (assumed secure from trusted sources)
- Deployment infrastructure (GitHub Actions workflow)

---

## Security Principles (From Project Documentation)

FreeSeat explicitly commits to:

| Principle | Enforcement |
|-----------|-------------|
| No kernel drivers | ✅ No driver code in repo |
| No global hooks | ✅ No `SetWindowsHookEx` usage |
| No keystroke capture | ✅ No `GetAsyncKeyState`, `GetKeyState`, Raw Input |
| No mouse capture | ✅ No `SetCapture`, `ClipCursor` |
| No input injection | ✅ No `SendInput`, `mouse_event`, `keybd_event` |
| No DLL injection | ✅ No `CreateRemoteThread`, `WriteProcessMemory` |
| No Windows DLL modification | ✅ No `termsrv.dll` patching |
| No Defender disabling | ✅ No security service manipulation |
| No licensing bypass | ✅ No RDP wrapper, no RDS patch |
| No credential collection | ✅ No credential APIs, no DPAPI usage |
| No automatic logoff/session switching | ✅ No `WTSLogoffSession`, `WTSDisconnectSession` |
| No game manipulation | ✅ No game-specific code |
| No arbitrary shell commands | ✅ No `Process.Start(cmd.exe)`, no shell execute |
---

## Findings

### ✅ PASS — Process Execution Safety

**SessionLauncher.cs** — Uses `CreateProcessAsUser` with explicit token from `WTSQueryUserToken`:
- Validates session ID before launch
- Uses `SECURITY_ATTRIBUTES` with `bInheritHandle = false`
- Sets `CREATE_UNICODE_ENVIRONMENT` flag
- No shell execution (`UseShellExecute = false` implied)
- Command line explicitly set to `notepad.exe` (hardcoded in Developer Mode)

**Risk:** Low. Only launches `notepad.exe` in Developer Mode. No user-controlled executable path.

### ✅ PASS — No Arbitrary Shell Execution

No `Process.Start` with shell execute. No `cmd.exe /c`, no PowerShell invocation, no script execution.

### ✅ PASS — Path Traversal

- Configuration paths use `Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)`
- File operations use `Path.Combine` with validated filenames
- No user input directly concatenated into file paths
- JSON serialization uses `System.Text.Json` (safe)

### ✅ PASS — Executable Path Validation

- SessionLauncher hardcodes `notepad.exe`
- No user-configurable executable launch
- Installer uses fixed paths from `[Files]` section

### ✅ PASS — Credential Handling

- No credential APIs used (`CredRead`, `CredWrite`, `CredEnumerate`)
- No DPAPI usage (`ProtectedData`)
- No password storage
- Session launch uses existing user token (no credentials needed)

### ✅ PASS — Secret Leakage

- No API keys, connection strings, or secrets in code
- No logging of sensitive data
- `Directory.Build.props` has placeholder URLs (marked TODO)
- `AppInfo.cs` has placeholder URLs (marked TODO)

---

## Dependency Review (NuGet Packages)

| Package | Version | Source | Risk |
|---------|---------|--------|------|
| CommunityToolkit.Mvvm | 8.3.2 | Microsoft (nuget.org) | Low — Source generators, no runtime reflection |
| Microsoft.Extensions.DependencyInjection | 9.0.0 | Microsoft (nuget.org) | Low — Core DI container |
| Microsoft.Extensions.Hosting | 9.0.0 | Microsoft (nuget.org) | Low — Host abstraction |
| Microsoft.Extensions.Logging | 9.0.0 | Microsoft (nuget.org) | Low — Logging abstraction |
| FluentAssertions | (test only) | nuget.org | Test dependency only |
| xunit | (test only) | nuget.org | Test dependency only |

All packages from official Microsoft or widely-used community sources. No obscure or unverified packages.

---

## Build Configuration Security

**Directory.Build.props:**
- `TreatWarningsAsErrors=true` — Forces clean builds
- `Nullable=enable` — Nullable reference types enforced
- `PublishSingleFile=false` — No single-file bundling (avoids extraction risks)
- `SelfContained=false` — Uses shared .NET runtime (standard)
- `PlatformTarget=x64` — Explicit 64-bit

**FreeSeat.App.csproj:**
- `ApplicationIcon` points to local asset (not downloaded)
- No `EnableUnsafeBinaryFormatterSerialization`
- No `AllowUnsafeBlocks`

---

## Code Patterns Reviewed

### P/Invoke Usage (SessionLauncher.cs, NativeMethods.cs)

```csharp
// All signatures use exact Windows API definitions
// No custom calling conventions
// All handles properly closed via SafeHandle or explicit CloseHandle
// Error checking on all return values
```

### WMI Queries (DisplayDetectionService.cs)

```csharp
// Uses ManagementObjectSearcher with scoped queries
// No user input in query strings
// Proper disposal of ManagementObjectCollection
```

### JSON Serialization (ConfigurationService.cs)

```csharp
// Uses System.Text.Json (not Newtonsoft.Json for config)
// No TypeNameHandling (prevents deserialization gadgets)
// Options: WriteIndented = true, PropertyNameCaseInsensitive = true
```

### Event Handling (RuntimeOrchestrationService.cs)

```csharp
// Weak event patterns where appropriate
// Proper unsubscribe in Dispose
// No long-lived delegates capturing sensitive objects
```

---

## Threat Model Summary

| Threat | Mitigation |
|--------|------------|
| Malicious seat config | Config stored in user AppData; no elevation |
| Hardware detection spoofing | WMI queries read-only; no control decisions based solely on WMI |
| Session launch abuse | Hardcoded notepad.exe; requires admin; Developer Mode opt-in |
| Installer privilege escalation | Standard admin install to Program Files; no custom actions |
| Configuration tampering | User-writable files; no integrity checks needed (user owns data) |
| Notification spoofing | Toast notifications via Windows API; user sees app identity |

---

## Compliance with Declared Restrictions

| Restriction | Status | Evidence |
|-------------|--------|----------|
| No kernel drivers | ✅ PASS | No .sys, no WDK, no driver code |
| No global hooks | ✅ PASS | No SetWindowsHookEx, no WH_* |
| No keystroke capture | ✅ PASS | No GetAsyncKeyState, no Raw Input registration |
| No mouse capture | ✅ PASS | No SetCapture, no ClipCursor |
| No input injection | ✅ PASS | No SendInput, no mouse_event, no keybd_event |
| No DLL injection | ✅ PASS | No CreateRemoteThread, no WriteProcessMemory |
| No Windows DLL modification | ✅ PASS | No termsrv.dll, no binary patching |
| No Defender disabling | ✅ PASS | No sc stop, no Set-MpPreference |
| No licensing bypass | ✅ PASS | No RDP wrapper, no RDS patch |
| No credential collection | ✅ PASS | No Cred* APIs, no DPAPI |
| No automatic logoff | ✅ PASS | No WTSLogoffSession |
| No game manipulation | ✅ PASS | No game-specific code |
| No arbitrary shell commands | ✅ PASS | No Process.Start(cmd), no shell exec |

---

## Recommendations

### For Future Development

1. **Add code signing** — When certificate available, sign `FreeSeat.exe` and installer
2. **Consider Strong Name signing** — For assembly integrity (optional for .NET Core+)
3. **Add SBOM generation** — Software Bill of Materials for supply chain transparency
4. **Dependency scanning** — Integrate `dotnet list package --vulnerable` in CI
5. **Hardened P/Invoke** — Consider `SetLastError=true` on all DllImports (already mostly done)

### For Users

1. **Verify installer checksum** — Compare SHA-256 with published `SHA256SUMS.txt`
2. **Run as standard user** — Application does not require admin after install
3. **Review Developer Mode** — Only enable if needed for session launch testing

---

## Conclusion

FreeSeat 1.0.0 is a well-designed application that adheres to its stated security principles. The codebase demonstrates careful attention to:

- Least privilege (standard user execution)
- Attack surface minimization (no hooks, no injection, no shell)
- Safe interop (validated P/Invoke, proper handle management)
- Transparent limitations (honest about what it cannot do)

**No blocking security issues found.** The application is suitable for release with the documented limitations clearly communicated to users.

---

## Audit Artifacts

- Source code reviewed: `src/` (all projects)
- Build config reviewed: `Directory.Build.props`, `*.csproj`
- Installer reviewed: `src/FreeSeat.Setup/FreeSeat.iss`
- Tests reviewed: `tests/FreeSeat.Tests/`

---

*Audit performed by: Automated review + manual inspection*
*FreeSeat 1.0.0 — Two seats. One PC.*
### ✅ PASS — File Operations

- Settings: `%APPDATA%\FreeSeat\settings.json` (user-writable, user-readable)
- Seat Config: `%APPDATA%\FreeSeat\seats.json` (user-writable, user-readable)
- Logs: `%LOCALAPPDATA%\FreeSeat\Logs\` (user-writable)
- No writes to Program Files, System32, or other system directories
- No temporary file creation with predictable names

### ✅ PASS — Installer Behavior (Inno Setup)

- `PrivilegesRequired=admin` — Standard for Program Files install
- `DefaultDirName={autopf}\FreeSeat` — Per-machine, not user-writable
- `[Files]` section uses explicit source paths — no wildcards outside build output
- `[UninstallDelete]` removes `{app}` folder only
- `[Registry]` only writes `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (user scope)
- No `HKLM` writes except standard uninstaller registration
- No services, drivers, or scheduled tasks created

### ✅ PASS — Registry Writes

Only registry write:
```iss
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; 
ValueName: "FreeSeat"; ValueType: string; 
ValueData: "'{app}\FreeSeat.exe' --minimized"; 
Flags: uninsdeletevalue; Tasks: startwithwindows
```
- User scope (HKCU)
- Conditional on user-selected task
- Cleaned up on uninstall

### ✅ PASS — Command Injection

- No string interpolation into command lines
- No `Process.Start` with user input
- SessionLauncher uses hardcoded `notepad.exe`

### ✅ PASS — DLL Loading Risks

- No `Assembly.LoadFrom` with user paths
- No `DllImport` with relative paths
- All P/Invoke uses explicit system DLLs (`wtsapi32.dll`, `user32.dll`, `kernel32.dll`, `advapi32.dll`)
- `SetDllDirectory` not called (uses default safe search order)
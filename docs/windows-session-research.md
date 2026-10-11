# Windows Session Architecture Research

## Overview

This document summarizes the research conducted for Milestone 2 of FreeSeat regarding Windows session architecture, relevant APIs, and feasibility of multi-seat operation using standard Windows functionality.

## Windows Session Architecture

### Session Concepts

| Concept | Description |
|---------|-------------|
| **Session** | A Windows session represents a single user'\''s interactive logon environment. Each session has a unique Session ID. |
| **Session 0** | Reserved for system services. Non-interactive. Runs services and drivers. |
| **Console Session** | The session connected to the physical monitor/keyboard/mouse (typically Session 1). |
| **Remote Session** | Sessions created via Remote Desktop Services (RDP). |
| **Interactive Session** | A session where a user can interact with the desktop (has a window station and desktop). |

### Session IDs

- **Session 0**: Services (non-interactive)
- **Session 1**: First console session (typically the first user to log on)
- **Session 2+**: Additional sessions (Fast User Switching, RDP)

### Key Windows Objects

| Object | Purpose |
|--------|---------|
| **Window Station** | Contains clipboard, atoms, and desktops. Only one interactive window station per session (WinSta0). |
| **Desktop** | Logical display surface containing windows, menus, hooks. Each session can have multiple desktops. |
| **Session Space** | Kernel memory region per session for session-specific data. |## Windows APIs Researched

### Session Enumeration APIs

| API | Purpose | Privileges Required | Windows 11 Support | Notes |
|-----|---------|---------------------|-------------------|-------|
| `WTSEnumerateSessions` | Enumerate sessions on a terminal server | Standard user | âœ… | Legacy API, returns basic info |
| `WTSEnumerateSessionsEx` | Enumerate sessions with extended info | Standard user | âœ… | Preferred (Vista+), returns session flags |
| `WTSQuerySessionInformation` | Query specific session details | Standard user (some info requires admin) | âœ… | Supports many info classes |
| `WTSGetActiveConsoleSessionId` | Get the active console session ID | Standard user | âœ… | Returns -1 if no console session |

### Session Information Classes (WTS_INFO_CLASS)

| Class | Description | Typical Access |
|-------|-------------|----------------|
| `WTSUserName` | User name | Standard user |
| `WTSDomainName` | Domain name | Standard user |
| `WTSConnectState` | Connection state (Active, Connected, Disconnected, etc.) | Standard user |
| `WTSClientName` | Client device name (for remote sessions) | Standard user |
| `WTSIdleTime` | Time since last input | Standard user |
| `WTSLogonTime` / `WTSConnectTime` / `WTSDisconnectTime` | Session timing | Standard user |
| `WTSClientDisplay` | Client display resolution | Standard user |
| `WTSSessionInfoEx` | Extended session info (flags, etc.) | Standard user |

### Session State Values (WTS_CONNECTSTATE_CLASS)

| Value | Name | Description |
|-------|------|-------------|
| 0 | WTSActive | User logged on, session connected to client |
| 1 | WTSConnected | Session connected to client |
| 2 | WTSConnectQuery | Connecting... |
| 3 | WTSShadow | Shadowing another session |
| 4 | WTSDisconnected | Logged on but disconnected |
| 5 | WTSIdle | Idle (waiting for client) |
| 6 | WTSListen | Listening for connection |
| 7 | WTSReset | Resetting |
| 8 | WTSDown | Down |
| 9 | WTSInit | Initializing |

### Process/Session Relationship APIs

| API | Purpose | Privileges Required |
|-----|---------|---------------------|
| `ProcessIdToSessionId` | Get session ID for a process | Standard user |
| `WTSQueryUserToken` | Get user token for session | Requires admin / TCB privilege |

### Session Creation/Management APIs (Require Admin)

| API | Purpose | Privileges Required |
|-----|---------|---------------------|
| `WTSLogoffSession` | Log off a session | Admin |
| `WTSDisconnectSession` | Disconnect a session | Admin |
| `WTSConnectSession` | Connect a session | Admin |
| `CreateProcessAsUser` | Launch process in user'\''s session | User token, SE_ASSIGNPRIMARYTOKEN |
| `CreateProcessWithTokenW` | Launch process with token | Token with TOKEN_QUERY, TOKEN_DUPLICATE, TOKEN_ASSIGN_PRIMARY |

### Session Change Notifications

| Mechanism | Description | Use Case |
|-----------|-------------|----------|
| `WTSRegisterSessionNotification` | Window message-based notifications | UI apps with message pump |
| `WTSWaitSystemEvent` | Event-based synchronous wait | Background services |
| `WTSEvent* flags` | Filter for specific events (logon, logoff, connect, disconnect, etc.) | Fine-grained control |
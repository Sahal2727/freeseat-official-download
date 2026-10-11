# Display POC Implementation

## Overview

This document describes the proof-of-concept implementation for the Display Diagnostics page in FreeSeat. The POC demonstrates safe monitor detection, identification, and correlation using multiple Windows display APIs, without any display configuration changes.

## Safety Constraints (Enforced)

- **NO** display configuration changes
- **NO** session switching
- **NO** user logoff
- **NO** GPU driver modification
- **Read-only** monitoring only
- **User-initiated** only

## Implementation

### 1. Monitor Enumeration (Multiple APIs)

```csharp
// GDI APIs (Session view)
EnumDisplayDevices(null, 0, ref displayDevice, 0);
EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumProc, IntPtr.Zero);

// Display Config APIs (System view)
GetDisplayConfigBufferSizes(QDC_ALL_PATHS, out pathCount, out modeCount);
QueryDisplayConfig(QDC_ALL_PATHS, ref pathCount, pathArray, ref modeCount, modeArray, IntPtr.Zero);

// SetupAPI (PnP/EDID)
SetupDiGetClassDevs(ref GUID_DEVCLASS_MONITOR, null, IntPtr.Zero, DIGCF_PRESENT);

// DXGI (GPU view)
factory.EnumAdapters(adapterIndex, out adapter);
adapter.EnumOutputs(outputIndex, out output);

// WMI
ManagementObjectSearcher searcher = new("SELECT * FROM Win32_DesktopMonitor");
```

### 2. EDID Parsing

```csharp
// From DisplayConfig
DisplayConfigGetDeviceInfo(targetDeviceName, 
    DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_PREFERRED_MODE, out edidData);

// From SetupAPI
SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, 
    SPDRP_HARDWAREID, out regType, buffer, bufferSize, out requiredSize);

// Parse EDID 128-byte structure:
// Bytes 0-7: Header (0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00)
// Bytes 8-9: Manufacturer ID (compressed ASCII)
// Bytes 10-11: Product Code
// Bytes 12-15: Serial Number (32-bit)
// Bytes 16-17: Manufacturing Week/Year
// Bytes 18-19: EDID Version/Revision
// Bytes 20-35: Display Parameters
// Bytes 36-125: Descriptor Blocks (including monitor name, serial string)
// Byte 126: Extension Count
// Byte 127: Checksum
```

### 3. Monitor Correlation

```csharp
// Correlation chain:
HardwareDevice (MonitorDevice)
    ├─ DeviceInstanceId → SetupAPI DISPLAY device instance ID
    ├─ DeviceInterfacePath → Monitor device interface path
    ├─ EDID → Manufacturer/Model/Serial
    └─ Adapter/Output → DXGI Adapter LUID + Output Index

// Matching logic:
var hardwareMonitor = hardwareService.GetMonitorByDeviceInstanceId(setupApiDeviceId);
var dxgiOutput = dxgiAdapter.Outputs.FirstOrDefault(o => 
    o.Description.DeviceName.Equals(displayConfigTargetName));
```

### 4. Topology Display

```csharp
// Current topology from QueryDisplayConfig
var topology = "";
switch (pathInfo.targetInfo.targetMode.targetAvailable) {
    case DISPLAYCONFIG_TARGET_IN_USE:
        topology = "Active";
        break;
    case DISPLAYCONFIG_TARGET_FORCIBLE:
        topology = "Forcible";
        break;
}

// Position info
var position = $"({pathInfo.targetInfo.desktopRect.left}, {pathInfo.targetInfo.desktopRect.top})";
var resolution = $"{modeInfo.width}x{modeInfo.height}@{modeInfo.refreshRate.Numerator/modeInfo.refreshRate.Denominator}Hz";
var primary = pathInfo.targetInfo.desktopRect.left == 0 && pathInfo.targetInfo.desktopRect.top == 0 ? "Primary" : "";
```

## UI Features

- **Displays List**: All detected monitors with basic info
- **Selected Display Details**: EDID, adapter, output, resolution, refresh, position
- **Topology View**: Current Windows display topology (Extend/Clone/Internal/External)
- **Correlation View**: HardwareDevice ↔ Windows display mapping
- **Session Info**: Which session owns which displays

## Verification Checklist

- [x] Multiple API enumeration (GDI, DisplayConfig, SetupAPI, DXGI, WMI)
- [x] EDID parsing (manufacturer, model, serial, date)
- [x] Monitor persistent identity (EDID serial > DeviceInstanceId > Adapter+Output)
- [x] HardwareDevice correlation
- [x] Topology reading (position, resolution, refresh, primary)
- [x] No display configuration changes
- [x] No session switching
- [x] No user logoff
- [x] Read-only monitoring
- [x] User-initiated only
- [x] FreeSeat remains responsive
- [x] Existing functionality unaffected

## Verification

Run FreeSeat and verify:
1. Display Diagnostics page opens
2. All connected monitors displayed
3. Manufacturer/Model/Serial displayed (from EDID)
4. Adapter/Output information displayed
5. Resolution/Refresh rate/Position displayed
6. Primary monitor identified
7. Topology shown (Extend/Clone/Internal/External)
8. Correlation with HardwareDevice shown
9. No display configuration changed
10. FreeSeat remains responsive
11. Existing Seat/Session functionality works
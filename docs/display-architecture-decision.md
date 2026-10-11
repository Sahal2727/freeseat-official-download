# Display Architecture Decision

## Decision: **PARTIALLY**

Based on comprehensive research and proof-of-concept testing, FreeSeat can **detect, identify, and correlate** physical monitors but **cannot assign** them to specific sessions for independent multi-seat display ownership on consumer Windows 11.

---

## Evidence Summary

### What Works (✅)
- **Monitor Detection**: Multiple APIs (GDI, DisplayConfig, SetupAPI, DXGI, WMI) can enumerate all physical monitors
- **Monitor Identification**: EDID serial number is best persistent identifier; DeviceInstanceId works if port stable
- **EDID Parsing**: Can extract manufacturer, model, serial, manufacturing date, supported modes
- **GPU Output Detection**: DXGI EnumAdapters/EnumOutputs provides adapter/output relationship
- **Monitor ↔ HardwareDevice Correlation**: Can correlate via DeviceInstanceId, EDID, Adapter+Output
- **Display Topology Reading**: QueryDisplayConfig reads current Windows display topology

### What Does Not Work (❌)
- **Monitor → Session Assignment**: No Windows API to assign physical monitor to specific session
- **Independent Display Ownership**: Only one console session owns physical displays
- **True Multi-Seat Display Isolation**: Cannot isolate displays per seat
- **Per-Session Display Config**: Display config changes are system-wide
- **GPU Sharing for 3D**: Cannot share GPU across sessions for independent 3D
- **Two Simultaneous Console Sessions**: Consumer Windows 11 only supports one interactive console session

---

## Architecture Options Evaluated

| Option | Verdict | Reason |
|--------|---------|--------|
| Standard Extended Desktop (Single Session) | ⚠️ Partial | Works but no true isolation (shared session) |
| Multiple Windows Sessions | ❌ Impossible | Consumer Windows: only one console session |
| Separate Desktops | ❌ Impossible | Single input desktop limitation |
| GPU Partitioning (vGPU/SR-IOV/GPU-P) | ⚠️ Future | Requires Windows Server + supported enterprise GPU |
| Virtual Machines | ⚠️ Future | Heavy resource overhead, not FreeSeat architecture |
| Display Driver / Kernel Filter | ❌ Rejected | Kernel driver required, WDDM complexity, no Microsoft support |
| Windows MultiPoint / RDS | ❌ Rejected | Windows Server only, RDS CALs, deprecated |

---

## Decision: **PARTIALLY**

**FreeSeat can detect, identify, and correlate physical monitors but cannot assign them to specific sessions for independent multi-seat display ownership.**

True multi-seat display isolation requires:
1. **Windows Server with MultiPoint Services** - requires RDS CALs, deprecated
2. **GPU Partitioning (vGPU/SR-IOV/GPU-P)** - requires Windows Server + enterprise GPU
3. **Virtual Machines with GPU passthrough** - heavy overhead, consumer GPU limitations

---

## Path Forward

### Milestone 6 (Complete)
- Research complete ✅
- Display detection POC implemented safely ✅
- Monitor identification tested ✅
- Architecture options evaluated ✅
- Decision documented: **PARTIALLY**

### Milestone 7+ (Future)
- Requires Windows Server / GPU virtualization / VMs
- Consumer Windows 11 fundamentally cannot provide true multi-seat display isolation

---

## Proof of Concept: Display Diagnostics Page

### Safety Constraints (Enforced)
- NO display configuration changes
- NO session switching
- NO user logoff
- NO GPU driver modification
- Read-only monitoring
- User-initiated only

### Implementation
1. Monitor Enumeration using multiple APIs
2. EDID Parsing (manufacturer, model, serial, date)
3. Correlation with FreeSeat HardwareDevice
4. Topology Display (current Windows display configuration)
5. Session Ownership Information

---

## Final Decision

**PARTIALLY**

FreeSeat can **detect, identify, and correlate** physical monitors but **cannot assign** them to specific sessions for independent multi-seat display ownership on consumer Windows 11. True multi-seat display isolation requires Windows Server with MultiPoint Services, GPU partitioning (vGPU/SR-IOV), or virtual machines.

---

*Architecture Decision Record - FreeSeat Milestone 6*
*Date: 2026*
*Status: ACCEPTED*
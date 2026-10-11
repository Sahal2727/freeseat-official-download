using System.Collections.Immutable;
using System.Management;
using System.Runtime.InteropServices;
using FreeSeat.Core.Hardware.Models;
using Microsoft.Extensions.Logging;

namespace FreeSeat.Infrastructure.Hardware;

internal sealed class MonitorDetector
{
    private const uint DisplayDeviceAttachedToDesktop = 0x00000001;
    private const uint DisplayDevicePrimaryDevice = 0x00000004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public int Size;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplayDevices(string? device, uint deviceIndex, ref DisplayDevice displayDevice, uint flags);

    private readonly ILogger<MonitorDetector> _logger;

    public MonitorDetector(ILogger<MonitorDetector> logger)
    {
        _logger = logger;
    }

    public ImmutableArray<MonitorDevice> Detect(CancellationToken cancellationToken = default)
    {
        var primaryStates = DetectPrimaryStates();
        var pnpMonitors = DetectViaPnP(cancellationToken, primaryStates);
        if (pnpMonitors.Count > 0)
            return pnpMonitors.ToImmutableArray();

        var monitors = new List<MonitorDevice>();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DesktopMonitor");
            var wmiMonitors = searcher.Get().Cast<ManagementObject>().ToList();

            using var videoSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            var videoControllers = videoSearcher.Get().Cast<ManagementObject>().ToList();

            var displaySettings = GetDisplaySettings();

            foreach (var monitor in wmiMonitors)
            {
                try
                {
                    var device = ParseMonitor(monitor, displaySettings, videoControllers, primaryStates);
                    if (device != null)
                        monitors.Add(device);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse monitor");
                }
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect monitors via WMI");
        }

        return monitors.ToImmutableArray();
    }

    private Dictionary<string, DisplaySetting> GetDisplaySettings()
    {
        var settings = new Dictionary<string, DisplaySetting>();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            foreach (var controller in searcher.Get().Cast<ManagementObject>())
            {
                var name = controller["Name"]?.ToString() ?? "";
                var width = ParseInt(controller["CurrentHorizontalResolution"]);
                var height = ParseInt(controller["CurrentVerticalResolution"]);
                var refresh = ParseInt(controller["CurrentRefreshRate"]);
                var bitDepth = ParseInt(controller["CurrentBitsPerPixel"]);

                if (!string.IsNullOrEmpty(name))
                {
                    settings[name] = new DisplaySetting
                    {
                        Width = width,
                        Height = height,
                        RefreshRate = refresh,
                        BitDepth = bitDepth
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get display settings");
        }

        return settings;
    }

    private MonitorDevice? ParseMonitor(ManagementObject monitor, Dictionary<string, DisplaySetting> displaySettings, List<ManagementObject> videoControllers, IReadOnlyDictionary<string, bool> primaryStates)
    {
        var name = monitor["Name"]?.ToString() ?? "Unknown Monitor";
        var deviceId = monitor["DeviceID"]?.ToString() ?? Guid.NewGuid().ToString();
        var pnpDeviceId = monitor["PNPDeviceID"]?.ToString();
        var primary = GetPrimaryState(pnpDeviceId ?? deviceId, primaryStates);

        var displaySetting = displaySettings.Values.FirstOrDefault();

        var monitorDevice = new MonitorDevice
        {
            Id = deviceId,
            FriendlyName = name,
            DeviceInstanceId = pnpDeviceId,
            ConnectionStatus = ConnectionStatus.Connected,
            Width = displaySetting?.Width,
            Height = displaySetting?.Height,
            RefreshRate = displaySetting?.RefreshRate,
            BitDepth = displaySetting?.BitDepth,
            IsPrimary = primary ?? false,
            IsPrimaryKnown = primary.HasValue,
            Manufacturer = WmiPropertyReader.GetOptionalValue(monitor, "Manufacturer")?.ToString(),
            Model = monitor["MonitorType"]?.ToString() ?? name,
            LastSeen = DateTime.UtcNow
        };

        TryGetEdidInfo(monitorDevice);

        return monitorDevice;
    }

    private void TryGetEdidInfo(MonitorDevice monitor)
    {
        try
        {
            var deviceId = monitor.DeviceInstanceId;
            if (!string.IsNullOrEmpty(deviceId))
            {
            }
        }
        catch
        {
        }
    }

    private List<MonitorDevice> DetectViaPnP(CancellationToken cancellationToken, IReadOnlyDictionary<string, bool> primaryStates)
    {
        var monitors = new List<MonitorDevice>();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE ClassGuid = '{4d36e96e-e325-11ce-bfc1-08002be10318}'");

            foreach (var entity in searcher.Get().Cast<ManagementObject>())
            {
                try
                {
                    var name = entity["Name"]?.ToString() ?? "Unknown Monitor";
                    var deviceId = entity["DeviceID"]?.ToString() ?? Guid.NewGuid().ToString();
                    var pnpDeviceId = entity["PNPDeviceID"]?.ToString();
                    var manufacturer = entity["Manufacturer"]?.ToString();
                    var status = entity["Status"]?.ToString();
                    var primary = GetPrimaryState(pnpDeviceId ?? deviceId, primaryStates);

                    if (status?.Equals("OK", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        monitors.Add(new MonitorDevice
                        {
                            Id = deviceId,
                            FriendlyName = name,
                            DeviceInstanceId = pnpDeviceId,
                            ConnectionStatus = ConnectionStatus.Connected,
                            Manufacturer = manufacturer,
                            Model = name,
                            IsPrimary = primary ?? false,
                            IsPrimaryKnown = primary.HasValue,
                            LastSeen = DateTime.UtcNow
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse PnP monitor");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect monitors via PnP");
        }

        return monitors;
    }

    private Dictionary<string, bool> DetectPrimaryStates()
    {
        var primaryStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var adapter = new DisplayDevice { Size = Marshal.SizeOf<DisplayDevice>() };
            for (uint adapterIndex = 0; EnumDisplayDevices(null, adapterIndex, ref adapter, 0); adapterIndex++)
            {
                var adapterIsPrimary = (adapter.StateFlags & DisplayDevicePrimaryDevice) != 0;
                var childDevices = new List<(string Token, bool IsAttached)>();

                for (uint childIndex = 0; ; childIndex++)
                {
                    var child = new DisplayDevice { Size = Marshal.SizeOf<DisplayDevice>() };
                    if (!EnumDisplayDevices(adapter.DeviceName, childIndex, ref child, 0))
                        break;

                    var token = GetMonitorToken(child.DeviceId);
                    if (token is not null)
                        childDevices.Add((token, (child.StateFlags & DisplayDeviceAttachedToDesktop) != 0));
                }

                var attachedChildCount = childDevices.Count(device => device.IsAttached);
                foreach (var child in childDevices)
                {
                    // A primary adapter with multiple active monitor children does not identify
                    // which physical monitor owns the primary desktop origin.
                    if (adapterIsPrimary && child.IsAttached && attachedChildCount != 1)
                        continue;

                    primaryStates[child.Token] = adapterIsPrimary && child.IsAttached;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not map physical monitors to Windows primary-display flags");
        }

        return primaryStates;
    }

    private static bool? GetPrimaryState(string? deviceInstanceId, IReadOnlyDictionary<string, bool> primaryStates)
    {
        var token = GetMonitorToken(deviceInstanceId);
        return token is not null && primaryStates.TryGetValue(token, out var isPrimary) ? isPrimary : null;
    }

    private static string? GetMonitorToken(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return null;

        var segments = deviceId.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 ? segments[1] : null;
    }

    private int? ParseInt(object? value)
    {
        if (value == null) return null;
        if (int.TryParse(value.ToString(), out var result))
            return result;
        return null;
    }

    private sealed class DisplaySetting
    {
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? RefreshRate { get; set; }
        public int? BitDepth { get; set; }
    }
}

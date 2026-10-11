using System.Collections.Immutable;
using System.Management;
using FreeSeat.Core.Hardware.Models;
using Microsoft.Extensions.Logging;

namespace FreeSeat.Infrastructure.Hardware;

/// <summary>
/// HID device detector for keyboards and mice.
/// </summary>
internal sealed class HidDeviceDetector
{
    private readonly ILogger<HidDeviceDetector> _logger;

    public HidDeviceDetector(ILogger<HidDeviceDetector> logger)
    {
        _logger = logger;
    }

    public (ImmutableArray<KeyboardDevice> Keyboards, ImmutableArray<MouseDevice> Mice) Detect(CancellationToken cancellationToken = default)
    {
        var keyboards = new List<KeyboardDevice>();
        var mice = new List<MouseDevice>();

        try
        {
            // Detect HID keyboards
            keyboards.AddRange(DetectKeyboards(cancellationToken));

            // Detect HID mice
            mice.AddRange(DetectMice(cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect HID devices");
        }

        return (keyboards.ToImmutableArray(), mice.ToImmutableArray());
    }

    private List<KeyboardDevice> DetectKeyboards(CancellationToken cancellationToken)
    {
        var keyboards = new List<KeyboardDevice>();

        try
        {
            // Use Win32_PnPEntity for HID keyboard devices
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE ClassGuid = '{4d36e96b-e325-11ce-bfc1-08002be10318}'");

            foreach (var entity in searcher.Get().Cast<ManagementObject>())
            {
                try
                {
                    var name = entity["Name"]?.ToString() ?? "Unknown Keyboard";
                    var deviceId = entity["DeviceID"]?.ToString() ?? Guid.NewGuid().ToString();
                    var pnpDeviceId = entity["PNPDeviceID"]?.ToString();
                    var manufacturer = entity["Manufacturer"]?.ToString();
                    var status = entity["Status"]?.ToString();

                    if (status?.Equals("OK", StringComparison.OrdinalIgnoreCase) == true &&
                        IsKeyboard(name, deviceId))
                    {
                        keyboards.Add(new KeyboardDevice
                        {
                            Id = deviceId,
                            FriendlyName = name,
                            DeviceInstanceId = pnpDeviceId,
                            ConnectionStatus = ConnectionStatus.Connected,
                            Manufacturer = manufacturer,
                            Model = name,
                            LastSeen = DateTime.UtcNow
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse keyboard");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect keyboards");
        }

        return keyboards;
    }

    private List<MouseDevice> DetectMice(CancellationToken cancellationToken)
    {
        var mice = new List<MouseDevice>();

        try
        {
            // Use Win32_PnPEntity for HID mouse devices
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE ClassGuid = '{4d36e96f-e325-11ce-bfc1-08002be10318}'");

            foreach (var entity in searcher.Get().Cast<ManagementObject>())
            {
                try
                {
                    var name = entity["Name"]?.ToString() ?? "Unknown Mouse";
                    var deviceId = entity["DeviceID"]?.ToString() ?? Guid.NewGuid().ToString();
                    var pnpDeviceId = entity["PNPDeviceID"]?.ToString();
                    var manufacturer = entity["Manufacturer"]?.ToString();
                    var status = entity["Status"]?.ToString();

                    if (status?.Equals("OK", StringComparison.OrdinalIgnoreCase) == true &&
                        IsMouse(name, deviceId))
                    {
                        mice.Add(new MouseDevice
                        {
                            Id = deviceId,
                            FriendlyName = name,
                            DeviceInstanceId = pnpDeviceId,
                            ConnectionStatus = ConnectionStatus.Connected,
                            Manufacturer = manufacturer,
                            Model = name,
                            LastSeen = DateTime.UtcNow
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse mouse");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect mice");
        }

        return mice;
    }

    private bool IsKeyboard(string name, string deviceId)
    {
        var lowerName = name.ToLowerInvariant();
        var lowerId = deviceId.ToLowerInvariant();
        
        // Common keyboard identifiers
        var keyboardKeywords = new[] { "keyboard", "kbd", "hid keyboard", "usb keyboard", "bluetooth keyboard" };
        return keyboardKeywords.Any(k => lowerName.Contains(k) || lowerId.Contains(k));
    }

    private bool IsMouse(string name, string deviceId)
    {
        var lowerName = name.ToLowerInvariant();
        var lowerId = deviceId.ToLowerInvariant();
        
        // Common mouse identifiers
        var mouseKeywords = new[] { "mouse", "pointing", "hid mouse", "usb mouse", "bluetooth mouse", "trackpad", "touchpad" };
        return mouseKeywords.Any(k => lowerName.Contains(k) || lowerId.Contains(k));
    }
}
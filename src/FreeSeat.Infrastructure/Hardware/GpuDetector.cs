using System.Collections.Immutable;
using System.Management;
using FreeSeat.Core.Hardware.Models;
using Microsoft.Extensions.Logging;

namespace FreeSeat.Infrastructure.Hardware;

/// <summary>
/// GPU/display adapter detector.
/// </summary>
internal sealed class GpuDetector
{
    private readonly ILogger<GpuDetector> _logger;

    public GpuDetector(ILogger<GpuDetector> logger)
    {
        _logger = logger;
    }

    public ImmutableArray<GpuDevice> Detect(CancellationToken cancellationToken = default)
    {
        var gpus = new List<GpuDevice>();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            
            foreach (var controller in searcher.Get().Cast<ManagementObject>())
            {
                try
                {
                    var name = controller["Name"]?.ToString() ?? "Unknown GPU";
                    var deviceId = controller["DeviceID"]?.ToString() ?? Guid.NewGuid().ToString();
                    var pnpDeviceId = controller["PNPDeviceID"]?.ToString();
                    var adapterRam = controller["AdapterRAM"] switch
                    {
                        uint value => value,
                        ulong value => value,
                        long value when value > 0 => (ulong)value,
                        _ => 0UL
                    };
                    var driverVersion = controller["DriverVersion"]?.ToString();
                    var driverDateStr = controller["DriverDate"]?.ToString();
                    var driverProvider = WmiPropertyReader.GetOptionalValue(controller, "DriverProviderName")?.ToString();
                    var architecture = controller["VideoArchitecture"]?.ToString();

                    DateTime? driverDate = null;
                    if (!string.IsNullOrEmpty(driverDateStr))
                    {
                        if (DateTime.TryParseExact(driverDateStr, "yyyyMMddHHmmss.ffffff+***", null, System.Globalization.DateTimeStyles.None, out var dt))
                            driverDate = dt;
                        else if (DateTime.TryParse(driverDateStr, out dt))
                            driverDate = dt;
                    }

                    var outputs = new List<string>();
                    var videoMode = controller["VideoModeDescription"]?.ToString();
                    if (!string.IsNullOrEmpty(videoMode))
                        outputs.Add(videoMode);

                    var gpu = new GpuDevice
                    {
                        Id = deviceId,
                        FriendlyName = name,
                        DeviceInstanceId = pnpDeviceId,
                        ConnectionStatus = ConnectionStatus.Connected,
                        Manufacturer = controller["AdapterCompatibility"]?.ToString(),
                        Model = name,
                        VideoRamBytes = adapterRam > 0 ? (long)adapterRam : null,
                        DriverVersion = driverVersion,
                        DriverDate = driverDate,
                        DriverProvider = driverProvider,
                        Architecture = architecture,
                        Outputs = outputs.ToImmutableArray(),
                        LastSeen = DateTime.UtcNow
                    };

                    gpus.Add(gpu);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse GPU");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect GPUs");
        }

        return gpus.ToImmutableArray();
    }
}

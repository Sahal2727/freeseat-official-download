using System.Management;
using FreeSeat.Core.Hardware.Models;
using Microsoft.Extensions.Logging;

namespace FreeSeat.Infrastructure.Hardware;

/// <summary>
/// CPU detector.
/// </summary>
internal sealed class CpuDetector
{
    private readonly ILogger<CpuDetector> _logger;

    public CpuDetector(ILogger<CpuDetector> logger)
    {
        _logger = logger;
    }

    public CpuInfo? Detect(CancellationToken cancellationToken = default)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            
            foreach (var processor in searcher.Get().Cast<ManagementObject>())
            {
                try
                {
                    var name = processor["Name"]?.ToString() ?? "Unknown CPU";
                    var manufacturer = processor["Manufacturer"]?.ToString();
                    var deviceId = processor["DeviceID"]?.ToString() ?? Guid.NewGuid().ToString();
                    var physicalCores = ParseInt(processor["NumberOfCores"]) ?? Environment.ProcessorCount;
                    var logicalProcessors = ParseInt(processor["NumberOfLogicalProcessors"]) ?? Environment.ProcessorCount;
                    var baseClock = ParseInt(processor["CurrentClockSpeed"]) ?? 0;
                    var maxClock = ParseInt(processor["MaxClockSpeed"]) ?? 0;
                    var l1Cache = ParseInt(WmiPropertyReader.GetOptionalValue(processor, "L1CacheSize")) ?? 0;
                    var l2Cache = ParseInt(processor["L2CacheSize"]) ?? 0;
                    var l3Cache = ParseInt(processor["L3CacheSize"]) ?? 0;
                    var architecture = processor["Architecture"]?.ToString();

                    return new CpuInfo
                    {
                        Id = deviceId,
                        Name = name,
                        Manufacturer = manufacturer,
                        DeviceInstanceId = processor["PNPDeviceID"]?.ToString(),
                        ConnectionStatus = ConnectionStatus.Connected,
                        PhysicalCores = physicalCores,
                        LogicalProcessors = logicalProcessors,
                        BaseClockMhz = baseClock > 0 ? baseClock : null,
                        MaxClockMhz = maxClock > 0 ? maxClock : null,
                        L1CacheKb = l1Cache > 0 ? l1Cache : null,
                        L2CacheKb = l2Cache > 0 ? l2Cache : null,
                        L3CacheKb = l3Cache > 0 ? l3Cache : null,
                        Architecture = architecture,
                        LastSeen = DateTime.UtcNow
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse CPU");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect CPU");
        }

        return null;
    }

    private int? ParseInt(object? value)
    {
        if (value == null) return null;
        if (int.TryParse(value.ToString(), out var result))
            return result;
        return null;
    }
}

using System.Management;
using FreeSeat.Core.Hardware.Models;
using Microsoft.Extensions.Logging;

namespace FreeSeat.Infrastructure.Hardware;

/// <summary>
/// Memory/RAM detector.
/// </summary>
internal sealed class MemoryDetector
{
    private readonly ILogger<MemoryDetector> _logger;

    public MemoryDetector(ILogger<MemoryDetector> logger)
    {
        _logger = logger;
    }

    public MemoryInfo? Detect(CancellationToken cancellationToken = default)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            
            foreach (var system in searcher.Get().Cast<ManagementObject>())
            {
                try
                {
                    var totalBytes = ParseLong(system["TotalPhysicalMemory"]) ?? 0;
                    
                    // Get available memory from performance counter or GC
                    var availableBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                    if (availableBytes > totalBytes)
                        availableBytes = totalBytes;

                    var slotCount = ParseInt(WmiPropertyReader.GetOptionalValue(system, "NumberOfMemorySlots")) ?? GetMemorySlotCount();
                    
                    // Get memory speed and type from Win32_PhysicalMemory
                    var (speed, type) = GetMemoryDetails();

                    return new MemoryInfo
                    {
                        Id = "memory",
                        FriendlyName = "System Memory",
                        DeviceInstanceId = "memory",
                        ConnectionStatus = ConnectionStatus.Connected,
                        TotalBytes = totalBytes,
                        AvailableBytes = availableBytes,
                        SlotCount = slotCount > 0 ? slotCount : null,
                        SpeedMhz = speed,
                        Type = type,
                        LastSeen = DateTime.UtcNow
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse memory info");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect memory");
        }

        return null;
    }

    private (int? speed, string? type) GetMemoryDetails()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            foreach (var memory in searcher.Get().Cast<ManagementObject>())
            {
                var speed = ParseInt(memory["Speed"]);
                var type = memory["MemoryType"]?.ToString();
                if (speed.HasValue || !string.IsNullOrEmpty(type))
                    return (speed, type);
            }
        }
        catch
        {
        }
        return (null, null);
    }

    private int? GetMemorySlotCount()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT MemoryDevices FROM Win32_PhysicalMemoryArray");
            var counts = searcher.Get()
                .Cast<ManagementObject>()
                .Select(array => ParseInt(WmiPropertyReader.GetOptionalValue(array, "MemoryDevices")) ?? 0)
                .Where(count => count > 0)
                .ToArray();

            return counts.Length > 0 ? counts.Sum() : null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to detect memory slot count");
            return null;
        }
    }

    private long? ParseLong(object? value)
    {
        if (value == null) return null;
        if (long.TryParse(value.ToString(), out var result))
            return result;
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

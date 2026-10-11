using System.Collections.Immutable;

namespace FreeSeat.Core.Hardware.Models;

/// <summary>
/// CPU information.
/// </summary>
public sealed record CpuInfo : HardwareDevice
{
    public override DeviceType DeviceType => DeviceType.Cpu;

    /// <summary>
    /// CPU name (e.g., "AMD Ryzen 5 9600X").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Manufacturer (e.g., "AMD", "Intel").
    /// </summary>
    public new string? Manufacturer { get; init; }

    /// <summary>
    /// Number of physical cores.
    /// </summary>
    public int PhysicalCores { get; init; }

    /// <summary>
    /// Number of logical processors.
    /// </summary>
    public int LogicalProcessors { get; init; }

    /// <summary>
    /// Base clock speed in MHz.
    /// </summary>
    public int? BaseClockMhz { get; init; }

    /// <summary>
    /// Max turbo/boost clock in MHz.
    /// </summary>
    public int? MaxClockMhz { get; init; }

    /// <summary>
    /// L1 cache size in KB.
    /// </summary>
    public int? L1CacheKb { get; init; }

    /// <summary>
    /// L2 cache size in KB.
    /// </summary>
    public int? L2CacheKb { get; init; }

    /// <summary>
    /// L3 cache size in KB.
    /// </summary>
    public int? L3CacheKb { get; init; }

    /// <summary>
    /// CPU architecture (e.g., "x64", "ARM64").
    /// </summary>
    public string? Architecture { get; init; }

    /// <summary>
    /// Creates a copy with updated connection status.
    /// </summary>
    public override HardwareDevice WithConnectionStatus(ConnectionStatus status)
    {
        return this with { ConnectionStatus = status, LastSeen = DateTime.UtcNow };
    }

    /// <summary>
    /// Returns a display string for cores/threads.
    /// </summary>
    public string GetCoresString()
    {
        if (PhysicalCores == LogicalProcessors)
            return $"{PhysicalCores} cores";
        return $"{PhysicalCores} cores / {LogicalProcessors} threads";
    }
}
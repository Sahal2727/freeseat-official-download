using System.Collections.Generic;
using System.Collections.Immutable;

namespace FreeSeat.Core.Hardware.Models;

/// <summary>
/// A complete snapshot of all hardware at a point in time.
/// </summary>
public sealed record HardwareSnapshot
{
    /// <summary>
    /// Timestamp when this snapshot was taken.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// All detected monitors.
    /// </summary>
    public ImmutableArray<MonitorDevice> Monitors { get; init; } = ImmutableArray<MonitorDevice>.Empty;

    /// <summary>
    /// All detected keyboards.
    /// </summary>
    public ImmutableArray<KeyboardDevice> Keyboards { get; init; } = ImmutableArray<KeyboardDevice>.Empty;

    /// <summary>
    /// All detected mice.
    /// </summary>
    public ImmutableArray<MouseDevice> Mice { get; init; } = ImmutableArray<MouseDevice>.Empty;

    /// <summary>
    /// All detected GPUs.
    /// </summary>
    public ImmutableArray<GpuDevice> Gpus { get; init; } = ImmutableArray<GpuDevice>.Empty;

    /// <summary>
    /// CPU information.
    /// </summary>
    public CpuInfo? Cpu { get; init; }

    /// <summary>
    /// Memory information.
    /// </summary>
    public MemoryInfo? Memory { get; init; }

    /// <summary>
    /// All detected storage devices.
    /// </summary>
    public ImmutableArray<StorageInfo> Storage { get; init; } = ImmutableArray<StorageInfo>.Empty;

    /// <summary>
    /// Creates an empty snapshot.
    /// </summary>
    public static HardwareSnapshot Empty => new();

    /// <summary>
    /// Total count of all devices.
    /// </summary>
    public int TotalDeviceCount => Monitors.Length + Keyboards.Length + Mice.Length + Gpus.Length + (Cpu != null ? 1 : 0) + (Memory != null ? 1 : 0) + Storage.Length;

    /// <summary>
    /// Gets all devices as a flat list.
    /// </summary>
    public IEnumerable<HardwareDevice> AllDevices
    {
        get
        {
            foreach (var m in Monitors) yield return m;
            foreach (var k in Keyboards) yield return k;
            foreach (var m in Mice) yield return m;
            foreach (var g in Gpus) yield return g;
            if (Cpu != null) yield return Cpu;
            if (Memory != null) yield return Memory;
            foreach (var s in Storage) yield return s;
        }
    }

    /// <summary>
    /// Gets devices of a specific type.
    /// </summary>
    public IEnumerable<HardwareDevice> GetDevicesOfType(DeviceType type)
    {
        return AllDevices.Where(d => d.DeviceType == type);
    }
}
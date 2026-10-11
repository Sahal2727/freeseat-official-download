namespace FreeSeat.Core.Hardware.Models;

/// <summary>
/// Memory/RAM information.
/// </summary>
public sealed record MemoryInfo : HardwareDevice
{
    public override DeviceType DeviceType => DeviceType.Memory;

    /// <summary>
    /// Total physical memory in bytes.
    /// </summary>
    public required long TotalBytes { get; init; }

    /// <summary>
    /// Available memory in bytes.
    /// </summary>
    public long AvailableBytes { get; init; }

    /// <summary>
    /// Memory usage percentage (0-100).
    /// </summary>
    public double UsagePercent => TotalBytes > 0 ? (1.0 - (double)AvailableBytes / TotalBytes) * 100 : 0;

    /// <summary>
    /// Number of memory slots.
    /// </summary>
    public int? SlotCount { get; init; }

    /// <summary>
    /// Memory speed in MHz.
    /// </summary>
    public int? SpeedMhz { get; init; }

    /// <summary>
    /// Memory type (e.g., "DDR4", "DDR5").
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Creates a copy with updated connection status.
    /// </summary>
    public override HardwareDevice WithConnectionStatus(ConnectionStatus status)
    {
        return this with { ConnectionStatus = status, LastSeen = DateTime.UtcNow };
    }

    /// <summary>
    /// Returns total memory as a formatted string.
    /// </summary>
    public string GetTotalString() => FormatBytes(TotalBytes);

    /// <summary>
    /// Returns available memory as a formatted string.
    /// </summary>
    public string GetAvailableString() => FormatBytes(AvailableBytes);

    /// <summary>
    /// Returns used memory as a formatted string.
    /// </summary>
    public string GetUsedString() => FormatBytes(TotalBytes - AvailableBytes);

    private static string FormatBytes(long bytes)
    {
        const long gb = 1024L * 1024 * 1024;
        const long mb = 1024L * 1024;
        
        if (bytes >= gb)
            return $"{bytes / (double)gb:F1} GB";
        if (bytes >= mb)
            return $"{bytes / (double)mb:F0} MB";
        return $"{bytes} bytes";
    }
}
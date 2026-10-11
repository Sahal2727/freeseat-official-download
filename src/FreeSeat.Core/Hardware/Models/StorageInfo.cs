using System.Collections.Immutable;

namespace FreeSeat.Core.Hardware.Models;

/// <summary>
/// Storage device information.
/// </summary>
public sealed record StorageInfo : HardwareDevice
{
    public override DeviceType DeviceType => DeviceType.Storage;

    /// <summary>
    /// Drive letter (e.g., "C:").
    /// </summary>
    public string? DriveLetter { get; init; }

    /// <summary>
    /// Volume label.
    /// </summary>
    public string? VolumeLabel { get; init; }

    /// <summary>
    /// File system (e.g., "NTFS", "ReFS", "FAT32").
    /// </summary>
    public string? FileSystem { get; init; }

    /// <summary>
    /// Total size in bytes.
    /// </summary>
    public long TotalBytes { get; init; }

    /// <summary>
    /// Free space in bytes.
    /// </summary>
    public long FreeBytes { get; init; }

    /// <summary>
    /// Used space percentage (0-100).
    /// </summary>
    public double UsedPercent => TotalBytes > 0 ? (1.0 - (double)FreeBytes / TotalBytes) * 100 : 0;

    /// <summary>
    /// Drive type (Fixed, Removable, Network, etc.).
    /// </summary>
    public string? DriveType { get; init; }

    /// <summary>
    /// Whether this is the system drive.
    /// </summary>
    public bool IsSystemDrive { get; init; }

    /// <summary>
    /// Creates a copy with updated connection status.
    /// </summary>
    public override HardwareDevice WithConnectionStatus(ConnectionStatus status)
    {
        return this with { ConnectionStatus = status, LastSeen = DateTime.UtcNow };
    }

    /// <summary>
    /// Returns total size as a formatted string.
    /// </summary>
    public string GetTotalString() => FormatBytes(TotalBytes);

    /// <summary>
    /// Returns free space as a formatted string.
    /// </summary>
    public string GetFreeString() => FormatBytes(FreeBytes);

    /// <summary>
    /// Returns used space as a formatted string.
    /// </summary>
    public string GetUsedString() => FormatBytes(TotalBytes - FreeBytes);

    private static string FormatBytes(long bytes)
    {
        const long gb = 1024L * 1024 * 1024;
        const long tb = 1024L * 1024 * 1024 * 1024;
        
        if (bytes >= tb)
            return $"{bytes / (double)tb:F2} TB";
        if (bytes >= gb)
            return $"{bytes / (double)gb:F1} GB";
        return $"{bytes} bytes";
    }
}
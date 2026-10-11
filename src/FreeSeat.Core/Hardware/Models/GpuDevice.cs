using System.Collections.Immutable;

namespace FreeSeat.Core.Hardware.Models;

/// <summary>
/// Represents a GPU/display adapter.
/// </summary>
public sealed record GpuDevice : HardwareDevice
{
    public override DeviceType DeviceType => DeviceType.Gpu;

    /// <summary>
    /// Video RAM in bytes.
    /// </summary>
    public long? VideoRamBytes { get; init; }

    /// <summary>
    /// Driver version.
    /// </summary>
    public string? DriverVersion { get; init; }

    /// <summary>
    /// Driver date.
    /// </summary>
    public DateTime? DriverDate { get; init; }

    /// <summary>
    /// Driver provider.
    /// </summary>
    public string? DriverProvider { get; init; }

    /// <summary>
    /// Supported display outputs (HDMI, DisplayPort, etc.).
    /// </summary>
    public ImmutableArray<string> Outputs { get; init; } = ImmutableArray<string>.Empty;

    /// <summary>
    /// GPU architecture/family.
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
    /// Returns VRAM as a formatted string.
    /// </summary>
    public string GetVramString()
    {
        if (!VideoRamBytes.HasValue)
            return "Unknown";

        var gb = VideoRamBytes.Value / (1024.0 * 1024.0 * 1024.0);
        return $"{gb:F1} GB";
    }
}
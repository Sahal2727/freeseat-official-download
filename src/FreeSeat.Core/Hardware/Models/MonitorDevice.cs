using System.Collections.Immutable;

namespace FreeSeat.Core.Hardware.Models;

/// <summary>
/// Represents a physical monitor/display device.
/// </summary>
public sealed record MonitorDevice : HardwareDevice
{
    public override DeviceType DeviceType => DeviceType.Monitor;

    /// <summary>
    /// EDID manufacturer ID (3-character code).
    /// </summary>
    public string? EdidManufacturerId { get; init; }

    /// <summary>
    /// EDID product code.
    /// </summary>
    public string? EdidProductCode { get; init; }

    /// <summary>
    /// Serial number from EDID.
    /// </summary>
    public string? SerialNumber { get; init; }

    /// <summary>
    /// Current horizontal resolution in pixels.
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Current vertical resolution in pixels.
    /// </summary>
    public int? Height { get; init; }

    /// <summary>
    /// Current refresh rate in Hz.
    /// </summary>
    public int? RefreshRate { get; init; }

    /// <summary>
    /// Whether this is the primary display.
    /// </summary>
    public bool IsPrimary { get; init; }

    /// <summary>
    /// Whether Windows could associate this monitor with an active display and determine its primary status.
    /// </summary>
    public bool IsPrimaryKnown { get; init; }

    public string PrimaryDisplayStatus => !IsPrimaryKnown
        ? "Primary: Unknown"
        : IsPrimary ? "Primary: Yes" : "Primary: No";

    /// <summary>
    /// X position in virtual desktop coordinates.
    /// </summary>
    public int? PositionX { get; init; }

    /// <summary>
    /// Y position in virtual desktop coordinates.
    /// </summary>
    public int? PositionY { get; init; }

    /// <summary>
    /// DPI/scaling factor (e.g., 100%, 125%, 150%).
    /// </summary>
    public int? Dpi { get; init; }

    /// <summary>
    /// Color depth (bits per pixel).
    /// </summary>
    public int? BitDepth { get; init; }

    /// <summary>
    /// Display orientation.
    /// </summary>
    public string? Orientation { get; init; }

    /// <summary>
    /// Physical width in millimeters.
    /// </summary>
    public int? PhysicalWidthMm { get; init; }

    /// <summary>
    /// Physical height in millimeters.
    /// </summary>
    public int? PhysicalHeightMm { get; init; }

    /// <summary>
    /// Video output technology (e.g., "HDMI", "DisplayPort", "DVI", "VGA", "Internal").
    /// </summary>
    public string? OutputTechnology { get; init; }

    /// <summary>
    /// Creates a copy with updated connection status.
    /// </summary>
    public override HardwareDevice WithConnectionStatus(ConnectionStatus status)
    {
        return this with { ConnectionStatus = status, LastSeen = DateTime.UtcNow };
    }

    /// <summary>
    /// Creates a copy with updated display settings.
    /// </summary>
    public MonitorDevice WithDisplaySettings(int? width, int? height, int? refreshRate, bool? isPrimary, int? positionX, int? positionY, int? dpi)
    {
        return this with
        {
            Width = width ?? Width,
            Height = height ?? Height,
            RefreshRate = refreshRate ?? RefreshRate,
            IsPrimary = isPrimary ?? IsPrimary,
            PositionX = positionX ?? PositionX,
            PositionY = positionY ?? PositionY,
            Dpi = dpi ?? Dpi,
            LastSeen = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Returns a display string for the resolution and refresh rate.
    /// </summary>
    public string GetResolutionString()
    {
        if (Width.HasValue && Height.HasValue)
        {
            var result = $"{Width} × {Height}";
            if (RefreshRate.HasValue)
                result += $" @ {RefreshRate} Hz";
            return result;
        }
        return "Unknown";
    }
}

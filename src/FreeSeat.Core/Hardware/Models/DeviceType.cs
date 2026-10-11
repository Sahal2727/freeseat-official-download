using System.ComponentModel;

namespace FreeSeat.Core.Hardware.Models;

/// <summary>
/// Types of hardware devices that can be detected.
/// </summary>
public enum DeviceType
{
    [Description("Unknown")]
    Unknown = 0,
    [Description("Monitor")]
    Monitor = 1,
    [Description("Keyboard")]
    Keyboard = 2,
    [Description("Mouse")]
    Mouse = 3,
    [Description("GPU")]
    Gpu = 4,
    [Description("CPU")]
    Cpu = 5,
    [Description("Memory")]
    Memory = 6,
    [Description("Storage")]
    Storage = 7,
    [Description("Other")]
    Other = 8,
}

/// <summary>
/// Extension methods for DeviceType.
/// </summary>
public static class DeviceTypeExtensions
{
    public static string GetDescription(this DeviceType type)
    {
        var field = type.GetType().GetField(type.ToString());
        if (field == null) return type.ToString();
        var attr = field.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
        return attr?.Description ?? type.ToString();
    }
}
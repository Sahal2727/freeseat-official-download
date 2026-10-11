using System.Management;

namespace FreeSeat.Infrastructure.Hardware;

internal static class WmiPropertyReader
{
    public static object? GetOptionalValue(ManagementBaseObject instance, string propertyName)
    {
        try
        {
            return instance[propertyName];
        }
        catch (ManagementException)
        {
            return null;
        }
    }
}

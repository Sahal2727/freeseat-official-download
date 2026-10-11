using System.Collections.Immutable;
using System.Management;
using FreeSeat.Core.Hardware.Models;
using Microsoft.Extensions.Logging;

namespace FreeSeat.Infrastructure.Hardware;

/// <summary>
/// Storage detector.
/// </summary>
internal sealed class StorageDetector
{
    private readonly ILogger<StorageDetector> _logger;

    public StorageDetector(ILogger<StorageDetector> logger)
    {
        _logger = logger;
    }

    public ImmutableArray<StorageInfo> Detect(CancellationToken cancellationToken = default)
    {
        var storage = new List<StorageInfo>();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType = 3");
            
            foreach (var disk in searcher.Get().Cast<ManagementObject>())
            {
                try
                {
                    var driveLetter = disk["DeviceID"]?.ToString() ?? "";
                    var volumeLabel = disk["VolumeName"]?.ToString();
                    var fileSystem = disk["FileSystem"]?.ToString();
                    var totalBytes = ParseLong(disk["Size"]) ?? 0;
                    var freeBytes = ParseLong(disk["FreeSpace"]) ?? 0;
                    var driveType = disk["DriveType"]?.ToString();

                    var isSystem = driveLetter.Equals(Environment.GetEnvironmentVariable("SystemDrive"), StringComparison.OrdinalIgnoreCase);

                    storage.Add(new StorageInfo
                    {
                        Id = driveLetter,
                        FriendlyName = $"Drive {driveLetter}",
                        DeviceInstanceId = driveLetter,
                        ConnectionStatus = ConnectionStatus.Connected,
                        DriveLetter = driveLetter,
                        VolumeLabel = volumeLabel,
                        FileSystem = fileSystem,
                        TotalBytes = totalBytes,
                        FreeBytes = freeBytes,
                        DriveType = driveType,
                        IsSystemDrive = isSystem,
                        LastSeen = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse storage device");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect storage");
        }

        return storage.ToImmutableArray();
    }

    private long? ParseLong(object? value)
    {
        if (value == null) return null;
        if (long.TryParse(value.ToString(), out var result))
            return result;
        return null;
    }
}
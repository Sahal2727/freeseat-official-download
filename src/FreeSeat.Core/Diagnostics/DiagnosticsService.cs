using System.Management;
using System.IO;
using FreeSeat.Core.Diagnostics;
using Microsoft.Extensions.Logging;

namespace FreeSeat.Core.Diagnostics;

/// <summary>
/// Implementation of diagnostics collection using WMI.
/// </summary>
public sealed class DiagnosticsService : IDiagnosticsService
{
    private readonly ILogger<DiagnosticsService> _logger;

    public DiagnosticsService(ILogger<DiagnosticsService> logger)
    {
        _logger = logger;
    }

    public Task<SystemDiagnostics> CollectDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Collecting system diagnostics...");
        
        var diagnostics = new SystemDiagnostics
        {
            OperatingSystem = GetOperatingSystem(),
            Architecture = GetArchitecture(),
            Cpu = GetCpuInfo(),
            Ram = GetRamInfo(),
            Gpu = GetGpuInfo(),
            FreeDiskSpace = GetFreeDiskSpace(),
            ApplicationVersion = AppInfo.Version,
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation("System diagnostics collected successfully");
        return Task.FromResult(diagnostics);
    }

    public Task<IReadOnlyList<CompatibilityResult>> CheckCompatibilityAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing system compatibility check...");
        
        var results = new List<CompatibilityResult>();

        results.Add(CheckWindowsVersion());
        results.Add(CheckArchitecture());
        results.Add(CheckCpu());
        results.Add(CheckRam());
        results.Add(CheckDiskSpace());
        results.Add(CheckGpu());

        _logger.LogInformation("System compatibility check completed");
        return Task.FromResult<IReadOnlyList<CompatibilityResult>>(results);
    }

    private CompatibilityResult CheckWindowsVersion()
    {
        var version = Environment.OSVersion.Version;
        var build = version.Build;
        
        if (version.Major >= 10 && build >= 22000)
        {
            return new CompatibilityResult
            {
                CheckName = "Windows Version",
                Status = CompatibilityStatus.Compatible,
                Details = "Windows " + version.Major + "." + version.Minor + " (Build " + build + ") - Windows 11 or later detected"
            };
        }
        else if (version.Major >= 10)
        {
            return new CompatibilityResult
            {
                CheckName = "Windows Version",
                Status = CompatibilityStatus.Warning,
                Details = "Windows " + version.Major + "." + version.Minor + " (Build " + build + ") - Windows 10 detected. Windows 11 recommended for full feature support.",
                Recommendation = "Upgrade to Windows 11 for the best experience."
            };
        }
        else
        {
            return new CompatibilityResult
            {
                CheckName = "Windows Version",
                Status = CompatibilityStatus.Unsupported,
                Details = "Windows " + version.Major + "." + version.Minor + " detected. This application requires Windows 10 or later.",
                Recommendation = "Please upgrade to Windows 10 or Windows 11."
            };
        }
    }

    private CompatibilityResult CheckArchitecture()
    {
        var is64Bit = Environment.Is64BitOperatingSystem;
        
        if (is64Bit)
        {
            return new CompatibilityResult
            {
                CheckName = "Architecture",
                Status = CompatibilityStatus.Compatible,
                Details = "64-bit operating system detected"
            };
        }
        else
        {
            return new CompatibilityResult
            {
                CheckName = "Architecture",
                Status = CompatibilityStatus.Unsupported,
                Details = "32-bit operating system detected. This application requires a 64-bit OS.",
                Recommendation = "Please use a 64-bit version of Windows."
            };
        }
    }
private CompatibilityResult CheckCpu()
    {
        try
        {
            var cpuInfo = GetCpuInfo();
            var coreCount = Environment.ProcessorCount;
            
            if (coreCount >= 2)
            {
                return new CompatibilityResult
                {
                    CheckName = "CPU",
                    Status = CompatibilityStatus.Compatible,
                    Details = cpuInfo + " (" + coreCount + " cores)"
                };
            }
            else
            {
                return new CompatibilityResult
                {
                    CheckName = "CPU",
                    Status = CompatibilityStatus.Warning,
                    Details = cpuInfo + " (" + coreCount + " core) - Multi-seat requires at least 2 cores for acceptable performance",
                    Recommendation = "A CPU with 4+ cores is recommended for multi-seat usage."
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get CPU info");
            return new CompatibilityResult
            {
                CheckName = "CPU",
                Status = CompatibilityStatus.Warning,
                Details = "Unable to detect CPU information",
                Recommendation = "Ensure your CPU meets minimum requirements (2+ cores)."
            };
        }
    }

    private CompatibilityResult CheckRam()
    {
        try
        {
            var totalRamBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            var totalRamGb = totalRamBytes / (1024.0 * 1024.0 * 1024.0);
            
            if (totalRamGb >= 8)
            {
                return new CompatibilityResult
                {
                    CheckName = "RAM",
                    Status = CompatibilityStatus.Compatible,
                    Details = totalRamGb.ToString("F1") + " GB available"
                };
            }
            else if (totalRamGb >= 4)
            {
                return new CompatibilityResult
                {
                    CheckName = "RAM",
                    Status = CompatibilityStatus.Warning,
                    Details = totalRamGb.ToString("F1") + " GB available - Minimum for basic operation",
                    Recommendation = "8 GB or more recommended for multi-seat usage."
                };
            }
            else
            {
                return new CompatibilityResult
                {
                    CheckName = "RAM",
                    Status = CompatibilityStatus.Unsupported,
                    Details = totalRamGb.ToString("F1") + " GB available - Insufficient for multi-seat operation",
                    Recommendation = "At least 8 GB RAM recommended."
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get RAM info");
            return new CompatibilityResult
            {
                CheckName = "RAM",
                Status = CompatibilityStatus.Warning,
                Details = "Unable to detect RAM information",
                Recommendation = "Ensure you have at least 8 GB RAM."
            };
        }
    }

    private CompatibilityResult CheckDiskSpace()
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\");
            var freeSpaceGb = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
            
            if (freeSpaceGb >= 5)
            {
                return new CompatibilityResult
                {
                    CheckName = "Disk Space",
                    Status = CompatibilityStatus.Compatible,
                    Details = freeSpaceGb.ToString("F1") + " GB free on system drive"
                };
            }
            else if (freeSpaceGb >= 1)
            {
                return new CompatibilityResult
                {
                    CheckName = "Disk Space",
                    Status = CompatibilityStatus.Warning,
                    Details = freeSpaceGb.ToString("F1") + " GB free - Low disk space",
                    Recommendation = "Free up disk space for optimal performance."
                };
            }
            else
            {
                return new CompatibilityResult
                {
                    CheckName = "Disk Space",
                    Status = CompatibilityStatus.Unsupported,
                    Details = freeSpaceGb.ToString("F1") + " GB free - Critically low disk space",
                    Recommendation = "Free up at least 5 GB of disk space before installing."
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get disk space info");
            return new CompatibilityResult
            {
                CheckName = "Disk Space",
                Status = CompatibilityStatus.Warning,
                Details = "Unable to detect disk space",
                Recommendation = "Ensure you have at least 5 GB free space."
            };
        }
    }
private CompatibilityResult CheckGpu()
    {
        try
        {
            var gpuInfo = GetGpuInfo();
            
            if (!string.IsNullOrWhiteSpace(gpuInfo) && !gpuInfo.Contains("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                return new CompatibilityResult
                {
                    CheckName = "GPU",
                    Status = CompatibilityStatus.Compatible,
                    Details = gpuInfo
                };
            }
            else
            {
                return new CompatibilityResult
                {
                    CheckName = "GPU",
                    Status = CompatibilityStatus.Warning,
                    Details = "No dedicated GPU detected or unable to identify GPU",
                    Recommendation = "A dedicated GPU is recommended for multi-seat with graphics acceleration."
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get GPU info");
            return new CompatibilityResult
            {
                CheckName = "GPU",
                Status = CompatibilityStatus.Warning,
                Details = "Unable to detect GPU information",
                Recommendation = "A dedicated GPU is recommended for multi-seat usage."
            };
        }
    }

    private string GetOperatingSystem()
    {
        var version = Environment.OSVersion;
        var osName = "Windows";
        
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem");
            foreach (var obj in searcher.Get())
            {
                return obj["Caption"]?.ToString() + " " + obj["Version"]?.ToString();
            }
        }
        catch
        {
        }
        
        return osName + " " + version.Version.Major + "." + version.Version.Minor + " (Build " + version.Version.Build + ")";
    }

    private string GetArchitecture()
    {
        return Environment.Is64BitOperatingSystem ? "x64" : "x86";
    }
private string GetCpuInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");
            var results = new List<string>();
            
            foreach (var obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString()?.Trim() ?? "Unknown CPU";
                var cores = obj["NumberOfCores"]?.ToString() ?? "?";
                var logical = obj["NumberOfLogicalProcessors"]?.ToString() ?? "?";
                results.Add(name + " (" + cores + " cores, " + logical + " logical)");
            }
            
            return string.Join("; ", results);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query CPU info via WMI");
            return "Unknown CPU (" + Environment.ProcessorCount + " logical processors)";
        }
    }

    private string GetRamInfo()
    {
        try
        {
            var totalRamBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            var totalRamGb = totalRamBytes / (1024.0 * 1024.0 * 1024.0);
            return totalRamGb.ToString("F1") + " GB";
        }
        catch
        {
            return "Unknown";
        }
    }

    private string GetGpuInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM, DriverVersion FROM Win32_VideoController WHERE AdapterRAM > 0");
            var results = new List<string>();
            
            foreach (var obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString()?.Trim() ?? "Unknown GPU";
                var ramBytes = obj["AdapterRAM"] as uint? ?? 0;
                var ramGb = ramBytes / (1024.0 * 1024.0 * 1024.0);
                var driver = obj["DriverVersion"]?.ToString() ?? "Unknown";
                results.Add(name + " (" + ramGb.ToString("F1") + " GB VRAM, Driver: " + driver + ")");
            }
            
            return results.Count > 0 ? string.Join("; ", results) : "No dedicated GPU detected";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query GPU info via WMI");
            return "Unknown GPU";
        }
    }

    private string GetFreeDiskSpace()
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\");
            var freeSpaceGb = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
            var totalSpaceGb = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
            return freeSpaceGb.ToString("F1") + " GB free of " + totalSpaceGb.ToString("F1") + " GB";
        }
        catch
        {
            return "Unknown";
        }
    }
}
namespace FreeSeat.Core.Diagnostics;

/// <summary>
/// Represents system diagnostic information.
/// </summary>
public sealed record SystemDiagnostics
{
    public string OperatingSystem { get; init; } = string.Empty;
    public string Architecture { get; init; } = string.Empty;
    public string Cpu { get; init; } = string.Empty;
    public string Ram { get; init; } = string.Empty;
    public string Gpu { get; init; } = string.Empty;
    public string FreeDiskSpace { get; init; } = string.Empty;
    public string ApplicationVersion { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Formats the diagnostics as a readable string for clipboard copying.
    /// </summary>
    public override string ToString()
    {
        return $$"""
            FreeSeat Diagnostics
            ====================
            Generated: {{GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}}
            
            Application Version: {{ApplicationVersion}}
            
            Operating System: {{OperatingSystem}}
            Architecture: {{Architecture}}
            CPU: {{Cpu}}
            RAM: {{Ram}}
            GPU: {{Gpu}}
            Free Disk Space: {{FreeDiskSpace}}
            """;
    }
}

/// <summary>
/// Compatibility check result for system requirements.
/// </summary>
public sealed record CompatibilityResult
{
    public required string CheckName { get; init; }
    public required CompatibilityStatus Status { get; init; }
    public required string Details { get; init; }
    public string? Recommendation { get; init; }
}

public enum CompatibilityStatus
{
    Compatible,
    Warning,
    Unsupported
}
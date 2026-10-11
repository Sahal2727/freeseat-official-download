namespace FreeSeat.Core.Diagnostics;

/// <summary>
/// Service for collecting system diagnostic information.
/// </summary>
public interface IDiagnosticsService
{
    /// <summary>
    /// Collects comprehensive system diagnostics.
    /// </summary>
    Task<SystemDiagnostics> CollectDiagnosticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a read-only system compatibility check.
    /// </summary>
    Task<IReadOnlyList<CompatibilityResult>> CheckCompatibilityAsync(CancellationToken cancellationToken = default);
}
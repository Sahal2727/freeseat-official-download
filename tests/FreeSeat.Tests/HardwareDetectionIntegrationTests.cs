using FreeSeat.Core.Diagnostics;
using FreeSeat.Core.Hardware.Interfaces;
using FreeSeat.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace FreeSeat.Tests;

public sealed class HardwareDetectionIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public HardwareDetectionIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task HardwareSnapshot_IncludesAdaptersReportedByDiagnostics()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure();

        using var provider = services.BuildServiceProvider();
        var hardwareService = provider.GetRequiredService<IHardwareDetectionService>();
        await hardwareService.RefreshAsync();
        var diagnostics = await provider.GetRequiredService<IDiagnosticsService>().CollectDiagnosticsAsync();

        var diagnosticGpuNames = diagnostics.Gpu
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => value.Split(" (", 2, StringSplitOptions.None)[0])
            .Where(value => !value.StartsWith("No ", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var snapshot = hardwareService.CurrentSnapshot;
        _output.WriteLine($"Monitors: {string.Join("; ", snapshot.Monitors.Select(m => $"{m.FriendlyName} [{m.PrimaryDisplayStatus}]"))}");
        _output.WriteLine($"GPUs: {string.Join("; ", snapshot.Gpus.Select(g => g.FriendlyName))}");
        _output.WriteLine($"CPU: {snapshot.Cpu?.Name ?? "not detected"}; RAM bytes: {snapshot.Memory?.TotalBytes.ToString() ?? "not detected"}");
        Assert.NotNull(snapshot.Cpu);
        Assert.NotNull(snapshot.Memory);
        Assert.InRange(snapshot.Monitors.Count(m => m.IsPrimaryKnown && m.IsPrimary), 0, 1);
        Assert.All(diagnosticGpuNames, expectedName =>
            Assert.Contains(snapshot.Gpus, gpu =>
                string.Equals(gpu.FriendlyName, expectedName, StringComparison.OrdinalIgnoreCase)));
    }
}

using FreeSeat.Core;
using FreeSeat.Core.Diagnostics;
using FreeSeat.Core.Hardware.Models;
using FreeSeat.Core.Seats.Models;
using FreeSeat.Infrastructure.Configuration;
using FluentAssertions;
using System.IO;
using Xunit;

namespace FreeSeat.Tests;

/// <summary>
/// Unit tests for application version and metadata.
/// </summary>
public class AppInfoTests
{
    [Fact]
    public void AppName_ShouldBeFreeSeat()
    {
        AppInfo.Name.Should().Be("FreeSeat");
    }

    [Fact]
    public void Tagline_ShouldBeCorrect()
    {
        AppInfo.Tagline.Should().Be("Two seats. One PC.");
    }

    [Fact]
    public void Creator_ShouldBeCorrect()
    {
        AppInfo.Creator.Should().Be("Created by Sahal");
    }

    [Fact]
    public void Copyright_ShouldBeCorrect()
    {
        AppInfo.Copyright.Should().Be("© 2026 Sahal");
    }

    [Fact]
    public void Version_ShouldBe100()
    {
        AppInfo.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void GitHubUrl_ShouldBePlaceholder()
    {
        AppInfo.GitHubUrl.Should().StartWith("TODO:");
    }

    [Fact]
    public void DocumentationUrl_ShouldBePlaceholder()
    {
        AppInfo.DocumentationUrl.Should().StartWith("TODO:");
    }

    [Fact]
    public void LicenseUrl_ShouldBePlaceholder()
    {
        AppInfo.LicenseUrl.Should().StartWith("TODO:");
    }
}
/// <summary>
/// Unit tests for diagnostics generation.
/// </summary>
public class DiagnosticsTests
{
    [Fact]
    public void SystemDiagnostics_ToString_ContainsRequiredFields()
    {
        var diagnostics = new SystemDiagnostics
        {
            OperatingSystem = "Windows 11 Pro",
            Architecture = "x64",
            Cpu = "Intel Core i7 (8 cores, 16 logical)",
            Ram = "16.0 GB",
            Gpu = "NVIDIA RTX 3080 (10.0 GB VRAM)",
            FreeDiskSpace = "500.0 GB free of 1000.0 GB",
            ApplicationVersion = "1.0.0",
            GeneratedAt = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)
        };

        var output = diagnostics.ToString();

        output.Should().Contain("FreeSeat Diagnostics");
        output.Should().Contain("Operating System: Windows 11 Pro");
        output.Should().Contain("Architecture: x64");
        output.Should().Contain("CPU: Intel Core i7");
        output.Should().Contain("RAM: 16.0 GB");
        output.Should().Contain("GPU: NVIDIA RTX 3080");
        output.Should().Contain("Free Disk Space: 500.0 GB free of 1000.0 GB");
        output.Should().Contain("Application Version: 1.0.0");
        output.Should().Contain("2026-01-15 12:00:00 UTC");
    }

    [Fact]
    public void CompatibilityResult_ShouldHaveCorrectStatuses()
    {
        var compatible = new CompatibilityResult
        {
            CheckName = "Test",
            Status = CompatibilityStatus.Compatible,
            Details = "OK"
        };
        compatible.Status.Should().Be(CompatibilityStatus.Compatible);

        var warning = new CompatibilityResult
        {
            CheckName = "Test",
            Status = CompatibilityStatus.Warning,
            Details = "Warning"
        };
        warning.Status.Should().Be(CompatibilityStatus.Warning);

        var unsupported = new CompatibilityResult
        {
            CheckName = "Test",
            Status = CompatibilityStatus.Unsupported,
            Details = "Not supported"
        };
        unsupported.Status.Should().Be(CompatibilityStatus.Unsupported);
    }
}
/// <summary>
/// Unit tests for settings serialization.
/// </summary>
public class SettingsTests
{
    [Fact]
    public void AppSettings_DefaultValues()
    {
        var settings = new AppSettings();

        settings.General.Language.Should().Be("en-US");
        settings.General.CheckForUpdates.Should().BeTrue();
        settings.General.SendAnonymousUsageData.Should().BeFalse();

        settings.Appearance.Theme.Should().Be(AppTheme.Dark);
        settings.Appearance.UseAccentColor.Should().BeTrue();

        settings.Startup.StartMinimized.Should().BeFalse();
        settings.Startup.StartWithWindows.Should().BeFalse();
    }

    [Fact]
    public void AppSettings_Serialization_RoundTrip()
    {
        var original = new AppSettings
        {
            General = new GeneralSettings
            {
                Language = "de-DE",
                CheckForUpdates = false,
                SendAnonymousUsageData = true
            },
            Appearance = new AppearanceSettings
            {
                Theme = AppTheme.Dark,
                UseAccentColor = false
            },
            Startup = new StartupSettings
            {
                StartMinimized = true,
                StartWithWindows = true
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(original, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);

        deserialized.Should().NotBeNull();
        deserialized!.General.Language.Should().Be("de-DE");
        deserialized.General.CheckForUpdates.Should().BeFalse();
        deserialized.General.SendAnonymousUsageData.Should().BeTrue();
        deserialized.Appearance.Theme.Should().Be(AppTheme.Dark);
        deserialized.Appearance.UseAccentColor.Should().BeFalse();
        deserialized.Startup.StartMinimized.Should().BeTrue();
        deserialized.Startup.StartWithWindows.Should().BeTrue();
    }

    [Fact]
    public void AppTheme_Enum_HasExpectedValues()
    {
        Enum.GetValues<AppTheme>().Should().BeEquivalentTo(new[] { AppTheme.Light, AppTheme.Dark, AppTheme.System });
    }
}
/// <summary>
/// Unit tests for seat configuration models.
/// </summary>
public class SeatModelTests
{
    [Fact]
    public void SeatId_New_IsUnique()
    {
        var id1 = SeatId.New();
        var id2 = SeatId.New();
        id1.Value.Should().NotBe(id2.Value);
    }

    [Fact]
    public void SeatId_Parse_RoundTrip()
    {
        var original = SeatId.New();
        var parsed = SeatId.Parse(original.ToString());
        parsed.Value.Should().Be(original.Value);
    }

    [Fact]
    public void SeatId_TryParse_ValidGuid()
    {
        var guid = Guid.NewGuid();
        SeatId.TryParse(guid.ToString(), out var seatId).Should().BeTrue();
        seatId.Value.Should().Be(guid);
    }

    [Fact]
    public void SeatId_TryParse_InvalidString()
    {
        SeatId.TryParse("not-a-guid", out var seatId).Should().BeFalse();
        seatId.Value.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Seat_CreateDefault_HasCorrectDefaults()
    {
        var seat = Seat.CreateDefault("Test Seat");

        seat.Id.Value.Should().NotBe(Guid.Empty);
        seat.Name.Should().Be("Test Seat");
        seat.IsEnabled.Should().BeTrue();
        seat.Assignment.Should().NotBeNull();
        seat.Assignment.MonitorId.Should().BeNull();
        seat.Assignment.KeyboardId.Should().BeNull();
        seat.Assignment.MouseId.Should().BeNull();
        seat.Assignment.SessionId.Should().BeNull();
        seat.Status.Should().Be(SeatStatus.Unconfigured);
    }

    [Fact]
    public void Seat_CreateDefaultPair_CreatesTwoSeats()
    {
        var (seat1, seat2) = Seat.CreateDefaultPair();

        seat1.Name.Should().Be("Seat 1");
        seat2.Name.Should().Be("Seat 2");
        seat1.Id.Value.Should().NotBe(seat2.Id.Value);
    }

    [Fact]
    public void Seat_WithName_ReturnsNewInstance()
    {
        var original = Seat.CreateDefault("Original");
        var updated = original.WithName("Updated");

        updated.Name.Should().Be("Updated");
        original.Name.Should().Be("Original"); // Immutable
        updated.Id.Should().Be(original.Id);
    }

    [Fact]
    public void Seat_WithEnabled_ReturnsNewInstance()
    {
        var original = Seat.CreateDefault("Test");
        var updated = original.WithEnabled(false);

        updated.IsEnabled.Should().BeFalse();
        original.IsEnabled.Should().BeTrue(); // Immutable
    }

    [Fact]
    public void Seat_WithMonitor_ReturnsNewInstance()
    {
        var original = Seat.CreateDefault("Test");
        var updated = original.WithMonitor("monitor-123");

        updated.Assignment.MonitorId.Should().Be("monitor-123");
        original.Assignment.MonitorId.Should().BeNull(); // Immutable
    }

    [Fact]
    public void Seat_WithKeyboard_ReturnsNewInstance()
    {
        var original = Seat.CreateDefault("Test");
        var updated = original.WithKeyboard("keyboard-456");

        updated.Assignment.KeyboardId.Should().Be("keyboard-456");
        original.Assignment.KeyboardId.Should().BeNull();
    }

    [Fact]
    public void Seat_WithMouse_ReturnsNewInstance()
    {
        var original = Seat.CreateDefault("Test");
        var updated = original.WithMouse("mouse-789");

        updated.Assignment.MouseId.Should().Be("mouse-789");
        original.Assignment.MouseId.Should().BeNull();
    }

    [Fact]
    public void Seat_WithSession_ReturnsNewInstance()
    {
        var original = Seat.CreateDefault("Test");
        var updated = original.WithSession(42);

        updated.Assignment.SessionId.Should().Be(42);
        original.Assignment.SessionId.Should().BeNull();
    }

    [Fact]
    public void Seat_ClearAssignments_ReturnsNewInstance()
    {
        var original = Seat.CreateDefault("Test")
            .WithMonitor("m1")
            .WithKeyboard("k1")
            .WithMouse("m2")
            .WithSession(1);

        var updated = original.ClearAssignments();

        updated.Assignment.MonitorId.Should().BeNull();
        updated.Assignment.KeyboardId.Should().BeNull();
        updated.Assignment.MouseId.Should().BeNull();
        updated.Assignment.SessionId.Should().BeNull();
    }
}
/// <summary>
/// Unit tests for seat assignment.
/// </summary>
public class SeatAssignmentTests
{
    [Fact]
    public void SeatAssignment_Default_IsEmpty()
    {
        var assignment = new SeatAssignment();

        assignment.MonitorId.Should().BeNull();
        assignment.KeyboardId.Should().BeNull();
        assignment.MouseId.Should().BeNull();
        assignment.SessionId.Should().BeNull();
        assignment.IsComplete.Should().BeFalse();
        assignment.HasAnyAssignment.Should().BeFalse();
    }

    [Fact]
    public void SeatAssignment_WithMonitor()
    {
        var assignment = new SeatAssignment().WithMonitor("monitor-1");
        assignment.MonitorId.Should().Be("monitor-1");
        assignment.HasAnyAssignment.Should().BeTrue();
    }

    [Fact]
    public void SeatAssignment_IsComplete_WhenAllAssigned()
    {
        var assignment = new SeatAssignment()
            .WithMonitor("m1")
            .WithKeyboard("k1")
            .WithMouse("m2")
            .WithSession(1);

        assignment.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void SeatAssignment_IsComplete_WhenPartial()
    {
        var assignment = new SeatAssignment()
            .WithMonitor("m1")
            .WithKeyboard("k1");

        assignment.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void SeatAssignment_Clear_ReturnsEmpty()
    {
        var assignment = new SeatAssignment()
            .WithMonitor("m1")
            .WithKeyboard("k1")
            .WithMouse("m2")
            .WithSession(1);

        var cleared = assignment.Clear();

        cleared.IsComplete.Should().BeFalse();
        cleared.HasAnyAssignment.Should().BeFalse();
    }
}
/// <summary>
/// Unit tests for seat validation.
/// </summary>
public class SeatValidationTests
{
    [Fact]
    public void SeatValidationResult_Success_IsValid()
    {
        var result = SeatValidationResult.Success();

        result.IsValid.Should().BeTrue();
        result.Status.Should().Be(SeatStatus.Ready);
        result.Issues.Should().BeEmpty();
    }

    [Fact]
    public void SeatValidationResult_Failure_WithIssues()
    {
        var issues = new[] { SeatValidationIssue.MissingRequired(DeviceType.Monitor) };
        var result = SeatValidationResult.Failure(issues);

        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(SeatStatus.Incomplete);
        result.Issues.Should().HaveCount(1);
    }

    [Fact]
    public void SeatValidationIssue_MissingRequired()
    {
        var issue = SeatValidationIssue.MissingRequired(DeviceType.Monitor);

        issue.Type.Should().Be(ValidationIssueType.MissingRequired);
        issue.DeviceType.Should().Be(DeviceType.Monitor);
        issue.Message.Should().Contain("monitor");
    }

    [Fact]
    public void SeatValidationIssue_DeviceUnavailable()
    {
        var issue = SeatValidationIssue.DeviceUnavailable(DeviceType.Keyboard, "kb-123");

        issue.Type.Should().Be(ValidationIssueType.DeviceUnavailable);
        issue.DeviceType.Should().Be(DeviceType.Keyboard);
        issue.AffectedId.Should().Be("kb-123");
        issue.Message.Should().Contain("keyboard");
        issue.Message.Should().Contain("kb-123");
    }

    [Fact]
    public void SeatValidationIssue_SessionUnavailable()
    {
        var issue = SeatValidationIssue.SessionUnavailable(42);

        issue.Type.Should().Be(ValidationIssueType.SessionUnavailable);
        issue.AffectedId.Should().Be("42");
        issue.Message.Should().Contain("session");
        issue.Message.Should().Contain("42");
    }

    [Fact]
    public void SeatValidationIssue_Conflict()
    {
        var issue = SeatValidationIssue.Conflict(DeviceType.Monitor, "mon-1", new[] { "Seat 1", "Seat 2" });

        issue.Type.Should().Be(ValidationIssueType.Conflict);
        issue.DeviceType.Should().Be(DeviceType.Monitor);
        issue.AffectedId.Should().Be("mon-1");
        issue.Message.Should().Contain("Seat 1");
        issue.Message.Should().Contain("Seat 2");
    }

    [Fact]
    public void SeatLifecycleState_GetDescription()
    {
        SeatLifecycleState.Ready.GetDescription().Should().Be("Ready");
        SeatLifecycleState.Incomplete.GetDescription().Should().Be("Incomplete");
        SeatLifecycleState.DeviceUnavailable.GetDescription().Should().Be("Device Unavailable");
        SeatLifecycleState.SessionUnavailable.GetDescription().Should().Be("Session Unavailable");
        SeatLifecycleState.Conflict.GetDescription().Should().Be("Conflict");
        SeatLifecycleState.Disabled.GetDescription().Should().Be("Disabled");
    }
}
/// <summary>
/// Unit tests for seat configuration.
/// </summary>
public class SeatConfigurationTests
{
    [Fact]
    public void SeatConfiguration_CreateDefault_HasTwoSeats()
    {
        var config = SeatConfiguration.CreateDefault();

        config.Count.Should().Be(2);
        config.Version.Should().Be(1);
        config.Seats.Should().HaveCount(2);
        config.Seats[0].Name.Should().Be("Seat 1");
        config.Seats[1].Name.Should().Be("Seat 2");
    }

    [Fact]
    public void SeatConfiguration_WithSeat_AddsOrUpdates()
    {
        var config = SeatConfiguration.CreateDefault();
        var newSeat = Seat.CreateDefault("New Seat");
        var updated = config.WithSeat(newSeat);

        updated.Count.Should().Be(3);
        updated.Seats.Should().Contain(s => s.Name == "New Seat");
    }

    [Fact]
    public void SeatConfiguration_WithoutSeat_RemovesSeat()
    {
        var config = SeatConfiguration.CreateDefault();
        var seatId = config.Seats[0].Id;
        var updated = config.WithoutSeat(seatId);

        updated.Count.Should().Be(1);
        updated.Seats.Should().NotContain(s => s.Id == seatId);
    }

    [Fact]
    public void SeatConfiguration_GetSeat_ById()
    {
        var config = SeatConfiguration.CreateDefault();
        var seat = config.GetSeat(config.Seats[0].Id);

        seat.Should().NotBeNull();
        seat!.Id.Should().Be(config.Seats[0].Id);
    }

    [Fact]
    public void SeatConfiguration_CompareTo_DetectsChanges()
    {
        var config1 = SeatConfiguration.CreateDefault();
        var seat = config1.Seats[0];
        var config2 = config1.WithSeat(seat.WithName("Renamed"));

        var diff = config1.CompareTo(config2);

        diff.HasChanges.Should().BeTrue();
        diff.TotalChanges.Should().Be(1);
        diff.Changed.Should().HaveCount(1);
        diff.Changed[0].NameChanged.Should().BeTrue();
    }

    [Fact]
    public void SeatConfiguration_CompareTo_NoChanges()
    {
        var seat1 = Seat.CreateDefault("Seat 1");
        var seat2 = Seat.CreateDefault("Seat 2");
        var config1 = new SeatConfiguration { Version = 1, Seats = [seat1, seat2], SavedAt = DateTime.UtcNow };
        var config2 = new SeatConfiguration { Version = 1, Seats = [seat1, seat2], SavedAt = DateTime.UtcNow };

        var diff = config1.CompareTo(config2);

        diff.HasChanges.Should().BeFalse();
        diff.TotalChanges.Should().Be(0);
    }
}
/// <summary>
/// Unit tests for installer/build configuration.
/// </summary>
public class InstallerConfigTests
{
    private static string GetRepoRoot() => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));

    [Fact]
    public void InnoSetupScript_Exists()
    {
        var scriptPath = Path.Combine(GetRepoRoot(), "src", "FreeSeat.Setup", "FreeSeat.iss");
        File.Exists(scriptPath).Should().BeTrue("Inno Setup script should exist");
    }

    [Fact]
    public void InnoSetupScript_ContainsRequiredSections()
    {
        var scriptPath = Path.Combine(GetRepoRoot(), "src", "FreeSeat.Setup", "FreeSeat.iss");
        var content = File.ReadAllText(scriptPath);

        content.Should().Contain("[Setup]");
        content.Should().Contain("[Languages]");
        content.Should().Contain("[Tasks]");
        content.Should().Contain("[Files]");
        content.Should().Contain("[Icons]");
        content.Should().Contain("[Run]");
        content.Should().Contain("[UninstallDelete]");
        content.Should().Contain("MyAppName");
        content.Should().Contain("MyAppVersion");
        content.Should().Contain("FreeSeat");
    }

    [Fact]
    public void DirectoryBuildProps_Exists()
    {
        var propsPath = Path.Combine(GetRepoRoot(), "Directory.Build.props");
        File.Exists(propsPath).Should().BeTrue("Directory.Build.props should exist");
    }

    [Fact]
    public void DirectoryBuildProps_ContainsVersion()
    {
        var propsPath = Path.Combine(GetRepoRoot(), "Directory.Build.props");
        var content = File.ReadAllText(propsPath);

        content.Should().Contain("<Version>1.0.0</Version>");
        content.Should().Contain("<AssemblyVersion>1.0.0.0</AssemblyVersion>");
        content.Should().Contain("<FileVersion>1.0.0.0</FileVersion>");
        content.Should().Contain("<InformationalVersion>1.0.0</InformationalVersion>");
    }
}

using System;
using System.IO;
using FreeSeat.App;
using Xunit;

namespace FreeSeat.Tests;

public sealed class StartupLogTests
{
    [Fact]
    public void WriteTo_CreatesMissingLogDirectoryAndAppendsMessage()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), $"FreeSeat-StartupLog-{Guid.NewGuid():N}");
        var logPath = Path.Combine(testRoot, "Logs", "startup.log");

        try
        {
            StartupLog.WriteTo(logPath, "first launch diagnostic");

            Assert.True(File.Exists(logPath));
            Assert.Contains("first launch diagnostic", File.ReadAllText(logPath));
        }
        finally
        {
            if (Directory.Exists(testRoot))
                Directory.Delete(testRoot, recursive: true);
        }
    }
}

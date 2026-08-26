using System.Management.Automation;
using MCPServerPS.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace MCPServerPS.Tests.Unit;

public class StreamHandlerTests
{
    // DataAddingEventArgs has an internal constructor, so records are pushed through the real
    // PSDataCollection<T> to trigger genuine DataAdding events instead of hand-building event args.
    private static (StreamHandler Handler, FakeLogger Logger, PowerShell Pwsh) CreateRegisteredHandler()
    {
        var logger = new FakeLogger();
        var handler = new StreamHandler(logger);
        var pwsh = PowerShell.Create();
        handler.RegisterStreamEvents(pwsh);
        return (handler, logger, pwsh);
    }

    [Fact]
    public void DebugRecord_LogsAtDebugLevel()
    {
        var (_, logger, pwsh) = CreateRegisteredHandler();
        using (pwsh)
        {
            pwsh.Streams.Debug.Add(new DebugRecord("debug message"));

            var entry = Assert.Single(logger.Collector.GetSnapshot());
            Assert.Equal(LogLevel.Debug, entry.Level);
            Assert.Contains("debug message", entry.Message);
        }
    }

    [Fact]
    public void WarningRecord_LogsAtWarningLevel()
    {
        var (_, logger, pwsh) = CreateRegisteredHandler();
        using (pwsh)
        {
            pwsh.Streams.Warning.Add(new WarningRecord("warning message"));

            var entry = Assert.Single(logger.Collector.GetSnapshot());
            Assert.Equal(LogLevel.Warning, entry.Level);
            Assert.Contains("warning message", entry.Message);
        }
    }

    [Fact]
    public void ErrorRecord_LogsAtErrorLevel_WithFormattedMessage()
    {
        var (_, logger, pwsh) = CreateRegisteredHandler();
        using (pwsh)
        {
            var error = new ErrorRecord(new InvalidOperationException("boom"), "TestError", ErrorCategory.NotSpecified, null);
            pwsh.Streams.Error.Add(error);

            var entry = Assert.Single(logger.Collector.GetSnapshot());
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Contains("boom", entry.Message);
        }
    }

    [Fact]
    public void InformationRecord_LogsAtInformationLevel()
    {
        var (_, logger, pwsh) = CreateRegisteredHandler();
        using (pwsh)
        {
            pwsh.Streams.Information.Add(new InformationRecord("info message", "TestSource"));

            var entry = Assert.Single(logger.Collector.GetSnapshot());
            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.Contains("info message", entry.Message);
        }
    }

    [Fact]
    public void VerboseRecord_LogsAtInformationLevel()
    {
        var (_, logger, pwsh) = CreateRegisteredHandler();
        using (pwsh)
        {
            pwsh.Streams.Verbose.Add(new VerboseRecord("verbose message"));

            var entry = Assert.Single(logger.Collector.GetSnapshot());
            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.Contains("verbose message", entry.Message);
        }
    }

    [Fact]
    public void ProgressRecord_LogsAtInformationLevel()
    {
        var (_, logger, pwsh) = CreateRegisteredHandler();
        using (pwsh)
        {
            pwsh.Streams.Progress.Add(new ProgressRecord(1, "activity", "status"));

            var entry = Assert.Single(logger.Collector.GetSnapshot());
            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.Contains("status", entry.Message);
        }
    }

    [Fact]
    public void UnregisterStreamEvents_StopsForwarding()
    {
        var (handler, logger, pwsh) = CreateRegisteredHandler();
        using (pwsh)
        {
            handler.UnregisterStreamEvents(pwsh);
            pwsh.Streams.Debug.Add(new DebugRecord("should not be logged"));

            Assert.Empty(logger.Collector.GetSnapshot());
        }
    }
}

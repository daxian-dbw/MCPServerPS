using MCPServerPS.Tools;
using ModelContextProtocol.Protocol;
using Xunit;

namespace MCPServerPS.Tests.Component;

public class PSScriptMcpServerToolTests
{
    private static readonly string ScriptPath = Path.Combine(AppContext.BaseDirectory, "Component", "Fixtures", "Scripts", "Counter.ps1");

    private static async Task<string?> InvokeAsync(PSScriptMcpServerTool tool, string action, int value = 0)
    {
        var request = RequestContextTestHelper.CreateRequest(tool.ProtocolTool.Name, new Dictionary<string, object?>
        {
            ["Action"] = action,
            ["Value"] = value
        });

        CallToolResult result = await tool.InvokeAsync(request);
        return result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;
    }

    [Fact]
    public async Task DifferentInstances_DoNotShareState()
    {
        var toolA = new PSScriptMcpServerTool(ScriptPath);
        var toolB = new PSScriptMcpServerTool(ScriptPath);

        await InvokeAsync(toolA, "Set", 42);
        string? bResult = await InvokeAsync(toolB, "Get");

        Assert.Equal("-1", bResult);
    }

    [Fact]
    public async Task SameInstance_PersistsStateAcrossCalls()
    {
        var tool = new PSScriptMcpServerTool(ScriptPath);

        await InvokeAsync(tool, "Set", 42);
        string? result = await InvokeAsync(tool, "Get");

        Assert.Equal("42", result);
    }

    [Fact]
    public async Task ConcurrentCallsToSameInstance_AreSerialized_NoLostUpdates()
    {
        var tool = new PSScriptMcpServerTool(ScriptPath);
        await InvokeAsync(tool, "Set", 0);

        // Task.Run is required to get genuine thread-pool concurrency: InvokeAsync completes
        // synchronously, so awaiting it directly in Select never yields and calls run one at a time.
        var tasks = Enumerable.Range(0, 5).Select(_ => Task.Run(() => InvokeAsync(tool, "Add"))).ToArray();
        await Task.WhenAll(tasks);

        string? final = await InvokeAsync(tool, "Get");
        Assert.Equal("5", final);
    }
}

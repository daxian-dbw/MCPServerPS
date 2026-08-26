using MCPServerPS.Tools;
using ModelContextProtocol.Protocol;
using Xunit;

namespace MCPServerPS.Tests.Component;

public class ModuleToolsMetadataTests
{
    private static readonly string ModulePath = Path.Combine(AppContext.BaseDirectory, "Component", "Fixtures", "Module", "ComponentTestModule.psd1");

    private static Dictionary<string, PSModuleMcpServerTool> GetTools(ModuleToolsMetadata metadata) =>
        metadata.GetFunctionMcpTools().Cast<PSModuleMcpServerTool>().ToDictionary(t => t.ProtocolTool.Name);

    private static async Task<string?> InvokeAsync(PSModuleMcpServerTool tool, IDictionary<string, object?>? arguments = null)
    {
        var request = RequestContextTestHelper.CreateRequest(tool.ProtocolTool.Name, arguments);
        CallToolResult result = await tool.InvokeAsync(request);
        return result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;
    }

    [Fact]
    public async Task FunctionTools_ShareModuleScopedState()
    {
        var tools = GetTools(new ModuleToolsMetadata(ModulePath));

        await InvokeAsync(tools["Set_ModuleCounter"], new Dictionary<string, object?> { ["Value"] = 7 });
        string? result = await InvokeAsync(tools["Get_ModuleCounter"]);

        Assert.Equal("7", result);
    }

    [Fact]
    public async Task ConcurrentCalls_AreSerialized_NoLostUpdates()
    {
        var tools = GetTools(new ModuleToolsMetadata(ModulePath));

        await InvokeAsync(tools["Set_ModuleCounter"], new Dictionary<string, object?> { ["Value"] = 0 });

        // Task.Run is required to get genuine thread-pool concurrency: InvokeAsync completes
        // synchronously, so awaiting it directly in Select never yields and calls run one at a time.
        var tasks = Enumerable.Range(0, 5).Select(_ => Task.Run(() => InvokeAsync(tools["Add_ModuleCounter"]))).ToArray();
        await Task.WhenAll(tasks);

        string? final = await InvokeAsync(tools["Get_ModuleCounter"]);
        Assert.Equal("5", final);
    }
}

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;

namespace MCPServerPS.Tests.E2E;

public class NativeCommandHangTests
{
    private static readonly string ScriptRoot = Path.Combine(AppContext.BaseDirectory, "E2E", "Fixtures", "Scripts");

    [Fact]
    public async Task NativeCommand_InvokedFromScriptTool_DoesNotHang()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await using McpClient client = await McpServerProcess.StartAsync(
            name: "E2E-NativeCommandHang",
            scriptRoot: ScriptRoot,
            cancellationToken: cts.Token);

        CallToolResult result = await client.CallToolAsync(
            "Get_GitVersion",
            new Dictionary<string, object?>(),
            cancellationToken: cts.Token);

        Assert.Null(result.IsError);
        string text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        Assert.Matches(@"^git version \d+\.\d+\.\d+", text.Trim());
    }
}

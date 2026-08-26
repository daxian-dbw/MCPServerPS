using ModelContextProtocol.Client;
using Xunit;

namespace MCPServerPS.Tests.E2E;

public class ServerInfoTests
{
    [Fact]
    public async Task ServerInfoName_MatchesNameParameter()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        string expectedName = $"E2E-ServerInfo-{Guid.NewGuid():N}";

        await using McpClient client = await McpServerProcess.StartAsync(expectedName, cancellationToken: cts.Token);

        Assert.Equal(expectedName, client.ServerInfo.Name);
    }

    [Fact]
    public async Task DefaultParameterSet_ExposesBuiltInTools()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await using McpClient client = await McpServerProcess.StartAsync("E2E-DefaultTools", cancellationToken: cts.Token);

        var tools = await client.ListToolsAsync(cancellationToken: cts.Token);
        Assert.Contains(tools, t => t.Name == "echo");
    }
}

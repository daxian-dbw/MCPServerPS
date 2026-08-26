using ModelContextProtocol.Client;

namespace MCPServerPS.Tests.E2E;

// Spawns a real `pwsh -Command "Import-Module ...; Start-MyMCP ..."` child process and speaks the
// real MCP stdio protocol to it via McpClient, the way an actual MCP client (e.g. VS Code) would.
internal static class McpServerProcess
{
    // ProjectReference content propagation copies MCPServerPS.psd1/.dll next to the test assembly.
    private static readonly string ModuleManifestPath = Path.Combine(AppContext.BaseDirectory, "MCPServerPS.psd1");

    internal static Task<McpClient> StartAsync(string name, string? scriptRoot = null, CancellationToken cancellationToken = default)
    {
        string command = $"Import-Module '{ModuleManifestPath}'; Start-MyMCP -Name '{name}'";
        if (scriptRoot is { })
        {
            command += $" -ScriptRoot '{scriptRoot}'";
        }

        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = name,
            Command = "pwsh",
            Arguments = ["-NoProfile", "-NonInteractive", "-Command", command],
            ShutdownTimeout = TimeSpan.FromSeconds(10),
        });

        return McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
    }
}

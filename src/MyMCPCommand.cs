using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Management.Automation;
using MCPServerPS.Tools;
using System.Reflection.Metadata;
using System.Reflection;

namespace MCPServerPS;

[Cmdlet(VerbsLifecycle.Start, "MyMCP", DefaultParameterSetName = "Default")]
public class MyMCPCommand : PSCmdlet
{
    private const string DefaultSet = "Default";
    private const string ScriptToolSet = nameof(ScriptRoot);
    private const string ModuleToolSet = nameof(Module);

    [Parameter(Mandatory = true, ParameterSetName = ScriptToolSet)]
    public string ScriptRoot { get; set; }

    [Parameter(Mandatory = true, ParameterSetName = ModuleToolSet)]
    public string Module { get; set; }

    protected override void ProcessRecord()
    {
        MarkPowerShellAsRunningInServerSide();
        Task mcpTask = Task.Run(async () => await StartMCPServer(ParameterSetName, ScriptRoot ?? Module));
        mcpTask.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Set the internal static property "IsServerSide" of PowerShell to true. This is a workaround to allow
    /// pwsh to behave as if it's running in a server-side context, so when starting a CLI command, it will
    /// redirect the stdin, stdout, and stderr of the CLI command to prevent it from messing with the MCP
    /// communication. Without this, some CLI commands will hang when running in a script/function tool.
    /// </summary>
    private static void MarkPowerShellAsRunningInServerSide()
    {
        Assembly assembly = typeof(PSObject).Assembly;
        Type type = assembly.GetType("System.Management.Automation.NativeCommandProcessor");
        PropertyInfo property = type.GetProperty("IsServerSide", BindingFlags.Static | BindingFlags.NonPublic);
        property.SetValue(null, true);
    }

    private static async Task StartMCPServer(string parameterSet, string scriptRootOrModule)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder([]);
        builder.Logging.AddConsole(consoleLogOptions =>
        {
            // Configure all logs to go to stderr
            consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        IMcpServerBuilder mcpBuilder = builder.Services
            .AddMcpServer()
            .WithStdioServerTransport();

        if (parameterSet is DefaultSet)
        {
            mcpBuilder.WithToolsFromAssembly();
        }
        else if (parameterSet is ScriptToolSet)
        {
            List<McpServerTool> tools = [];
            if (Directory.Exists(scriptRootOrModule))
            {
                foreach (string file in Directory.EnumerateFiles(scriptRootOrModule, "*.ps1"))
                {
                    tools.Add(new PSScriptMcpServerTool(file));
                }
            }
            mcpBuilder.WithTools(tools);
        }
        else
        {
            ModuleToolsMetadata metadata = new(scriptRootOrModule);
            var tools = metadata.GetFunctionMcpTools();
            mcpBuilder.WithTools(tools);
        }

        await builder.Build().RunAsync();
    }
}

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"Hello from C#: {message}";

    [McpServerTool, Description("Echoes in reverse the message sent by the client.")]
    public static string ReverseEcho(string message) => new([.. message.Reverse()]);
}

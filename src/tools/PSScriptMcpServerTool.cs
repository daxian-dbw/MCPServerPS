using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPServerPS.Tools;

public class PSScriptMcpServerTool : McpServerTool
{
    private readonly string _scriptPath;
    private readonly PowerShell _pwsh;
    private readonly Tool _tool;

    internal PSScriptMcpServerTool(string scriptPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(scriptPath);

        var iss = InitialSessionState.CreateDefault2();
        if (OperatingSystem.IsWindows())
        {
            iss.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Unrestricted;
        }

        _scriptPath = scriptPath;
        _pwsh = PowerShell.Create(iss);

        ExternalScriptInfo scriptInfo = _pwsh
            .AddCommand("Get-Command")
            .AddParameter("Name", _scriptPath)
            .Execute<ExternalScriptInfo>() ?? throw new ArgumentException($"The script '{_scriptPath}' cannot be found.");
        _tool = PSToolUtils.CreateToolForScriptOrFunction(_pwsh, scriptInfo);
    }

    public override Tool ProtocolTool => _tool;

    public override ValueTask<CallToolResult> InvokeAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        Dictionary<string, PSObject> realArgs = null;
        if (request.Params?.Arguments is { } argDict)
        {
            realArgs = PSToolUtils.ConvertArgs(_pwsh, argDict);
        }

        _pwsh.AddCommand(_scriptPath);
        if (realArgs is { })
        {
            foreach (var kvp in realArgs)
            {
                _pwsh.AddParameter(kvp.Key, kvp.Value);
            }
        }

        ILoggerProvider loggerProvider = request.Server.AsClientLoggerProvider();
        ILogger logger = loggerProvider.CreateLogger(_tool.Name);
        StreamHandler streamHandler = new(logger);

        try
        {
            streamHandler.RegisterStreamEvents(_pwsh);
            Collection<PSObject> results = _pwsh.Execute();
            return ValueTask.FromResult(GetCallToolResult(results));
        }
        catch (Exception e)
        {
            return ValueTask.FromResult(PSToolUtils.GetErrorResult(_tool.Name, e));
        }
        finally
        {
            streamHandler.UnregisterStreamEvents(_pwsh);
        }
    }

    private CallToolResult GetCallToolResult(Collection<PSObject> results)
    {
        if (results is null || results.Count is 0)
        {
            return new CallToolResult() { Content = [] };
        }

        if (results.Count is 1 && results[0].BaseObject is string text)
        {
            return new CallToolResult() { Content = [new TextContentBlock { Text = text }] };
        }

        object input = results.Count is 1 ? results[0] : results;
        string json = _pwsh
            .AddCommand("ConvertTo-Json")
            .AddParameter("InputObject", input)
            .AddParameter("Depth", 1)
            .AddParameter("EnumsAsStrings", true)
            .AddParameter("Compress", true)
            .ExecuteAndReturnString(errorTemplate: string.Empty);

        return new CallToolResult() { Content = [new TextContentBlock { Text = json }] };
    }
}

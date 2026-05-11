using ModelContextProtocol.Server;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace MCPServerPS.Tools;

[McpServerToolType]
public sealed class PowerShellTools
{
    private readonly InitialSessionState _iss;
    private readonly PowerShell _pwsh;

    public PowerShellTools()
    {
        _iss = InitialSessionState.CreateDefault2();
        if (OperatingSystem.IsWindows())
        {
            _iss.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Unrestricted;
        }
        _pwsh = PowerShell.Create(_iss);
    }

    [McpServerTool, Description("Get help content for a PowerShell cmdlet.")]
    public string GetHelpForCmdlet([Description("The name of a PowerShell cmdlet to get help for.")] string command)
    {
        const string error = """
            Failed to retrieve the help content due to the following error:
            ```
            {0}
            ```
            Check to see if it's caused by the passed-in command name, and if so, please try again.
            """;

        return _pwsh
            .AddCommand("Get-Help").AddParameter("Name", command)
            .AddCommand("Out-String").AddParameter("Width", 150)
            .ExecuteAndReturnString(error);
    }

    [McpServerTool, Description("Get help content about one or more parameters of a PowerShell cmdlet.")]
    public string GetHelpForParameter(
        [Description("The name of a PowerShell cmdlet.")] string command,
        [Description("The names of one or more parameters of the specified PowerShell cmdlet.")] string[] parameters)
    {
        const string error = """
            Failed to retrieve the help content due to the following error:
            ```
            {0}
            ```
            Check to see if it's caused by the passed-in command name or parameter name(s), and if so, please try again.
            """;

        return _pwsh
            .AddCommand("Get-Help").AddParameter("Name", command).AddParameter("Parameter", parameters)
            .AddCommand("Out-String").AddParameter("Width", 150)
            .ExecuteAndReturnString(error);
    }
}

internal static class PowerShellExt
{
    public static string ExecuteAndReturnString(this PowerShell pwsh, string errorTemplate)
    {
        try
        {
            string result = pwsh.Invoke<string>().FirstOrDefault();
            return result;
        }
        catch (Exception e)
        {
            return string.Format(errorTemplate, e.Message);
        }
        finally
        {
            pwsh.Commands.Clear();
            pwsh.Streams.ClearStreams();
        }
    }

    public static Collection<PSObject> Execute(this PowerShell pwsh)
    {
        try
        {
            var result = pwsh.Invoke();
            return result;
        }
        finally
        {
            pwsh.Commands.Clear();
            pwsh.Streams.ClearStreams();
        }
    }

    public static T Execute<T>(this PowerShell pwsh)
    {
        try
        {
            T result = pwsh.Invoke<T>().FirstOrDefault();
            return result;
        }
        finally
        {
            pwsh.Commands.Clear();
            pwsh.Streams.ClearStreams();
        }
    }
}

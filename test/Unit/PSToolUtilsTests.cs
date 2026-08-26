using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text.Json;
using MCPServerPS.Tools;
using ModelContextProtocol.Protocol;
using Xunit;

namespace MCPServerPS.Tests.Unit;

public class PSToolUtilsTests
{
    private static readonly string FixturesDir = Path.Combine(AppContext.BaseDirectory, "Unit", "Fixtures", "Scripts");

    // Matches the ExecutionPolicy the production PSScriptMcpServerTool/ModuleToolsMetadata set, since Get-Command
    // on an .ps1 must load its script block to inspect help/parameters, which the default policy blocks on Windows.
    private static PowerShell CreatePowerShell()
    {
        var iss = InitialSessionState.CreateDefault2();
        if (OperatingSystem.IsWindows())
        {
            iss.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Unrestricted;
        }

        return PowerShell.Create(iss);
    }

    private static ExternalScriptInfo GetScriptInfo(PowerShell pwsh, string fileName)
    {
        string path = Path.Combine(FixturesDir, fileName);
        return pwsh.AddCommand("Get-Command").AddParameter("Name", path).Execute<ExternalScriptInfo>();
    }

    [Fact]
    public void CreateTool_ValidScript_GeneratesExpectedSchema()
    {
        using PowerShell pwsh = CreatePowerShell();
        ExternalScriptInfo scriptInfo = GetScriptInfo(pwsh, "Get-Widget.ps1");

        Tool tool = PSToolUtils.CreateToolForScriptOrFunction(pwsh, scriptInfo);

        Assert.Equal("Get_Widget", tool.Name);
        Assert.Equal("Returns a fixture widget object for testing tool schema generation.", tool.Description);

        JsonElement properties = tool.InputSchema.GetProperty("properties");
        Assert.True(properties.TryGetProperty("Name", out _));
        Assert.True(properties.TryGetProperty("Count", out _));

        string[] required = tool.InputSchema.GetProperty("required").EnumerateArray().Select(e => e.GetString()!).ToArray();
        Assert.Equal(["Name"], required);
    }

    [Fact]
    public void CreateTool_NoCommentBasedHelp_Throws()
    {
        using PowerShell pwsh = CreatePowerShell();
        ExternalScriptInfo scriptInfo = GetScriptInfo(pwsh, "NoHelp.ps1");

        var ex = Assert.Throws<InvalidDataException>(() => PSToolUtils.CreateToolForScriptOrFunction(pwsh, scriptInfo));
        Assert.Contains("No description found", ex.Message);
    }

    [Fact]
    public void CreateTool_MultipleParameterSets_Throws()
    {
        using PowerShell pwsh = CreatePowerShell();
        ExternalScriptInfo scriptInfo = GetScriptInfo(pwsh, "MultiParamSet.ps1");

        var ex = Assert.Throws<InvalidDataException>(() => PSToolUtils.CreateToolForScriptOrFunction(pwsh, scriptInfo));
        Assert.Contains("cannot have more than 1 parameter set", ex.Message);
    }

    [Fact]
    public void CreateTool_MissingParameterDescription_Throws()
    {
        using PowerShell pwsh = CreatePowerShell();
        ExternalScriptInfo scriptInfo = GetScriptInfo(pwsh, "MissingParamDescription.ps1");

        var ex = Assert.Throws<InvalidDataException>(() => PSToolUtils.CreateToolForScriptOrFunction(pwsh, scriptInfo));
        Assert.Contains("No description found for the parameter", ex.Message);
    }
}

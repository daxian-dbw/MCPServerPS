# MCPServerPS

This project creates a PowerShell module that serves as a Model Context Protocol (MCP) server.
The module exposes the `Start-MyMCP` cmdlet, which starts the MCP server and dynamically exposes tools based on how it is invoked.

## MCP server as a PowerShell Module

**Benefits of shipping an MCP server as a PowerShell module:**

- Small size: 3.35 MB. Only the .NET MCP SDK assemblies are required.
- Availability: easy to distribute and install via PowerShell Gallery across platforms.
- Flexible: expose C# tools, `.ps1` script tools, or module function tools.
- For an MCP server that needs PowerShell features, the runtime is naturally available (runs in `pwsh.exe`), so no need to ship PowerShell SDK assemblies.

## Building the Module

To build the project, run:

```pwsh
dotnet publish .\MCPServerPS.csproj
```

The published module will be available at `.\out\MCPServerPS`.

After deploying the module to your PowerShell module path, you can start the MCP server using the `Start-MyMCP` cmdlet.

## Start-MyMCP Cmdlet Parameter Sets

The `Start-MyMCP` cmdlet supports three parameter sets:

```
Start-MyMCP [<CommonParameters>]
Start-MyMCP -ScriptRoot <string> [<CommonParameters>]
Start-MyMCP -Module <string> [<CommonParameters>]
```

- **Default**: `Start-MyMCP` exposes all MCP tools defined in the C# code of this assembly.
- **ScriptRoot**: `Start-MyMCP -ScriptRoot <path-to-directory>` exposes each `.ps1` script file in the specified directory as an MCP tool.
- **Module**: `Start-MyMCP -Module <module-name-or-path-to-module>` exposes each function within the specified module as an MCP tool.

Examples of script tools and module function tools can be found in the `./scripts` folder.

Both script tools and module function tools use comment-based help to define the description and parameters for each tool. This ensures that tool metadata is discoverable and user-friendly.

### Runspace Behavior

- Each script tool runs in its own dedicated Runspace, so they are isolated from each other.
- Module function tools share the same Runspace for the module, allowing changes to module state made by one function tool to be visible to others. This is because module functions are considered related and may need to share state.

## Using MCPServerPS in VSCode

### 1. Exposing C# MCP Tools

```json
{
    "servers": {
        "MCPServerPS": {
            "type": "stdio",
            "command": "pwsh",
            "args": [
                "-noprofile",
                "-c",
                "MCPServerPS\\Start-MyMCP"
            ]
        }
    },
    "inputs": []
}
```

### 2. Exposing PowerShell Script Tools

Assume the local repo root is at `E:\repos\MCPServerPS`.

```json
{
    "servers": {
        "MCPServerPS": {
            "type": "stdio",
            "command": "pwsh",
            "args": [
                "-noprofile",
                "-c",
                "MCPServerPS\\Start-MyMCP -ScriptRoot E:\\repos\\MCPServerPS\\scripts"
            ]
        }
    },
    "inputs": []
}
```

### 3. Exposing Module Function Tools

Assume the local repo root is at `E:\repos\MCPServerPS`.

```json
{
    "servers": {
        "MCPServerPS": {
            "type": "stdio",
            "command": "pwsh",
            "args": [
                "-noprofile",
                "-c",
                "MCPServerPS\\Start-MyMCP -Module E:\\repos\\MCPServerPS\\scripts\\tools.psm1"
            ]
        }
    },
    "inputs": []
}
```

## Installing latest version from main

### Prerequisites

1. PSResourceGet installed.
2. A SecretStore created called `default`

### Instructions

#### Setup a PAT to read the feed.  

Create a [Personal Access Token (Classic)](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens#creating-a-personal-access-token-classic) with `package:read` permission. At the time of writing, this is the only type of token that allows these premissions.

```powershell
Set-Secret -Name GitHubPackageRead
```

The command should prompt for the secret.

Register the Repository.
This assumes you called you vault default as described in the prerequisites.

```powershell
Register-PSResourceRepository -name dongbo -uri https://nuget.pkg.github.com/daxian-dbw/index.json -ApiVersion V3 -CredentialInfo @{VaultName='default';SecretName='GitHubPackageRead'}
```

```powershell
Install-PSResource MCPServerPS
```

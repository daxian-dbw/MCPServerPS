# MCPServerPS Project Specification

**Version**: 1.0
**Date**: January 5, 2026
**Status**: Draft

---

## 1. Project Overview

MCPServerPS is a PowerShell module that serves as a Model Context Protocol (MCP) server. It provides a flexible framework for exposing tools to MCP clients through PowerShell scripts, module functions, or compiled C# code. The module's primary entry point is the `Start-MyMCP` cmdlet, which starts the MCP server and dynamically exposes tools based on invocation parameters.

### 1.1 Key Benefits

- **Small size**: 3.35 MB - only requires .NET MCP SDK assemblies
- **Cross-platform availability**: Easy distribution and installation via PowerShell Gallery
- **Flexible tool sources**: Supports C# tools, `.ps1` script tools, and module function tools
- **Natural PowerShell integration**: Runtime available in `pwsh.exe` without shipping PowerShell SDK assemblies

---

## 2. Command Surface

### 2.1 Start-MyMCP Cmdlet

The `Start-MyMCP` cmdlet is the primary interface for starting the MCP server. It supports three parameter sets:

```powershell
Start-MyMCP [<CommonParameters>]
Start-MyMCP -ScriptRoot <string> [<CommonParameters>]
Start-MyMCP -Module <string> [<CommonParameters>]
```

#### Parameter Set Behaviors

1. **Default (No Parameters)**
   - Exposes all MCP tools defined in the C# code of the MCPServerPS assembly
   - Suitable for using built-in tools

2. **ScriptRoot Parameter**
   - Syntax: `Start-MyMCP -ScriptRoot <path-to-directory>`
   - Exposes each `.ps1` script file in the specified directory as an MCP tool
   - Each script becomes a separate tool with metadata derived from comment-based help

3. **Module Parameter**
   - Syntax: `Start-MyMCP -Module <module-name-or-path-to-module>`
   - Exposes each exported function within the specified module as an MCP tool
   - Functions use comment-based help for tool metadata

### 2.2 Runspace Behavior

The module implements different runspace strategies based on tool type:

- **Script Tools** (from `-ScriptRoot`): Each script tool runs in its own dedicated runspace, providing complete isolation between tool invocations and thus parallel tool calling is supported.
- **Module Function Tools** (from `-Module`): All functions from the same module share a single runspace, allowing changes to module state made by one function to be visible to others. This design recognizes that module functions are typically related and may need to share state. Parallel tool calling is not supported.

---

## 3. Tool Construction Requirements

### 3.1 Eligibility Criteria

For a PowerShell script or function to be exposed as an MCP tool, it must meet the following requirements:

1. **Type**: Must be either `ExternalScriptInfo` (for `.ps1` scripts) or `FunctionInfo` (for module functions)
2. **Parameter Sets**: Cannot have more than 1 parameter set
3. **Comment-Based Help**: Must include:
   - Script/function description (mandatory)
   - Parameter descriptions for all declared parameters (mandatory if any parameters exist)

### 3.2 JSON Schema Generation

The module automatically generates JSON schema for tool parameters using the following rules:

1. **SwitchParameter Mapping**
   - PowerShell `[SwitchParameter]` type maps to JSON `bool` type

2. **Required Parameters**
   - Mark parameters as `required` in the schema when they are mandatory in PowerShell

3. **Default Values** (for non-mandatory parameters)
   - Use the constant expression default value defined in the script/function, OR
   - Use the default value of the parameter type

4. **Schema Generation Method**
   - Use `Microsoft.Extension.AI.AIJsonUtilities.CreateJsonSchema` method to create the parameter schema for each parameter

---

## 4. Design Review and Key Decisions

This section addresses critical design questions and documents the decisions made for the project's evolution.

### 4.1 Community Tool Sharing and Discovery

**Question**: How should the community share and discover tool modules?

**Decision**: Adopt a tag-based discovery pattern similar to PowerShell `Crescendo` modules.

**Implementation**:
- Module authors add `MCPTools` to the `Tags` array in their module manifest:
  ```powershell
  @{
      ModuleVersion = '1.0.0'
      # ... other manifest properties
      PrivateData = @{
          PSData = @{
              Tags = @('MCPTools', 'MCP', 'AI')
          }
      }
  }
  ```

- Users discover tool modules via:
  ```powershell
  Find-PSResource -Tag MCPTools
  ```

**Rationale**:
- Mirrors the successful Crescendo pattern (`Find-PSResource -Tag Crescendo`)
- Leverages existing PowerShell Gallery infrastructure
- Simple for both authors and consumers
- No additional registry or discovery service required

### 4.2 Multiple Module Support

**Question**: Should `Start-MyMCP -Module` accept multiple modules simultaneously?

**Decision**: Extend `-Module` parameter to accept `string[]` (array of module names or paths).

**Implementation Details**:

1. **Parameter Type**:
   ```powershell
   -Module <string[]>
   ```

2. **Behavior**:
   - Load and expose tools from all specified modules
   - Tools from different modules are aggregated into a single tool set

3. **Tool Filtering Parameters** (new):
   - `-IncludeTools <string[]>`: Whitelist of specific function names to expose
   - `-ExcludeTools <string[]>`: Blacklist of function names to exclude
   - Both support wildcards (e.g., `Get-*`, `*-Config`)

4. **Collision Handling**:
   - Optional `-UsePrefix` parameter to namespace tools (e.g., `ModuleName_FunctionName`)
   - Without prefix: last module wins on name collision, with warning emitted
   - With prefix: tools are automatically namespaced to avoid collisions

5. **Safety Safeguards**:
   - `-MaxTools <int>` parameter to limit total exposed tools (default: 50)
   - Prevents accidental exposure of very large tool sets
   - Configurable/overrideable by user

**Example Usage**:
```powershell
# Expose tools from multiple modules
Start-MyMCP -Module @('Module1', 'Module2', 'Module3')

# With filtering
Start-MyMCP -Module @('Module1', 'Module2') -IncludeTools @('Get-*', 'Set-Config')

# With namespacing
Start-MyMCP -Module @('Module1', 'Module2') -UsePrefix

# With custom limit
Start-MyMCP -Module @('LargeModule1', 'LargeModule2') -MaxTools 100
```

**Rationale**:
- Users may need tools from multiple specialized modules
- Provides flexibility while maintaining safety through filtering and limits
- Addresses the "too many tools" concern with explicit controls

### 4.3 MCP Tool Hints Declaration

**Question**: How should scripts and module functions declare MCP tool hints (`destructiveHint`, `idempotentHint`, `openWorldHint`, `readOnlyHint`)?

**Decision**: Use comment-based help's `.NOTES` section with structured key-value format.

**Supported Hint Keys**:
- `DestructiveHint`: Tool may modify environment or have side effects
- `IdempotentHint`: Safe to call multiple times; produces consistent results
- `OpenWorldHint`: Tool may access external resources or services
- `ReadOnlyHint`: Tool does not modify its environment or have side effects

**Format Specification**:
```powershell
<#
.SYNOPSIS
Adds two numbers together.

.DESCRIPTION
This function takes two integers and returns their sum.

.PARAMETER A
The first number to add.

.PARAMETER B
The second number to add.

.NOTES
ReadOnlyHint: This tool does not modify its environment or have side effects.

.EXAMPLE
Add-Number -A 5 -B 3
Returns 8
#>
function Add-Number {
    param(
        [int]$A,
        [int]$B
    )
    return $A + $B
}
```

**Parsing Logic**:
1. Extract `.NOTES` section from `Get-Help` output (`$help.alertSet.alert[0].Text`)
2. Parse lines matching pattern: `<HintKey>:\s*<description>`
3. Map recognized hint keys to MCP tool metadata fields
4. Emit warnings for unrecognized hint keys (ignore them)

**Validation Rules**:
- **Conflicting hints** (e.g., both `DestructiveHint` and `ReadOnlyHint`): 
  - Default behavior: Error and refuse to expose tool
- **Missing hints**: Acceptable; hints are optional metadata

**Rationale**:
- Leverages existing comment-based help infrastructure
- Familiar pattern for PowerShell developers
- Human-readable in source code
- Accessible via `Get-Help` for documentation
- Extensible for future hint types

### 4.4 Future Considerations

- If we integrate AIShell into PowerShell, making PowerShell an MCP client, then how to detect MCP server tool modules on user's machine?
- If there are lots of MCP tool modules, maybe we need a tool to tell the agent what tools to use for a specific task.

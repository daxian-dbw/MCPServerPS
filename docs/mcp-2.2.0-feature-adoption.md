# MCP SDK 2.2.0 Feature Adoption Opportunities

This document tracks features introduced in the `ModelContextProtocol` NuGet package (up to
2.2.0) that MCPServerPS does not yet use, but could adopt to improve the experience of exposing
PowerShell scripts, module functions, and C# tools as MCP tools.

## 1. Progress notifications (high value, low effort)

**Current state**: `StreamHandler.ProgressDataAdding` (in
[`src/tools/StreamHandler.cs`](../src/tools/StreamHandler.cs)) forwards PowerShell's
`Write-Progress` output as a plain `Information` log line:

```csharp
_logger.LogInformation("PROGRESS: {ProgressMessage}", record.StatusDescription);
```

**Opportunity**: `RequestContext<CallToolRequestParams>` exposes real progress-notification
support tied to the client's `progressToken`. Since `Write-Progress` maps almost 1:1 to MCP's
progress concept, a long-running script/function could report real progress percentage/status
to the client's UI (e.g. a progress bar) instead of an invisible stderr log line.

## 2. Elicitation / MRTR (high value, addresses a real gap)

**Current state**: `MarkPowerShellAsRunningInServerSide()` (in
[`src/MyMCPCommand.cs`](../src/MyMCPCommand.cs)) prevents *native CLI* commands from hanging when
there's no interactive terminal attached, but does nothing for pure PowerShell interactive
prompts such as `Read-Host` or `$host.UI.PromptForChoice()` — those still hang or fail today.

**Opportunity**: The SDK's elicitation support (`ElicitRequestParams`) and MRTR
(`InputRequiredException`) let a tool call pause mid-execution and ask the *connected client* for
input, then resume. This is the correct fix for interactive PowerShell prompts, rather than
suppressing/ignoring them.

## 3. Tool annotations (ReadOnly/Destructive/Idempotent hints) — natural fit for PowerShell

**Current state**: `PSToolUtils.CreateToolForScriptOrFunction` (in
[`src/tools/PSToolUtils.cs`](../src/tools/PSToolUtils.cs)) only sets `Name`, `Description`, and
`InputSchema` on the generated `Tool` — `Tool.Annotations` is never populated.

**Opportunity**: MCP's `ToolAnnotations` (`ReadOnlyHint`, `DestructiveHint`, `IdempotentHint`,
`OpenWorldHint`) map almost perfectly onto PowerShell's approved-verb conventions that scripts and
functions already follow (`Get-`/`Test-`/`Show-` are read-only; `Remove-`/`Clear-`/`Stop-` are
destructive; etc.). These hints could be derived automatically from the command's verb with very
little effort, giving MCP clients (e.g. VS Code) better auto-approval/confirmation UX for free.

## 4. Structured content / output schema (medium value)

**Current state**: Tool results are hand-serialized via `ConvertTo-Json` into a raw text blob
(`GetCallToolResult` in both `PSScriptMcpServerTool` and `PSModuleMcpServerTool`).

**Opportunity**: `UseStructuredContent` plus an auto-generated `OutputSchema` would let results
come back as validated, schema-described structured JSON (`CallToolResult.StructuredContent`)
instead of an opaque text string — more reliable for clients/models to consume than parsing
embedded JSON text.

## 5. Tasks extension (situational)

**Current state**: Every tool call runs synchronously to completion within a single request.

**Opportunity**: `ModelContextProtocol.Extensions.Tasks` lets a tool call run as a background MCP
Task that the client can poll instead of holding a single synchronous request open. Worth
considering only if long-running scripts are a real use case for this project — otherwise it adds
a dependency for limited benefit.

## Suggested priority

1. Progress notifications — cheap win, directly improves UX for long-running scripts.
2. Tool annotations from verbs — cheap win, improves client-side approval UX.
3. Elicitation / MRTR — most impactful, but the biggest change (touches the stream-handling and
   tool-invocation model).
4. Structured content / output schema — nice-to-have, depends on how results are consumed.
5. Tasks extension — situational, only if long-running scripts become a real requirement.

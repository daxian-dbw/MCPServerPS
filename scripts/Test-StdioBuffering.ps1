<#
.SYNOPSIS
    Minimal repro of stdio/buffering issue with external commands in MCP context.

.DESCRIPTION
    This script provides a minimal reproduction of the stdio/buffering issue that occurs
    when running external commands (like git --version) in an MCP context and trying to
    capture their output.

    The issue manifests when:
    1. Running external commands that write to stdout
    2. Trying to capture the output into a PowerShell variable
    3. The MCP stdio transport interferes with output capture

    This tool tests various methods of capturing external command output to identify
    which approaches work reliably in an MCP context.

.PARAMETER Command
    The external command to run. Default: 'git'

.PARAMETER Arguments
    Arguments to pass to the command. Default: '--version'
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$Command = 'git',

    [Parameter(Mandatory=$false)]
    [string]$Arguments = '--version'
)

# Minimal repro: $gitVersion = git --version
Write-Host "Running command: git --version"
$gitVersion = git --version
Write-Host "Captured output: '$gitVersion'"

# Query the publish workflow runs using gh cli
gh run list --workflow publish.yml --limit 5 --json 'databaseId,createdAt,displayTitle' --repo daxian-dbw/MCPServerPS

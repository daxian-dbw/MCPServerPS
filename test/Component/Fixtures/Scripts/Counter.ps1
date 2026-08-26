<#
.SYNOPSIS
    Sets, gets, or increments a global-scoped counter.
.DESCRIPTION
    Used to verify PSScriptMcpServerTool Runspace isolation/persistence: 'Set' stores -Value,
    'Add' reads-sleeps-writes back +1 (to widen a race window for concurrency tests), and 'Get'
    returns the current value, or -1 if the counter was never set in this Runspace.
.PARAMETER Action
    One of 'Set', 'Get', or 'Add'.
.PARAMETER Value
    The value to store when -Action is 'Set'. Ignored otherwise.
#>
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Set', 'Get', 'Add')]
    [string]$Action,

    [Parameter(Mandatory = $false)]
    [int]$Value = 0
)

switch ($Action) {
    'Set' {
        $global:ComponentTestCounter = $Value
    }
    'Add' {
        $current = if (Test-Path variable:global:ComponentTestCounter) { $global:ComponentTestCounter } else { 0 }
        Start-Sleep -Milliseconds 50
        $global:ComponentTestCounter = $current + 1
    }
}

if (Test-Path variable:global:ComponentTestCounter) {
    $global:ComponentTestCounter
}
else {
    -1
}

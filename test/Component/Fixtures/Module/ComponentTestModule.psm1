$script:Counter = 0

<#
.SYNOPSIS
    Sets the module-scoped counter to the given value.
.DESCRIPTION
    Used to verify that function tools from the same module share one Runspace/module-scope state.
.PARAMETER Value
    The value to store in the module-scoped counter.
#>
function Set-ModuleCounter {
    param(
        [Parameter(Mandatory = $true)]
        [int]$Value
    )

    $script:Counter = $Value
    $script:Counter
}

<#
.SYNOPSIS
    Gets the current value of the module-scoped counter.
.DESCRIPTION
    Used to verify that function tools from the same module share one Runspace/module-scope state.
#>
function Get-ModuleCounter {
    $script:Counter
}

<#
.SYNOPSIS
    Increments the module-scoped counter by 1.
.DESCRIPTION
    Reads then sleeps before writing back, to widen the race window a missing lock would need to
    lose an update; used to verify concurrent module function tool calls are truly serialized.
#>
function Add-ModuleCounter {
    $current = $script:Counter
    Start-Sleep -Milliseconds 50
    $script:Counter = $current + 1
    $script:Counter
}

Export-ModuleMember -Function Set-ModuleCounter, Get-ModuleCounter, Add-ModuleCounter

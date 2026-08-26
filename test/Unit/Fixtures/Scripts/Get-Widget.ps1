<#
.SYNOPSIS
    Gets a widget by name.
.DESCRIPTION
    Returns a fixture widget object for testing tool schema generation.
.PARAMETER Name
    The name of the widget to retrieve.
.PARAMETER Count
    The number of widgets to return.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Name,

    [Parameter(Mandatory = $false)]
    [int]$Count = 1
)

[pscustomobject]@{ Name = $Name; Count = $Count }

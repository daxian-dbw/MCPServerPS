<#
.SYNOPSIS
    A script with more than one parameter set.
.DESCRIPTION
    Used to verify PSToolUtils rejects commands with multiple parameter sets.
.PARAMETER Name
    The name parameter.
.PARAMETER Id
    The id parameter.
#>
param(
    [Parameter(Mandatory = $true, ParameterSetName = 'ByName')]
    [string]$Name,

    [Parameter(Mandatory = $true, ParameterSetName = 'ById')]
    [int]$Id
)

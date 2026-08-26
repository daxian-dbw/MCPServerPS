<#
.SYNOPSIS
    A script whose parameter is missing a .PARAMETER description.
.DESCRIPTION
    Used to verify PSToolUtils rejects parameters without a description.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Name
)

$Name

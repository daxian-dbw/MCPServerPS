<#
.SYNOPSIS

Retrieves merged pull requests from the PowerShell repository that are marked for backport approved to a specific target release.

.DESCRIPTION

This script queries the PowerShell GitHub repository to find merged pull requests that have been labeled for backport approved to a specified target release version. It uses the GitHub CLI to fetch PR information including number, title, URL, and merge date, then sorts the results from oldest to newest based on merge date.

The script targets PRs with labels in the format "Backport-{TargetRelease}.x-Approved" where TargetRelease is the specified version.

.PARAMETER TargetRelease
The target PowerShell release version to check for backport approved PRs. Must be one of: '7.4', '7.5', or '7.6'.
This parameter is mandatory and determines which backport label to search for.

.OUTPUTS

System.Object[]. Returns an array of PowerShell objects containing PR information with properties:
- number: The pull request number
- title: The pull request title
- url: The pull request URL
- mergedAt: The date and time when the PR was merged
#>

param(
    [ValidateSet('7.4', '7.5', '7.6')]
    [Parameter(Mandatory)]
    [string] $TargetRelease
)

$Owner = "PowerShell"
$Repo = "PowerShell"
$approvalLabel = "Backport-$TargetRelease.x-Approved"

Write-Verbose "Target release: $TargetRelease" -Verbose

$prsJson = gh pr list --repo "$Owner/$Repo" --label $approvalLabel --state merged --json number,title,url,mergedAt --limit 100 2>&1
$prs = $prsJson | ConvertFrom-Json

if ($null -eq $prs -or $prs.Count -eq 0) {
    Write-Debug "No pull requests found for label $approvalLabel" -Debug
    return "No pull requests found for label $approvalLabel"
}

# Sort PRs from oldest merged to newest merged
$prs | Sort-Object mergedAt

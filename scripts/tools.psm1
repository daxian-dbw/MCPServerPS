<#
.SYNOPSIS

Calculate the sum of 2 integers.

.DESCRIPTION

Calculate the sum of 2 integers.

.PARAMETER number1
The first integer.

.PARAMETER number2
The second integer.

.OUTPUTS

System.Int32. Add-Number returns a number that is the sum.
#>
function Add-Number {
    param(
        [Parameter(Mandatory)]
        [int] $number1,

        [Parameter(Mandatory)]
        [int] $number2
    )

    $number1 + $number2
}


<#
.SYNOPSIS

Retrieves merged pull requests from the PowerShell repository that are marked for backport consideration to a specific target release.

.DESCRIPTION

This script queries the PowerShell GitHub repository to find merged pull requests that have been labeled for backport consideration to a specified target release version. It uses the GitHub CLI to fetch PR information including number, title, URL, and merge date, then sorts the results from oldest to newest based on merge date.

The script targets PRs with labels in the format "Backport-{TargetRelease}.x-Consider" where TargetRelease is the specified version.

.PARAMETER TargetRelease
The target PowerShell release version to check for backport consideration. Must be one of: '7.4', '7.5', or '7.6'.
This parameter is mandatory and determines which backport label to search for.

.OUTPUTS

System.Object[]. Returns an array of PowerShell objects containing PR information with properties:
- number: The pull request number
- title: The pull request title
- url: The pull request URL
- mergedAt: The date and time when the PR was merged
#>
function get_prs_with_backport_label {
    param(
        [ValidateSet('7.4', '7.5', '7.6')]
        [Parameter(Mandatory)]
        [string] $TargetRelease
    )

    $Owner = "PowerShell"
    $Repo = "PowerShell"
    $considerLabel = "Backport-$TargetRelease.x-Consider"

    Write-Verbose "Target Release: $TargetRelease" -Verbose

    $prsJson = gh pr list --repo "$Owner/$Repo" --label $considerLabel --state merged --json number,title,url,mergedAt --limit 100 2>&1
    $prs = $prsJson | ConvertFrom-Json

    Write-Debug "Result count: $($prs.Length)" -Debug

    # Sort PRs from oldest merged to newest merged
    $prs | Sort-Object mergedAt
}



<#
.SYNOPSIS

Verifies if a specific pull request from the PowerShell repository is eligible for backport to a target release.

.DESCRIPTION

This script validates whether a specific pull request is ready for backporting to a specified target release version. It performs several validation checks:

1. Verifies the PR is in a merged state
2. Checks if the PR has already been backported (has "Backport-{TargetRelease}.x-Done" label)
3. Searches for existing backport PRs to prevent duplicates

The script uses the GitHub CLI to fetch PR information and returns a validation result object with detailed feedback about the backport eligibility.

.PARAMETER PRNumber
The pull request number to verify for backport eligibility. This parameter is mandatory.

.PARAMETER TargetRelease
The target PowerShell release version to check for backport eligibility. Must be one of: '7.4', '7.5', or '7.6'.
This parameter is mandatory and determines which backport version to validate against.

.OUTPUTS

System.Management.Automation.PSCustomObject. Returns a PowerShell object containing:
- pr_info: Original PR information (state, merge_commit, title, author, labels)
- validation_status: "success" if eligible for backport, "failed" if not
- validation_feedback: Detailed message explaining the validation result
- existing_backport_pr_info: Information about existing backport PR (if found)
- original_pr_Info: Original PR info when backport already exists
#>
function verify_candidate_pr_for_backport {
    param(
        [parameter(Mandatory)]
        [string] $PRNumber,

        [ValidateSet('7.4', '7.5', '7.6')]
        [parameter(Mandatory)]
        [string] $TargetRelease
    )

    $prJson = gh pr view $PRNumber --repo PowerShell/PowerShell `
    --json state,mergeCommit,title,author,labels `
    --jq '{state: .state, merge_commit: .mergeCommit.oid, title: .title, author: .author.login, labels: [.labels[].name]}'

    $prInfo = $prJson | ConvertFrom-Json

    if ($prInfo.state -ne "MERGED") {
        return [PSCustomObject]@{
            pr_info = $prInfo
            validation_status = "failed"
            validation_feedback = "The PR #$PRNumber hasn't been merged (state: $($prInfo.state)). Please stop the backport and inform the user."
        }
    }

    if ($prInfo.labels -contains "Backport-$TargetRelease.x-Done") {
        return [PSCustomObject]@{
            pr_info = $prInfo
            validation_status = "failed"
            validation_feedback = "The PR has the label 'Backport-$TargetRelease.x-Done', which indicates it was already backported successfully. Please stop the backport and inform the user."
        }
    }

    $existingBackportPR = gh pr list --repo PowerShell/PowerShell `
        --search "in:title [release/v$TargetRelease] $($prInfo.title)" `
        --state all `
        --json number,state | ConvertFrom-Json

    if ($existingBackportPR) {
        return [PSCustomObject]@{
            original_pr_Info = $prInfo
            existing_backport_pr_info = $existingBackportPR
            validation_status = "failed"
            validation_feedback = "The backport PR for the original PR #$PRNumber already exists: #$($existingBackportPR.number). Ask the user if they want to continue the backport."
        }
    }

    return [PSCustomObject]@{
        pr_info = $prInfo
        validation_status = "success"
        validation_feedback = "The PR was merged and hasn't been backported yet. Please proceed."
    }
}



<#
.SYNOPSIS

Adds a file name extension to a supplied name.

.DESCRIPTION

Adds a file name extension to a supplied name.
Takes any strings for the file name or extension.

.PARAMETER Name
The file name to add extension to.

.PARAMETER Extension
The file extension(s) to use.

.PARAMETER UseGroup
Indicates whether group the result in curly brace.

.OUTPUTS

System.String. Add-Extension returns a string with the extension
or file name.
#>
function Add_Extension {
    param(
        [Parameter(Mandatory)]
        [string] $Name,

        [string[]] $Extension = "txt",
        [switch] $UseGroup
    )

    $result = $Extension | ForEach-Object { $Name, $_ -join '.' } | Join-String -Separator ', '
    $UseGroup ? "{$result}" : $result
}

<#
.SYNOPSIS
    Finds MCPServerPS from a GitHub Packages-backed PSResource repository,
    ensuring the gh CLI token has the required read:packages scope.
#>
[CmdletBinding()]
param(
    [string]$RepositoryName = 'daxian-dbw',
    [string]$ModuleName     = 'MCPServerPS'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# --- Ensure gh CLI is available ---
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw 'The GitHub CLI (gh) is not installed or not on PATH.'
}

# --- Ensure user is logged in and has read:packages scope ---
$statusOutput = gh auth status 2>&1 | Out-String
if ($LASTEXITCODE -ne 0) {
    throw "Not logged in to GitHub CLI. Run 'gh auth login' first."
}

$activeSection = ($statusOutput -split '(?=\S+\.com)') |
    Where-Object { $_ -match 'Active account: true' } |
    Select-Object -First 1

if (-not $activeSection) {
    throw "No active GitHub CLI account found. Run 'gh auth login' first."
}

if ($activeSection -notmatch 'read:packages') {
    Write-Host 'Your gh token is missing the read:packages scope.' -ForegroundColor Yellow
    Write-Host 'Running: gh auth refresh -s read:packages' -ForegroundColor Yellow
    Write-Host ''
    gh auth refresh -s read:packages
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to refresh token with read:packages scope.'
    }
    Write-Host 'Token updated successfully.' -ForegroundColor Green
}

# --- Build credential and query the repository ---
$secureToken = gh auth token | ConvertTo-SecureString -AsPlainText -Force
$credential  = [pscredential]::new('gh-token', $secureToken)

if (get-module -Name $ModuleName -ListAvailable -erroraction SilentlyContinue)
{
    Write-Verbose -Verbose -Message "Installing $ModuleName from $RepositoryName"
    Install-PSResource -Repository $RepositoryName -Name $ModuleName -Credential $credential
}
else {
    Write-Verbose -Verbose -Message "Updating $ModuleName from $RepositoryName"
    Update-PSResource -Repository $RepositoryName -Name $ModuleName -Credential $credential    
}

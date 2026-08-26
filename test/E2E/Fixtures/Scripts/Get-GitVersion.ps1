<#
.SYNOPSIS
    Gets the installed git version by invoking the git CLI.
.DESCRIPTION
    Regression guard for the native-command-hang workaround (MarkPowerShellAsRunningInServerSide):
    shells out to a real external process and captures its output, the pattern that used to hang
    when invoked from a tool running inside the MCP stdio server.
#>
$version = git --version
$version

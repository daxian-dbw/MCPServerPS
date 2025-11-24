@{

# Script module or binary module file associated with this manifest.
RootModule = 'MCPServerPS.dll'

# Version number of this module.
ModuleVersion = '0.0.1'

# Supported PSEditions
CompatiblePSEditions = @('Core')

# ID used to uniquely identify this module
GUID = 'ee73e061-d75d-4853-88a0-ac4e3964a74f'

# Author of this module
Author = 'dongbow'

# Company or vendor of this module
CompanyName = 'Unknown'

# Copyright statement for this module
Copyright = '(c) dongbow. All rights reserved.'

# Description of the functionality provided by this module
Description = "PowerShell module that hosts a Model Context Protocol (MCP) server to expose C#, script, and module tools as MCP endpoints."

# Minimum version of the PowerShell engine required by this module
PowerShellVersion = '7.4'

# Functions to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no functions to export.
FunctionsToExport = @()

# Cmdlets to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no cmdlets to export.
CmdletsToExport = @('Start-MyMCP')

# Variables to export from this module
VariablesToExport = '*'

# Aliases to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no aliases to export.
AliasesToExport = @()

# HelpInfo URI of this module
# HelpInfoURI = ''

# Default prefix for commands exported from this module. Override the default prefix using Import-Module -Prefix.
# DefaultCommandPrefix = ''

}

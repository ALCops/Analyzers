<#
.SYNOPSIS
Retrieve Business Central artifact URLs for Current, NextMinor, and NextMajor sandbox releases.

.DESCRIPTION
Uses the BCContainerHelper module's Get-BCArtifactUrl command to resolve BC Sandbox artifact
URLs for the 'core' country, selecting Current, NextMinor, and NextMajor.

The 'core' artifact ZIP contains ALLanguage.vsix in its root. Get-BC-DevTools.ps1 uses
HTTP Range requests to extract ALLanguage.vsix from this ZIP without a full download,
then reflects on Microsoft.Dynamics.Nav.Analyzers.Common.dll inside the VSIX to obtain
both the AL Language assembly version and target framework moniker.

Extracts the BC version from the returned URL (path segment index 4):
  https://bcartifacts-.../sandbox/{version}/core → {version}

Example: https://bcartifacts-exdbf9fwegejdqak.b02.azurefd.net/sandbox/25.0.23364.46804/core
         → version = 25.0.23364.46804

Current is stable (isBeta = false). NextMinor and NextMajor are insider builds (isBeta = true).
NextMajor is silently skipped when no next major release is available yet.

Emits a compact JSON array to STDOUT. Each element:
  { "version": "25.0.23364.46804", "select": "Current", "isBeta": false, "assetUri": "https://..." }

.EXAMPLE
  .\BC-Artifacts.ps1 | ConvertFrom-Json | Format-Table version, select, isBeta
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# Ensure BCContainerHelper is available
if (-not (Get-Module -ListAvailable -Name 'BcContainerHelper')) {
    Write-Host "Installing BcContainerHelper module..." -ForegroundColor Yellow
    Install-Module -Name BcContainerHelper -Force -AllowClobber -Scope CurrentUser
}

Import-Module BcContainerHelper -DisableNameChecking -ErrorAction Stop

# Selects to query, with their beta classification
$selects = @(
    @{ Select = 'SecondToLastMajor'; IsBeta = $false },
    @{ Select = 'Current'; IsBeta = $false },
    @{ Select = 'NextMinor'; IsBeta = $true },
    @{ Select = 'NextMajor'; IsBeta = $true }
)

$results = foreach ($item in $selects) {
    try {
        $url = Get-BCArtifactUrl -type Sandbox -country core -select $item.Select -accept_insiderEula 6>$null

        if (-not $url) {
            Write-Verbose "No artifact URL returned for -select $($item.Select); skipping."
            continue
        }

        # Extract the BC version from the URL path segment at index 4:
        # https://bcartifacts-.../sandbox/25.0.23364.46804/core
        #   [0]='https:' [1]='' [2]='host' [3]='sandbox' [4]='25.0.23364.46804' [5]='core'
        $version = $url.Split('/')[4]

        [PSCustomObject]@{
            version  = $version
            select   = $item.Select
            isBeta   = $item.IsBeta
            assetUri = $url
        }
    }
    catch {
        Write-Warning "Failed to retrieve artifact URL for -select $($item.Select): $($_.Exception.Message)"
    }
}

Write-Output $results | ConvertTo-Json -Compress

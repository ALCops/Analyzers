<#
.SYNOPSIS
Resolve the latest (or latest stable) NuGet version for a package from the flat container and emit its download URL.

.DESCRIPTION
Queries {base}/{package-lower}/index.json, optionally filters out pre-release versions, selects the newest,
and returns a compact object { version, source } or JSON.

.PARAMETER PackageId
NuGet package ID. Default: Microsoft.Dynamics.BusinessCentral.Development.Tools

.PARAMETER IncludePrerelease
Boolean switch to include prerelease versions when determining latest. Default: $true

.PARAMETER BaseUrl
Flat container base URL. Default: https://api.nuget.org/v3-flatcontainer

.PARAMETER TimeoutSec
HTTP timeout per attempt. Default: 20

.PARAMETER Retries
Number of retry attempts on transient failures. Default: 3

#>

[CmdletBinding()]
param(
    [string]$PackageId = 'Microsoft.Dynamics.BusinessCentral.Development.Tools',
    [bool]$IncludePrerelease = $true,
    [string]$BaseUrl = 'https://api.nuget.org/v3-flatcontainer',
    [int]$TimeoutSec = 20,
    [int]$Retries = 3
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($PackageId)) {
    throw 'PackageId is required.'
}
if ($Retries -lt 1) {
    $Retries = 1
}

$pkgLower = $PackageId.ToLowerInvariant().Trim()
$base = $BaseUrl.TrimEnd('/')
$indexUrl = "$base/$pkgLower/index.json"

# Basic retry on transient errors
$idx = $null
$attempt = 0
$lastErr = $null
$headers = @{
    'User-Agent' = "PowerShell/$($PSVersionTable.PSVersion) NuGetIndexFetcher/1.0 ($env:COMPUTERNAME)"
}

while (-not $idx -and $attempt -lt $Retries) {
    try {
        $attempt++
        $idx = Invoke-RestMethod -Uri $indexUrl -Method GET -Headers $headers -TimeoutSec $TimeoutSec
    }
    catch {
        $lastErr = $_
        if ($attempt -ge $Retries) {
            throw "Failed to query NuGet index for '$PackageId' after $attempt attempt(s). $($_.Exception.Message)"
        }
        Start-Sleep -Seconds ([Math]::Min(2 * $attempt, 5))
    }
}

if (-not $idx -or -not $idx.versions) {
    throw "NuGet index did not return a versions list for '$PackageId'."
}

$versions = @($idx.versions)

if (-not $IncludePrerelease) {
    # filter out versions that contain a hyphen (SemVer pre-release)
    $versions = $versions | Where-Object { $_ -notmatch '-' }
}

if (-not $versions -or $versions.Count -eq 0) {
    if ($IncludePrerelease) {
        throw "No versions found for '$PackageId'."
    }
    else {
        throw "No stable versions found for '$PackageId'. Consider -IncludePrerelease."
    }
}

$results =
foreach ($v in $versions) {
    [pscustomobject]@{
        version  = $v
        assetUri = "$base/$pkgLower/$v/$pkgLower.$v.nupkg"
    }
}

Write-Output $results | ConvertTo-Json -Compress
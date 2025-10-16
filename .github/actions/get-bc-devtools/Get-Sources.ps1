<#
.SYNOPSIS
Merge VSIX (Marketplace) and NuGet package sources into a single JSON list.

.DESCRIPTION
Executes two helper scripts (Marketplace.ps1 and NuGet-Packages.ps1) that output JSON.
Normalizes to objects with: version, uri, type. Emits compressed JSON to STDOUT.

#>

$ErrorActionPreference = 'Stop'

function Get-TargetFrameworkCache {
    param(
        [string]$JsonPath = "$PSScriptRoot\TargetFramework.json"
    )
    
    if (Test-Path $JsonPath) {
        try {
            $jsonContent = Get-Content $JsonPath -Raw | ConvertFrom-Json
            # Create a hashtable for quick lookup by PackageType and PackageVersion combination
            $lookup = @{}
            foreach ($item in $jsonContent) {
                $key = "$($item.PackageType):$($item.PackageVersion)"
                $lookup[$key] = @{
                    Version         = $item.Version
                    TargetFramework = $item.TargetFramework
                }
            }
            return $lookup
        }
        catch {
            Write-Warning "Failed to read or parse TargetFramework.json: $($_.Exception.Message)"
            return @{}
        }
    }
    else {
        return @{}
    }
}

# Read TargetFramework data for lookup
$targetFrameworkCache = Get-TargetFrameworkCache

$marketplace = & "$PSScriptRoot/Marketplace.ps1" | ConvertFrom-Json

$marketplaceLatest = $marketplace `
| Where-Object properties -ne $null `
| Where-Object { $_.properties.key -notcontains 'Microsoft.VisualStudio.Code.PreRelease' } | Select-Object -First 1 -ExpandProperty version

$marketplacePreview = $marketplace `
| Where-Object properties -ne $null `
| Where-Object { $_.properties.key -contains 'Microsoft.VisualStudio.Code.PreRelease' } | Select-Object -First 1 -ExpandProperty version

$vsixSources = 
foreach ($item in $marketplace) {

    $VSIXPackage = $item.files | Where-Object { $_.assetType -eq "Microsoft.VisualStudio.Services.VSIXPackage" }

    # Get Version and TargetFramework from JSON data if available using PackageType and PackageVersion
    $lookupKey = "VSIX:$($item.version)"
    $frameworkInfo = $targetFrameworkCache[$lookupKey]
    $frameworkInfoVersion = if ($frameworkInfo) { $frameworkInfo.Version } else { "" }
    $frameworkInfoTFM = if ($frameworkInfo) { $frameworkInfo.TargetFramework } else { "" }

    [PSCustomObject]@{
        version        = $frameworkInfoVersion
        packageType    = 'VSIX'
        packageVersion = $item.version
        isLatest       = ($item.version -eq $marketplaceLatest)
        isPreview      = ($item.version -eq $marketplacePreview)
        uri            = $VSIXPackage.source
        tfm            = $frameworkInfoTFM
    }
}

$nupkgs = & "$PSScriptRoot/NuGet-Packages.ps1" | ConvertFrom-Json

$nugetLatest =
$nupkgs |
Where-Object { $_.version -and ($_.version -notmatch '-') } |
Sort-Object { [version](($_.version -split '-')[0]) } -Descending |
Select-Object -First 1 -ExpandProperty version

$nugetPreview =
$nupkgs |
Where-Object { $_.version -and ($_.version -match '-') } |
Sort-Object { [version](($_.version -split '-')[0]) } -Descending |
Select-Object -First 1 -ExpandProperty version

$nugetSources =
foreach ($item in $nupkgs) {
    # Get Version and TargetFramework from JSON data if available using PackageType and PackageVersion
    $lookupKey = "NuGet:$($item.version)"
    $frameworkInfo = $targetFrameworkCache[$lookupKey]
    $frameworkInfoVersion = if ($frameworkInfo) { $frameworkInfo.Version } else { "" }
    $frameworkInfoTFM = if ($frameworkInfo) { $frameworkInfo.TargetFramework } else { "" }

    [PSCustomObject]@{
        version        = $frameworkInfoVersion
        packageType    = 'NuGet'
        packageVersion = $item.version
        isLatest       = ($item.version -eq $nugetLatest)
        isPreview      = ($item.version -eq $nugetPreview)
        uri            = $item.assetUri
        tfm            = $frameworkInfoTFM
    }
}

# TODO: Combine, drop nulls and de-duplicate
$sources = @($vsixSources + $nugetSources) 

# Best-effort version sorting (with fall back to string compare)
$sources = $sources | Sort-Object { 
    # Remove everything after the first dash for sorting
    $baseVersion = $_.version -split '-' | Select-Object -First 1
    try {
        [System.Version]$baseVersion
    }
    catch {
        # If parsing fails, use original string
        $_.version
    }
} -Descending

# Emit compact JSON to STDOUT
Write-Output $sources | ConvertTo-Json -Compress
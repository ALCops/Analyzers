#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Downloads pinned BC Development Tools and extracts them to TFM-specific subfolders.

.DESCRIPTION
    Fetches three pinned versions of Microsoft.Dynamics.BusinessCentral.Development.Tools
    from VS Marketplace (VSIX) and NuGet, then extracts the Analyzers DLLs to:
    <RepoRoot>/Microsoft.Dynamics.BusinessCentral.Development.Tools/<TFM>/

.NOTES
    Requires: PowerShell 7+ and System.IO.Compression.FileSystem
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression.FileSystem

# Pinned versions
$sources = @(
    @{
        AssemblyVersion = '12.0.13.24028'
        PackageVersion  = '12.0.875970'
        TFM             = 'netstandard2.1'
        Type            = 'VSIX'
        PathInArchive   = 'extension/bin/Analyzers'
    }
    @{
        AssemblyVersion = '16.0.27.57058'
        PackageVersion  = '16.0.1826476'
        TFM             = 'net8.0'
        Type            = 'VSIX'
        PathInArchive   = 'extension/bin/Analyzers'
    }
    @{
        AssemblyVersion = '18.0.41.39415'
        PackageVersion  = '18.0.41.39415'
        TFM             = 'net10.0'
        Type            = 'NuGet'
        PathInArchive   = 'tools/net10.0/any'
    }
)

function Get-DownloadUrl {
    param([hashtable]$Source)

    switch ($Source.Type) {
        'VSIX' {
            return "https://ms-dynamics-smb.gallery.vsassets.io/_apis/public/gallery/publisher/ms-dynamics-smb/extension/al/$($Source.PackageVersion)/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"
        }
        'NuGet' {
            $pkgLower = 'microsoft.dynamics.businesscentral.development.tools'
            $version = $Source.PackageVersion
            return "https://api.nuget.org/v3-flatcontainer/$pkgLower/$version/$pkgLower.$version.nupkg"
        }
        default { throw "Unknown source type: $($Source.Type)" }
    }
}

function Extract-ArchiveSubfolder {
    param(
        [string]$ArchivePath,
        [string]$PathInArchive,
        [string]$DestinationPath
    )

    # Clean and recreate destination
    if (Test-Path $DestinationPath) {
        Remove-Item -Path $DestinationPath -Recurse -Force
    }
    New-Item -ItemType Directory -Path $DestinationPath -Force | Out-Null

    # Normalize to ZIP forward-slash form with trailing slash
    $norm = ($PathInArchive -replace '\\', '/').TrimStart('/')
    if ($norm.Length -gt 0 -and -not $norm.EndsWith('/')) { $norm += '/' }

    $archive = [System.IO.Compression.ZipFile]::OpenRead($ArchivePath)
    try {
        $matchingEntries = $archive.Entries | Where-Object {
            ($_.FullName -replace '\\', '/').StartsWith($norm, [StringComparison]::OrdinalIgnoreCase)
        }

        if (-not $matchingEntries) {
            throw "Path '$PathInArchive' not found in archive '$ArchivePath'."
        }

        foreach ($entry in $matchingEntries) {
            if ([string]::IsNullOrEmpty($entry.Name)) { continue }

            $full = ($entry.FullName -replace '\\', '/')
            $rel = $full.Substring($norm.Length)
            if ($rel.Contains('..')) { continue }

            $destPath = Join-Path $DestinationPath ($rel -replace '/', [IO.Path]::DirectorySeparatorChar)
            $destDir = Split-Path $destPath -Parent
            if (-not (Test-Path $destDir)) {
                New-Item -ItemType Directory -Path $destDir -Force | Out-Null
            }

            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destPath, $true)
        }
    }
    finally {
        $archive.Dispose()
    }
}

# Resolve paths
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..') | Select-Object -ExpandProperty Path
$targetRoot = Join-Path $repoRoot 'Microsoft.Dynamics.BusinessCentral.Development.Tools'

Write-Host "Target root: $targetRoot" -ForegroundColor Cyan
Write-Host ""

foreach ($source in $sources) {
    $tfm = $source.TFM
    $url = Get-DownloadUrl -Source $source
    $destPath = Join-Path $targetRoot $tfm

    Write-Host "[$tfm] Downloading $($source.Type) v$($source.AssemblyVersion) (package $($source.PackageVersion))..." -ForegroundColor Yellow

    # Download to temp
    $tempFile = Join-Path ([IO.Path]::GetTempPath()) "bcdevtools_${tfm}_$([Guid]::NewGuid().ToString('N').Substring(0,8)).zip"
    try {
        Invoke-WebRequest -Uri $url -OutFile $tempFile -UseBasicParsing

        Write-Host "[$tfm] Extracting to $destPath..." -ForegroundColor Yellow
        Extract-ArchiveSubfolder -ArchivePath $tempFile -PathInArchive $source.PathInArchive -DestinationPath $destPath

        $fileCount = (Get-ChildItem -Path $destPath -Recurse -File).Count
        Write-Host "[$tfm] Done ($fileCount files extracted)" -ForegroundColor Green
    }
    finally {
        Remove-Item -Path $tempFile -Force -ErrorAction SilentlyContinue
    }

    Write-Host ""
}

Write-Host "All BC DevTools dependencies set up successfully." -ForegroundColor Green

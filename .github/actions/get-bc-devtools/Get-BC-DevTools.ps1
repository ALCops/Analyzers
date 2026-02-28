<#
.SYNOPSIS
Get BC DevTools sources with TargetFramework and AssemblyVersion analysis.

.DESCRIPTION
This script analyzes BC DevTools sources for TargetFramework and AssemblyVersion information by:
1. Reading existing analysis from TargetFramework.json
2. Getting all sources from Get-Sources.ps1
3. Processing any missing versions by downloading and analyzing assemblies
4. Updating the TargetFramework.json file
5. Outputting enhanced BC DevTools sources JSON with TargetFramework and AssemblyVersion data

This is the main script used by the get-bc-devtools GitHub action.

.PARAMETER MaxVersions
Maximum number of versions to analyze in this run. Default: 100

.PARAMETER JsonPath
Path to the TargetFramework.json file. Default: TargetFramework.json in script directory
#>

[CmdletBinding()]
param(
    [int]$MaxVersions = 100,
    [string]$JsonPath = "$PSScriptRoot\TargetFramework.json"
)

$ErrorActionPreference = 'Stop'

# Import required modules
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Read-TargetFrameworkJson {
    param(
        [string]$JsonPath
    )
    
    if (Test-Path $JsonPath) {
        Write-Host "Reading existing TargetFramework.json..." -ForegroundColor Yellow
        $jsonContent = Get-Content $JsonPath -Raw | ConvertFrom-Json
        Write-Host "Found $($jsonContent.Count) existing entries" -ForegroundColor Green
        return $jsonContent
    }
    else {
        Write-Host "No existing TargetFramework.json found, creating new..." -ForegroundColor Yellow
        return @()
    }
}

function Save-TargetFrameworkJson {
    param(
        [array]$Data,
        [string]$JsonPath
    )
    
    Write-Host "Updating TargetFramework.json with $($Data.Count) entries..." -ForegroundColor Yellow
    
    # Sort by version for better readability
    $sortedData = $Data | Sort-Object { [version]$_.Version } -Descending
    $jsonOutput = $sortedData | ConvertTo-Json -Depth 3
    $jsonOutput | Set-Content $JsonPath -Encoding UTF8
    
    Write-Host "TargetFramework.json updated successfully" -ForegroundColor Green
}

function Find-MissingVersions {
    param(
        [array]$ExistingData,
        [array]$AllSources
    )
    
    # Use composite key (PackageType:PackageVersion) to uniquely identify each cached entry.
    # Comparing on assembly version alone is unreliable: it is empty for uncached sources,
    # and two different packages (VSIX + NuGet) can share the same assembly version.
    $existingKeys = $ExistingData | ForEach-Object { "$($_.PackageType):$($_.PackageVersion)" }
    $missingVersions = @()
    
    foreach ($source in $AllSources) {
        $key = "$($source.packageType):$($source.packageVersion)"
        if ($key -notin $existingKeys) {
            $missingVersions += $source
        }
    }
    
    Write-Host "Found $($missingVersions.Count) missing versions to process" -ForegroundColor Yellow
    return $missingVersions
}

function Get-AssemblyInfo {
    param(
        [string]$AssemblyPath
    )
    
    try {
        # First try to use Cecil or other metadata readers if available, but fallback to reflection
        # Load assembly metadata without loading the assembly into the current AppDomain
        $bytes = [System.IO.File]::ReadAllBytes($AssemblyPath)
        
        try {
            # Try to load as reflection-only first
            $assembly = [System.Reflection.Assembly]::ReflectionOnlyLoad($bytes)
        }
        catch {
            # Fallback to regular load
            $assembly = [System.Reflection.Assembly]::Load($bytes)
        }
        
        # Get Assembly Version
        $assemblyVersion = $assembly.GetName().Version.ToString()
        
        # Try to get TargetFrameworkAttribute
        $customAttributes = $assembly.GetCustomAttributesData()
        $targetFrameworkAttr = $customAttributes | Where-Object { 
            $_.AttributeType.Name -eq 'TargetFrameworkAttribute' 
        }
        
        $targetFramework = "unknown"
        if ($targetFrameworkAttr) {
            $frameworkName = $targetFrameworkAttr.ConstructorArguments[0].Value
            
            # Parse the framework name to extract just the target framework moniker
            if ($frameworkName -match '\.NETStandard,Version=v(.+)') {
                $targetFramework = "netstandard$($matches[1])"
            }
            elseif ($frameworkName -match '\.NETCoreApp,Version=v(.+)') {
                $targetFramework = "net$($matches[1])"
            }
            elseif ($frameworkName -match '\.NETFramework,Version=v(.+)') {
                $version = $matches[1] -replace '\.', ''
                $targetFramework = "net$version"
            }
            else {
                # Return a cleaned version of the framework name
                $targetFramework = $frameworkName -replace '\.NET|,Version=v', '' -replace '\.', ''
            }
        }
        else {
            # Alternative: try to get from AssemblyMetadataAttribute
            $metadataAttrs = $customAttributes | Where-Object { 
                $_.AttributeType.Name -eq 'AssemblyMetadataAttribute' 
            }
            
            foreach ($attr in $metadataAttrs) {
                $key = $attr.ConstructorArguments[0].Value
                $value = $attr.ConstructorArguments[1].Value
                if ($key -eq 'TargetFramework') {
                    $targetFramework = $value
                    break
                }
            }
        }

        # Last-resort fallback: some assemblies (e.g. older BC BCArtifact builds) are compiled
        # without emitting TargetFrameworkAttribute at all. Infer the TFM from the referenced
        # assemblies instead — a reference to 'netstandard' reliably identifies netstandard2.0
        # builds, and a reference to 'System.Runtime' with no netstandard reference indicates net8.0+.
        if ($targetFramework -eq 'unknown') {
            $refNames = $assembly.GetReferencedAssemblies() | ForEach-Object { $_.Name }
            $netstdRef = $assembly.GetReferencedAssemblies() | Where-Object { $_.Name -eq 'netstandard' }
            if ($netstdRef) {
                $targetFramework = "netstandard$($netstdRef.Version.Major).$($netstdRef.Version.Minor)"
            }
            elseif ('System.Runtime' -in $refNames) {
                $targetFramework = 'net8.0'
            }
        }
        
        return [PSCustomObject]@{
            TargetFramework = $targetFramework
            Version         = $assemblyVersion
        }
    }
    catch {
        Write-Warning "Failed to analyze assembly '$AssemblyPath': $($_.Exception.Message)"
        return [PSCustomObject]@{
            TargetFramework = "analysis-error"
            Version         = "analysis-error"
        }
    }
}

function Get-AssetInfo {
    param(
        [PSCustomObject]$Source
    )
    
    $PackageType = $Source.PackageType
    $PackageVersion = $Source.PackageVersion
    $uri = $Source.uri
    
    Write-Host "Processing $PackageVersion ($PackageType)..." -ForegroundColor Yellow

    # Create version-specific temp directory
    $TempPath = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { [IO.Path]::GetTempPath() }
    $TempDirectory = Join-Path -Path $TempPath -ChildPath ("bcdevtools_{0}" -f ([Guid]::NewGuid().ToString('N')))
    if (Test-Path $TempDirectory) {
        Remove-Item $TempDirectory -Recurse -Force
    }
    New-Item -ItemType Directory -Path $TempDirectory -Force | Out-Null

    # BCArtifact: the core artifact ZIP contains ALLanguage.vsix in its root.
    # Step 1: HTTP Range-extract ALLanguage.vsix from the remote core ZIP (avoids full download).
    # Step 2: Extract Microsoft.Dynamics.Nav.Analyzers.Common.dll from the now-local VSIX
    #         (VSIX is itself a ZIP, but it is local so no nested HTTP range requests needed).
    # Step 3: Reflect on the DLL via Get-AssemblyInfo — identical to the VSIX/NuGet path,
    #         giving both the true AL Language assembly version and target framework moniker.
    if ($PackageType -eq 'BCArtifact') {
        $vsixPath = Join-Path $TempDirectory 'ALLanguage.vsix'
        $dllPath = Join-Path $TempDirectory 'Microsoft.Dynamics.Nav.Analyzers.Common.dll'
        try {
            # Step 1: HTTP Range-extract ALLanguage.vsix from the remote core ZIP
            $rangeScript = Join-Path (Split-Path $PSScriptRoot -Parent) 'shared\Get-RemoteZipEntry.ps1'
            & $rangeScript -Uri $uri -EntryPath 'ALLanguage.vsix' -OutputPath $vsixPath

            # Step 2: extract the Analyzers DLL from the now-local VSIX
            $vsixZip = [System.IO.Compression.ZipFile]::OpenRead($vsixPath)
            try {
                $dllEntry = $vsixZip.Entries | Where-Object {
                    $_.FullName -ieq 'extension/bin/Analyzers/Microsoft.Dynamics.Nav.Analyzers.Common.dll'
                } | Select-Object -First 1

                if (-not $dllEntry) {
                    throw "Microsoft.Dynamics.Nav.Analyzers.Common.dll not found inside ALLanguage.vsix."
                }

                [System.IO.Compression.ZipFileExtensions]::ExtractToFile($dllEntry, $dllPath, $true)
            }
            finally {
                $vsixZip.Dispose()
            }

            # Step 3: reflect on the DLL — returns both TargetFramework and Version (AL assembly version)
            return Get-AssemblyInfo -AssemblyPath $dllPath
        }
        catch {
            Write-Warning "  Failed to process BCArtifact for $PackageVersion`: $($_.Exception.Message)"
            return [PSCustomObject]@{ TargetFramework = 'error'; Version = 'error' }
        }
        finally {
            if (Test-Path $TempDirectory) {
                Remove-Item $TempDirectory -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }

    # Determine the archive-internal folder and the full entry path for the target DLL
    $pathInArchive = switch ($PackageType) {
        'VSIX' { 'extension/bin/Analyzers' }
        'NuGet' { 'tools/net8.0/any' }
        default { throw "Unknown asset type: $PackageType" }
    }
    $entryPath = "$pathInArchive/Microsoft.Dynamics.Nav.Analyzers.Common.dll"
    $dllPath = Join-Path $TempDirectory 'Microsoft.Dynamics.Nav.Analyzers.Common.dll'

    try {
        # HTTP Range Requests: download only the target DLL from the remote archive
        $rangeScript = Join-Path (Split-Path $PSScriptRoot -Parent) 'shared\Get-RemoteZipEntry.ps1'
        & $rangeScript -Uri $uri -EntryPath $entryPath -OutputPath $dllPath

        $assemblyInfo = Get-AssemblyInfo -AssemblyPath $dllPath
        return $assemblyInfo
    }
    catch {
        Write-Warning "  Failed to process $PackageVersion`: $($_.Exception.Message)"
        return [PSCustomObject]@{
            TargetFramework = "error"
            Version         = "error"
        }
    }
    finally {
        # Cleanup version temp directory
        if (Test-Path $TempDirectory) {
            Remove-Item $TempDirectory -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

# Main execution
Write-Host "BC DevTools TargetFramework Analysis" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green

try {
    # Step 1: Read existing TargetFramework.json
    $existingData = Read-TargetFrameworkJson -JsonPath $JsonPath
    
    # Step 2: Get all sources using Get-Sources.ps1
    Write-Host "Retrieving BC DevTools sources..." -ForegroundColor Yellow
    $sourcesJson = & "$PSScriptRoot\Get-Sources.ps1" -JsonPath $JsonPath
    $allSources = $sourcesJson | ConvertFrom-Json
    Write-Host "Found $($allSources.Count) total sources from BC DevTools" -ForegroundColor Green
    
    # Step 3: Compare and find missing versions
    $missingSources = Find-MissingVersions -ExistingData $existingData -AllSources $allSources
    
    if ($missingSources.Count -eq 0) {
        Write-Host "No missing versions found. TargetFramework.json is up to date!" -ForegroundColor Green
        
        # Emit the JSON to STDOUT for the get-bc-devtools action
        Write-Output $sourcesJson
        return
    }
    
    # Sort by PackageType then by version (descending) for predictable processing order
    $missingSources = $missingSources | Sort-Object `
    @{ Expression = { $_.packageType } }, `
    @{ Expression = { [version](($_.packageVersion -split '-')[0]) }; Descending = $true }

    # Limit the number of missing versions to process
    $sourcesToProcess = $missingSources | Select-Object -First $MaxVersions
    
    if ($sourcesToProcess.Count -lt $missingSources.Count) {
        Write-Host "Processing first $($sourcesToProcess.Count) of $($missingSources.Count) missing versions (limited by MaxVersions parameter)" -ForegroundColor Yellow
    }
    else {
        Write-Host "Processing all $($sourcesToProcess.Count) missing versions..." -ForegroundColor Yellow
    }
    
    # Step 4: Process missing versions
    $newResults = @()
    foreach ($source in $sourcesToProcess) {
        $assemblyInfo = Get-AssetInfo -Source $source

        $newEntry = [PSCustomObject]@{
            version         = $assemblyInfo.Version
            packageType     = $source.PackageType
            packageVersion  = $source.PackageVersion
            targetFramework = $assemblyInfo.TargetFramework
            beta            = [bool]$source.isBeta
        }
       
        $newResults += $newEntry
    }
    
    # Step 5a: Update TargetFramework.json
    $updatedData = @($existingData) + @($newResults)
    Save-TargetFrameworkJson -Data $updatedData -JsonPath $JsonPath
    
    # Step 5b: Output the updated sources as JSON (like the original Get-BC-DevTools.ps1)
    Write-Host "Retrieving updated BC DevTools sources with TargetFramework data..." -ForegroundColor Yellow
    $updatedSourcesJson = & "$PSScriptRoot\Get-Sources.ps1" -JsonPath $JsonPath
    
    # Emit the JSON to STDOUT for the get-bc-devtools action
    Write-Output $updatedSourcesJson
    
    Write-Host "Analysis complete! Processed $($newResults.Count) new versions." -ForegroundColor Green
}
catch {
    Write-Error "Analysis failed: $($_.Exception.Message)"
    throw
}
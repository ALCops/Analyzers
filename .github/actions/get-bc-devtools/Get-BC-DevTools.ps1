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
    
    $existingVersions = $ExistingData | ForEach-Object { $_.Version }
    $missingVersions = @()
    
    foreach ($source in $AllSources) {
        if ($source.version -notin $existingVersions) {
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
            
            # Final fallback: try to infer from runtime version
            if ($targetFramework -eq "unknown") {
                $runtimeVersion = $assembly.ImageRuntimeVersion
                if ($runtimeVersion -match '^v4\.') {
                    $targetFramework = "net472" # Common .NET Framework target
                }
                elseif ($runtimeVersion -match '^v2\.') {
                    $targetFramework = "net20"
                }
                else {
                    $targetFramework = "unknown-$runtimeVersion"
                }
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
    
    try {
        # Download the asset
        $fileName = if ($assetType -eq 'VSIX') { "$PackageVersion.vsix" } else { "$PackageVersion.nupkg" }
        $downloadPath = Join-Path $TempDirectory $fileName
        
        Write-Host "  Downloading from: $uri" -ForegroundColor Gray
        Invoke-WebRequest -Uri $uri -OutFile $downloadPath -TimeoutSec 60
        
        # Determine extraction path based on asset type
        $pathInArchive = switch ($PackageType) {
            'VSIX' { 'extension/bin/Analyzers' }
            'NuGet' { 'tools/net8.0/any' }
            default { 
                throw "Unknown asset type: $PackageType"
            }
        }
        
        # Extract the required files
        $extractPath = Join-Path $TempDirectory 'extracted'
        $extractScript = Join-Path (Split-Path $PSScriptRoot -Parent) 'setup-bc-devtools\Extract-RequiredFiles.ps1'
        
        # Load the required assembly for the extraction script
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        & $extractScript -DestinationPath $extractPath -ArchivePath $downloadPath -PathInArchive $pathInArchive
        
        # Look for the target DLL
        $dllPath = Join-Path $extractPath 'Microsoft.Dynamics.Nav.CodeAnalysis.dll'
        
        if (Test-Path $dllPath) {
            Write-Host "  Found DLL, analyzing..." -ForegroundColor Green
            $assemblyInfo = Get-AssemblyInfo -AssemblyPath $dllPath
            return $assemblyInfo
        }
        else {
            Write-Warning "  Microsoft.Dynamics.Nav.CodeAnalysis.dll not found in extracted files"
            return [PSCustomObject]@{
                TargetFramework = "not-found"
                AssemblyVersion = "not-found"
            }
        }
    }
    catch {
        Write-Warning "  Failed to process $version`: $($_.Exception.Message)"
        return [PSCustomObject]@{
            TargetFramework = "error"
            AssemblyVersion = "error"
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
    $sourcesJson = & "$PSScriptRoot\Get-Sources.ps1"
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
            Version         = $assemblyInfo.Version
            PackageType     = $source.PackageType
            PackageVersion  = $source.PackageVersion
            TargetFramework = $assemblyInfo.TargetFramework
        }
       
        $newResults += $newEntry
    }
    
    # Step 5a: Update TargetFramework.json
    $updatedData = @($existingData) + @($newResults)
    Save-TargetFrameworkJson -Data $updatedData -JsonPath $JsonPath
    
    # Step 5b: Output the updated sources as JSON (like the original Get-BC-DevTools.ps1)
    Write-Host "Retrieving updated BC DevTools sources with TargetFramework data..." -ForegroundColor Yellow
    $updatedSourcesJson = & "$PSScriptRoot\Get-Sources.ps1"
    
    # Emit the JSON to STDOUT for the get-bc-devtools action
    Write-Output $updatedSourcesJson
    
    Write-Host "Analysis complete! Processed $($newResults.Count) new versions." -ForegroundColor Green
}
catch {
    Write-Error "Analysis failed: $($_.Exception.Message)"
    throw
}
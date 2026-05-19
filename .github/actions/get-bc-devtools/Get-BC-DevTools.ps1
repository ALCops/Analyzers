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
        # Use System.Reflection.Metadata to read PE metadata without loading the assembly
        # into the runtime. This avoids cross-runtime failures (e.g. net10.0 DLL on a net8.0 host).
        $stream = [System.IO.File]::OpenRead($AssemblyPath)
        try {
            $peReader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
            $mdReader = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($peReader)

            # Assembly version
            $asmDef = $mdReader.GetAssemblyDefinition()
            $assemblyVersion = $asmDef.Version.ToString()

            # Walk custom attributes on the assembly to find TargetFrameworkAttribute
            $targetFramework = 'unknown'
            foreach ($attrHandle in $asmDef.GetCustomAttributes()) {
                $attr = $mdReader.GetCustomAttribute($attrHandle)

                # Resolve the attribute type name
                $ctorHandle = $attr.Constructor
                $typeName = $null
                if ($ctorHandle.Kind -eq [System.Reflection.Metadata.HandleKind]::MemberReference) {
                    $memberRef = $mdReader.GetMemberReference([System.Reflection.Metadata.MemberReferenceHandle]$ctorHandle)
                    $parentHandle = $memberRef.Parent
                    if ($parentHandle.Kind -eq [System.Reflection.Metadata.HandleKind]::TypeReference) {
                        $typeRef = $mdReader.GetTypeReference([System.Reflection.Metadata.TypeReferenceHandle]$parentHandle)
                        $typeName = $mdReader.GetString($typeRef.Name)
                    }
                }

                if ($typeName -ne 'TargetFrameworkAttribute') { continue }

                # Decode the fixed-length string argument from the attribute blob
                $blobReader = $mdReader.GetBlobReader($attr.Value)
                $blobReader.ReadUInt16() | Out-Null   # skip prolog (0x0001)
                $frameworkName = $blobReader.ReadSerializedString()

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
                break
            }

            return [PSCustomObject]@{
                TargetFramework = $targetFramework
                Version         = $assemblyVersion
            }
        }
        finally {
            if ($peReader) { $peReader.Dispose() }
            $stream.Dispose()
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

function Remove-StaleEntries {
    param(
        [array]$ExistingData,
        [array]$AllSources
    )
    
    if ($AllSources.Count -eq 0) {
        Write-Host "Skipping stale-entry pruning: no active sources returned (upstream may be unavailable)" -ForegroundColor Yellow
        return $ExistingData
    }

    $activeKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($source in $AllSources) {
        [void]$activeKeys.Add("$($source.packageType):$($source.packageVersion)")
    }

    $pruned = @($ExistingData | Where-Object { $activeKeys.Contains("$($_.PackageType):$($_.PackageVersion)") })
    $removedCount = $ExistingData.Count - $pruned.Count

    if ($removedCount -gt 0) {
        Write-Host "Pruned $removedCount stale entries from TargetFramework.json" -ForegroundColor Yellow
    }

    return $pruned
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

    # Determine the archive-internal folder and the full entry path for the target DLL.
    # For NuGet packages that ship multiple TFMs, we want the highest.
    # Order highest-first so the first successful extraction wins.
    $nugetTfmPaths = @('tools/net10.0/any', 'tools/net8.0/any')
    $pathInArchive = switch ($PackageType) {
        'VSIX' { 'extension/bin/Analyzers' }
        'NuGet' { $nugetTfmPaths[0] }
        default { throw "Unknown asset type: $PackageType" }
    }
    $dllPath = Join-Path $TempDirectory 'Microsoft.Dynamics.Nav.Analyzers.Common.dll'

    try {
        $rangeScript = Join-Path (Split-Path $PSScriptRoot -Parent) 'shared\Get-RemoteZipEntry.ps1'

        if ($PackageType -eq 'NuGet') {
            # Try each known NuGet TFM path until one succeeds
            $extracted = $false
            foreach ($tfmPath in $nugetTfmPaths) {
                $entryPath = "$tfmPath/Microsoft.Dynamics.Nav.Analyzers.Common.dll"
                try {
                    & $rangeScript -Uri $uri -EntryPath $entryPath -OutputPath $dllPath
                    $extracted = $true
                    break
                }
                catch {
                    # Entry not found at this TFM path, try next
                }
            }
            if (-not $extracted) {
                throw "Microsoft.Dynamics.Nav.Analyzers.Common.dll not found in NuGet package at any known TFM path: $($nugetTfmPaths -join ', ')"
            }
        }
        else {
            $entryPath = "$pathInArchive/Microsoft.Dynamics.Nav.Analyzers.Common.dll"
            & $rangeScript -Uri $uri -EntryPath $entryPath -OutputPath $dllPath
        }

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
    
    # Step 3b: Prune stale entries no longer present in any source
    $preExistingCount = $existingData.Count
    $existingData = Remove-StaleEntries -ExistingData $existingData -AllSources $allSources
    $entriesWerePruned = $existingData.Count -lt $preExistingCount

    if ($missingSources.Count -eq 0 -and -not $entriesWerePruned) {
        Write-Host "No missing versions found. TargetFramework.json is up to date!" -ForegroundColor Green
        
        # Emit the JSON to STDOUT for the get-bc-devtools action
        Write-Output $sourcesJson
        return
    }

    if ($missingSources.Count -eq 0) {
        Write-Host "No missing versions found, but stale entries were pruned." -ForegroundColor Green
        Save-TargetFrameworkJson -Data $existingData -JsonPath $JsonPath
        
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
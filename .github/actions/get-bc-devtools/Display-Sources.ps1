<#
.SYNOPSIS
Renders a markdown-formatted table of BC DevTools sources to the workflow log.

.DESCRIPTION
Reads the SOURCES environment variable (stringified JSON produced by the get-bc-devtools action)
and writes a human-readable table to the GitHub Actions step log.
Intended to be called from action.yml after the sources have been resolved.
#>

$ErrorActionPreference = 'Stop'

$sources = $env:SOURCES | ConvertFrom-Json

Write-Host ""
Write-Host "### Microsoft Dynamics Business Central Development Tools Sources ###"
Write-Host ""

$tableData = $sources | ForEach-Object {
    $status = ""
    if ($_.isLatest -eq $true) { 
        $status = "Latest" 
    }
    elseif ($_.isPreview -eq $true) { 
        $status = "Pre-Release" 
    }
    
    [PSCustomObject]@{
        version         = $_.version
        status          = $status
        packageType     = $_.packageType
        packageVersion  = $_.packageVersion
        targetFramework = $_.tfm
        beta            = $_.isBeta
        source          = $_.uri
    }
}

# Calculate column widths
$versionWidth = [Math]::Max(($tableData.version | Measure-Object -Property Length -Maximum).Maximum, 7)
$statusWidth = [Math]::Max(($tableData.status  | Measure-Object -Property Length -Maximum).Maximum, 6)
$packageTypeWidth = [Math]::Max(($tableData.packageType    | Measure-Object -Property Length -Maximum).Maximum, 4)
$packageVersionWidth = [Math]::Max(($tableData.packageVersion | Measure-Object -Property Length -Maximum).Maximum, 15)
$targetFrameworkWidth = [Math]::Max(($tableData.targetFramework | Measure-Object -Property Length -Maximum).Maximum, 3)

$betaValues = $tableData | ForEach-Object { if ($_.beta -eq $true) { 'X' } else { '' } }
$betaWidth = [Math]::Max(($betaValues | Measure-Object -Property Length -Maximum).Maximum, 4)

$sourceWidth = 60

$header = "| {0,-$versionWidth} | {1,-$statusWidth} | {2,-$packageTypeWidth} | {3,-$packageVersionWidth} | {4,-$targetFrameworkWidth} | {5,-$betaWidth} | {6,-$sourceWidth} |" -f "Version", "Status", "Type", "Package Version", "TFM", "Beta", "Source"
$separator = "|{0}|{1}|{2}|{3}|{4}|{5}|{6}|" -f ("-" * ($versionWidth + 2)), ("-" * ($statusWidth + 2)), ("-" * ($packageTypeWidth + 2)), ("-" * ($packageVersionWidth + 2)), ("-" * ($targetFrameworkWidth + 2)), ("-" * ($betaWidth + 2)), ("-" * ($sourceWidth + 2))

Write-Host ""
Write-Host $header
Write-Host $separator

foreach ($item in $tableData) {
    $truncatedSource = if ($item.source.Length -gt $sourceWidth) { 
        "..." + $item.source.Substring($item.source.Length - ($sourceWidth - 3))
    }
    else { 
        $item.source 
    }
    
    $betaChar = if ($item.beta -eq $true) { 'X' } else { '' }
    $row = "| {0,-$versionWidth} | {1,-$statusWidth} | {2,-$packageTypeWidth} | {3,-$packageVersionWidth} | {4,-$targetFrameworkWidth} | {5,-$betaWidth} | {6,-$sourceWidth} |" -f $item.version, $item.status, $item.packageType, $item.packageVersion, $item.targetFramework, $betaChar, $truncatedSource
    Write-Host $row
}

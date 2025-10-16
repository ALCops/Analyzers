<# 
.SYNOPSIS
Query the VS Marketplace and return the VSIX download URL(s) for a VS Code extension.

.PARAMETER Publisher
Marketplace publisher short name. Default: ms-dynamics-smb

.PARAMETER Extension
Extension short name. Default: al
#>

[CmdletBinding()]
param(
    [string]$Publisher = 'ms-dynamics-smb',
    [string]$Extension = 'al'
)

$ErrorActionPreference = 'Stop'

function Invoke-MarketplaceQuery {
    param(
        [Parameter(Mandatory)]
        [string]$Publisher,
        [Parameter(Mandatory)]
        [string]$Extension
    )

    # Build the extension identifier used in filterType 7
    $extensionId = "$Publisher.$Extension"

    # Request payload (matches your sample; flags=147 required to get versions/files)
    $payload = @{
        filters    = @(
            @{
                criteria   = @(
                    # 7 = ExtensionName
                    @{ filterType = 7; value = $extensionId }                  # Publisher.Extension

                    # 8 = Target
                    @{ filterType = 8; value = 'Microsoft.VisualStudio.Code' } # Target (Microsoft.VisualStudio.Code)
                    
                    # 12 = ExcludeWithFlags
                    @{ filterType = 12; value = '4096' }                       # Exclude Unpublished (4096)
                )
                pageNumber = 1
                pageSize   = 100
                sortBy     = 0
                sortOrder  = 0
            }
        )
        assetTypes = @( "Microsoft.VisualStudio.Services.VSIXPackage" )
        flags      = 147 # IncludeVersions, IncludeFiles, IncludeVersionProperties, IncludeAssetUri, see https://github.com/microsoft/vscode/blob/12ae331012923024bedaf873ba4259a8c64db020/src/vs/platform/extensionManagement/common/extensionGalleryService.ts#L86
    }

    $uri = 'https://marketplace.visualstudio.com/_apis/public/gallery/extensionquery?api-version=3.0-preview.1'

    $response = Invoke-RestMethod -Method POST -Uri $uri -ContentType 'application/json' -Body ($payload | ConvertTo-Json -Depth 10)

    if (-not $response.results -or -not $response.results[0].extensions) {
        throw "No extensions returned for '$extensionId'."
    }

    $extensions = $response.results[0].extensions |
    Where-Object { $_.extensionName -eq $Extension -and $_.publisher.publisherName -eq $Publisher } |
    Select-Object -First 1

    if (-not $extensions) { 
        throw "Extension '$extensionId' not found."
    }
    if (-not $extensions.versions) {
        throw "No versions listed for '$extensionId'." 
    }

    return $extensions
}

function ConvertTo-Version {
    [OutputType([System.Version])]
    Param (
        [Parameter(Mandatory = $true)]
        [string] $version
    ) 
    
    $result = $null
    if ([System.Version]::TryParse($version, [ref]$result)) {
        return $result
    }
    else {
        Write-Error "The value '$($version)' is not a valid input."
    }
}

# Query the marketplace
$marketplace = Invoke-MarketplaceQuery -Publisher $Publisher -Extension $Extension

# Hardcoded filter for minimum AL Language version 12.x
$marketplaceFiltered = $marketplace.versions | Where-Object { $(ConvertTo-Version($_.version)) -ge [System.Version]::Parse("12.0.0") }

# Find current release version
$currentRelease = $marketplaceFiltered `
| Where-Object properties -ne $null `
| Where-Object { $_.properties.key -notcontains 'Microsoft.VisualStudio.Code.PreRelease' } `
| Select-Object -First 1 -ExpandProperty version

# Remove pre-release versions lower than current release
$marketplaceFiltered = $marketplaceFiltered `
| Where-Object { 
    $version = ConvertTo-Version($_.version)
    $preRelease = $_.properties | Where-Object { $_.key -eq 'Microsoft.VisualStudio.Code.PreRelease' }

    if ($preRelease -and $version -lt (ConvertTo-Version $currentRelease)) {
        return $false
    }
    return $true
}

Write-Output $marketplaceFiltered | ConvertTo-Json -Compress -Depth 3
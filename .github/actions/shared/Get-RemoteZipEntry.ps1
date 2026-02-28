<#
.SYNOPSIS
Extracts a single file from a remote ZIP archive using HTTP Range Requests.

.DESCRIPTION
Avoids downloading the entire archive by:
  1. Issuing a HEAD request to obtain Content-Length.
  2. Fetching the last 65,536 bytes to locate the End of Central Directory (EOCD).
  3. Fetching only the Central Directory to find the target entry's metadata.
  4. Fetching the Local File Header and compressed data for that entry only.
  5. Decompressing (Deflate/method 8, or Stored/method 0) and writing to OutputPath.

Uses System.Net.HttpWebRequest with .AddRange() (not Invoke-WebRequest), which
preserves the Range header across redirects. This is critical for CDNs like the
VS Code Marketplace (gallerycdn.vsassets.io) that do not advertise Accept-Ranges
in HEAD responses but honour range requests on GET.

Supports both ZIP32 and ZIP64 archives.

.PARAMETER Uri
The remote URL of the ZIP/VSIX/nupkg file.

.PARAMETER EntryPath
The full path of the entry to extract, using forward slashes.
Example: 'extension/bin/Analyzers/Microsoft.Dynamics.Nav.CodeAnalysis.dll'

.PARAMETER OutputPath
Local file path where the decompressed entry will be written.

.EXAMPLE
Get-RemoteZipEntry.ps1 `
    -Uri 'https://example.com/package.vsix' `
    -EntryPath 'extension/bin/Analyzers/Microsoft.Dynamics.Nav.CodeAnalysis.dll' `
    -OutputPath 'C:\Temp\Microsoft.Dynamics.Nav.CodeAnalysis.dll'
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Uri,

    [Parameter(Mandatory = $true)]
    [string]$EntryPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$script:TotalBytesDownloaded = 0

# ===== HTTP helpers using HttpWebRequest (preserves Range across redirects) =====

function Get-RemoteFileSize {
    param([string]$Url)
    $request = [System.Net.HttpWebRequest]::Create($Url)
    $request.Method = 'HEAD'
    $request.AllowAutoRedirect = $true
    $request.Timeout = 60000
    $response = $request.GetResponse()
    $size = $response.ContentLength
    $response.Close()
    if ($size -le 0) { throw "Server did not return Content-Length for HEAD request." }
    return [long]$size
}

function Get-RemoteBytes {
    <#
    .SYNOPSIS
    Download a byte range from a remote URL with retry logic.
    #>
    param(
        [string]$Url,
        [long]$Start,
        [long]$Length,
        [int]$MaxRetries = 3
    )

    Write-Verbose "    Range GET bytes=${Start}-$($Start + $Length - 1) ($([math]::Round($Length / 1KB, 1)) KB)..."

    for ($attempt = 1; $attempt -le $MaxRetries; $attempt++) {
        try {
            $request = [System.Net.HttpWebRequest]::Create($Url)
            $request.Method = 'GET'
            $request.AddRange([long]$Start, [long]($Start + $Length - 1))
            $request.AllowAutoRedirect = $true
            # Escalating timeouts: 15 s, 30 s, 60 s
            $timeoutMs = @(15000, 30000, 60000)[$attempt - 1]
            $request.Timeout = $timeoutMs
            $request.ReadWriteTimeout = $timeoutMs

            $response = $request.GetResponse()
            $stream = $response.GetResponseStream()
            $ms = [System.IO.MemoryStream]::new()
            $stream.CopyTo($ms)
            $stream.Close()
            $response.Close()

            $bytes = $ms.ToArray()
            $ms.Close()
            $script:TotalBytesDownloaded += $bytes.Length

            # Detect server ignoring the Range header (returns full file)
            if ($bytes.Length -gt ($Length * 1.1 + 4096)) {
                throw "Server returned $($bytes.Length) bytes instead of the requested $Length. Range requests may not be supported."
            }

            return , $bytes
        }
        catch {
            if ($attempt -lt $MaxRetries) {
                $wait = @(5, 10)[($attempt - 1)]
                Write-Warning "    Attempt $attempt/$MaxRetries failed: $($_.Exception.Message). Retrying in ${wait}s..."
                Start-Sleep -Seconds $wait
            }
            else {
                throw
            }
        }
    }
}

# ===== Binary-read helpers =====

function Read-UInt16 { param([byte[]]$Bytes, [int]$Offset); [System.BitConverter]::ToUInt16($Bytes, $Offset) }
function Read-UInt32 { param([byte[]]$Bytes, [int]$Offset); [System.BitConverter]::ToUInt32($Bytes, $Offset) }
function Read-UInt64 { param([byte[]]$Bytes, [int]$Offset); [System.BitConverter]::ToUInt64($Bytes, $Offset) }

function Get-ByteSlice {
    param([byte[]]$Data, [long]$Offset, [long]$Length)
    $result = [byte[]]::new($Length)
    [System.Buffer]::BlockCopy($Data, $Offset, $result, 0, $Length)
    return , $result
}

# ===== ZIP parsing =====

function Find-CentralDirectory {
    <#
    .SYNOPSIS
    Locate the Central Directory using the EOCD (ZIP32) or ZIP64 EOCD locator.
    Returns a hashtable with Offset and Size (both [long]).
    #>
    param([byte[]]$EocdData, [long]$ZipSize, [long]$EocdReadSize)

    # Try ZIP64 EOCD first (signature 0x06064b50)
    for ($i = $EocdData.Length - 56; $i -ge 0; $i--) {
        if ((Read-UInt32 $EocdData $i) -eq 0x06064b50) {
            $cdSize = [long](Read-UInt64 $EocdData ($i + 40))
            $cdStart = [long](Read-UInt64 $EocdData ($i + 48))
            Write-Verbose "  [RangeRequest] ZIP64 EOCD found: CD offset=$cdStart, CD size=$cdSize"
            return @{ Offset = $cdStart; Size = $cdSize }
        }
    }

    # Regular EOCD (signature 0x06054b50)
    for ($i = $EocdData.Length - 22; $i -ge 0; $i--) {
        if ((Read-UInt32 $EocdData $i) -eq 0x06054b50) {
            $cdSize = [long](Read-UInt32 $EocdData ($i + 12))
            $cdStart = [long](Read-UInt32 $EocdData ($i + 16))
            Write-Verbose "  [RangeRequest] EOCD found: CD offset=$cdStart, CD size=$cdSize"
            return @{ Offset = $cdStart; Size = $cdSize }
        }
    }

    throw "Cannot find ZIP End of Central Directory record in the last $EocdReadSize bytes."
}

function Get-CentralDirectoryEntries {
    <#
    .SYNOPSIS
    Parse Central Directory bytes into an array of entry objects.
    Handles ZIP64 extra fields for large archives.
    #>
    param([byte[]]$CdBytes)

    $entries = [System.Collections.Generic.List[PSObject]]::new()
    $offset = 0

    while ($offset + 46 -le $CdBytes.Length) {
        if ((Read-UInt32 $CdBytes $offset) -ne 0x02014b50) { break }

        $method = Read-UInt16 $CdBytes ($offset + 10)
        $compSize = [long](Read-UInt32 $CdBytes ($offset + 20))
        $uncompSize = [long](Read-UInt32 $CdBytes ($offset + 24))
        $fnLen = [int](Read-UInt16 $CdBytes ($offset + 28))
        $extraLen = [int](Read-UInt16 $CdBytes ($offset + 30))
        $commentLen = [int](Read-UInt16 $CdBytes ($offset + 32))
        $headerOff = [long](Read-UInt32 $CdBytes ($offset + 42))

        $filename = [System.Text.Encoding]::UTF8.GetString(
            (Get-ByteSlice $CdBytes ($offset + 46) $fnLen))

        # ZIP64 extra field (tag 0x0001): override sizes and offset if 0xFFFFFFFF
        $extraStart = $offset + 46 + $fnLen
        if ($extraLen -gt 0 -and
            ($compSize -eq 0xFFFFFFFF -or $uncompSize -eq 0xFFFFFFFF -or $headerOff -eq 0xFFFFFFFF)) {
            $ei = $extraStart
            while ($ei + 4 -le $extraStart + $extraLen) {
                if ((Read-UInt16 $CdBytes $ei) -eq 0x0001) {
                    $z64p = $ei + 4
                    if ($uncompSize -eq 0xFFFFFFFF) { $uncompSize = [long](Read-UInt64 $CdBytes $z64p); $z64p += 8 }
                    if ($compSize -eq 0xFFFFFFFF) { $compSize = [long](Read-UInt64 $CdBytes $z64p); $z64p += 8 }
                    if ($headerOff -eq 0xFFFFFFFF) { $headerOff = [long](Read-UInt64 $CdBytes $z64p) }
                    break
                }
                $ei += 4 + (Read-UInt16 $CdBytes ($ei + 2))
            }
        }

        $entries.Add([PSCustomObject]@{
                Filename         = $filename
                Method           = [int]$method
                CompressedSize   = $compSize
                UncompressedSize = $uncompSize
                HeaderOffset     = $headerOff
            })

        $offset += 46 + $fnLen + $extraLen + $commentLen
    }

    return , $entries.ToArray()
}

# ===================================================================
# Main
# ===================================================================

Write-Verbose "  [RangeRequest] HEAD $Uri"

# Step 1: HEAD — get file size
$zipSize = Get-RemoteFileSize -Url $Uri
Write-Verbose "  [RangeRequest] Remote file: $([math]::Round($zipSize / 1MB, 2)) MB"

# Step 2: Read EOCD from the tail of the file
$eocdReadSize = [long][Math]::Min(65536, $zipSize)
$eocdData = Get-RemoteBytes -Url $Uri -Start ($zipSize - $eocdReadSize) -Length $eocdReadSize

$cd = Find-CentralDirectory -EocdData $eocdData -ZipSize $zipSize -EocdReadSize $eocdReadSize

# Step 3: Download the Central Directory (reuse if already in the EOCD buffer)
$cdOffset = $cd.Offset
$cdSize = $cd.Size

if ($cdOffset -ge ($zipSize - $eocdReadSize)) {
    # The CD was already included in the EOCD read — slice it out
    $sliceStart = [int]($eocdReadSize - ($zipSize - $cdOffset))
    $cdBytes = Get-ByteSlice $eocdData $sliceStart ([int]$cdSize)
    Write-Verbose "  [RangeRequest] Central Directory was in EOCD buffer (no extra request)"
}
else {
    $cdBytes = Get-RemoteBytes -Url $Uri -Start $cdOffset -Length $cdSize
}

# Step 4: Parse CD entries and find the target
$entries = Get-CentralDirectoryEntries -CdBytes $cdBytes
Write-Verbose "  [RangeRequest] $($entries.Count) entries in Central Directory"

$normalizedEntry = ($EntryPath -replace '\\', '/').TrimStart('/')
$target = $entries | Where-Object {
    ($_.Filename -replace '\\', '/') -ieq $normalizedEntry
} | Select-Object -First 1

if (-not $target) {
    throw "Entry '$normalizedEntry' not found in the Central Directory ($($entries.Count) entries scanned)."
}

Write-Verbose "  [RangeRequest] Found '$($target.Filename)' (method=$($target.Method), compressed=$([math]::Round($target.CompressedSize/1KB,1)) KB, offset=$($target.HeaderOffset))"

if ($target.Method -notin 0, 8) {
    throw "Unsupported compression method $($target.Method). Only Stored (0) and Deflate (8) are supported."
}

# Step 5: Download local file header + compressed data
$hdrData = Get-RemoteBytes -Url $Uri -Start $target.HeaderOffset -Length 30
if ((Read-UInt32 $hdrData 0) -ne 0x04034b50) {
    throw "Invalid local file header signature at offset $($target.HeaderOffset)."
}

$localFnLen = [int](Read-UInt16 $hdrData 26)
$localExtraLen = [int](Read-UInt16 $hdrData 28)
$dataOffset = $target.HeaderOffset + 30 + $localFnLen + $localExtraLen

$compressedData = Get-RemoteBytes -Url $Uri -Start $dataOffset -Length $target.CompressedSize

Write-Verbose "  [RangeRequest] Decompressing $([math]::Round($target.CompressedSize/1KB,1)) KB (method=$($target.Method))..."

# Step 6: Decompress and write
$destDir = Split-Path $OutputPath -Parent
if ($destDir -and -not (Test-Path $destDir)) {
    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
}

if ($target.Method -eq 0) {
    # Stored — already uncompressed
    [System.IO.File]::WriteAllBytes($OutputPath, $compressedData)
}
else {
    # Deflate (method 8)
    $inputStream = [System.IO.MemoryStream]::new($compressedData)
    $outputStream = [System.IO.FileStream]::new($OutputPath, [System.IO.FileMode]::Create)
    $deflate = [System.IO.Compression.DeflateStream]::new(
        $inputStream, [System.IO.Compression.CompressionMode]::Decompress)
    try {
        $deflate.CopyTo($outputStream)
    }
    finally {
        $deflate.Dispose()
        $outputStream.Dispose()
        $inputStream.Dispose()
    }
}

$outputSize = (Get-Item $OutputPath).Length
$totalKB = [math]::Round($script:TotalBytesDownloaded / 1KB, 1)
Write-Verbose "  [RangeRequest] Written '$($OutputPath | Split-Path -Leaf)' ($([math]::Round($outputSize/1KB, 1)) KB), total downloaded: $totalKB KB of $([math]::Round($zipSize/1MB,2)) MB"
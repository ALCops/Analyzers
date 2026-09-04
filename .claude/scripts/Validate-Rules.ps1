<#
.SYNOPSIS
Validates the .claude/rules/ tree: frontmatter, live path globs, required and forbidden rule-doc sections,
stale-fact patterns in the general guides, and link targets. Run before opening a PR that touches .claude/.

.EXAMPLE
pwsh .claude/scripts/Validate-Rules.ps1
Exit code 0 when clean, 1 when any finding is reported.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repo = (git rev-parse --show-toplevel).Trim()
Set-Location $repo
$tracked = @(git ls-files) | ForEach-Object { $_.Replace([char]92, [char]47) }
$findings = [System.Collections.Generic.List[object]]::new()
function Add-Finding([string]$Check, [string]$File, [string]$Detail) {
    $script:findings.Add([pscustomobject]@{ Check = $Check; File = $File; Detail = $Detail })
}
function Get-RelPath([string]$Full) { [IO.Path]::GetRelativePath($repo, $Full).Replace([char]92, [char]47) }

function ConvertTo-GlobRegex([string]$Glob) {
    $sb = [System.Text.StringBuilder]::new('^')
    $i = 0
    while ($i -lt $Glob.Length) {
        $c = $Glob[$i]
        if ($c -eq '*' -and $i + 1 -lt $Glob.Length -and $Glob[$i + 1] -eq '*') {
            if ($i + 2 -lt $Glob.Length -and $Glob[$i + 2] -eq '/') { [void]$sb.Append('(.*/)?'); $i += 3 } else { [void]$sb.Append('.*'); $i += 2 }
            continue
        }
        switch ($c) {
            '*' { [void]$sb.Append('[^/]*') }
            '?' { [void]$sb.Append('[^/]') }
            '{' { [void]$sb.Append('(') }
            '}' { [void]$sb.Append(')') }
            ',' { [void]$sb.Append('|') }
            default { [void]$sb.Append([regex]::Escape([string]$c)) }
        }
        $i++
    }
    [void]$sb.Append('$')
    return $sb.ToString()
}

$ruleFiles = Get-ChildItem -Path '.claude/rules' -Filter '*.md' -Recurse | Sort-Object FullName
$guides = $ruleFiles | Where-Object { $_.Directory.Name -eq 'rules' }
$ruleDocs = $ruleFiles | Where-Object { $_.Directory.Name -eq 'diagnostics' }

foreach ($f in $ruleFiles) {
    $rel = Get-RelPath $f.FullName
    $text = Get-Content -Raw -LiteralPath $f.FullName
    $lines = $text -split "`r?`n"

    # 1. frontmatter with paths
    if ($lines[0] -ne '---') { Add-Finding 'frontmatter' $rel 'file does not start with a --- frontmatter block'; continue }
    $end = [array]::IndexOf($lines, '---', 1)
    if ($end -lt 0) { Add-Finding 'frontmatter' $rel 'frontmatter block is not closed'; continue }
    $fm = $lines[1..($end - 1)]
    $globs = @($fm | Where-Object { $_ -match '^\s*-\s*"?([^"]+)"?\s*$' } | ForEach-Object { $Matches[1].Trim() })
    if (-not ($fm -match '^paths:')) { Add-Finding 'frontmatter' $rel 'no paths: entry (the file would load in every session)' }
    if ($globs.Count -eq 0) { Add-Finding 'frontmatter' $rel 'paths: has no globs' }

    # 2. every glob matches a tracked file
    foreach ($g in $globs) {
        $rx = ConvertTo-GlobRegex $g
        if (-not ($tracked | Where-Object { $_ -match $rx } | Select-Object -First 1)) { Add-Finding 'orphan-glob' $rel "no tracked file matches '$g'" }
    }

    # 5. link targets exist
    foreach ($m in [regex]::Matches($text, '\.claude/[A-Za-z0-9_./-]+\.md')) {
        if (-not (Test-Path -LiteralPath (Join-Path $repo $m.Value))) { Add-Finding 'broken-link' $rel "link target missing: $($m.Value)" }
    }
    foreach ($m in [regex]::Matches($text, '`([a-z0-9-]+\.md)`')) {
        $name = $m.Groups[1].Value
        $candidates = @("$repo/.claude/rules/$name", "$repo/.claude/rules/diagnostics/$name") + @(Get-ChildItem "$repo/.claude/skills" -Recurse -Filter $name | ForEach-Object FullName)
        if (-not ($candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1)) { Add-Finding 'broken-link' $rel "sibling reference has no file: $name" }
    }
}

# 3. rule docs: required and forbidden sections, test-folder glob
foreach ($f in $ruleDocs) {
    $rel = Get-RelPath $f.FullName
    $text = Get-Content -Raw -LiteralPath $f.FullName
    foreach ($h in '## Purpose', '## Design decisions') {
        if ($text -notmatch "(?m)^$([regex]::Escape($h))\s*$") { Add-Finding 'missing-section' $rel "no '$h' heading" }
    }
    foreach ($m in [regex]::Matches($text, '(?m)^## (Architecture|Roadmap|Phase 2|Relationship|Method classification|Receiver forms)[^\r\n]*')) {
        Add-Finding 'forbidden-section' $rel "heading '$($m.Value)' narrates code or defers work; move to code, a design row, or a GitHub issue"
    }
    if ($text -match '(?m)^## Known issues\s*\r?\n\s*\r?\n-\s*None') { Add-Finding 'empty-section' $rel "Known issues says 'None'; omit the section instead" }
    if ($text -notmatch '(?m)^\s*-\s*"?src/ALCops\.[A-Za-z]+\.Test/') { Add-Finding 'test-glob' $rel 'paths: has no test-folder glob (src/ALCops.{Cop}.Test/...)' }
}

# 4. general guides: no bare issue refs, no measurements, no time-bound wording
foreach ($f in $guides) {
    $rel = Get-RelPath $f.FullName
    $text = Get-Content -Raw -LiteralPath $f.FullName
    foreach ($m in [regex]::Matches($text, '(?<![\w/`])#\d{2,4}\b')) { Add-Finding 'issue-ref' $rel "bare issue reference '$($m.Value)'; general guides stay issue-free (rule docs and the regression catalog keep the links)" }
    foreach ($m in [regex]::Matches($text, '~?\b\d+(\.\d+)?\s?(ms|μs|us)\b')) { Add-Finding 'measurement' $rel "measurement '$($m.Value)' rots; state the cost qualitatively" }
    foreach ($m in [regex]::Matches($text, '(?i)\b(currently|should migrate|not yet implemented)\b')) { Add-Finding 'time-bound' $rel "'$($m.Value)' describes a moment, not a rule; describe the current state as fact" }
}

if ($findings.Count -eq 0) {
    Write-Host "OK: $($ruleFiles.Count) rules files ($($guides.Count) guides, $($ruleDocs.Count) rule docs) pass all checks."
    exit 0
}
$findings | Sort-Object Check, File | Format-Table -AutoSize -Wrap | Out-String -Width 200 | Write-Host
Write-Host "$($findings.Count) finding(s)."
exit 1

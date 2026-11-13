# Compare the vendored skills in .opencode/skills against their upstream SKILL.md.
#
# Every external skill carries an "Upstream source" footer with its Source and Registry
# URLs, so nothing has to be listed here: add a footer and the skill is audited too.
#
# What counts as a change is the *body*. The description line is ours to normalise (some
# upstream descriptions are broken), so a different description is reported but never
# treated as drift, and -Update keeps the local one.
#
# Usage (from the repo root):
#   powershell -File tools/skills-check.ps1              # report only
#   powershell -File tools/skills-check.ps1 -Update      # refresh the ones whose body changed
#
# Exit codes: 0 = every body matches, 1 = at least one body differs, 3 = a download failed.

param(
    [switch]$Update
)

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path $PSScriptRoot -Parent
$skillsRoot = Join-Path $projectRoot '.opencode\skills'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$today = (Get-Date).ToString('yyyy-MM-dd')

function Get-FooterValue([string] $footer, [string] $key) {
    $m = [regex]::Match($footer, "(?m)^- " + [regex]::Escape($key) + ":\s*(.+?)\s*$")
    if ($m.Success) { return $m.Groups[1].Value }
    return $null
}

function Get-RawUrl([string] $source) {
    # https://github.com/<owner>/<repo>/blob/<ref>/<path> -> raw.githubusercontent.com
    $m = [regex]::Match($source, '^https://github\.com/([^/]+/[^/]+)/blob/([^/]+)/(.+)$')
    if (-not $m.Success) { return $null }
    return "https://raw.githubusercontent.com/$($m.Groups[1].Value)/$($m.Groups[2].Value)/$($m.Groups[3].Value)"
}

# Frontmatter and body, with LF endings and no BOM.
function Split-Skill([string] $text) {
    $t = ($text -replace '^\uFEFF', '') -replace "`r`n", "`n"
    if ($t -match '(?s)^---\n(.*?)\n---\n(.*)$') {
        return @{ Frontmatter = $matches[1]; Body = $matches[2] }
    }
    return @{ Frontmatter = ''; Body = $t }
}

# Whitespace-insensitive, so re-wrapping or re-indenting upstream is not a change.
function Normalize([string] $text) {
    return (($text -split '\s+') | Where-Object { $_ -ne '' }) -join ' '
}

# The description key plus its folded continuation lines.
function Get-Description([string] $frontmatter) {
    $lines = $frontmatter -split "`n"
    $start = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^description:') { $start = $i; break }
    }
    if ($start -lt 0) { return $null }
    $block = @($lines[$start])
    for ($i = $start + 1; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^\s+\S' -or $lines[$i].Trim() -eq '') { $block += $lines[$i] }
        else { break }
    }
    return ($block -join "`n").TrimEnd()
}

# Upstream frontmatter with our description put back, so version bumps land but the
# discoverability line we wrote survives.
function Merge-Frontmatter([string] $upstreamFm, [string] $localFm) {
    $localDescription = Get-Description $localFm
    $upstreamDescription = Get-Description $upstreamFm
    if (-not $localDescription -or -not $upstreamDescription) { return $upstreamFm }

    $lines = $upstreamFm -split "`n"
    $out = New-Object System.Collections.Generic.List[string]
    $i = 0
    while ($i -lt $lines.Count) {
        if ($lines[$i] -match '^description:') {
            $out.Add($localDescription)
            $i++
            while ($i -lt $lines.Count -and ($lines[$i] -match '^\s+\S' -or $lines[$i].Trim() -eq '')) { $i++ }
            continue
        }
        $out.Add($lines[$i])
        $i++
    }
    return ($out -join "`n")
}

function Get-RegistryFacts([string] $registryUrl) {
    # Best effort: the page is Next.js, so the numbers are plain text in the HTML.
    $facts = @{ Installs = $null; Audits = $null }
    try { $page = (Invoke-WebRequest -Uri $registryUrl -UseBasicParsing -TimeoutSec 30).Content }
    catch { return $facts }

    $i = [regex]::Match($page, 'Installs\s*([0-9][0-9.,]*\s?[KMB]?)')
    if ($i.Success) { $facts.Installs = ($i.Groups[1].Value -replace '\s', '') }

    $auditBlock = [regex]::Match($page, 'Security Audits(.*?)(Browse all skills|All skills)')
    if ($auditBlock.Success) {
        $pairs = [regex]::Matches($auditBlock.Groups[1].Value, '(Gen Agent Trust Hub|Socket|Snyk)(Pass|Warn|Fail|Pending)')
        if ($pairs.Count -gt 0) {
            $facts.Audits = (($pairs | ForEach-Object {
                "$($_.Groups[1].Value) $($_.Groups[2].Value.ToUpper())"
            }) -join ', ')
        }
    }
    return $facts
}

function Get-UpstreamCommit([string] $ownerRepo, [string] $path) {
    $uri = "https://api.github.com/repos/$ownerRepo/commits?path=$path&per_page=1"
    $c = (Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 30 -Headers @{ 'User-Agent' = 'unity-game-skills-check' }).Content | ConvertFrom-Json
    if (-not $c -or $c.Count -eq 0) { return $null }
    return @{
        Sha  = ($c[0].sha).Substring(0, 10)
        Date = ([datetime]$c[0].commit.committer.date).ToString('yyyy-MM-dd')
    }
}

$skillFiles = Get-ChildItem $skillsRoot -Directory | ForEach-Object {
    $file = Join-Path $_.FullName 'SKILL.md'
    if (Test-Path $file) { $file }
}

$identical = 0
$changed = @()
$failed = @()

foreach ($file in $skillFiles) {
    $local = [System.IO.File]::ReadAllText($file, $utf8)
    $cut = $local.IndexOf('## Upstream source')
    if ($cut -lt 0) { continue }   # project skill: ours, nothing to compare

    $footer = $local.Substring($cut)
    $localText = $local.Substring(0, $cut) -replace '(?s)\s*---\s*$', ''
    $localParts = Split-Skill $localText

    $id = Split-Path (Split-Path $file -Parent) -Leaf
    $source = Get-FooterValue $footer 'Source'
    $registry = Get-FooterValue $footer 'Registry'
    $installs = Get-FooterValue $footer 'Installs when fetched'
    $audits = Get-FooterValue $footer 'Security audits on skills.sh'

    if (-not $source) {
        Write-Host ("  {0,-26} no Source line in the footer, skipped" -f $id) -ForegroundColor Yellow
        continue
    }
    $rawUrl = Get-RawUrl $source
    if (-not $rawUrl) {
        Write-Host ("  {0,-26} Source is not a github blob URL, skipped" -f $id) -ForegroundColor Yellow
        continue
    }

    try { $upstream = (Invoke-WebRequest -Uri $rawUrl -UseBasicParsing -TimeoutSec 45).Content }
    catch {
        $failed += $id
        Write-Host ("  {0,-26} DOWNLOAD FAILED: {1}" -f $id, $_.Exception.Message) -ForegroundColor Red
        continue
    }
    $upstreamParts = Split-Skill $upstream

    $bodyMatches = (Normalize $localParts.Body) -eq (Normalize $upstreamParts.Body)
    $frontmatterMatches = (Normalize $localParts.Frontmatter) -eq (Normalize $upstreamParts.Frontmatter)

    if ($bodyMatches -and $frontmatterMatches) {
        $identical++
        Write-Host ("  {0,-26} identical" -f $id) -ForegroundColor DarkGray
        continue
    }
    if ($bodyMatches) {
        $identical++
        Write-Host ("  {0,-26} identical body, local description kept" -f $id) -ForegroundColor DarkGray
        continue
    }

    $changed += $id
    $delta = (Normalize $upstreamParts.Body).Length - (Normalize $localParts.Body).Length
    Write-Host ("  {0,-26} CHANGED ({1:+#;-#;0} chars)" -f $id, $delta) -ForegroundColor Yellow

    if (-not $Update) { continue }

    # --- rewrite: upstream body, merged frontmatter, fresh provenance footer ---
    if (-not $upstreamParts.Frontmatter) {
        Write-Host ("    {0}: upstream has no frontmatter, not overwriting" -f $id) -ForegroundColor Red
        continue
    }
    if ($upstreamParts.Frontmatter -notmatch "(?m)^name:\s*$([regex]::Escape($id))\s*$") {
        Write-Host ("    {0}: upstream name does not match the folder, not overwriting" -f $id) -ForegroundColor Red
        continue
    }
    $frontmatter = Merge-Frontmatter $upstreamParts.Frontmatter $localParts.Frontmatter
    $body = $upstreamParts.Body.TrimEnd()

    # Never invent numbers: refresh them when the registry answers, keep the old ones when not.
    $facts = Get-RegistryFacts $registry
    if ($facts.Installs) { $installs = $facts.Installs }
    if ($facts.Audits) { $audits = $facts.Audits }

    $provenance = ''
    $m = [regex]::Match($source, '^https://github\.com/([^/]+/[^/]+)/blob/([^/]+)/(.+)$')
    if ($m.Success) {
        try {
            $commit = Get-UpstreamCommit $m.Groups[1].Value $m.Groups[3].Value
            if ($commit) { $provenance = "- Upstream commit: $($commit.Sha) ($($commit.Date))`n" }
        }
        catch { Write-Host "    ${id}: could not read the upstream commit" -ForegroundColor Yellow }
    }

    $newFooter = @"
---

## Upstream source

Vendored from the public skills.sh catalog so it travels with this repository.
The content is upstream's (unmodified).

- Source: $source
- Registry: $registry
- Installs when fetched: $installs
- Fetched: $today
$provenance- Security audits on skills.sh: $audits
"@
    [System.IO.File]::WriteAllText($file, "---`n$frontmatter`n---`n`n$body`n`n$newFooter", $utf8)
    Write-Host ("    {0}: updated" -f $id) -ForegroundColor Green
}

Write-Host ''
Write-Host ("$identical identical, $($changed.Count) changed, $($failed.Count) failed")
if ($changed.Count -gt 0 -and -not $Update) {
    Write-Host 'Run again with -Update to refresh the changed ones.'
    exit 1
}
if ($failed.Count -gt 0) { exit 3 }
exit 0

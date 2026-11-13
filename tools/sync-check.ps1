# Bring this working copy up to date with what the other computer pushed, then verify
# what landed. This is the first thing to run at the start of a session.
#
# What it compares, beyond plain `git pull`:
#   - commits and Machine trailers, so you know which machine produced what
#   - uncommitted or untracked leftovers that could collide with the pull
#   - serialized Unity files (scenes, prefabs, assets, ProjectSettings) vs a running
#     Editor: the pull waits if Unity could overwrite them
#   - the Unity version and packages the other machine used, against what is installed
#   - the code, the content CSVs, the docs and the skills, each with its own check
#
# Usage (from the repo root):
#   powershell -File tools/sync-check.ps1                 # fetch, pull, verify
#   powershell -File tools/sync-check.ps1 -NoPull         # report only
#   powershell -File tools/sync-check.ps1 -SkipVerify     # sync only
#   powershell -File tools/sync-check.ps1 -AuditSkills    # also compare skills with upstream
#
# Exit codes: 0 = in sync and verified, 1 = a human decision is needed, 2 = no network.

param(
    [switch]$NoPull,
    [switch]$SkipVerify,
    [switch]$AuditSkills
)

# Native git writes progress and ref updates to stderr; with ErrorActionPreference Stop,
# PowerShell 5.1 turns that into a terminating error. Keep it on Continue and check
# $LASTEXITCODE after the commands that matter.
$ErrorActionPreference = 'Continue'

$projectRoot = Split-Path $PSScriptRoot -Parent
$branch = 'main'

function Write-Step([string] $text) {
    Write-Host ''
    Write-Host $text -ForegroundColor Cyan
}
function Write-Ok([string] $text) { Write-Host "  ok    $text" -ForegroundColor Green }
function Write-Note([string] $text) { Write-Host "  note  $text" -ForegroundColor Gray }
function Write-Warn([string] $text) { Write-Host "  warn  $text" -ForegroundColor Yellow }
function Write-Bad([string] $text) { Write-Host "  STOP  $text" -ForegroundColor Red }

# Which kind of work a path belongs to. The categories decide the checks afterwards.
function Get-Category([string] $path) {
    if ($path -match '^Assets/.*\.cs$' -or $path -match '^tools/.*\.ps1$' -or $path -match '\.csproj$') { return 'code' }
    if ($path -match '^Assets/Resources/Content/.*\.csv$') { return 'content' }
    if ($path -match '^docs/' -or $path -eq 'mkdocs.yml' -or $path -eq 'requirements.txt') { return 'docs' }
    if ($path -match '^\.opencode/skills/') { return 'skills' }
    if ($path -eq 'ProjectSettings/ProjectVersion.txt' -or $path -match '^Packages/') { return 'unity-version' }
    if ($path -match '^Assets/.*\.(unity|prefab|asset|controller|anim|mat|spriteatlas|meta)$') { return 'serialized' }
    if ($path -match '^(ProjectSettings|UserSettings)/') { return 'serialized' }
    return 'other'
}

Push-Location $projectRoot
try {
    Write-Host ''
    Write-Host "== Sync check: $projectRoot ==" -ForegroundColor White

    # ---------------------------------------------------------------- 1. prerequisites
    Write-Step '[1/6] Prerequisites'
    git rev-parse --git-dir | Out-Null
    $hooksPath = (git config core.hooksPath) 2>$null
    if ($hooksPath -ne 'tools/git-hooks') {
        git config core.hooksPath tools/git-hooks
        Write-Note 'core.hooksPath was not set: pointed it at tools/git-hooks (Machine trailer)'
    }
    else { Write-Ok 'commit hook configured (Machine trailer)' }

    $lfsRaw = (git lfs version 2>$null)
    $lfsVersion = $null
    if ($LASTEXITCODE -eq 0 -and $lfsRaw) { $lfsVersion = (($lfsRaw -split '\s+')[0] -replace '^git-lfs/', '') }
    if ($lfsVersion) { Write-Ok "git lfs $lfsVersion" }
    else { Write-Warn 'git lfs is not installed: art/audio commits will not download' }

    $currentBranch = (git rev-parse --abbrev-ref HEAD).Trim()
    if ($currentBranch -ne $branch) { Write-Warn "on branch '$currentBranch', the routine expects '$branch'" }

    # ---------------------------------------------------------------- 2. local state
    Write-Step '[2/6] Local state'
    $porcelain = @(git status --porcelain=v1)
    $dirty = @($porcelain | Where-Object { $_ -and -not $_.StartsWith('??') } | ForEach-Object { $_.Substring(3).Trim() })
    $untracked = @($porcelain | Where-Object { $_ -and $_.StartsWith('??') } | ForEach-Object { $_.Substring(3).Trim() })
    if ($dirty.Count -eq 0) { Write-Ok 'no uncommitted changes' }
    else {
        Write-Warn "$($dirty.Count) uncommitted file(s):"
        $dirty | Select-Object -First 8 | ForEach-Object { Write-Host "          $_" -ForegroundColor DarkGray }
        if ($dirty.Count -gt 8) { Write-Host "          ... and $($dirty.Count - 8) more" -ForegroundColor DarkGray }
    }
    if ($untracked.Count -gt 0) {
        Write-Warn "$($untracked.Count) untracked file(s) (do not stage strangers):"
        $untracked | Select-Object -First 6 | ForEach-Object { Write-Host "          $_" -ForegroundColor DarkGray }
        if ($untracked.Count -gt 6) { Write-Host "          ... and $($untracked.Count - 6) more" -ForegroundColor DarkGray }
    }

    # ---------------------------------------------------------------- 3. remote
    Write-Step "[3/6] Remote ($branch)"
    git fetch origin --prune 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Bad 'git fetch failed: no network, or the remote needs credentials'
        exit 2
    }
    $counts = (git rev-list --left-right --count "HEAD...origin/$branch").Trim() -split '\s+'
    $ahead = [int]$counts[0]
    $behind = [int]$counts[1]
    Write-Note "ahead $ahead / behind $behind"

    if ($ahead -gt 0) {
        Write-Warn "this machine has $ahead unpushed commit(s):"
        git log "origin/$branch..HEAD" --format='          %h %s' | Select-Object -First 5
    }

    $incoming = @()
    $manifestRange = @('HEAD', "origin/$branch")
    if ($behind -gt 0) {
        Write-Ok "$behind commit(s) from the other machine:"
        $log = @(git log "HEAD..origin/$branch" --format='%h|%s|%(trailers:key=Machine,valueonly,separator=)')
        foreach ($line in $log) {
            $p = $line -split '\|'
            $machine = if ($p[2]) { $p[2].Trim() } else { '?' }
            Write-Host ("          {0} {1}  [{2}]" -f $p[0], $p[1], $machine) -ForegroundColor DarkGray
        }
        $incoming = @(git diff --name-only "HEAD" "origin/$branch")
    }
    else {
        Write-OK 'already up to date'
    }

    # ---------------------------------------------------------------- 4. apply
    Write-Step '[4/6] Apply'
    $categories = @{}
    foreach ($path in $incoming) {
        $c = Get-Category $path
        if (-not $categories.ContainsKey($c)) { $categories[$c] = @() }
        $categories[$c] += $path
    }

    if ($behind -eq 0) {
        Write-Note 'nothing to pull'
    }
    elseif ($NoPull) {
        Write-Warn "-NoPull: $($incoming.Count) file(s) are waiting to be pulled"
    }
    else {
        $blockers = @()

        # Local edits to a file the pull must write = git refuses; say it before it happens.
        $overlap = @($dirty | Where-Object { $incoming -contains $_ })
        if ($overlap.Count -gt 0) {
            $blockers += "uncommitted local edits on files the pull has to write:"
            $overlap | Select-Object -First 8 | ForEach-Object { $blockers += "    $_" }
            $blockers += '  commit them, stash them, or ask before discarding.'
        }
        # Untracked files that the pull wants to create.
        $added = @(git diff --name-only --diff-filter=A "HEAD" "origin/$branch")
        $clash = @($untracked | Where-Object { $added -contains $_ })
        if ($clash.Count -gt 0) {
            $blockers += "untracked files that the incoming commit also adds:"
            $clash | Select-Object -First 8 | ForEach-Object { $blockers += "    $_" }
        }
        # Serialized assets: Unity rewrites them on focus, so it has to be closed first.
        if ($categories.ContainsKey('serialized')) {
            $unity = @(Get-Process -Name 'Unity' -ErrorAction SilentlyContinue)
            if ($unity.Count -gt 0) {
                $blockers += "$($categories['serialized'].Count) serialized Unity file(s) are incoming and the Editor is running:"
                $blockers += '  close Unity, then run this script again (ProjectSettings and scenes cannot be pulled under a live Editor).'
            }
        }

        if ($blockers.Count -gt 0) {
            foreach ($line in $blockers) { Write-Bad $line }
            Write-Host ''
            Write-Host '  Nothing was pulled.' -ForegroundColor Red
            exit 1
        }

        # Captured, not streamed: git writes ref updates to stderr and they break the flow.
        $pullOutput = git pull --ff-only origin $branch 2>&1
        if ($LASTEXITCODE -ne 0) {
            $pullOutput | Select-Object -Last 12 | ForEach-Object { Write-Host "          $_" -ForegroundColor DarkGray }
            Write-Bad 'git pull --ff-only failed: resolve it by hand before doing anything else'
            exit 1
        }
        Write-Ok "pulled $behind commit(s), $($incoming.Count) file(s) touched"
        $manifestRange = @('HEAD@{1}', 'HEAD')
    }

    # Report the categories that arrived, which is what the checks work from.
    if ($incoming.Count -gt 0) {
        foreach ($key in ($categories.Keys | Sort-Object)) {
            $n = @($categories[$key]).Count
            Write-Note ("{0,-13} {1,3} file(s)" -f $key, $n)
            if ($key -eq 'skills') {
                @($categories[$key]) | ForEach-Object { Write-Host "          $_" -ForegroundColor DarkGray }
            }
        }
    }

    # ---------------------------------------------------------------- 5. environment
    Write-Step '[5/6] Environment the other machine expects'
    $versionFile = Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt'
    $editorVersion = ((Get-Content $versionFile | Where-Object { $_ -match '^m_EditorVersion:' }) -replace '^m_EditorVersion:\s*', '').Trim()
    $editorPath = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$editorVersion"
    if (Test-Path $editorPath) { Write-Ok "Unity $editorVersion installed" }
    else { Write-Warn "Unity $editorVersion is NOT installed; install it from the Hub before opening the project" }

    $unityRunning = @(Get-Process -Name 'Unity' -ErrorAction SilentlyContinue).Count -gt 0
    if ($unityRunning) { Write-Note 'the Editor is open right now: it will reimport whatever landed' }

    if ($categories.ContainsKey('unity-version')) {
        Write-Warn 'ProjectSettings or Packages changed: open Unity once so it can upgrade the assets'
        $manifestDiff = @(git diff $manifestRange[0] $manifestRange[1] -- Packages/manifest.json 2>$null | Where-Object { $_ -match '^[+-]\s' -and $_ -notmatch '^[+-]\s*[{}]' })
        foreach ($line in ($manifestDiff | Select-Object -First 10)) { Write-Host "          $line" -ForegroundColor DarkGray }
    }

    # ---------------------------------------------------------------- 6. verify
    Write-Step '[6/6] Verify what landed'
    if ($SkipVerify) {
        Write-Note '-SkipVerify: no check was run'
        exit 0
    }
    if ($incoming.Count -eq 0 -and -not $AuditSkills) {
        Write-Note 'nothing landed, so there is nothing to verify'
    }

    $failed = @()
    if ($categories.ContainsKey('code') -and (Test-Path (Join-Path $PSScriptRoot 'compile-check.ps1'))) {
        Write-Host '  -> compile check' -ForegroundColor Cyan
        & (Join-Path $PSScriptRoot 'compile-check.ps1') | Select-Object -Last 4
        if ($LASTEXITCODE -ne 0) { $failed += 'compile check' }
    }

    if ($categories.ContainsKey('content')) {
        Write-Host '  -> content check' -ForegroundColor Cyan
        dotnet run --project (Join-Path $PSScriptRoot 'content-check') | Select-Object -Last 6
        if ($LASTEXITCODE -ne 0) { $failed += 'content check' }
    }

    if ($categories.ContainsKey('docs')) {
        $python = Get-Command python -ErrorAction SilentlyContinue
        if ($python) {
            Write-Host '  -> mkdocs build' -ForegroundColor Cyan
            python -m mkdocs build 2>&1 | Select-Object -Last 5
            if ($LASTEXITCODE -ne 0) { $failed += 'mkdocs build' }
        }
        else { Write-Warn 'python is not on PATH: build the docs site by hand' }
    }

    if ($categories.ContainsKey('serialized')) {
        Write-Note 'serialized assets landed: open Unity and let it reimport before Play mode'
    }

    if ($AuditSkills) {
        Write-Host '  -> skills against upstream' -ForegroundColor Cyan
        & (Join-Path $PSScriptRoot 'skills-check.ps1')
        if ($LASTEXITCODE -eq 1) { Write-Note 'some skills changed upstream: run tools/skills-check.ps1 -Update' }
    }

    Write-Host ''
    if ($failed.Count -gt 0) {
        Write-Bad ('failed: ' + ($failed -join ', '))
        exit 1
    }
    Write-Ok 'up to date and verified'
    exit 0
}
finally {
    Pop-Location
}

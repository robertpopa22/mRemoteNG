<#
.SYNOPSIS
    Batch-marks issues as released in the Issue Intelligence DB.
    Phase 1 of the 2026-02-10 triage plan.

.DESCRIPTION
    Updates our_status from "new" to "released" for all 25 v1.79.0 issues
    and 1 v1.80.0 issue. Uses -SkipComment (no GitHub posting).
    Run this ONCE to backfill the DB after initial sync.
#>
param(
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot
$updateScript = Join-Path $scriptDir "Update-Status.ps1"

# v1.79.0 fixes (24 issues via 26 PRs, released 2026-02-09)
$v179 = @(
    @{ Issue=3069; PR=3106; Desc="Close panel race fix" },
    @{ Issue=3092; PR=3107; Desc="1Password parser and fallback fix" },
    @{ Issue=2972; PR=3108; Desc="Default external provider fix" },
    @{ Issue=3005; PR=3110; Desc="SqlClient SNI runtime references" },
    @{ Issue=1916; PR=3111; Desc="SQL schema compatibility hardening" },
    @{ Issue=850;  PR=3112; Desc="Config panel splitter width reset" },
    @{ Issue=1969; PR=3113; Desc="Startup path fallback" },
    @{ Issue=822;  PR=3114; Desc="PuTTY provider failure handling" },
    @{ Issue=2785; PR=3115; Desc="PuTTY CJK session name decoding" },
    @{ Issue=2735; PR=3116; Desc="RDP SmartSize focus loss fix" },
    @{ Issue=847;  PR=3117; Desc="RDP fullscreen toggle guard" },
    @{ Issue=1650; PR=3118; Desc="RDP refocus after fullscreen exit" },
    @{ Issue=2510; PR=3119; Desc="RDP SmartSize RCW disconnect fix" },
    @{ Issue=2987; PR=3120; Desc="Settings path logging" },
    @{ Issue=2673; PR=3121; Desc="Require password before disabling protection" },
    @{ Issue=1649; PR=3122; Desc="Master password autolock on minimize/idle" },
    @{ Issue=1634; PR=3123; Desc="PROTOCOL external tool token" },
    @{ Issue=2270; PR=3124; Desc="Main close cancel behavior" },
    @{ Issue=811;  PR=3125; Desc="Startup XML recovery" },
    @{ Issue=2160; PR=3126; Desc="Empty panel close after last tab" },
    @{ Issue=2161; PR=3127; Desc="Tab drag autoscroll on overflow" },
    @{ Issue=2171; PR=3128; Desc="Config connections panel focus" },
    @{ Issue=2166; PR=3129; Desc="Tab close race under resize" },
    @{ Issue=2155; PR=3130; Desc="Inheritance label width fix" }
)

# v1.80.0 fixes
$v180 = @(
    @{ Issue=2142; PR=0; Desc="Resize RDP connections on monitor connect/disconnect" }
)

$totalSuccess = 0
$totalFail = 0
$totalSkip = 0

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Phase 1: Batch Mark Issues as Released" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Process v1.79.0
Write-Host "--- v1.79.0 issues (24) ---" -ForegroundColor Yellow
foreach ($item in $v179) {
    $num = $item.Issue
    $pr = $item.PR
    $desc = $item.Desc

    if ($DryRun) {
        Write-Host "[DRY RUN] Would update #$num -> released (v1.79.0, PR #$pr)" -ForegroundColor DarkGray
        $totalSkip++
        continue
    }

    Write-Host "Updating #$num -> released (PR #$pr)..." -NoNewline
    try {
        $args = @(
            "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $updateScript,
            "-Issue", $num,
            "-Repo", "upstream",
            "-Status", "released",
            "-Description", $desc,
            "-Release", "v1.79.0",
            "-ReleaseUrl", "https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.79.0",
            "-SkipComment"
        )
        if ($pr -gt 0) { $args += @("-PR", $pr) }

        $output = & powershell.exe @args 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host " OK" -ForegroundColor Green
            $totalSuccess++
        } else {
            Write-Host " FAIL (exit $LASTEXITCODE)" -ForegroundColor Red
            Write-Host ($output | Out-String) -ForegroundColor DarkGray
            $totalFail++
        }
    } catch {
        Write-Host " ERROR: $_" -ForegroundColor Red
        $totalFail++
    }
}

Write-Host ""
Write-Host "--- v1.80.0 issues (1) ---" -ForegroundColor Yellow
foreach ($item in $v180) {
    $num = $item.Issue
    $desc = $item.Desc

    if ($DryRun) {
        Write-Host "[DRY RUN] Would update #$num -> released (v1.80.0)" -ForegroundColor DarkGray
        $totalSkip++
        continue
    }

    Write-Host "Updating #$num -> released..." -NoNewline
    try {
        $args = @(
            "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $updateScript,
            "-Issue", $num,
            "-Repo", "upstream",
            "-Status", "released",
            "-Description", $desc,
            "-Release", "v1.80.0",
            "-SkipComment"
        )

        $output = & powershell.exe @args 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host " OK" -ForegroundColor Green
            $totalSuccess++
        } else {
            Write-Host " FAIL (exit $LASTEXITCODE)" -ForegroundColor Red
            Write-Host ($output | Out-String) -ForegroundColor DarkGray
            $totalFail++
        }
    } catch {
        Write-Host " ERROR: $_" -ForegroundColor Red
        $totalFail++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Results: $totalSuccess OK, $totalFail failed, $totalSkip skipped" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

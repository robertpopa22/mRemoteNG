<#
.SYNOPSIS
    Fast batch update - marks issues as released directly in JSON files.
    Phase 1 of the 2026-02-10 triage plan. No GitHub comments (local only).
#>
$ErrorActionPreference = "Stop"
$dbRoot = Join-Path $PSScriptRoot "..\issues-db\upstream"
$now = [datetime]::UtcNow.ToString("yyyy-MM-dd")
$nowFull = [datetime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")

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

$v180 = @(
    @{ Issue=2142; PR=0; Desc="Resize RDP connections on monitor connect/disconnect" }
)

$ok = 0; $fail = 0

Write-Host "=== Phase 1: Batch Mark Released (fast, local only) ===" -ForegroundColor Cyan

foreach ($list in @(@{Items=$v179; Release="v1.79.0"; Url="https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.79.0"}, @{Items=$v180; Release="v1.80.0"; Url=""})) {
    $rel = $list.Release
    Write-Host "`n--- $rel ---" -ForegroundColor Yellow

    foreach ($item in $list.Items) {
        $padded = $item.Issue.ToString().PadLeft(4, '0')
        $fp = Join-Path $dbRoot "$padded.json"

        if (!(Test-Path $fp)) {
            Write-Host "  #$($item.Issue) - NOT FOUND" -ForegroundColor Red
            $fail++
            continue
        }

        $json = Get-Content $fp -Raw | ConvertFrom-Json
        $oldStatus = $json.our_status

        $json.our_status = "released"
        $json.target_release = $rel
        if ($item.PR -gt 0) { $json.our_pr = $item.PR }
        $json.last_synced = $nowFull

        # Add iteration entry
        $iterSeq = 1
        if ($json.iterations -and $json.iterations.Count -gt 0) {
            $iterSeq = ($json.iterations | Measure-Object -Property seq -Maximum).Maximum + 1
        }

        $iterEntry = [ordered]@{
            seq = $iterSeq
            date = $now
            type = "released"
            description = $item.Desc
            pr = if ($item.PR -gt 0) { $item.PR } else { $null }
            branch = $null
            release = $rel
            comment_posted = $false
        }

        if (-not $json.iterations) {
            $json | Add-Member -NotePropertyName "iterations" -NotePropertyValue @() -Force
        }
        $iters = [System.Collections.ArrayList]@($json.iterations)
        $iters.Add($iterEntry) | Out-Null
        $json.iterations = @($iters)

        $json | ConvertTo-Json -Depth 10 | Out-File -Encoding utf8 $fp
        Write-Host "  #$($item.Issue) $oldStatus -> released ($rel, PR#$($item.PR))" -ForegroundColor Green
        $ok++
    }
}

Write-Host "`n=== Done: $ok updated, $fail failed ===" -ForegroundColor Cyan

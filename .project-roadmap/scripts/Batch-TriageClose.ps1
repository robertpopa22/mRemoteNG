<#
.SYNOPSIS
    Phase 3: Mark duplicates and not-planned issues locally.
    No GitHub comments - will be consolidated in final upstream PR.
#>
$ErrorActionPreference = "Stop"
$dbRoot = Join-Path $PSScriptRoot "..\issues-db\upstream"
$now = [datetime]::UtcNow.ToString("yyyy-MM-dd")
$nowFull = [datetime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")

$duplicates = @(
    @{ Issue=520;  Desc="Duplicate - Alt Tab on Windows 10" },
    @{ Issue=1684; Desc="Duplicate of #2160 (fixed in PR #3126)" },
    @{ Issue=1837; Desc="Duplicate - Can't find Use VM ID property" },
    @{ Issue=1874; Desc="Duplicate - Use stored credentials in hostname" },
    @{ Issue=2537; Desc="Duplicate - log4net.dll old version (dependency updated)" },
    @{ Issue=3051; Desc="Duplicate/Won't Fix - false positive AI scanner finding" },
    @{ Issue=3062; Desc="Duplicate of #3069 (fixed in PR #3106)" },
    @{ Issue=2706; Desc="Duplicate of #3069 (fixed in PR #3106)" }
)

$wontfix = @(
    @{ Issue=2664; Desc="Not our bug - Windows Update KB5050094 caused RDP failures, fixed by KB5044285" },
    @{ Issue=3088; Desc="Not our bug - Windows Update KB5074109 auth error, cannot reproduce" },
    @{ Issue=2653; Desc="Stale - RDP clipboard Server 2025, workaround confirmed (rdpclip.exe)" },
    @{ Issue=2610; Desc="Stale - 'New release?' question, v1.79.0 and v1.80.0 now exist" },
    @{ Issue=2833; Desc="Not an issue - Renovate bot dependency dashboard" },
    @{ Issue=2662; Desc="Website issue - not application code" },
    @{ Issue=2818; Desc="Website infrastructure - CSP header, not application code" },
    @{ Issue=3020; Desc="Stale - network printer redirection, no repro, likely Windows/driver" },
    @{ Issue=2576; Desc="Stale - RD Gateway failure, likely Windows config" }
)

$ok = 0; $fail = 0

function Update-Issue($num, $status, $desc) {
    $padded = $num.ToString().PadLeft(4, '0')
    $fp = Join-Path $dbRoot "$padded.json"
    if (!(Test-Path $fp)) {
        Write-Host "  #$num - NOT FOUND" -ForegroundColor Red
        $script:fail++
        return
    }
    $json = Get-Content $fp -Raw | ConvertFrom-Json
    $json.our_status = $status
    $json.last_synced = $nowFull

    $iterSeq = 1
    if ($json.iterations -and $json.iterations.Count -gt 0) {
        $iterSeq = ($json.iterations | Measure-Object -Property seq -Maximum).Maximum + 1
    }
    $iterEntry = [ordered]@{
        seq = $iterSeq; date = $now; type = $status
        description = $desc; pr = $null; branch = $null; release = $null; comment_posted = $false
    }
    if (-not $json.iterations) {
        $json | Add-Member -NotePropertyName "iterations" -NotePropertyValue @() -Force
    }
    $iters = [System.Collections.ArrayList]@($json.iterations)
    $iters.Add($iterEntry) | Out-Null
    $json.iterations = @($iters)
    $json | ConvertTo-Json -Depth 10 | Out-File -Encoding utf8 $fp
    Write-Host "  #$num -> $status" -ForegroundColor Green
    $script:ok++
}

Write-Host "=== Phase 3: Triage Close (local only) ===" -ForegroundColor Cyan

Write-Host "`n--- Duplicates (8) ---" -ForegroundColor Yellow
foreach ($d in $duplicates) { Update-Issue $d.Issue "duplicate" $d.Desc }

Write-Host "`n--- Won't Fix / Not Planned (9) ---" -ForegroundColor Yellow
foreach ($w in $wontfix) { Update-Issue $w.Issue "wontfix" $w.Desc }

Write-Host "`n=== Done: $ok updated, $fail failed ===" -ForegroundColor Cyan

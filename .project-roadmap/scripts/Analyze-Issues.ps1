<#
.SYNOPSIS
    Issue Intelligence System - Analyze script.
    Reads all synced issues, identifies what needs attention,
    classifies by priority, and outputs actionable recommendations.

.DESCRIPTION
    MANDATORY: Run after Sync-Issues.ps1 to review new/changed issues.
    Produces a prioritized list of items needing response, triage, or roadmap inclusion.

.PARAMETER ShowAll
    Show all issues, not just those needing action.

.PARAMETER Priority
    Filter by priority level (P0-critical, P1-security, P2-bug, P3-enhancement, P4-debt).

.PARAMETER Status
    Filter by our_status (new, triaged, roadmap, in-progress, testing, released, wontfix, duplicate).

.PARAMETER WaitingOnly
    Show only issues where someone is waiting for our response.

.EXAMPLE
    # Show issues needing action
    .\Analyze-Issues.ps1

    # Show only issues waiting for our response
    .\Analyze-Issues.ps1 -WaitingOnly

    # Show all issues with their status
    .\Analyze-Issues.ps1 -ShowAll

    # Filter by priority
    .\Analyze-Issues.ps1 -Priority P2-bug
#>
param(
    [switch]$ShowAll,

    [string]$Priority,

    [string]$Status,

    [switch]$WaitingOnly
)

$ErrorActionPreference = "Stop"
$DbRoot = Join-Path $PSScriptRoot "..\issues-db"
$MetaPath = Join-Path $DbRoot "_meta.json"

if (!(Test-Path $MetaPath)) {
    throw "Issue DB not initialized. Run Sync-Issues.ps1 first."
}

$meta = Get-Content $MetaPath -Raw | ConvertFrom-Json
$ourUser = $meta.our_github_user

Write-Host "=== Issue Intelligence System - Analysis ===" -ForegroundColor Cyan
if ($meta.last_sync) {
    Write-Host "Last sync: $($meta.last_sync)"
} else {
    Write-Host "WARNING: No sync has been run yet. Run Sync-Issues.ps1 first." -ForegroundColor Red
    return
}
Write-Host ""

# --- Load all issues ---
function Load-AllIssues {
    $issues = @()
    foreach ($repoKey in @("upstream", "fork")) {
        $dir = Join-Path $DbRoot $repoKey
        if (!(Test-Path $dir)) { continue }
        $files = Get-ChildItem -Path $dir -Filter "*.json" -File
        foreach ($f in $files) {
            $issue = Get-Content $f.FullName -Raw | ConvertFrom-Json
            $issue | Add-Member -NotePropertyName "repo_key" -NotePropertyValue $repoKey -Force
            $issue | Add-Member -NotePropertyName "file_path" -NotePropertyValue $f.FullName -Force
            $issues += $issue
        }
    }
    return $issues
}

$allIssues = Load-AllIssues
Write-Host "Total issues in DB: $($allIssues.Count)" -ForegroundColor DarkGray

# --- Auto-classify new issues based on labels ---
function Auto-Classify {
    param([object]$issue)

    $suggestion = @{
        priority = $null
        category = $null
        action   = $null
    }

    $labels = @()
    if ($issue.labels) { $labels = @($issue.labels) }

    # Priority from labels
    if ("critical" -in $labels -or "Security" -in $labels) {
        $suggestion.priority = "P0-critical"
    } elseif ("Bug" -in $labels) {
        if ("1.78.*" -in $labels) {
            $suggestion.priority = "P2-bug"
        } else {
            $suggestion.priority = "P3-enhancement"
        }
    } elseif ("Enhancement" -in $labels) {
        $suggestion.priority = "P3-enhancement"
    } elseif ("Duplicate" -in $labels) {
        $suggestion.priority = "P4-debt"
        $suggestion.action = "verify-duplicate"
    } elseif ("Need 2 check" -in $labels) {
        $suggestion.priority = "P2-bug"
        $suggestion.action = "needs-verification"
    } else {
        $suggestion.priority = "P3-enhancement"
    }

    # Check iteration status - has there been user feedback after our fix?
    if ($issue.iterations -and $issue.iterations.Count -gt 0) {
        $lastIter = $issue.iterations | Select-Object -Last 1
        if ($lastIter.type -eq "user-feedback" -or $lastIter.result -eq "partial") {
            $suggestion.action = "iteration-needed"
        }
    }

    # Check if waiting for us
    if ($issue.waiting_for_us) {
        if (-not $suggestion.action) {
            $suggestion.action = "respond"
        }
    }

    return $suggestion
}

# --- Filter issues ---
$filtered = $allIssues

if ($WaitingOnly) {
    $filtered = $filtered | Where-Object { $_.waiting_for_us -eq $true }
} elseif (-not $ShowAll) {
    $filtered = $filtered | Where-Object { $_.needs_action -eq $true -or $_.our_status -eq "new" -or $_.waiting_for_us -eq $true }
}

if ($Priority) {
    $filtered = $filtered | Where-Object { $_.priority -eq $Priority }
}

if ($Status) {
    $filtered = $filtered | Where-Object { $_.our_status -eq $Status }
}

# --- Categorize ---
$urgent     = @()  # Waiting for us + P0/P1
$respond    = @()  # Waiting for us
$triage     = @()  # New, needs triage
$iteration  = @()  # User feedback after our fix
$roadmap    = @()  # In roadmap, for awareness
$other      = @()  # Everything else

foreach ($issue in $filtered) {
    $classification = Auto-Classify -issue $issue

    if ($issue.waiting_for_us -and ($classification.priority -in @("P0-critical", "P1-security"))) {
        $urgent += @{ issue = $issue; classification = $classification }
    } elseif ($classification.action -eq "iteration-needed") {
        $iteration += @{ issue = $issue; classification = $classification }
    } elseif ($issue.waiting_for_us) {
        $respond += @{ issue = $issue; classification = $classification }
    } elseif ($issue.our_status -eq "new") {
        $triage += @{ issue = $issue; classification = $classification }
    } elseif ($issue.our_status -eq "roadmap") {
        $roadmap += @{ issue = $issue; classification = $classification }
    } else {
        $other += @{ issue = $issue; classification = $classification }
    }
}

# --- Output ---
function Print-IssueRow {
    param([object]$item)
    $i = $item.issue
    $c = $item.classification
    $iterCount = if ($i.iterations) { $i.iterations.Count } else { 0 }
    $unread = if ($i.unread_comments) { $i.unread_comments } else { 0 }

    $line = "  #{0,-5} [{1,-8}] [{2,-14}] {3}" -f $i.number, $i.our_status, $c.priority, $i.title
    if ($line.Length -gt 120) { $line = $line.Substring(0, 117) + "..." }

    Write-Host $line -NoNewline
    if ($iterCount -gt 0) { Write-Host " (iter:$iterCount)" -ForegroundColor DarkYellow -NoNewline }
    if ($unread -gt 0) { Write-Host " [+$unread unread]" -ForegroundColor Cyan -NoNewline }
    if ($c.action) { Write-Host " -> $($c.action)" -ForegroundColor Yellow -NoNewline }
    Write-Host ""
}

if ($urgent.Count -gt 0) {
    Write-Host "!! URGENT - Waiting for us (critical/security) !!" -ForegroundColor Red
    foreach ($item in $urgent) { Print-IssueRow -item $item }
    Write-Host ""
}

if ($iteration.Count -gt 0) {
    Write-Host ">> ITERATION NEEDED - User feedback after our fix <<" -ForegroundColor Magenta
    foreach ($item in $iteration) { Print-IssueRow -item $item }
    Write-Host ""
}

if ($respond.Count -gt 0) {
    Write-Host ">> RESPOND - Waiting for our response <<" -ForegroundColor Yellow
    foreach ($item in $respond) { Print-IssueRow -item $item }
    Write-Host ""
}

if ($triage.Count -gt 0) {
    Write-Host "-- NEW - Needs triage --" -ForegroundColor Green
    foreach ($item in $triage) { Print-IssueRow -item $item }
    Write-Host ""
}

if ($roadmap.Count -gt 0) {
    Write-Host "-- ROADMAP - Tracked for next release --" -ForegroundColor Blue
    foreach ($item in $roadmap) { Print-IssueRow -item $item }
    Write-Host ""
}

if ($other.Count -gt 0) {
    Write-Host "-- OTHER --" -ForegroundColor DarkGray
    foreach ($item in $other) { Print-IssueRow -item $item }
    Write-Host ""
}

# --- Summary stats ---
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "Total in DB:       $($allIssues.Count)"
Write-Host "Shown:             $($filtered.Count)"
Write-Host "Urgent:            $($urgent.Count)" -ForegroundColor $(if ($urgent.Count -gt 0) { "Red" } else { "Gray" })
Write-Host "Iteration needed:  $($iteration.Count)" -ForegroundColor $(if ($iteration.Count -gt 0) { "Magenta" } else { "Gray" })
Write-Host "Awaiting response: $($respond.Count)" -ForegroundColor $(if ($respond.Count -gt 0) { "Yellow" } else { "Gray" })
Write-Host "New (triage):      $($triage.Count)" -ForegroundColor $(if ($triage.Count -gt 0) { "Green" } else { "Gray" })
Write-Host "In roadmap:        $($roadmap.Count)" -ForegroundColor $(if ($roadmap.Count -gt 0) { "Blue" } else { "Gray" })

# --- Status distribution ---
Write-Host ""
Write-Host "--- Status Distribution ---" -ForegroundColor DarkGray
$statusGroups = $allIssues | Group-Object -Property our_status | Sort-Object Count -Descending
foreach ($g in $statusGroups) {
    Write-Host "  $($g.Name): $($g.Count)"
}

# --- Suggest next steps ---
Write-Host ""
Write-Host "--- Suggested Next Steps ---" -ForegroundColor DarkGray
if ($urgent.Count -gt 0) {
    Write-Host "  1. RESPOND to $($urgent.Count) urgent issues immediately" -ForegroundColor Red
}
if ($iteration.Count -gt 0) {
    Write-Host "  2. RE-FIX $($iteration.Count) issues with user feedback (iteration loop)" -ForegroundColor Magenta
}
if ($respond.Count -gt 0) {
    Write-Host "  3. Reply to $($respond.Count) issues waiting for response" -ForegroundColor Yellow
}
if ($triage.Count -gt 0) {
    Write-Host "  4. Triage $($triage.Count) new issues (run: Update-Status.ps1 -Issue <N> -Status triaged)" -ForegroundColor Green
}
Write-Host "  Run: .\Update-Status.ps1 -Issue <N> -Status <status> to transition issues" -ForegroundColor DarkGray
Write-Host "  Run: .\Generate-Report.ps1 to create markdown report" -ForegroundColor DarkGray

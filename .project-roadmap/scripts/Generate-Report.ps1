<#
.SYNOPSIS
    Issue Intelligence System - Report generator.
    Aggregates all issue JSONs into a structured markdown report.

.DESCRIPTION
    Generates a comprehensive markdown report showing:
    - Sync status and last run time
    - Issues needing immediate attention (waiting for us, iteration needed)
    - Roadmap items with progress
    - Status distribution and statistics
    - Full issue inventory with iteration history

.PARAMETER OutputDir
    Report output directory (default: .project-roadmap/issues-db/reports/).

.PARAMETER IncludeAll
    Include all issues in the report (not just actionable ones).

.PARAMETER NoSave
    Print to console only, do not save to file.

.EXAMPLE
    # Generate standard report
    .\Generate-Report.ps1

    # Full inventory report
    .\Generate-Report.ps1 -IncludeAll

    # Console only
    .\Generate-Report.ps1 -NoSave
#>
param(
    [string]$OutputDir,

    [switch]$IncludeAll,

    [switch]$NoSave
)

$ErrorActionPreference = "Stop"
$DbRoot = Join-Path $PSScriptRoot "..\issues-db"
$MetaPath = Join-Path $DbRoot "_meta.json"
$RoadmapPath = Join-Path $DbRoot "_roadmap.json"

if (-not $OutputDir) {
    $OutputDir = Join-Path $DbRoot "reports"
}

if (!(Test-Path $MetaPath)) {
    throw "Issue DB not initialized. Run Sync-Issues.ps1 first."
}

$meta = Get-Content $MetaPath -Raw | ConvertFrom-Json
$now = [datetime]::UtcNow
$reportDate = $now.ToString("yyyy-MM-dd")

# --- Load all issues ---
$allIssues = @()
foreach ($repoKey in @("upstream", "fork")) {
    $dir = Join-Path $DbRoot $repoKey
    if (!(Test-Path $dir)) { continue }
    $files = Get-ChildItem -Path $dir -Filter "*.json" -File
    foreach ($f in $files) {
        $issue = Get-Content $f.FullName -Raw | ConvertFrom-Json
        $issue | Add-Member -NotePropertyName "repo_key" -NotePropertyValue $repoKey -Force
        $allIssues += $issue
    }
}

# --- Load roadmap ---
$roadmap = $null
if (Test-Path $RoadmapPath) {
    $roadmap = Get-Content $RoadmapPath -Raw | ConvertFrom-Json
}

# --- Categorize ---
$urgent     = $allIssues | Where-Object { $_.waiting_for_us -and $_.priority -in @("P0-critical", "P1-security") }
$iteration  = $allIssues | Where-Object {
    $_.iterations -and $_.iterations.Count -gt 0 -and
    ($_.iterations[-1].type -eq "iteration-reopen" -or $_.iterations[-1].type -eq "user-feedback")
}
$waitingForUs = $allIssues | Where-Object { $_.waiting_for_us }
$newIssues    = $allIssues | Where-Object { $_.our_status -eq "new" }
$inProgress   = $allIssues | Where-Object { $_.our_status -eq "in-progress" }
$testing      = $allIssues | Where-Object { $_.our_status -eq "testing" }
$released     = $allIssues | Where-Object { $_.our_status -eq "released" }
$roadmapItems = $allIssues | Where-Object { $_.our_status -eq "roadmap" }

# --- Build report ---
$report = @()
$report += "# Issue Intelligence Report - $reportDate"
$report += ""
$report += "Generated: $($now.ToString('yyyy-MM-dd HH:mm:ss')) UTC"
$report += "Last sync: $($meta.last_sync)"
$report += "Total issues tracked: $($allIssues.Count)"
$report += ""

# Stats table
$report += "## Status Summary"
$report += ""
$report += "| Status | Count |"
$report += "|--------|-------|"
$statusGroups = $allIssues | Group-Object -Property our_status | Sort-Object Count -Descending
foreach ($g in $statusGroups) {
    $report += "| $($g.Name) | $($g.Count) |"
}
$report += ""

# Urgent
if ($urgent) {
    $report += "## !! URGENT - Critical/Security Waiting for Response"
    $report += ""
    foreach ($i in $urgent) {
        $report += "- **#$($i.number)** [$($i.priority)] $($i.title)"
        $report += "  - Repo: $($i.repo) | Status: $($i.our_status) | Updated: $($i.github_updated_at)"
    }
    $report += ""
}

# Iteration needed
if ($iteration) {
    $report += "## Iteration Needed - User Feedback After Fix"
    $report += ""
    $report += "> These issues had a fix applied but user reported it's incomplete."
    $report += "> This is the #3044 pattern: fix -> test -> user feedback -> re-fix."
    $report += ""
    foreach ($i in $iteration) {
        $iterCount = if ($i.iterations) { $i.iterations.Count } else { 0 }
        $report += "- **#$($i.number)** [$($i.priority)] $($i.title) - iteration $iterCount"
        if ($i.iterations) {
            foreach ($iter in $i.iterations) {
                $report += "  - [$($iter.date)] $($iter.type): $($iter.description)"
            }
        }
    }
    $report += ""
}

# Waiting for response
if ($waitingForUs) {
    $report += "## Waiting for Our Response"
    $report += ""
    foreach ($i in ($waitingForUs | Sort-Object { $_.github_updated_at })) {
        $lastComment = if ($i.comments) { $i.comments | Select-Object -Last 1 } else { $null }
        $report += "- **#$($i.number)** [$($i.our_status)] $($i.title)"
        if ($lastComment) {
            $snippet = $lastComment.snippet
            if ($snippet.Length -gt 100) { $snippet = $snippet.Substring(0, 100) + "..." }
            $report += "  - Last: @$($lastComment.author) ($($lastComment.date)): $snippet"
        }
    }
    $report += ""
}

# New issues (need triage)
if ($newIssues) {
    $report += "## New Issues - Need Triage"
    $report += ""
    foreach ($i in ($newIssues | Sort-Object { $_.github_updated_at } -Descending)) {
        $labels = if ($i.labels) { ($i.labels -join ", ") } else { "none" }
        $report += "- **#$($i.number)** $($i.title)"
        $report += "  - Labels: $labels | Author: @$($i.author) | Created: $($i.created_at)"
    }
    $report += ""
}

# Roadmap
if ($roadmapItems -or ($roadmap -and $roadmap.items.Count -gt 0)) {
    $report += "## Roadmap - Target: $($roadmap.target_release)"
    $report += ""
    $report += "| # | Priority | Title | Status |"
    $report += "|---|----------|-------|--------|"
    foreach ($item in $roadmap.items) {
        $issue = $allIssues | Where-Object { $_.number -eq $item.number -and $_.repo -eq $item.repo }
        $status = if ($issue) { $issue.our_status } else { $item.our_status }
        $report += "| #$($item.number) | $($item.priority) | $($item.title) | $status |"
    }
    $report += ""
}

# In progress
if ($inProgress) {
    $report += "## In Progress"
    $report += ""
    foreach ($i in $inProgress) {
        $branch = if ($i.our_branch) { "``$($i.our_branch)``" } else { "no branch" }
        $report += "- **#$($i.number)** $($i.title) - $branch"
    }
    $report += ""
}

# Testing
if ($testing) {
    $report += "## In Testing"
    $report += ""
    foreach ($i in $testing) {
        $pr = if ($i.our_pr) { "PR #$($i.our_pr)" } else { "no PR" }
        $report += "- **#$($i.number)** $($i.title) - $pr"
    }
    $report += ""
}

# Recently released (last 30 days)
$recentReleased = $released | Where-Object {
    $_.iterations -and ($_.iterations | Where-Object {
        $_.type -eq "released" -and
        [datetime]$_.date -gt $now.AddDays(-30)
    })
}
if ($recentReleased) {
    $report += "## Recently Released (last 30 days)"
    $report += ""
    foreach ($i in $recentReleased) {
        $rel = ($i.iterations | Where-Object { $_.type -eq "released" } | Select-Object -Last 1).release
        $report += "- **#$($i.number)** $($i.title) - $rel"
    }
    $report += ""
}

# Full inventory (if requested)
if ($IncludeAll) {
    $report += "## Full Issue Inventory"
    $report += ""
    $report += "| # | Repo | Status | Priority | Title | Iterations |"
    $report += "|---|------|--------|----------|-------|------------|"
    foreach ($i in ($allIssues | Sort-Object number)) {
        $iterCount = if ($i.iterations) { $i.iterations.Count } else { 0 }
        $shortTitle = $i.title
        if ($shortTitle.Length -gt 60) { $shortTitle = $shortTitle.Substring(0, 57) + "..." }
        $report += "| #$($i.number) | $($i.repo_key) | $($i.our_status) | $($i.priority) | $shortTitle | $iterCount |"
    }
    $report += ""
}

# Footer
$report += "---"
$report += "*Generated by Issue Intelligence System v1.0.0*"
$report += "*Next: run ``Sync-Issues.ps1`` to refresh, ``Analyze-Issues.ps1`` to review, ``Update-Status.ps1`` to transition*"

# --- Output ---
$reportText = $report -join "`n"

if ($NoSave) {
    Write-Host $reportText
} else {
    if (!(Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
    }
    $reportPath = Join-Path $OutputDir "${reportDate}_sync.md"

    # If report for today already exists, append timestamp
    if (Test-Path $reportPath) {
        $reportPath = Join-Path $OutputDir "${reportDate}_$($now.ToString('HHmm'))_sync.md"
    }

    $reportText | Out-File -Encoding utf8 $reportPath
    Write-Host "Report saved to: $reportPath" -ForegroundColor Green
    Write-Host ""
    Write-Host "Quick stats:" -ForegroundColor Cyan
    Write-Host "  Total tracked:    $($allIssues.Count)"
    Write-Host "  Urgent:           $($urgent.Count)" -ForegroundColor $(if ($urgent.Count -gt 0) { "Red" } else { "Gray" })
    Write-Host "  Iteration needed: $($iteration.Count)" -ForegroundColor $(if ($iteration.Count -gt 0) { "Magenta" } else { "Gray" })
    Write-Host "  Waiting for us:   $($waitingForUs.Count)" -ForegroundColor $(if ($waitingForUs.Count -gt 0) { "Yellow" } else { "Gray" })
    Write-Host "  New (triage):     $($newIssues.Count)" -ForegroundColor $(if ($newIssues.Count -gt 0) { "Green" } else { "Gray" })
}

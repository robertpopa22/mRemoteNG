<#
.SYNOPSIS
    Issue Intelligence System - Status transition script.
    Updates issue lifecycle status, adds iteration entries, optionally posts GitHub comments.

.DESCRIPTION
    Transitions an issue through the lifecycle:
      new -> triaged -> roadmap -> in-progress -> testing -> released
    Also supports: wontfix, duplicate, user-feedback (iteration loop).
    Optionally posts a templated comment on the GitHub issue.

.PARAMETER Issue
    Issue number to update.

.PARAMETER Repo
    Which repo: "upstream" (default) or "fork".

.PARAMETER Status
    New lifecycle status.

.PARAMETER Description
    Description for the iteration entry (what changed, why).

.PARAMETER PR
    PR number associated with this transition.

.PARAMETER Branch
    Branch name for in-progress transitions.

.PARAMETER Release
    Release tag for released transitions.

.PARAMETER ReleaseUrl
    Release download URL.

.PARAMETER PostComment
    Post a templated comment to GitHub. Default: prompt for confirmation.

.PARAMETER SkipComment
    Skip posting any GitHub comment.

.PARAMETER Priority
    Set priority level (P0-critical, P1-security, P2-bug, P3-enhancement, P4-debt).

.PARAMETER Notes
    Add/update notes for the issue.

.PARAMETER AddToRoadmap
    Add this issue to _roadmap.json.

.EXAMPLE
    # Triage a new issue
    .\Update-Status.ps1 -Issue 3044 -Status triaged -Priority P2-bug

    # Start working on it
    .\Update-Status.ps1 -Issue 3044 -Status in-progress -Branch "fix/3044-comma-split" -Description "Working on .cmd batch comma escaping"

    # Mark as testing
    .\Update-Status.ps1 -Issue 3044 -Status testing -PR 3150 -Description "PR #3150 merged, batch comma fix"

    # User reports it's not fully fixed (iteration loop)
    .\Update-Status.ps1 -Issue 3044 -Status in-progress -Description "User feedback: still broken with & character"

    # Mark as released
    .\Update-Status.ps1 -Issue 3044 -Status released -Release "v1.80.0" -ReleaseUrl "https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.80.0"
#>
param(
    [Parameter(Mandatory)]
    [int]$Issue,

    [ValidateSet("upstream", "fork")]
    [string]$Repo = "upstream",

    [Parameter(Mandatory)]
    [ValidateSet("new", "triaged", "roadmap", "in-progress", "testing", "released", "wontfix", "duplicate")]
    [string]$Status,

    [string]$Description,

    [int]$PR,

    [string]$Branch,

    [string]$Release,

    [string]$ReleaseUrl,

    [switch]$PostComment,

    [switch]$SkipComment,

    [string]$Priority,

    [string]$Notes,

    [switch]$AddToRoadmap
)

$ErrorActionPreference = "Stop"
$DbRoot = Join-Path $PSScriptRoot "..\issues-db"
$MetaPath = Join-Path $DbRoot "_meta.json"
$RoadmapPath = Join-Path $DbRoot "_roadmap.json"

if (!(Test-Path $MetaPath)) {
    throw "Issue DB not initialized. Run Sync-Issues.ps1 first."
}

$meta = Get-Content $MetaPath -Raw | ConvertFrom-Json
$repoFullName = if ($Repo -eq "upstream") { $meta.repos.upstream } else { $meta.repos.fork }

# --- Load issue ---
$padded = $Issue.ToString().PadLeft(4, '0')
$filePath = Join-Path $DbRoot $Repo "$padded.json"

if (!(Test-Path $filePath)) {
    throw "Issue #$Issue not found in $Repo DB. Run Sync-Issues.ps1 first."
}

$issueData = Get-Content $filePath -Raw | ConvertFrom-Json
$oldStatus = $issueData.our_status

Write-Host "=== Issue #$Issue Status Update ===" -ForegroundColor Cyan
Write-Host "Title:      $($issueData.title)"
Write-Host "Old status: $oldStatus"
Write-Host "New status: $Status"
Write-Host ""

# --- Validate transition ---
$validTransitions = @{
    "new"         = @("triaged", "roadmap", "in-progress", "released", "wontfix", "duplicate")
    "triaged"     = @("roadmap", "in-progress", "wontfix", "duplicate")
    "roadmap"     = @("in-progress", "wontfix")
    "in-progress" = @("testing", "roadmap", "wontfix")
    "testing"     = @("released", "in-progress")  # in-progress = iteration loop (user feedback)
    "released"    = @("in-progress")               # re-open if user reports still broken
    "wontfix"     = @("new", "triaged")
    "duplicate"   = @("new", "triaged")
}

if ($validTransitions.ContainsKey($oldStatus)) {
    if ($Status -notin $validTransitions[$oldStatus]) {
        Write-Warning "Non-standard transition: $oldStatus -> $Status"
        Write-Warning "Standard transitions from '$oldStatus': $($validTransitions[$oldStatus] -join ', ')"
        $confirm = Read-Host "Continue anyway? (y/N)"
        if ($confirm -ne 'y') { return }
    }
}

# --- Detect iteration loop ---
$isIteration = ($oldStatus -in @("testing", "released")) -and ($Status -eq "in-progress")
if ($isIteration) {
    Write-Host ">> ITERATION LOOP detected: user feedback after fix, re-opening <<" -ForegroundColor Magenta
}

# --- Update issue fields ---
$issueData.our_status = $Status

if ($Priority) { $issueData.priority = $Priority }
if ($Branch) { $issueData.our_branch = $Branch }
if ($PR) { $issueData.our_pr = $PR }
if ($Release) { $issueData.target_release = $Release }
if ($Notes) { $issueData.notes = $Notes }

# --- Add iteration entry ---
$iterationType = $Status
if ($isIteration) { $iterationType = "iteration-reopen" }

$iterSeq = 1
if ($issueData.iterations -and $issueData.iterations.Count -gt 0) {
    $iterSeq = ($issueData.iterations | Measure-Object -Property seq -Maximum).Maximum + 1
}

$iterEntry = [ordered]@{
    seq         = $iterSeq
    date        = [datetime]::UtcNow.ToString("yyyy-MM-dd")
    type        = $iterationType
    description = if ($Description) { $Description } else { "Status changed to $Status" }
    pr          = if ($PR) { $PR } else { $null }
    branch      = if ($Branch) { $Branch } else { $null }
    release     = if ($Release) { $Release } else { $null }
    comment_posted = $false
}

# Ensure iterations is an array
if (-not $issueData.iterations) {
    $issueData | Add-Member -NotePropertyName "iterations" -NotePropertyValue @() -Force
}
$iters = [System.Collections.ArrayList]@($issueData.iterations)
$iters.Add($iterEntry) | Out-Null
$issueData.iterations = @($iters)

# --- Post GitHub comment (if requested) ---
$commentBody = $null
$templates = $meta.comment_templates

switch ($Status) {
    "roadmap" {
        $commentBody = $templates.triaged_to_roadmap
    }
    "in-progress" {
        if ($isIteration) {
            $commentBody = $templates.iteration_ack
        } else {
            $commentBody = $templates.in_progress
        }
    }
    "testing" {
        $prUrl = if ($PR) { "https://github.com/$repoFullName/pull/$PR" } else { "(PR pending)" }
        $commentBody = $templates.testing -replace '\{pr_url\}', $prUrl
    }
    "released" {
        $commentBody = $templates.released -replace '\{release_tag\}', $Release -replace '\{release_url\}', $ReleaseUrl
    }
}

if ($commentBody -and -not $SkipComment) {
    if ($PostComment) {
        Write-Host "Posting comment to #$Issue on $repoFullName..." -ForegroundColor Yellow
        Write-Host "--- Comment preview ---" -ForegroundColor DarkGray
        Write-Host $commentBody
        Write-Host "--- End preview ---" -ForegroundColor DarkGray

        try {
            gh issue comment $Issue --repo $repoFullName --body $commentBody 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host "Comment posted successfully." -ForegroundColor Green
                # Mark iteration as comment posted
                $issueData.iterations[-1].comment_posted = $true
            } else {
                Write-Warning "Failed to post comment (gh exit code: $LASTEXITCODE)"
            }
        } catch {
            Write-Warning "Error posting comment: $_"
        }
    } else {
        Write-Host "--- Suggested comment (not posted, use -PostComment to send) ---" -ForegroundColor DarkGray
        Write-Host $commentBody
        Write-Host "--- End suggestion ---" -ForegroundColor DarkGray
    }
}

# --- Update roadmap ---
if ($AddToRoadmap -or $Status -eq "roadmap") {
    if (Test-Path $RoadmapPath) {
        $roadmap = Get-Content $RoadmapPath -Raw | ConvertFrom-Json

        # Check if already in roadmap
        $existingItem = $roadmap.items | Where-Object { $_.number -eq $Issue -and $_.repo -eq $repoFullName }
        if (-not $existingItem) {
            $roadmapItem = [ordered]@{
                number         = $Issue
                repo           = $repoFullName
                title          = $issueData.title
                priority       = $issueData.priority
                our_status     = $Status
                added_date     = [datetime]::UtcNow.ToString("yyyy-MM-dd")
                target_release = $issueData.target_release
            }

            $items = [System.Collections.ArrayList]@($roadmap.items)
            $items.Add($roadmapItem) | Out-Null
            $roadmap.items = @($items)
            $roadmap.last_updated = [datetime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            $roadmap | ConvertTo-Json -Depth 10 | Out-File -Encoding utf8 $RoadmapPath
            Write-Host "Added to roadmap." -ForegroundColor Green
        } else {
            # Update existing roadmap entry
            $existingItem.our_status = $Status
            $existingItem.priority = $issueData.priority
            $roadmap.last_updated = [datetime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            $roadmap | ConvertTo-Json -Depth 10 | Out-File -Encoding utf8 $RoadmapPath
            Write-Host "Updated roadmap entry." -ForegroundColor Green
        }
    }
}

# --- Save issue JSON ---
$issueData.last_synced = [datetime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
$issueData | ConvertTo-Json -Depth 10 | Out-File -Encoding utf8 $filePath

Write-Host ""
Write-Host "Issue #$Issue updated: $oldStatus -> $Status" -ForegroundColor Green
if ($isIteration) {
    Write-Host "Iteration #$iterSeq recorded (feedback loop)" -ForegroundColor Magenta
}
Write-Host "File: $filePath" -ForegroundColor DarkGray

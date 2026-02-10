<#
.SYNOPSIS
    Issue Intelligence System - Sync script.
    Fetches issues and comments from upstream + fork repos via gh CLI,
    compares with local JSON database, creates/updates per-issue files.

.DESCRIPTION
    MANDATORY: Run this before any release, triage, or roadmap session.
    Scans both repos for new issues, new comments, and updated comments.
    Updates .project-roadmap/issues-db/ JSON files (git-tracked).

.PARAMETER Repos
    Which repos to sync: "both" (default), "upstream", or "fork".

.PARAMETER IssueNumbers
    Optional array of specific issue numbers to sync (faster for targeted updates).

.PARAMETER MaxIssues
    Max issues to fetch per repo (default 1000).

.PARAMETER IncludeClosed
    Include closed issues (default: open only).

.PARAMETER Verbose
    Show detailed progress output.

.EXAMPLE
    # Full sync of both repos
    .\Sync-Issues.ps1

    # Sync only upstream
    .\Sync-Issues.ps1 -Repos upstream

    # Sync specific issues
    .\Sync-Issues.ps1 -IssueNumbers 3044,3069

    # Include closed issues
    .\Sync-Issues.ps1 -IncludeClosed
#>
param(
    [ValidateSet("both", "upstream", "fork")]
    [string]$Repos = "both",

    [int[]]$IssueNumbers = @(),

    [int]$MaxIssues = 1000,

    [switch]$IncludeClosed,

    [switch]$VerboseOutput
)

$ErrorActionPreference = "Stop"
$DbRoot = Join-Path $PSScriptRoot "..\issues-db"
$MetaPath = Join-Path $DbRoot "_meta.json"

# --- Validate prerequisites ---
if (!(Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "gh CLI not found in PATH. Install from https://cli.github.com/"
}

if (!(Test-Path $MetaPath)) {
    throw "Issue DB not initialized. Missing: $MetaPath"
}

$meta = Get-Content $MetaPath -Raw | ConvertFrom-Json
$upstreamRepo = $meta.repos.upstream
$forkRepo = $meta.repos.fork
$ourUser = $meta.our_github_user
$syncStart = [datetime]::UtcNow

Write-Host "=== Issue Intelligence System - Sync ===" -ForegroundColor Cyan
Write-Host "Time: $($syncStart.ToString('yyyy-MM-dd HH:mm:ss')) UTC"
Write-Host "Repos: $Repos"
if ($IssueNumbers.Count -gt 0) {
    Write-Host "Targeted issues: $($IssueNumbers -join ', ')"
}
Write-Host ""

# --- Helper: ensure directory exists ---
function Ensure-Dir([string]$path) {
    if (!(Test-Path $path)) {
        New-Item -ItemType Directory -Force -Path $path | Out-Null
    }
}

# --- Helper: create or update issue JSON ---
function Process-Issue {
    param(
        [object]$ghIssue,
        [string]$repoKey,
        [string]$repoFullName
    )

    $num = $ghIssue.number
    $padded = $num.ToString().PadLeft(4, '0')
    $dir = Join-Path $DbRoot $repoKey
    Ensure-Dir $dir
    $filePath = Join-Path $dir "$padded.json"

    # Fetch full issue with comments
    if ($VerboseOutput) { Write-Host "  Fetching #$num comments..." -ForegroundColor DarkGray }

    $ghJson = gh issue view $num --repo $repoFullName --json number,title,state,labels,createdAt,updatedAt,body,author,comments 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "  Failed to fetch #$num from $repoFullName : $ghJson"
        return @{ status = "error"; number = $num }
    }
    $ghFull = $ghJson | ConvertFrom-Json

    # Build comments array from GitHub data
    $ghComments = @()
    if ($ghFull.comments) {
        foreach ($c in $ghFull.comments) {
            $cSnippet = $c.body
            if ($c.body.Length -gt 500) { $cSnippet = $c.body.Substring(0, 500) + "..." }
            $cIsOurs = ($c.author.login -eq $ourUser)
            $ghComments += @{
                id            = $c.id
                author        = $c.author.login
                date          = $c.createdAt
                snippet       = $cSnippet
                is_ours       = $cIsOurs
                analyzed      = $false
                action_needed = $false
            }
        }
    }

    # Check if existing JSON exists
    $isNew = $false
    $newCommentCount = 0
    $existing = $null

    if (Test-Path $filePath) {
        $existing = Get-Content $filePath -Raw | ConvertFrom-Json

        # Detect new comments by comparing IDs
        $existingIds = @()
        if ($existing.comments) {
            $existingIds = @($existing.comments | ForEach-Object { $_.id })
        }
        foreach ($gc in $ghComments) {
            if ($gc.id -notin $existingIds) {
                $newCommentCount++
                # New comment - mark as not analyzed, possibly needing action
                $gc.analyzed = $false
                $gc.action_needed = -not $gc.is_ours
            } else {
                # Existing comment - preserve analyzed/action_needed from DB
                $existingComment = $existing.comments | Where-Object { $_.id -eq $gc.id }
                if ($existingComment) {
                    $gc.analyzed = $existingComment.analyzed
                    $gc.action_needed = $existingComment.action_needed
                }
            }
        }
    } else {
        $isNew = $true
        $newCommentCount = $ghComments.Count
        # Mark all non-our comments as needing analysis
        foreach ($gc in $ghComments) {
            $gc.action_needed = -not $gc.is_ours
        }
    }

    # Build labels array
    $labels = @()
    if ($ghFull.labels) {
        $labels = @($ghFull.labels | ForEach-Object { $_.name })
    }

    # Determine needs_action: any un-analyzed non-our comments, or new issue
    $unreadCount = ($ghComments | Where-Object { -not $_.analyzed -and -not $_.is_ours }).Count
    $needsAction = ($unreadCount -gt 0) -or $isNew

    # Determine if last comment is from someone else (they're waiting for us)
    $lastComment = $ghComments | Select-Object -Last 1
    $waitingForUs = $false
    if ($lastComment -and -not $lastComment.is_ours) {
        $waitingForUs = $true
    }

    # Pre-compute conditional values before building hashtable
    $bodySnippet = $ghFull.body
    if ($ghFull.body -and $ghFull.body.Length -gt 500) {
        $bodySnippet = $ghFull.body.Substring(0, 500) + "..."
    }

    $prevStatus = "new"
    if ($existing -and $existing.our_status) { $prevStatus = $existing.our_status }

    $prevPriority = $null
    if ($existing -and $existing.priority) { $prevPriority = $existing.priority }

    $prevTargetRelease = $null
    if ($existing -and $existing.target_release) { $prevTargetRelease = $existing.target_release }

    $prevBranch = $null
    if ($existing -and $existing.our_branch) { $prevBranch = $existing.our_branch }

    $prevPR = $null
    if ($existing -and $existing.our_pr) { $prevPR = $existing.our_pr }

    $prevIterations = @()
    if ($existing -and $existing.iterations) { $prevIterations = @($existing.iterations) }

    $prevNotes = ""
    if ($existing -and $existing.notes) { $prevNotes = $existing.notes }

    # Build issue object
    $issueObj = [ordered]@{
        number             = $num
        repo               = $repoFullName
        title              = $ghFull.title
        state              = $ghFull.state.ToLower()
        labels             = $labels
        author             = $ghFull.author.login
        created_at         = $ghFull.createdAt
        github_updated_at  = $ghFull.updatedAt
        body_snippet       = $bodySnippet
        our_status         = $prevStatus
        priority           = $prevPriority
        target_release     = $prevTargetRelease
        our_branch         = $prevBranch
        our_pr             = $prevPR
        iterations         = $prevIterations
        comments           = $ghComments
        comments_cursor    = $ghFull.updatedAt
        unread_comments    = $unreadCount
        needs_action       = $needsAction
        waiting_for_us     = $waitingForUs
        last_synced        = $syncStart.ToString("yyyy-MM-ddTHH:mm:ssZ")
        notes              = $prevNotes
    }

    # Write JSON
    $issueObj | ConvertTo-Json -Depth 10 | Out-File -Encoding utf8 $filePath

    $resultStatus = "updated"
    if ($isNew) { $resultStatus = "new" }

    return @{
        status          = $resultStatus
        number          = $num
        new_comments    = $newCommentCount
        needs_action    = $needsAction
        waiting_for_us  = $waitingForUs
    }
}

# --- Main sync logic ---
$stats = @{
    repos_synced    = @()
    issues_new      = 0
    issues_updated  = 0
    issues_error    = 0
    comments_new    = 0
    needs_action    = 0
    waiting_for_us  = 0
}

$reposToSync = @()
if ($Repos -eq "both" -or $Repos -eq "upstream") {
    $reposToSync += @{ key = "upstream"; name = $upstreamRepo }
}
if ($Repos -eq "both" -or $Repos -eq "fork") {
    $reposToSync += @{ key = "fork"; name = $forkRepo }
}

foreach ($repo in $reposToSync) {
    $repoKey = $repo.key
    $repoName = $repo.name
    Write-Host "--- Syncing: $repoName ($repoKey) ---" -ForegroundColor Yellow

    if ($IssueNumbers.Count -gt 0) {
        # Targeted sync - fetch specific issues
        $issues = @()
        foreach ($num in $IssueNumbers) {
            $issues += @{ number = $num }
        }
    } else {
        # Full sync - fetch issue list
        $stateArg = $(if ($IncludeClosed) { "all" } else { "open" })
        Write-Host "  Fetching issue list (state=$stateArg, limit=$MaxIssues)..."
        $listJson = gh issue list --repo $repoName --state $stateArg --limit $MaxIssues --json number,title,updatedAt 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "  Failed to list issues from $repoName : $listJson"
            continue
        }
        $issues = $listJson | ConvertFrom-Json
        Write-Host "  Found $($issues.Count) issues" -ForegroundColor Green
    }

    $processed = 0
    foreach ($issue in $issues) {
        $processed++
        $pct = [math]::Floor(($processed / [math]::Max($issues.Count, 1)) * 100)
        Write-Host "  [$pct%] Processing #$($issue.number)..." -NoNewline

        $result = Process-Issue -ghIssue $issue -repoKey $repoKey -repoFullName $repoName

        switch ($result.status) {
            "new" {
                $stats.issues_new++
                Write-Host " NEW" -ForegroundColor Green -NoNewline
            }
            "updated" {
                $stats.issues_updated++
                Write-Host " updated" -ForegroundColor DarkGray -NoNewline
            }
            "error" {
                $stats.issues_error++
                Write-Host " ERROR" -ForegroundColor Red -NoNewline
            }
        }

        if ($result.new_comments -gt 0) {
            $stats.comments_new += $result.new_comments
            Write-Host " (+$($result.new_comments) comments)" -ForegroundColor Cyan -NoNewline
        }

        if ($result.waiting_for_us) {
            $stats.waiting_for_us++
            Write-Host " [WAITING FOR US]" -ForegroundColor Red -NoNewline
        } elseif ($result.needs_action) {
            $stats.needs_action++
            Write-Host " [needs action]" -ForegroundColor Yellow -NoNewline
        }

        Write-Host ""
    }

    $stats.repos_synced += $repoName
    Write-Host ""
}

# --- Update _meta.json ---
$meta.last_sync = $syncStart.ToString("yyyy-MM-ddTHH:mm:ssZ")
$meta.last_sync_stats = [ordered]@{
    repos_synced    = $stats.repos_synced
    issues_new      = $stats.issues_new
    issues_updated  = $stats.issues_updated
    issues_error    = $stats.issues_error
    comments_new    = $stats.comments_new
    needs_action    = $stats.needs_action
    waiting_for_us  = $stats.waiting_for_us
    duration_sec    = [math]::Round(([datetime]::UtcNow - $syncStart).TotalSeconds, 1)
}
$meta | ConvertTo-Json -Depth 10 | Out-File -Encoding utf8 $MetaPath

# --- Summary ---
Write-Host "=== Sync Complete ===" -ForegroundColor Cyan
Write-Host "Duration: $([math]::Round(([datetime]::UtcNow - $syncStart).TotalSeconds, 1))s"
$colorNew = if ($stats.issues_new -gt 0) { "Green" } else { "Gray" }
$colorComments = if ($stats.comments_new -gt 0) { "Cyan" } else { "Gray" }
$colorErrors = if ($stats.issues_error -gt 0) { "Red" } else { "Gray" }
Write-Host "New issues:      $($stats.issues_new)" -ForegroundColor $colorNew
Write-Host "Updated issues:  $($stats.issues_updated)"
Write-Host "New comments:    $($stats.comments_new)" -ForegroundColor $colorComments
Write-Host "Errors:          $($stats.issues_error)" -ForegroundColor $colorErrors
Write-Host ""

if ($stats.waiting_for_us -gt 0) {
    Write-Host "!! $($stats.waiting_for_us) issues WAITING FOR OUR RESPONSE !!" -ForegroundColor Red
}
if ($stats.needs_action -gt 0) {
    Write-Host ">> $($stats.needs_action) issues need action (run Analyze-Issues.ps1)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Next step: .\Analyze-Issues.ps1" -ForegroundColor DarkGray

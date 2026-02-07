[CmdletBinding()]
param(
    [string]$Repo = "mRemoteNG/mRemoteNG",
    [string]$OutputDir = "D:\github\mRemoteNG\NEXTUP",
    [string]$AnalysisDir = "D:\github\LOCAL\analysis\mRemoteNG",
    [int]$P2BatchSize = 25,
    [int]$P5Top = 30
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$gh = "D:\github\LOCAL\tools\gh\bin\gh.exe"
if (-not (Test-Path $gh)) {
    throw "gh executable not found at $gh"
}

function Has-Label {
    param(
        [Parameter(Mandatory = $true)]$Issue,
        [Parameter(Mandatory = $true)][string]$LabelName
    )

    foreach ($label in $Issue.labels) {
        if ($label.name -eq $LabelName) {
            return $true
        }
    }

    return $false
}

function To-IssueRow {
    param(
        [Parameter(Mandatory = $true)]$Issue
    )

    $labelNames = @()
    foreach ($label in $Issue.labels) {
        $labelNames += $label.name
    }

    [PSCustomObject]@{
        number    = [int]$Issue.number
        title     = [string]$Issue.title
        url       = [string]$Issue.url
        updatedAt = [string]$Issue.updatedAt
        createdAt = [string]$Issue.createdAt
        labels    = $labelNames
    }
}

$cutoff = (Get-Date).AddDays(-365)
$todayStamp = (Get-Date).ToString("yyyy-MM-dd")

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
New-Item -ItemType Directory -Path $AnalysisDir -Force | Out-Null
$packageAnalysisDir = Join-Path $AnalysisDir "packages"
New-Item -ItemType Directory -Path $packageAnalysisDir -Force | Out-Null

Write-Host "Fetching open issues from $Repo..."
$issuesRaw = & $gh issue list --repo $Repo --state open --limit 1000 --json number,title,url,updatedAt,createdAt,labels
if ($LASTEXITCODE -ne 0) {
    throw "Failed to fetch issue list."
}

$issues = $issuesRaw | ConvertFrom-Json

$p1Duplicate = @($issues | Where-Object { Has-Label -Issue $_ -LabelName "duplicate" })

$p2Need2Check = @($issues | Where-Object { Has-Label -Issue $_ -LabelName "Need 2 check" })
$p2Need2CheckStale = @($p2Need2Check | Where-Object { [datetime]$_.updatedAt -lt $cutoff } | Sort-Object { [datetime]$_.updatedAt })
$p2Batch1 = @($p2Need2CheckStale | Select-Object -First $P2BatchSize)

$p3InProgress = @($issues | Where-Object { Has-Label -Issue $_ -LabelName "In progress" })
$p3InDevelopment = @($issues | Where-Object { Has-Label -Issue $_ -LabelName "In development" })
$p3InProgressStale = @($p3InProgress | Where-Object { [datetime]$_.updatedAt -lt $cutoff } | Sort-Object { [datetime]$_.updatedAt })
$p3InDevelopmentStale = @($p3InDevelopment | Where-Object { [datetime]$_.updatedAt -lt $cutoff } | Sort-Object { [datetime]$_.updatedAt })

$versionLabelRegex = "^(1\.\d+(\.\d+)?|1\.8 \(Fenix\)|v1\.)"
$p4VersionDebt = @()
$versionLabelCounts = @{}

foreach ($issue in $issues) {
    $issueHasVersionLabel = $false
    foreach ($label in $issue.labels) {
        if ($label.name -match $versionLabelRegex) {
            $issueHasVersionLabel = $true
            if (-not $versionLabelCounts.ContainsKey($label.name)) {
                $versionLabelCounts[$label.name] = 0
            }
            $versionLabelCounts[$label.name] += 1
        }
    }

    if ($issueHasVersionLabel) {
        $p4VersionDebt += $issue
    }
}

$p5ExcludeLabels = @(
    "duplicate",
    "wontfix",
    "invalid",
    "question",
    "documentation",
    "good first issue",
    "website"
)

$p5PriorityLabels = @(
    "bug",
    "regression",
    "critical",
    "security",
    "crash",
    "high priority",
    "high"
)

$p5TitleRegex = "(?i)crash|password|upgrade|telnet|mysql|rdp|ssh|locking|error|fail"

$p5Candidates = @(
    $issues | Where-Object {
        $labelNames = @($_.labels | ForEach-Object { $_.name.ToLowerInvariant() })

        foreach ($excluded in $p5ExcludeLabels) {
            if ($labelNames -contains $excluded) {
                return $false
            }
        }

        foreach ($priority in $p5PriorityLabels) {
            if ($labelNames -contains $priority) {
                return $true
            }
        }

        return ($_.title -match $p5TitleRegex)
    } | Sort-Object { [datetime]$_.updatedAt } -Descending | Select-Object -First $P5Top
)

$p1DuplicateRows = @($p1Duplicate | ForEach-Object { To-IssueRow -Issue $_ })
$p2Need2CheckRows = @($p2Need2Check | ForEach-Object { To-IssueRow -Issue $_ })
$p2Need2CheckStaleRows = @($p2Need2CheckStale | ForEach-Object { To-IssueRow -Issue $_ })
$p2Batch1Rows = @($p2Batch1 | ForEach-Object { To-IssueRow -Issue $_ })
$p3InProgressRows = @($p3InProgress | ForEach-Object { To-IssueRow -Issue $_ })
$p3InDevelopmentRows = @($p3InDevelopment | ForEach-Object { To-IssueRow -Issue $_ })
$p3InProgressStaleRows = @($p3InProgressStale | ForEach-Object { To-IssueRow -Issue $_ })
$p3InDevelopmentStaleRows = @($p3InDevelopmentStale | ForEach-Object { To-IssueRow -Issue $_ })
$p4VersionDebtRows = @($p4VersionDebt | ForEach-Object { To-IssueRow -Issue $_ })
$p5CandidateRows = @($p5Candidates | ForEach-Object { To-IssueRow -Issue $_ })

$p1DuplicateRows | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $packageAnalysisDir "p1_duplicate_open.json")
$p2Need2CheckRows | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $packageAnalysisDir "p2_need2check_open.json")
$p2Need2CheckStaleRows | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $packageAnalysisDir "p2_need2check_stale.json")
$p2Batch1Rows | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $packageAnalysisDir "p2_batch1.json")
$p3InProgressRows | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $packageAnalysisDir "p3_inprogress_open.json")
$p3InDevelopmentRows | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $packageAnalysisDir "p3_indevelopment_open.json")
$p3InProgressStaleRows | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $packageAnalysisDir "p3_inprogress_stale.json")
$p3InDevelopmentStaleRows | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $packageAnalysisDir "p3_indevelopment_stale.json")
$p4VersionDebtRows | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $packageAnalysisDir "p4_version_debt.json")
$p5CandidateRows | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $packageAnalysisDir "p5_release_candidates.json")

$versionCountRows = @()
foreach ($labelName in ($versionLabelCounts.Keys | Sort-Object)) {
    $versionCountRows += [PSCustomObject]@{
        label = $labelName
        count = [int]$versionLabelCounts[$labelName]
    }
}

$versionCountRows | ConvertTo-Json -Depth 3 | Set-Content -Path (Join-Path $packageAnalysisDir "p4_version_label_counts.json")

$reportPath = Join-Path $OutputDir ("P1_P5_EXECUTION_{0}.md" -f $todayStamp)
$report = [System.Collections.Generic.List[string]]::new()

$report.Add("# P1-P5 Execution Snapshot")
$report.Add("")
$report.Add("Date: $todayStamp")
$report.Add("Repo: $Repo")
$report.Add("Cutoff for stale triage: $($cutoff.ToString('yyyy-MM-dd'))")
$report.Add("")
$report.Add("## Summary")
$report.Add("")
$report.Add("- P1 duplicate open: $($p1DuplicateRows.Count)")
$report.Add("- P2 need2check open: $($p2Need2CheckRows.Count)")
$report.Add("- P2 need2check stale (>365d): $($p2Need2CheckStaleRows.Count)")
$report.Add("- P3 in progress open: $($p3InProgressRows.Count)")
$report.Add("- P3 in progress stale (>365d): $($p3InProgressStaleRows.Count)")
$report.Add("- P3 in development open: $($p3InDevelopmentRows.Count)")
$report.Add("- P3 in development stale (>365d): $($p3InDevelopmentStaleRows.Count)")
$report.Add("- P4 version debt open: $($p4VersionDebtRows.Count)")
$report.Add("- P5 release candidates (top): $($p5CandidateRows.Count)")
$report.Add("")

$report.Add("## P1 - Duplicate (open)")
$report.Add("")
foreach ($issue in ($p1DuplicateRows | Sort-Object number)) {
    $report.Add(("- #{0} - {1} ({2})" -f $issue.number, $issue.title, $issue.url))
}
$report.Add("")

$report.Add("## P2 - Need 2 check (Batch 1)")
$report.Add("")
foreach ($issue in $p2Batch1Rows) {
    $report.Add(("- #{0} - {1} | updated {2} | {3}" -f $issue.number, $issue.title, ([datetime]$issue.updatedAt).ToString("yyyy-MM-dd"), $issue.url))
}
$report.Add("")

$report.Add("## P3 - Stale in-progress labels")
$report.Add("")
$report.Add("### In progress stale")
foreach ($issue in ($p3InProgressStaleRows | Select-Object -First 40)) {
    $report.Add(("- #{0} - {1} | updated {2} | {3}" -f $issue.number, $issue.title, ([datetime]$issue.updatedAt).ToString("yyyy-MM-dd"), $issue.url))
}
$report.Add("")
$report.Add("### In development stale")
foreach ($issue in ($p3InDevelopmentStaleRows | Select-Object -First 40)) {
    $report.Add(("- #{0} - {1} | updated {2} | {3}" -f $issue.number, $issue.title, ([datetime]$issue.updatedAt).ToString("yyyy-MM-dd"), $issue.url))
}
$report.Add("")

$report.Add("## P4 - Version label debt")
$report.Add("")
foreach ($row in ($versionCountRows | Sort-Object count -Descending)) {
    $report.Add(("- `{0}`: {1}" -f $row.label, $row.count))
}
$report.Add("")

$report.Add("## P5 - Release stabilization candidates")
$report.Add("")
foreach ($issue in $p5CandidateRows) {
    $report.Add(("- #{0} - {1} | updated {2} | {3}" -f $issue.number, $issue.title, ([datetime]$issue.updatedAt).ToString("yyyy-MM-dd"), $issue.url))
}
$report.Add("")
$report.Add("## Next Moves")
$report.Add("")
$report.Add("1. P1: attempt duplicate closure/cross-linking where rights allow.")
$report.Add("2. P2: run triage comments on batch 1 and classify fixed/duplicate/cannot-repro.")
$report.Add("3. P3: relabel stale in-progress issues to Need 2 check where applicable.")
$report.Add("4. P4: prepare label policy note for modern version taxonomy.")
$report.Add("5. P5: pick top 3 fixable bugs and open dedicated implementation branches.")

$report | Set-Content -Path $reportPath

Write-Host "Wrote report: $reportPath"
Write-Host "Wrote JSON snapshots under: $packageAnalysisDir"

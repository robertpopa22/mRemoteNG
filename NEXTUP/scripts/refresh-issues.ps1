param(
    [string]$Repo = "mRemoteNG/mRemoteNG",
    [string]$OutputDir = "D:\github\LOCAL\analysis\mRemoteNG"
)

$ErrorActionPreference = "Stop"

if (!(Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "gh CLI not found in PATH. Run D:\github\LOCAL\env.cmd first."
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$snapshotPath = Join-Path $OutputDir "open_issues_snapshot.json"
$summaryPath = Join-Path $OutputDir "open_issues_summary.txt"
$packagesPath = Join-Path $OutputDir "issue_packages_preview.txt"

Write-Host "Fetching open issue snapshot from $Repo ..."
$json = gh issue list --repo $Repo --state open --limit 1000 --json number,title,labels,updatedAt,url
$issues = $json | ConvertFrom-Json
$issues | ConvertTo-Json -Depth 10 | Out-File -Encoding utf8 $snapshotPath

$cut365 = [datetime]"2025-02-07T00:00:00Z"
$cut730 = [datetime]"2024-02-07T00:00:00Z"

function CountLabel([string]$label) {
    return ($issues | Where-Object { $_.labels.name -contains $label }).Count
}

$total = $issues.Count
$stale365 = ($issues | Where-Object { [datetime]$_.updatedAt -lt $cut365 }).Count
$stale730 = ($issues | Where-Object { [datetime]$_.updatedAt -lt $cut730 }).Count
$need2 = CountLabel "Need 2 check"
$need2Stale365 = ($issues | Where-Object { ($_.labels.name -contains "Need 2 check") -and ([datetime]$_.updatedAt -lt $cut365) }).Count
$inDev = CountLabel "In development"
$inProgress = CountLabel "In progress"
$duplicate = CountLabel "Duplicate"
$security = CountLabel "Security"
$critical = CountLabel "critical"

@(
    "repo=$Repo"
    "generated_utc=$([datetime]::UtcNow.ToString("s"))Z"
    "open_total=$total"
    "stale_365=$stale365"
    "stale_730=$stale730"
    "need_2_check=$need2"
    "need_2_check_stale_365=$need2Stale365"
    "in_development=$inDev"
    "in_progress=$inProgress"
    "duplicate=$duplicate"
    "security=$security"
    "critical=$critical"
) | Out-File -Encoding ascii $summaryPath

$criticalIssues = $issues |
    Where-Object { $_.labels.name -contains "critical" } |
    Sort-Object { [datetime]$_.updatedAt }

$duplicateIssues = $issues |
    Where-Object { $_.labels.name -contains "Duplicate" } |
    Sort-Object { [datetime]$_.updatedAt }

$need2StalePreview = $issues |
    Where-Object { ($_.labels.name -contains "Need 2 check") -and ([datetime]$_.updatedAt -lt $cut365) } |
    Sort-Object { [datetime]$_.updatedAt } |
    Select-Object -First 30

$content = @()
$content += "=== PACKAGE P0 CRITICAL ==="
$content += ($criticalIssues | ForEach-Object { "#$($_.number) | $($_.updatedAt) | $($_.title)" })
$content += ""
$content += "=== PACKAGE P1 DUPLICATE ==="
$content += ($duplicateIssues | ForEach-Object { "#$($_.number) | $($_.updatedAt) | $($_.title)" })
$content += ""
$content += "=== PACKAGE P2 NEED2CHECK STALE PREVIEW (30) ==="
$content += ($need2StalePreview | ForEach-Object { "#$($_.number) | $($_.updatedAt) | $($_.title)" })

$content | Out-File -Encoding utf8 $packagesPath

Write-Host "Done."
Write-Host "Snapshot : $snapshotPath"
Write-Host "Summary  : $summaryPath"
Write-Host "Packages : $packagesPath"

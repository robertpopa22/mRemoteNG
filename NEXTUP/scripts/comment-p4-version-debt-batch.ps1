param(
    [int]$Skip = 0,
    [int]$Take = 25,
    [int]$MinDaysSinceUpdate = 180,
    [string]$Repo = "mRemoteNG/mRemoteNG",
    [switch]$Comment,
    [string]$GhExe = "C:\Progra~1\GitHub~1\gh.exe",
    [ValidateSet("updatedAt", "createdAt", "number")]
    [string]$SortBy = "updatedAt",
    [string]$Message = "Triage refresh (P4 version-label debt): this issue is tagged with legacy version labels. Please retest on the latest build (fork v1.79.x or upstream dev), then share reproducible steps and expected vs actual behavior. If still valid, keep open and relabel to current triage scope (for example Need 2 check). If fixed, close with resolution."
)

if (-not (Test-Path $GhExe))
{
    throw "gh executable not found at $GhExe"
}

$versionLabelRegex = "^(1\.\d+(\.\d+)?|1\.8 \(Fenix\)|v1\.)"
$cutoff = (Get-Date).AddDays(-1 * [math]::Abs($MinDaysSinceUpdate))

function Get-VersionLabelsForIssue
{
    param($Issue)

    $labels = @()
    foreach ($label in $Issue.labels)
    {
        if ($label.name -match $versionLabelRegex)
        {
            $labels += $label.name
        }
    }
    return $labels
}

$raw = & $GhExe issue list --repo $Repo --state open --limit 1000 --json number,title,url,updatedAt,createdAt,labels
$issues = $raw | ConvertFrom-Json

$versionIssues = @()
foreach ($issue in $issues)
{
    $versionLabels = Get-VersionLabelsForIssue -Issue $issue
    if ($versionLabels.Count -eq 0)
    {
        continue
    }

    if ([datetime]$issue.updatedAt -ge $cutoff)
    {
        continue
    }

    $versionIssues += [PSCustomObject]@{
        number        = [int]$issue.number
        title         = [string]$issue.title
        url           = [string]$issue.url
        createdAt     = [string]$issue.createdAt
        updatedAt     = [string]$issue.updatedAt
        versionLabels = $versionLabels
    }
}

$sorted = @($versionIssues | Sort-Object $SortBy)
$batch = @($sorted | Select-Object -Skip $Skip -First $Take)

if ($batch.Count -eq 0)
{
    Write-Output "No version-debt issues found for Skip=$Skip Take=$Take MinDaysSinceUpdate=$MinDaysSinceUpdate"
    exit 0
}

if (-not $Comment)
{
    $batch | ConvertTo-Json -Depth 5
    exit 0
}

foreach ($issue in $batch)
{
    $labelsCsv = ($issue.versionLabels -join ", ")
    $body = @"
$Message

Current legacy version labels: $labelsCsv
"@
    $commentUrl = & $GhExe issue comment $issue.number --repo $Repo --body $body
    Write-Output ("{0} {1}" -f $issue.number, $commentUrl)
}

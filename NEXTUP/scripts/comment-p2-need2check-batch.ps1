param(
    [int]$Skip = 0,
    [int]$Take = 25,
    [string]$Repo = "mRemoteNG/mRemoteNG",
    [switch]$Comment,
    [string]$GhExe = "C:\Progra~1\GitHub~1\gh.exe",
    [ValidateSet("createdAt", "updatedAt", "number")]
    [string]$SortBy = "createdAt",
    [string]$Message = "Triage refresh (P2): this issue has been inactive for a long time. Please retest on the latest build and share reproducible steps, expected vs actual behavior, and logs/screenshots. If there is no confirmation/update, maintainers can close it as stale or not reproducible."
)

if (-not (Test-Path $GhExe))
{
    throw "gh executable not found at $GhExe"
}

$raw = & $GhExe issue list --repo $Repo --state open --label "Need 2 check" --limit 500 --json number,title,url,updatedAt,createdAt
$issues = $raw | ConvertFrom-Json | Sort-Object $SortBy
$batch = $issues | Select-Object -Skip $Skip -First $Take

if (-not $batch -or $batch.Count -eq 0)
{
    Write-Output "No issues found for Skip=$Skip Take=$Take"
    exit 0
}

if (-not $Comment)
{
    $batch | ConvertTo-Json -Depth 4
    exit 0
}

foreach ($issue in $batch)
{
    $number = [int]$issue.number
    $url = & $GhExe issue comment $number --repo $Repo --body $Message
    Write-Output "$number $url"
}

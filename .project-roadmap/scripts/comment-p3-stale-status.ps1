param(
    [Parameter(Mandatory = $true)]
    [string]$JsonPath,
    [string]$Repo = "mRemoteNG/mRemoteNG",
    [string]$GhExe = "C:\Progra~1\GitHub~1\gh.exe",
    [string]$Message = "Triage (P3 stale status): this issue is labeled as actively worked, but activity appears stale. Please confirm whether work is still in progress. If not, please relabel to Need 2 check (or close) to keep status labels accurate."
)

if (-not (Test-Path $GhExe))
{
    throw "gh executable not found at $GhExe"
}

if (-not (Test-Path $JsonPath))
{
    throw "JSON file not found: $JsonPath"
}

$items = Get-Content -Path $JsonPath -Raw | ConvertFrom-Json
foreach ($item in $items)
{
    $number = [int]$item.number
    $url = & $GhExe issue comment $number --repo $Repo --body $Message
    Write-Output "$number $url"
}

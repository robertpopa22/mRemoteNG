[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Command,

    [Parameter(Mandatory = $true)]
    [ValidateSet("PASS", "FAIL", "PARTIAL")]
    [string]$Result,

    [Parameter(Mandatory = $true)]
    [string]$Resolution,

    [string]$Category = "general",
    [string]$ErrorPattern = "",
    [string]$Evidence = "",
    [double]$DurationSeconds = 0,
    [double]$LostTimeSeconds = -1
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Normalize-Cell([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    $normalized = $Value -replace "\r?\n", " "
    $normalized = $normalized -replace "\|", "\\|"
    return $normalized.Trim()
}

$nextupRoot = Split-Path -Parent $PSScriptRoot
$markdownLog = Join-Path $nextupRoot "COMMAND_FEEDBACK_LOG.md"
$jsonLog = Join-Path $nextupRoot "command-feedback.jsonl"

$now = Get-Date
$timestampDisplay = $now.ToString("yyyy-MM-dd HH:mm:ss")
$timestampIso = $now.ToString("s")

if ($DurationSeconds -lt 0) {
    throw "DurationSeconds must be >= 0."
}

if ($LostTimeSeconds -lt -1) {
    throw "LostTimeSeconds must be >= 0 or -1 for auto mode."
}

$effectiveLostSeconds = if ($LostTimeSeconds -ge 0) {
    $LostTimeSeconds
}
elseif ($Result -eq "PASS") {
    0
}
else {
    $DurationSeconds
}

if (-not (Test-Path $markdownLog)) {
    @"
# Command Feedback Log

Last updated: $($now.ToString("yyyy-MM-dd"))

| Timestamp | Category | Result | Command | Error Pattern | Resolution | Evidence | Duration(s) | Lost(s) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
"@ | Set-Content -Path $markdownLog -Encoding utf8
}

$safeCategory = Normalize-Cell $Category
$safeResult = Normalize-Cell $Result
$safeCommand = Normalize-Cell $Command
$safeErrorPattern = Normalize-Cell $ErrorPattern
$safeResolution = Normalize-Cell $Resolution
$safeEvidence = Normalize-Cell $Evidence
$safeDuration = [math]::Round($DurationSeconds, 2)
$safeLost = [math]::Round($effectiveLostSeconds, 2)

$commandCell = '`' + $safeCommand + '`'
$markdownLine = "| $timestampDisplay | $safeCategory | $safeResult | $commandCell | $safeErrorPattern | $safeResolution | $safeEvidence | $safeDuration | $safeLost |"
Add-Content -Path $markdownLog -Value $markdownLine -Encoding utf8

$entry = [ordered]@{
    timestamp = $timestampIso
    category = $Category
    result = $Result
    command = $Command
    error_pattern = $ErrorPattern
    resolution = $Resolution
    evidence = $Evidence
    duration_seconds = [math]::Round($DurationSeconds, 2)
    lost_time_seconds = [math]::Round($effectiveLostSeconds, 2)
}

$entry | ConvertTo-Json -Compress | Add-Content -Path $jsonLog -Encoding utf8

Write-Host "Logged command feedback: $Result [$Category]"

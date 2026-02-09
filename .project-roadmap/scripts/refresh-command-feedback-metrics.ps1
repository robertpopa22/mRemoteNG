[CmdletBinding()]
param(
    [int]$Days = 0,
    [int]$Top = 10,
    [string]$OutputPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Safe-Number([object]$Value) {
    if ($null -eq $Value) {
        return 0.0
    }

    $parsed = 0.0
    $ok = [double]::TryParse($Value.ToString(), [ref]$parsed)
    if ($ok) {
        return $parsed
    }

    return 0.0
}

function Get-OptionalValue([object]$Obj, [string]$Name) {
    if ($null -eq $Obj) {
        return $null
    }

    $property = $Obj.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Escape-Cell([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return ($Value -replace "\|", "\\|").Trim()
}

$nextupRoot = Split-Path -Parent $PSScriptRoot
$jsonLog = Join-Path $nextupRoot "command-feedback.jsonl"

if (-not (Test-Path $jsonLog)) {
    throw "Missing log file: $jsonLog"
}

$records = @()
Get-Content -Path $jsonLog | ForEach-Object {
    $line = $_
    if ([string]::IsNullOrWhiteSpace($line)) {
        return
    }

    try {
        $raw = $line | ConvertFrom-Json
    }
    catch {
        return
    }

    $timestamp = [datetime]::MinValue
    try {
        if ($raw.timestamp) {
            $timestamp = [datetime]::Parse($raw.timestamp.ToString())
        }
    }
    catch {
        $timestamp = [datetime]::MinValue
    }

    $durationRaw = Get-OptionalValue -Obj $raw -Name "duration_seconds"
    $lostRaw = Get-OptionalValue -Obj $raw -Name "lost_time_seconds"
    $resultRaw = Get-OptionalValue -Obj $raw -Name "result"

    $duration = Safe-Number $durationRaw
    $lost = if ($null -ne $lostRaw) {
        Safe-Number $lostRaw
    }
    elseif ($resultRaw -ne "PASS") {
        $duration
    }
    else {
        0.0
    }

    $records += [pscustomobject]@{
        Timestamp = $timestamp
        Category = [string]$raw.category
        Result = [string]$raw.result
        Command = [string]$raw.command
        ErrorPattern = [string]$raw.error_pattern
        Resolution = [string]$raw.resolution
        Evidence = [string]$raw.evidence
        DurationSeconds = $duration
        LostTimeSeconds = $lost
    }
}

if ($Days -gt 0) {
    $cutoff = (Get-Date).AddDays(-$Days)
    $records = $records | Where-Object { $_.Timestamp -ge $cutoff }
}

if (-not $records -or $records.Count -eq 0) {
    Write-Host "No command feedback data found for the selected window."
    exit 1
}

$total = @($records).Count
$passCount = @($records | Where-Object { $_.Result -eq "PASS" }).Count
$failCount = @($records | Where-Object { $_.Result -eq "FAIL" }).Count
$partialCount = @($records | Where-Object { $_.Result -eq "PARTIAL" }).Count

$totalDuration = [math]::Round((($records | Measure-Object -Property DurationSeconds -Sum).Sum), 2)
$totalLost = [math]::Round((($records | Measure-Object -Property LostTimeSeconds -Sum).Sum), 2)
$successRate = if ($total -gt 0) { [math]::Round((100.0 * $passCount / $total), 2) } else { 0.0 }

$errorGroups = @(
    $records |
        Where-Object { $_.Result -ne "PASS" } |
        ForEach-Object {
            $cat = if ([string]::IsNullOrWhiteSpace($_.Category)) { "uncategorized" } else { $_.Category.Trim() }
            $pat = if ([string]::IsNullOrWhiteSpace($_.ErrorPattern)) { "(no pattern)" } else { $_.ErrorPattern.Trim() }
            [pscustomobject]@{
                Key = "$cat :: $pat"
                DurationSeconds = $_.DurationSeconds
                LostTimeSeconds = $_.LostTimeSeconds
            }
        } |
        Group-Object -Property Key |
        ForEach-Object {
            $sumDuration = [math]::Round((($_.Group | Measure-Object -Property DurationSeconds -Sum).Sum), 2)
            $sumLost = [math]::Round((($_.Group | Measure-Object -Property LostTimeSeconds -Sum).Sum), 2)
            $count = $_.Count
            $avgLost = if ($count -gt 0) { [math]::Round($sumLost / $count, 2) } else { 0.0 }
            [pscustomobject]@{
                Key = $_.Name
                Count = $count
                TotalDurationSeconds = $sumDuration
                TotalLostSeconds = $sumLost
                AvgLostSeconds = $avgLost
            }
        } |
        Sort-Object -Property @{ Expression = "Count"; Descending = $true }, @{ Expression = "TotalLostSeconds"; Descending = $true } |
        Select-Object -First $Top
)

$categoryGroups = @(
    $records |
        Group-Object -Property Category |
        ForEach-Object {
            $name = if ([string]::IsNullOrWhiteSpace($_.Name)) { "uncategorized" } else { $_.Name }
            $sumDuration = [math]::Round((($_.Group | Measure-Object -Property DurationSeconds -Sum).Sum), 2)
            $sumLost = [math]::Round((($_.Group | Measure-Object -Property LostTimeSeconds -Sum).Sum), 2)
            [pscustomobject]@{
                Category = $name
                Count = $_.Count
                TotalDurationSeconds = $sumDuration
                TotalLostSeconds = $sumLost
            }
        } |
        Sort-Object -Property @{ Expression = "TotalLostSeconds"; Descending = $true }, @{ Expression = "Count"; Descending = $true } |
        Select-Object -First $Top
)

$commandTimeGroups = @(
    $records |
        Group-Object -Property Command |
        ForEach-Object {
            $name = if ([string]::IsNullOrWhiteSpace($_.Name)) { "(empty)" } else { $_.Name }
            $sumDuration = [math]::Round((($_.Group | Measure-Object -Property DurationSeconds -Sum).Sum), 2)
            $sumLost = [math]::Round((($_.Group | Measure-Object -Property LostTimeSeconds -Sum).Sum), 2)
            [pscustomobject]@{
                Command = $name
                Count = $_.Count
                TotalDurationSeconds = $sumDuration
                TotalLostSeconds = $sumLost
            }
        } |
        Sort-Object -Property @{ Expression = "TotalLostSeconds"; Descending = $true }, @{ Expression = "Count"; Descending = $true } |
        Select-Object -First $Top
)

$windowLabel = if ($Days -gt 0) { "last $Days day(s)" } else { "all history" }
$outFile = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    Join-Path $nextupRoot "COMMAND_FEEDBACK_METRICS.md"
}
else {
    $OutputPath
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Command Feedback Metrics")
$lines.Add("")
$lines.Add("Generated: $((Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))")
$lines.Add("Window: $windowLabel")
$lines.Add("")
$lines.Add("## Summary")
$lines.Add("")
$lines.Add("- Total entries: $total")
$lines.Add("- PASS: $passCount")
$lines.Add("- FAIL: $failCount")
$lines.Add("- PARTIAL: $partialCount")
$lines.Add("- Success rate: $successRate%")
$lines.Add("- Total execution time tracked: $totalDuration s")
$lines.Add("- Total lost time tracked: $totalLost s")
$lines.Add("")
$lines.Add("## Top Error Patterns (by frequency)")
$lines.Add("")
$lines.Add("| Error key | Count | Duration(s) | Lost(s) | Avg lost(s) |")
$lines.Add("| --- | --- | --- | --- | --- |")

if (@($errorGroups).Count -eq 0) {
    $lines.Add("| (none) | 0 | 0 | 0 | 0 |")
}
else {
    foreach ($row in $errorGroups) {
        $key = Escape-Cell $row.Key
        $lines.Add("| $key | $($row.Count) | $($row.TotalDurationSeconds) | $($row.TotalLostSeconds) | $($row.AvgLostSeconds) |")
    }
}

$lines.Add("")
$lines.Add("## Top Categories (by lost time)")
$lines.Add("")
$lines.Add("| Category | Count | Duration(s) | Lost(s) |")
$lines.Add("| --- | --- | --- | --- |")

foreach ($row in $categoryGroups) {
    $category = Escape-Cell $row.Category
    $lines.Add("| $category | $($row.Count) | $($row.TotalDurationSeconds) | $($row.TotalLostSeconds) |")
}

$lines.Add("")
$lines.Add("## Top Commands (by lost time)")
$lines.Add("")
$lines.Add("| Command | Count | Duration(s) | Lost(s) |")
$lines.Add("| --- | --- | --- | --- |")

foreach ($row in $commandTimeGroups) {
    $command = Escape-Cell $row.Command
    $commandCell = '`' + $command + '`'
    $lines.Add("| $commandCell | $($row.Count) | $($row.TotalDurationSeconds) | $($row.TotalLostSeconds) |")
}

$lines | Set-Content -Path $outFile -Encoding utf8

Write-Host "Metrics refreshed at: $outFile"
Write-Host ("Summary => total={0}, pass={1}, fail={2}, partial={3}, lost_s={4}" -f $total, $passCount, $failCount, $partialCount, $totalLost)

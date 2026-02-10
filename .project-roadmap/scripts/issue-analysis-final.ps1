$issues = @()
Get-ChildItem 'D:\github\mRemoteNG\.project-roadmap\issues-db\upstream\*.json' | ForEach-Object {
    $j = Get-Content $_.FullName -Raw | ConvertFrom-Json
    $labels = @()
    if ($j.labels) { $labels = @($j.labels) }
    $year = if ($j.created_at -and $j.created_at.Length -ge 4) { $j.created_at.Substring(0,4) } else { 'unknown' }
    $issues += [PSCustomObject]@{
        Number = $j.number
        State = $j.state
        Labels = $labels
        LabelsStr = ($labels -join ',')
        WaitingForUs = $j.waiting_for_us
        Year = $year
        Title = $j.title
    }
}

# Issues we already fixed in v1.79/v1.80
$fixedIssues = @(3069, 3092, 2972, 3005, 1916, 850, 1969, 822, 2785, 2735, 847, 1650, 2510, 2987, 2673, 1649, 1634, 2270, 811, 2160, 2161, 2171, 2166, 2155, 2142)
Write-Output "=== Issues in our DB that we already fixed in v1.79/v1.80 ==="
$alreadyFixed = $issues | Where-Object { $fixedIssues -contains $_.Number }
Write-Output "  Count: $($alreadyFixed.Count)"
foreach ($af in ($alreadyFixed | Sort-Object { [int]$_.Number })) {
    Write-Output ("  #{0,-6} ({1}) [{2}] {3}" -f $af.Number, $af.Year, $af.LabelsStr, $af.Title)
}

# How many are RTFM/Support Request/Question
Write-Output ""
Write-Output "=== RTFM / Support Request / Question issues ==="
$support = $issues | Where-Object { ($_.Labels -contains 'RTFM') -or ($_.Labels -contains 'Support Request') -or ($_.Labels -contains 'Question') }
Write-Output "  Count: $($support.Count)"
foreach ($s in $support) {
    Write-Output ("  #{0,-6} ({1}) [{2}] {3}" -f $s.Number, $s.Year, $s.LabelsStr, $s.Title)
}

# Issues from 2024-2026 (recent, likely still relevant)
Write-Output ""
Write-Output "=== RECENT ISSUES (2024-2026) by label ==="
$recent = $issues | Where-Object { [int]$_.Year -ge 2024 }
Write-Output "  Total recent: $($recent.Count)"
$recentBugs = $recent | Where-Object { $_.Labels -contains 'Bug' }
$recentEnh = $recent | Where-Object { $_.Labels -contains 'Enhancement' }
$recentN2c = $recent | Where-Object { $_.Labels -contains 'Need 2 check' }
$recentSec = $recent | Where-Object { $_.Labels -contains 'Security' }
Write-Output ("  Recent bugs:         {0}" -f $recentBugs.Count)
Write-Output ("  Recent enhancements: {0}" -f $recentEnh.Count)
Write-Output ("  Recent need 2 check: {0}" -f $recentN2c.Count)
Write-Output ("  Recent security:     {0}" -f $recentSec.Count)

# Overlap analysis: how many issues appear in MULTIPLE triage categories
Write-Output ""
Write-Output "=== OVERLAP ANALYSIS ==="
$n2cStale = $issues | Where-Object { $_.Labels -contains 'Need 2 check' -and [int]$_.Year -le 2023 }
$oldEnh = $issues | Where-Object { $_.Labels -contains 'Enhancement' -and [int]$_.Year -le 2017 }
$inDev = $issues | Where-Object { ($_.Labels -contains 'In development') -or ($_.Labels -contains 'In progress') }
$dups = $issues | Where-Object { $_.Labels -contains 'Duplicate' }
$noLabels = $issues | Where-Object { $_.Labels.Count -eq 0 }

# Collect unique issue numbers across all triage categories
$triageSet = @{}
foreach ($i in $dups) { $triageSet[$i.Number] = $true }
foreach ($i in $n2cStale) { $triageSet[$i.Number] = $true }
foreach ($i in $oldEnh) { $triageSet[$i.Number] = $true }
foreach ($i in $inDev) { $triageSet[$i.Number] = $true }
foreach ($i in $noLabels) { $triageSet[$i.Number] = $true }

Write-Output ("  Unique issues across ALL triage categories: {0}" -f $triageSet.Count)
Write-Output ("  Out of total DB:                            {0}" -f $issues.Count)
Write-Output ("  Percentage triageable:                      {0:N1}%" -f (($triageSet.Count / $issues.Count) * 100))

# Remaining issues NOT in any triage category
$remaining = $issues | Where-Object { -not $triageSet.ContainsKey($_.Number) }
Write-Output ""
Write-Output "=== REMAINING (not in any triage category): $($remaining.Count) ==="
Write-Output "  These need manual review or are potentially active/valid"
$remaining | Group-Object Year | Sort-Object Name | ForEach-Object {
    Write-Output ("  {0}  {1,5}" -f $_.Name, $_.Count)
}

# Remaining by key label
$remBugs = ($remaining | Where-Object { $_.Labels -contains 'Bug' }).Count
$remEnh = ($remaining | Where-Object { $_.Labels -contains 'Enhancement' }).Count
$remN2c = ($remaining | Where-Object { $_.Labels -contains 'Need 2 check' }).Count
$remSec = ($remaining | Where-Object { $_.Labels -contains 'Security' }).Count
$remVer = ($remaining | Where-Object { $_.Labels -contains 'Verified' }).Count
Write-Output ("  Remaining bugs:         {0}" -f $remBugs)
Write-Output ("  Remaining enhancements: {0}" -f $remEnh)
Write-Output ("  Remaining need 2 check: {0}" -f $remN2c)
Write-Output ("  Remaining security:     {0}" -f $remSec)
Write-Output ("  Remaining verified:     {0}" -f $remVer)

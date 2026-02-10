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
        CreatedAt = $j.created_at
    }
}

Write-Output "=============================================="
Write-Output "  ISSUE INTELLIGENCE DB - STATISTICAL ANALYSIS"
Write-Output "  Total issues: $($issues.Count)"
Write-Output "=============================================="

# --- STATE ---
Write-Output ""
Write-Output "=== BY STATE ==="
$issues | Group-Object State | Sort-Object Count -Descending | ForEach-Object {
    Write-Output ("  {0,-10} {1,5}" -f $_.Name, $_.Count)
}

# --- BY YEAR ---
Write-Output ""
Write-Output "=== BY YEAR (created_at) ==="
$issues | Group-Object Year | Sort-Object Name | ForEach-Object {
    Write-Output ("  {0}  {1,5}" -f $_.Name, $_.Count)
}

# --- WAITING FOR US BY YEAR ---
Write-Output ""
Write-Output "=== WAITING_FOR_US=True BY YEAR ==="
$waitingIssues = $issues | Where-Object { $_.WaitingForUs -eq $true }
Write-Output "  Total waiting_for_us=True: $($waitingIssues.Count)"
$waitingIssues | Group-Object Year | Sort-Object Name | ForEach-Object {
    Write-Output ("  {0}  {1,5}" -f $_.Name, $_.Count)
}

# --- WAITING FOR US BY STATE ---
Write-Output ""
Write-Output "=== WAITING_FOR_US by STATE ==="
$issues | Group-Object { "$($_.State)|waiting=$($_.WaitingForUs)" } | Sort-Object Name | ForEach-Object {
    Write-Output ("  {0,-30} {1,5}" -f $_.Name, $_.Count)
}

# --- INDIVIDUAL LABEL COUNTS ---
Write-Output ""
Write-Output "=== INDIVIDUAL LABEL FREQUENCY ==="
$allLabels = @{}
foreach ($issue in $issues) {
    foreach ($label in $issue.Labels) {
        $l = $label.Trim()
        if ($l) {
            if (-not $allLabels.ContainsKey($l)) { $allLabels[$l] = 0 }
            $allLabels[$l]++
        }
    }
}
$allLabels.GetEnumerator() | Sort-Object Value -Descending | ForEach-Object {
    Write-Output ("  {0,-45} {1,5}" -f $_.Key, $_.Value)
}

# --- LABEL COMBINATION COUNTS ---
Write-Output ""
Write-Output "=== TOP LABEL COMBINATIONS (top 40) ==="
$issues | Group-Object LabelsStr | Sort-Object Count -Descending | Select-Object -First 40 | ForEach-Object {
    $combo = if ($_.Name) { $_.Name } else { '(no labels)' }
    Write-Output ("  {0,-60} {1,5}" -f $combo, $_.Count)
}

# --- KEY CATEGORIES ---
Write-Output ""
Write-Output "=== KEY CATEGORIES ==="

$bugs = $issues | Where-Object { $_.Labels -contains 'Bug' }
$enhancements = $issues | Where-Object { $_.Labels -contains 'Enhancement' }
$need2check = $issues | Where-Object { $_.Labels -contains 'Need 2 check' }
$duplicates = $issues | Where-Object { $_.Labels -contains 'Duplicate' }
$security = $issues | Where-Object { $_.Labels -contains 'Security' }
$inDev = $issues | Where-Object { ($_.Labels -contains 'In development') -or ($_.Labels -contains 'In progress') }
$noLabels = $issues | Where-Object { $_.Labels.Count -eq 0 }
$verified = $issues | Where-Object { $_.Labels -contains 'Verified' }
$ready = $issues | Where-Object { $_.Labels -contains 'Ready' }
$helpWanted = $issues | Where-Object { $_.Labels -contains 'Help Wanted' }

Write-Output ("  Bug:              {0,5}  (open: {1}, closed: {2})" -f $bugs.Count, ($bugs | Where-Object { $_.State -eq 'open' }).Count, ($bugs | Where-Object { $_.State -eq 'closed' }).Count)
Write-Output ("  Enhancement:      {0,5}  (open: {1}, closed: {2})" -f $enhancements.Count, ($enhancements | Where-Object { $_.State -eq 'open' }).Count, ($enhancements | Where-Object { $_.State -eq 'closed' }).Count)
Write-Output ("  Need 2 check:     {0,5}  (open: {1}, closed: {2})" -f $need2check.Count, ($need2check | Where-Object { $_.State -eq 'open' }).Count, ($need2check | Where-Object { $_.State -eq 'closed' }).Count)
Write-Output ("  Duplicate:        {0,5}  (open: {1}, closed: {2})" -f $duplicates.Count, ($duplicates | Where-Object { $_.State -eq 'open' }).Count, ($duplicates | Where-Object { $_.State -eq 'closed' }).Count)
Write-Output ("  Security:         {0,5}  (open: {1}, closed: {2})" -f $security.Count, ($security | Where-Object { $_.State -eq 'open' }).Count, ($security | Where-Object { $_.State -eq 'closed' }).Count)
Write-Output ("  In dev/progress:  {0,5}  (open: {1}, closed: {2})" -f $inDev.Count, ($inDev | Where-Object { $_.State -eq 'open' }).Count, ($inDev | Where-Object { $_.State -eq 'closed' }).Count)
Write-Output ("  (no labels):      {0,5}  (open: {1}, closed: {2})" -f $noLabels.Count, ($noLabels | Where-Object { $_.State -eq 'open' }).Count, ($noLabels | Where-Object { $_.State -eq 'closed' }).Count)
Write-Output ("  Verified:         {0,5}  (open: {1}, closed: {2})" -f $verified.Count, ($verified | Where-Object { $_.State -eq 'open' }).Count, ($verified | Where-Object { $_.State -eq 'closed' }).Count)
Write-Output ("  Ready:            {0,5}  (open: {1}, closed: {2})" -f $ready.Count, ($ready | Where-Object { $_.State -eq 'open' }).Count, ($ready | Where-Object { $_.State -eq 'closed' }).Count)
Write-Output ("  Help Wanted:      {0,5}  (open: {1}, closed: {2})" -f $helpWanted.Count, ($helpWanted | Where-Object { $_.State -eq 'open' }).Count, ($helpWanted | Where-Object { $_.State -eq 'closed' }).Count)

# --- TRIAGE-RELEVANT SUBSETS ---
Write-Output ""
Write-Output "=== TRIAGE-RELEVANT SUBSETS ==="

# Need 2 check older than 2 years (before 2024)
$n2cStale = $need2check | Where-Object { $_.State -eq 'open' -and [int]$_.Year -le 2023 }
Write-Output ""
Write-Output "--- 'Need 2 check' + OPEN + created <= 2023 (stale unverified): $($n2cStale.Count) ---"
$n2cStale | Group-Object Year | Sort-Object Name | ForEach-Object {
    Write-Output ("  {0}  {1,5}" -f $_.Name, $_.Count)
}

# Duplicates still open
$dupOpen = $duplicates | Where-Object { $_.State -eq 'open' }
Write-Output ""
Write-Output "--- 'Duplicate' + OPEN (should be closed): $($dupOpen.Count) ---"
foreach ($d in $dupOpen) {
    Write-Output ("  #{0,-6} ({1}) {2}" -f $d.Number, $d.Year, $d.Title)
}

# Enhancement from 2015-2017 still open
$oldEnhancements = $enhancements | Where-Object { $_.State -eq 'open' -and [int]$_.Year -le 2017 }
Write-Output ""
Write-Output "--- Enhancement + OPEN + created <= 2017 (likely deferred): $($oldEnhancements.Count) ---"
$oldEnhancements | Group-Object Year | Sort-Object Name | ForEach-Object {
    Write-Output ("  {0}  {1,5}" -f $_.Name, $_.Count)
}

# In development/In progress but open (potentially stale)
$inDevOpen = $inDev | Where-Object { $_.State -eq 'open' }
Write-Output ""
Write-Output "--- 'In development'/'In progress' + OPEN (potentially stale): $($inDevOpen.Count) ---"
foreach ($d in $inDevOpen) {
    Write-Output ("  #{0,-6} ({1}) [{2}] {3}" -f $d.Number, $d.Year, $d.LabelsStr, $d.Title)
}

# Open bugs by year
Write-Output ""
Write-Output "--- OPEN Bugs by year ---"
$openBugs = $bugs | Where-Object { $_.State -eq 'open' }
Write-Output "  Total open bugs: $($openBugs.Count)"
$openBugs | Group-Object Year | Sort-Object Name | ForEach-Object {
    Write-Output ("  {0}  {1,5}" -f $_.Name, $_.Count)
}

# Open issues with NO labels
$noLabelsOpen = $noLabels | Where-Object { $_.State -eq 'open' }
Write-Output ""
Write-Output "--- OPEN issues with NO labels: $($noLabelsOpen.Count) ---"
$noLabelsOpen | Group-Object Year | Sort-Object Name | ForEach-Object {
    Write-Output ("  {0}  {1,5}" -f $_.Name, $_.Count)
}

# Issues tagged with version milestones
Write-Output ""
Write-Output "=== VERSION MILESTONE LABELS ==="
$versionLabels = $allLabels.GetEnumerator() | Where-Object { $_.Key -match '^\d+\.\d+' } | Sort-Object Key
foreach ($vl in $versionLabels) {
    $vlIssues = $issues | Where-Object { $_.Labels -contains $vl.Key }
    $vlOpen = ($vlIssues | Where-Object { $_.State -eq 'open' }).Count
    Write-Output ("  {0,-20} total: {1,4}  open: {2,4}" -f $vl.Key, $vl.Value, $vlOpen)
}

# Bugs that are also Enhancement (mis-labeled?)
$bugAndEnh = $issues | Where-Object { ($_.Labels -contains 'Bug') -and ($_.Labels -contains 'Enhancement') }
Write-Output ""
Write-Output "--- Bug AND Enhancement (potential mis-label): $($bugAndEnh.Count) ---"
foreach ($be in $bugAndEnh) {
    Write-Output ("  #{0,-6} ({1}) [{2}] {3}" -f $be.Number, $be.Year, $be.LabelsStr, $be.Title)
}

# Summary for bulk triage
Write-Output ""
Write-Output "=============================================="
Write-Output "  BULK TRIAGE RECOMMENDATIONS"
Write-Output "=============================================="
Write-Output ""
Write-Output ("  1. CLOSE duplicates still open:                    {0,4} issues" -f $dupOpen.Count)
Write-Output ("  2. STALE 'Need 2 check' (open, <=2023):           {0,4} issues" -f $n2cStale.Count)
Write-Output ("  3. DEFERRED enhancements (open, <=2017):           {0,4} issues" -f $oldEnhancements.Count)
Write-Output ("  4. STALE 'In dev/progress' (open):                 {0,4} issues" -f $inDevOpen.Count)
Write-Output ("  5. UNLABELED open issues:                          {0,4} issues" -f $noLabelsOpen.Count)
Write-Output ("  6. Bug+Enhancement dual-label (review):            {0,4} issues" -f $bugAndEnh.Count)

$totalActionable = $dupOpen.Count + $n2cStale.Count + $oldEnhancements.Count + $inDevOpen.Count + $noLabelsOpen.Count + $bugAndEnh.Count
# Some issues may be counted in multiple categories
Write-Output ""
Write-Output ("  TOTAL actionable (may overlap):                    {0,4} issues" -f $totalActionable)
Write-Output ("  TOTAL in DB:                                       {0,4} issues" -f $issues.Count)

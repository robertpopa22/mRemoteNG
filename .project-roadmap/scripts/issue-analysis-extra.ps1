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

# "Not Planned" issues
Write-Output "=== 'Not Planned' issues ==="
$notPlanned = $issues | Where-Object { $_.Labels -contains 'Not Planned' }
foreach ($np in $notPlanned) {
    Write-Output ("  #{0,-6} ({1}) [{2}] {3}" -f $np.Number, $np.Year, $np.LabelsStr, $np.Title)
}

# "Cannot Reproduce" issues
Write-Output ""
Write-Output "=== 'Cannot Reproduce' issues ==="
$cantRepro = $issues | Where-Object { $_.Labels -contains 'Cannot Reproduce' }
foreach ($cr in $cantRepro) {
    Write-Output ("  #{0,-6} ({1}) [{2}] {3}" -f $cr.Number, $cr.Year, $cr.LabelsStr, $cr.Title)
}

# "Won't Fix" issues
Write-Output ""
Write-Output "=== 'Won't Fix' issues ==="
$wontFix = $issues | Where-Object { $_.Labels -contains "Won't Fix" }
foreach ($wf in $wontFix) {
    Write-Output ("  #{0,-6} ({1}) [{2}] {3}" -f $wf.Number, $wf.Year, $wf.LabelsStr, $wf.Title)
}

# "Fixed" label issues still open
Write-Output ""
Write-Output "=== 'Fixed' label but OPEN ==="
$fixed = $issues | Where-Object { $_.Labels -contains 'Fixed' }
foreach ($f in $fixed) {
    Write-Output ("  #{0,-6} ({1}) [{2}] {3}" -f $f.Number, $f.Year, $f.LabelsStr, $f.Title)
}

# Security issues
Write-Output ""
Write-Output "=== Security issues (all 27) ==="
$security = $issues | Where-Object { $_.Labels -contains 'Security' }
foreach ($s in ($security | Sort-Object { [int]$_.Number })) {
    Write-Output ("  #{0,-6} ({1}) waiting={2,-5} [{3}] {4}" -f $s.Number, $s.Year, $s.WaitingForUs, $s.LabelsStr, $s.Title)
}

# Security Vuln
Write-Output ""
Write-Output "=== 'Security Vuln' issues ==="
$secVuln = $issues | Where-Object { $_.Labels -contains 'Security Vuln' }
foreach ($sv in ($secVuln | Sort-Object { [int]$_.Number })) {
    Write-Output ("  #{0,-6} ({1}) waiting={2,-5} [{3}] {4}" -f $sv.Number, $sv.Year, $sv.WaitingForUs, $sv.LabelsStr, $sv.Title)
}

# "AI" label issues
Write-Output ""
Write-Output "=== 'AI' label issues ==="
$ai = $issues | Where-Object { $_.Labels -contains 'AI' }
foreach ($a in $ai) {
    Write-Output ("  #{0,-6} ({1}) [{2}] {3}" -f $a.Number, $a.Year, $a.LabelsStr, $a.Title)
}

# "critical" label issues
Write-Output ""
Write-Output "=== 'critical' label issues ==="
$crit = $issues | Where-Object { $_.Labels -contains 'critical' }
foreach ($c in $crit) {
    Write-Output ("  #{0,-6} ({1}) [{2}] {3}" -f $c.Number, $c.Year, $c.LabelsStr, $c.Title)
}

# Priority High issues
Write-Output ""
Write-Output "=== 'Priority - High' issues ==="
$priHigh = $issues | Where-Object { $_.Labels -contains 'Priority - High' }
foreach ($ph in ($priHigh | Sort-Object { [int]$_.Number })) {
    Write-Output ("  #{0,-6} ({1}) waiting={2,-5} [{3}] {4}" -f $ph.Number, $ph.Year, $ph.WaitingForUs, $ph.LabelsStr, $ph.Title)
}

# Old bugs (2016-2018) that are waiting_for_us
Write-Output ""
Write-Output "=== OLD BUGS waiting_for_us (2016-2018) ==="
$oldBugsWaiting = $issues | Where-Object { $_.Labels -contains 'Bug' -and $_.WaitingForUs -eq $true -and [int]$_.Year -le 2018 }
Write-Output "  Count: $($oldBugsWaiting.Count)"
foreach ($ob in ($oldBugsWaiting | Sort-Object { [int]$_.Number })) {
    Write-Output ("  #{0,-6} ({1}) [{2}] {3}" -f $ob.Number, $ob.Year, $ob.LabelsStr, $ob.Title)
}

# Cross-tabulation: waiting_for_us by label category
Write-Output ""
Write-Output "=== WAITING_FOR_US=True by category ==="
$waitAll = $issues | Where-Object { $_.WaitingForUs -eq $true }
$waitBugs = $waitAll | Where-Object { $_.Labels -contains 'Bug' }
$waitEnh = $waitAll | Where-Object { $_.Labels -contains 'Enhancement' }
$waitN2c = $waitAll | Where-Object { $_.Labels -contains 'Need 2 check' }
$waitSec = $waitAll | Where-Object { $_.Labels -contains 'Security' }
Write-Output ("  Bugs waiting:         {0,4}" -f $waitBugs.Count)
Write-Output ("  Enhancements waiting: {0,4}" -f $waitEnh.Count)
Write-Output ("  Need 2 check waiting: {0,4}" -f $waitN2c.Count)
Write-Output ("  Security waiting:     {0,4}" -f $waitSec.Count)
Write-Output ("  Total waiting:        {0,4}" -f $waitAll.Count)

# Issues with milestone 1.76.20 (ancient)
Write-Output ""
Write-Output "=== Milestone 1.76.20 issues ==="
$m176 = $issues | Where-Object { $_.Labels -contains '1.76.20' }
foreach ($m in $m176) {
    Write-Output ("  #{0,-6} ({1}) [{2}] {3}" -f $m.Number, $m.Year, $m.LabelsStr, $m.Title)
}

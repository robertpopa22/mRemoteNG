<#
.SYNOPSIS
    Watches a running mRemoteNG process while you open and close RDP sessions, and says
    whether closed sessions are being kept.

.DESCRIPTION
    Written for #182: roughly 300 MB retained per RDP session, about 2 GB after eight, with
    every tab and panel closed. The lab can only stand up small xrdp sessions, so the
    reporter's class of session -- a real Windows desktop -- is measured here, on the
    machine and the servers you already use, with you at the keyboard.

    Every second it samples the process's private bytes and, once a second on change,
    prints a line. Open an RDP session, wait for the desktop to paint, close its tab,
    repeat; press Enter when done. It then reports the cost of the first session, what
    was retained after each close, and the slope across later sessions -- the leak's
    signature is each session adding its own chunk and keeping it, not "back to
    baseline", which a first connection never quite returns to.

.PARAMETER ProcessName
    Defaults to mRemoteNG. Point it at the daily-driver process; the script refuses to
    guess if more than one is running.

.EXAMPLE
    pwsh -NoProfile -File scripts\measure-rdp-memory.ps1
    # then: open RDP tab -> desktop paints -> close tab -> repeat 3-4 times -> Enter
#>
[CmdletBinding()]
param(
    [string]$ProcessName = 'mRemoteNG'
)

$ErrorActionPreference = 'Stop'

$procs = @(Get-Process -Name $ProcessName -ErrorAction SilentlyContinue)
if ($procs.Count -eq 0) { throw "No $ProcessName process is running. Start it first." }
if ($procs.Count -gt 1) { throw "$($procs.Count) $ProcessName processes are running (PIDs $($procs.Id -join ', ')). Close all but one." }
$proc = $procs[0]

function PrivateMb([System.Diagnostics.Process]$p) {
    $p.Refresh()
    [math]::Round($p.PrivateMemorySize64 / 1MB)
}

$baseline = PrivateMb $proc
Write-Host ("Watching {0} (PID {1}). Baseline private bytes: {2} MB." -f $ProcessName, $proc.Id, $baseline)
Write-Host "Open an RDP session, let the desktop paint, close its tab. Repeat 3-4 times."
Write-Host "Press Enter here when the last tab is closed and everything has settled."
Write-Host ""

$samples = [System.Collections.Generic.List[pscustomobject]]::new()
$last = $baseline
$start = Get-Date

$reader = [Console]::In
while (-not [Console]::KeyAvailable) {
    Start-Sleep -Milliseconds 1000
    if ($proc.HasExited) { throw "$ProcessName exited during the measurement." }
    $now = PrivateMb $proc
    if ([math]::Abs($now - $last) -ge 5) {
        $t = [int]((Get-Date) - $start).TotalSeconds
        $samples.Add([pscustomobject]@{ Seconds = $t; Mb = $now; Delta = $now - $last })
        Write-Host ("  t+{0,4}s  {1,6} MB  ({2:+#;-#;0} MB)" -f $t, $now, ($now - $last))
        $last = $now
    }
}
[void][Console]::ReadLine()

$final = PrivateMb $proc
Write-Host ""
Write-Host ("Final private bytes: {0} MB ({1:+#;-#;0} MB against baseline)." -f $final, ($final - $baseline))

# Peaks are the local maxima (a session open), troughs the minima after each (a session closed).
$peaks = @(); $troughs = @()
for ($i = 1; $i -lt $samples.Count - 1; $i++) {
    if ($samples[$i].Mb -gt $samples[$i-1].Mb -and $samples[$i].Mb -ge $samples[$i+1].Mb) { $peaks += $samples[$i].Mb }
    if ($samples[$i].Mb -lt $samples[$i-1].Mb -and $samples[$i].Mb -le $samples[$i+1].Mb) { $troughs += $samples[$i].Mb }
}
if ($samples.Count -gt 0) { $troughs += $final }

if ($peaks.Count -lt 2 -or $troughs.Count -lt 2) {
    Write-Host "Fewer than two open/close cycles were seen; run it again with at least three."
    exit 2
}

$firstCost = $peaks[0] - $baseline
$retained = @()
for ($i = 0; $i -lt $troughs.Count; $i++) { $retained += ($troughs[$i] - $baseline) }
$laterAdded = $troughs[-1] - $troughs[0]
$laterCount = [math]::Max($troughs.Count - 1, 1)
$perLater = [math]::Round($laterAdded / $laterCount)

Write-Host ""
Write-Host ("First session cost about {0} MB." -f $firstCost)
Write-Host ("Retained after each close (above baseline): {0} MB." -f ($retained -join ', '))
Write-Host ("Sessions after the first added {0} MB in total, about {1} MB each." -f $laterAdded, $perLater)
Write-Host ""

if ($perLater -ge [math]::Round($firstCost / 2)) {
    Write-Host ("LEAK: every session after the first keeps about {0} MB of its {1} MB." -f $perLater, $firstCost) -ForegroundColor Red
    exit 1
}
Write-Host ("No accumulation: later sessions keep about {0} MB each against a cost of {1} MB." -f $perLater, $firstCost) -ForegroundColor Green
exit 0

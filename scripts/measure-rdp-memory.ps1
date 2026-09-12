<#
.SYNOPSIS
    Watches a running mRemoteNG process while you open and close RDP sessions, and says
    whether closed sessions are being kept.

.DESCRIPTION
    Written for #182: roughly 300 MB retained per RDP session, about 2 GB after eight, with
    every tab and panel closed. The lab can only stand up small xrdp sessions, so the
    reporter's class of session -- a real Windows desktop -- is measured here, on the
    machine and the servers you already use, with you at the keyboard.

    You mark each close yourself. The first version of this script tried to find the
    open/close points in the sampled series on its own and got it wrong: an RDP desktop
    painting swings private bytes by hundreds of MB from one second to the next, its
    peak-and-trough guess produced fifteen "sessions" out of five, and it printed
    "No accumulation" over a run that had kept almost a gigabyte. The reporter caught that.
    So: open a session, let the desktop paint, close its tab, wait a few seconds, press
    Enter. Repeat three or four times. Type q and Enter when done.

    The verdict is on the slope -- what each session after the first leaves behind -- not
    on a return to baseline, which a first connection never quite manages (JIT, caches).

.PARAMETER ProcessName
    Defaults to mRemoteNG. The script refuses to guess if more than one is running.

.EXAMPLE
    pwsh -NoProfile -File scripts\measure-rdp-memory.ps1
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

# Settle, then take the lowest of a few readings: private bytes wobble by a few MB at rest.
function SettledMb([System.Diagnostics.Process]$p) {
    Start-Sleep -Seconds 3
    $readings = 1..4 | ForEach-Object { Start-Sleep -Milliseconds 500; PrivateMb $p }
    ($readings | Measure-Object -Minimum).Minimum
}

$baseline = SettledMb $proc
Write-Host ("Watching {0} (PID {1}). Baseline private bytes: {2} MB." -f $ProcessName, $proc.Id, $baseline)
Write-Host "Open an RDP session, let the desktop paint, close its tab, wait a few seconds,"
Write-Host "then press Enter here. Repeat three or four times. Type q and press Enter when done."
Write-Host ""

$afterClose = [System.Collections.Generic.List[int]]::new()
$peakWhileOpen = [System.Collections.Generic.List[int]]::new()
$peak = $baseline

while ($true) {
    # Track the peak between marks so the cost of a session is known.
    while (-not [Console]::KeyAvailable) {
        Start-Sleep -Milliseconds 500
        if ($proc.HasExited) { throw "$ProcessName exited during the measurement." }
        $now = PrivateMb $proc
        if ($now -gt $peak) { $peak = $now }
    }
    $line = [Console]::ReadLine()
    if ($line -match '^\s*q') { break }

    $closed = SettledMb $proc
    $afterClose.Add($closed)
    $peakWhileOpen.Add($peak)
    $n = $afterClose.Count
    Write-Host ("  session {0}: peak while open {1} MB, after close {2} MB ({3:+#;-#;0} MB above baseline)" -f $n, $peak, $closed, ($closed - $baseline))
    $peak = $closed
}

if ($afterClose.Count -lt 2) {
    Write-Host "Fewer than two sessions were marked; at least three are needed to see a slope."
    exit 2
}

$firstCost = [math]::Max($peakWhileOpen[0] - $baseline, 1)
$retainedFirst = $afterClose[0] - $baseline
$laterAdded = $afterClose[$afterClose.Count - 1] - $afterClose[0]
$laterCount = $afterClose.Count - 1
$perLater = [math]::Round($laterAdded / $laterCount)

Write-Host ""
Write-Host ("First session cost about {0} MB and left {1} MB behind after closing." -f $firstCost, $retainedFirst)
Write-Host ("The next {0} session(s) added {1} MB in total, about {2} MB each." -f $laterCount, $laterAdded, $perLater)
Write-Host ("Retained after each close, above baseline: {0} MB." -f (($afterClose | ForEach-Object { $_ - $baseline }) -join ', '))
Write-Host ""

if ($perLater -ge [math]::Round($firstCost / 2)) {
    Write-Host ("LEAK: every session after the first keeps about {0} MB of its {1} MB." -f $perLater, $firstCost) -ForegroundColor Red
    exit 1
}
Write-Host ("No accumulation: later sessions keep about {0} MB each against a cost of {1} MB." -f $perLater, $firstCost) -ForegroundColor Green
exit 0

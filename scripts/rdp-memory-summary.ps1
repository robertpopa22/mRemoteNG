# Shared by the interactive measurement and its offline regression tests.
function Write-RdpMemorySummary {
    param([long]$baseline, [long[]]$peakWhileOpen, [long[]]$afterClose)
    if ($peakWhileOpen.Count -ne $afterClose.Count -or $baseline -lt 0 -or
        @($peakWhileOpen + $afterClose | Where-Object { $_ -lt 0 }).Count -gt 0) {
        throw 'Invalid memory samples.'
    }
    if ($afterClose.Count -lt 3) {
        Write-Host "Fewer than three sessions were marked; at least three are needed for this summary."
        return 2
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
    
    if ($laterAdded -gt 0) {
        Write-Host ("Observed growth after the first close: +{0} MB across {1} further sessions." -f $laterAdded, $laterCount) -ForegroundColor Yellow
    } else {
        Write-Host ("Net change from the first to the last close: {0:+#;-#;0} MB across {1} further sessions." -f $laterAdded, $laterCount)
    }
    Write-Host "These private-byte samples do not establish the cause or rule out a leak. Review the full per-close series."
    return 0
}

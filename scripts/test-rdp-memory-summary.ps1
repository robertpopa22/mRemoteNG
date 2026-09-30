$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'rdp-memory-summary.ps1')

$cases = @(
    @{ Name='reported'; Closed=@(274,376,511,525,520,651,778,774,903,899,888,1019,1136,1274); Status=0; Text='Observed growth after the first close: +1000 MB' },
    @{ Name='flat'; Closed=@(274,274,274); Status=0; Text='Net change from the first to the last close: 0 MB' },
    @{ Name='decreasing'; Closed=@(274,260,250); Status=0; Text='Net change from the first to the last close: -24 MB' },
    @{ Name='insufficient'; Closed=@(274,376); Status=2; Text='Fewer than three sessions' }
)
foreach ($case in $cases) {
    $records = @(Write-RdpMemorySummary -baseline 99 -peakWhileOpen (@(431) * $case.Closed.Count) -afterClose $case.Closed 6>&1)
    $status = $records[-1]
    $report = ($records | Where-Object { $_ -is [System.Management.Automation.InformationRecord] }) -join "`n"
    if ($status -ne $case.Status -or -not $report.Contains($case.Text)) {
        throw "Wrong report for $($case.Name): $report (status $status)"
    }
    if ($report -match 'No accumulation|VERDICT|LEAK:|leak.free') {
        throw "Unsupported diagnosis in $($case.Name): $report"
    }
    if ($case.Status -eq 0 -and -not $report.Contains('do not establish the cause or rule out a leak')) {
        throw "Missing measurement limitation in $($case.Name)"
    }
}
Write-Host "$($cases.Count) memory-report regressions passed."

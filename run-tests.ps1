# run-tests.ps1 - Multi-process parallel test runner for mRemoteNG
#
# WHY MULTI-PROCESS: The production code uses shared mutable singletons
# (DefaultConnectionInheritance.Instance, Runtime.ConnectionsService, Runtime.EncryptionKey)
# that are not thread-safe. NUnit fixture-level parallelism causes race conditions.
# Instead, we run 5 separate dotnet test processes (each with isolated static state)
# in parallel, grouped by namespace.
#
# Usage:
#   powershell.exe -NoProfile -ExecutionPolicy Bypass -File run-tests.ps1
#   powershell.exe -NoProfile -ExecutionPolicy Bypass -File run-tests.ps1 -Sequential
#   powershell.exe -NoProfile -ExecutionPolicy Bypass -File run-tests.ps1 -NoBuild

param(
    [switch]$Sequential,     # Force single-process sequential execution
    [int]$Timeout = 15000,   # Per-test timeout in ms (default 15s, matches .runsettings)
    [switch]$NoBuild         # Skip build step (use existing binaries)
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

# --- Step 1: Detect CPU cores ---
$cpuCores = [Environment]::ProcessorCount

Write-Host "=== mRemoteNG Test Runner ===" -ForegroundColor Cyan
Write-Host "CPU: $cpuCores logical processors"
Write-Host "Timeout: ${Timeout}ms per test"
Write-Host "Mode: $(if ($Sequential) { 'Sequential (1 process)' } else { 'Full parallel (5 processes)' })"
Write-Host ""

# --- Step 2: Kill stale processes ---
Write-Host "Cleaning stale processes..." -ForegroundColor Yellow
foreach ($proc in @('testhost', 'notepad')) {
    Get-Process -Name $proc -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}

# --- Step 3: Build (unless -NoBuild) ---
if (-not $NoBuild) {
    Write-Host "Building..." -ForegroundColor Yellow
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$repoRoot\build.ps1"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "BUILD FAILED (exit code $LASTEXITCODE)" -ForegroundColor Red
        exit 1
    }
    Write-Host ""
}

# --- Step 4: Paths ---
$testDll = "$repoRoot\mRemoteNGTests\bin\x64\Release\mRemoteNGTests.dll"
$specsDll = "$repoRoot\mRemoteNGSpecs\bin\x64\Release\mRemoteNGSpecs.dll"
$runSettings = "$repoRoot\mRemoteNGTests\mRemoteNGTests.runsettings"

if (-not (Test-Path $testDll)) {
    Write-Host "ERROR: Test DLL not found at $testDll" -ForegroundColor Red
    Write-Host "HINT: Run build.ps1 first (it builds the entire solution including test projects)" -ForegroundColor Yellow
    Write-Host "TEST_DLL_MISSING" -ForegroundColor Red
    exit 1
}

# Verify test DLL is not stale (warn if older than 1 hour)
$testDllAge = (Get-Date) - (Get-Item $testDll).LastWriteTime
if ($testDllAge.TotalHours -gt 1) {
    Write-Host "WARNING: Test DLL is $([math]::Round($testDllAge.TotalHours, 1))h old -- may be stale" -ForegroundColor Yellow
    Write-Host "Consider running without -NoBuild to recompile" -ForegroundColor Yellow
}

# --- Step 5: Define test groups (each runs in a separate process) ---
# Group by namespace to ensure each process has isolated static state.
# UI tests use RunWithMessagePump pattern and need their own process.
$groups = @(
    @{
        Name = "Security"
        Filter = "FullyQualifiedName~mRemoteNGTests.Security"
    },
    @{
        Name = "Tools+Misc"
        Filter = "(FullyQualifiedName~mRemoteNGTests.Tools|FullyQualifiedName~mRemoteNGTests.Messages|FullyQualifiedName~mRemoteNGTests.App|FullyQualifiedName~mRemoteNGTests.Container|FullyQualifiedName~mRemoteNGTests.ExternalConnectors|FullyQualifiedName~mRemoteNGTests.Installer|FullyQualifiedName~mRemoteNGTests.BinaryFileTests|FullyQualifiedName~mRemoteNGTests.Properties|FullyQualifiedName~mRemoteNGTests.TestHelpers|FullyQualifiedName~mRemoteNGTests.Themes)"
    },
    @{
        Name = "Config"
        Filter = "FullyQualifiedName~mRemoteNGTests.Config"
    },
    @{
        Name = "Connection+Rest"
        Filter = "(FullyQualifiedName~mRemoteNGTests.Connection|FullyQualifiedName~mRemoteNGTests.Credential|FullyQualifiedName~mRemoteNGTests.IntegrationTests|FullyQualifiedName~mRemoteNGTests.nUnitForms|FullyQualifiedName~mRemoteNGTests.Tree|FullyQualifiedName~mRemoteNGTests.BinaryFile)"
    },
    @{
        Name = "UI"
        Filter = "FullyQualifiedName~mRemoteNGTests.UI"
    }
)

# --- Step 6: Run tests ---
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

if ($Sequential) {
    # Single process, all tests
    Write-Host "Running all tests sequentially..." -ForegroundColor Green
    $seqArgs = @('test', $testDll, '--verbosity', 'normal', '-s', $runSettings)
    $seqArgs += '--'
    $seqArgs += "NUnit.DefaultTimeout=$Timeout"
    & dotnet @seqArgs
    $testExitCode = $LASTEXITCODE
} else {
    # Multi-process parallel
    Write-Host "Launching $($groups.Count) parallel test processes..." -ForegroundColor Green
    Write-Host ""

    $jobs = @()
    $logDir = "$repoRoot\TestResults"
    if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }

    foreach ($group in $groups) {
        $logFile = "$logDir\$($group.Name).log"
        Write-Host "  [$($group.Name)] starting..." -ForegroundColor DarkGray

        $job = Start-Job -ScriptBlock {
            param($dll, $settings, $filter, $timeout, $log)
            $args = @('test', $dll, '--verbosity', 'normal', '-s', $settings, '--filter', $filter, '--', "NUnit.DefaultTimeout=$timeout")
            & dotnet @args 2>&1 | Tee-Object -FilePath $log
            $LASTEXITCODE
        } -ArgumentList $testDll, $runSettings, $group.Filter, $Timeout, $logFile

        $jobs += @{ Job = $job; Name = $group.Name; Log = $logFile }
    }

    # Wait for all jobs and parse results from LOG FILES (not job output).
    # Why: Receive-Job returns a mix of String, ErrorRecord, and Int objects.
    # Filtering with Where-Object { $_ -is [string] } can miss lines or get
    # confused by deserialized types. The log file is always clean text.
    # Also handles "Total tests: Unknown" (test host crash) by falling back
    # to Passed + Failed + Skipped.
    $testExitCode = 0
    $totalPassed = 0
    $totalFailed = 0
    $totalSkipped = 0
    $totalTests = 0

    foreach ($entry in $jobs) {
        $result = Receive-Job -Job $entry.Job -Wait
        $jobExitCode = $result | Where-Object { $_ -is [int] } | Select-Object -Last 1
        if ($null -eq $jobExitCode) { $jobExitCode = 0 }

        # Parse results from the LOG FILE (reliable plain text)
        $passed = 0; $failed = 0; $skipped = 0; $total = 0
        if (Test-Path $entry.Log) {
            $logContent = Get-Content -Path $entry.Log -Raw -ErrorAction SilentlyContinue
            if ($logContent) {
                $passMatch = [regex]::Match($logContent, 'Passed:\s+(\d+)')
                $failMatch = [regex]::Match($logContent, 'Failed:\s+(\d+)')
                $skipMatch = [regex]::Match($logContent, 'Skipped:\s+(\d+)')
                $totalMatch = [regex]::Match($logContent, 'Total tests:\s+(\d+)')

                $passed = if ($passMatch.Success) { [int]$passMatch.Groups[1].Value } else { 0 }
                $failed = if ($failMatch.Success) { [int]$failMatch.Groups[1].Value } else { 0 }
                $skipped = if ($skipMatch.Success) { [int]$skipMatch.Groups[1].Value } else { 0 }

                if ($totalMatch.Success) {
                    $total = [int]$totalMatch.Groups[1].Value
                } else {
                    # "Total tests: Unknown" (test host crash) — derive from counts
                    $total = $passed + $failed + $skipped
                }
            }
        }

        $totalPassed += $passed
        $totalFailed += $failed
        $totalSkipped += $skipped
        $totalTests += $total

        $color = if ($failed -gt 0) { "Red" } else { "Green" }
        Write-Host "  [$($entry.Name)] $passed/$total passed $(if ($failed -gt 0) { "($failed FAILED)" })" -ForegroundColor $color

        if ($jobExitCode -ne 0 -and $testExitCode -eq 0) { $testExitCode = 1 }
        Remove-Job -Job $entry.Job
    }
}

$stopwatch.Stop()
$elapsed = $stopwatch.Elapsed

Write-Host ""
Write-Host "mRemoteNGTests completed in $($elapsed.Minutes)m $($elapsed.Seconds).$($elapsed.Milliseconds.ToString('000'))s" -ForegroundColor Cyan
if (-not $Sequential) {
    # -- SANITY CHECKS --
    # Detect phantom runs: if total elapsed < 10s, tests didn't actually execute
    if ($elapsed.TotalSeconds -lt 10) {
        Write-Host "PHANTOM_TEST_RUN: completed in $($elapsed.TotalSeconds.ToString('F1'))s -- tests did NOT run!" -ForegroundColor Red
        exit 99
    }
    # Detect garbled output: passed > total is impossible
    if ($totalTests -gt 0 -and $totalPassed -gt $totalTests) {
        Write-Host "GARBLED_OUTPUT: $totalPassed/$totalTests passed -- concurrent output corruption!" -ForegroundColor Red
        exit 98
    }
    # Detect empty runs: 0 tests found
    if ($totalTests -eq 0) {
        Write-Host "NO_TESTS_FOUND: 0 tests executed -- DLL may be stale or incompatible" -ForegroundColor Red
        exit 97
    }
    Write-Host "Total: $totalPassed/$totalTests passed, $totalFailed failed" -ForegroundColor $(if ($totalFailed -gt 0) { "Red" } else { "Green" })

    # Detect uncovered tests: compare parallel sum vs full DLL test count
    # If agents added tests in new namespaces not covered by group filters, warn loudly
    $listOutput = & dotnet test $testDll --list-tests --verbosity quiet 2>&1 | Out-String
    $allTestCount = ($listOutput -split "`n" | Where-Object { $_ -match '^\s{4}\S' }).Count
    if ($allTestCount -gt 0 -and $totalTests -lt $allTestCount) {
        $missing = $allTestCount - $totalTests
        Write-Host "WARNING: $missing tests NOT covered by parallel groups ($totalTests/$allTestCount). Update group filters in run-tests.ps1!" -ForegroundColor Yellow
    }
}

# --- Step 7: Run specs ---
if (Test-Path $specsDll) {
    Write-Host ""
    Write-Host "Running mRemoteNGSpecs..." -ForegroundColor Green
    & dotnet test $specsDll --verbosity normal -- NUnit.DefaultTimeout=$Timeout
    if ($LASTEXITCODE -ne 0 -and $testExitCode -eq 0) {
        $testExitCode = $LASTEXITCODE
    }
}

# --- Step 8: Kill leftover processes ---
Write-Host ""
Write-Host "Cleaning leftover processes..." -ForegroundColor Yellow
foreach ($proc in @('testhost', 'notepad')) {
    $killed = Get-Process -Name $proc -ErrorAction SilentlyContinue
    if ($killed) {
        $killed | Stop-Process -Force -ErrorAction SilentlyContinue
        Write-Host "  Killed $($killed.Count) $proc process(es)"
    }
}

# --- Step 9: Summary ---
Write-Host ""
if ($testExitCode -eq 0) {
    Write-Host "ALL TESTS PASSED ($($elapsed.Minutes)m $($elapsed.Seconds)s)" -ForegroundColor Green
} else {
    Write-Host "TESTS FAILED (exit code $testExitCode)" -ForegroundColor Red
}

exit $testExitCode

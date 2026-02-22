# run-tests.ps1 - Multi-process parallel test runner for mRemoteNG
#
# WHY MULTI-PROCESS: The production code uses shared mutable singletons
# (DefaultConnectionInheritance.Instance, Runtime.ConnectionsService, Runtime.EncryptionKey)
# that are not thread-safe. NUnit fixture-level parallelism causes race conditions.
# Instead, we run separate dotnet test processes (each with isolated static state)
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
Write-Host "Mode: $(if ($Sequential) { 'Sequential (1 process)' } else { 'Full parallel (6 processes)' })"
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

# --- Step 5: Define test groups (each runs in a separate process) ---      
$groups = @(
    @{
        Name = "Security"
        Filter = "FullyQualifiedName~mRemoteNGTests.Security"
    },
    @{
        Name = "Tools+Misc"
        Filter = "FullyQualifiedName~mRemoteNGTests.Tools|FullyQualifiedName~mRemoteNGTests.Messages|FullyQualifiedName~mRemoteNGTests.App|FullyQualifiedName~mRemoteNGTests.Container|FullyQualifiedName~mRemoteNGTests.ExternalConnectors|FullyQualifiedName~mRemoteNGTests.Installer|FullyQualifiedName~mRemoteNGTests.BinaryFileTests|FullyQualifiedName~mRemoteNGTests.Properties|FullyQualifiedName~mRemoteNGTests.TestHelpers|FullyQualifiedName~mRemoteNGTests.Themes"
    },
    @{
        Name = "Config"
        Filter = "FullyQualifiedName~mRemoteNGTests.Config"
    },
    @{
        Name = "Connection+Rest"
        Filter = "FullyQualifiedName~mRemoteNGTests.Connection|FullyQualifiedName~mRemoteNGTests.Credential|FullyQualifiedName~mRemoteNGTests.IntegrationTests|FullyQualifiedName~mRemoteNGTests.nUnitForms|FullyQualifiedName~mRemoteNGTests.Tree|FullyQualifiedName~mRemoteNGTests.BinaryFile"
    },
    @{
        Name = "UI"
        Filter = "FullyQualifiedName~mRemoteNGTests.UI"
    },
    @{
        Name = "Root"
        Filter = "FullyQualifiedName~mRemoteNGTests&!FullyQualifiedName~mRemoteNGTests."
    }
)

# --- Step 6: Run tests ---
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

if ($Sequential) {
    Write-Host "Running all tests sequentially..." -ForegroundColor Green   
    $seqArgs = @('test', $testDll, '--verbosity', 'normal', '-s', $runSettings)
    $seqArgs += '--'
    $seqArgs += "NUnit.DefaultTimeout=$Timeout"
    & dotnet @seqArgs
    $testExitCode = $LASTEXITCODE
} else {
    Write-Host "Launching $($groups.Count) parallel test processes..." -ForegroundColor Green
    Write-Host ""

    $jobs = @()
    $logDir = "$repoRoot\TestResults"
    if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }

    foreach ($group in $groups) {
        $logFile = "$logDir\$($group.Name).log"
        $job = Start-Job -ScriptBlock {
            param($dll, $settings, $filter, $timeout, $log)
            $args = @('test', $dll, '--verbosity', 'normal', '-s', $settings, '--filter', "$filter", '--', "NUnit.DefaultTimeout=$timeout")
            & dotnet @args 2>&1 | Tee-Object -FilePath $log
            $LASTEXITCODE
        } -ArgumentList $testDll, $runSettings, $group.Filter, $Timeout, $logFile
        $jobs += @{ Job = $job; Name = $group.Name; Log = $logFile }        
    }

    $testExitCode = 0
    $totalPassed = 0
    $totalFailed = 0
    $totalSkipped = 0
    $totalTests = 0

    foreach ($entry in $jobs) {
        $result = Receive-Job -Job $entry.Job -Wait
        $jobExitCode = 0
        $tmpInt = 0
        foreach ($item in $result) {
            if ([int]::TryParse("$item", [ref]$tmpInt)) { $jobExitCode = $tmpInt }
        }

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
                $total = if ($totalMatch.Success) { [int]$totalMatch.Groups[1].Value } else { $passed + $failed + $skipped }
            }
        }

        if ($failed -gt 0 -and $jobExitCode -eq 0) { $jobExitCode = 1 }     
        $totalPassed += $passed; $totalFailed += $failed; $totalSkipped += $skipped; $totalTests += $total
        $color = if ($failed -gt 0) { "Red" } else { "Green" }
        Write-Host "  [$($entry.Name)] $passed/$total passed $(if ($failed -gt 0) { "($failed FAILED)" })" -ForegroundColor $color
        if ($jobExitCode -ne 0 -and $testExitCode -eq 0) { $testExitCode = 1 }
        Remove-Job -Job $entry.Job
    }

    # --- Step 6.5: Run Remnant tests (not covered by groups) sequentially ---
    Write-Host ""
    Write-Host "Checking for remnant tests..." -ForegroundColor Yellow
    $listOutput = & dotnet test $testDll --list-tests --verbosity quiet 2>&1 | Out-String
    $allTestCount = ($listOutput -split "`n" | Where-Object { $_ -match '^\s{4}\S' }).Count

    if ($totalTests -lt $allTestCount) {
        Write-Host "Running remnants..." -ForegroundColor Cyan
        $groupPatterns = $groups | ForEach-Object { $_.Filter -replace 'FullyQualifiedName~', '' -replace 'FullyQualifiedName!~', '' }
        $remnantFilter = ($groupPatterns | ForEach-Object { "!FullyQualifiedName~$($_)" }) -join "&"
        $remnantFilter = $remnantFilter -replace '\(', '' -replace '\)', ''
        $remnantLog = "$logDir\Remnants.log"
        & dotnet test $testDll --verbosity normal -s $runSettings --filter "$remnantFilter" -- NUnit.DefaultTimeout=$Timeout 2>&1 | Tee-Object -FilePath $remnantLog
        if ($LASTEXITCODE -ne 0 -and $testExitCode -eq 0) { $testExitCode = 1 }
    }
}

$stopwatch.Stop()
$elapsed = $stopwatch.Elapsed
Write-Host ""
Write-Host "mRemoteNGTests completed in $($elapsed.Minutes)m $($elapsed.Seconds)s" -ForegroundColor Cyan

# --- Step 7: Run specs ---
if (Test-Path $specsDll) {
    Write-Host "Running mRemoteNGSpecs..." -ForegroundColor Green
    & dotnet test $specsDll --verbosity normal -- NUnit.DefaultTimeout=$Timeout
    if ($LASTEXITCODE -ne 0 -and $testExitCode -eq 0) { $testExitCode = $LASTEXITCODE }
}

# --- Step 8: Kill leftover processes ---
foreach ($proc in @('testhost', 'notepad')) { Get-Process -Name $proc -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue }

# --- Step 9: Summary ---
if ($testExitCode -eq 0) {
    Write-Host "ALL TESTS PASSED" -ForegroundColor Green
} else {
    Write-Host "TESTS FAILED (exit code $testExitCode)" -ForegroundColor Red
}
exit $testExitCode

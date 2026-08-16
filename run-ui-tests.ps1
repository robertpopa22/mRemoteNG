<#
.SYNOPSIS
    Runs the UI acceptance battery (mRemoteNGSpecs) against the real application.

.DESCRIPTION
    This battery drives the built executable as a user does: it launches windows, clicks, types and
    reads the UI. That makes it slow, and it takes over the desktop while it runs — so it is
    deliberately NOT part of run-tests.ps1 and is gated behind the fast automated suite.

    Order matters. The 6,600-test unit suite finishes in about three minutes and catches almost
    everything; there is no reason to occupy the machine's UI for several more minutes to discover a
    failure the cheap suite would have found. So unless -NoUnitGate is passed, the unit suite runs
    first and the battery only starts if it is green.

    Some scenarios need lab targets (RDP, SSH, MariaDB, VNC). Those tests skip themselves when the
    target is unreachable, exactly like the live SQL tests in the unit suite, so this is safe to run
    on a machine with no lab.

.PARAMETER NoUnitGate
    Skip the unit suite and run the battery directly. For iterating on a single UI test.

.PARAMETER Filter
    NUnit filter expression, e.g. -Filter "FullyQualifiedName~StartupAcceptanceTests".

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File run-ui-tests.ps1
    pwsh -NoProfile -ExecutionPolicy Bypass -File run-ui-tests.ps1 -NoUnitGate -Filter "FullyQualifiedName~Startup"
#>
[CmdletBinding()]
param(
    [switch]$NoUnitGate,
    [string]$Filter = '',
    [string]$ResultsDirectory = "$env:TEMP\mrng-ui-results"
)

$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot

if (-not $NoUnitGate) {
    Write-Host "=== Unit suite first (the battery only runs if this is green) ===" -ForegroundColor Cyan
    & bash "$repo/run-tests-core.sh"
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "Unit suite is not green (exit $LASTEXITCODE). Not starting the UI battery." -ForegroundColor Red
        Write-Host "Fix the automated failures first — they are cheaper to diagnose and this would" -ForegroundColor Red
        Write-Host "occupy the desktop for several minutes to tell you less." -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host ""
}

$dll = Join-Path $repo 'mRemoteNGSpecs\bin\x64\Release\mRemoteNGSpecs.dll'
if (-not (Test-Path $dll)) {
    throw "Battery not built: $dll not found. Run build.ps1 first."
}

# Results outside the repo: TestResults inside the source tree causes cascading testhost crashes on
# .NET 10, which is why run-tests.ps1 does the same.
New-Item -ItemType Directory -Force -Path $ResultsDirectory | Out-Null

Write-Host "=== UI acceptance battery ===" -ForegroundColor Cyan
Write-Host "    The desktop will be driven while this runs. Avoid typing." -ForegroundColor Yellow

$testArgs = @(
    'test', $dll,
    '--results-directory', $ResultsDirectory,
    '--verbosity', 'normal',
    '--logger', 'trx;LogFileName=ui-acceptance.trx'
)
if ($Filter) { $testArgs += @('--filter', $Filter) }

& dotnet @testArgs
$code = $LASTEXITCODE

Write-Host ""
Write-Host "Results: $ResultsDirectory" -ForegroundColor Cyan
if ($code -ne 0) {
    Write-Host "Failed scenarios keep their deployment directory, with a screenshot, a UIA tree" -ForegroundColor Yellow
    Write-Host "dump and the application log under _failure\." -ForegroundColor Yellow
}
exit $code

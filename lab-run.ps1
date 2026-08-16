<#
.SYNOPSIS
    Builds, deploys and runs the UI acceptance battery inside the isolated lab guest.

.DESCRIPTION
    The battery finds things the workstation structurally cannot. Every scenario there starts
    against a machine that already trusts the lab's certificates, has answered the first-run
    prompts, has PuTTY's host key cached and has the Visual C++ runtime installed — so the
    scenarios quietly assume a warm environment and pass. Running the same battery on a clean
    guest surfaced ten defects in one sitting, three of which had been reported as product bugs
    when they were unanswered dialogs, and one of which was a real fix for #143.

    Doing that by hand is a dozen steps: stage the build, archive it, copy it over, unpack it,
    start an interactive task, poll for completion, pull the results back and parse them. This
    script is that sequence, so an iteration costs one command instead of a page of PowerShell.

    Why a scheduled task and not Invoke-Command: PowerShell Direct runs in session 0, which has no
    desktop, so anything launched from there is invisible to UI Automation. A task registered with
    an Interactive principal runs inside the logged-on console session, where a desktop exists.

.PARAMETER Filter
    NUnit filter, e.g. "FullyQualifiedName~SessionLifecycle". Omit to run everything.

.PARAMETER NoBuild
    Skip the build and deploy what is already in bin.

.PARAMETER NoDeploy
    Re-run in the guest against whatever is already deployed there. Much faster when iterating on
    which tests to run rather than on the code.

.PARAMETER Artifacts
    Pull the failure artifacts (screenshot, UIA tree, application log) for every failed scenario.

.EXAMPLE
    pwsh -NoProfile -File lab-run.ps1
    pwsh -NoProfile -File lab-run.ps1 -Filter "FullyQualifiedName~Startup" -NoBuild
    pwsh -NoProfile -File lab-run.ps1 -NoDeploy -Artifacts
#>
[CmdletBinding()]
param(
    [string]$Filter = '',
    [switch]$NoBuild,
    [switch]$NoDeploy,
    [switch]$Artifacts,
    [string]$VMName = 'mRNG-Lab-WinSrv2025',
    [string]$GuestUser = 'Administrator',
    [int]$TimeoutMinutes = 45
)

$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot

# The guest is a throwaway on an isolated network, but this repository is public: a literal
# password in a committed file is a standing secret regardless of what it protects, and it teaches
# the wrong pattern to whoever copies the file next.
$password = $env:MRNG_LAB_GUEST_PASSWORD
if ([string]::IsNullOrEmpty($password)) {
    throw "Set MRNG_LAB_GUEST_PASSWORD to the lab guest's password before running this."
}

$guestRoot = 'C:\mRNG-Lab'
$stage     = Join-Path ([IO.Path]::GetTempPath()) 'mrng-lab-stage'
$payload   = Join-Path ([IO.Path]::GetTempPath()) 'mrng-lab-payload.zip'

function Write-Step($text) { Write-Host "==> $text" -ForegroundColor Cyan }

if (-not $NoBuild) {
    Write-Step 'Build'
    & pwsh -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo 'build.ps1') -NoRestore
    if ($LASTEXITCODE -ne 0) { throw "Build failed ($LASTEXITCODE)." }
}

$secure = ConvertTo-SecureString $password -AsPlainText -Force
$cred    = [PSCredential]::new($GuestUser, $secure)
$session = New-PSSession -VMName $VMName -Credential $cred

try {
    if (-not $NoDeploy) {
        Write-Step 'Stage'
        if (Test-Path $stage) { [IO.Directory]::Delete($stage, $true) }

        # The layout must mirror the repository: AppDriver resolves the executable four directories
        # above the test assembly, so a flattened copy fails before FlaUI is ever involved.
        # robocopy also normalises file attributes, which Copy-Item -ToSession chokes on.
        $null = robocopy (Join-Path $repo 'mRemoteNG\bin\x64\Release') `
                         (Join-Path $stage 'mRemoteNG\bin\x64\Release') `
                         /E /NFL /NDL /NJH /NJS /R:1 /W:1 /XD Settings _uiscenarios /XF *.log
        $null = robocopy (Join-Path $repo 'mRemoteNGSpecs\bin\x64\Release') `
                         (Join-Path $stage 'mRemoteNGSpecs\bin\x64\Release') `
                         /E /NFL /NDL /NJH /NJS /R:1 /W:1 /XD _uiscenarios

        if (Test-Path $payload) { [IO.File]::Delete($payload) }
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [IO.Compression.ZipFile]::CreateFromDirectory($stage, $payload)
        Write-Host ("    payload {0:N0} MB" -f ((Get-Item $payload).Length / 1MB))

        Write-Step 'Deploy'
        Copy-Item -ToSession $session -Path $payload -Destination 'C:\lab-payload.zip' -Force
        Invoke-Command -Session $session -ScriptBlock {
            param($root)
            foreach ($p in @("$root\mRemoteNG", "$root\mRemoteNGSpecs")) {
                if (Test-Path $p) { [IO.Directory]::Delete($p, $true) }
            }
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            [IO.Compression.ZipFile]::ExtractToDirectory('C:\lab-payload.zip', $root)
        } -ArgumentList $guestRoot
    }

    Write-Step ('Run' + $(if ($Filter) { " ($Filter)" } else { ' (everything)' }))
    Invoke-Command -Session $session -ScriptBlock {
        param($filter)
        [Environment]::SetEnvironmentVariable('MRNG_BATTERY_FILTER', $filter, 'Machine')
        Remove-Item 'C:\mRNG-Lab\_results\done.txt' -ErrorAction SilentlyContinue
        Start-ScheduledTask -TaskName 'mRNG-UI-Battery'
    } -ArgumentList $Filter

    $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
    $spin = 0
    do {
        Start-Sleep -Seconds 20
        $done = Invoke-Command -Session $session -ScriptBlock {
            Test-Path 'C:\mRNG-Lab\_results\done.txt'
        }
        $spin++
        Write-Host ("    running... {0:mm\:ss}" -f ([TimeSpan]::FromSeconds($spin * 20))) -NoNewline
        Write-Host "`r" -NoNewline
    } while (-not $done -and (Get-Date) -lt $deadline)
    Write-Host ''

    if (-not $done) { throw "The battery did not finish within $TimeoutMinutes minutes." }

    Write-Step 'Results'
    $summary = Invoke-Command -Session $session -ScriptBlock {
        $trx = 'C:\mRNG-Lab\_results\ui-acceptance.trx'
        if (-not (Test-Path $trx)) { return $null }

        [xml]$doc = Get-Content $trx -Raw
        $counters = $doc.TestRun.ResultSummary.Counters

        $failures = @()
        foreach ($result in $doc.TestRun.Results.UnitTestResult) {
            if ($result.outcome -eq 'Failed') {
                $failures += [pscustomobject]@{
                    Name    = $result.testName
                    Message = ($result.Output.ErrorInfo.Message -split "`n" | Select-Object -First 4) -join "`n"
                    Output  = ($result.Output.StdOut -split "`n" |
                               Where-Object { $_ -match 'dialogs on screen|answered|focus |close failed|top-level' }) -join "`n"
                }
            }
        }

        [pscustomobject]@{
            Total    = [int]$counters.total
            Passed   = [int]$counters.passed
            Failed   = [int]$counters.failed
            Failures = $failures
        }
    }

    if ($null -eq $summary) { throw 'The run produced no results file.' }

    $colour = if ($summary.Failed -eq 0) { 'Green' } else { 'Red' }
    Write-Host ("    total {0}   passed {1}   failed {2}" -f $summary.Total, $summary.Passed, $summary.Failed) `
               -ForegroundColor $colour

    foreach ($failure in $summary.Failures) {
        Write-Host ''
        Write-Host "    FAILED  $($failure.Name)" -ForegroundColor Red
        $failure.Message -split "`n" | ForEach-Object { Write-Host "      $_" }
        if ($failure.Output) {
            Write-Host '      --- what the scenario saw ---' -ForegroundColor DarkGray
            $failure.Output -split "`n" | ForEach-Object { Write-Host "      $_" -ForegroundColor DarkGray }
        }
    }

    if ($Artifacts -and $summary.Failed -gt 0) {
        Write-Step 'Artifacts'
        $local = Join-Path $repo 'lab-artifacts'
        New-Item -ItemType Directory -Force -Path $local | Out-Null

        $dirs = Invoke-Command -Session $session -ScriptBlock {
            Get-ChildItem 'C:\mRNG-Lab\mRemoteNGSpecs\bin\x64\Release\_uiscenarios' -Directory -ErrorAction SilentlyContinue |
                Where-Object { Test-Path (Join-Path $_.FullName '_failure') } |
                Select-Object -ExpandProperty FullName
        }

        foreach ($dir in $dirs) {
            $name = Split-Path $dir -Leaf
            $target = Join-Path $local $name
            New-Item -ItemType Directory -Force -Path $target | Out-Null
            foreach ($file in @('desktop.png', 'uia-tree.txt', 'mRemoteNG.log')) {
                $remote = Join-Path $dir "_failure\$file"
                try { Copy-Item -FromSession $session -Path $remote -Destination $target -Force -ErrorAction Stop }
                catch { }
            }
            Write-Host "    $target"
        }
    }

    exit $(if ($summary.Failed -eq 0) { 0 } else { 1 })
}
finally {
    Remove-PSSession $session
}

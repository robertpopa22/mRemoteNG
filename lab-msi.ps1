<#
.SYNOPSIS
    Installs the MSI in the isolated lab guest and verifies the installed application.

.DESCRIPTION
    Installing an MSI is the one verification that cannot be done on a developer workstation: it
    writes to Program Files, registers an uninstall entry and replaces whatever mRemoteNG is
    already there. The project rules forbid it for exactly that reason, which left the installer
    untested by anything except CI producing the file. A disposable guest removes the objection.

    Three things are checked, in order of how badly they have bitten before:

    1. The install succeeds and lays the application out where the manifest says.

    2. A same-version UPGRADE actually replaces the files on disk. This is issue #129: an upgrade
       left stale DLLs behind, so the application ran with a mixture of old and new assemblies and
       failed in ways no test could reproduce. The fix was AllowSameVersionUpgrades plus scheduling
       RemoveExistingProducts after InstallInitialize.

       It has to be two separately built MSIs, not the same file twice. Package.wxs does not pin a
       ProductCode, so every build generates a new one — which is what makes the second install an
       upgrade. Installing the identical file again is a *repair*, and the log says so plainly:
       "Skipping RemoveExistingProducts action: current configuration is maintenance mode". A repair
       also honours file-versioning rules and deliberately preserves modified unversioned files, so
       that route reports a failure for behaviour that is entirely correct. Measured, and worth
       recording: the first version of this check did exactly that and blamed #129 for it.

    3. The installed application starts and shows a window. That exercises the shipped assembly
       layout, which is what #150 broke and what no unit test can see, because the suite runs
       against the build output rather than against what the installer chose to include.

    Launching happens through a scheduled task with an Interactive principal: PowerShell Direct
    lands in session 0, which has no desktop, so a window would never appear there.

.EXAMPLE
    $env:MRNG_LAB_GUEST_PASSWORD = '...'
    pwsh -NoProfile -File lab-msi.ps1
#>
[CmdletBinding()]
param(
    [string]$VMName = 'mRNG-Lab-WinSrv2025',
    [string]$GuestUser = 'Administrator',
    [switch]$KeepInstalled
)

$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot

$password = $env:MRNG_LAB_GUEST_PASSWORD
if ([string]::IsNullOrEmpty($password)) {
    throw "Set MRNG_LAB_GUEST_PASSWORD to the lab guest's password before running this."
}

$msi = Join-Path $repo 'mRemoteNGInstaller\bin\Release\en-US\mRemoteNG-Installer.msi'
if (-not (Test-Path $msi)) {
    throw "No MSI at $msi. Run build-msi.ps1 first."
}

# Two builds, two ProductCodes, so the second install is an upgrade rather than a repair.
$temp = [IO.Path]::GetTempPath()
$msiFirst = Join-Path $temp 'mRemoteNG-Installer-first.msi'
Copy-Item $msi $msiFirst -Force

Write-Host "==> Building a second installer (new ProductCode)" -ForegroundColor Cyan

# Output is held rather than shown: the WiX toolchain probes for Visual Studio and prints
# "'vswhere.exe' is not recognized" on the way, which is alarming and harmless. The build is
# verified by its exit code and by the two installers differing, so the noise buys nothing — but it
# is printed in full if the build actually fails.
$buildOutput = & pwsh -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo 'build-msi.ps1') 2>&1
if ($LASTEXITCODE -ne 0) {
    $buildOutput | ForEach-Object { Write-Host $_ }
    throw "The second MSI build failed ($LASTEXITCODE)."
}

$msiSecond = Join-Path $temp 'mRemoteNG-Installer-second.msi'
Copy-Item $msi $msiSecond -Force

if ((Get-FileHash $msiFirst).Hash -eq (Get-FileHash $msiSecond).Hash) {
    throw "The two installers are byte-identical, so the second install would be a repair, not an upgrade."
}

function Write-Step($text) { Write-Host "==> $text" -ForegroundColor Cyan }
function Write-Pass($text) { Write-Host "    PASS  $text" -ForegroundColor Green }
function Write-Fail($text) { Write-Host "    FAIL  $text" -ForegroundColor Red }

$secure  = ConvertTo-SecureString $password -AsPlainText -Force
$cred    = [PSCredential]::new($GuestUser, $secure)
$session = New-PSSession -VMName $VMName -Credential $cred
$failures = 0

try {
    Write-Step 'Copy installers'
    Copy-Item -ToSession $session -Path $msiFirst  -Destination 'C:\mRemoteNG-first.msi'  -Force
    Copy-Item -ToSession $session -Path $msiSecond -Destination 'C:\mRemoteNG-second.msi' -Force
    Write-Host ("    {0:N1} MB each" -f ((Get-Item $msiFirst).Length / 1MB))

    Write-Step 'Install'
    $install = Invoke-Command -Session $session -ScriptBlock {
        # Any earlier install has to go first, or this measures an upgrade instead of an install.
        Get-Process mRemoteNG -ErrorAction SilentlyContinue | Stop-Process -Force
        $existing = Get-ChildItem 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
                                  'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall' `
                                  -ErrorAction SilentlyContinue |
            ForEach-Object { Get-ItemProperty $_.PSPath } |
            Where-Object { $_.DisplayName -like 'mRemoteNG*' }

        foreach ($product in $existing) {
            Start-Process msiexec.exe -ArgumentList "/x", $product.PSChildName, "/qn", "/norestart" -Wait
        }

        $log = 'C:\msi-install.log'
        $p = Start-Process msiexec.exe -ArgumentList "/i", "C:\mRemoteNG-first.msi", "/qn", "/norestart", "/l*v", $log -Wait -PassThru

        $exe = 'C:\Program Files\mRemoteNG\mRemoteNG.exe'
        [pscustomobject]@{
            ExitCode  = $p.ExitCode
            ExePath   = $exe
            Installed = Test-Path $exe
            FileCount = if (Test-Path 'C:\Program Files\mRemoteNG') {
                            (Get-ChildItem 'C:\Program Files\mRemoteNG' -Recurse -File).Count
                        } else { 0 }
        }
    }

    if ($install.ExitCode -eq 0 -and $install.Installed) {
        Write-Pass "installed ($($install.FileCount) files) at $($install.ExePath)"
    } else {
        Write-Fail "install failed: msiexec exit $($install.ExitCode), exe present = $($install.Installed)"
        $failures++
        throw 'Install failed; the remaining checks would be meaningless.'
    }

    Write-Step 'Same-version upgrade replaces files (#129)'
    $upgrade = Invoke-Command -Session $session -ScriptBlock {
        # Stand in for a stale file from an earlier version: if the upgrade skips it because the
        # version did not change, the marker survives and that is exactly the #129 defect.
        $victim = 'C:\Program Files\mRemoteNG\mRemoteNG.dll'
        if (-not (Test-Path $victim)) { $victim = 'C:\Program Files\mRemoteNG\mRemoteNG.exe' }

        $original = Get-FileHash $victim -Algorithm SHA256
        $backup = "$victim.orig"
        Copy-Item $victim $backup -Force
        Set-Content -Path $victim -Value 'STALE FILE MARKER' -Encoding ascii -Force
        $marker = Get-FileHash $victim -Algorithm SHA256

        $p = Start-Process msiexec.exe -ArgumentList "/i", "C:\mRemoteNG-second.msi", "/qn", "/norestart",
                                                     "/l*v", "C:\msi-upgrade.log" -Wait -PassThru
        $after = if (Test-Path $victim) { Get-FileHash $victim -Algorithm SHA256 } else { $null }

        # Whether the upgrade path even ran is the first thing to know if this check fails.
        #
        # Detected by WIX_UPGRADE_DETECTED carrying the previous ProductCode, NOT by the absence of
        # "Skipping RemoveExistingProducts": a major upgrade spawns a nested uninstall of the old
        # product, and that nested sequence logs exactly that line every time. Keying on it reported
        # "handled as maintenance" for a textbook upgrade.
        $removed = Select-String -Path 'C:\msi-upgrade.log' `
                                 -Pattern 'Adding WIX_UPGRADE_DETECTED property' -Quiet

        # Whatever happens, leave a working install behind for the launch check.
        if ($null -eq $after -or $after.Hash -eq $marker.Hash) {
            Copy-Item $backup $victim -Force
        }
        Remove-Item $backup -Force -ErrorAction SilentlyContinue

        [pscustomobject]@{
            File        = $victim
            ExitCode    = $p.ExitCode
            Restored    = ($null -ne $after -and $after.Hash -eq $original.Hash)
            Stale       = ($null -ne $after -and $after.Hash -eq $marker.Hash)
            UpgradeRan  = $removed
        }
    }

    if (-not $upgrade.UpgradeRan) {
        Write-Fail ("the second install was handled as maintenance, not an upgrade — " +
                    "RemoveExistingProducts was skipped, so this run says nothing about #129")
        $failures++
    } elseif ($upgrade.Restored) {
        Write-Pass "same-version upgrade replaced $(Split-Path $upgrade.File -Leaf)"
    } elseif ($upgrade.Stale) {
        Write-Fail "the marker survived the upgrade — it skipped $($upgrade.File). This is #129."
        $failures++
    } else {
        Write-Fail "the upgrade left $($upgrade.File) in an unexpected state (msiexec exit $($upgrade.ExitCode))"
        $failures++
    }

    Write-Step 'Installed application starts'
    $probe = @'
$ErrorActionPreference = 'Continue'
$out = 'C:\msi-launch.txt'
$exe = 'C:\Program Files\mRemoteNG\mRemoteNG.exe'
$p = Start-Process $exe -PassThru
Start-Sleep -Seconds 25
$p.Refresh()
if ($p.HasExited) {
    "EXITED $($p.ExitCode)" | Out-File $out -Encoding ascii
} else {
    "WINDOW $($p.MainWindowHandle) TITLE $($p.MainWindowTitle)" | Out-File $out -Encoding ascii
    $p | Stop-Process -Force
}
'@
    Invoke-Command -Session $session -ScriptBlock {
        param($script)
        Set-Content -Path 'C:\msi-launch-probe.ps1' -Value $script -Encoding utf8
        Remove-Item 'C:\msi-launch.txt' -ErrorAction SilentlyContinue
        Unregister-ScheduledTask -TaskName 'mRNG-MSI-Launch' -Confirm:$false -ErrorAction SilentlyContinue
        $a = New-ScheduledTaskAction -Execute 'powershell.exe' `
                -Argument '-NoProfile -ExecutionPolicy Bypass -File C:\msi-launch-probe.ps1'
        $pr = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType Interactive -RunLevel Highest
        Register-ScheduledTask -TaskName 'mRNG-MSI-Launch' -Action $a -Principal $pr | Out-Null
        Start-ScheduledTask -TaskName 'mRNG-MSI-Launch'
    } -ArgumentList $probe

    $launch = $null
    $deadline = (Get-Date).AddMinutes(3)
    do {
        Start-Sleep -Seconds 10
        $launch = Invoke-Command -Session $session -ScriptBlock {
            if (Test-Path 'C:\msi-launch.txt') { (Get-Content 'C:\msi-launch.txt' -Raw).Trim() } else { $null }
        }
    } while (-not $launch -and (Get-Date) -lt $deadline)

    if ($launch -like 'WINDOW*' -and $launch -notlike 'WINDOW 0 *') {
        Write-Pass "the installed application showed a window — $launch"
    } else {
        Write-Fail "the installed application did not show a window — $($launch ?? 'no result')"
        $failures++
    }

    if (-not $KeepInstalled) {
        Write-Step 'Uninstall'
        $removed = Invoke-Command -Session $session -ScriptBlock {
            Get-Process mRemoteNG -ErrorAction SilentlyContinue | Stop-Process -Force
            $product = Get-ChildItem 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
                                     'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall' `
                                     -ErrorAction SilentlyContinue |
                ForEach-Object { Get-ItemProperty $_.PSPath } |
                Where-Object { $_.DisplayName -like 'mRemoteNG*' } |
                Select-Object -First 1

            if ($null -eq $product) { return [pscustomobject]@{ Found = $false; Clean = $false } }

            Start-Process msiexec.exe -ArgumentList "/x", $product.PSChildName, "/qn", "/norestart" -Wait
            [pscustomobject]@{ Found = $true; Clean = -not (Test-Path 'C:\Program Files\mRemoteNG\mRemoteNG.exe') }
        }

        if ($removed.Found -and $removed.Clean) {
            Write-Pass 'uninstall removed the application'
        } else {
            Write-Fail "uninstall left something behind (found=$($removed.Found) clean=$($removed.Clean))"
            $failures++
        }
    }

    Write-Host ''
    if ($failures -eq 0) {
        Write-Host '    installer verified' -ForegroundColor Green
    } else {
        Write-Host "    $failures check(s) failed" -ForegroundColor Red
    }
    exit $failures
}
finally {
    Remove-PSSession $session
}

<#
.SYNOPSIS
    Verifies in the lab guest where an installed copy keeps its settings, before and after an upgrade (#214).

.DESCRIPTION
    The MSI ships the build compiled as portable. A portable build chooses its settings folder by
    whether it can write beside the executable, so an installed copy used %APPDATA% when a standard
    user ran it and the program folder when it ran elevated, and it reported itself as portable.

    This runs, in the isolated guest:

    1. the BASELINE installer: a standard user, then an elevated administrator;
    2. an upgrade to the CANDIDATE installer: the same two runs;
    3. a portable copy of the candidate without the installer marker.

    It reads, for every run, which settings file the application logged, where its log went, and the
    edition it announced. The checks are what charter D11 asks of #214: existing settings still read
    after the upgrade, nothing written to the program folder, the right edition shown, and a portable
    copy still portable.

    The standard user is created for the run with a random password that never leaves memory, and is
    removed afterwards with its profile.

.EXAMPLE
    $env:MRNG_LAB_GUEST_PASSWORD = '...'
    pwsh -NoProfile -File lab-msi-edition.ps1 -BaselineMsi .\baseline.msi -CandidateMsi .\candidate.msi
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$BaselineMsi,
    [Parameter(Mandatory)][string]$CandidateMsi,
    [string]$ConnectionsFixture = (Join-Path $PSScriptRoot 'mRemoteNGTests\Resources\confCons_v2_6.xml'),
    [string]$VMName = 'mRNG-Lab-WinSrv2025',
    [string]$GuestUser = 'Administrator'
)

$ErrorActionPreference = 'Stop'
$password = $env:MRNG_LAB_GUEST_PASSWORD
if ([string]::IsNullOrEmpty($password)) { throw 'Set MRNG_LAB_GUEST_PASSWORD before running this.' }
foreach ($m in $BaselineMsi, $CandidateMsi) { if (-not (Test-Path $m)) { throw "Missing installer: $m" } }
if ((Get-FileHash $BaselineMsi).Hash -eq (Get-FileHash $CandidateMsi).Hash) { throw 'Baseline and candidate are the same file.' }

function Write-Step($text) { Write-Host "==> $text" -ForegroundColor Cyan }
function Write-Pass($text) { Write-Host "    PASS  $text" -ForegroundColor Green }
function Write-Fail($text) { Write-Host "    FAIL  $text" -ForegroundColor Red; $script:failures++ }
function Write-Info($text) { Write-Host "    $text" }
function Show-Run($run) {
    Write-Info "edition:  $($run.Starting)"
    Write-Info "settings: $($run.SettingsFile)"
    Write-Info "log:      $($run.Log)"
    Write-Info "connections loaded: $($run.Nodes); process seen: $($run.ProcessSeen); task result: $($run.TaskResult); profiles: $($run.Profiles)"
    Write-Info "windows: $($run.Windows)"
    Write-Info "per-user folder: $($run.PerUserFiles)"
    if ($null -eq $run.Nodes -and $run.Tail) { Write-Info "log tail:`n      $($run.Tail)" }
}

$failures = 0
# A fresh account per run: a profile left half-deleted by an earlier run makes Windows hand the
# next logon a broken profile, and the application then dies before it can log.
$stdUser = 'mrng-std' + (Get-Random -Minimum 1000 -Maximum 9999)
$stdPassword = -join ((48..57 + 65..90 + 97..122) | Get-Random -Count 24 | ForEach-Object { [char]$_ }) + '!a9'
$cred = [PSCredential]::new($GuestUser, (ConvertTo-SecureString $password -AsPlainText -Force))
$session = New-PSSession -VMName $VMName -Credential $cred

# Runs mRemoteNG once in the guest, as the standard user or as the elevated administrator, then
# reports which settings file and log it used and the edition it announced.
$runApp = {
    param([string]$Exe, [string]$As, [string]$StdUser, [string]$StdPassword)
    $ErrorActionPreference = 'Stop'
    $logName = 'mRemoteNG Connection Manager.log'
    $exeDir = Split-Path $Exe
    $profileUser = if ($As -eq 'std') { $StdUser } else { $env:USERNAME }
    $logCandidates = @(
        (Join-Path $exeDir $logName),
        "C:\Users\$profileUser\AppData\Local\mRemoteNG Connection Manager\$logName"
    )
    foreach ($l in $logCandidates) { Remove-Item $l -Force -ErrorAction SilentlyContinue }
    Get-Process mRemoteNG -ErrorAction SilentlyContinue | Stop-Process -Force

    $taskName = 'mRNG-Edition-Run'
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
    $action = New-ScheduledTaskAction -Execute $Exe
    if ($As -eq 'std') {
        Register-ScheduledTask -TaskName $taskName -Action $action -User $StdUser -Password $StdPassword `
            -RunLevel Limited | Out-Null
    } else {
        $principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType Interactive -RunLevel Highest
        Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal | Out-Null
    }
    Start-ScheduledTask -TaskName $taskName
    Start-Sleep -Seconds 30
    $ran = @(Get-Process mRemoteNG -ErrorAction SilentlyContinue).Count
    # Every visible top-level window of the process: a modal dialog explains a stalled startup.
    if (-not ('LabWin.Enum' -as [type])) {
        Add-Type -Namespace LabWin -Name Enum -MemberDefinition @'
public delegate bool EnumProc(System.IntPtr h, System.IntPtr l);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc f, System.IntPtr l);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(System.IntPtr h, out uint p);
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool IsWindowVisible(System.IntPtr h);
[System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] public static extern int GetWindowText(System.IntPtr h, System.Text.StringBuilder s, int n);
public static string Titles(uint pid) {
    var list = new System.Collections.Generic.List<string>();
    EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p);
        if (p == pid && IsWindowVisible(h)) { var sb = new System.Text.StringBuilder(256); GetWindowText(h, sb, 256); list.Add(sb.ToString()); }
        return true; }, System.IntPtr.Zero);
    return string.Join(" | ", list);
}
'@
    }
    $windows = (Get-Process mRemoteNG -ErrorAction SilentlyContinue | ForEach-Object { [LabWin.Enum]::Titles([uint32]$_.Id) }) -join ' || '
    $settingsDirFiles = (Get-ChildItem "C:\Users\$profileUser\AppData\Roaming\mRemoteNG Connection Manager" -ErrorAction SilentlyContinue).Name -join ','
    Get-Process mRemoteNG -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2
    $taskResult = (Get-ScheduledTaskInfo -TaskName $taskName -ErrorAction SilentlyContinue).LastTaskResult
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    $log = $logCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $log) {
        # A profile that Windows could not load lands in C:\Users\TEMP*; look for any fresh log.
        $log = Get-ChildItem 'C:\Users\*\AppData\Local\mRemoteNG Connection Manager' -Filter $logName -ErrorAction SilentlyContinue |
            Where-Object { $_.LastWriteTime -gt (Get-Date).AddMinutes(-2) } |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
    }
    $lines = if ($log) { Get-Content $log } else { @() }
    $starting = $lines | Where-Object { $_ -match 'Connection Manager .* starting\.' } | Select-Object -Last 1
    $settings = $lines | Where-Object { $_ -match 'User settings file: ' } | Select-Object -Last 1
    $load = $lines | Where-Object { $_ -match 'event=connections_load .*nodes=(\d+)' } | Select-Object -Last 1
    [pscustomobject]@{
        Nodes          = if ($load) { [int]([regex]::Match($load, 'nodes=(\d+)')).Groups[1].Value } else { $null }
        ProcessSeen    = $ran
        Windows        = $windows
        PerUserFiles   = $settingsDirFiles
        TaskResult     = $taskResult
        Profiles       = (Get-ChildItem 'C:\Users' -Directory | Where-Object { $_.Name -like "$profileUser*" -or $_.Name -like 'TEMP*' }).Name -join ','
        Tail           = ($lines | Where-Object { $_ -notmatch 'event=(heartbeat|resource_sample)' } |
                          Select-Object -Last 12 | ForEach-Object {
                              $s = $_ -replace '^\S+ mono_ms=\d+ pid=\d+ app_session=\w+ ', ''
                              $s.Substring(0, [Math]::Min(170, $s.Length)) }) -join "`n      "
        Log            = $log
        Starting       = if ($starting) { ($starting -replace '^.*INFO\s+-\s+', '') } else { $null }
        SettingsFile   = if ($settings) { ([regex]::Match($settings, 'User settings file: (.+?) \(')).Groups[1].Value } else { $null }
        ProgramSettings = Test-Path (Join-Path $exeDir 'Settings')
        Marker         = Test-Path (Join-Path $exeDir 'mRemoteNG.installed')
    }
}

$install = {
    param([string]$Msi, [string]$LogFile)
    $p = Start-Process msiexec.exe -ArgumentList '/i', $Msi, '/qn', '/norestart', '/l*v', $LogFile -Wait -PassThru
    $p.ExitCode
}

try {
    Write-Step 'Prepare the guest'
    Copy-Item -ToSession $session -Path $BaselineMsi  -Destination 'C:\mRemoteNG-baseline.msi'  -Force
    Copy-Item -ToSession $session -Path $CandidateMsi -Destination 'C:\mRemoteNG-candidate.msi' -Force
    Invoke-Command -Session $session -ArgumentList $stdUser, $stdPassword -ScriptBlock {
        param($StdUser, $StdPassword)
        Get-Process mRemoteNG -ErrorAction SilentlyContinue | Stop-Process -Force
        Get-ChildItem 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall' -ErrorAction SilentlyContinue |
            ForEach-Object { Get-ItemProperty $_.PSPath } | Where-Object { $_.DisplayName -like 'mRemoteNG*' } |
            ForEach-Object { Start-Process msiexec.exe -ArgumentList '/x', $_.PSChildName, '/qn', '/norestart' -Wait }
        Remove-Item 'C:\Program Files\mRemoteNG', 'C:\mRNG-portable' -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item "C:\Users\$env:USERNAME\AppData\Roaming\mRemoteNG Connection Manager",
                    "C:\Users\$env:USERNAME\AppData\Local\mRemoteNG Connection Manager" -Recurse -Force -ErrorAction SilentlyContinue
        # Remove every account and profile an earlier run of this script left behind.
        Get-LocalUser -Name 'mrng-std*' -ErrorAction SilentlyContinue | Remove-LocalUser
        Get-CimInstance Win32_UserProfile | Where-Object { $_.LocalPath -like '*\mrng-std*' -and -not $_.Loaded } |
            Remove-CimInstance -ErrorAction SilentlyContinue
        Get-ChildItem 'C:\Users' -Directory -Filter 'mrng-std*' -ErrorAction SilentlyContinue |
            ForEach-Object { cmd /c "rd /s /q `"$($_.FullName)`"" 2>$null }
        New-LocalUser -Name $StdUser -Password (ConvertTo-SecureString $StdPassword -AsPlainText -Force) `
            -PasswordNeverExpires -Description 'mRemoteNG lab: standard user for #214' | Out-Null
        Add-LocalGroupMember -Group 'Users' -Member $StdUser
        # A task that runs while the user is not logged on needs the batch logon right, which
        # Performance Log Users holds by default. The user stays a standard user.
        Add-LocalGroupMember -Group 'Performance Log Users' -Member $StdUser
    }

    $exe = 'C:\Program Files\mRemoteNG\mRemoteNG.exe'

    Write-Step 'Baseline install'
    $code = Invoke-Command -Session $session -ScriptBlock $install -ArgumentList 'C:\mRemoteNG-baseline.msi', 'C:\msi-baseline.log'
    if ($code -ne 0) { Write-Fail "baseline install exit $code"; throw 'baseline install failed' }
    Write-Pass 'baseline installed'

    Write-Step 'Baseline, standard user'
    $b1 = Invoke-Command -Session $session -ScriptBlock $runApp -ArgumentList $exe, 'std', $stdUser, $stdPassword
    Show-Run $b1
    if (-not $b1.SettingsFile) { Write-Fail 'baseline standard run logged no settings file' }

    Write-Step 'Baseline, elevated administrator (the defect)'
    $b2 = Invoke-Command -Session $session -ScriptBlock $runApp -ArgumentList $exe, 'admin', $stdUser, $stdPassword
    Show-Run $b2
    Write-Info "program-folder Settings created: $($b2.ProgramSettings)"

    Write-Step 'Baseline, elevated, with real connections in the program folder'
    # Stands in for an administrator who always ran elevated: their connections live in the
    # program folder. The fixture loads without a master password.
    Copy-Item -ToSession $session -Path $ConnectionsFixture -Destination 'C:\Program Files\mRemoteNG\Settings\confCons.xml' -Force
    $b3 = Invoke-Command -Session $session -ScriptBlock $runApp -ArgumentList $exe, 'admin', $stdUser, $stdPassword
    Show-Run $b3
    if ($b3.Nodes -gt 0) { Write-Pass "baseline sees $($b3.Nodes) connections in the program folder" } else { Write-Fail "baseline loaded $($b3.Nodes) connections" }

    Write-Step 'Upgrade to the candidate'
    $code = Invoke-Command -Session $session -ScriptBlock $install -ArgumentList 'C:\mRemoteNG-candidate.msi', 'C:\msi-candidate.log'
    $upgraded = Invoke-Command -Session $session -ScriptBlock {
        Select-String -Path 'C:\msi-candidate.log' -Pattern 'Adding WIX_UPGRADE_DETECTED property' -Quiet
    }
    if ($code -eq 0 -and $upgraded) { Write-Pass 'candidate installed as an upgrade' } else { Write-Fail "upgrade exit $code, upgrade detected $upgraded" }

    Write-Step 'Candidate, standard user'
    $c1 = Invoke-Command -Session $session -ScriptBlock $runApp -ArgumentList $exe, 'std', $stdUser, $stdPassword
    Show-Run $c1
    if ($c1.Marker) { Write-Pass 'installer marker present beside the executable' } else { Write-Fail 'installer marker missing' }
    if ($c1.SettingsFile -and $c1.SettingsFile -eq $b1.SettingsFile) { Write-Pass 'the upgrade reads the same settings file as before' }
    else { Write-Fail "settings file changed: '$($b1.SettingsFile)' -> '$($c1.SettingsFile)'" }
    if ($c1.Starting -and $c1.Starting -notmatch 'Portable') { Write-Pass 'announces the installed edition' } else { Write-Fail "edition line: $($c1.Starting)" }
    if ($null -ne $c1.Nodes) { Write-Pass "standard user loads its own connections without a file picker ($($c1.Nodes))" }
    else { Write-Fail 'standard user did not reach the connections load (file picker or crash)' }

    Write-Step 'Candidate, elevated administrator'
    # The program folder's settings are fingerprinted by content before the run, and must reappear
    # unchanged, either in place or retired as Settings.migrated.
    $fingerprint = {
        param([string]$Dir)
        if (-not (Test-Path $Dir)) { return $null }
        (Get-ChildItem $Dir -File | Sort-Object Name | ForEach-Object { "$($_.Name)=$((Get-FileHash $_.FullName).Hash)" }) -join ';'
    }
    $before = Invoke-Command -Session $session -ScriptBlock $fingerprint -ArgumentList 'C:\Program Files\mRemoteNG\Settings'
    $c2 = Invoke-Command -Session $session -ScriptBlock $runApp -ArgumentList $exe, 'admin', $stdUser, $stdPassword
    $inPlace = Invoke-Command -Session $session -ScriptBlock $fingerprint -ArgumentList 'C:\Program Files\mRemoteNG\Settings'
    $retired = Invoke-Command -Session $session -ScriptBlock $fingerprint -ArgumentList 'C:\Program Files\mRemoteNG\Settings.migrated'
    $after = if ($inPlace) { $inPlace } else { $retired }
    if ($retired -and -not $inPlace) { Write-Pass 'the program folder copy was retired as Settings.migrated' }
    else { Write-Fail "program folder settings not retired (in place: $([bool]$inPlace), retired: $([bool]$retired))" }
    Show-Run $c2
    if ($c2.SettingsFile -and $c2.SettingsFile -like 'C:\Users\*\AppData\Roaming\*') { Write-Pass 'elevated run uses the per-user folder' }
    else { Write-Fail "elevated run settings file: $($c2.SettingsFile)" }
    if ($before -and $before -eq $after) { Write-Pass 'the program folder settings are byte-for-byte unchanged' } else { Write-Fail 'the program folder settings changed' }
    if ($c2.Nodes -eq $b3.Nodes) { Write-Pass "the upgraded elevated run still has its $($c2.Nodes) connections" }
    else { Write-Fail "connections after upgrade: $($c2.Nodes), before: $($b3.Nodes)" }
    if ($c2.Log -and $c2.Log -notlike 'C:\Program Files\*') { Write-Pass 'elevated run logs per user' } else { Write-Fail "elevated log: $($c2.Log)" }

    Write-Step 'Portable copy of the candidate (no marker)'
    Invoke-Command -Session $session -ScriptBlock {
        Copy-Item 'C:\Program Files\mRemoteNG' 'C:\mRNG-portable' -Recurse -Force
        Remove-Item 'C:\mRNG-portable\mRemoteNG.installed', 'C:\mRNG-portable\Settings' -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item 'C:\mRNG-portable\*.log' -Force -ErrorAction SilentlyContinue
    }
    $p1 = Invoke-Command -Session $session -ScriptBlock $runApp -ArgumentList 'C:\mRNG-portable\mRemoteNG.exe', 'admin', $stdUser, $stdPassword
    Show-Run $p1
    if ($p1.SettingsFile -like 'C:\mRNG-portable\Settings\*') { Write-Pass 'portable copy keeps settings beside the executable' } else { Write-Fail "portable settings file: $($p1.SettingsFile)" }
    if ($p1.Starting -match 'Portable') { Write-Pass 'portable copy announces the portable edition' } else { Write-Fail "portable edition line: $($p1.Starting)" }
}
finally {
    try {
        Invoke-Command -Session $session -ArgumentList $stdUser -ScriptBlock {
            param($StdUser)
            Get-Process mRemoteNG -ErrorAction SilentlyContinue | Stop-Process -Force
            Unregister-ScheduledTask -TaskName 'mRNG-Edition-Run' -Confirm:$false -ErrorAction SilentlyContinue
            Get-ChildItem 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall' -ErrorAction SilentlyContinue |
                ForEach-Object { Get-ItemProperty $_.PSPath } | Where-Object { $_.DisplayName -like 'mRemoteNG*' } |
                ForEach-Object { Start-Process msiexec.exe -ArgumentList '/x', $_.PSChildName, '/qn', '/norestart' -Wait }
            Remove-Item 'C:\mRNG-portable', 'C:\Program Files\mRemoteNG' -Recurse -Force -ErrorAction SilentlyContinue
            Get-CimInstance Win32_UserProfile | Where-Object { $_.LocalPath -like "*\$StdUser*" -and -not $_.Loaded } | Remove-CimInstance -ErrorAction SilentlyContinue
            Remove-LocalUser -Name $StdUser -ErrorAction SilentlyContinue
            Remove-Item 'C:\mRemoteNG-baseline.msi', 'C:\mRemoteNG-candidate.msi' -Force -ErrorAction SilentlyContinue
        }
    }
    finally { Remove-PSSession $session }
}

Write-Host ''
if ($failures -eq 0) { Write-Host '    #214 verified in the lab' -ForegroundColor Green } else { Write-Host "    $failures check(s) failed" -ForegroundColor Red }
exit $failures

# Kept in the test project so Sonar classifies this Pester fixture as test code.
Describe 'MSI edition lab orchestration' {
    BeforeAll {
        $subject = Join-Path $PSScriptRoot '../../lab-msi-edition.ps1'
        $installedExe = 'C:\Program Files\mRemoteNG\mRemoteNG.exe'
        $portableExe = 'C:\mRNG-portable\mRemoteNG.exe'
        $programSettings = 'C:\Program Files\mRemoteNG\Settings'
        $userSettings = 'C:\Users\test\AppData\Roaming\mRemoteNG Connection Manager\mRemoteNG.settings'
        $userLog = 'C:\Users\test\AppData\Local\mRemoteNG Connection Manager\mRemoteNG.log'

        function New-Observation($settingsFile, $nodes, $starting, $log, $marker = $false) {
            [pscustomobject]@{
                Nodes = $nodes
                ProcessSeen = 1
                Windows = 'mRemoteNG'
                PerUserFiles = 'confCons.xml,mRemoteNG.settings'
                TaskResult = 0
                Profiles = 'test'
                Tail = 'Synthetic startup diagnostic'
                Log = $log
                Starting = $starting
                SettingsFile = $settingsFile
                ProgramSettings = $settingsFile -like "$programSettings\*"
                Marker = $marker
            }
        }
    }

    BeforeEach {
        $originalPassword = $env:MRNG_LAB_GUEST_PASSWORD
        $env:MRNG_LAB_GUEST_PASSWORD = 'fake-password-for-tests'
        $baseline = Join-Path $TestDrive 'baseline.msi'
        $candidate = Join-Path $TestDrive 'candidate.msi'
        Set-Content -LiteralPath $baseline -Value 'synthetic baseline'
        Set-Content -LiteralPath $candidate -Value 'synthetic candidate'
        $arguments = @{
            BaselineMsi = $baseline
            CandidateMsi = $candidate
            VMName = 'mock-vm-' + [guid]::NewGuid().ToString('N')
            GuestUser = 'fake-guest-user'
        }
        $lab = @{
            BaselineExit = 0
            CandidateExit = 0
            UpgradeDetected = $true
            CleanupFails = $false
            RunGuestStartup = $false
            Fingerprints = @('confCons.xml=abc', $null, 'confCons.xml=abc')
            FingerprintIndex = 0
            Observations = @(
                (New-Observation $userSettings 0 'Portable starting.' $userLog),
                (New-Observation "$programSettings\mRemoteNG.settings" 0 'Portable starting.' 'C:\Program Files\mRemoteNG\mRemoteNG.log'),
                (New-Observation "$programSettings\mRemoteNG.settings" 3 'Portable starting.' 'C:\Program Files\mRemoteNG\mRemoteNG.log'),
                (New-Observation $userSettings 0 'Installed starting.' $userLog $true),
                (New-Observation $userSettings 3 'Installed starting.' $userLog $true),
                (New-Observation 'C:\mRNG-portable\Settings\mRemoteNG.settings' 0 'Portable starting.' 'C:\mRNG-portable\mRemoteNG.log')
            )
            ObservationIndex = 0
            Calls = [System.Collections.Generic.List[string]]::new()
        }

        # Create an object of the right parameter type without opening a runspace.
        $mockSession = New-MockObject -Type ([System.Management.Automation.Runspaces.PSSession])
        Mock New-PSSession { $mockSession }
        Mock Copy-Item {}
        Mock Remove-PSSession {}
        Mock Write-Host {}
        Mock Invoke-Command {
            if ($Session.Count -ne 1 -or $Session[0] -ne $mockSession) { throw 'Expected the fake lab session.' }

            # Inspect the remote operations to select observations; executing a guest block here
            # would run installer, account and filesystem operations on the test host.
            switch -Regex ($ScriptBlock.ToString()) {
                'New-LocalUser' {
                    $lab.Calls.Add('prepare')
                    break
                }
                'Start-Process msiexec\.exe -ArgumentList ''/i''' {
                    $lab.Calls.Add($ArgumentList[0])
                    if ($lab.RunGuestStartup) { return & $ScriptBlock @ArgumentList }
                    switch ($ArgumentList[0]) {
                        'C:\mRemoteNG-baseline.msi' { return $lab.BaselineExit }
                        'C:\mRemoteNG-candidate.msi' { return $lab.CandidateExit }
                        default { throw "Unexpected installer: $($ArgumentList[0])" }
                    }
                }
                'Start-ScheduledTask' {
                    $lab.Calls.Add("$($ArgumentList[0]) [$($ArgumentList[1])]")
                    if ($lab.RunGuestStartup) {
                        $observation = $lab.Observations[$lab.ObservationIndex++]
                        return & $ScriptBlock @ArgumentList
                    }
                    return $lab.Observations[$lab.ObservationIndex++]
                }
                'WIX_UPGRADE_DETECTED' {
                    $lab.Calls.Add('detect upgrade')
                    if ($lab.RunGuestStartup) { return & $ScriptBlock }
                    return $lab.UpgradeDetected
                }
                'Get-FileHash' {
                    $lab.Calls.Add($ArgumentList[0])
                    if ($lab.RunGuestStartup) {
                        $fingerprint = $lab.Fingerprints[$lab.FingerprintIndex++]
                        return & $ScriptBlock @ArgumentList
                    }
                    return $lab.Fingerprints[$lab.FingerprintIndex++]
                }
                'Copy-Item .*mRNG-portable' {
                    $lab.Calls.Add('prepare portable')
                    if ($lab.RunGuestStartup) { & $ScriptBlock }
                    break
                }
                'Remove-LocalUser' {
                    $lab.Calls.Add('cleanup')
                    if ($lab.CleanupFails) { throw 'Synthetic guest cleanup failure.' }
                    break
                }
                default { throw 'Unexpected remote operation.' }
            }
        }
    }

    AfterEach {
        $env:MRNG_LAB_GUEST_PASSWORD = $originalPassword
    }

    It 'checks the baseline, upgrade and portable edition in order and cleans up' {
        & $subject @arguments
        $LASTEXITCODE | Should -Be 0

        $lab.Calls.ToArray() | Should -Be @(
            'prepare', 'C:\mRemoteNG-baseline.msi',
            "$installedExe [std]", "$installedExe [admin]", "$installedExe [admin]",
            'C:\mRemoteNG-candidate.msi', 'detect upgrade', "$installedExe [std]",
            $programSettings, "$installedExe [admin]", $programSettings, "$programSettings.migrated",
            'prepare portable', "$portableExe [admin]", 'cleanup'
        )
        Should -Invoke Copy-Item -Exactly -Times 3
        Should -Invoke Copy-Item -Exactly -Times 1 -ParameterFilter {
            $Path -like '*confCons_v2_6.xml' -and $Destination -eq 'C:\Program Files\mRemoteNG\Settings\confCons.xml'
        }
        Should -Invoke Remove-PSSession -Exactly -Times 1 -ParameterFilter { $Session[0] -eq $mockSession }
    }

    It 'rejects a missing guest password before connecting' {
        $env:MRNG_LAB_GUEST_PASSWORD = $null

        { & $subject @arguments } | Should -Throw '*Set MRNG_LAB_GUEST_PASSWORD*'
        Should -Invoke New-PSSession -Exactly -Times 0
    }

    It 'parses guest startup diagnostics and runs both task registration modes' {
        $lab.RunGuestStartup = $true
        Mock Get-Process {}
        Mock Stop-Process { throw 'The fixture must not stop any real process.' }
        Mock Unregister-ScheduledTask {}
        Mock New-ScheduledTaskPrincipal { [Microsoft.Management.Infrastructure.CimInstance]::new('MSFT_TaskPrincipal') }
        Mock Start-Process {
            if ($FilePath -ne 'msiexec.exe' -or $ArgumentList[0] -ne '/i') { throw 'Unexpected process request.' }
            [pscustomobject]@{ ExitCode = 0 }
        }
        Mock Register-ScheduledTask {}
        Mock Start-ScheduledTask {}
        Mock Start-Sleep {}
        Mock Add-Type {}
        Mock Get-ScheduledTaskInfo { [pscustomobject]@{ LastTaskResult = 0 } }
        Mock Remove-Item { throw 'Unexpected file removal.' }
        Mock Remove-Item {} -ParameterFilter { $Path -like '*mRemoteNG Connection Manager.log' -or $Path[0] -like 'C:\mRNG-portable\*' }
        Mock Select-String { $true } -ParameterFilter { $Path -eq 'C:\msi-candidate.log' -and $Quiet }
        Mock Get-ChildItem { [pscustomobject]@{ Name = 'mRemoteNG.settings' } } -ParameterFilter { $Path -like 'C:\Users*' }
        Mock Get-ChildItem {
            [pscustomobject]@{ FullName = $observation.Log; LastWriteTime = (Get-Date).AddMinutes(-5) }
            [pscustomobject]@{ FullName = $observation.Log; LastWriteTime = Get-Date }
        } -ParameterFilter { $Path -like 'C:\Users\*\AppData\Local\mRemoteNG Connection Manager' }
        Mock Get-ChildItem { [pscustomobject]@{ Name = 'confCons.xml'; FullName = 'C:\Synthetic\confCons.xml' } } -ParameterFilter { $Path -like "$programSettings*" }
        $installerHashes = @{}
        foreach ($installer in $baseline, $candidate) { $installerHashes[$installer] = Get-FileHash $installer }
        Mock Get-FileHash {
            if ($Path -eq 'C:\Synthetic\confCons.xml') { return [pscustomobject]@{ Hash = ($fingerprint -split '=', 2)[1] } }
            if ($installerHashes.ContainsKey($Path[0])) { return $installerHashes[$Path[0]] }
            throw 'Unexpected file hash request.'
        }
        Mock Test-Path {
            if ($Path -in @($baseline, $candidate)) { return [IO.File]::Exists($Path) }
            if ($Path -in @($programSettings, "$programSettings.migrated")) { return $null -ne $fingerprint }
            $Path -eq $observation.Log -or ($observation.Marker -and $Path -like '*mRemoteNG.installed')
        }
        Mock Get-Content {
            "INFO - mRemoteNG Connection Manager $($observation.Starting)"
            "User settings file: $($observation.SettingsFile) (size: 1)"
            "event=connections_load nodes=$($observation.Nodes) duration_ms=1"
        } -ParameterFilter { $Path -like '*mRemoteNG Connection Manager.log' -or $Path -like '*mRemoteNG.log' }

        # Use the real guest startup block with mocked Windows operations and synthetic logs.
        # The session boundary still intercepts guest provisioning and cleanup.
        $lab.Observations[0].Log = 'C:\Program Files\mRemoteNG\mRemoteNG Connection Manager.log'
        $lab.Observations[1].Log = $lab.Observations[0].Log
        $lab.Observations[2].Log = $lab.Observations[0].Log
        $lab.Observations[3].Log = 'C:\Users\TEMP\AppData\Local\mRemoteNG Connection Manager\mRemoteNG Connection Manager.log'
        $lab.Observations[4].Log = "C:\Users\$env:USERNAME\AppData\Local\mRemoteNG Connection Manager\mRemoteNG Connection Manager.log"
        $lab.Observations[5].Log = 'C:\mRNG-portable\mRemoteNG Connection Manager.log'
        & $subject @arguments
        $LASTEXITCODE | Should -Be 0

        Should -Invoke Register-ScheduledTask -Exactly -Times 2 -ParameterFilter { $RunLevel -eq 'Limited' }
        Should -Invoke New-ScheduledTaskPrincipal -Exactly -Times 4 -ParameterFilter { $RunLevel -eq 'Highest' }
        Should -Invoke Start-ScheduledTask -Exactly -Times 6
        Should -Invoke Start-Process -Exactly -Times 2 -ParameterFilter { '/norestart' -in $ArgumentList -and $Wait -and $PassThru }
        Should -Invoke Select-String -Exactly -Times 1 -ParameterFilter { $Pattern -eq 'Adding WIX_UPGRADE_DETECTED property' }
        Should -Invoke Copy-Item -Exactly -Times 1 -ParameterFilter { $Destination -eq 'C:\mRNG-portable' -and $Recurse }
        Should -Invoke Remove-Item -Exactly -Times 1 -ParameterFilter { $Path -contains 'C:\mRNG-portable\mRemoteNG.installed' }
        Should -Invoke Get-Content -Exactly -Times 1 -ParameterFilter { $Path -like 'C:\Users\TEMP\*' }
        Should -Invoke Remove-PSSession -Exactly -Times 1
    }

    It 'rejects a missing installer before connecting' {
        $arguments.CandidateMsi = Join-Path $TestDrive 'missing.msi'

        { & $subject @arguments } | Should -Throw '*Missing installer:*'
        Should -Invoke New-PSSession -Exactly -Times 0
    }

    It 'rejects identical installers even when their filenames differ' {
        Set-Content -LiteralPath $candidate -Value (Get-Content -LiteralPath $baseline)

        { & $subject @arguments } | Should -Throw '*are the same file*'
        Should -Invoke New-PSSession -Exactly -Times 0
    }

    It 'stops after a failed baseline installation and still cleans up' {
        $lab.BaselineExit = 1603

        { & $subject @arguments } | Should -Throw '*baseline install failed*'
        $lab.Calls.ToArray() | Should -Be @('prepare', 'C:\mRemoteNG-baseline.msi', 'cleanup')
        Should -Invoke Remove-PSSession -Exactly -Times 1
    }

    It 'reports each failed check and returns the failure count after cleanup' {
        $lab.Observations[0].SettingsFile = $null
        $lab.Observations[2].Nodes = 0
        $lab.UpgradeDetected = $false
        $lab.Observations[3] = New-Observation $userSettings $null 'Portable starting.' $userLog
        $lab.Fingerprints = @('confCons.xml=abc', 'confCons.xml=def', $null)
        $lab.Observations[4] = New-Observation "$programSettings\mRemoteNG.settings" 1 'Installed starting.' 'C:\Program Files\mRemoteNG\mRemoteNG.log' $true
        $lab.Observations[5] = New-Observation $userSettings 0 'Installed starting.' $userLog

        & $subject @arguments
        $LASTEXITCODE | Should -Be 14

        Should -Invoke Write-Host -Exactly -Times 14 -ParameterFilter { $Object -like '    FAIL  *' }
        Should -Invoke Write-Host -Exactly -Times 1 -ParameterFilter { $Object -like '*Synthetic startup diagnostic*' }
        $lab.Calls[-1] | Should -Be 'cleanup'
        Should -Invoke Remove-PSSession -Exactly -Times 1
    }

    It 'disposes the session even if guest cleanup fails' {
        $lab.CleanupFails = $true

        { & $subject @arguments } | Should -Throw '*Synthetic guest cleanup failure*'
        Should -Invoke Remove-PSSession -Exactly -Times 1
    }
}

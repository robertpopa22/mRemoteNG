[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-CommandVersion {
    param([Parameter(Mandatory)][string]$Name)
    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if (-not $command) { return $null }
    $commandPath = if ($command.Path) { $command.Path } else { $command.Source }
    if (-not $commandPath) { return $null }
    $item = Get-Item -LiteralPath $commandPath -ErrorAction SilentlyContinue
    [pscustomobject]@{
        Available = $true
        Version = $item.VersionInfo.FileVersion
    }
}

$windowsApp = Get-AppxPackage -Name 'MicrosoftCorporationII.WindowsApp' -ErrorAction SilentlyContinue |
    Sort-Object Version -Descending |
    Select-Object -First 1
$systemDirectory = [Environment]::SystemDirectory
$activeXPath = Join-Path $systemDirectory 'mstscax.dll'

[pscustomobject]@{
    EmbeddedActiveX = [pscustomobject]@{
        Available = Test-Path -LiteralPath $activeXPath
        Version = (Get-Item -LiteralPath $activeXPath -ErrorAction SilentlyContinue).VersionInfo.FileVersion
        Default = $true
    }
    Mstsc = Get-CommandVersion -Name 'mstsc.exe'
    WindowsApp = if ($windowsApp) { [pscustomobject]@{ Available = $true; Version = $windowsApp.Version.ToString(); PreviewForRemotePc = $true } } else { [pscustomobject]@{ Available = $false; Version = $null; PreviewForRemotePc = $true } }
    FreeRdp = if ($freeRdp = Get-CommandVersion -Name 'wfreerdp.exe') { $freeRdp } else { [pscustomobject]@{ Available = $false; Version = $null } }
}

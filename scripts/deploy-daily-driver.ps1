<#
.SYNOPSIS
    Deploys a self-contained build to the maintainer's daily-driver install, without touching the
    connections and settings that live there.

.DESCRIPTION
    The daily driver is a real, in-use installation: its Settings folder holds the maintainer's
    servers. The publish output carries a Settings folder of its own — a development one, with an
    empty confCons.xml — so copying `publish\*` over the install silently replaces the user's
    connections with an empty file. That happened on 2026-09-03; the rotating backups were the only
    reason the data came back.

    So this script exists instead of an ad-hoc copy, and it does three things a copy does not:

      * it refuses to run while mRemoteNG is running (never overwrite a live install);
      * it copies the payload with Settings EXCLUDED, in both directions — the old payload is
        deleted except for Settings and logs, and the new payload is copied without its own Settings;
      * it hashes the target's confCons.xml before and after and fails loudly if it differs at all.
        A deploy that alters the user's data is a failed deploy, and it says so instead of finishing
        green.

.PARAMETER Target
    The daily-driver install. Defaults to the maintainer's.

.PARAMETER Source
    The self-contained publish output. Build it with: build.ps1 -SelfContained

.EXAMPLE
    pwsh -NoProfile -File scripts\deploy-daily-driver.ps1
#>
[CmdletBinding()]
param(
    [string]$Target = 'E:\OneDrive\_Portable\mRemoteNG-latest',
    [string]$Source = "$PSScriptRoot\..\mRemoteNG\bin\x64\Release\publish"
)

$ErrorActionPreference = 'Stop'

function Write-Step($text) { Write-Host "==> $text" -ForegroundColor Cyan }

# How many connections does the install hold right now? Counted from the file rather than from the
# app, so it works whether or not the file is encrypted with a master password.
function Get-ConnectionFileFacts([string]$settingsPath) {
    $file = Join-Path $settingsPath 'confCons.xml'
    if (-not (Test-Path -LiteralPath $file)) {
        return [pscustomobject]@{ Exists = $false; Length = 0; Nodes = 0; Hash = '' }
    }

    $content = Get-Content -LiteralPath $file -Raw
    # A fully encrypted file has no readable nodes; the hash is what actually guards it either way.
    $encrypted = $content -match 'FullFileEncryption="true"'
    [pscustomobject]@{
        Exists    = $true
        Length    = (Get-Item -LiteralPath $file).Length
        Nodes     = ([regex]::Matches($content, '<Node ')).Count
        Encrypted = $encrypted
        Hash      = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash
    }
}

if (-not (Test-Path -LiteralPath $Source)) {
    throw "No publish output at '$Source'. Build it first: build.ps1 -SelfContained"
}
if (-not (Test-Path -LiteralPath (Join-Path $Source 'mRemoteNG.exe'))) {
    throw "'$Source' has no mRemoteNG.exe — is that really the publish folder?"
}
if (-not (Test-Path -LiteralPath $Target)) {
    throw "No install at '$Target'."
}

$running = Get-Process mRemoteNG -ErrorAction SilentlyContinue
if ($running) {
    throw ("mRemoteNG is running (PID {0}). Close it first — never overwrite a live install." -f ($running.Id -join ', '))
}

$targetSettings = Join-Path $Target 'Settings'
function Format-Facts($facts) {
    if (-not $facts.Exists) { return 'no confCons.xml' }
    if ($facts.Encrypted)   { return ("{0} bytes, fully encrypted" -f $facts.Length) }
    return ("{0} bytes, {1} connection node(s)" -f $facts.Length, $facts.Nodes)
}

$before = Get-ConnectionFileFacts $targetSettings
Write-Step ("Before: " + (Format-Facts $before))

# A copy of the whole Settings folder, kept aside for this deploy. Cheap, and it is the difference
# between an incident and a footnote.
$snapshot = Join-Path ([IO.Path]::GetTempPath()) ("mrng-daily-settings-{0:yyyyMMdd-HHmmss}" -f (Get-Date))
if (Test-Path -LiteralPath $targetSettings) {
    Write-Step "Snapshot Settings -> $snapshot"
    $null = New-Item -ItemType Directory -Path $snapshot -Force
    $null = robocopy $targetSettings $snapshot /E /NFL /NDL /NJH /NJS /R:1 /W:1
}

Write-Step 'Remove the old payload (Settings and logs stay)'
Get-ChildItem -LiteralPath $Target -Force |
    Where-Object { $_.Name -ne 'Settings' -and $_.Extension -ne '.log' } |
    Remove-Item -Recurse -Force

Write-Step 'Copy the new payload (its own Settings excluded)'
# /XD Settings is the whole point: the publish tree ships a development Settings folder, and copying
# it over a real install replaces the user's connections with an empty file.
$null = robocopy $Source $Target /E /NFL /NDL /NJH /NJS /R:2 /W:2 /XD Settings
if ($LASTEXITCODE -ge 8) { throw "robocopy failed with exit code $LASTEXITCODE." }

$after = Get-ConnectionFileFacts $targetSettings
Write-Step ("After:  " + (Format-Facts $after))

if ($before.Exists -and -not $after.Exists) {
    throw "DEPLOY DAMAGED THE INSTALL: confCons.xml is gone. Restore it from $snapshot"
}
if ($before.Hash -ne $after.Hash) {
    throw ("DEPLOY DAMAGED THE INSTALL: confCons.xml changed ({0} -> {1}). Restore it from {2}" -f `
           (Format-Facts $before), (Format-Facts $after), $snapshot)
}

$version = (Get-Item (Join-Path $Target 'mRemoteNG.dll')).VersionInfo.FileVersion
Write-Host ''
Write-Host ("Deployed {0} to {1}" -f $version, $Target) -ForegroundColor Green
Write-Host ("Connections preserved: " + (Format-Facts $after) + ", unchanged (SHA256 match).") -ForegroundColor Green
Write-Host ("Settings snapshot: {0}" -f $snapshot)

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$SourceDirectory,

    [Parameter(Mandatory)]
    [string]$TargetDirectory,

    [string]$LegacyProfileDirectory,

    [switch]$StopRunningApplication,

    [ValidateRange(1, 300)]
    [int]$CloseTimeoutSeconds = 20,

    [ValidateRange(1, 10)]
    [int]$RollbackCount = 2
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-CanonicalPath {
    param([Parameter(Mandatory)][string]$Path)

    $expanded = [Environment]::ExpandEnvironmentVariables($Path.Trim().Trim('"'))
    if (-not [IO.Path]::IsPathRooted($expanded)) {
        $expanded = Join-Path (Get-Location).Path $expanded
    }

    return [IO.Path]::GetFullPath($expanded).TrimEnd(
        [IO.Path]::DirectorySeparatorChar,
        [IO.Path]::AltDirectorySeparatorChar)
}

function Test-PathInside {
    param(
        [Parameter(Mandatory)][string]$Candidate,
        [Parameter(Mandatory)][string]$Parent
    )

    if ($Candidate.Equals($Parent, [StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    $prefix = $Parent + [IO.Path]::DirectorySeparatorChar
    return $Candidate.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)
}

function Assert-NotRootPath {
    param([Parameter(Mandatory)][string]$Path)

    $root = [IO.Path]::GetPathRoot($Path).TrimEnd(
        [IO.Path]::DirectorySeparatorChar,
        [IO.Path]::AltDirectorySeparatorChar)
    if ($Path.Equals($root, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to deploy to a filesystem root."
    }
}

function Assert-NoReparsePoints {
    param(
        [Parameter(Mandatory)][string]$Path,
        [switch]$Recurse
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    $rootItem = Get-Item -LiteralPath $Path -Force
    if ($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw "Refusing a reparse-point path: $Path"
    }

    if ($Recurse) {
        $reparsePoint = Get-ChildItem -LiteralPath $Path -Force -Recurse |
            Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint } |
            Select-Object -First 1
        if ($reparsePoint) {
            throw "Refusing a tree containing a reparse point: $($reparsePoint.FullName)"
        }
    }
}

function Assert-NoReparsePointAncestors {
    param([Parameter(Mandatory)][string]$Path)

    $current = $Path
    while (-not (Test-Path -LiteralPath $current)) {
        $parent = Split-Path -Parent $current
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current) {
            return
        }
        $current = $parent
    }

    while (-not [string]::IsNullOrWhiteSpace($current)) {
        $item = Get-Item -LiteralPath $current -Force
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw "Refusing a path below a reparse point: $current"
        }
        $parent = Split-Path -Parent $current
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current) {
            break
        }
        $current = $parent
    }
}

function Get-FileHashMap {
    param([Parameter(Mandatory)][string]$Root)

    $map = [ordered]@{}
    if (-not (Test-Path -LiteralPath $Root)) {
        return $map
    }

    Assert-NoReparsePoints -Path $Root -Recurse
    Get-ChildItem -LiteralPath $Root -File -Force -Recurse |
        Sort-Object FullName |
        ForEach-Object {
            $relativePath = [IO.Path]::GetRelativePath($Root, $_.FullName)
            $map[$relativePath] = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        }

    return $map
}

function Assert-HashMapsEqual {
    param(
        [Parameter(Mandatory)]$Expected,
        [Parameter(Mandatory)]$Actual,
        [Parameter(Mandatory)][string]$Description
    )

    if ($Expected.Count -ne $Actual.Count) {
        throw "$Description file count changed ($($Expected.Count) -> $($Actual.Count))."
    }

    foreach ($relativePath in $Expected.Keys) {
        if (-not $Actual.Contains($relativePath) -or
            -not $Actual[$relativePath].Equals($Expected[$relativePath], [StringComparison]::OrdinalIgnoreCase)) {
            throw "$Description hash validation failed."
        }
    }
}

function Copy-ProgramTree {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination
    )

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    Get-ChildItem -LiteralPath $Source -Force |
        Where-Object {
            $_.Name -notin @('Settings', '_rollback', '_deploy-state') -and
            $_.Extension -ne '.log'
        } |
        ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination $Destination -Recurse -Force
        }
}

function Get-ProgramHashMap {
    param([Parameter(Mandatory)][string]$Root)

    $map = [ordered]@{}
    $programItems = @(Get-ChildItem -LiteralPath $Root -Force | Where-Object {
        $_.Name -notin @('Settings', '_rollback', '_deploy-state') -and
        $_.Extension -ne '.log'
    })

    foreach ($programItem in $programItems) {
        Assert-NoReparsePoints -Path $programItem.FullName -Recurse:$programItem.PSIsContainer
        $files = if ($programItem.PSIsContainer) {
            @(Get-ChildItem -LiteralPath $programItem.FullName -File -Force -Recurse)
        } else {
            @($programItem)
        }
        foreach ($file in $files | Sort-Object FullName) {
            $relativePath = [IO.Path]::GetRelativePath($Root, $file.FullName)
            $map[$relativePath] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        }
    }
    return $map
}

function Resolve-LegacyConnectionFile {
    param([Parameter(Mandatory)][string]$LegacyRoot)

    $settingsPath = Join-Path $LegacyRoot 'mRemoteNG.settings'
    if (-not (Test-Path -LiteralPath $settingsPath -PathType Leaf)) {
        throw "Legacy profile has no mRemoteNG.settings file."
    }

    [xml]$settingsXml = Get-Content -LiteralPath $settingsPath -Raw
    $settingNode = $settingsXml.SelectSingleNode(
        "//*[local-name()='setting' and @name='CustomConsPath']")
    $valueNode = if ($settingNode) {
        $settingNode.SelectSingleNode("*[local-name()='value']")
    } else {
        $null
    }
    $configuredPath = if ($valueNode) { $valueNode.InnerText } elseif ($settingNode) { $settingNode.InnerText } else { '' }
    if ([string]::IsNullOrWhiteSpace($configuredPath)) {
        throw "Legacy settings do not identify the active connection file."
    }

    $candidate = [Environment]::ExpandEnvironmentVariables($configuredPath.Trim())
    if (-not [IO.Path]::IsPathRooted($candidate)) {
        $candidate = Join-Path $LegacyRoot $candidate
    }
    $candidate = Get-CanonicalPath -Path $candidate

    if (-not (Test-PathInside -Candidate $candidate -Parent $LegacyRoot)) {
        throw "Legacy settings point outside the supplied legacy profile directory."
    }
    Assert-NoReparsePoints -Path $candidate
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        throw "The active legacy connection file does not exist."
    }

    return $candidate
}

function Initialize-PortableProfile {
    param(
        [Parameter(Mandatory)][string]$Target,
        [string]$LegacyRoot
    )

    $settingsDirectory = Join-Path $Target 'Settings'
    $connectionFile = Join-Path $settingsDirectory 'confCons.xml'
    $stateDirectory = Join-Path $Target '_deploy-state'
    $profileStatePath = Join-Path $stateDirectory 'profile-state.json'

    if (Test-Path -LiteralPath $connectionFile -PathType Leaf) {
        New-Item -ItemType Directory -Path $stateDirectory -Force | Out-Null
        if (-not (Test-Path -LiteralPath $profileStatePath)) {
            [ordered]@{
                schema = 1
                initializedUtc = [DateTime]::UtcNow.ToString('o')
                migratedFromLegacy = $false
            } | ConvertTo-Json | Set-Content -LiteralPath $profileStatePath -Encoding utf8
        }
        return
    }

    if (Test-Path -LiteralPath $profileStatePath) {
        throw "Profile state says initialization completed, but Settings\confCons.xml is missing."
    }
    if ([string]::IsNullOrWhiteSpace($LegacyRoot)) {
        throw "No existing profile is present and no legacy profile directory was supplied."
    }

    $legacyConnectionFile = Resolve-LegacyConnectionFile -LegacyRoot $LegacyRoot
    New-Item -ItemType Directory -Path $settingsDirectory -Force | Out-Null

    foreach ($name in @('mRemoteNG.settings', 'pnlLayout.xml', 'extApps.xml')) {
        $sourcePath = Join-Path $LegacyRoot $name
        if (Test-Path -LiteralPath $sourcePath -PathType Leaf) {
            Copy-Item -LiteralPath $sourcePath -Destination (Join-Path $settingsDirectory $name) -Force
        }
    }

    Copy-Item -LiteralPath $legacyConnectionFile -Destination $connectionFile -Force
    $legacyConnectionName = [IO.Path]::GetFileName($legacyConnectionFile)
    Get-ChildItem -LiteralPath $LegacyRoot -File -Filter "$legacyConnectionName.*.backup" |
        ForEach-Object {
            $suffix = $_.Name.Substring($legacyConnectionName.Length)
            Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $settingsDirectory "confCons.xml$suffix") -Force
        }

    $sourceHash = (Get-FileHash -LiteralPath $legacyConnectionFile -Algorithm SHA256).Hash
    $targetHash = (Get-FileHash -LiteralPath $connectionFile -Algorithm SHA256).Hash
    if (-not $sourceHash.Equals($targetHash, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Legacy connection-file migration failed hash validation."
    }

    New-Item -ItemType Directory -Path $stateDirectory -Force | Out-Null
    [ordered]@{
        schema = 1
        initializedUtc = [DateTime]::UtcNow.ToString('o')
        migratedFromLegacy = $true
        sourceFileName = $legacyConnectionName
        sourceSha256 = $sourceHash
    } | ConvertTo-Json | Set-Content -LiteralPath $profileStatePath -Encoding utf8
}

function Get-RunningTargetProcesses {
    param([Parameter(Mandatory)][string]$Target)

    return @(Get-Process -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            if ($_.Path -and (Test-PathInside -Candidate (Get-CanonicalPath -Path $_.Path) -Parent $Target)) {
                $_
            }
        }
        catch {
            # Protected/system processes may deny Path access; they cannot be matched safely.
        }
    })
}

function Assert-ApplicationStopped {
    param([Parameter(Mandatory)][string]$Target)

    $running = @(Get-RunningTargetProcesses -Target $Target)
    if ($running.Count -eq 0) {
        return
    }

    if (-not $StopRunningApplication) {
        throw "The deployed application is running. Close it before deployment."
    }

    foreach ($process in $running) {
        $null = $process.CloseMainWindow()
    }

    $deadline = [DateTime]::UtcNow.AddSeconds($CloseTimeoutSeconds)
    do {
        Start-Sleep -Milliseconds 250
        $running = @(Get-RunningTargetProcesses -Target $Target)
    } while ($running.Count -gt 0 -and [DateTime]::UtcNow -lt $deadline)

    if ($running.Count -gt 0) {
        throw "The deployed application did not close gracefully; no files were changed."
    }
}

function Remove-TreeSafely {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$AllowedParent
    )

    $canonicalPath = Get-CanonicalPath -Path $Path
    if (-not (Test-PathInside -Candidate $canonicalPath -Parent $AllowedParent) -or
        $canonicalPath.Equals($AllowedParent, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing cleanup outside the expected parent."
    }
    if (-not (Test-Path -LiteralPath $canonicalPath)) {
        return
    }
    Assert-NoReparsePoints -Path $canonicalPath -Recurse
    Remove-Item -LiteralPath $canonicalPath -Recurse -Force
}

$source = Get-CanonicalPath -Path $SourceDirectory
$target = Get-CanonicalPath -Path $TargetDirectory
$legacy = if ([string]::IsNullOrWhiteSpace($LegacyProfileDirectory)) {
    $null
} else {
    Get-CanonicalPath -Path $LegacyProfileDirectory
}

Assert-NotRootPath -Path $source
Assert-NotRootPath -Path $target
Assert-NoReparsePointAncestors -Path $source
Assert-NoReparsePointAncestors -Path $target
if (-not (Test-Path -LiteralPath $source -PathType Container)) {
    throw "Portable build output does not exist: $source"
}
if ((Test-PathInside -Candidate $source -Parent $target) -or
    (Test-PathInside -Candidate $target -Parent $source)) {
    throw "Source and target directories must not overlap."
}
if ($legacy -and ((Test-PathInside -Candidate $target -Parent $legacy) -or
                  (Test-PathInside -Candidate $legacy -Parent $target))) {
    throw "Legacy profile and target directories must not overlap."
}

Assert-NoReparsePoints -Path $source -Recurse
if ($legacy) {
    Assert-NoReparsePointAncestors -Path $legacy
    Assert-NoReparsePoints -Path $legacy -Recurse
}

foreach ($requiredFile in @('mRemoteNG.exe', 'mRemoteNG.deps.json', 'mRemoteNG.runtimeconfig.json')) {
    $requiredPath = Join-Path $source $requiredFile
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf) -or
        (Get-Item -LiteralPath $requiredPath).Length -eq 0) {
        throw "Portable build output is incomplete: $requiredFile is missing or empty."
    }
}

$targetParent = Split-Path -Parent $target
New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
Assert-NoReparsePoints -Path $targetParent
New-Item -ItemType Directory -Path $target -Force | Out-Null
Assert-NoReparsePoints -Path $target

Assert-ApplicationStopped -Target $target
Initialize-PortableProfile -Target $target -LegacyRoot $legacy

$settingsDirectory = Join-Path $target 'Settings'
$profileBefore = Get-FileHashMap -Root $settingsDirectory
if ($profileBefore.Count -eq 0) {
    throw "The portable Settings profile is empty; refusing to deploy."
}

$stage = Join-Path ([IO.Path]::GetTempPath()) ("mRemoteNG-portable-stage-" + [Guid]::NewGuid().ToString('N'))
$backupRoot = Join-Path $target '_rollback'
$backup = Join-Path $backupRoot ("program-" + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmssfff'))
$stateDirectory = Join-Path $target '_deploy-state'
$manifestPath = Join-Path $stateDirectory 'program-manifest.json'
$installedTopLevelNames = [Collections.Generic.List[string]]::new()
$backupCreated = $false

try {
    Copy-ProgramTree -Source $source -Destination $stage
    $sourceHashes = Get-ProgramHashMap -Root $source
    $stageHashes = Get-FileHashMap -Root $stage
    Assert-HashMapsEqual -Expected $sourceHashes -Actual $stageHashes -Description 'Staged program'

    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    $backupCreated = $true
    $protectedNames = @('Settings', '_rollback', '_deploy-state')
    Get-ChildItem -LiteralPath $target -Force |
        Where-Object {
            $_.Name -notin $protectedNames -and
            $_.Extension -ne '.log'
        } |
        ForEach-Object {
            if ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Refusing to move a target reparse point: $($_.FullName)"
            }
            Move-Item -LiteralPath $_.FullName -Destination $backup
        }

    Get-ChildItem -LiteralPath $stage -Force | ForEach-Object {
        $installedTopLevelNames.Add($_.Name)
        Move-Item -LiteralPath $_.FullName -Destination $target
    }

    $deployedHashes = Get-ProgramHashMap -Root $target
    Assert-HashMapsEqual -Expected $sourceHashes -Actual $deployedHashes -Description 'Deployed program'

    $profileAfter = Get-FileHashMap -Root $settingsDirectory
    Assert-HashMapsEqual -Expected $profileBefore -Actual $profileAfter -Description 'Portable profile'

    New-Item -ItemType Directory -Path $stateDirectory -Force | Out-Null
    [ordered]@{
        schema = 1
        deployedUtc = [DateTime]::UtcNow.ToString('o')
        sourceFileCount = $sourceHashes.Count
        files = @($sourceHashes.Keys | ForEach-Object {
            [ordered]@{ path = $_; sha256 = $sourceHashes[$_] }
        })
    } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding utf8

    $oldBackups = @(Get-ChildItem -LiteralPath $backupRoot -Directory -Filter 'program-*' |
        Sort-Object Name -Descending |
        Select-Object -Skip $RollbackCount)
    foreach ($oldBackup in $oldBackups) {
        Remove-TreeSafely -Path $oldBackup.FullName -AllowedParent $backupRoot
    }

    $deployedExe = Get-Item -LiteralPath (Join-Path $target 'mRemoteNG.exe')
    Write-Host "Portable deploy succeeded: $($deployedExe.VersionInfo.ProductVersion)" -ForegroundColor Green
    Write-Host "Preserved Settings files: $($profileAfter.Count)" -ForegroundColor DarkGray
}
catch {
    if ($backupCreated) {
        foreach ($name in $installedTopLevelNames) {
            $installedPath = Join-Path $target $name
            if (Test-Path -LiteralPath $installedPath) {
                Remove-TreeSafely -Path $installedPath -AllowedParent $target
            }
        }
        Get-ChildItem -LiteralPath $backup -Force -ErrorAction SilentlyContinue | ForEach-Object {
            Move-Item -LiteralPath $_.FullName -Destination $target -Force
        }
    }
    throw
}
finally {
    if (Test-Path -LiteralPath $stage) {
        Remove-TreeSafely -Path $stage -AllowedParent (Get-CanonicalPath -Path ([IO.Path]::GetTempPath()))
    }
}

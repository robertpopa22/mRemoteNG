[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$SourceDirectory,

    [Parameter(Mandatory)]
    [string]$TargetDirectory,

    [string]$LegacyProfileDirectory,

    # Where replaced program versions are kept. Defaults to <target>\_rollback. Point it
    # outside a cloud-synced folder to keep backups from being uploaded; it must be on the
    # target's volume so that every move in and out of it is a rename, never a copy.
    [string]$RollbackDirectory,

    [switch]$StopRunningApplication,

    [ValidateRange(1, 300)]
    [int]$CloseTimeoutSeconds = 20,

    [ValidateRange(1, 10)]
    [int]$RollbackCount = 2
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Top-level names in the target that are not part of the program and are never moved,
# replaced or hashed as program files.
$script:NonProgramNames = @('Settings', '_rollback', '_deploy-state')

if (-not ('MRemoteNGDeploy.ReparsePoint' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MRemoteNGDeploy
{
    public static class ReparsePoint
    {
        private const uint FileAttributeReparsePoint = 0x400;

        // IO_REPARSE_TAG_CLOUD and IO_REPARSE_TAG_CLOUD_1..F differ only in bits 12-15.
        private const uint CloudTagMask = 0xFFFF0FFF;
        private const uint CloudTag = 0x9000001A;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct Win32FindData
        {
            public uint FileAttributes;
            public uint CreationTimeLow, CreationTimeHigh;
            public uint LastAccessTimeLow, LastAccessTimeHigh;
            public uint LastWriteTimeLow, LastWriteTimeHigh;
            public uint FileSizeHigh, FileSizeLow;
            public uint Reserved0;
            public uint Reserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string FileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)] public string AlternateFileName;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindFirstFileExW(
            string fileName, int infoLevel, out Win32FindData data, int searchOp, IntPtr searchFilter, int flags);

        [DllImport("kernel32.dll")]
        private static extern bool FindClose(IntPtr handle);

        // Returns the reparse tag of a file or directory, or 0 when it is not a reparse point.
        // The tag comes from the directory entry, so the item itself is never opened: opening a
        // cloud placeholder can start a download. GetFileAttributes can hide the reparse bit of a
        // cloud placeholder altogether, which is why FileSystemInfo.Attributes is not used.
        public static uint GetTag(string fullPath)
        {
            string query = fullPath.StartsWith(@"\\?\", StringComparison.Ordinal) ? fullPath
                : fullPath.StartsWith(@"\\", StringComparison.Ordinal) ? @"\\?\UNC\" + fullPath.Substring(2)
                : @"\\?\" + fullPath;

            Win32FindData data;
            IntPtr handle = FindFirstFileExW(query, 1 /* FindExInfoBasic */, out data, 0, IntPtr.Zero, 0);
            if (handle == new IntPtr(-1))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot read the directory entry of " + fullPath);
            FindClose(handle);

            if ((data.FileAttributes & FileAttributeReparsePoint) == 0)
                return 0;
            if (data.Reserved0 == 0)
                throw new InvalidOperationException("Reparse point with an unreadable tag: " + fullPath);
            return data.Reserved0;
        }

        // Cloud-file placeholders (OneDrive and other sync providers) hold ordinary content in
        // place. Unlike junctions, mount points and symbolic links they redirect nowhere.
        public static bool IsCloudFile(uint tag)
        {
            return (tag & CloudTagMask) == CloudTag;
        }
    }
}
'@
}

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

function Test-PathsOverlap {
    param(
        [Parameter(Mandatory)][string]$First,
        [Parameter(Mandatory)][string]$Second
    )

    return (Test-PathInside -Candidate $First -Parent $Second) -or
           (Test-PathInside -Candidate $Second -Parent $First)
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

function Test-ProgramItem {
    param([Parameter(Mandatory)][IO.FileSystemInfo]$Item)

    return $Item.Name -notin $script:NonProgramNames -and $Item.Extension -ne '.log'
}

# Junctions, mount points, symbolic links and any reparse point this script does not know are
# refused: following one could move, hash or delete files outside the tree being deployed.
# Cloud-file placeholders are ordinary content and are accepted.
function Assert-NotLink {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Message
    )

    $tag = [MRemoteNGDeploy.ReparsePoint]::GetTag($Path)
    if ($tag -ne 0 -and -not [MRemoteNGDeploy.ReparsePoint]::IsCloudFile($tag)) {
        throw ("{0}: {1} (reparse tag 0x{2:X8})" -f $Message, $Path, $tag)
    }
}

function Assert-NoLinks {
    param(
        [Parameter(Mandatory)][string]$Path,
        [switch]$Recurse
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    Assert-NotLink -Path (Get-CanonicalPath -Path $Path) -Message 'Refusing a link or unsupported reparse point'

    if ($Recurse) {
        # Get-ChildItem descends into cloud placeholders but not into links; a link is still
        # listed itself, so it is caught here.
        Get-ChildItem -LiteralPath $Path -Force -Recurse | ForEach-Object {
            Assert-NotLink -Path $_.FullName -Message 'Refusing a tree containing a link or unsupported reparse point'
        }
    }
}

function Assert-NoLinkAncestors {
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
        $parent = Split-Path -Parent $current
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current) {
            break
        }
        Assert-NotLink -Path $current -Message 'Refusing a path below a link or unsupported reparse point'
        $current = $parent
    }
}

function Get-FileHashMap {
    param([Parameter(Mandatory)][string]$Root)

    $map = [ordered]@{}
    if (-not (Test-Path -LiteralPath $Root)) {
        return $map
    }

    Assert-NoLinks -Path $Root -Recurse
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
        [Parameter(Mandatory)][AllowEmptyCollection()]$Expected,
        [Parameter(Mandatory)][AllowEmptyCollection()]$Actual,
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
        Where-Object { Test-ProgramItem -Item $_ } |
        ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination $Destination -Recurse -Force
        }
}

function Get-ProgramHashMap {
    param([Parameter(Mandatory)][string]$Root)

    $map = [ordered]@{}
    $programItems = @(Get-ChildItem -LiteralPath $Root -Force | Where-Object { Test-ProgramItem -Item $_ })

    foreach ($programItem in $programItems) {
        Assert-NoLinks -Path $programItem.FullName -Recurse:$programItem.PSIsContainer
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
    Assert-NoLinks -Path $candidate
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
    Assert-NoLinks -Path $canonicalPath -Recurse
    Remove-Item -LiteralPath $canonicalPath -Recurse -Force
    if (Test-Path -LiteralPath $canonicalPath) {
        throw "Cleanup left $canonicalPath behind."
    }
}

# Renames an item, and only renames it. When a rename is refused, Move-Item falls back to
# copying the tree and deleting the source file by file, so a single locked file leaves half a
# folder on each side. A rename either happens or it does not. It is retried briefly because a
# sync client or a virus scanner can hold a file open for a moment.
function Move-ByRename {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Destination
    )

    if (Test-Path -LiteralPath $Destination) {
        throw "Cannot move $Path to $Destination because the destination exists."
    }
    $isDirectory = [IO.Directory]::Exists($Path)
    for ($attempt = 1; ; $attempt++) {
        try {
            if ($isDirectory) {
                [IO.Directory]::Move($Path, $Destination)
            } else {
                [IO.File]::Move($Path, $Destination)
            }
            return
        }
        catch {
            $exception = $_.Exception
            while ($exception -is [Management.Automation.MethodInvocationException] -and $exception.InnerException) {
                $exception = $exception.InnerException
            }
            $transient = ($exception -is [IO.IOException] -and
                          $exception -isnot [IO.DirectoryNotFoundException] -and
                          $exception -isnot [IO.FileNotFoundException]) -or
                         $exception -is [UnauthorizedAccessException]
            if (-not $transient -or $attempt -ge 20) {
                throw "Cannot move $Path to ${Destination}: $($exception.Message)"
            }
            Start-Sleep -Milliseconds 250
        }
    }
}

# Takes an item out of the target by renaming it into the discard directory, so the target
# never holds a half-deleted folder.
function Move-ToDiscard {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Discard
    )

    New-Item -ItemType Directory -Path $Discard -Force | Out-Null
    $destination = Join-Path $Discard (Split-Path -Leaf $Path)
    if (Test-Path -LiteralPath $destination) {
        $destination = "$destination.$([Guid]::NewGuid().ToString('N'))"
    }
    Move-ByRename -Path $Path -Destination $destination
}

# Puts the program that was in place before this deployment back, then proves it by hash
# against the map taken before anything moved. Throws, naming where every file is, unless the
# target ends up exactly as it was.
function Restore-PreviousProgram {
    param(
        [Parameter(Mandatory)][string]$Target,
        [Parameter(Mandatory)][string]$Backup,
        [Parameter(Mandatory)][string]$Discard,
        [Parameter(Mandatory)][AllowEmptyCollection()][string[]]$InstalledNames,
        [Parameter(Mandatory)][AllowEmptyCollection()]$ExpectedHashes
    )

    $problems = [Collections.Generic.List[string]]::new()

    foreach ($name in $InstalledNames) {
        $installedPath = Join-Path $Target $name
        if (Test-Path -LiteralPath $installedPath) {
            try {
                Move-ToDiscard -Path $installedPath -Discard $Discard
            }
            catch {
                $problems.Add("could not move the new '$name' out of the target: $($_.Exception.Message)")
            }
        }
    }

    if (Test-Path -LiteralPath $Backup) {
        foreach ($item in @(Get-ChildItem -LiteralPath $Backup -Force)) {
            try {
                $destination = Join-Path $Target $item.Name
                if (Test-Path -LiteralPath $destination) {
                    Move-ToDiscard -Path $destination -Discard $Discard
                }
                Move-ByRename -Path $item.FullName -Destination $destination
            }
            catch {
                $problems.Add("could not move '$($item.Name)' back from the backup: $($_.Exception.Message)")
            }
        }
    }

    try {
        $restoredHashes = Get-ProgramHashMap -Root $Target
        Assert-HashMapsEqual -Expected $ExpectedHashes -Actual $restoredHashes -Description 'Restored program'
    }
    catch {
        $problems.Add("the target does not match the previous program: $($_.Exception.Message)")
    }

    if ($problems.Count -gt 0) {
        throw ("The previous program could NOT be restored intact in $Target. " +
            "Files not moved back are still in $Backup; files taken out of the target are in $Discard. " +
            "Problems: " + ($problems -join ' | '))
    }

    try {
        if (Test-Path -LiteralPath $Backup) {
            [IO.Directory]::Delete($Backup, $false)
        }
        if (Test-Path -LiteralPath $Discard) {
            Remove-TreeSafely -Path $Discard -AllowedParent (Split-Path -Parent $Discard)
        }
    }
    catch {
        Write-Warning "The previous program is restored, but cleanup after the rollback failed: $($_.Exception.Message)"
    }
}

function Remove-OldBackups {
    param(
        [Parameter(Mandatory)][string]$BackupRoot,
        [Parameter(Mandatory)][int]$Keep
    )

    foreach ($discard in @(Get-ChildItem -LiteralPath $BackupRoot -Directory -Force -Filter 'discard-*')) {
        try {
            Remove-TreeSafely -Path $discard.FullName -AllowedParent $BackupRoot
        }
        catch {
            Write-Warning "Could not remove $($discard.FullName): $($_.Exception.Message)"
        }
    }

    $kept = 0
    $backups = @(Get-ChildItem -LiteralPath $BackupRoot -Directory -Force -Filter 'program-*' |
        Sort-Object Name -Descending)
    foreach ($oldBackup in $backups) {
        try {
            # An empty backup is what a restored failed deployment leaves: it holds nothing to roll
            # back to and must not take a retention slot from one that does.
            if (-not (Get-ChildItem -LiteralPath $oldBackup.FullName -Force | Select-Object -First 1)) {
                [IO.Directory]::Delete($oldBackup.FullName, $false)
                continue
            }
            if ($kept -lt $Keep) {
                $kept++
                continue
            }
            Remove-TreeSafely -Path $oldBackup.FullName -AllowedParent $BackupRoot
        }
        catch {
            Write-Warning "Could not remove old backup $($oldBackup.FullName): $($_.Exception.Message)"
        }
    }
}

$source = Get-CanonicalPath -Path $SourceDirectory
$target = Get-CanonicalPath -Path $TargetDirectory
$legacy = if ([string]::IsNullOrWhiteSpace($LegacyProfileDirectory)) {
    $null
} else {
    Get-CanonicalPath -Path $LegacyProfileDirectory
}
$defaultBackupRoot = Join-Path $target '_rollback'
$backupRoot = if ([string]::IsNullOrWhiteSpace($RollbackDirectory)) {
    $defaultBackupRoot
} else {
    Get-CanonicalPath -Path $RollbackDirectory
}

Assert-NotRootPath -Path $source
Assert-NotRootPath -Path $target
Assert-NotRootPath -Path $backupRoot
Assert-NoLinkAncestors -Path $source
Assert-NoLinkAncestors -Path $target
Assert-NoLinkAncestors -Path $backupRoot
if (-not (Test-Path -LiteralPath $source -PathType Container)) {
    throw "Portable build output does not exist: $source"
}
if (Test-PathsOverlap -First $source -Second $target) {
    throw "Source and target directories must not overlap."
}
if ($legacy -and (Test-PathsOverlap -First $target -Second $legacy)) {
    throw "Legacy profile and target directories must not overlap."
}
if ((Test-PathInside -Candidate $backupRoot -Parent $target) -and
    -not (Test-PathInside -Candidate $backupRoot -Parent $defaultBackupRoot)) {
    throw "A rollback directory inside the target must be its _rollback folder."
}
if (Test-PathInside -Candidate $target -Parent $backupRoot) {
    throw "The target must not be inside the rollback directory."
}
if ((Test-PathsOverlap -First $backupRoot -Second $source) -or
    ($legacy -and (Test-PathsOverlap -First $backupRoot -Second $legacy))) {
    throw "The rollback directory must not overlap the source or the legacy profile."
}
if (-not [IO.Path]::GetPathRoot($backupRoot).Equals([IO.Path]::GetPathRoot($target), [StringComparison]::OrdinalIgnoreCase)) {
    throw "The rollback directory must be on the same volume as the target."
}

Assert-NoLinks -Path $source -Recurse
if ($legacy) {
    Assert-NoLinkAncestors -Path $legacy
    Assert-NoLinks -Path $legacy -Recurse
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
Assert-NoLinks -Path $targetParent
New-Item -ItemType Directory -Path $target -Force | Out-Null
Assert-NoLinks -Path $target
New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
Assert-NoLinks -Path $backupRoot

Assert-ApplicationStopped -Target $target
Initialize-PortableProfile -Target $target -LegacyRoot $legacy

$settingsDirectory = Join-Path $target 'Settings'
$profileBefore = Get-FileHashMap -Root $settingsDirectory
if ($profileBefore.Count -eq 0) {
    throw "The portable Settings profile is empty; refusing to deploy."
}

$stamp = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmssfff')
$stage = Join-Path ([IO.Path]::GetTempPath()) ("mRemoteNG-portable-stage-" + [Guid]::NewGuid().ToString('N'))
$backup = Join-Path $backupRoot "program-$stamp"
$discard = Join-Path $backupRoot "discard-$stamp"
$stateDirectory = Join-Path $target '_deploy-state'
$manifestPath = Join-Path $stateDirectory 'program-manifest.json'
$installedTopLevelNames = [Collections.Generic.List[string]]::new()
$previousProgram = $null

try {
    Copy-ProgramTree -Source $source -Destination $stage
    $sourceHashes = Get-ProgramHashMap -Root $source
    $stageHashes = Get-FileHashMap -Root $stage
    Assert-HashMapsEqual -Expected $sourceHashes -Actual $stageHashes -Description 'Staged program'

    # Taken before anything in the target moves: a rollback is proven against this map.
    $previousProgram = Get-ProgramHashMap -Root $target
    $previousItems = @(Get-ChildItem -LiteralPath $target -Force | Where-Object { Test-ProgramItem -Item $_ })

    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    foreach ($item in $previousItems) {
        Assert-NotLink -Path $item.FullName -Message 'Refusing to move a target link or unsupported reparse point'
        Move-ByRename -Path $item.FullName -Destination (Join-Path $backup $item.Name)
    }

    foreach ($item in @(Get-ChildItem -LiteralPath $stage -Force)) {
        $installedTopLevelNames.Add($item.Name)
        Move-Item -LiteralPath $item.FullName -Destination $target
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
}
catch {
    $deployError = $_
    if ($null -ne $previousProgram) {
        try {
            Restore-PreviousProgram -Target $target -Backup $backup -Discard $discard `
                -InstalledNames $installedTopLevelNames.ToArray() -ExpectedHashes $previousProgram
        }
        catch {
            Write-Host "DEPLOYMENT FAILED AND ROLLBACK FAILED: $target needs manual repair." -ForegroundColor Red
            throw "Deployment failed: $($deployError.Exception.Message) Rollback failed: $($_.Exception.Message)"
        }
        Write-Host "Deployment failed; the previous program was restored and verified by hash." -ForegroundColor Yellow
    }
    throw $deployError
}
finally {
    if (Test-Path -LiteralPath $stage) {
        try {
            Remove-TreeSafely -Path $stage -AllowedParent (Get-CanonicalPath -Path ([IO.Path]::GetTempPath()))
        }
        catch {
            Write-Warning "Could not remove the staging directory ${stage}: $($_.Exception.Message)"
        }
    }
}

# Retention runs only after a verified deployment and can no longer undo it: a backup that
# cannot be pruned today is left for the next run, with a warning.
Remove-OldBackups -BackupRoot $backupRoot -Keep $RollbackCount

$deployedExe = Get-Item -LiteralPath (Join-Path $target 'mRemoteNG.exe')
Write-Host "Portable deploy succeeded: $($deployedExe.VersionInfo.ProductVersion)" -ForegroundColor Green
Write-Host "Preserved Settings files: $($profileAfter.Count)" -ForegroundColor DarkGray

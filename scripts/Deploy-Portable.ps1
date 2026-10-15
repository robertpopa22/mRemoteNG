<#
.SYNOPSIS
Deploys a portable mRemoteNG build into a target directory, preserving Settings.

.DESCRIPTION
Two ways in, one swap. With -SourceDirectory the whole program is copied from a build output.
With -Mini a MINI package (scripts/Publish-Release.ps1 -Channel mini) is applied over the FULL it
was computed against: the complete new program is assembled in <target>\_staging\<version>\tree
from hard links to the unchanged local files plus the files carried in the package, verified
against the package manifest, and only then swapped in. Both paths use the same rename-based
swap: every top-level program item goes to <target>\_rollback\program-<stamp>, the new items are
moved in, and any failure puts the previous program back and proves it by hash. Settings,
_rollback, _deploy-state and _staging are never moved, replaced or hashed as program files.
Design: docs/plans/UPDATE-FULL-MINI-2026-10-15.md (E).

.PARAMETER SourceDirectory
A portable build output (for example mRemoteNG\bin\x64\Release\publish). The whole program in the
target is replaced by it.

.PARAMETER Mini
A MINI zip: manifest.json at its root (schema 1, channel mini) plus the files listed in its
"delta". "files" is the complete program after the update, "remove" the files it drops. No
network is used. The MINI is refused, and nothing in the target changes, when:
the target holds mRemoteNG.installed (an MSI copy) or is not a portable copy with
Settings\confCons.xml; any file in "files" that is not in "delta" is missing locally or differs
by sha256 or size ("base differs; FULL required", with the first 20 paths); the manifest or a
path in it is malformed or escapes the target; a delta file in the zip does not match its hash
or size; or the assembled tree does not match "files" exactly.
Local program files that are in neither "files" nor "remove" are not carried over; they are
listed and kept in the rollback folder.

.PARAMETER TargetDirectory
The portable copy to deploy into. With -Mini it must already exist.

.PARAMETER LegacyProfileDirectory
-SourceDirectory only: a legacy %APPDATA% profile to migrate when the target has no Settings yet.

.PARAMETER StopRunningApplication
Close a running deployed application instead of refusing to deploy. The close has three
steps: CloseMainWindow, answering the "confirm exit" dialog with Yes through UI Automation;
WM_CLOSE to every window of the process when there is no main window (hidden to tray);
Stop-Process at the deadline, logged as a warning. Open sessions are closed.
Without this switch a running application makes the script throw. With -Mini the application
is only closed after the package and the local base have been verified.

.PARAMETER CloseTimeoutSeconds
How long to wait for the application to close on its own before it is terminated.

.EXAMPLE
pwsh -File scripts\Deploy-Portable.ps1 -SourceDirectory mRemoteNG\bin\x64\Release\publish -TargetDirectory D:\prod\mRemoteNG

.EXAMPLE
pwsh -File scripts\Deploy-Portable.ps1 -Mini mRemoteNG-mini-1.84.0-a1b2c3d4e5-x64.zip -TargetDirectory D:\prod\mRemoteNG -StopRunningApplication
#>
[CmdletBinding(DefaultParameterSetName = 'Source')]
param(
    [Parameter(Mandatory, ParameterSetName = 'Source')]
    [string]$SourceDirectory,

    [Parameter(Mandatory, ParameterSetName = 'Mini')]
    [string]$Mini,

    [Parameter(Mandatory)]
    [string]$TargetDirectory,

    [Parameter(ParameterSetName = 'Source')]
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
$script:NonProgramNames = @('Settings', '_rollback', '_deploy-state', '_staging')

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

if (-not ('MRemoteNGDeploy.WindowPoster' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MRemoteNGDeploy
{
    public static class WindowPoster
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_CLOSE = 0x0010;

        // Posts WM_CLOSE to every top-level window (visible or hidden) of the process.
        public static int PostCloseToProcess(int processId)
        {
            List<IntPtr> windows = new List<IntPtr>();
            EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
            {
                uint pid;
                GetWindowThreadProcessId(hWnd, out pid);
                if (pid == (uint)processId)
                {
                    windows.Add(hWnd);
                }
                return true;
            }, IntPtr.Zero);

            int posted = 0;
            foreach (IntPtr hWnd in windows)
            {
                if (PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero))
                {
                    posted++;
                }
            }
            return posted;
        }
    }
}
'@
}

function Confirm-ExitDialog {
    param([Parameter(Mandatory)][int]$ProcessId)

    try {
        Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes -ErrorAction Stop
        $auto = [System.Windows.Automation.AutomationElement]
        $pidCondition = New-Object System.Windows.Automation.PropertyCondition($auto::ProcessIdProperty, $ProcessId)
        $buttonCondition = New-Object System.Windows.Automation.PropertyCondition(
            $auto::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
        $yesNames = @('Yes', '&Yes', 'Da', '&Da')

        $windows = $auto::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children, $pidCondition)
        foreach ($window in $windows) {
            $buttons = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, $buttonCondition)
            foreach ($button in $buttons) {
                if ($yesNames -contains $button.Current.Name) {
                    $pattern = $null
                    if ($button.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pattern)) {
                        $pattern.Invoke()
                        return $true
                    }
                }
            }
        }
    }
    catch {
        # UI Automation is unavailable or the window vanished mid-scan; the poll retries.
    }
    return $false
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

    # Step 1: ask each process to close. Step 2: with no main window (hidden to tray),
    # post WM_CLOSE to every window of the process instead.
    foreach ($process in $running) {
        $closeRequested = $false
        try { $closeRequested = $process.CloseMainWindow() } catch { }
        if ($closeRequested) {
            Write-Host "close requested for process $($process.Id)"
        }
        else {
            $posted = [MRemoteNGDeploy.WindowPoster]::PostCloseToProcess($process.Id)
            Write-Host "WM_CLOSE posted to $posted windows of process $($process.Id)"
        }
    }

    # Answer the confirm-exit dialog with Yes while the processes shut down.
    $deadline = [DateTime]::UtcNow.AddSeconds($CloseTimeoutSeconds)
    do {
        Start-Sleep -Milliseconds 250
        $running = @(Get-RunningTargetProcesses -Target $Target)
        foreach ($process in $running) {
            if (Confirm-ExitDialog -ProcessId $process.Id) {
                Write-Host "confirm-exit dialog answered Yes for process $($process.Id)"
            }
        }
    } while ($running.Count -gt 0 -and [DateTime]::UtcNow -lt $deadline)

    if ($running.Count -eq 0) {
        return
    }

    # Step 3: still alive at the deadline.
    foreach ($process in $running) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
    Write-Warning "mRemoteNG did not close on its own within $CloseTimeoutSeconds s; terminated."
    $killDeadline = [DateTime]::UtcNow.AddSeconds(10)
    do {
        Start-Sleep -Milliseconds 250
        $running = @(Get-RunningTargetProcesses -Target $Target)
    } while ($running.Count -gt 0 -and [DateTime]::UtcNow -lt $killDeadline)

    if ($running.Count -gt 0) {
        throw "The deployed application could not be terminated; no files were changed."
    }
    Write-Host "terminated after timeout"
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

if (-not ('MRemoteNGDeploy.HardLink' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

namespace MRemoteNGDeploy
{
    public static class HardLink
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateHardLinkW(string newFileName, string existingFileName, IntPtr securityAttributes);

        // Returns 0 on success, otherwise the Win32 error code.
        public static int Create(string newFileName, string existingFileName)
        {
            return CreateHardLinkW(newFileName, existingFileName, IntPtr.Zero) ? 0 : Marshal.GetLastWin32Error();
        }
    }
}
'@
}

# The swap shared by both deployment paths. Every top-level program item of the target is renamed
# into <BackupRoot>\program-<stamp>, the staged items are moved in, the result and the Settings
# profile are proven by hash and _deploy-state\program-manifest.json is written. On any failure
# the previous program is put back and proven by hash before the error is rethrown.
# Returns the Settings hash map taken after the swap.
function Install-StagedProgram {
    param(
        [Parameter(Mandatory)][string]$Target,
        [Parameter(Mandatory)][string]$Stage,
        [Parameter(Mandatory)][string]$BackupRoot,
        [Parameter(Mandatory)][AllowEmptyCollection()]$ExpectedHashes,
        [Parameter(Mandatory)][AllowEmptyCollection()]$ProfileBefore,
        [Collections.IDictionary]$ManifestInfo = [ordered]@{},
        # Move the staged items in by rename only. Valid only when the stage is on the target's volume.
        [switch]$RenameOnly
    )

    $settingsDirectory = Join-Path $Target 'Settings'
    $stamp = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmssfff')
    $backup = Join-Path $BackupRoot "program-$stamp"
    $discard = Join-Path $BackupRoot "discard-$stamp"
    $stateDirectory = Join-Path $Target '_deploy-state'
    $manifestPath = Join-Path $stateDirectory 'program-manifest.json'
    $installedTopLevelNames = [Collections.Generic.List[string]]::new()
    $previousProgram = $null

    try {
        # Taken before anything in the target moves: a rollback is proven against this map.
        $previousProgram = Get-ProgramHashMap -Root $Target
        $previousItems = @(Get-ChildItem -LiteralPath $Target -Force | Where-Object { Test-ProgramItem -Item $_ })

        New-Item -ItemType Directory -Path $backup -Force | Out-Null
        foreach ($item in $previousItems) {
            Assert-NotLink -Path $item.FullName -Message 'Refusing to move a target link or unsupported reparse point'
            Move-ByRename -Path $item.FullName -Destination (Join-Path $backup $item.Name)
        }

        foreach ($item in @(Get-ChildItem -LiteralPath $Stage -Force)) {
            $installedTopLevelNames.Add($item.Name)
            if ($RenameOnly) {
                Move-ByRename -Path $item.FullName -Destination (Join-Path $Target $item.Name)
            } else {
                Move-Item -LiteralPath $item.FullName -Destination $Target
            }
        }

        $deployedHashes = Get-ProgramHashMap -Root $Target
        Assert-HashMapsEqual -Expected $ExpectedHashes -Actual $deployedHashes -Description 'Deployed program'

        $profileAfter = Get-FileHashMap -Root $settingsDirectory
        Assert-HashMapsEqual -Expected $ProfileBefore -Actual $profileAfter -Description 'Portable profile'

        New-Item -ItemType Directory -Path $stateDirectory -Force | Out-Null
        $programManifest = [ordered]@{
            schema = 1
            deployedUtc = [DateTime]::UtcNow.ToString('o')
        }
        foreach ($key in $ManifestInfo.Keys) {
            $programManifest[$key] = $ManifestInfo[$key]
        }
        $programManifest['sourceFileCount'] = $deployedHashes.Count
        $programManifest['files'] = @($deployedHashes.Keys | ForEach-Object {
            [ordered]@{ path = $_; sha256 = $deployedHashes[$_] }
        })
        $programManifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    }
    catch {
        $deployError = $_
        if ($null -ne $previousProgram) {
            try {
                Restore-PreviousProgram -Target $Target -Backup $backup -Discard $discard `
                    -InstalledNames $installedTopLevelNames.ToArray() -ExpectedHashes $previousProgram
            }
            catch {
                Write-Host "DEPLOYMENT FAILED AND ROLLBACK FAILED: $Target needs manual repair." -ForegroundColor Red
                throw "Deployment failed: $($deployError.Exception.Message) Rollback failed: $($_.Exception.Message)"
            }
            Write-Host "Deployment failed; the previous program was restored and verified by hash." -ForegroundColor Yellow
        }
        throw $deployError
    }

    return $profileAfter
}

# --- MINI packages (docs/plans/UPDATE-FULL-MINI-2026-10-15.md, B and E) ---

$script:ReservedNamePattern = '^(CON|PRN|AUX|NUL|CONIN\$|CONOUT\$|COM[0-9]|LPT[0-9])(\..*)?$'

# A package path is relative, uses '\', has no empty, '.' or '..' segment, no drive or stream
# colon, no trailing dot or space and no reserved device name.
function Assert-PackagePath {
    param(
        [AllowNull()][AllowEmptyString()][string]$Path,
        [Parameter(Mandatory)][string]$Description
    )

    $unsafe = [string]::IsNullOrWhiteSpace($Path) -or $Path.Contains('/') -or $Path.Contains(':') -or
        $Path.StartsWith('\') -or $Path.IndexOfAny([IO.Path]::GetInvalidPathChars()) -ge 0
    if (-not $unsafe) {
        foreach ($segment in $Path.Split('\')) {
            if ($segment -in @('', '.', '..') -or $segment -ne $segment.TrimEnd('.', ' ') -or
                $segment -match $script:ReservedNamePattern) {
                $unsafe = $true
                break
            }
        }
    }
    if ($unsafe) {
        throw "MINI refused: $Description has an unsafe path '$Path'."
    }
}

# Resolves a package path under a root and proves the result stays under it (zip-slip guard).
function Get-PathUnder {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$RelativePath
    )

    $full = [IO.Path]::GetFullPath([IO.Path]::Combine($Root, $RelativePath))
    if (-not (Test-PathInside -Candidate $full -Parent $Root) -or
        $full.Equals($Root, [StringComparison]::OrdinalIgnoreCase)) {
        throw "MINI refused: '$RelativePath' resolves outside $Root."
    }
    return $full
}

function Get-ManifestValue {
    param(
        [Parameter(Mandatory)]$Object,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Description
    )

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        throw "MINI refused: $Description has no '$Name'."
    }
    return $property.Value
}

function ConvertTo-PackageFile {
    param(
        [Parameter(Mandatory)]$Item,
        [Parameter(Mandatory)][string]$Description
    )

    $path = [string](Get-ManifestValue -Object $Item -Name 'path' -Description $Description)
    Assert-PackagePath -Path $path -Description $Description
    $sha256 = [string](Get-ManifestValue -Object $Item -Name 'sha256' -Description "$Description '$path'")
    if ($sha256 -notmatch '^[0-9a-fA-F]{64}$') {
        throw "MINI refused: $Description '$path' has no valid sha256."
    }
    $size = [long](Get-ManifestValue -Object $Item -Name 'size' -Description "$Description '$path'")
    if ($size -lt 0) {
        throw "MINI refused: $Description '$path' has a negative size."
    }
    return [pscustomobject]@{ Path = $path; Sha256 = $sha256.ToUpperInvariant(); Size = $size }
}

# Reads and validates manifest.json from an open MINI zip. Every zip entry other than the manifest
# must be a delta file, every delta file must be in the zip with the manifest's size, and every
# delta file must appear in "files" with the same hash and size.
function Read-MiniPackage {
    param([Parameter(Mandatory)][IO.Compression.ZipArchive]$Zip)

    $entries = [Collections.Generic.Dictionary[string, IO.Compression.ZipArchiveEntry]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $Zip.Entries) {
        if ($entry.FullName.EndsWith('/') -and $entry.Length -eq 0) {
            continue
        }
        $name = $entry.FullName.Replace('/', '\')
        if ($entries.ContainsKey($name)) {
            throw "MINI refused: the zip holds '$name' twice."
        }
        $entries[$name] = $entry
    }
    if (-not $entries.ContainsKey('manifest.json')) {
        throw 'MINI refused: the zip has no manifest.json at its root.'
    }
    $reader = [IO.StreamReader]::new($entries['manifest.json'].Open(), [Text.UTF8Encoding]::new($false))
    try {
        $manifestText = $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
    [void]$entries.Remove('manifest.json')
    try {
        $manifest = $manifestText | ConvertFrom-Json
    }
    catch {
        throw "MINI refused: manifest.json is not valid JSON: $($_.Exception.Message)"
    }

    $schema = Get-ManifestValue -Object $manifest -Name 'schema' -Description 'the manifest'
    $channel = [string](Get-ManifestValue -Object $manifest -Name 'channel' -Description 'the manifest')
    $rid = [string](Get-ManifestValue -Object $manifest -Name 'rid' -Description 'the manifest')
    $version = [string](Get-ManifestValue -Object $manifest -Name 'version' -Description 'the manifest')
    $baseFull = [string](Get-ManifestValue -Object $manifest -Name 'baseFull' -Description 'the manifest')
    $commit = [string](Get-ManifestValue -Object $manifest -Name 'commit' -Description 'the manifest')
    if ("$schema" -ne '1') { throw "MINI refused: manifest schema '$schema' is not supported." }
    if ($channel -ne 'mini') { throw "MINI refused: the package is channel '$channel', not 'mini'." }
    if ($rid -ne 'win-x64') { throw "MINI refused: the package is for '$rid', not 'win-x64'." }
    if ($version -notmatch '^\d+\.\d+\.\d+\+[0-9a-f]+$') { throw "MINI refused: version '$version' is malformed." }
    if ($baseFull -notmatch '^\d+\.\d+\.\d+\+[0-9a-f]+$') { throw "MINI refused: baseFull '$baseFull' is malformed." }

    $files = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($item in @(Get-ManifestValue -Object $manifest -Name 'files' -Description 'the manifest')) {
        $file = ConvertTo-PackageFile -Item $item -Description 'files'
        if ($files.ContainsKey($file.Path)) {
            throw "MINI refused: 'files' lists '$($file.Path)' twice."
        }
        $top = $file.Path.Split('\')[0]
        if ($top -in $script:NonProgramNames -or (-not $file.Path.Contains('\') -and $top -like '*.log')) {
            throw "MINI refused: '$($file.Path)' is not a program file and is never written by a deployment."
        }
        $files[$file.Path] = $file
    }
    if (-not $files.ContainsKey('mRemoteNG.exe')) {
        throw "MINI refused: 'files' has no mRemoteNG.exe."
    }

    $delta = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($item in @(Get-ManifestValue -Object $manifest -Name 'delta' -Description 'the manifest')) {
        $file = ConvertTo-PackageFile -Item $item -Description 'delta'
        if ($delta.ContainsKey($file.Path)) {
            throw "MINI refused: 'delta' lists '$($file.Path)' twice."
        }
        if (-not $files.ContainsKey($file.Path) -or
            $files[$file.Path].Sha256 -ne $file.Sha256 -or $files[$file.Path].Size -ne $file.Size) {
            throw "MINI refused: delta '$($file.Path)' does not match its entry in 'files'."
        }
        if (-not $entries.ContainsKey($file.Path)) {
            throw "MINI refused: delta '$($file.Path)' is not in the zip."
        }
        if ($entries[$file.Path].Length -ne $file.Size) {
            throw ("MINI refused: delta '{0}' is {1} bytes in the zip, the manifest says {2}." -f
                $file.Path, $entries[$file.Path].Length, $file.Size)
        }
        $delta[$file.Path] = $file
    }
    foreach ($name in $entries.Keys) {
        if (-not $delta.ContainsKey($name)) {
            throw "MINI refused: the zip carries '$name', which is not in 'delta'."
        }
    }

    $remove = [Collections.Generic.List[string]]::new()
    foreach ($item in @(Get-ManifestValue -Object $manifest -Name 'remove' -Description 'the manifest')) {
        $path = [string]$item
        Assert-PackagePath -Path $path -Description 'remove'
        if ($files.ContainsKey($path)) {
            throw "MINI refused: '$path' is both in 'files' and in 'remove'."
        }
        $remove.Add($path)
    }

    return [pscustomobject]@{
        Version  = $version
        Commit   = $commit
        BaseFull = $baseFull
        Files    = $files
        Delta    = $delta
        Remove   = $remove
        Entries  = $entries
    }
}

# Where the unchanged copy of a base file is found. Normally in the program folder. A portable copy
# moves <exe>\Themes into Settings\Themes on its first start when Settings\Themes does not exist yet
# (SettingsFileInfo.MigratePortableSettings), so a program theme file missing from Themes is looked
# up there; it must still match the manifest by hash, and it is copied, never linked, because the
# Settings copy belongs to the user.
function Get-BaseFileSource {
    param(
        [Parameter(Mandatory)][string]$Target,
        [Parameter(Mandatory)][string]$RelativePath
    )

    $programPath = Get-PathUnder -Root $Target -RelativePath $RelativePath
    if ([IO.File]::Exists($programPath)) {
        return [pscustomobject]@{ Path = $programPath; Relocated = $false }
    }
    if ($RelativePath -match '^Themes\\[^\\]+$') {
        $settingsPath = Get-PathUnder -Root $Target -RelativePath ('Settings\' + $RelativePath)
        if ([IO.File]::Exists($settingsPath)) {
            return [pscustomobject]@{ Path = $settingsPath; Relocated = $true }
        }
    }
    return $null
}

# The local base must be the FULL the MINI was computed against: every file the MINI does not carry
# is taken from the target as it is, so each must be there with the manifest's size and sha256.
function Assert-MiniBase {
    param(
        [Parameter(Mandatory)][string]$Target,
        [Parameter(Mandatory)]$Package
    )

    $mismatches = [Collections.Generic.List[string]]::new()
    $relocated = 0
    foreach ($file in $Package.Files.Values) {
        if ($Package.Delta.ContainsKey($file.Path)) {
            continue
        }
        $baseSource = Get-BaseFileSource -Target $Target -RelativePath $file.Path
        if ($null -eq $baseSource) {
            $mismatches.Add("missing  $($file.Path)")
            continue
        }
        $localPath = $baseSource.Path
        if ($baseSource.Relocated) {
            $relocated++
        }
        $length = ([IO.FileInfo]::new($localPath)).Length
        if ($length -ne $file.Size) {
            $mismatches.Add("size     $($file.Path) ($length bytes, expected $($file.Size))")
            continue
        }
        $hash = (Get-FileHash -LiteralPath $localPath -Algorithm SHA256).Hash
        if (-not $hash.Equals($file.Sha256, [StringComparison]::OrdinalIgnoreCase)) {
            $mismatches.Add("sha256   $($file.Path)")
        }
    }

    if ($mismatches.Count -gt 0) {
        throw (("MINI refused: base differs; FULL required. MINI {0} is built on FULL {1}; {2} local file(s) " +
            "do not match it (first 20):`n  {3}") -f $Package.Version, $Package.BaseFull, $mismatches.Count,
            (($mismatches | Select-Object -First 20) -join "`n  "))
    }
    if ($relocated -gt 0) {
        Write-Host "$relocated program theme file(s) found in Settings\Themes, where the application moved them; they will be copied back"
    }
}

# Relative paths of every program file in the target (no hashing).
function Get-ProgramFileList {
    param([Parameter(Mandatory)][string]$Root)

    foreach ($programItem in @(Get-ChildItem -LiteralPath $Root -Force | Where-Object { Test-ProgramItem -Item $_ })) {
        $programFiles = if ($programItem.PSIsContainer) {
            @(Get-ChildItem -LiteralPath $programItem.FullName -File -Force -Recurse)
        } else {
            @($programItem)
        }
        foreach ($file in $programFiles) {
            [IO.Path]::GetRelativePath($Root, $file.FullName)
        }
    }
}

# Assembles the complete new program: hard links to the unchanged local files, the delta files
# extracted from the zip. Files in "remove" and local extras are simply not linked.
function New-MiniTree {
    param(
        [Parameter(Mandatory)][string]$Target,
        [Parameter(Mandatory)][string]$Tree,
        [Parameter(Mandatory)]$Package
    )

    [void][IO.Directory]::CreateDirectory($Tree)
    $copied = [Collections.Generic.List[string]]::new()
    $linked = 0
    $relocatedCopies = 0
    foreach ($file in $Package.Files.Values) {
        $destination = Get-PathUnder -Root $Tree -RelativePath $file.Path
        [void][IO.Directory]::CreateDirectory((Split-Path -Parent $destination))
        if ($Package.Delta.ContainsKey($file.Path)) {
            [IO.Compression.ZipFileExtensions]::ExtractToFile($Package.Entries[$file.Path], $destination, $false)
            continue
        }
        $baseSource = Get-BaseFileSource -Target $Target -RelativePath $file.Path
        $existing = $baseSource.Path
        if ($baseSource.Relocated) {
            Copy-Item -LiteralPath $existing -Destination $destination
            $relocatedCopies++
            continue
        }
        $errorCode = [MRemoteNGDeploy.HardLink]::Create($destination, $existing)
        if ($errorCode -eq 0) {
            $linked++
        } else {
            Copy-Item -LiteralPath $existing -Destination $destination
            $copied.Add("$($file.Path) (Win32 error $errorCode)")
        }
    }
    Write-Host ("Assembled {0}: {1} hard-linked, {2} from the package, {3} copied from Settings\Themes, {4} copied after a failed hard link" -f
        $Tree, $linked, $Package.Delta.Count, $relocatedCopies, $copied.Count)
    if ($copied.Count -gt 0) {
        Write-Warning ("A hard link could not be created for {0} file(s); they were copied instead: {1}" -f
            $copied.Count, (($copied | Select-Object -First 10) -join ', '))
    }
}

# The assembled tree must be exactly "files": the same set of paths, each with its size and sha256.
function Assert-MiniTree {
    param(
        [Parameter(Mandatory)][string]$Tree,
        [Parameter(Mandatory)]$Package
    )

    $treeHashes = Get-FileHashMap -Root $Tree
    $problems = [Collections.Generic.List[string]]::new()
    foreach ($file in $Package.Files.Values) {
        if (-not $treeHashes.Contains($file.Path)) {
            $problems.Add("missing  $($file.Path)")
            continue
        }
        $length = ([IO.FileInfo]::new((Join-Path $Tree $file.Path))).Length
        if ($length -ne $file.Size) {
            $problems.Add("size     $($file.Path) ($length bytes, expected $($file.Size))")
        } elseif (-not $treeHashes[$file.Path].Equals($file.Sha256, [StringComparison]::OrdinalIgnoreCase)) {
            $problems.Add("sha256   $($file.Path)")
        }
    }
    foreach ($path in $treeHashes.Keys) {
        if (-not $Package.Files.ContainsKey($path)) {
            $problems.Add("extra    $path")
        }
    }
    if ($problems.Count -gt 0) {
        throw (("MINI refused: the assembled program does not match the manifest; nothing was changed. " +
            "{0} problem(s) (first 20):`n  {1}") -f $problems.Count, (($problems | Select-Object -First 20) -join "`n  "))
    }
}


$target = Get-CanonicalPath -Path $TargetDirectory
$defaultBackupRoot = Join-Path $target '_rollback'
$backupRoot = if ([string]::IsNullOrWhiteSpace($RollbackDirectory)) {
    $defaultBackupRoot
} else {
    Get-CanonicalPath -Path $RollbackDirectory
}

Assert-NotRootPath -Path $target
Assert-NotRootPath -Path $backupRoot
Assert-NoLinkAncestors -Path $target
Assert-NoLinkAncestors -Path $backupRoot
if ((Test-PathInside -Candidate $backupRoot -Parent $target) -and
    -not (Test-PathInside -Candidate $backupRoot -Parent $defaultBackupRoot)) {
    throw "A rollback directory inside the target must be its _rollback folder."
}
if (Test-PathInside -Candidate $target -Parent $backupRoot) {
    throw "The target must not be inside the rollback directory."
}
if (-not [IO.Path]::GetPathRoot($backupRoot).Equals([IO.Path]::GetPathRoot($target), [StringComparison]::OrdinalIgnoreCase)) {
    throw "The rollback directory must be on the same volume as the target."
}

if ($PSCmdlet.ParameterSetName -eq 'Mini') {
    $miniPath = Get-CanonicalPath -Path $Mini
    if (-not (Test-Path -LiteralPath $miniPath -PathType Leaf)) {
        throw "MINI package does not exist: $miniPath"
    }
    if (Test-PathInside -Candidate $miniPath -Parent $target) {
        throw "The MINI package must not be inside the target directory."
    }
    if (-not (Test-Path -LiteralPath (Join-Path $target 'mRemoteNG.exe') -PathType Leaf)) {
        throw "MINI refused: $target holds no mRemoteNG.exe; a MINI applies only over an existing portable copy."
    }
    if (Test-Path -LiteralPath (Join-Path $target 'mRemoteNG.installed')) {
        throw "MINI refused: $target is an MSI-installed copy (mRemoteNG.installed); update it with the installer."
    }
    if (-not (Test-Path -LiteralPath (Join-Path $target 'Settings\confCons.xml') -PathType Leaf)) {
        throw "MINI refused: $target is not a portable copy with a profile (no Settings\confCons.xml)."
    }

    Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
    $stagingRoot = Join-Path $target '_staging'
    $stagingVersion = $null
    try {
        $zip = [IO.Compression.ZipFile]::OpenRead($miniPath)
        try {
            $package = Read-MiniPackage -Zip $zip
            $deltaBytes = 0L
            foreach ($file in $package.Delta.Values) {
                $deltaBytes += $file.Size
            }
            Write-Host ("MINI {0} over base FULL {1}: {2} files, {3} in the package ({4:N0} bytes), {5} removed" -f
                $package.Version, $package.BaseFull, $package.Files.Count, $package.Delta.Count, $deltaBytes,
                $package.Remove.Count)

            Assert-NoLinks -Path $target -Recurse
            Assert-MiniBase -Target $target -Package $package
            Write-Host "Local base matches: $($package.Files.Count - $package.Delta.Count) unchanged files verified by size and sha256"

            $dropped = @(Get-ProgramFileList -Root $target | Where-Object {
                -not $package.Files.ContainsKey($_) -and $_ -notin $package.Remove
            })
            if ($dropped.Count -gt 0) {
                Write-Warning ("{0} local program file(s) are not part of {1} and will stay only in the rollback folder: {2}" -f
                    $dropped.Count, $package.Version, (($dropped | Select-Object -First 10) -join ', '))
            }

            if (Test-Path -LiteralPath $stagingRoot) {
                Assert-NoLinks -Path $stagingRoot
            }
            $stagingVersion = Join-Path $stagingRoot ($package.Version.Replace('+', '-'))
            if (Test-Path -LiteralPath $stagingVersion) {
                Remove-TreeSafely -Path $stagingVersion -AllowedParent $stagingRoot
            }
            $tree = Join-Path $stagingVersion 'tree'
            New-MiniTree -Target $target -Tree $tree -Package $package
            Assert-MiniTree -Tree $tree -Package $package
            Write-Host "Assembled program verified: $($package.Files.Count) files match the manifest by size and sha256"
        }
        finally {
            $zip.Dispose()
        }

        Assert-ApplicationStopped -Target $target
        New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
        Assert-NoLinks -Path $backupRoot
        Initialize-PortableProfile -Target $target

        $profileBefore = Get-FileHashMap -Root (Join-Path $target 'Settings')
        if ($profileBefore.Count -eq 0) {
            throw "The portable Settings profile is empty; refusing to deploy."
        }

        $expectedHashes = [ordered]@{}
        foreach ($file in $package.Files.Values) {
            $expectedHashes[$file.Path] = $file.Sha256
        }
        $profileAfter = Install-StagedProgram -Target $target -Stage $tree -BackupRoot $backupRoot `
            -ExpectedHashes $expectedHashes -ProfileBefore $profileBefore -RenameOnly -ManifestInfo ([ordered]@{
                channel = 'mini'
                version = $package.Version
                commit = $package.Commit
                baseFull = $package.BaseFull
                package = [IO.Path]::GetFileName($miniPath)
            })
    }
    finally {
        if ($stagingVersion -and (Test-Path -LiteralPath $stagingVersion)) {
            try {
                Remove-TreeSafely -Path $stagingVersion -AllowedParent $stagingRoot
            }
            catch {
                Write-Warning "Could not remove the staging directory ${stagingVersion}: $($_.Exception.Message)"
            }
        }
        if ((Test-Path -LiteralPath $stagingRoot) -and
            -not (Get-ChildItem -LiteralPath $stagingRoot -Force | Select-Object -First 1)) {
            [IO.Directory]::Delete($stagingRoot, $false)
        }
    }
}
else {
    $source = Get-CanonicalPath -Path $SourceDirectory
    $legacy = if ([string]::IsNullOrWhiteSpace($LegacyProfileDirectory)) {
        $null
    } else {
        Get-CanonicalPath -Path $LegacyProfileDirectory
    }

    Assert-NotRootPath -Path $source
    Assert-NoLinkAncestors -Path $source
    if (-not (Test-Path -LiteralPath $source -PathType Container)) {
        throw "Portable build output does not exist: $source"
    }
    if (Test-PathsOverlap -First $source -Second $target) {
        throw "Source and target directories must not overlap."
    }
    if ($legacy -and (Test-PathsOverlap -First $target -Second $legacy)) {
        throw "Legacy profile and target directories must not overlap."
    }
    if ((Test-PathsOverlap -First $backupRoot -Second $source) -or
        ($legacy -and (Test-PathsOverlap -First $backupRoot -Second $legacy))) {
        throw "The rollback directory must not overlap the source or the legacy profile."
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

    $profileBefore = Get-FileHashMap -Root (Join-Path $target 'Settings')
    if ($profileBefore.Count -eq 0) {
        throw "The portable Settings profile is empty; refusing to deploy."
    }

    $stage = Join-Path ([IO.Path]::GetTempPath()) ("mRemoteNG-portable-stage-" + [Guid]::NewGuid().ToString('N'))
    try {
        Copy-ProgramTree -Source $source -Destination $stage
        $sourceHashes = Get-ProgramHashMap -Root $source
        $stageHashes = Get-FileHashMap -Root $stage
        Assert-HashMapsEqual -Expected $sourceHashes -Actual $stageHashes -Description 'Staged program'

        $profileAfter = Install-StagedProgram -Target $target -Stage $stage -BackupRoot $backupRoot `
            -ExpectedHashes $sourceHashes -ProfileBefore $profileBefore
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
}

# Retention runs only after a verified deployment and can no longer undo it: a backup that
# cannot be pruned today is left for the next run, with a warning.
Remove-OldBackups -BackupRoot $backupRoot -Keep $RollbackCount

$deployedExe = Get-Item -LiteralPath (Join-Path $target 'mRemoteNG.exe')
Write-Host "Portable deploy succeeded: $($deployedExe.VersionInfo.ProductVersion)" -ForegroundColor Green
Write-Host "Preserved Settings files: $($profileAfter.Count)" -ForegroundColor DarkGray

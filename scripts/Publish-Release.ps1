#requires -Version 7.0
<#
.SYNOPSIS
    Builds a FULL or MINI mRemoteNG package with a manifest and (optionally) uploads it to a GitHub release.

.DESCRIPTION
    Local publishing, no GitHub Actions. Design: docs/plans/UPDATE-FULL-MINI-2026-10-15.md (B, C).
    Builds from a clean detached worktree of HEAD in %TEMP%; never writes to the invoking checkout.
    Signing (Ed25519) is NOT implemented yet (step 4): no .sig is produced.

.PARAMETER Channel
    full: whole self-contained program. mini: only files changed against a published FULL.
.PARAMETER Tag
    Release tag: nightly or vX.Y.Z.
.PARAMETER BaseFull
    MINI only: version of the FULL to diff against (1.84.0+sha or 1.84.0-sha). Default: newest FULL manifest on the tag.
.PARAMETER BaseFullManifestPath
    MINI only: read the base FULL manifest from this local file instead of the release (offline / dry-run proof).
.PARAMETER DryRun
    Build, manifest and zip locally; no upload.
.PARAMETER OutputDirectory
    Where zip and manifest are written. Default %TEMP%\mrng-release\<ver>.
.PARAMETER SkipTests
    Skip run-tests.ps1.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('full', 'mini')][string]$Channel,
    [ValidatePattern('^(nightly|v\d+\.\d+\.\d+)$')][string]$Tag = 'nightly',
    [string]$BaseFull,
    [string]$BaseFullManifestPath,
    [switch]$DryRun,
    [string]$OutputDirectory,
    [switch]$SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Repo = 'robertpopa22/mRemoteNG'
$MinClientVersion = '1.84.0'
$ExcludedTopLevel = @('Settings', '_rollback', '_deploy-state', '_staging')
# Files that force a FULL package (plan B). Matched case-insensitively on leaf name and on relative path.
$FullOnlyPatterns = @('hostfxr.dll', 'coreclr.dll', 'System.Private.CoreLib.dll', '*.runtimeconfig.json',
    '*.deps.json', 'runtimes\*', 'PuTTYNG.exe', '*mstscax*', 'Interop.*.dll', 'AxInterop.*.dll')
$MiniMaxDeltaFraction = 0.40

function Fail([string]$Message) { throw [System.InvalidOperationException]::new($Message) }

function Invoke-Native([string]$What, [scriptblock]$Block) {
    & $Block
    if ($LASTEXITCODE -ne 0) { Fail "$What failed (exit $LASTEXITCODE)" }
}

function ConvertTo-JsonString([string]$s) {
    $sb = [System.Text.StringBuilder]::new('"')
    foreach ($c in $s.ToCharArray()) {
        switch ($c) {
            '"' { [void]$sb.Append('\"') }
            '\' { [void]$sb.Append('\\') }
            "`n" { [void]$sb.Append('\n') }
            "`r" { [void]$sb.Append('\r') }
            "`t" { [void]$sb.Append('\t') }
            default {
                if ([int]$c -lt 0x20) { [void]$sb.Append(('\u{0:x4}' -f [int]$c)) } else { [void]$sb.Append($c) }
            }
        }
    }
    [void]$sb.Append('"')
    $sb.ToString()
}

# Canonical JSON: object keys sorted ordinally, 2-space indent, LF. Arrays keep the order given.
function ConvertTo-CanonicalJson($Value, [int]$Level = 0) {
    $pad = '  ' * $Level
    $padIn = '  ' * ($Level + 1)
    if ($null -eq $Value) { return 'null' }
    if ($Value -is [string]) { return ConvertTo-JsonString $Value }
    if ($Value -is [bool]) { return $(if ($Value) { 'true' } else { 'false' }) }
    if ($Value -is [int] -or $Value -is [long]) { return ([long]$Value).ToString([cultureinfo]::InvariantCulture) }
    if ($Value -is [System.Collections.IDictionary]) {
        $keys = [string[]]@($Value.Keys)
        [Array]::Sort($keys, [StringComparer]::Ordinal)
        if ($keys.Count -eq 0) { return '{}' }
        $items = foreach ($k in $keys) { "$padIn$(ConvertTo-JsonString $k): $(ConvertTo-CanonicalJson $Value[$k] ($Level + 1))" }
        return "{`n$($items -join ",`n")`n$pad}"
    }
    if ($Value -is [System.Collections.IEnumerable]) {
        $arr = @($Value)
        if ($arr.Count -eq 0) { return '[]' }
        $items = foreach ($e in $arr) { "$padIn$(ConvertTo-CanonicalJson $e ($Level + 1))" }
        return "[`n$($items -join ",`n")`n$pad]"
    }
    Fail "Unsupported JSON value type $($Value.GetType().FullName)"
}

function Write-Utf8NoBom([string]$Path, [string]$Text) {
    [System.IO.File]::WriteAllBytes($Path, [System.Text.UTF8Encoding]::new($false).GetBytes($Text))
}

function ConvertTo-VersionPair([string]$Text) {
    if ($Text -notmatch '^(\d+\.\d+\.\d+)[+-]([0-9a-f]+)$') { Fail "Version '$Text' is not <major.minor.patch>+<sha> (or -<sha>)" }
    [pscustomobject]@{ Version = "$($Matches[1])+$($Matches[2])"; FileVersion = "$($Matches[1])-$($Matches[2])" }
}

function Get-MajorMinor([string]$Version) { ($Version -split '\.')[0..1] -join '.' }

function Get-PackageEntries([string]$Root) {
    $rootFull = (Resolve-Path -LiteralPath $Root).Path.TrimEnd('\')
    $entries = foreach ($f in Get-ChildItem -LiteralPath $rootFull -Recurse -File -Force) {
        $rel = $f.FullName.Substring($rootFull.Length + 1)
        $top = ($rel -split '\\')[0]
        if ($rel.Contains('\') -and $top -in $ExcludedTopLevel) { continue }
        if (-not $rel.Contains('\') -and ($top -in $ExcludedTopLevel -or $top -like '*.log')) { continue }
        [pscustomobject]@{
            path   = $rel
            sha256 = (Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            size   = [long]$f.Length
        }
    }
    $sorted = [System.Collections.Generic.List[object]]::new(@($entries))
    $sorted.Sort([Comparison[object]] { param($a, $b) [string]::CompareOrdinal($a.path, $b.path) })
    , $sorted.ToArray()
}

function ConvertTo-ManifestFiles($Entries) {
    foreach ($e in $Entries) { [ordered]@{ path = $e.path; sha256 = $e.sha256; size = [long]$e.size } }
}

function Test-FullOnly([string]$Path) {
    $leaf = Split-Path -Leaf $Path
    foreach ($p in $FullOnlyPatterns) { if ($leaf -like $p -or $Path -like $p) { return $true } }
    $false
}

function New-ZipFromEntries([string]$ZipPath, [string]$SourceRoot, $Entries, [string]$ExtraName, [string]$ExtraFile) {
    Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
    if (Test-Path -LiteralPath $ZipPath) { Remove-Item -LiteralPath $ZipPath -Force }
    $zip = [System.IO.Compression.ZipFile]::Open($ZipPath, 'Create')
    try {
        foreach ($e in $Entries) {
            [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip, (Join-Path $SourceRoot $e.path), ($e.path -replace '\\', '/'), [System.IO.Compression.CompressionLevel]::Optimal)
        }
        if ($ExtraFile) {
            [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip, $ExtraFile, $ExtraName, [System.IO.Compression.CompressionLevel]::Optimal)
        }
    } finally { $zip.Dispose() }
}

function Get-ReleaseAssets([string]$ReleaseTag) {
    $json = & gh release view $ReleaseTag --repo $Repo --json assets 2>&1
    if ($LASTEXITCODE -ne 0) { Fail "gh release view $ReleaseTag failed: $($json | Select-Object -First 1)" }
    @(($json | ConvertFrom-Json).assets)
}

function Save-ReleaseAsset([string]$ReleaseTag, [string]$AssetName, [string]$Directory) {
    New-Item -ItemType Directory -Force -Path $Directory | Out-Null
    & gh release download $ReleaseTag --repo $Repo --pattern $AssetName --dir $Directory --clobber 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { Fail "gh release download $AssetName from $ReleaseTag failed" }
    $p = Join-Path $Directory $AssetName
    if (-not (Test-Path -LiteralPath $p)) { Fail "Asset $AssetName not found on release $ReleaseTag" }
    $p
}

# The upload alone left the release title and body naming an older build (2026-10-17): reporters
# read the text, not the asset list. Rewrites the three places that name the latest FULL and reads
# the result back. Body lines it does not find are a failure, not a skip.
function Get-NightlyReleaseBody([string]$Body, [string]$Version, [string]$FileVersion, [string]$ZipName, [long]$SizeMb, [string]$Sha) {
    $commit = $Version.Split('+')[1]
    $nl = if ($Body.Contains("`r`n")) { "`r`n" } else { "`n" }
    $current = '(?m)^\*\*Current build:\*\* .*$'
    $latestRow = '(?m)^\| \*\*`mRemoteNG-full-(?<old>[^`]+)-x64\.zip`\*\* \(\d+ MB\) \| \*\*Latest, recommended\.\*\*.*$'
    $previousRow = '(?m)^\| `mRemoteNG-full-[^`]+-x64\.zip` \(\d+ MB\) \| Previous FULL[^\r\n]*\r?\n'
    $shaLine = '(?m)^SHA-256 of the latest FULL: `[0-9a-f]{64}`'
    foreach ($p in @($current, $latestRow, $shaLine)) {
        if ($Body -notmatch $p) { Fail "Release body has no line matching $p; edit the release text by hand" }
    }

    $old = [regex]::Match($Body, $latestRow).Groups['old'].Value
    $row = "| **``$ZipName``** ($SizeMb MB) | **Latest, recommended.** Full portable build of the current version. Extract into a fresh folder and run. |"
    if ($old -ne $FileVersion) {
        $Body = [regex]::Replace($Body, $previousRow, '')
        $row += "$nl| ``mRemoteNG-full-$old-x64.zip`` ($SizeMb MB) | Previous FULL. |"
    }
    $Body = [regex]::Replace($Body, $current, "**Current build:** ``$Version`` ([commit $commit](https://github.com/$Repo/commit/$commit))")
    $Body = [regex]::Replace($Body, $latestRow, $row.Replace('$', '$$'))
    [regex]::Replace($Body, $shaLine, "SHA-256 of the latest FULL: ``$Sha``")
}

# The upload alone left the release title and body naming an older build (2026-10-17): reporters
# read the text, not the asset list. Rewrites the lines that name the latest FULL and reads the
# result back. A body line it does not find is a failure, not a skip.
function Update-NightlyReleaseText([string]$Version, [string]$FileVersion, [string]$ZipPath, [string]$Workspace) {
    $json = & gh release view nightly --repo $Repo --json body 2>&1
    if ($LASTEXITCODE -ne 0) { Fail "gh release view nightly failed: $($json | Select-Object -First 1)" }
    $sha = (Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $sizeMb = [long][math]::Round((Get-Item -LiteralPath $ZipPath).Length / 1e6)
    $body = Get-NightlyReleaseBody ([string](($json | ConvertFrom-Json).body)) $Version $FileVersion (Split-Path -Leaf $ZipPath) $sizeMb $sha

    New-Item -ItemType Directory -Force -Path $Workspace | Out-Null
    $notes = Join-Path $Workspace 'nightly-notes.md'
    Write-Utf8NoBom $notes $body
    $title = "Nightly Build — $Version (self-contained x64)"
    Invoke-Native 'gh release edit' { gh release edit nightly --repo $Repo --title $title --notes-file $notes }

    # GitHub can serve the previous text for a few seconds after an edit (seen 2026-10-18: the
    # immediate read-back failed while the release itself was correct), so retry before failing.
    $ok = $false
    for ($try = 1; $try -le 6 -and -not $ok; $try++) {
        if ($try -gt 1) { Start-Sleep -Seconds 5 }
        $back = & gh release view nightly --repo $Repo --json name,body 2>&1
        if ($LASTEXITCODE -ne 0) { continue }
        $back = $back | ConvertFrom-Json
        # Compare on the version, not the whole title: the title carries an em dash, which the console decoding of gh output can garble.
        $ok = ([string]$back.name).Contains($Version) -and ([string]$back.body).Contains("``$Version``") -and ([string]$back.body).Contains($sha)
    }
    if (-not $ok) { Fail "Release text read-back does not name $Version and its SHA-256" }
    Write-Host "Release text updated: $title" -ForegroundColor Green
}

$script:failed = $false
$repoRoot =(& git -C $PSScriptRoot rev-parse --show-toplevel).Trim() -replace '/', '\'
if ($LASTEXITCODE -ne 0) { Write-Error 'REFUSED: not inside a git checkout'; exit 1 }
$buildTree = Join-Path ([System.IO.Path]::GetTempPath()) ('mrng-build-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
$scratch = Join-Path ([System.IO.Path]::GetTempPath()) ('mrng-scratch-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
$worktreeAdded = $false

try {
    $dirty = & git -C $repoRoot status --porcelain --untracked-files=no
    if ($dirty) { Fail 'Working tree is dirty (tracked changes); commit or stash before publishing' }
    $headSha = (& git -C $repoRoot rev-parse HEAD).Trim()
    $headShort = $headSha.Substring(0, 10)

    # Tag / HEAD sanity (plan C). On a dry run an unpublished HEAD is only a warning.
    & git -C $repoRoot merge-base --is-ancestor HEAD origin/main 2>$null
    if ($LASTEXITCODE -ne 0) {
        if ($DryRun) { Write-Warning "HEAD $headShort is not on origin/main (allowed for -DryRun)" }
        else { Fail "HEAD $headShort is not on origin/main" }
    }
    if ($Tag -ne 'nightly') {
        $csprojVer = ([xml](Get-Content -Raw (Join-Path $repoRoot 'mRemoteNG\mRemoteNG.csproj'))).SelectSingleNode('//Version').InnerText
        if ("v$csprojVer" -ne $Tag) { Fail "Tag $Tag does not match csproj <Version> $csprojVer" }
    }

    New-Item -ItemType Directory -Force -Path $scratch | Out-Null

    # MINI inputs first: fail fast before a 3-minute build.
    $baseManifest = $null
    if ($Channel -eq 'mini') {
        if ($BaseFullManifestPath) {
            if (-not (Test-Path -LiteralPath $BaseFullManifestPath)) { Fail "BaseFullManifestPath not found: $BaseFullManifestPath" }
            $baseManifest = Get-Content -Raw -LiteralPath $BaseFullManifestPath | ConvertFrom-Json
        } else {
            $basePair = $null
            if ($BaseFull) { $basePair = ConvertTo-VersionPair $BaseFull }
            else {
                $cands = Get-ReleaseAssets $Tag | Where-Object { $_.name -match '^manifest-\d+\.\d+\.\d+-[0-9a-f]+\.json$' } |
                    Sort-Object createdAt -Descending
                foreach ($c in $cands) {
                    $m = Get-Content -Raw -LiteralPath (Save-ReleaseAsset $Tag $c.name $scratch) | ConvertFrom-Json
                    if ($m.channel -eq 'full') { $basePair = ConvertTo-VersionPair $m.version; break }
                }
                if (-not $basePair) { Fail "No FULL manifest found on release $Tag; publish a FULL first" }
            }
            $baseName = "manifest-$($basePair.FileVersion).json"
            if (-not (Get-ReleaseAssets $Tag | Where-Object name -eq $baseName)) { Fail "Base FULL manifest $baseName is not on release $Tag" }
            $baseManifest = Get-Content -Raw -LiteralPath (Save-ReleaseAsset $Tag $baseName $scratch) | ConvertFrom-Json
        }
        if ($baseManifest.channel -ne 'full') { Fail "Base manifest $($baseManifest.version) is not a FULL manifest" }
        if ($BaseFull -and $BaseFullManifestPath -and (ConvertTo-VersionPair $BaseFull).Version -ne $baseManifest.version) {
            Fail "-BaseFull $BaseFull does not match the manifest at -BaseFullManifestPath ($($baseManifest.version))"
        }
    }

    Write-Host "Creating clean worktree at HEAD $headShort ..." -ForegroundColor Cyan
    Invoke-Native 'git worktree add' { git -C $repoRoot worktree add --detach $buildTree HEAD | Out-Null }
    $worktreeAdded = $true

    $env:MREMOTENG_PORTABLE_CHAIN = '1'
    Push-Location $buildTree
    try {
        if (-not $SkipTests) {
            # The self-contained publish does not build the test project; build framework-dependent first, test, then package.
            Write-Host 'Building framework-dependent (test project) ...' -ForegroundColor Cyan
            Invoke-Native 'build.ps1' { pwsh -NoProfile -ExecutionPolicy Bypass -File (Join-Path $buildTree 'build.ps1') }
            Invoke-Native 'run-tests.ps1' { pwsh -NoProfile -ExecutionPolicy Bypass -File (Join-Path $buildTree 'run-tests.ps1') -Headless -NoBuild }
        }
        Write-Host 'Building self-contained x64 ...' -ForegroundColor Cyan
        Invoke-Native 'build.ps1 -SelfContained' { pwsh -NoProfile -ExecutionPolicy Bypass -File (Join-Path $buildTree 'build.ps1') -SelfContained }
        $publish = Join-Path $buildTree 'mRemoteNG\bin\x64\Release\publish'
        if (-not (Test-Path (Join-Path $publish 'mRemoteNG.exe'))) { Fail "Build produced no $publish\mRemoteNG.exe" }

        $verify = Join-Path $buildTree 'scripts\verify-shipped-assemblies.ps1'
        if (Test-Path $verify) {
            Invoke-Native 'verify-shipped-assemblies.ps1' { pwsh -NoProfile -ExecutionPolicy Bypass -File $verify -Path $publish }
        }

        # Version identity from the binary itself.
        $verOut = Join-Path $scratch 'version.txt'
        Start-Process -FilePath (Join-Path $publish 'mRemoteNG.exe') -ArgumentList '--version' -Wait -NoNewWindow -RedirectStandardOutput $verOut | Out-Null
        $verText = Get-Content -Raw -LiteralPath $verOut -ErrorAction SilentlyContinue
        if (-not $verText -or $verText -notmatch '(\d+\.\d+\.\d+)\+([0-9a-f]+)(\.dirty)?\b') {
            Fail "Cannot parse '--version' output: '$("$verText".Trim())'"
        }
        if ($Matches[3]) { Fail "Version identity contains .dirty: $($Matches[0])" }
        $version = "$($Matches[1])+$($Matches[2])"
        $fileVer = "$($Matches[1])-$($Matches[2])"
        if (-not $headSha.StartsWith($Matches[2])) { Fail "Binary commit $($Matches[2]) is not HEAD $headShort" }
        $buildUtc = [DateTime]::UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")

        if (-not $OutputDirectory) { $OutputDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "mrng-release\$fileVer" }
        New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
        $OutputDirectory = (Resolve-Path $OutputDirectory).Path

        Write-Host 'Hashing package files ...' -ForegroundColor Cyan
        $entries = Get-PackageEntries $publish
        $allFiles = @(ConvertTo-ManifestFiles $entries)

        $zipName = "mRemoteNG-$Channel-$fileVer-x64.zip"
        $manifestName = "manifest-$fileVer.json"
        $zipPath = Join-Path $OutputDirectory $zipName
        $manifestPath = Join-Path $OutputDirectory $manifestName
        $releaseBase = "https://github.com/$Repo/releases/download/$Tag"
        $deltaCount = 0; $removeCount = 0; $deltaBytes = 0L; $deltaPaths = @()

        if ($Channel -eq 'full') {
            $manifest = [ordered]@{
                schema = 1; version = $version; commit = $headSha; channel = 'full'; rid = 'win-x64'
                buildUtc = $buildUtc; baseFull = $null; minClientVersion = $MinClientVersion
                fullUrl = "$releaseBase/$zipName"; files = $allFiles
            }
            Write-Utf8NoBom $manifestPath ((ConvertTo-CanonicalJson $manifest) + "`n")
            New-ZipFromEntries $zipPath $publish $entries $null $null
        } else {
            if ((Get-MajorMinor $version) -ne (Get-MajorMinor $baseManifest.version)) {
                Fail "major.minor differs from base ($version vs $($baseManifest.version)); publish a FULL"
            }
            $baseMap = [System.Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
            foreach ($b in $baseManifest.files) { $baseMap[$b.path] = $b }
            $curMap = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
            $delta = [System.Collections.Generic.List[object]]::new()
            foreach ($e in $entries) {
                [void]$curMap.Add($e.path)
                if (-not $baseMap.ContainsKey($e.path) -or $baseMap[$e.path].sha256 -ne $e.sha256) { $delta.Add($e) }
            }
            $remove = [System.Collections.Generic.List[string]]::new()
            foreach ($b in $baseManifest.files) { if (-not $curMap.Contains($b.path)) { $remove.Add([string]$b.path) } }
            $remove.Sort([StringComparer]::Ordinal)

            if ($delta.Count + $remove.Count -gt 0) {
                Write-Host ("Changed vs base: {0} delta, {1} remove" -f $delta.Count, $remove.Count) -ForegroundColor Yellow
                @($delta | ForEach-Object { "  delta:  $($_.path)" }) + @($remove | ForEach-Object { "  remove: $_" }) |
                    Select-Object -First 40 | ForEach-Object { Write-Host $_ }
            }
            $blocked = @((@($delta | ForEach-Object path) + @($remove)) | Where-Object { Test-FullOnly $_ })
            if ($blocked.Count -gt 0) {
                $more = if ($blocked.Count -gt 5) { " (+$($blocked.Count - 5) more)" } else { '' }
                Fail "MINI refused: FULL-only file(s) changed: $(($blocked | Select-Object -First 5) -join ', ')$more"
            }
            # Not Measure-Object: on an empty delta (unchanged commit) it has no Sum under StrictMode.
            $deltaBytes = 0L; foreach ($d in $delta) { $deltaBytes += [long]$d.size }
            $baseTotal = [long](($baseManifest.files | Measure-Object size -Sum).Sum)
            if ($baseTotal -gt 0 -and $deltaBytes -gt $MiniMaxDeltaFraction * $baseTotal) {
                Fail ('MINI refused: delta {0:N0} bytes is over {1:P0} of base FULL ({2:N0}); publish a FULL' -f $deltaBytes, $MiniMaxDeltaFraction, $baseTotal)
            }
            $deltaCount = $delta.Count; $removeCount = $remove.Count; $deltaPaths = @($delta | ForEach-Object path)

            $manifest = [ordered]@{
                schema = 1; version = $version; commit = $headSha; channel = 'mini'; rid = 'win-x64'
                buildUtc = $buildUtc; baseFull = [string]$baseManifest.version; minClientVersion = $MinClientVersion
                fullUrl = [string]$baseManifest.fullUrl; files = $allFiles
                delta = @(ConvertTo-ManifestFiles $delta); remove = @($remove)
            }
            Write-Utf8NoBom $manifestPath ((ConvertTo-CanonicalJson $manifest) + "`n")
            New-ZipFromEntries $zipPath $publish $delta 'manifest.json' $manifestPath
        }

        # TODO(step 4): Ed25519 signature -- produce "$manifestName.sig" over the exact manifest bytes here
        # (key from Vault, in memory only), self-verify with the public key in code, and add it to the upload.
    } finally { Pop-Location }

    $zipSize = (Get-Item -LiteralPath $zipPath).Length
    Write-Host ''
    Write-Host "version : $version" -ForegroundColor Green
    Write-Host "channel : $Channel   tag: $Tag   dryrun: $($DryRun.IsPresent)"
    Write-Host ('files   : {0}   delta: {1} ({2:N0} bytes)   remove: {3}' -f $allFiles.Count, $deltaCount, $deltaBytes, $removeCount)
    Write-Host ('zip     : {0:N0} bytes  {1}' -f $zipSize, $zipPath)
    Write-Host "manifest: $manifestPath"
    if ($deltaCount -gt 0 -and $deltaCount -le 30) { $deltaPaths | ForEach-Object { Write-Host "  delta: $_" } }

    if ($DryRun) { Write-Host 'DryRun: nothing uploaded.' }
    else {
        $existing = @(Get-ReleaseAssets $Tag | Where-Object { $_.name -like "*-$fileVer-*" -or $_.name -eq $manifestName })
        if ($existing.Count -gt 0) { Fail "Release $Tag already has assets for ${fileVer}: $(($existing.name | Select-Object -First 3) -join ', ')" }
        Invoke-Native 'gh release upload' { gh release upload $Tag $zipPath $manifestPath --repo $Repo }
        if ($Tag -eq 'nightly') {
            $latest = Join-Path $OutputDirectory 'manifest-latest.json'
            Copy-Item -LiteralPath $manifestPath -Destination $latest -Force
            Invoke-Native 'gh release upload manifest-latest' { gh release upload $Tag $latest --repo $Repo --clobber }
        }
        $rb = Save-ReleaseAsset $Tag $manifestName (Join-Path $scratch 'readback')
        if (-not [System.Linq.Enumerable]::SequenceEqual([byte[]][System.IO.File]::ReadAllBytes($rb), [byte[]][System.IO.File]::ReadAllBytes($manifestPath))) {
            Fail "Read-back of $manifestName differs from the local manifest"
        }
        Write-Host "Uploaded and verified read-back: $manifestName" -ForegroundColor Green
        if ($Tag -eq 'nightly' -and $Channel -eq 'full') { Update-NightlyReleaseText $version $fileVer $zipPath (Join-Path $scratch 'notes') }
        elseif ($Tag -eq 'nightly') { Write-Warning 'MINI upload: the release text still names the previous build; add the MINI to the chain row by hand.' }
    }
}
catch {
    [Console]::Error.WriteLine("REFUSED: $($_.Exception.Message)")
    $script:failed = $true
}
finally {
    if ($worktreeAdded) { git -C $repoRoot worktree remove --force $buildTree 2>&1 | Out-Null }
    if (Test-Path $scratch) { Remove-Item -Recurse -Force $scratch -ErrorAction SilentlyContinue }
}
if ($script:failed) { exit 1 }

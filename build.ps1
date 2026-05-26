param(
    [switch]$SelfContained,
    [switch]$Portable,     # Self-contained + PORTABLE flag (settings in app folder, embeds .NET runtime)
    [switch]$Rebuild,
    [switch]$NoRestore,    # Skip dotnet restore (use for fast incremental builds)
    [string]$Configuration = "Release",
    [ValidateSet("x64", "x86", "ARM64")]
    [string]$Arch = "x64"
)

# Map arch to MSBuild Platform and .NET RuntimeIdentifier
$platform = $Arch
$rid = switch ($Arch) {
    "x64"   { "win-x64" }
    "x86"   { "win-x86" }
    "ARM64" { "win-arm64" }
}

# Find the newest VS BuildTools installation (VS2026 > VS2022 > etc.)
$vsBasePaths = @(
    "C:\Program Files\Microsoft Visual Studio",
    "C:\Program Files (x86)\Microsoft Visual Studio"
)

$devShell = $null
foreach ($base in $vsBasePaths) {
    if (Test-Path $base) {
        $versions = Get-ChildItem $base -Directory | Sort-Object Name -Descending
        foreach ($ver in $versions) {
            $editions = @("Enterprise", "Professional", "Community", "BuildTools")
            foreach ($ed in $editions) {
                $candidate = Join-Path $ver.FullName "$ed\Common7\Tools\Launch-VsDevShell.ps1"
                if (Test-Path $candidate) {
                    $devShell = $candidate
                    break
                }
            }
            if ($devShell) { break }
        }
    }
    if ($devShell) { break }
}

if (-not $devShell) {
    throw "No Visual Studio installation found. Install VS2026 or VS2022 BuildTools."
}

Write-Host "Using: $devShell"
& $devShell -Arch amd64

$sln = "$PSScriptRoot\mRemoteNG.sln"

# Disable MSBuild worker-node reuse. With -m, reusable nodes survive the build
# by design and keep open handles on obj\*.cache. When build.ps1 runs inside a
# sandboxed agent (e.g. codex-rescue) whose process group is torn down on exit,
# those nodes orphan and keep locking the cache, so the next build fails with
# MSB3491 "Access to the path is denied". Disabling reuse makes nodes exit with
# the build. Costs ~1s on repeat builds; correctness over the marginal speed.
$env:MSBUILDDISABLENODEREUSE = '1'

$timer = [System.Diagnostics.Stopwatch]::StartNew()

if ($Portable) {
    Write-Host "Building portable edition ($Arch, self-contained + PORTABLE flag)..."
    if (-not $NoRestore) {
        dotnet restore $sln --runtime $rid
    }
    # PublishReadyToRun=false avoids NETSDK1094 crossgen2 issue; startup impact is negligible.
    msbuild $sln -m -nodeReuse:false "-verbosity:minimal" "-p:Configuration=Release" "-p:Platform=$platform" "-p:DefineConstants=PORTABLE" "-p:SelfContained=true" "-p:RuntimeIdentifier=$rid" "-p:PublishReadyToRun=false" "-p:SignAssembly=false" "-p:PublishDir=bin\$platform\Portable\" -t:Publish
    Write-Host "Portable output: mRemoteNG\bin\$platform\Portable\" -ForegroundColor Green
} elseif ($SelfContained) {
    Write-Host "Building self-contained $Arch (embedded .NET runtime)..."
    if (-not $NoRestore) {
        dotnet restore $sln --runtime $rid /p:PublishReadyToRun=true
    }
    # PublishReadyToRun=false avoids NETSDK1094 crossgen2 issue on local builds.
    msbuild $sln -m -nodeReuse:false "-verbosity:minimal" "-p:Configuration=Release" "-p:Platform=$platform" "-p:SelfContained=true" "-p:RuntimeIdentifier=$rid" "-p:PublishReadyToRun=false" "-p:PublishDir=bin\$platform\Release\publish\" -t:Publish
} else {
    Write-Host "Building framework-dependent $Arch..."
    if (-not $NoRestore) {
        dotnet restore $sln
    }
    $msbuildArgs = @('-m', '-nodeReuse:false', '-verbosity:minimal', "-p:Configuration=$Configuration", "-p:Platform=$platform", '-p:SignAssembly=false')
    if ($Rebuild) { $msbuildArgs += '-t:Rebuild' }
    if ($NoRestore) { $msbuildArgs += '-p:RestorePackages=false' }
    msbuild $sln @msbuildArgs
}

$timer.Stop()

# Move satellite assemblies from stale culture folders into Languages/
$outDir = Join-Path $PSScriptRoot "mRemoteNG\bin\$Arch\$Configuration"
$langDir = Join-Path $outDir "Languages"
$knownDirs = @('Assemblies','Icons','Languages','Plugins','publish','ref','runtimes','Schemas','Settings','Themes')
$moved = 0
Get-ChildItem $outDir -Directory -ErrorAction SilentlyContinue | Where-Object {
    $_.Name -notin $knownDirs -and $_.Name -notlike 'win-*'
} | ForEach-Object {
    $resDlls = Get-ChildItem $_.FullName -Filter '*.resources.dll' -ErrorAction SilentlyContinue
    if ($resDlls) {
        $dest = Join-Path $langDir $_.Name
        if (-not (Test-Path $dest)) { New-Item $dest -ItemType Directory -Force | Out-Null }
        $resDlls | ForEach-Object {
            $target = Join-Path $dest $_.Name
            if (-not (Test-Path $target)) { Move-Item $_.FullName $target } else { Remove-Item $_.FullName }
        }
        $moved++
    }
    if ((Get-ChildItem $_.FullName -Force -ErrorAction SilentlyContinue).Count -eq 0) {
        Remove-Item $_.FullName -Force
    }
}
if ($moved -gt 0) { Write-Host "Moved $moved culture folder(s) into Languages/" -ForegroundColor DarkGray }

# Copy local test connections file if present — DEVELOPMENT MACHINES ONLY.
# Never do this on CI: the release ZIP would ship the developer's working set.
# Guard every known CI signal so the step cannot run under a release workflow.
$isCI = $env:CI -or $env:GITHUB_ACTIONS -or $env:TF_BUILD -or $env:BUILD_BUILDID -or $env:JENKINS_URL -or $env:GITLAB_CI
if (-not $isCI) {
    $localConfCons = Join-Path 'E:\OneDrive\_Portable\mRemoteNG 1.77.1' 'confCons.xml'
    if (Test-Path $localConfCons) {
        $settingsDir = Join-Path $outDir 'Settings'
        if (-not (Test-Path $settingsDir)) { New-Item $settingsDir -ItemType Directory -Force | Out-Null }
        Copy-Item $localConfCons (Join-Path $settingsDir 'confCons.xml') -Force
        Write-Host "Copied test connections from portable install (local dev only)" -ForegroundColor DarkGray
    }
}

$elapsed = $timer.Elapsed.TotalSeconds.ToString('F1')
Write-Host "Build completed in ${elapsed}s" -ForegroundColor Cyan

# Append timing to build log (CSV: timestamp, seconds, mode, arch, hostname)
$buildMode = if ($Portable) { 'portable' } elseif ($SelfContained) { 'self-contained' } elseif ($NoRestore) { 'no-restore' } else { 'full' }
$logLine = "$([DateTime]::Now.ToString('yyyy-MM-dd HH:mm:ss')),$elapsed,$buildMode,$Arch,$env:COMPUTERNAME"
$logFile = Join-Path $PSScriptRoot 'build-timing.log'
if (-not (Test-Path $logFile)) {
    'timestamp,seconds,mode,arch,hostname' | Out-File $logFile -Encoding utf8
}
$logLine | Out-File $logFile -Append -Encoding utf8

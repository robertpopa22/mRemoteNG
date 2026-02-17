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
    throw "No Visual Studio installation found."
}

Write-Host "Using: $devShell"
& $devShell -Arch amd64

MSBuild.exe mRemoteNGTests\mRemoteNGTests.csproj -restore -m "-verbosity:normal" "-p:Configuration=Release" "-p:Platform=x64" -t:Rebuild

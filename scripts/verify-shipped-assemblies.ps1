<#
.SYNOPSIS
    Verifies that a shipped mRemoteNG package contains every assembly it declares it needs.

.DESCRIPTION
    mRemoteNG does not keep its dependencies beside the executable. The build moves most of them
    into an Assemblies\ subdirectory and a custom AssemblyResolve handler finds them there. That
    arrangement lets a newly added package resolve perfectly during development -- the SDK output
    has it -- and then throw FileNotFoundException on a user's machine, because the copy step never
    learned about it. #150 was that failure.

    The oracle is deps.json: the build's own statement of what it expects to load. Comparing against
    it cannot drift the way a hand-written expected-file list would.

    This runs against the artifact that actually ships (a ZIP, or an unpacked directory), which is
    the difference that matters. The equivalent app-level test can only see the build output on the
    machine that ran it; the packaging step in between is exactly where files go missing.

.PARAMETER Path
    A .zip package or a directory containing mRemoteNG.exe and mRemoteNG.deps.json.

.EXAMPLE
    pwsh -File scripts/verify-shipped-assemblies.ps1 -Path mRemoteNG-Portable.zip
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Path
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $Path)) {
    Write-Error "Not found: $Path"
    exit 2
}

$temp = $null
try {
    if ((Get-Item -LiteralPath $Path).PSIsContainer) {
        $root = (Resolve-Path -LiteralPath $Path).Path
    }
    else {
        $temp = Join-Path ([System.IO.Path]::GetTempPath()) ("mrng-verify-" + [guid]::NewGuid().ToString('N').Substring(0, 12))
        New-Item -ItemType Directory -Path $temp | Out-Null
        Expand-Archive -LiteralPath $Path -DestinationPath $temp -Force
        $root = $temp
    }

    $depsJson = Get-ChildItem -Path $root -Filter 'mRemoteNG.deps.json' -Recurse -File |
        Select-Object -First 1
    if (-not $depsJson) {
        Write-Error "mRemoteNG.deps.json not found under $Path -- cannot verify what the package needs."
        exit 1
    }

    $appDir = $depsJson.DirectoryName
    $deps = Get-Content -LiteralPath $depsJson.FullName -Raw | ConvertFrom-Json

    # Every runtime asset the build declares, flattened to bare file names.
    $declared = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($target in $deps.targets.PSObject.Properties) {
        foreach ($library in $target.Value.PSObject.Properties) {
            $runtime = $library.Value.runtime
            if (-not $runtime) { continue }
            foreach ($asset in $runtime.PSObject.Properties) {
                $file = ($asset.Name -split '/')[-1]
                if ($file -like '*.dll') { [void]$declared.Add($file) }
            }
        }
    }

    if ($declared.Count -eq 0) {
        Write-Error "deps.json declared no runtime assemblies -- the package is not verifiable."
        exit 1
    }

    # Both places the resolver looks: beside the executable, and in Assemblies\.
    $present = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($dir in @($appDir, (Join-Path $appDir 'Assemblies'))) {
        if (Test-Path -LiteralPath $dir) {
            Get-ChildItem -LiteralPath $dir -Filter '*.dll' -File |
                ForEach-Object { [void]$present.Add($_.Name) }
        }
    }

    $missing = @($declared | Where-Object { -not $present.Contains($_) } | Sort-Object)

    Write-Host "Package:  $Path"
    Write-Host "Declared: $($declared.Count) runtime assemblies"
    Write-Host "Present:  $($present.Count) in the package"

    if ($missing.Count -gt 0) {
        Write-Error ("Assemblies the build says it needs are absent from the shipped package. " +
                     "These resolve during development and throw FileNotFoundException on a user's " +
                     "machine (#150):`n  " + ($missing -join "`n  "))
        exit 1
    }

    # A populated Assemblies\ is the arrangement the resolver is written for. Without this check the
    # comparison above would still pass if the copy step stopped running and everything happened to
    # land beside the executable instead.
    $assembliesDir = Join-Path $appDir 'Assemblies'
    if (-not (Test-Path -LiteralPath $assembliesDir) -or
        -not (Get-ChildItem -LiteralPath $assembliesDir -Filter '*.dll' -File)) {
        Write-Error "The Assemblies\ subdirectory is missing or empty -- the custom AssemblyResolve handler has nowhere to look."
        exit 1
    }

    Write-Host "Assembly completeness check passed."
    exit 0
}
finally {
    if ($temp -and (Test-Path -LiteralPath $temp)) {
        Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
    }
}

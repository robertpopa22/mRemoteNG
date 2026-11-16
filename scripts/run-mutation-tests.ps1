param(
    [string]$OutputDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'mRemoteNG-mutation')
)

$ErrorActionPreference = 'Stop'
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
$architectureWasMissing = [Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT -and -not $env:PROCESSOR_ARCHITECTURE

Push-Location (Join-Path $PSScriptRoot '../tests/mutation')
try {
    if ($architectureWasMissing) {
        # The .NET Windows MSI installer assumes this variable exists when Buildalyzer calls dotnet --info.
        $env:PROCESSOR_ARCHITECTURE = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString() -replace '^X64$', 'AMD64'
    }

    dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw "Stryker.NET tool restore failed with exit code $LASTEXITCODE."
    }

    dotnet tool run dotnet-stryker -- --skip-version-check --output $outputPath
    exit $LASTEXITCODE
}
finally {
    if ($architectureWasMissing) {
        Remove-Item Env:PROCESSOR_ARCHITECTURE
    }
    Pop-Location
}

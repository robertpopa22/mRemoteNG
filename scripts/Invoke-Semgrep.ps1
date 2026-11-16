#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

$semgrepVersion = '1.185.0'
# Fetch the original rules for local use under https://semgrep.dev/legal/rules-license/.
$rulesRevision = 'a5ad1714f00c9f7fde980cab0ea1d6dc6d414fd2'
$rulePaths = @(
    'csharp/dotnet/security/use_deprecated_cipher_algorithm.yaml'
    'csharp/dotnet/security/use_ecb_mode.yaml'
    'csharp/dotnet/security/use_weak_rng_for_keygeneration.yaml'
    'csharp/dotnet/security/use_weak_rsa_encryption_padding.yaml'
    'csharp/lang/correctness/sslcertificatetrust/sslcertificatetrust-handshake-no-trust.yaml'
    'csharp/lang/security/filesystem/unsafe-path-combine.yaml'
    'csharp/lang/security/http/http-listener-wildcard-bindings.yaml'
    'csharp/lang/security/injections/os-command.yaml'
    'csharp/lang/security/insecure-deserialization/binary-formatter.yaml'
    'csharp/lang/security/insecure-deserialization/newtonsoft.yaml'
    'csharp/lang/security/regular-expression-dos/regular-expression-dos-infinite-timeout.yaml'
    'csharp/lang/security/sqli/csharp-sqli.yaml'
    'csharp/lang/security/xxe/xmldocument-unsafe-parser-override.yaml'
    'csharp/lang/security/xxe/xmlreadersettings-unsafe-parser-override.yaml'
    'csharp/lang/security/xxe/xmltextreader-unsafe-defaults.yaml'
)

$output = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
$runDirectory = Join-Path $output ([Guid]::NewGuid().ToString('N'))
$rulesDirectory = Join-Path $runDirectory 'rules'
$null = New-Item -ItemType Directory -Path $rulesDirectory -Force
$rulesBase = "https://raw.githubusercontent.com/semgrep/semgrep-rules/$rulesRevision"
Invoke-WebRequest -Uri "$rulesBase/LICENSE" -OutFile (Join-Path $rulesDirectory 'LICENSE')
foreach ($rulePath in $rulePaths) {
    Invoke-WebRequest -Uri "$rulesBase/$rulePath" -OutFile (Join-Path $rulesDirectory (Split-Path -Leaf $rulePath))
}

$jsonReport = Join-Path $runDirectory 'semgrep.json'
$sarifReport = Join-Path $runDirectory 'semgrep.sarif'
$previousSettings = $env:SEMGREP_SETTINGS_FILE
$previousAppToken = $env:SEMGREP_APP_TOKEN
Write-Host "Semgrep reports: $runDirectory"
Push-Location (Join-Path $PSScriptRoot '..')
try {
    $env:SEMGREP_SETTINGS_FILE = Join-Path $runDirectory 'settings.yml'
    $env:SEMGREP_APP_TOKEN = $null
    # Scan authored code, skipping only the generated NSwag client.
    & uv tool run --from "semgrep==$semgrepVersion" semgrep scan `
        --oss-only --metrics=off --disable-version-check --strict --verbose `
        --no-rewrite-rule-ids --max-target-bytes=0 `
        --config $rulesDirectory --include '*.cs' --json-output $jsonReport --sarif-output $sarifReport `
        --exclude 'ExternalConnectors/DSS/SecretServerRestClient.cs' `
        mRemoteNG ExternalConnectors mRemoteNG.Plugins
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    # Keep scanner errors authoritative when findings and parser errors coexist.
    $report = Get-Content -LiteralPath $jsonReport -Raw | ConvertFrom-Json
    if ($report.paths.scanned.Count -eq 0 -or $report.errors.Count -gt 0 -or
        -not (Test-Path -LiteralPath $sarifReport -PathType Leaf)) {
        throw 'Semgrep reports are missing or the scan is incomplete.'
    }
    if ($report.results.Count -gt 0) {
        exit 10
    }
    exit 0
}
finally {
    $env:SEMGREP_SETTINGS_FILE = $previousSettings
    $env:SEMGREP_APP_TOKEN = $previousAppToken
    Pop-Location
}

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
$relativeOutput = [IO.Path]::GetRelativePath($repoRoot, $outputPath)
if (-not [IO.Path]::IsPathRooted($relativeOutput) -and $relativeOutput -notmatch '^\.\.([/\\]|$)') {
    throw 'Store PowerShell test reports in a directory outside the repository.'
}

Import-Module Pester -RequiredVersion 6.2.0
$null = New-Item -ItemType Directory -Path $outputPath -Force
$sourceName = 'lab-msi-edition.ps1'
$coveragePath = Join-Path $outputPath 'powershell-cobertura.xml'
$sonarPath = Join-Path $outputPath 'powershell-sonar-coverage.xml'

$configuration = New-PesterConfiguration
$configuration.Run.Path = Join-Path $repoRoot 'mRemoteNGTests/PowerShell'
$configuration.Run.PassThru = $true
$configuration.CodeCoverage.Enabled = $true
$configuration.CodeCoverage.Path = Join-Path $repoRoot $sourceName
$configuration.CodeCoverage.ReportRoot = $repoRoot
$configuration.CodeCoverage.OutputFormat = 'Cobertura'
$configuration.CodeCoverage.OutputPath = $coveragePath
$result = Invoke-Pester -Configuration $configuration
if ($result.Result -ne 'Passed' -or $result.PassedCount -eq 0) {
    throw 'PowerShell tests failed or did not run.'
}
if ($result.CodeCoverage.CommandsExecutedCount -eq 0 -or -not (Test-Path -LiteralPath $coveragePath -PathType Leaf)) {
    throw 'Pester did not measure PowerShell code coverage.'
}

[xml]$measuredCoverage = Get-Content -Raw -LiteralPath $coveragePath
# Pester's class-level Cobertura lines combine hits for commands on the same source line.
$lines = $measuredCoverage.SelectNodes("/coverage/packages/package/classes/class[@filename='$sourceName']/lines/line")
if ($lines.Count -eq 0) { throw "The Pester report has no lines for $sourceName." }

[xml]$sonarCoverage = '<coverage version="1" />'
$file = $sonarCoverage.CreateElement('file')
$file.SetAttribute('path', $sourceName)
$null = $sonarCoverage.DocumentElement.AppendChild($file)
$coveredCount = 0
foreach ($line in $lines) {
    $covered = [int]$line.hits -gt 0
    if ($covered) { $coveredCount++ }
    $lineToCover = $sonarCoverage.CreateElement('lineToCover')
    $lineToCover.SetAttribute('lineNumber', $line.number)
    $lineToCover.SetAttribute('covered', [System.Xml.XmlConvert]::ToString($covered))
    $null = $file.AppendChild($lineToCover)
}
$sonarCoverage.Save($sonarPath)
Write-Host "PowerShell coverage: $coveredCount/$($lines.Count) lines. Sonar report: $sonarPath"

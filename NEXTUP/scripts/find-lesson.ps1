[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Pattern,

    [int]$MaxResults = 25
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$nextupRoot = Split-Path -Parent $PSScriptRoot
$targets = @(
    (Join-Path $nextupRoot "LESSONS.md"),
    (Join-Path $nextupRoot "COMMAND_FEEDBACK_LOG.md"),
    (Join-Path $nextupRoot "command-feedback.jsonl")
)

$found = @()
foreach ($target in $targets) {
    if (-not (Test-Path $target)) {
        continue
    }

    $matches = Select-String -Path $target -Pattern $Pattern -SimpleMatch -CaseSensitive:$false
    foreach ($match in $matches) {
        $found += [pscustomobject]@{
            Path = $target
            Line = $match.LineNumber
            Text = $match.Line.Trim()
        }
    }
}

if ($found.Count -eq 0) {
    Write-Host "No lessons found for pattern: $Pattern"
    exit 1
}

$found |
    Select-Object -First $MaxResults |
    ForEach-Object {
        Write-Host ("{0}:{1}  {2}" -f $_.Path, $_.Line, $_.Text)
    }

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Show-Usage() {
    Write-Host "Usage: nx [--no-capture] <alias> [args]"
    Write-Host "  g|git       - Run git with canonical path"
    Write-Host "  h|gh        - Run gh with canonical path"
    Write-Host "  p|ps        - Run PowerShell with canonical path"
    Write-Host "  lesson ...  - Shortcut to find-lesson.ps1"
    Write-Host "  metrics ... - Shortcut to refresh-command-feedback-metrics.ps1"
    Write-Host "  paths       - Print canonical path aliases"
}

function Sanitize-Fragment([string]$Value, [int]$MaxLength = 48) {
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return "none"
    }

    $sanitized = $Value.ToLowerInvariant() -replace "[^a-z0-9]+", "-"
    $sanitized = $sanitized.Trim("-")
    if ([string]::IsNullOrWhiteSpace($sanitized)) {
        $sanitized = "none"
    }

    if ($sanitized.Length -gt $MaxLength) {
        $sanitized = $sanitized.Substring(0, $MaxLength).Trim("-")
    }

    if ([string]::IsNullOrWhiteSpace($sanitized)) {
        return "none"
    }

    return $sanitized
}

function Get-CommandDisplay([string]$Executable, [string[]]$ExecutableArgs) {
    $parts = New-Object System.Collections.Generic.List[string]
    $parts.Add($Executable)
    foreach ($arg in $ExecutableArgs) {
        if ($arg -match '[\s"`]') {
            $escaped = $arg -replace '"', '\"'
            $parts.Add('"' + $escaped + '"')
        }
        else {
            $parts.Add($arg)
        }
    }
    return ($parts -join " ")
}

function Get-RelativePathCompat([string]$BasePath, [string]$TargetPath) {
    try {
        if ([System.IO.Path].GetMethod("GetRelativePath", [type[]]@([string], [string]))) {
            return [System.IO.Path]::GetRelativePath($BasePath, $TargetPath)
        }
    }
    catch {
    }

    $normalizedBase = [System.IO.Path]::GetFullPath($BasePath)
    $normalizedTarget = [System.IO.Path]::GetFullPath($TargetPath)

    if (-not $normalizedBase.EndsWith("\")) {
        $normalizedBase = $normalizedBase + "\"
    }

    $baseUri = New-Object System.Uri($normalizedBase)
    $targetUri = New-Object System.Uri($normalizedTarget)
    $relativeUri = $baseUri.MakeRelativeUri($targetUri)
    return [System.Uri]::UnescapeDataString($relativeUri.ToString().Replace('/', '\'))
}

function New-CaptureContext([string]$AliasName, [string[]]$ForwardArgs, [string]$NextupRoot) {
    $startedAt = Get-Date
    $runId = "{0}_{1}" -f $startedAt.ToString("yyyyMMdd_HHmmss_fff"), ([Guid]::NewGuid().ToString("N").Substring(0, 6))
    $aliasFrag = Sanitize-Fragment $AliasName
    $argFrag = if ($ForwardArgs.Count -gt 0) { Sanitize-Fragment $ForwardArgs[0] } else { "none" }
    $dayPath = Join-Path (Join-Path $NextupRoot "command-output") ($startedAt.ToString("yyyy-MM-dd"))
    New-Item -ItemType Directory -Force -Path $dayPath | Out-Null

    $baseName = "$runId-$aliasFrag-$argFrag"
    return [pscustomobject]@{
        StartedAt = $startedAt
        RunId = $runId
        Alias = $AliasName
        OutputRoot = Join-Path $NextupRoot "command-output"
        DayPath = $dayPath
        LogPath = Join-Path $dayPath ($baseName + ".log.txt")
        MetaPath = Join-Path $dayPath ($baseName + ".meta.json")
        IndexPath = Join-Path (Join-Path $NextupRoot "command-output") "index.jsonl"
    }
}

function Persist-RunMetadata(
    [pscustomobject]$Context,
    [string]$Executable,
    [string[]]$ExecutableArgs,
    [int]$ExitCode,
    [bool]$CaptureEnabled
) {
    $endedAt = Get-Date
    $durationSeconds = [math]::Round((New-TimeSpan -Start $Context.StartedAt -End $endedAt).TotalSeconds, 3)
    $commandDisplay = Get-CommandDisplay -Executable $Executable -ExecutableArgs $ExecutableArgs
    $success = ($ExitCode -eq 0)
    $relativeLogPath = Get-RelativePathCompat -BasePath $Context.OutputRoot -TargetPath $Context.LogPath
    $relativeMetaPath = Get-RelativePathCompat -BasePath $Context.OutputRoot -TargetPath $Context.MetaPath

    $meta = [ordered]@{
        run_id = $Context.RunId
        alias = $Context.Alias
        started_at = $Context.StartedAt.ToString("s")
        ended_at = $endedAt.ToString("s")
        duration_seconds = $durationSeconds
        executable = $Executable
        arguments = $ExecutableArgs
        command_display = $commandDisplay
        exit_code = $ExitCode
        success = $success
        capture_enabled = $CaptureEnabled
        log_path = $Context.LogPath
        log_path_relative = $relativeLogPath
    }

    $meta | ConvertTo-Json -Depth 8 | Set-Content -Path $Context.MetaPath -Encoding utf8

    $indexEntry = [ordered]@{
        run_id = $Context.RunId
        alias = $Context.Alias
        started_at = $Context.StartedAt.ToString("s")
        duration_seconds = $durationSeconds
        exit_code = $ExitCode
        success = $success
        command_display = $commandDisplay
        log_path_relative = $relativeLogPath
        meta_path_relative = $relativeMetaPath
    }

    $indexEntry | ConvertTo-Json -Compress | Add-Content -Path $Context.IndexPath -Encoding utf8
    Write-Host ("[nx] Output saved: {0}" -f $Context.LogPath)
}

function Invoke-ExternalCommand(
    [pscustomobject]$Context,
    [string]$Executable,
    [string[]]$ExecutableArgs,
    [bool]$CaptureEnabled
) {
    $global:LASTEXITCODE = 0
    $originalErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $writer = $null
    try {
        if ($CaptureEnabled) {
            $writer = New-Object System.IO.StreamWriter($Context.LogPath, $false, [System.Text.UTF8Encoding]::new($false))
        }

        $outputLines = & $Executable @ExecutableArgs 2>&1
        foreach ($entry in @($outputLines)) {
            $line = $entry.ToString()
            if ($CaptureEnabled -and $null -ne $writer) {
                $writer.WriteLine($line)
                $writer.Flush()
            }
            Write-Host $line
        }
    }
    finally {
        $ErrorActionPreference = $originalErrorActionPreference
        if ($null -ne $writer) {
            $writer.Dispose()
        }
    }

    if ($null -ne $LASTEXITCODE) {
        return [int]$LASTEXITCODE
    }

    if ($?) {
        return 0
    }

    return 1
}

function Write-InternalLines(
    [pscustomobject]$Context,
    [string[]]$Lines,
    [bool]$CaptureEnabled
) {
    foreach ($line in $Lines) {
        Write-Output $line
    }

    if ($CaptureEnabled) {
        $Lines | Set-Content -Path $Context.LogPath -Encoding utf8
    }
}

$allArgs = @($args)
$captureEnabled = $true
$filtered = New-Object System.Collections.Generic.List[string]
foreach ($arg in $allArgs) {
    if ($arg -eq "--no-capture") {
        $captureEnabled = $false
        continue
    }
    if ($arg -eq "--capture") {
        $captureEnabled = $true
        continue
    }
    $filtered.Add($arg)
}

if ($filtered.Count -eq 0) {
    Show-Usage
    exit 1
}

$aliasName = [string]$filtered[0]
$forwardArgs = @()
if ($filtered.Count -gt 1) {
    $forwardArgs = @($filtered[1..($filtered.Count - 1)])
}

$gitExe = "C:\PROGRA~1\Git\cmd\git.exe"
$ghExe = "C:\PROGRA~1\GITHUB~1\gh.exe"
$psExe = "C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe"

$repoRoot = "D:\github\mRemoteNG"
$nextupRoot = Join-Path $repoRoot "NEXTUP"
$localRoot = "D:\github\LOCAL"
$aliasLower = $aliasName.ToLowerInvariant()
$context = New-CaptureContext -AliasName $aliasLower -ForwardArgs $forwardArgs -NextupRoot $nextupRoot

$executable = ""
$exeArgs = @()
$internalLines = @()
$isInternal = $false
$exitCode = 1

switch ($aliasLower) {
    "g" {
        $executable = $gitExe
        $exeArgs = $forwardArgs
    }
    "git" {
        $executable = $gitExe
        $exeArgs = $forwardArgs
    }
    "h" {
        $executable = $ghExe
        $exeArgs = $forwardArgs
    }
    "gh" {
        $executable = $ghExe
        $exeArgs = $forwardArgs
    }
    "p" {
        $executable = $psExe
        $exeArgs = $forwardArgs
    }
    "ps" {
        $executable = $psExe
        $exeArgs = $forwardArgs
    }
    "lesson" {
        $scriptPath = Join-Path $nextupRoot "scripts\find-lesson.ps1"
        $executable = $psExe
        $exeArgs = @("-NoProfile", "-File", $scriptPath) + $forwardArgs
    }
    "metrics" {
        $scriptPath = Join-Path $nextupRoot "scripts\refresh-command-feedback-metrics.ps1"
        $executable = $psExe
        $exeArgs = @("-NoProfile", "-File", $scriptPath) + $forwardArgs
    }
    "paths" {
        $isInternal = $true
        $internalLines = @(
            "REPO_ROOT=$repoRoot"
            "NEXTUP_ROOT=$nextupRoot"
            "LOCAL_ROOT=$localRoot"
            "GIT_EXE=$gitExe"
            "GH_EXE=$ghExe"
            "PS_EXE=$psExe"
        )
        $exitCode = 0
    }
    default {
        $isInternal = $true
        $internalLines = @(
            "Unknown alias: $aliasName"
            "Usage: nx [--no-capture] <alias> [args]"
            "  g|git       - Run git with canonical path"
            "  h|gh        - Run gh with canonical path"
            "  p|ps        - Run PowerShell with canonical path"
            "  lesson ...  - Shortcut to find-lesson.ps1"
            "  metrics ... - Shortcut to refresh-command-feedback-metrics.ps1"
            "  paths       - Print canonical path aliases"
        )
        $exitCode = 1
    }
}

if ($isInternal) {
    Write-InternalLines -Context $context -Lines $internalLines -CaptureEnabled $captureEnabled
    Persist-RunMetadata -Context $context -Executable ("internal:" + $aliasLower) -ExecutableArgs $forwardArgs -ExitCode $exitCode -CaptureEnabled $captureEnabled
    exit $exitCode
}

$exitCode = Invoke-ExternalCommand -Context $context -Executable $executable -ExecutableArgs $exeArgs -CaptureEnabled $captureEnabled
Persist-RunMetadata -Context $context -Executable $executable -ExecutableArgs $exeArgs -ExitCode $exitCode -CaptureEnabled $captureEnabled
exit $exitCode

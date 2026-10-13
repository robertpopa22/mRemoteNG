[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string]$LogPath,

    [switch]$AsJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-NumberSummary {
    param([long[]]$Values)

    if (-not $Values -or $Values.Count -eq 0) { return $null }
    $sorted = @($Values | Sort-Object)
    $average = ($sorted | Measure-Object -Average).Average
    $p50Index = [Math]::Min($sorted.Count - 1, [Math]::Floor(($sorted.Count - 1) * 0.50))
    $p95Index = [Math]::Min($sorted.Count - 1, [Math]::Ceiling(($sorted.Count - 1) * 0.95))

    [ordered]@{
        Count = $sorted.Count
        MinMs = $sorted[0]
        AverageMs = [Math]::Round($average, 1)
        P50Ms = $sorted[$p50Index]
        P95Ms = $sorted[$p95Index]
        MaxMs = $sorted[-1]
    }
}

function ConvertFrom-PerfFields {
    param([string]$Text)

    $fields = @{}
    foreach ($match in [regex]::Matches($Text, '(?<key>[a-z0-9_]+)=(?:"(?<quoted>[^"]*)"|(?<bare>\S+))')) {
        $value = if ($match.Groups['quoted'].Success) { $match.Groups['quoted'].Value } else { $match.Groups['bare'].Value }
        $fields[$match.Groups['key'].Value] = $value
    }
    return $fields
}

$lines = @(Get-Content -LiteralPath $LogPath -ErrorAction Stop)
$perfEvents = [System.Collections.Generic.List[object]]::new()
$appSessions = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$warningCount = 0
$errorCount = 0

foreach ($line in $lines) {
    if ($line -match 'app_session=(?<session>[a-f0-9]{12})') {
        [void]$appSessions.Add($Matches.session)
    }
    if ($line -match '\sWARN(?:ING)?\s*-') { $warningCount++ }
    if ($line -match '\sERROR\s*-') { $errorCount++ }

    if ($line -match '\[Perf\]\s+event=(?<event>[a-z0-9_.-]+)(?<rest>.*)$') {
        $fields = ConvertFrom-PerfFields -Text $Matches.rest
        $perfEvents.Add([pscustomobject]@{ Event = $Matches.event; Fields = $fields })
        continue
    }

    # Compatibility with logs written before structured runtime diagnostics. New-format
    # human startup lines have app_session= and are deliberately not counted a second time.
    if ($line -notmatch 'app_session=' -and $line -match '\[Startup\]\s+(?<phase>[^:]+):\s*(?<duration>\d+)ms') {
        $legacyPhaseText = $Matches.phase.Trim()
        $legacyDuration = $Matches.duration
        $legacyPhase = switch -Regex ($legacyPhaseText) {
            '^SettingsLoad$' { 'settings_load'; break }
            '^InitializeProgram$' { 'initialize_program'; break }
            '^PanelLayout$' { 'panel_layout'; break }
            '^LoadConnections$' { 'load_connections'; break }
            '^StartupDataLogger$' { 'startup_data_logger'; break }
            '^IconLoader$' { 'icon_loader'; break }
            default { ($Matches.phase.Trim() -replace '\s+', '_').ToLowerInvariant() }
        }
        $perfEvents.Add([pscustomobject]@{
            Event = 'startup_phase'
            Fields = @{ phase = $legacyPhase; duration_ms = $legacyDuration }
        })
    }
}

$startup = @($perfEvents | Where-Object Event -eq 'startup_phase' | Group-Object { $_.Fields.phase } | ForEach-Object {
    [pscustomobject]@{
        Phase = $_.Name
        Timing = Get-NumberSummary -Values @($_.Group | ForEach-Object { [long]$_.Fields.duration_ms })
    }
})

$saveEvents = @($perfEvents | Where-Object Event -eq 'connections_save')
$rdpTimings = @($perfEvents | Where-Object Event -eq 'rdp_phase' | Group-Object { $_.Fields.phase } | ForEach-Object {
    [pscustomobject]@{
        Phase = $_.Name
        Timing = Get-NumberSummary -Values @($_.Group | ForEach-Object { [long]$_.Fields.duration_ms })
    }
})
function Get-Field {
    param($PerfEvent, [string]$Key)
    if ($PerfEvent.Fields.ContainsKey($Key)) { $PerfEvent.Fields[$Key] } else { 'na' }
}

$networkSummaries = @($perfEvents | Where-Object Event -eq 'rdp_network_summary')
$networkLines = @($perfEvents | Where-Object Event -eq 'rdp_network')
$reconnectEvents = @($perfEvents | Where-Object Event -eq 'rdp_reconnect')
$disconnectCodes = @($perfEvents | Where-Object { $_.Event -eq 'rdp_phase' -and (Get-Field $_ 'phase') -eq 'disconnected' } |
    Group-Object { "$((Get-Field $_ 'code'))/$((Get-Field $_ 'extended_code'))" } | Sort-Object Count -Descending | ForEach-Object {
        [pscustomobject]@{ CodeExtended = $_.Name; Count = $_.Count }
    })
$shapeEvents = @($perfEvents | Where-Object Event -eq 'rdp_shape')
$rdpNetwork = [ordered]@{
    Summaries = $networkSummaries.Count
    RttMs = Get-NumberSummary -Values @($networkLines | ForEach-Object { [long](Get-Field $_ 'rtt_ms') })
    BandwidthKbps = Get-NumberSummary -Values @($networkLines | ForEach-Object { [long](Get-Field $_ 'bandwidth_kbps') })
    QualityLevels = @($networkLines | Group-Object { (Get-Field $_ 'quality') } | Sort-Object Name | ForEach-Object {
        [pscustomobject]@{ Quality = $_.Name; Lines = $_.Count }
    })
    ReconnectAttempts = @($reconnectEvents | Where-Object { (Get-Field $_ 'phase') -eq 'attempt' }).Count
    Reconnects = @($reconnectEvents | Where-Object { (Get-Field $_ 'phase') -eq 'reconnected' }).Count
    OutageMs = Get-NumberSummary -Values @($reconnectEvents | Where-Object { (Get-Field $_ 'phase') -eq 'reconnected' } | ForEach-Object { [long](Get-Field $_ 'outage_ms') })
    DisconnectCodes = $disconnectCodes
    AppliedShapes = @($shapeEvents | Group-Object { "net_type=$((Get-Field $_ 'net_type')) bw_detect=$((Get-Field $_ 'bw_detect')) color_depth=$((Get-Field $_ 'color_depth')) perf_flags=$((Get-Field $_ 'perf_flags')) perf_requested=$((Get-Field $_ 'perf_flags_requested')) compress=$((Get-Field $_ 'compress')) bitmap_persist=$((Get-Field $_ 'bitmap_persist'))" } |
        Sort-Object Count -Descending | ForEach-Object { [pscustomobject]@{ Shape = $_.Name; Count = $_.Count } })
}
$heartbeatEvents = @($perfEvents | Where-Object Event -eq 'heartbeat')
$latestHeartbeat = if ($heartbeatEvents.Count) { $heartbeatEvents[-1].Fields } else { $null }
$engineInventoryEvent = $perfEvents | Where-Object Event -eq 'rdp_engine_inventory' | Select-Object -Last 1

$report = [ordered]@{
    LogFile = [IO.Path]::GetFileName($LogPath)
    SizeKiB = [Math]::Round((Get-Item -LiteralPath $LogPath).Length / 1KB, 1)
    Lines = $lines.Count
    AppSessions = $appSessions.Count
    Severity = [ordered]@{ Warnings = $warningCount; Errors = $errorCount }
    Startup = $startup
    ConnectionSaves = [ordered]@{
        Total = $saveEvents.Count
        Failed = @($saveEvents | Where-Object { $_.Fields.outcome -eq 'failed' }).Count
        Timing = Get-NumberSummary -Values @($saveEvents | ForEach-Object { [long]$_.Fields.duration_ms })
    }
    RdpPhases = $rdpTimings
    RdpNetwork = $rdpNetwork
    UiStalls = @($perfEvents | Where-Object { $_.Event -eq 'ui_stall' -and $_.Fields.state -eq 'detected' }).Count
    SafeExceptions = @($perfEvents | Where-Object Event -eq 'exception').Count
    LatestHeartbeat = $latestHeartbeat
    EngineInventory = if ($engineInventoryEvent) { $engineInventoryEvent.Fields } else { $null }
}

if ($AsJson) {
    $report | ConvertTo-Json -Depth 8
} else {
    [pscustomobject]$report
}

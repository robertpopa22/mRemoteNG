$asm = [System.Reflection.Assembly]::LoadFrom("D:/github/mRemoteNG/mRemoteNG/bin/x64/Release/Interop.MSTSCLib.dll")

# Check AdvancedSettings7 for SpanMonitors
$type7 = $asm.GetTypes() | Where-Object { $_.Name -eq "IMsRdpClientAdvancedSettings7" }
if ($type7) {
    Write-Host "=== IMsRdpClientAdvancedSettings7 members matching Span/Multimon/Monitor ==="
    $type7.GetMembers() | Where-Object { $_.Name -match "Span|Multimon|Monitor" } | ForEach-Object { Write-Host "  $($_.Name)" }

    Write-Host ""
    Write-Host "=== All IMsRdpClientAdvancedSettings7 members ==="
    $type7.GetMembers() | Select-Object -ExpandProperty Name | Sort-Object
}

# Check NonScriptable interfaces for multimon
Write-Host ""
Write-Host "=== NonScriptable multimon-related ==="
$ns = $asm.GetTypes() | Where-Object { $_.Name -like "*NonScriptable*" }
foreach ($t in $ns) {
    $members = $t.GetMembers() | Where-Object { $_.Name -match "Span|Multimon|Monitor|UseAll" }
    if ($members) {
        Write-Host $t.FullName
        $members | ForEach-Object { Write-Host "  $($_.Name)" }
    }
}

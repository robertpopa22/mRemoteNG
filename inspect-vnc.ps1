Add-Type -Path 'C:/Users/robert.popa/.nuget/packages/vncsharpcore/1.2.1/lib/net6.0-windows7.0/VncSharpCore.dll'
$asm = [System.Reflection.Assembly]::LoadFrom('C:/Users/robert.popa/.nuget/packages/vncsharpcore/1.2.1/lib/net6.0-windows7.0/VncSharpCore.dll')
$t = $asm.GetType('VncSharpCore.RemoteDesktop')
if ($null -eq $t) {
    Write-Output "Type not found. Available types:"
    foreach ($type in $asm.GetExportedTypes()) {
        Write-Output $type.FullName
    }
    exit
}
Write-Output "=== Properties ==="
foreach ($p in $t.GetProperties()) {
    Write-Output "$($p.PropertyType.Name) $($p.Name)"
}
Write-Output "=== Public Methods (declared) ==="
foreach ($m in $t.GetMethods()) {
    if ($m.DeclaringType -eq $t) {
        $params = ($m.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', '
        Write-Output "$($m.ReturnType.Name) $($m.Name)($params)"
    }
}

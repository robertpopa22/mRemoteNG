$dll = 'C:/Users/robert.popa/.nuget/packages/vncsharpcore/1.2.1/lib/net6.0-windows7.0/VncSharpCore.dll'
$bytes = [System.IO.File]::ReadAllBytes($dll)
$asm = [System.Reflection.Assembly]::Load($bytes)
foreach ($t in $asm.GetExportedTypes()) {
    Write-Host ("TYPE: " + $t.FullName)
    foreach ($m in $t.GetMethods([System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::DeclaredOnly)) {
        $parms = ($m.GetParameters() | ForEach-Object { $_.ParameterType.Name + " " + $_.Name }) -join ", "
        Write-Host ("  METHOD: " + $m.Name + "(" + $parms + ")")
    }
    foreach ($p in $t.GetProperties([System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::DeclaredOnly)) {
        Write-Host ("  PROP: " + $p.PropertyType.Name + " " + $p.Name)
    }
    foreach ($e in $t.GetEvents([System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::DeclaredOnly)) {
        Write-Host ("  EVENT: " + $e.Name)
    }
}

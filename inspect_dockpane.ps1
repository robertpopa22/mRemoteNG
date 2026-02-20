$dll = Join-Path $env:USERPROFILE '.nuget\packages\dockpanelsuite\3.1.1\lib\netcoreapp3.1\WeifenLuo.WinFormsUI.Docking.dll'
Write-Host "DLL path: $dll"
Write-Host "Exists: $(Test-Path $dll)"
$asm = [System.Reflection.Assembly]::LoadFrom($dll)
Write-Host "Assembly: $($asm.FullName)"
$types = $asm.GetTypes() | Where-Object { $_.Name -eq 'DockPane' }
Write-Host "DockPane types found: $($types.Count)"
$type = $types[0]
Write-Host "Type: $($type.FullName)"
Write-Host ""
Write-Host "=== All public methods ==="
$type.GetMethods([System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Instance) | ForEach-Object {
    $params = $_.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }
    Write-Host "$($_.Name)($($params -join ', '))"
}

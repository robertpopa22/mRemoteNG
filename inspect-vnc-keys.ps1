Add-Type -Path 'C:/Users/robert.popa/.nuget/packages/vncsharpcore/1.2.1/lib/net6.0-windows7.0/VncSharpCore.dll'
$asm = [System.Reflection.Assembly]::LoadFrom('C:/Users/robert.popa/.nuget/packages/vncsharpcore/1.2.1/lib/net6.0-windows7.0/VncSharpCore.dll')
$t = $asm.GetType('VncSharpCore.SpecialKeys')

if ($null -ne $t) {
    Write-Output "=== VncSharpCore.SpecialKeys Enum Values ==="
    foreach ($name in [Enum]::GetNames($t)) {
        $val = [Enum]::Format($t, [Enum]::Parse($t, $name), "d")
        Write-Output "$name = $val"
    }
} else {
    Write-Output "Type VncSharpCore.SpecialKeys not found."
    Write-Output "Available types:"
    if ($asm) {
        foreach ($type in $asm.GetExportedTypes()) {
            Write-Output $type.FullName
        }
    }
}

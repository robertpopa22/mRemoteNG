$targetFile = "D:\github\mRemoteNG\mRemoteNG\obj\x64\Release\mRemoteNG.dll"
if (Test-Path $targetFile) {
    Write-Host "File exists, checking lock..."
    try {
        $stream = [System.IO.File]::Open($targetFile, 'Open', 'ReadWrite', 'None')
        $stream.Close()
        Write-Host "File is NOT locked"
    } catch {
        Write-Host "File IS LOCKED: $_"
    }
} else {
    Write-Host "File does not exist"
}

# Check for any dotnet/msbuild processes
Get-Process -Name dotnet,msbuild,VBCSCompiler,testhost -ErrorAction SilentlyContinue |
    Select-Object Id, ProcessName, StartTime | Format-Table -AutoSize

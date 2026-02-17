Get-ChildItem 'D:\github\mRemoteNG' -Directory -Recurse -Filter 'obj' | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "All obj dirs cleaned"

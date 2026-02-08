@echo off
setlocal EnableExtensions

set "PS_EXE=C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe"
set "NX_SCRIPT=D:\github\mRemoteNG\NEXTUP\scripts\nx.ps1"
"%PS_EXE%" -NoProfile -ExecutionPolicy Bypass -File "%NX_SCRIPT%" %*
exit /b %errorlevel%

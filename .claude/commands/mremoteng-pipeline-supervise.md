# /mremoteng-pipeline-supervise — Stop or restart the unattended pipeline

**Does:** check, stop, or restart `orchestrator_supervisor.py`.

**Does not:** triage an issue or replace `/mremoteng-fix-repo`. Daily work does not start here.

The host is Windows. Use PowerShell. Do not use `ps`, `kill`, `rm`, or `tail`.

## What to do

### Status

```powershell
python D:\github\mRemoteNG\.project-roadmap\scripts\orchestrator_supervisor.py --check
Get-CimInstance Win32_Process -Filter "Name = 'python.exe'" |
  Where-Object { $_.CommandLine -match "iis_orchestrator|orchestrator_supervisor" } |
  Select-Object ProcessId, CommandLine
```

### Stop

Stop those processes. Remove `.project-roadmap\scripts\orchestrator.lock` only after the processes are gone. If the user asked only to stop, finish here.

### Start

Run this only when the user asked to start the unattended pipeline:

```powershell
python D:\github\mRemoteNG\.project-roadmap\scripts\orchestrator_supervisor.py --orchestrator-args "<args>"
```

Start it in the background. Closing the agent session does not stop it. The log is `.project-roadmap\scripts\supervisor.log`. Read it with `Get-Content -Tail 40`.

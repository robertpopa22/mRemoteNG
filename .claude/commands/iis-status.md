# /iis-status — Show IIS Orchestrator Status

Show the current status of the IIS Orchestrator (running or last completed run).

## What to do

1. Read the status file:
   ```
   D:\github\mRemoteNG\.project-roadmap\scripts\orchestrator-status.json
   ```
2. Read the last 30 lines of the log file:
   ```
   D:\github\mRemoteNG\.project-roadmap\scripts\orchestrator.log
   ```
3. Present a summary to the user including:
   - **Running state**: Is the orchestrator currently active?
   - **Current phase**: issues / warnings / done
   - **Current task**: What file/issue is being processed right now
   - **Issues**: synced, triaged, implemented, failed, skipped (by reason)
   - **Warnings**: start count → current count (fixed count, % improvement)
   - **Commits**: List of commits made this session (hash + message)
   - **Errors**: Any errors encountered (file, step, description)
   - **Duration**: How long the session has been running
   - **Report time**: Current date and time when the status is generated

4. If the status file doesn't exist, inform the user that no orchestrator run has been recorded yet.

## Format

Present the status in a clean, readable format. Example:

```
IIS Orchestrator Status: RUNNING                          Report: 2026-02-19 21:45
Phase: warnings | Task: fixing ConnectionInfo.cs (15 warnings)

Warnings: 2302 → 2086 (-216 fixed, 9.4%)
  CS8618: 354 → 200  CS8602: 630 → 580

Commits: 2
  [OK] 3fb8ed18 chore: fix 216 nullable warnings in ConnectionContextMenu.cs
  [OK] a1b2c3d4 chore: fix 45 nullable warnings in ConnectionInfo.cs

Errors: 1
  [10:25] ProtocolBase.cs: build — cascade warnings increased

Duration: 00:15:23
```

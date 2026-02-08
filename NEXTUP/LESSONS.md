# Lessons Learned System

Last updated: 2026-02-08
Scope: `D:\github\mRemoteNG` modernization and release work.

## Goal

Create a persistent memory of what works, what fails, and the fastest known fix so we do not repeat the same errors.

## Operating Rules (Always-On)

1. Use explicit executable paths for core tools in this environment:
   - `C:\PROGRA~1\Git\cmd\git.exe`
   - `C:\PROGRA~1\GITHUB~1\gh.exe`
   - `C:\Windows\System32\cmd.exe`
   - `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe`
   - preferred wrapper: `D:\github\mRemoteNG\NEXTUP\scripts\nx.cmd`
2. Prefer single-step commands without nested quoting in `cmd /c`.
3. For CI status and logs, use authenticated `gh` instead of anonymous REST calls.
4. For this solution, validate with full MSBuild (not SDK-only build paths) when COMReference is in scope.
5. After each failure, immediately log:
   - command attempted
   - exact error pattern
   - root cause
   - proven fix
   - evidence (run URL / commit / file)

## Fast Fix Map

| Symptom | Root Cause | Immediate Fix |
| --- | --- | --- |
| `'git' is not recognized` | PATH not inherited in current command runner | Call `C:\PROGRA~1\Git\cmd\git.exe` directly |
| `'gh' is not recognized` | PATH not inherited | Call `C:\PROGRA~1\GITHUB~1\gh.exe` directly |
| `cmd /c` string treated as literal with quotes | Nested/escaped quote handling in wrapper | Avoid nested quotes; prefer plain arguments |
| `gh issue list --label \"Need 2 check\"` breaks in this wrapper | Escaped quotes are passed literally, so spaced labels are split | Use `--json ... --jq` label filtering and encode spaces as `\u0020` in jq strings (for example `Need\u00202\u0020check`) |
| GitHub REST polling returns rate-limit or inconsistent access | Unauthenticated requests | Use `gh api` / `gh run view` with authenticated session |
| `MSB4803` / COMReference failures with `dotnet build` | .NET Core MSBuild path cannot handle full Framework COM flow | Use full MSBuild path from VS Build Tools |

## Daily Loop

1. Run command.
2. If failure: log it with `scripts/log-command-feedback.ps1`.
3. Before trying a similar action: search prior lessons with `scripts/find-lesson.ps1`.
4. Reuse proven command patterns from the log.
5. Refresh metrics to prioritize recurring and costly errors:
   - `scripts/refresh-command-feedback-metrics.ps1`

## Prioritization Model

1. Frequency priority:
   - errors with highest repeat count are fixed first.
2. Time-loss priority:
   - categories/commands with highest `Lost(s)` are optimized next.
3. Combined priority:
   - if two items have similar frequency, fix the one with higher lost time first.

## Local Artifacts

- Rules: `D:\github\mRemoteNG\NEXTUP\LESSONS.md`
- Human log: `D:\github\mRemoteNG\NEXTUP\COMMAND_FEEDBACK_LOG.md`
- Machine log: `D:\github\mRemoteNG\NEXTUP\command-feedback.jsonl`
- Metrics: `D:\github\mRemoteNG\NEXTUP\COMMAND_FEEDBACK_METRICS.md`
- Scripts:
  - `D:\github\mRemoteNG\NEXTUP\scripts\nx.cmd`
  - `D:\github\mRemoteNG\NEXTUP\scripts\log-command-feedback.ps1`
  - `D:\github\mRemoteNG\NEXTUP\scripts\find-lesson.ps1`
  - `D:\github\mRemoteNG\NEXTUP\scripts\refresh-command-feedback-metrics.ps1`

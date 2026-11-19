---
name: mremoteng-pipeline
description: Inspect the mRemoteNG IIS issue and analyzer backlog, preview bounded batches, or handle an explicitly requested supervisor operation.
---

# Inspect the upstream backlog and IIS supervisor

Read [CLAUDE.md](../../../CLAUDE.md), [CHARTER.md](../../../CHARTER.md) and the
IIS section of the [developer guide](../../../.project-roadmap/DEVELOPER_GUIDE.md).
Use `/mremoteng-pipeline` in Claude Code or `$mremoteng-pipeline` in Codex
with the same arguments. Resolve paths from the repository root. For fork
issue work use [mremoteng-fix-repo](../mremoteng-fix-repo/SKILL.md). For product
verification use [mremoteng-verify](../mremoteng-verify/SKILL.md).

## Modes

| Argument | Action |
| --- | --- |
| `status` | Read status, logs, lock owners and process state. |
| No arguments | Report the cached backlog and current status. |
| `quick` | Sync issue metadata, analyze it and print a report. This updates the local issue DB. |
| `issues\|warnings\|analyzer-warnings\|all [--max-issues N] [--max-files N] --dry-run` | Preview from cached issue data or existing analyzer evidence. State which requested bounds apply and which data is missing. |
| `issues\|warnings\|analyzer-warnings\|all` without `--dry-run`, or `start "<orchestrator args>"` | Execution request. Check runtime readiness below before launching. |
| `stop` | Stop only the identified supervisor and orchestrator from the requested run. |

## Read status and prepare a preview

From PowerShell, read the existing files under `.project-roadmap/scripts/`:
`orchestrator-status.json`, the tails of `orchestrator.log` and
`supervisor.log`, and the PID metadata in `orchestrator.lock` and
`supervisor.lock`. Compare them to live process IDs and creation times. Report
missing or stale evidence and duplicate processes without changing them.

```powershell
Get-CimInstance Win32_Process -Filter "Name = 'python.exe' OR Name = 'pythonw.exe'" |
  Where-Object { $_.CommandLine -match '(iis_orchestrator|orchestrator_supervisor)\.py' } |
  Select-Object ProcessId, ParentProcessId, CreationDate, Name
```

For cached counts use the issue DB, or use the IIS reporting commands when an
operational log is acceptable. These Python entries open the IIS log. Neither
needs a test preflight or agent. Include unfinished maintainer actions and
label the cache date. For warning previews, use the most recent matching
build log and identify its revision. If there is no matching log, report the gap.

```powershell
python .project-roadmap/scripts/iis_orchestrator.py analyze --repos upstream --show-all
python .project-roadmap/scripts/iis_orchestrator.py report --include-all --no-save
```

For `quick`, check for an active writer and pre-existing edits to the issue DB
before running `iis_orchestrator.py sync`. Preserve local edits and use the
cached report if a sync would overwrite them. For an explicit `stop`, verify
the run ownership, stop its supervisor before its child can be restarted,
verify both PIDs exited, and clear only that run's proven stale locks when
needed. Never treat a stale status file as proof of a running process.

## Runtime readiness

The current implementation does not meet the charter for automated writes.
Report the blocker for the requested run using the matching source:

- [The supervisor](../../../.project-roadmap/scripts/orchestrator_supervisor.py)
  implements `--check` as health check with recovery. Recovery can stop
  processes and change Git state. Use the status procedure for observation.
- [IIS](../../../.project-roadmap/scripts/iis_orchestrator.py) calls test
  hygiene before and after `issues` and `all`, including `--dry-run`. Hygiene
  can run agents and commit. Use the preview procedure above.
- Legacy IIS can select a provider excluded by D12, works against a fixed
  checkout, stages whole source directories and can publish through Git.
  Selecting an allowed provider or finding a clean Git status alone leaves
  those risks in place. Preserve WIP without stashing it to launch this runner.

Resume unattended execution only after the owning runtime proves the requested
mode uses approved providers and explicit models, isolates implementation,
preserves WIP and stages exact paths, follows the required verification and
publication gates, and respects D14 when waiting for an active agent. Verify
the actual configuration and a bounded isolated run before suggesting
supervised operation. A phantom or zero-test result leaves verification incomplete.

For authorized runtime maintenance, locate the affected symbol and reproduce
the orchestration defect through a controlled fixture or mocked boundary. An
unbounded live batch is outside that verification. Report phase, queue scope,
current or last task, exact failure or blocker and evidence location.

**#ponytail.** Fix the cause once in the shared path, reusing existing runner
logic. Cover any changed argument, caller and state transition with the smallest
meaningful check and keep required safety and verification gates. Use
`#ponytail` in implementation briefs. Record known shortcuts with `shortcut:`
and their replacement condition, and track follow-up in the project TODO. End
a response after code changes with `#ponytail:` and the omitted scope,
unverified checks and relevant risk.

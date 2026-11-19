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
| `status` | Read IIS status and the supervisor's health findings. |
| No arguments | Report the cached backlog and current status. |
| `quick` | Sync issue metadata, analyze it and print a report. This updates the local issue DB. |
| `issues\|warnings\|analyzer-warnings\|all [--max-issues N] [--max-files N] --dry-run` | Preview cached issue data or a supplied build log without starting a worker. |
| `issues\|warnings\|analyzer-warnings --candidate-root <path>` | Prepare or verify one candidate after the checks below. |
| `start "<orchestrator args>"` or an unattended write batch | Legacy supervisor and write batches remain disabled. |
| `stop` | Stop only the identified supervisor and orchestrator from the requested run. |

## Read status and prepare a preview

Use the read-only entry points. A health finding exits nonzero and preserves
the lock and other evidence. For a warning preview pass the existing build
log and its source SHA. A missing log or revision leaves the preview incomplete.

```powershell
python .project-roadmap/scripts/iis_orchestrator.py status
python .project-roadmap/scripts/orchestrator_supervisor.py --check
python .project-roadmap/scripts/iis_orchestrator.py issues --dry-run --max-issues 2
python .project-roadmap/scripts/iis_orchestrator.py analyzer-warnings --dry-run --warning-log $buildLog --warning-revision $baseSha --max-files 2
```

For a detailed cached report use the IIS reporting commands below. Include
unfinished maintainer actions and the cache date.

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

## Prepare and verify one isolated candidate

Use a clean linked worktree detached at the orchestrator's current SHA,
with no local post-build hook. The current implementation route is Codex
in its workspace sandbox, with explicit model and effort. Verify subscription
scope and actual provider/model evidence in a limited native probe before
using this mode with real issue data. Use `--issue` to select an exact cached
issue or provide a revision-matched build log for one warning file.

```powershell
python .project-roadmap/scripts/iis_orchestrator.py issues --issue $issueNumber --candidate-root $candidateRoot --codex-model $model --agent-effort $effort
python .project-roadmap/scripts/iis_orchestrator.py issues --candidate-root $candidateRoot --candidate-digest $candidateHash --review-note $diffReview
```

The first command records a claim and writes the candidate patch and state to
the linked worktree's Git directory. A prior claim requires reconciliation
instead of redispatch. Inspect the exact patch before the second command.
Verification reruns the tripwire, full build and canonical suite against
the reviewed hash. The resulting state is `awaiting-required-verification`.
Follow [mremoteng-verify](../mremoteng-verify/SKILL.md) for the remaining review
and applicable lab and scope-based checks before integration. These commands
leave commit, push and public replies to the coordinator.

Resume supervised or unattended batches only after the applicable checkpoint
in [the runtime plan](../../../docs/PLAN-MRNGRUN-2026-11.md) has evidence.
A phantom, crashed or zero-test result leaves verification incomplete.

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

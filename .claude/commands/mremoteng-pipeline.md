# /mremoteng-pipeline — Unattended batch over the upstream backlog: run, status, stop, start

**Use when:** you want the headless orchestrator (`iis_orchestrator.py`, which calls `claude -p` /
Codex / Gemini as sub-agents) to sweep many issues or analyzer warnings without a human in the loop,
or you want to see, stop or restart such a run.

**Not for:** answering or fixing a fork issue. That is `/mremoteng-fix-repo` (interactive, with
counter-opinions, UI check and a confirmation gate before push). Not for a quality check of the
tree: that is `/mremoteng-verify`.

Difference in one line: `fix-repo` = a human-supervised session on the fork's open issues;
`pipeline` = an unattended batch, mostly over the ~800 upstream issues and warnings, that commits
and pushes on its own.

## Usage

- `/mremoteng-pipeline` — sync → analyze → report → orchestrate issues → report
- `/mremoteng-pipeline quick` — sync + analyze + report, no AI fixing
- `/mremoteng-pipeline issues|warnings|all [--max-issues N] [--max-files N] [--dry-run]`
- `/mremoteng-pipeline status` — read only: is it running, where it is, last errors
- `/mremoteng-pipeline stop` — stop the supervisor and orchestrator
- `/mremoteng-pipeline start "<orchestrator args>"` — start under the supervisor, detached

## Run

Issues we opened (including an auto-submitted crash report with no comments) are in scope. Do not
drop one because the author is us. A `wontfix` announcement stays out.

1. **Guard the working tree.** The orchestrator commits with
   `git add -- mRemoteNG/ mRemoteNGTests/ mRemoteNGSpecs/`, so any untracked or modified file there
   that is not the run's work gets swept into its commit. Stash it first
   (`git stash push -u -m pipeline-hold -- <path>`) and pop it at the end.
2. `python .project-roadmap/scripts/iis_orchestrator.py sync`
3. `python .project-roadmap/scripts/iis_orchestrator.py analyze` — keep the summary and the
   MAINTAINER ACTIONS block.
4. `python .project-roadmap/scripts/iis_orchestrator.py report --include-all` — note the stats.
5. Skip in `quick` mode. Dry-run first:
   `python .project-roadmap/scripts/iis_orchestrator.py issues --dry-run`.
   If its test-hygiene step reports `PHANTOM` (tests did not run), stop: every fix would be
   unverified. Fix the runner, then continue.
6. Real run, in the background:
   `python .project-roadmap/scripts/iis_orchestrator.py <issues|warnings|all> [args]`.
   Follow `orchestrator-status.json`; you are notified when it exits.
7. `report --include-all` again and compare with step 4.

Present: issues synced and waiting-for-us count; triaged / implemented / wontfix / duplicate /
needs_info / failed; report path; commits (`git log origin/main@{1}..origin/main`); errors;
duration; delta against step 4.

## Status (read only)

```powershell
Get-Content D:\github\mRemoteNG\.project-roadmap\scripts\orchestrator-status.json
Get-Content D:\github\mRemoteNG\.project-roadmap\scripts\orchestrator.log -Tail 30
python D:\github\mRemoteNG\.project-roadmap\scripts\orchestrator_supervisor.py --check
Get-CimInstance Win32_Process -Filter "Name = 'python.exe'" |
  Where-Object { $_.CommandLine -match "iis_orchestrator|orchestrator_supervisor" } |
  Select-Object ProcessId, CreationDate, CommandLine
```

Expect at most one supervisor and one orchestrator. A second one is an orphan; name it. A stale
`orchestrator.lock` with no process means the last run died. For progress over all issues use
`iis_orchestrator.py analyze`, not a hand-rolled count.

## Stop

Stop the processes listed above. Remove `.project-roadmap\scripts\orchestrator.lock` only after
they are gone.

## Start

Only when asked:

```powershell
python D:\github\mRemoteNG\.project-roadmap\scripts\orchestrator_supervisor.py --orchestrator-args "<args>"
```

Run it detached; closing the session does not stop it. Log: `.project-roadmap\scripts\supervisor.log`.

## Editing the script

When a run exposes a defect in `iis_orchestrator.py`, fix it like any other code: find the symbol
by name (not a line number from an old note), read the tail of `orchestrator.log`, change only
what the run proved wrong, show the diff, and do not launch the orchestrator to test it.

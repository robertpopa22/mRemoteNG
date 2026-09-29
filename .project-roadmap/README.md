# .project-roadmap

Working state for the fork: the issue database, the fork-network radar, the orchestrator, and the
plans and reports they produce. Nothing here ships to users.

Two documents outrank anything in this folder. [../CHARTER.md](../CHARTER.md) holds what the project
is for, which rules cannot be bent, and the log of decisions with their reasoning.
[../CLAUDE.md](../CLAUDE.md) holds the build, test and workflow rules. Read those first; this folder
is where the work in progress lives.

## Current

| | |
|---|---|
| Version | 1.84.0 (stable, published 2026-09-25) |
| Current plan | [CATCH-UP-PLAN-2026-09-29.md](CATCH-UP-PLAN-2026-09-29.md) |
| Why that plan | [UPSTREAM-ALIGNMENT-2026-09-16.md](UPSTREAM-ALIGNMENT-2026-09-16.md) |

## Files

| File | Purpose |
|------|---------|
| `EXECUTION-PLAN-2026-09.md` | The workstreams in flight, each with a checkable "done when" |
| `CATCH-UP-PLAN-2026-09-29.md` | Current request catch-up plan and execution checkpoint |
| `UPSTREAM-ALIGNMENT-2026-09-16.md` | The intelligence the plan was derived from — our state, upstream's direction, the divergence arithmetic |
| `LESSONS.md` | Build, test, CI and release lessons. Ageing; trust it less the older the entry |
| `DEVELOPER_GUIDE.md` | Orchestrator operations, release checklists, issue tracking |
| `RESPONSE-REVIEW-2026-09-29.md` | Monthly request review, outstanding actions and unsent reply drafts |
| `../docs/ISSUE-RESPONSE-WORKFLOW.md` | Current response workflow and publication checks |
| `VERIFICATION_PLAN.md` | What the UI verification is allowed to do, and what it must not |
| `issues-db/` | The issue database, one JSON per issue, plus generated sync reports |
| `fork-intel/` | Fork-network radar. `EXCLUDE.json` is the memory of decisions and stays tracked; `db/` is a regenerable harvest cache and is deliberately **not** tracked (see charter D3) |

## Issue Intelligence System

```bash
python .project-roadmap/scripts/iis_orchestrator.py sync --repos fork     # pull issues and comments
python .project-roadmap/scripts/iis_orchestrator.py analyze --repos fork --waiting-only
python .project-roadmap/scripts/iis_orchestrator.py update --issue N --repo fork --status testing --notes "..."
```

## Fork radar

```bash
python .project-roadmap/fork-intel/fork_intel.py discover   # enumerate active forks
python .project-roadmap/fork-intel/fork_intel.py diverge    # compare against upstream
python .project-roadmap/fork-intel/fork_intel.py screen     # drop noise, flag security
python .project-roadmap/fork-intel/fork_intel.py triage     # one AI judgement per candidate
python .project-roadmap/fork-intel/fork_intel.py preapprove # the consensus gate (charter D6)
python .project-roadmap/fork-intel/fork_intel.py report     # ranked report + import queue
```

`preapprove` votes with independent model families, rotates the triage family out of its own
verdict, casts one ballot blind, and audits a sample of unanimous approvals. The reasoning is in
charter D6; do not change its quorum without reading that entry.

## Archives

`historical/` holds completed release cycles (v1.79.0, v1.80.0) and the 830-issue triage plan from
February 2026. Kept for provenance, not for guidance.

---

*This file used to open by telling every session to read `CURRENT_PLAN.md` first — a file that had
been deleted — and to announce v1.81.0-beta.2 as current, two stable releases later. If you find
something here that is no longer true, fixing it is part of the task you are on.*

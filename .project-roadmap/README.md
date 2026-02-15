# .project-roadmap

Persistent workspace for mRemoteNG fork modernization.

## >>> CURRENT PLAN: [CURRENT_PLAN.md](CURRENT_PLAN.md) <<<

**Citeste INTOTDEAUNA `CURRENT_PLAN.md` la inceputul fiecarei sesiuni!**
Contine: ce s-a facut, unde am ramas, lectii critice, plan de executie.

## Current: v1.81.0-beta.2

Nullable warnings cleanup COMPLETED (2,338 → 0, 100%).
Branch: `main`
Release: https://github.com/robertpopa22/mRemoteNG/releases/tag/20260215-v1.81.0-beta.2-NB-(3396)

## Active Files

| File | Purpose |
|------|---------|
| **`CURRENT_PLAN.md`** | **PLANUL CURENT** — citeste PRIMUL! Warning resolution, progres, lectii |
| `LESSONS.md` | Build, test, CI, and release lessons learned |
| `ISSUE_BINARYFORMATTER.md` | Open blocker: DockPanelSuite BinaryFormatter on .NET 10 |
| `CVE-2023-30367_ASSESSMENT.md` | SecureString migration assessment — deferred to v1.81.0 |

## Scripts

| Script | Purpose |
|--------|---------|
| `scripts/Sync-Issues.ps1` | **MANDATORY** — Sync issues+comments from both repos |
| `scripts/Analyze-Issues.ps1` | Analyze what needs action |
| `scripts/Update-Status.ps1` | Transition issue lifecycle + post GitHub comments |
| `scripts/Generate-Report.ps1` | Generate markdown reports |
| `scripts/orchestrate.py` | **IIS Orchestrator** — automated issue resolution & warning cleanup |
| `scripts/find-lesson.ps1` | Search lessons by keyword |

## Archives

| Folder | Contents |
|--------|----------|
| `historical/v1.79.0/` | v1.79.0 release cycle (26 PRs, triage, execution logs, scripts) |
| `historical/v1.80.0/` | v1.80.0 code analysis & error backlog (12 items, all resolved) |
| `TRIAGE_PLAN_2026-02-10.md` | Historical — 830-issue triage plan for v1.80.0 (completed) |

## Resume Workflow

1. **Read `CURRENT_PLAN.md`** — vezi unde am ramas si ce trebuie facut.
2. Read `LESSONS.md` before starting any build, test, CI, or release task.
3. Run `build.ps1` to verify clean build before making changes.
4. See `CODE_SIGNING_POLICY.md` at repo root for release signing policy.

# .project-roadmap Modernization Workspace

This folder is the persistent execution workspace for modernizing this fork to a stable, releasable version.

## Current Release Cycle

- **v1.79.0** — Released 2026-02-09 (26 PRs, 3 platforms)
- **v1.80.0** — In progress (self-contained build, security hardening, performance, features, docs)

## Goals

- Keep a single source of truth for plan, status, and issue triage.
- Track objective acceptance criteria before closing any phase.
- Record lessons learned to avoid repeating mistakes.

## Active Files

| File | Contents |
|------|----------|
| `LESSONS.md` | Durable rules from real failures and fixes (v1.79.0 + v1.80.0) |
| `ISSUE_PACKAGES.md` | Issue triage strategy and intervention packages |
| `P7_TEST_COVERAGE_ANALYSIS_2026-02-08.md` | Test coverage gaps analysis |
| `ANALYSIS_2026-02-09_SUPPLEMENT.md` | Supplementary static analysis & TODO review |
| `DEEP_DIVE_ANALYSIS_2026-02-09.md` | Deep dive analysis of concurrency, security, and SQL risks |
| `UPSTREAM_PR_PACKAGES_2026-02-07.md` | Catalog of all 26 upstream PRs |
| `UPSTREAM_ISSUES_SNAPSHOT_2026-02-09.md` | Upstream issue state snapshot |
| `ISSUE_BINARYFORMATTER.md` | .NET 10 BinaryFormatter crash analysis |
| `COMMAND_FEEDBACK_LOG.md` | Chronological feedback log of commands |

## Archived

Completed execution logs for v1.79.0 release cycle are in `historical/v1.79.0/`.

## Scripts

| Script | Purpose |
|--------|---------|
| `scripts/nx.cmd` | Command wrapper with PATH fixes |
| `scripts/log-command-feedback.ps1` | Log command failures |
| `scripts/find-lesson.ps1` | Search lessons by keyword |
| `scripts/refresh-command-feedback-metrics.ps1` | Refresh error metrics |
| `scripts/refresh-issues.ps1` | Fetch fresh issue snapshot from upstream |
| `scripts/refresh-p1-p5.ps1` | Generate P1-P5 package snapshots |

## Code Signing (MANDATORY for releases)

All release binaries MUST be signed via SignPath Foundation. The CI workflow (`Build_mR-NB.yml`) enforces this:
- Step 10c submits to SignPath — if it fails, the release step is skipped
- No unsigned binaries are ever published
- See `CODE_SIGNING_POLICY.md` at repo root for full policy

### Setup (one-time, already done):
1. Register at [signpath.org](https://signpath.org/) for free OSS certificate
2. Add GitHub secrets: `SIGNPATH_API_TOKEN`, `SIGNPATH_ORGANIZATION_ID`
3. Configure SignPath project: slug `mRemoteNG`, policy `release-signing`
4. Install SignPath GitHub App on the repository

## Resume Workflow

1. Read `LESSONS.md` before starting any build, test, CI, or release task.
2. Check the plan file in Claude Code session for current phase progress.
3. Run `build.ps1` to verify clean build before making changes.

## Command Learning Loop

1. Before repeating a risky command, search lessons:
   - `D:\github\mRemoteNG\.project-roadmap\scripts\nx.cmd lesson -Pattern "<keyword>"`
2. After each failed/partial command, log it:
   - `powershell -File D:\github\mRemoteNG\.project-roadmap\scripts\log-command-feedback.ps1 -Command "<cmd>" -Result FAIL -Resolution "<fix>" -Category "<cat>" -ErrorPattern "<error>" -DurationSeconds <sec>`

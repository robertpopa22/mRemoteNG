# .project-roadmap

Persistent workspace for mRemoteNG fork modernization.

## Current: v1.80.0

Self-contained builds, security hardening, performance, CI improvements.
Branch: `release/1.80`

## Active Files

| File | Purpose |
|------|---------|
| `LESSONS.md` | Build, test, CI, and release lessons learned |
| `ISSUE_BINARYFORMATTER.md` | Open blocker: DockPanelSuite BinaryFormatter on .NET 10 |

## Scripts

| Script | Purpose |
|--------|---------|
| `scripts/find-lesson.ps1` | Search lessons by keyword |
| `scripts/refresh-issues.ps1` | Fetch upstream issue snapshot |

## Archives

| Folder | Contents |
|--------|----------|
| `historical/v1.79.0/` | v1.79.0 release cycle (26 PRs, triage, execution logs, scripts) |
| `historical/v1.80.0/` | v1.80.0 code analysis & error backlog (12 items, all resolved) |

## Resume Workflow

1. Read `LESSONS.md` before starting any build, test, CI, or release task.
2. Run `build.ps1` to verify clean build before making changes.
3. See `CODE_SIGNING_POLICY.md` at repo root for release signing policy.

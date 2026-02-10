# Issue Intelligence System v1.0.0

**MANDATORY**: This system MUST be used for all issue tracking, triage, and release communication.
Do NOT manage issues manually — always use the scripts below for consistency.

## Overview

A git-tracked JSON database that syncs GitHub issues from both upstream (`mRemoteNG/mRemoteNG`) and fork (`robertpopa22/mRemoteNG`) repositories. Provides lifecycle tracking, iteration detection, automated GitHub comments, and markdown reports.

**Why JSON instead of SQLite?** JSON files are git-friendly (diffs, merges, code review), human-readable, and natively supported by PowerShell and `gh` CLI. SQLite is binary — no diffs, unresolvable merge conflicts, and invisible in PRs.

## Quick Start

```powershell
# 1. Sync issues from both repos (MANDATORY before any triage/release)
.\.project-roadmap\scripts\Sync-Issues.ps1

# 2. Analyze what needs attention
.\.project-roadmap\scripts\Analyze-Issues.ps1

# 3. Update an issue's status (with optional GitHub comment)
.\.project-roadmap\scripts\Update-Status.ps1 -Issue 3044 -Status in-progress -Description "Working on .cmd batch fix"

# 4. Generate markdown report
.\.project-roadmap\scripts\Generate-Report.ps1
```

## Directory Structure

```
issues-db/
├── _meta.json         # Sync metadata: last run time, stats, config
├── _roadmap.json      # Prioritized items for next release
├── upstream/          # Issues from mRemoteNG/mRemoteNG
│   ├── 0822.json
│   ├── 3044.json      # Example: iteration loop issue
│   └── ...
├── fork/              # Issues from robertpopa22/mRemoteNG
│   └── ...
├── reports/           # Generated markdown reports
│   └── 2026-02-10_sync.md
└── README.md          # This file
```

## Issue Lifecycle

```
new → triaged → roadmap → in-progress → testing → released
                               ↑              |
                               └──────────────┘
                          user-feedback → re-fix (iteration loop)
```

### Lifecycle States

| State | Meaning | GitHub Comment |
|-------|---------|----------------|
| `new` | Just synced, not yet reviewed | None |
| `triaged` | Reviewed, priority assigned | None |
| `roadmap` | Accepted for next release | "Thank you! Added to roadmap." |
| `in-progress` | Actively being worked on | "Fix is being worked on." |
| `testing` | PR merged, in validation | "Fix merged, in testing. PR: {url}" |
| `released` | Shipped in a release | "Available in {version}. Download: {url}" |
| `wontfix` | Decided not to fix | Custom message |
| `duplicate` | Duplicate of another issue | Custom message |

### Iteration Loop (the #3044 pattern)

When a user reports that our fix didn't fully resolve the issue:

1. `testing` → `in-progress` (iteration-reopen)
2. Script records the feedback in `iterations[]`
3. GitHub comment acknowledges the feedback
4. Work resumes on the issue
5. When re-fixed: `in-progress` → `testing` → `released`

Each iteration is tracked with sequence numbers, dates, and descriptions.

## Scripts Reference

### Sync-Issues.ps1
Fetches issues and comments from GitHub. **Run this FIRST, every session.**

```powershell
# Full sync (both repos)
.\Sync-Issues.ps1

# Upstream only
.\Sync-Issues.ps1 -Repos upstream

# Specific issues (fast)
.\Sync-Issues.ps1 -IssueNumbers 3044,3069

# Include closed issues
.\Sync-Issues.ps1 -IncludeClosed
```

### Analyze-Issues.ps1
Reviews synced data, identifies what needs attention.

```powershell
# Show actionable items
.\Analyze-Issues.ps1

# Show only items waiting for our response
.\Analyze-Issues.ps1 -WaitingOnly

# Show everything
.\Analyze-Issues.ps1 -ShowAll

# Filter by priority or status
.\Analyze-Issues.ps1 -Priority P2-bug
.\Analyze-Issues.ps1 -Status in-progress
```

### Update-Status.ps1
Transitions issues through the lifecycle.

```powershell
# Triage
.\Update-Status.ps1 -Issue 3044 -Status triaged -Priority P2-bug

# Start work
.\Update-Status.ps1 -Issue 3044 -Status in-progress -Branch "fix/3044-comma-split"

# Testing (with PR)
.\Update-Status.ps1 -Issue 3044 -Status testing -PR 3150

# Iteration: user says it's not fixed
.\Update-Status.ps1 -Issue 3044 -Status in-progress -Description "User feedback: .cmd still broken"

# Release (with GitHub comment)
.\Update-Status.ps1 -Issue 3044 -Status released -Release "v1.80.0" -ReleaseUrl "https://..." -PostComment

# Add to roadmap
.\Update-Status.ps1 -Issue 3044 -Status roadmap -AddToRoadmap
```

**-PostComment** flag posts the templated message to GitHub. Without it, the script shows a preview only.

### Generate-Report.ps1
Creates markdown reports aggregating all tracked issues.

```powershell
# Standard report
.\Generate-Report.ps1

# Full inventory
.\Generate-Report.ps1 -IncludeAll

# Console only (no file)
.\Generate-Report.ps1 -NoSave
```

## Per-Issue JSON Schema

```json
{
  "number": 3044,
  "repo": "mRemoteNG/mRemoteNG",
  "title": "Issue title",
  "state": "open",
  "labels": ["Bug", "1.78.*"],
  "author": "username",
  "created_at": "2025-12-11T22:41:50Z",
  "github_updated_at": "2026-02-10T06:04:01Z",
  "body_snippet": "First 500 chars of issue body...",

  "our_status": "in-progress",
  "priority": "P2-bug",
  "target_release": "v1.80.0",
  "our_branch": "fix/3044-comma-split",
  "our_pr": 3109,

  "iterations": [
    {
      "seq": 1,
      "date": "2026-02-07",
      "type": "fix",
      "description": "PR #3109 — escaping fixes",
      "pr": 3109,
      "branch": null,
      "release": null,
      "comment_posted": true
    }
  ],

  "comments": [
    {
      "id": 12345,
      "author": "user",
      "date": "2026-02-09T19:22:10Z",
      "snippet": "First 500 chars...",
      "is_ours": false,
      "analyzed": true,
      "action_needed": true
    }
  ],

  "comments_cursor": "2026-02-10T06:04:01Z",
  "unread_comments": 0,
  "needs_action": true,
  "waiting_for_us": false,
  "last_synced": "2026-02-10T12:00:00Z",
  "notes": "Free-form notes"
}
```

## Workflow: Standard Session

1. **`Sync-Issues.ps1`** — Fetch latest from GitHub
2. **`Analyze-Issues.ps1`** — See what needs attention
3. **`Update-Status.ps1`** — Triage new issues, update in-progress items
4. **Work on code** — Fix bugs, implement features
5. **`Update-Status.ps1 -Status testing`** — Mark as testing after PR merge
6. **`Generate-Report.ps1`** — Generate report for the session
7. **Commit the JSON changes** — `git add .project-roadmap/issues-db/ && git commit`

## Workflow: Release

1. **`Sync-Issues.ps1`** — Final sync before release
2. **`Update-Status.ps1 -Status released`** for each resolved issue
3. **Use `-PostComment`** to notify users on GitHub
4. **`Generate-Report.ps1 -IncludeAll`** — Full inventory for release notes
5. **Commit everything** — JSON updates are part of the release commit

## Rules

1. **ALWAYS run Sync-Issues.ps1 before triage or release** — stale data = missed comments
2. **ALWAYS use Update-Status.ps1 for transitions** — maintains iteration history
3. **ALWAYS commit JSON changes** — this is git-tracked, not ephemeral
4. **NEVER edit JSON files manually** — use the scripts for consistency
5. **ALWAYS use -PostComment for released status** — users must be notified
6. **Track iterations** — if a user says "still broken", that's an iteration, not a new issue

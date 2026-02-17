# Issue Intelligence System v1.0.0

**MANDATORY**: This system MUST be used for all issue tracking, triage, and release communication.
Do NOT manage issues manually — always use the scripts below for consistency.

## Overview

A git-tracked JSON database that syncs GitHub issues from both upstream (`mRemoteNG/mRemoteNG`) and fork (`robertpopa22/mRemoteNG`) repositories. Provides lifecycle tracking, iteration detection, automated GitHub comments, and markdown reports.

**Why JSON instead of SQLite?** JSON files are git-friendly (diffs, merges, code review), human-readable, and natively supported by Python and `gh` CLI. SQLite is binary — no diffs, unresolvable merge conflicts, and invisible in PRs.

## Quick Start

```bash
# 1. Sync issues from both repos (MANDATORY before any triage/release)
python .project-roadmap/scripts/iis_orchestrator.py sync

# 2. Analyze what needs attention
python .project-roadmap/scripts/iis_orchestrator.py analyze

# 3. Update an issue's status (with optional GitHub comment)
python .project-roadmap/scripts/iis_orchestrator.py update --issue 3044 --status in-progress --description "Working on .cmd batch fix"

# 4. Generate markdown report
python .project-roadmap/scripts/iis_orchestrator.py report
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

scripts/
├── iis_orchestrator.py          # Main orchestrator (sync, triage, implement, report)
├── orchestrator.log             # Internal log (auto-flushed, ALWAYS read this)
├── orchestrator-status.json     # Machine-readable state
├── orchestrator.lock            # Single-instance lock (PID-based)
├── _agent_rate_limits.json      # Persistent agent rate-limit state
├── _comment_rate.json           # GitHub comment rate-limit state
├── chain-context/               # Per-session AI agent context files
│   └── 20260217_*.json          # Triage/implement results per issue
└── find-lesson.ps1              # Search LESSONS.md by keyword
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

All IIS functions are in a single Python script: `iis_orchestrator.py`.

### issues (AI-driven auto-fix mode)
Runs the full multi-agent orchestrator: sync → triage → implement → build → test → commit → notify.

```bash
# Process all open issues (612+)
python iis_orchestrator.py issues

# Limit to N issues
python iis_orchestrator.py issues --max-issues 50

# Use specific agent
python iis_orchestrator.py issues --agent gemini
```

**Agent chain:** Codex → Gemini → Claude (fallback order). Rate-limited agents are automatically skipped.

### sync
Fetches issues and comments from GitHub. **Run this FIRST, every session.**

```bash
# Full sync (both repos)
python iis_orchestrator.py sync

# Upstream only
python iis_orchestrator.py sync --repos upstream

# Specific issues (fast)
python iis_orchestrator.py sync --issues 3044,3069

# Include closed issues
python iis_orchestrator.py sync --include-closed
```

### analyze
Reviews synced data, identifies what needs attention.

```bash
# Show actionable items
python iis_orchestrator.py analyze

# Show only items waiting for our response
python iis_orchestrator.py analyze --waiting-only

# Show everything
python iis_orchestrator.py analyze --show-all

# Filter by priority or status
python iis_orchestrator.py analyze --priority P2-bug
python iis_orchestrator.py analyze --status in-progress
```

### update
Transitions issues through the lifecycle.

```bash
# Triage
python iis_orchestrator.py update --issue 3044 --status triaged --priority P2-bug

# Start work
python iis_orchestrator.py update --issue 3044 --status in-progress --branch "fix/3044-comma-split"

# Testing (with PR)
python iis_orchestrator.py update --issue 3044 --status testing --pr 3150

# Iteration: user says it's not fixed
python iis_orchestrator.py update --issue 3044 --status in-progress --description "User feedback: .cmd still broken"

# Release (with GitHub comment)
python iis_orchestrator.py update --issue 3044 --status released --release "v1.80.0" --release-url "https://..." --post-comment

# Add to roadmap
python iis_orchestrator.py update --issue 3044 --status roadmap --add-to-roadmap
```

**--post-comment** flag posts the templated message to GitHub. Without it, the script shows a preview only.

### report
Creates markdown reports aggregating all tracked issues.

```bash
# Standard report
python iis_orchestrator.py report

# Full inventory
python iis_orchestrator.py report --include-all

# Console only (no file)
python iis_orchestrator.py report --no-save
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

1. **`iis_orchestrator.py sync`** — Fetch latest from GitHub
2. **`iis_orchestrator.py analyze`** — See what needs attention
3. **`iis_orchestrator.py update`** — Triage new issues, update in-progress items
4. **Work on code** — Fix bugs, implement features
5. **Run tests** — Build + `dotnet test` must pass
6. **Commit per issue** — Each issue fix gets its own commit immediately after tests pass (see Rule 7)
7. **`iis_orchestrator.py update --issue N --status testing`** — Mark as testing after PR merge
8. **`iis_orchestrator.py report`** — Generate report for the session
9. **Commit the JSON changes** — `git add .project-roadmap/issues-db/ && git commit`

## Workflow: Release

1. **`iis_orchestrator.py sync`** — Final sync before release
2. **`iis_orchestrator.py update --issue N --status released --post-comment`** for each resolved issue
3. **Use `--post-comment`** to notify users on GitHub
4. **`iis_orchestrator.py report --include-all`** — Full inventory for release notes
5. **Commit everything** — JSON updates are part of the release commit

## Agent Rate-Limit Management

The orchestrator automatically detects and persists rate limits for each AI agent. State is stored in `_agent_rate_limits.json` and survives across orchestrator restarts.

### How it works
1. When an agent returns a rate-limit error, the orchestrator parses the reset date
2. The agent is marked as rate-limited in `_agent_rate_limits.json`
3. On subsequent calls, `_agent_dispatch()` checks the rate state and skips the agent instantly
4. When the reset date passes, the agent is automatically re-enabled

### Manual management
```bash
# Check current rate limits
cat .project-roadmap/scripts/_agent_rate_limits.json

# Clear a rate limit manually (e.g., after upgrading API plan)
python -c "import json; d=json.load(open('.project-roadmap/scripts/_agent_rate_limits.json')); del d['codex']; json.dump(d, open('.project-roadmap/scripts/_agent_rate_limits.json','w'), indent=2)"
```

### Agent configuration
| Agent | CLI Tool | Sandbox | Approval | Model |
|-------|----------|---------|----------|-------|
| Codex | `codex exec` | `workspace-write` | `never` (auto-approve) | `gpt-5.3-codex` |
| Gemini | `gemini` | N/A (native) | `-y` (auto-approve) | `gemini-3-pro-preview` |
| Claude | `claude -p` | N/A (native) | `--dangerously-skip-permissions` | default |

## Rules

1. **ALWAYS run `iis_orchestrator.py sync` before triage or release** — stale data = missed comments
2. **ALWAYS use `iis_orchestrator.py update` for transitions** — maintains iteration history
3. **ALWAYS commit JSON changes** — this is git-tracked, not ephemeral
4. **NEVER edit JSON files manually** — use the scripts for consistency
5. **ALWAYS use `--post-comment` for released status** — users must be notified
6. **Track iterations** — if a user says "still broken", that's an iteration, not a new issue
7. **COMMIT PER ISSUE after tests pass** — After fixing an issue, run the full build + test suite. If all tests pass, create a commit immediately for that issue before moving to the next one. Commit message format: `fix(#NNNN): short description`. This ensures each fix is atomic, bisectable, and individually revertable. Never batch multiple issue fixes into a single commit.

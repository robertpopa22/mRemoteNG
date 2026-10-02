# Best practices

**Does:** index the numbered lessons in [docs/bp/](bp/). A lesson is an incident plus the reusable rule that follows from it.

**Does not:** hold policy (that is [CHARTER.md](../CHARTER.md)), build or test commands (that is [CLAUDE.md](../CLAUDE.md)), the log field contract (that is [RUNTIME_DIAGNOSTICS.md](RUNTIME_DIAGNOSTICS.md)), a procedure (that is `.claude/commands/`), or anything about a maintainer's own machines.

Read the lesson. Do not copy it into the charter, the manual, or a runbook.

| ID | Lesson |
| --- | --- |
| [BP-001](bp/BP-001-runtime-evidence.md) | A real-use log that cannot separate a suspicion: retention, disconnect codes, XML reloads, UI stalls, and a missing `process_stop` |
| [BP-002](bp/BP-002-commit-messages.md) | A closing word next to `#n` closes the GitHub issue |
| [BP-003](bp/BP-003-dpi-child-fit.md) | A per-monitor DPI bounce can leave a child at the old size; moving it does not refit it |
| [BP-004](bp/BP-004-open-issue-disposition.md) | Every open issue is in the run queue; an ask or a named build that is already our last word is not posted again |

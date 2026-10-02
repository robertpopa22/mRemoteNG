# Best practices

**Does:** index the current lessons in [docs/bp/](bp/). A lesson is an incident. The rule it produced lives in the document named in the lesson, once.

**Does not:** hold policy (that is [CHARTER.md](../CHARTER.md)), build or test commands (that is [CLAUDE.md](../CLAUDE.md)), the log field contract (that is [RUNTIME_DIAGNOSTICS.md](RUNTIME_DIAGNOSTICS.md)), a procedure (that is `.claude/commands/`), or anything about a maintainer's own machines.

A lesson that no longer matches the charter is withdrawn, not kept beside the new one. The retired row names the replacement. It does not restate the old rule.

| ID | Ver | Updated | Lesson |
| --- | --- | --- | --- |
| [BP-001](bp/BP-001-runtime-evidence.md) | 1 | 2026-10-02 | A real-use log that cannot separate a suspicion. The directive is D9. |
| [BP-002](bp/BP-002-commit-messages.md) | 1 | 2026-10-02 | A closing word next to `#n` closes the GitHub issue. |
| [BP-003](bp/BP-003-dpi-child-fit.md) | 2 | 2026-10-02 | A per-monitor DPI bounce can leave a child, including a tool strip, at the old size. |
| [BP-005](bp/BP-005-premature-close.md) | 2 | 2026-10-02 | A contradicted close was ignored for a week. The open-list rule is D10. |
| [BP-006](bp/BP-006-round-trip.md) | 1 | 2026-10-02 | A save that returns success has not proved that the bytes changed. |
| [BP-007](bp/BP-007-driven-path.md) | 1 | 2026-10-02 | A UI claim counts only the path that was actually driven. |

## Retired

| ID | When | Replacement |
| --- | --- | --- |
| BP-004 | 2026-10-02 | D10 and [BP-005](bp/BP-005-premature-close.md) version 2. The file is gone. |

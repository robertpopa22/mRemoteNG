# BP-009 — A thanks on a closed issue is not a new report

**Version:** 1 · **Updated:** 2026-10-04

**Does:** record that a closed issue whose latest comment is not ours was treated as new work when the comment was already read.

**Does not:** restate the open-list rule. That is charter D10. The procedure is `.claude/commands/mremoteng-fix-repo.md`.

## Incident

On 2026-10-04 the queue listing included 25 closed issues because the latest stored comment was not ours. The comments were thanks, a confirmation, or a duplicate mark. Each was already analyzed, and none of those issues had been updated after 2026-07-27. The sync had re-read only closed issues updated since 2026-09-29. The only new closed record was our own crash close.

## Rule

A closed issue is work when the latest comment is newer than our last word and still asks for something we have not answered. A thanks, a confirmation, or a duplicate mark is not that comment. Every open issue is still in the run.

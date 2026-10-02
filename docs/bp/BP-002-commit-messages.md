# BP-002 — A closing word next to an issue number closes it

**Version:** 1 · **Updated:** 2026-10-02

**Does:** record that GitHub closes an issue when `fix`, `fixes`, `close`, `closes`, `resolve`, or `resolves` stands next to `#n`, even when the commit body says the issue stays open.

**Does not:** define reply policy or the star closer (those are charter D8 and [ISSUE-RESPONSE-WORKFLOW.md](../ISSUE-RESPONSE-WORKFLOW.md)), and it does not replace the commit steps in the fix-repo runbook.

## Incident

On 2026-10-01, commit `47c0893b5` used `fix(#182)`. GitHub closed #182. The commit body said the issue stayed open. The close still happened.

## Rule

A diagnostic commit, or any change that must leave the issue open, uses `diag(#n):` and does not put those words next to the number.

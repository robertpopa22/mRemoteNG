# BP-027 — A diagnostic collects nothing until the build in use contains it

**Version:** 1 · **Updated:** 2026-11-10

**Does:** record why an issue that waits on a recurrence must first compare the build that is running with the commit that added the diagnostic.

**Does not:** say how a running copy is refreshed, name the fields a build emits ([RUNTIME_DIAGNOSTICS.md](../RUNTIME_DIAGNOSTICS.md)), or restate the directive ([D9](../../CHARTER.md), evidence in [BP-001](BP-001-runtime-evidence.md)).

## Incident

#223 (RDP keyboard stops while the mouse still works) was observed on build `7a43a3cf29`. The `rdp_input` diagnostic was committed the next day (`0b89e0c86`) and published in a nightly. The issue then stayed open for thirteen days as "waiting for a recurrence with these diagnostics". On 2026-11-10 the log of a session in real use showed `process_start ... build_id=7a43a3cf29`: that copy still ran the build from before the diagnostic, so a recurrence there would have produced the same unreadable log as the first report. A second copy could not be read that day, so its build is unknown.

The session reports in between recorded "no `rdp_input` line" as a clean result. None of them recorded which build had written the log. For the copy that was read, "no line" meant "no instrumentation", not "no recurrence".

## Rule

Before an issue is left open to wait for a recurrence, read `build_id` on the last `process_start` of each copy in real use and check that the diagnostic's commit is an ancestor of it (`git merge-base --is-ancestor <diag commit> <build_id>`). If it is not, the state of the issue is "diagnostic not yet in the build in use", and the session report says so with the two commits. The absence of a diagnostic line is evidence only from a build that can write that line. A copy whose log could not be read counts as not instrumented until its `process_start` says otherwise.

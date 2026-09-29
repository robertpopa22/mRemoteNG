# Catch-up plan — September 29, 2026

Maintainer instruction: create a plan to bring everything up to date, then execute it.
Scope: mRemoteNG fork intake, active issue/PR disposition, response workflow and verification.
The [monthly review](RESPONSE-REVIEW-2026-09-29.md) is the evidence baseline. Existing September
architecture work remains in [its plan](EXECUTION-PLAN-2026-09.md).

## Completion contract

Every current request has a source-backed status, next owner/action and a completed reply where
one is needed. Feasible local defects are reproduced and addressed with tests; unreproduced
environment-specific reports get bounded diagnostic work rather than a guessed fix. Build,
tests, public-release status and reporter confirmation are separate. External publication,
vendor submissions and human security overrides require the concrete final action to be authorized.
"Up to date" does not turn an unresolved report into "fixed".

## Execution order

1. **Reconcile intake and evidence.** Sync fork/upstream, inspect recent closed feedback and PRs,
   separate fresh work from historical flags; preserve unanswered items. **Done:** 25 monthly fork
   issues, 27 PRs inventoried; #200/#196/#198 need replies; #199 is the only open PR. Full upstream
   backlog is monitored context, not a mandate to fix hundreds of upstream issues.
2. **Update the response system.** Canon, charter, evidence-based templates, completed-file and
   freshness checks, scoped analysis, regression tests. **Done locally:** 18 regression tests
   passed; the live fork queue shows 3 incoming replies and 25 historical closed records separately.
3. **Work the current defects.** #200 handle lifecycle first; #198 residual DPI layout next;
   #196 embedded Entra diagnostic boundary. Reproduce before editing product code. For #182/#197/
   #177/#165, verify shipped diagnostics, latest feedback and outstanding local work; respect
   attempt budgets and do not send premature repeat pings.
4. **Close maintainer-owned gaps.** Review PR #199 and repeated dependency-label warning; reconcile
   #192 signing/submission status and upstream #3514 without equating policy blocking with quarantine;
   retain the #179 UX proposal for human discussion. The verification build surfaced SSH.NET
   vulnerability warnings: investigate the exact advisories before deciding the bounded remedy.
5. **Verify and consolidate.** Run relevant regression checks and the required build, inspect the
   public diff, commit each verified scope without bypassing the security tripwire. Keep exact
   unsent replies and remaining external actions reviewable. Refresh this plan with evidence.

## Checkpoint — 11:44 Europe/Bucharest

- No public comments, issue closures, merges, vendor submissions or release publication performed.
- Working tree was clean at entry. Current changes are intake snapshots and this workflow update.
- First IIS regression run: 16 passed. Product build still running; new package advisories observed,
  not suppressed. No product code has been edited yet.

### 11:48 checkpoint

Build completed successfully (97.7 seconds), with existing analyzer warnings and two current
SSH.NET advisories. The repository's existing local post-build hook also deployed local build
3721 and preserved its 23 Settings files; it warned that an old backup could not be removed.
No release was published. The isolated lab baseline is running. No product code edited yet.

### 12:04 checkpoint

- Eight active problem reports have evidence-based local statuses and notes; fresh incoming
  requests remain unanswered until publication. The independent maintainer-action register is
  implemented and tested, so a promise cannot disappear merely because we spoke last.
- 20 IIS tests pass. The full product suite passed **7,323/7,323**, with no crashed groups (476s).
- UI baseline passed 4/4. The retained close-confirmation screenshot shows a complete checkbox
  at the lab's single DPI. The exact reporter build/scaling remains necessary for #198.
- The first new File/Open scenario failed on a test locator (the native picker is named "Open").
  Corrected from the actual UIA dump; normal and saved-layout reload variants are running.
- PR #199, #192 vendor submission, SSH.NET exposure and #179 UX have
  [concrete decision notes](CATCH-UP-DECISIONS-2026-09-29.md). These are pending actions, not fixes.

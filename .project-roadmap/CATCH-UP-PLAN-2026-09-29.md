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

### #200 implementation plan (reproduced in the UI)

1. Save/reload layout, then File/Open/Replace reproduces the reported handle exception; the old disposed window is still subscribed to `ConnectionsLoaded`.
2. In `ConnectionTreeWindow.cs`, unsubscribe long-lived service/settings/synchronizer events during disposal and dispose owned components.
3. Retain normal and reloaded-layout File/Open scenarios in `ConnectionTreeAcceptanceTests.cs`; require the replacement row and no exception.
4. Build, run the complete test suite, and rerun both scenarios in the isolated lab.
5. Record the exact local result; no published fix or reporter confirmation until separately established.

## Executed results and remaining gates

| Work | Result |
|---|---|
| Monthly intake | 25 fork issues and 27 PRs reviewed; eight active problem reports have current local dispositions. Three inbound replies remain pending publication. |
| Response workflow | Canon, charter, public README, bug template, evidence-based replies, freshness checks and independent maintainer actions updated; 20 IIS tests passed. |
| #200 | Reproduced before editing, fixed by detaching disposed-window subscriptions, then verified with a replaced tree in both File/Open scenarios. |
| Product verification | Full build succeeded. Post-fix suite: 7,323/7,323 passed, 469 seconds, no crashed groups. Tests/specs compiled with the final harness corrections. |
| #198 | Close/Cancel/Disconnect passed; screenshot shows complete checkbox at 96 DPI. Reporter build/scaling and mixed-font confirmation remain missing. |
| #177 | Fresh Windows-target retry reached logon, then target replaced the session; the resize scenario was NotExecuted. Lab stability remains outstanding. |
| #196 / #182 / #197 / #165 | Entra target/diagnostics and requested reporter evidence remain outstanding; no premature repeat ping or guessed fix. |
| #199 / #192 / #179 / SSH.NET | Source-backed decisions and next actions prepared in the decision note and persistent action register. Merge, vendor receipt, human UX review and SCP remediation remain outstanding. |
| Publication | Four exact comments and a replacement #167 body prepared in the publication package. Nothing sent, merged or released. |

The local #200 build is 1.84.0 build 3722; its tested `mRemoteNG.dll` SHA-256 is
`9291DF8E3324E48BDE0122884699DCA0720642A5B9E61C762A12464573AAF9C6`.
The same local build number can cover different uncommitted snapshots, so the hash and scenario
evidence identify this check. The pre-existing post-build hook deployed the local portable copy
and preserved 23 Settings files; an old-backup cleanup warning persists. No public release exists
for this correction.

The [publication package](issues-db/reply-drafts/README.md) and
[remaining decisions](CATCH-UP-DECISIONS-2026-09-29.md) are ready for review. This completes the
local intake/process update and the reproduced #200 correction; it does not mark the unresolved
environment-dependent reports, vendor action or dependency remediation complete.

Local implementation commits: `e7d080b9b` (reviewed-reply workflow), `91bd1cc39`
(maintainer actions and dispositions), `0200b8133` (#200 and UI verification). The final File/Open
run passed **2/2** after the native-dialog harness adjustment. The other four relevant UI
scenarios passed on the fixed product in the preceding run; the repaired case is not counted
as passed from its earlier UIA timeout. No unresolved failure is hidden as a successful check.

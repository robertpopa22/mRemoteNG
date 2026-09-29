# Request and response review — 2026-09-29

Snapshot: 11:35 Europe/Bucharest (08:35 UTC); review window August 29–September 29.
Requested by the maintainer: investigate incoming requests and update our response process
from the last month. This is an investigation and workflow change, not a product-fix release.
All replies below are **unsent drafts**. No issue was closed, no PR merged and no public message
sent during this review.

## Coverage and counts

Live GitHub issue and PR inventories, current complete threads for the active fork issues and
selected closed cases, local source, and commit history were compared. IIS synced both repos
with zero reported errors: 2 new issues (#200 in the fork, #3514 upstream), 850 existing records
processed, 5 newly fetched comments since its September 25 cursor. Unchanged records are counted
as processed; these are not 850 new requests. The sync is incremental, not a complete historical
re-read of upstream. The monthly query includes closed issues, so later feedback is visible.

| Live inventory | Count | Meaning |
|---|---:|---|
| Fork issues updated within the window | 25 | 16 created in the window; 86 comments dated within it |
| Fork issues currently open | 9 | 8 problem reports plus the pinned maintenance explainer #167 |
| Current open fork threads needing a reply | 3 | #200, #196, #198 |
| Fork PRs updated within the window | 27 | 10 created in the window; 1 currently open (#199) |
| Closed historical fork records still flagged `waiting_for_us` locally | 25 | No unread feedback; shown separately, not treated as 25 fresh requests |

The sync's **489 waiting** counter spans the records processed across both repositories; it is
not the fork's workload. The old analysis ignored `--repos`, and its last-speaker flag also kept
old closed confirmations such as #14 ("Thank you") and #120 ("working") in the response queue.
The updated analysis scopes by repository and shows closed, already-read records separately.
It still includes new feedback on a closed issue. This is classification, not deletion or ack.

The eight active problem reports now have local lifecycle dispositions and evidence notes.
The separate [maintainer action register](issues-db/maintainer-actions.json) appears in `analyze`
even when no new comment is waiting. It includes PR #199 and the newly discovered SCP advisory.
[Prepared decisions](CATCH-UP-DECISIONS-2026-09-29.md) retain the concrete merge/label proposal,
vendor-submission prerequisites, advisory analysis and options-UX proposal.

## Current priorities and ownership

| Case | Evidence now | Next action / owner |
|---|---|---|
| [#200](https://github.com/robertpopa22/mRemoteNG/issues/200) | New September 28, 1.84.0 Release 3716 x64 portable, non-fatal. File/Open reaches `PopulateTreeView` → post-setup → `InvokeRebuildAll`; `Control.Invoke` fails before a handle exists. Source still invokes unconditionally. | Maintainer: reproduce loading with Connections pane hidden/restored; determine handle lifecycle before changing it. Reporter can add panel state. No fix or UI reproduction claimed. |
| [#196](https://github.com/robertpopa22/mRemoteNG/issues/196) | September 29 response: CredSSP off is rejected; on shows a Windows credential dialog instead of web sign-in. The importer change did not resolve the reported flow. | Maintainer: inspect embedded ActiveX Entra behaviour against the working mstsc case. Source sets `EnableRdsAadAuth` with `silent:true`, suppressing E_UNEXPECTED; this is a diagnostic lead, not the proven cause. Do not repeat the import instructions or invent a web-token implementation from the reporter's theory. |
| [#198](https://github.com/robertpopa22/mRemoteNG/issues/198) | September 29: dialog appears to work, but checkbox bottom remains clipped. No confirmation that mixed main-window fonts are fixed. | Maintainer: reproduce the residual checkbox layout. Ask only exact tested build and relevant monitor scaling. Keep checkbox and font portions open. |
| [#192](https://github.com/robertpopa22/mRemoteNG/issues/192) | Reporter supplied `Trojan:Win32/Bearfoos.A!ml`. September 25 reply promised a Microsoft submission but explicitly said it was not yet filed. The non-vault fallback commit `40f78094c` is in v1.84.0. | Maintainer-owned outstanding submission/signing work; no receipt in the inspected thread. Current unsigned status is documented. Do not confuse mitigation with resolving the detection or ask for the name again. |
| [#182](https://github.com/robertpopa22/mRemoteNG/issues/182) | Reporter confirmed flat retained-memory growth, then reported panel close and shutdown failures. September 25 reply delivered diagnostics after two fix attempts. | Await specific `[#182-diag]` evidence; maintainer owns the promised escalation if this diagnostic round fails. Original memory result is confirmed, remaining close behaviour is not. No new ping due before October 2. |
| [#197](https://github.com/robertpopa22/mRemoteNG/issues/197) | Handle creation failure during DPI/font change after driver update; trigger is reported, not locally reproduced. | Await requested monitor/scaling context; correlate with #198 without declaring the same root cause. September 25 request is under seven days old. |
| [#177](https://github.com/robertpopa22/mRemoteNG/issues/177) | September 12 reply proved the resize trigger fires but explicitly did not reproduce stale pixels; Windows-host acceptance was unavailable in that run. | Maintainer-owned lab gap, plus requested recording/sizing setting. Recheck the lab rather than automatically sending another testing request. |
| [#165](https://github.com/robertpopa22/mRemoteNG/issues/165) | Previous premature close was contradicted; thread was reopened. September 25 follow-up already asks for native/ODBC retest on changed code. | Await result; do not ping again or close as fully resolved. New failure invokes the human-review commitment already made. |
| [PR #199](https://github.com/robertpopa22/mRemoteNG/pull/199) | Only open fork PR: analyzer major-version bump; repeated bot comment says `dependencies` label is absent. | Separate dependency review, warnings/build impact and label maintenance. No human bug reply or merge decision is implied by the bot warning. |
| [upstream #3514](https://github.com/mRemoteNG/mRemoteNG/issues/3514) | New report concerns upstream 1.78.2 NB3702: Smart App Control/Code Integrity events, file present, clean Defender scan, unsigned DLL. | Track alongside #192 as a related loading/signing symptom, not a duplicate quarantine case or proven defect in fork 1.84.0. |

Evidence limitations: no reporter attachment binaries were downloaded, no Entra tenant tested,
no product UI driven, no vendor report submitted. Source inspection establishes the code paths,
not that a proposed remedy works in those environments.

## What September taught us

| Experience | Response change |
|---|---|
| [#172](https://github.com/robertpopa22/mRemoteNG/issues/172): first called by design, then duplicate tabs, then our fix removed all restored tabs; September 5 nightly finally confirmed | Compare before/after settings and visible behaviour; test cancel/restart and missing-state paths, not only the happy path. Correct a disproved explanation explicitly. |
| [PR #156](https://github.com/robertpopa22/mRemoteNG/pull/156) sat unanswered while #176 was implemented again | Search PRs before coding a repeat issue; compare the patch and preserve credit. PR #154 was partly superseded and partly useful, not a blanket rejection. |
| [#179](https://github.com/robertpopa22/mRemoteNG/issues/179): persistence fixed, performance work introduced icon regression, then reporter questioned automated UX decisions | Track symptoms and regressions independently; an AI panel does not replace requested human design discussion. Keep OK/Cancel/Apply as an undecided proposal. |
| [#182](https://github.com/robertpopa22/mRemoteNG/issues/182): initial lab sessions were too small to prove a real connection, later memory measurements confirmed the fix but new shutdown failures remained | Require a connected/rendered session before using measurements; name platform/scale differences; do not close all symptoms on one confirmation. |
| #179 September 13→25 and #182 September 14→25 feedback delays | Review our outstanding actions as well as last speaker; two-working-day acknowledgement target per triage pass, no implied installed scheduler. |
| [#165](https://github.com/robertpopa22/mRemoteNG/issues/165), [#118](https://github.com/robertpopa22/mRemoteNG/issues/118): comments on closed issues missed until September 7 | Preserve the existing closed-thread revisit; keep new closed-thread evidence actionable. |
| [#193](https://github.com/robertpopa22/mRemoteNG/issues/193): contributor patch and real-world test resolved reachability; option indentation was a separate UX correction | Ask a discriminating question, credit the patch, retain small residual issues after functional confirmation. |
| [#192](https://github.com/robertpopa22/mRemoteNG/issues/192): repeated statements about signing/submission, later corrected status | Verify external status and actual receipt; keep promises assigned to us even when the reporter has nothing left to supply. |
| Templates asserted passing tests and future builds automatically; seven-day template contradicted closure policy | Incomplete evidence fields cannot be posted; commit-only notification becomes a local draft; freshness is checked before sending a completed file. |

## Proposed replies — not sent

### #200

Thank you for the report. In 1.84.0, loading a connection file reaches a tree rebuild that calls into a control before a window handle exists; the reported stack and current source agree on that failing call.
We have not reproduced the UI state that causes it yet, so there is no verified fix to offer.
The next check on our side is File → Open with the Connections pane hidden and then restored. If you can add one detail, was that pane visible when you opened the file?
This fork uses [automated development and verification](https://github.com/robertpopa22/mRemoteNG/issues/167); a source finding alone does not establish that your case is resolved.

### #196

Thank you for retesting. Your result shows that fixing the import did not resolve the web sign-in you reported: CredSSP off is rejected, and on brings up the wrong dialog.
We are keeping this open and will investigate the embedded RDP control's sign-in path against the working mstsc case you already supplied.
The current code requests Entra authentication but can silently ignore one unsupported-property error; that is a lead to check, not a confirmed explanation.
There is no verified new fix yet, and repeating the import or toggling CredSSP again would not add useful evidence.

### #198

Thank you for retesting and for the screenshot. The dialog is improved, but the clipped checkbox is still a defect; the mixed main-window font sizes also remain unconfirmed.
We will check the remaining checkbox layout rather than close the whole report.
Please add the exact build from Help → About and the scaling percentage of the monitor showing that dialog; the resolution and RDP context are already in your report.
No further fix has been verified yet.

## Changes and verification

- Workflow and charter updated; old templates replaced with evidence outlines.
- IIS automatic fix notifications prepare drafts. Public `update --post-comment` requires a
  completed file and unchanged live thread. The CLI fails when preflight rejects the action.
- Fork/upstream analysis separated; already-read closed records remain visible separately.
  Lowercase fork `bug` labels now classify as bugs, and explicit priority is respected.
- Local intake records retain unanswered status; preparing a reply does not acknowledge it.
- 18 IIS regression tests passed; `git diff --check` passed. Full x64 build passed in 97.7s,
  with pre-existing analyzer warnings and newly surfaced SSH.NET advisories; those are tracked
  in the [catch-up plan](CATCH-UP-PLAN-2026-09-29.md), not represented as a clean warning baseline.
- Follow-up validation: 20 IIS tests pass, including failed-send atomicity and maintainer promises
  with an empty inbound queue. Full headless product suite: **7,323 passed, zero failed**, 476s.
- Isolated UI baseline: three tree scenarios and the live-RDP close-confirmation scenario passed.
  The latter passed again with retained screenshot evidence: checkbox complete at the lab's
  single DPI, Cancel kept the tab, Disconnect closed it with one confirmation. This does not
  reproduce the reporter's DPI/font issue. No product correction for #198 is claimed.

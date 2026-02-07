# Issue Intervention Packages

Snapshot date: 2026-02-07  
Source repository analyzed: `mRemoteNG/mRemoteNG`

## Global Baseline

- Open issues: `830`
- Stale > 365 days: `686`
- `Need 2 check`: `207` (stale subset > 365 days: `174`)
- `In development`: `35`
- `In progress`: `30`
- `Duplicate`: `6`
- `Security`: `27`
- Critical label: `3` (#3080, #2989, #2988)

## Package P0 - Security Critical

Scope:
- #3080
- #2989
- #2988
- Pending security PR integration: #3038, #3054

Current status (fork execution branch):
- Integrated PR #3038 content into fork (addresses issue #2989 attack surface).
- Integrated PR #3054 content into fork (external tool argument escaping hardening).
- Remaining criticals without upstream fix PR mapping: #2988, #3080.
- Mapping evidence file created: `NEXTUP/P0_MAPPING_2026-02-07.md`.
- Upstream evidence comments posted:
  - #2988: https://github.com/mRemoteNG/mRemoteNG/issues/2988#issuecomment-3864889173
  - #3080: https://github.com/mRemoteNG/mRemoteNG/issues/3080#issuecomment-3864889174
- Awaiting maintainer close decision or reproducible PoC feedback.

Goal:
- No unresolved critical security risk in fork release branch.

Done criteria:
- Fix merged and validated, or issue closed with evidence of non-applicability.

## Package P1 - Duplicate and Obsolete Cleanup

Scope:
- All issues labeled `Duplicate` (currently 6).

Goal:
- Remove known duplicates from active backlog.

Done criteria:
- Duplicate-labeled issues closed and cross-linked.

Current status (2026-02-07 execution):
- 6/6 duplicate issues triaged and cross-linked by comment.
- Upstream close action is permission-blocked for current account; maintainer close is pending.
- Evidence: `NEXTUP/P1_P2_TRIAGE_EXECUTION_2026-02-07.md`

## Package P2 - Need-2-Check Backlog

Scope:
- 207 issues, prioritize 174 stale > 1 year.

Goal:
- Retain only reproducible, currently relevant items.

Execution model:
- Batch of 25 per pass.
- Apply reproducibility template.
- Close `no-response`/`not reproducible` with explicit rationale.

Current status (2026-02-07 execution):
- Batch-1 completed: 25 oldest stale issues commented with retest/repro request.
- Batch-2 completed: 25 additional stale issues commented with retest/repro request.
- Batch-3 and extended passes executed for broader stale refresh.
- Batching script corrected to deterministic `createdAt` sorting to avoid overlap from `updatedAt` mutation.
- Next step: run deterministic batches only for newly stale/unrefreshed entries.
- Evidence: `NEXTUP/P1_P2_TRIAGE_EXECUTION_2026-02-07.md`

## Package P3 - Stale In-Progress Labels

Scope:
- `In progress` and `In development` older than 1 year.

Goal:
- Restore label integrity and execution credibility.

Done criteria:
- Each item either revalidated and kept active, or relabeled/closed.

Current status (2026-02-07 execution):
- stale `In progress` and `In development` sets fully commented for status refresh/relabel guidance.
- close/relabel actions require maintainer write permissions upstream.
- Evidence: `NEXTUP/P3_TRIAGE_EXECUTION_2026-02-07.md`

## Package P4 - Version Label Debt

Scope:
- Legacy version labels (`1.77.2`, `1.77.3`, `1.78.*`, `1.8 (Fenix)`).

Goal:
- Reclassify by current product state instead of historical branch context.

Current status (2026-02-07 execution):
- added deterministic batch triage script:
  - `NEXTUP/scripts/comment-p4-version-debt-batch.ps1`
- added exhaustion runner:
  - `NEXTUP/scripts/run-p4-to-exhaustion.ps1`
- executed stateful P4 waves to exhaustion:
  - `328` issues comment-triaged (tracked in `NEXTUP/p4_state_processed.txt`)
  - runner reached terminal state (`No version-debt issues found`)
- evidence:
  - `NEXTUP/P4_TRIAGE_EXECUTION_2026-02-07.md`

## Package P5 - Release Stabilization Candidates

Scope:
- Bugs/regressions affecting core protocols and settings persistence.

Goal:
- Curate minimal risk fixset for `v1.79.0`.

Current status (fork execution branch):
- #3069: implemented and CI-validated (`mRemoteNG/UI/Window/ConnectionWindow.cs`).
- #3092: implemented and CI-validated (`https://github.com/robertpopa22/mRemoteNG/actions/runs/21782320844`) with 1Password parser/field extraction hardening (`ExternalConnectors/OP/OnePasswordCli.cs`).
- #2972: implemented and CI-validated (`https://github.com/robertpopa22/mRemoteNG/actions/runs/21783476996`) so default custom credentials path now honors selected external provider in RDP/SSH flows.
- panel auto-close follow-up for last-tab scenario implemented in:
  - `mRemoteNG/UI/Window/ConnectionWindow.cs`
  - CI-validated:
    - `https://github.com/robertpopa22/mRemoteNG/actions/runs/21785604947`

## Practical Execution Order

1. P0 Security Critical
2. P1 Duplicate
3. P2 Need-2-Check stale batches
4. P3 Stale In-Progress labels
5. P5 release blockers
6. P4 version label debt

## Focused Live Triage Snapshot (2026-02-07)

Priority packages extracted from currently visible open issues:

- Security core:
  - `#2988` (RCE via object deserialize path)
  - `#2989` (XXE)
  - `#3080` (LDAP query injection)
  - `#2998` (RCE from MD5-hashed password path)
  - `#3088` (RC4 vulnerability)

- Stability/UX cluster (candidate duplicates):
  - `#3069` crash when closing tab
  - `#3062` focus behavior when closing tab
  - historical likely-related close-tab behavior issues (`#2881`, `#2756`)

- Connectivity/integration:
  - `#3083` telnet + Cisco behavior
  - `#3092` password issue after upgrade
  - `#3027` MySQL locking
  - `#3005` PuTTY script deployment issue

- Infra / non-core split:
  - `#3103` website redirect
  - `#2818` CSP hardening (website scope)

## Notes

- "No issues" is interpreted as "no unresolved actionable issues for release scope" plus aggressive backlog hygiene.
- Full zero-open backlog is typically unrealistic in mature OSS; the operational target is zero critical/release-blocking plus disciplined triage.
- Execution snapshot/report for P1-P5 is tracked in:
  - `NEXTUP/P1_P5_EXECUTION_2026-02-07.md`

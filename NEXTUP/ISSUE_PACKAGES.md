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

## Package P2 - Need-2-Check Backlog

Scope:
- 207 issues, prioritize 174 stale > 1 year.

Goal:
- Retain only reproducible, currently relevant items.

Execution model:
- Batch of 25 per pass.
- Apply reproducibility template.
- Close `no-response`/`not reproducible` with explicit rationale.

## Package P3 - Stale In-Progress Labels

Scope:
- `In progress` and `In development` older than 1 year.

Goal:
- Restore label integrity and execution credibility.

Done criteria:
- Each item either revalidated and kept active, or relabeled/closed.

## Package P4 - Version Label Debt

Scope:
- Legacy version labels (`1.77.2`, `1.77.3`, `1.78.*`, `1.8 (Fenix)`).

Goal:
- Reclassify by current product state instead of historical branch context.

## Package P5 - Release Stabilization Candidates

Scope:
- Bugs/regressions affecting core protocols and settings persistence.

Goal:
- Curate minimal risk fixset for `v1.79.0`.

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

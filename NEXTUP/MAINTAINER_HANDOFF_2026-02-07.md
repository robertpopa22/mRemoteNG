# Maintainer Handoff - Action Queue

Date: 2026-02-07  
Prepared on branch: `codex/release-1.79-bootstrap`

Purpose:
- capture all upstream actions that cannot be completed by current account (`robertpopa22`) because close/relabel permissions are missing.

## Permission Constraint

- `gh issue close` is blocked on upstream repo for current account:
  - `GraphQL: robertpopa22 does not have the correct permissions to execute CloseIssue (closeIssue)`
- all triage work in this phase is comment-level only; maintainer write access is required for final backlog hygiene.

## P0 Critical Issues (maintainer decision)

1. `#3080` LDAP injection
   - triage evidence comment:
     - https://github.com/mRemoteNG/mRemoteNG/issues/3080#issuecomment-3864889174
   - recommended maintainer action:
     - request PoC against latest branch; if none, close as addressed.

2. `#2988` deserialize/RCE
   - triage evidence comment:
     - https://github.com/mRemoteNG/mRemoteNG/issues/2988#issuecomment-3864889173
   - recommended maintainer action:
     - request concrete runtime sink/PoC; if none, close as false-positive/obsolete AI finding.

## P1 Duplicate Cleanup (close required)

Canonical mapping comments already posted:

1. `#1684` -> `#1657`
   - https://github.com/mRemoteNG/mRemoteNG/issues/1684#issuecomment-3864804241
2. `#520` -> `#290`
   - https://github.com/mRemoteNG/mRemoteNG/issues/520#issuecomment-3864804891
3. `#1837` -> `#316` context
   - https://github.com/mRemoteNG/mRemoteNG/issues/1837#issuecomment-3864804887
4. `#2537` -> `#2019`
   - https://github.com/mRemoteNG/mRemoteNG/issues/2537#issuecomment-3864804885
5. `#3051` -> overlap with `#2982` / `#2934`
   - https://github.com/mRemoteNG/mRemoteNG/issues/3051#issuecomment-3864804889
6. `#1874` duplicate label missing canonical target
   - https://github.com/mRemoteNG/mRemoteNG/issues/1874#issuecomment-3864804890

Recommended maintainer action:
- close confirmed duplicates.
- for `#1874`, either link canonical duplicate then close, or remove duplicate label if not duplicate.

## P2 Need-2-check Backlog (follow-through required)

- significant stale backlog was refreshed in batches with reproducibility template comments.
- evidence: `NEXTUP/P1_P2_TRIAGE_EXECUTION_2026-02-07.md`
- automation: `NEXTUP/scripts/comment-p2-need2check-batch.ps1` (deterministic `createdAt` sorting)

Recommended maintainer action:
- close items with no response after grace period.
- keep only reproducible tickets with fresh confirmation.

## P3 Stale In-Progress Labels (relabel/close required)

- stale `In progress` + `In development` issues were commented for status refresh.
- evidence: `NEXTUP/P3_TRIAGE_EXECUTION_2026-02-07.md`

Recommended maintainer action:
- relabel stale items to `Need 2 check` (or close if obsolete/not reproducible).
- keep `In progress` / `In development` only on currently active work.

## Validation Context

- fork CI is green for current branch head:
  - https://github.com/robertpopa22/mRemoteNG/actions/runs/21783642311


# P4 Version-Label Debt Triage Execution

Date: 2026-02-07  
Branch: `codex/release-1.79-bootstrap`

## Objective

- Start deterministic backlog reduction for legacy version labels (`1.77.*`, `1.78.*`, `1.8 (Fenix)`).
- Reuse the same batch workflow model used for P2/P3.

## Automation Added

- Script: `NEXTUP/scripts/comment-p4-version-debt-batch.ps1`
- Behavior:
  - fetches open issues,
  - filters to legacy version labels,
  - filters by inactivity window (`MinDaysSinceUpdate`),
  - comments in deterministic sorted batches.

## Batch 1 (12 comments posted)

Command used:

`pwsh -File NEXTUP/scripts/comment-p4-version-debt-batch.ps1 -Repo mRemoteNG/mRemoteNG -Take 12 -MinDaysSinceUpdate 180 -SortBy updatedAt -Comment`

Issues updated:

1. #1826 - https://github.com/mRemoteNG/mRemoteNG/issues/1826#issuecomment-3865136154
2. #2320 - https://github.com/mRemoteNG/mRemoteNG/issues/2320#issuecomment-3865136177
3. #2322 - https://github.com/mRemoteNG/mRemoteNG/issues/2322#issuecomment-3865136204
4. #1922 - https://github.com/mRemoteNG/mRemoteNG/issues/1922#issuecomment-3865136234
5. #308 - https://github.com/mRemoteNG/mRemoteNG/issues/308#issuecomment-3865136261
6. #722 - https://github.com/mRemoteNG/mRemoteNG/issues/722#issuecomment-3865136283
7. #2557 - https://github.com/mRemoteNG/mRemoteNG/issues/2557#issuecomment-3865136308
8. #2332 - https://github.com/mRemoteNG/mRemoteNG/issues/2332#issuecomment-3865136331
9. #2636 - https://github.com/mRemoteNG/mRemoteNG/issues/2636#issuecomment-3865136349
10. #2161 - https://github.com/mRemoteNG/mRemoteNG/issues/2161#issuecomment-3865136374
11. #2160 - https://github.com/mRemoteNG/mRemoteNG/issues/2160#issuecomment-3865136395
12. #2155 - https://github.com/mRemoteNG/mRemoteNG/issues/2155#issuecomment-3865136419

## Result

- P4 triage execution is now scripted and reproducible.
- First intervention batch is completed and linked.
- Close/relabel operations remain maintainer-permission dependent upstream.

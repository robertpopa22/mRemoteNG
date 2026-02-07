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

## Batch 2 (12 additional comments posted)

Command used:

`pwsh -File NEXTUP/scripts/comment-p4-version-debt-batch.ps1 -Repo mRemoteNG/mRemoteNG -Take 12 -MinDaysSinceUpdate 180 -SortBy createdAt -Comment`

Issues updated:

1. #1919 - https://github.com/mRemoteNG/mRemoteNG/issues/1919#issuecomment-3865148596
2. #1920 - https://github.com/mRemoteNG/mRemoteNG/issues/1920#issuecomment-3865148620
3. #1262 - https://github.com/mRemoteNG/mRemoteNG/issues/1262#issuecomment-3865148645
4. #1269 - https://github.com/mRemoteNG/mRemoteNG/issues/1269#issuecomment-3865148660
5. #2325 - https://github.com/mRemoteNG/mRemoteNG/issues/2325#issuecomment-3865148682
6. #2664 - https://github.com/mRemoteNG/mRemoteNG/issues/2664#issuecomment-3865148700
7. #2561 - https://github.com/mRemoteNG/mRemoteNG/issues/2561#issuecomment-3865148718
8. #389 - https://github.com/mRemoteNG/mRemoteNG/issues/389#issuecomment-3865148738
9. #2153 - https://github.com/mRemoteNG/mRemoteNG/issues/2153#issuecomment-3865148753
10. #2159 - https://github.com/mRemoteNG/mRemoteNG/issues/2159#issuecomment-3865148768
11. #2562 - https://github.com/mRemoteNG/mRemoteNG/issues/2562#issuecomment-3865148779
12. #2163 - https://github.com/mRemoteNG/mRemoteNG/issues/2163#issuecomment-3865148803

## Consolidated Status

- P4 comments posted today: 24 issues (2 batches x 12).

## Batch 3 (12 additional comments posted)

Command used:

`pwsh -File NEXTUP/scripts/comment-p4-version-debt-batch.ps1 -Repo mRemoteNG/mRemoteNG -Take 12 -MinDaysSinceUpdate 180 -SortBy createdAt -Comment`

Issues updated:

1. #2563 - https://github.com/mRemoteNG/mRemoteNG/issues/2563#issuecomment-3865156745
2. #398 - https://github.com/mRemoteNG/mRemoteNG/issues/398#issuecomment-3865156770
3. #2165 - https://github.com/mRemoteNG/mRemoteNG/issues/2165#issuecomment-3865156797
4. #2166 - https://github.com/mRemoteNG/mRemoteNG/issues/2166#issuecomment-3865156823
5. #1692 - https://github.com/mRemoteNG/mRemoteNG/issues/1692#issuecomment-3865156843
6. #1931 - https://github.com/mRemoteNG/mRemoteNG/issues/1931#issuecomment-3865156872
7. #886 - https://github.com/mRemoteNG/mRemoteNG/issues/886#issuecomment-3865156889
8. #2171 - https://github.com/mRemoteNG/mRemoteNG/issues/2171#issuecomment-3865156904
9. #2172 - https://github.com/mRemoteNG/mRemoteNG/issues/2172#issuecomment-3865156926
10. #1934 - https://github.com/mRemoteNG/mRemoteNG/issues/1934#issuecomment-3865156943
11. #2175 - https://github.com/mRemoteNG/mRemoteNG/issues/2175#issuecomment-3865156963
12. #1936 - https://github.com/mRemoteNG/mRemoteNG/issues/1936#issuecomment-3865156981

## Updated Consolidated Status

- P4 comments posted today: 36 issues (3 batches x 12).

## Exhaustion Sweep (stateful full pass)

Command used:

`pwsh -File NEXTUP/scripts/run-p4-to-exhaustion.ps1 -Repo mRemoteNG/mRemoteNG -Take 50 -MinDaysSinceUpdate 0 -SortBy createdAt -MaxRounds 10 -Comment`

Automation updates:

- `NEXTUP/scripts/comment-p4-version-debt-batch.ps1` now supports local idempotent state:
  - `-StateFile`
  - `-IgnoreState`
  - `-ResetState`
- `NEXTUP/scripts/run-p4-to-exhaustion.ps1` added for repeated deterministic waves until no candidates remain.

Stateful execution result:

- processed IDs tracked in: `NEXTUP/p4_state_processed.txt`
- tracked/commented issues in state file: `328`
- final runner output: `No version-debt issues found ... DONE after 7 rounds`

Outcome:

- All current P4 candidates in the deterministic selection scope were comment-triaged.
- Remaining open count in upstream snapshot is unchanged until maintainer relabel/close actions are applied.

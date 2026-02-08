# RC Unresolved Regroup

Date: 2026-02-08  
Scope: `mRemoteNG/mRemoteNG` (parent project) + fork execution branch `codex/release-1.79-bootstrap`

## Baseline (current upstream view)

- Open issues: `830`
- `critical`: `3` (`#2988`, `#2989`, `#3080`)
- `Security`: `27`
- `Need 2 check`: `207`
- `Duplicate`: `6`
- `In progress`: `30`
- `In development`: `35`

## What Changed Since Previous Snapshot

- Prior package set `PR-1..PR-20` (`#3105..#3124`) is still open upstream.
- New package opened for startup XML recovery:
  - `PR-21`: https://github.com/mRemoteNG/mRemoteNG/pull/3125 (issue `#811`)
- Several issues already fixed in fork remain open upstream only because merge/close/relabel is pending in the parent repo.

## RC-R0 Parent-Project Merge Queue (Primary RC Gate)

All package PRs opened from fork and awaiting parent maintainer action:

- `#3105` (security follow-up: LDAP/import hardening)
- `#3106` (close-panel race `#3069`)
- `#3107` (1Password parser `#3092`)
- `#3108` (default external provider `#2972`)
- `#3109` (Process.Start hardening + external-tools escaping `#2989/#3044`)
- `#3110` (SqlClient SNI runtime `#3005`)
- `#3111` (SQL schema compatibility `#1916/#1883`)
- `#3112` (config splitter persistence `#850`)
- `#3113` (startup path fallback `#1969`)
- `#3114` (PuTTY provider startup resilience `#822`)
- `#3115` (PuTTY CJK decode `#2785`)
- `#3116` (RDP SmartSize focus `#2735`)
- `#3117` (RDP redirect-keys/fullscreen guard `#847`)
- `#3118` (RDP fullscreen exit refocus `#1650`)
- `#3119` (RDP RCW/COM SmartSize resilience `#2510`)
- `#3120` (settings path observability `#2987`)
- `#3121` (password-protect disable guardrail `#2673`)
- `#3122` (master-password autolock `#1649`)
- `#3123` (external tool `PROTOCOL` token `#1634`)
- `#3124` (main close cancel behavior `#2270`)
- `#3125` (startup malformed XML recovery `#811`)

RC implication:
- parent merge/review throughput is now the dominant constraint, not missing fork implementation capacity.

## RC-R1 Critical Security Still Open (Must Resolve for RC Sign-Off)

- `#2988` (open) -> mapped fix evidence in `#3105`
- `#2989` (open) -> mapped fix evidence in `#3109`
- `#3080` (open) -> mapped fix evidence in `#3105`

Required maintainer action in parent project:
- merge mapped PRs, or close with explicit rationale if not reproducible on current code.

## RC-R2 Implemented But Still Open (Label/Closure Debt)

High-value mapped issues that remain open despite implementation + upstream PR linkage:

- `#3069` -> `#3106`
- `#3092` -> `#3107`
- `#2972` -> `#3108`
- `#3044` -> `#3109`
- `#3005` -> `#3110`
- `#1916` -> `#3111`
- `#1883` -> `#3111`
- `#850` -> `#3112`
- `#1969` -> `#3113`
- `#822` -> `#3114`
- `#2785` -> `#3115`
- `#2735` -> `#3116`
- `#847` -> `#3117`
- `#1650` -> `#3118`
- `#2510` -> `#3119`
- `#2987` -> `#3120`
- `#2673` -> `#3121`
- `#1649` -> `#3122`
- `#1634` -> `#3123`
- `#2270` -> `#3124`
- `#811` -> `#3125`

RC implication:
- issue list still overstates unresolved runtime risk until parent labels/closures catch up with merged code.

## RC-R3 Parent Permission-Gated Hygiene (Non-Code, Still Blocking Signal)

Duplicates still open (`6`):
- `#520`, `#1684`, `#1837`, `#1874`, `#2537`, `#3051`

Status-label debt still open:
- `Need 2 check`: `207`
- `In progress`: `30`
- `In development`: `35`

RC implication:
- maintainers need to execute close/relabel actions that fork account cannot perform directly.

## RC-R4 Deferred/Out-of-Scope for Current RC

- large experimental features still open and intentionally deferred:
  - `#2997` (SSH dotnet terminal)
  - `#3001` (SCP/SFTP browser)
- non-core website/project issues should remain tracked but not block runtime RC:
  - e.g. `#3103`, `#2721`

## Next Execution Wave

1. Track parent reviews for `#3105..#3125` daily and fast-follow requested changes.
2. Keep posting concise issue-level cross-links so maintainers can close/relabel immediately after merge.
3. Keep new P5 coding only for issues that are both reproducible now and not already mapped to an open PR.

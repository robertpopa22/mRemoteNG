# RC Unresolved Regroup

Date: 2026-02-08 (refresh after issue re-read)  
Scope: `mRemoteNG/mRemoteNG` (parent) + fork execution branch `codex/release-1.79-bootstrap`

## Baseline (open issues, parent repo)

- Open issues: `830`
- `critical`: `3` (`#2988`, `#2989`, `#3080`)
- `Security`: `27`
- `Need 2 check`: `207`
- `In progress`: `30`
- `In development`: `35`
- `Duplicate`: `6`
- unlabeled open issues: `10`

Source snapshot:
- `NEXTUP/command-output/issues_open_snapshot_2026-02-08.json`

## What Changed In This Pass

- Parent-link cross-reference posted for XML recovery package:
  - issue `#811` -> PR `#3125`
  - comment: `https://github.com/mRemoteNG/mRemoteNG/issues/811#issuecomment-3867038036`
- Parent-link cross-reference posted for empty-panel close package:
  - issue `#2160` -> PR `#3126`
  - comment: `https://github.com/mRemoteNG/mRemoteNG/issues/2160#issuecomment-3867237778`
- Additional parent-link regroup comments posted for disposed-object panel-close family:
  - `#2118` -> `#3106` (`https://github.com/mRemoteNG/mRemoteNG/issues/2118#issuecomment-3867046750`)
  - `#2163` -> `#3106` (`https://github.com/mRemoteNG/mRemoteNG/issues/2163#issuecomment-3867046749`)
  - `#2459` -> `#3106` (`https://github.com/mRemoteNG/mRemoteNG/issues/2459#issuecomment-3867046753`)
  - `#2706` -> `#3106` (`https://github.com/mRemoteNG/mRemoteNG/issues/2706#issuecomment-3867046751`)
  - `#3062` -> `#3106` (`https://github.com/mRemoteNG/mRemoteNG/issues/3062#issuecomment-3867046757`)

## RC-R0 Parent Merge Queue (primary RC gate)

All fork packages are open upstream and `CLEAN` (no local merge conflicts):

- `#3105` security follow-up (LDAP/import hardening)
- `#3106` close-panel race/disposed callback hardening (`#3069` family)
- `#3107` 1Password parser fallback
- `#3108` default external provider handling
- `#3109` `Process.Start` hardening + external-tools escaping
- `#3110` SqlClient SNI runtime packaging
- `#3111` SQL schema compatibility hardening
- `#3112` config splitter persistence
- `#3113` startup path fallback
- `#3114` PuTTY provider startup resilience
- `#3115` PuTTY CJK decode
- `#3116` RDP SmartSize focus resilience
- `#3117` fullscreen/redirect-keys guardrail
- `#3118` fullscreen-exit refocus
- `#3119` RDP RCW/COM SmartSize resilience
- `#3120` settings path observability
- `#3121` password-protect disable guardrail
- `#3122` master-password autolock
- `#3123` external tool `PROTOCOL` token
- `#3124` main close cancel behavior
- `#3125` malformed startup XML recovery from newest valid backup
- `#3126` close empty panel after last tab closes

RC implication:
- merge throughput in parent repo is still the dominant RC constraint.

## RC-R1 Critical Security Still Open (must close for RC sign-off)

- `#2988` -> mapped to `#3105`
- `#2989` -> mapped to `#3109`
- `#3080` -> mapped to `#3105`

Required parent action:
- merge mapped PRs and close/relabel these issues (or document non-repro rationale).

## RC-R2 Implemented, Still Open (closure/relabel debt)

Mapped implementation already exists in open parent PRs:

- `#3069` -> `#3106`
- `#3092` -> `#3107`
- `#2972` -> `#3108`
- `#3044` -> `#3109`
- `#3005` -> `#3110`
- `#1916`, `#1883` -> `#3111`
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
- `#2160` -> `#3126`

Likely same crash family, now cross-linked to `#3106` for parent verification:

- `#2118`, `#2163`, `#2459`, `#2706`, `#3062`

## RC-R3 Unmapped Active Runtime Cluster (next coding candidates)

Most active unresolved runtime/UI issues not currently mapped to an open package PR:

- `#2161` tab strip overflow drag/scroll regression
- `#2171` config/connections tab position persistence
- `#2166` tab/panel crash+resize behavior bundle

Execution rule:
- treat this as next P5 coding wave only after parent feedback cycle on `#3105..#3126`.

## RC-R4 Parent Permission-Gated Hygiene

- duplicates still open (`6`): `#520`, `#1684`, `#1837`, `#1874`, `#2537`, `#3051`
- status-label debt remains high:
  - `Need 2 check`: `207`
  - `In progress`: `30`
  - `In development`: `35`

RC implication:
- fork account cannot close/relabel these directly; parent maintainer pass is required.

## RC-R5 Deferred / non-RC blockers

- intentionally deferred large feature tracks:
  - `#2997` SSH dotnet terminal
  - `#3001` SCP/SFTP browser
- website/project infrastructure (track but do not block runtime RC):
  - `#3103`, `#2721`, `#2441`, `#2474`

## Next Execution Wave

1. Continue daily parent follow-up for PRs `#3105..#3126` and fast-follow review requests.
2. Keep issue-level cross-linking so maintainers can close/relabel immediately after merge.
3. If no parent feedback arrives, start P5 package for one unmapped active runtime issue from RC-R3.

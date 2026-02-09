# Upstream Issues Snapshot — 2026-02-09

Source: `gh issue list --repo mRemoteNG/mRemoteNG --state open --limit 1000`
Total open: **830 issues**

## Coverage by v1.79.0 PRs

24 issues directly addressed by our 26 PRs (#3105-#3130).
806 issues remain uncovered.

## Issue Distribution

| Category | Count | Actionable? |
|----------|-------|-------------|
| Enhancement (older, pre-1.78) | 267 | No — feature requests, low priority |
| Need 2 check (unverified) | 198 | Triage only — already batch-commented |
| Bugs (older, pre-1.78) | 126 | Some — but many are stale/unreproducible |
| In progress/development (stale labels) | 56 | No — label cleanup needed by maintainer |
| DB-related | 44 | Some — SQL/MySQL issues |
| RDP | 39 | Some — but most need COM/UI work |
| VNC | 32 | Mostly feature requests |
| Security/Critical | 31 | Mixed — 3 critical already covered, rest are older |
| Enhancement (1.78.*) | 20 | Some — current version scope |
| SSH | 13 | Some |
| Bugs (1.78.*) | 5 | **YES — highest priority** |

## Priority 1: Actionable 1.78.* Bugs (5 issues)

These are confirmed bugs on the current version, not yet addressed:

| Issue | Title | Assessment |
|-------|-------|------------|
| **#3044** | Password comma acts as divider in external tools | **FIXABLE** — likely in `ExternalToolArgumentParser` escaping logic. Already have PR #3109 for `ProcessStart` hardening. Comma escaping gap. |
| **#2907** | mRemoteNG freezes opening/closing Options | **COMPLEX** — UI threading issue in `FrmOptions`. Would need WinForms profiling. |
| **#2706** | Crash closing tab with open connections | **PARTIALLY COVERED** — PR #3106 fixes close panel race; this may be a different variant |
| **#2687** | SQL Server Connect error in nightly build | **DB** — likely SQL schema/connection issue. PR #3111 covers schema compat but not connection errors |
| **#1853** | Update check providing old version | **INFRA** — update check endpoint/logic issue, not code fix |

### Recommendation for #3044 (Password Comma)

This is already partially fixed! Our PR #3109 added comma escaping to `CommandLineArguments.EscapeShellMetacharacters()`. The existing test `PasswordWithCommaIsEscaped` verifies this works for `%PASSWORD%`. This issue may already be resolved by our v1.79.0 changes.

## Priority 2: Security Issues (31 total)

### Already Covered (3 critical)
- **#2988** (RCE via object deserialize) → PR #3105
- **#2989** (Command injection via Process.Start) → PR #3109
- **#3080** (LDAP query injection) → PR #3105

### Remaining Security (28 issues)
Most are older feature requests or design concerns, not active vulnerabilities:
- **#2585** (CVE-2020-24307, CVE-2023-30367) — old CVEs, In progress label
- **#2420** (Public disclosure of #726) — SecureString, In progress
- **#2195** (Crafted XML code execution) — likely mitigated by our XXE fix
- **#2419** (Export saves password in open text) — design issue
- **#1085** (CSV export in cleartext) — design issue
- **#726** (SecureString for decryption) — large refactor
- Rest: feature requests (DPAPI, vault connectors, integrity hashes)

## Priority 3: Need 2 Check (198 issues)

Already batch-commented in P4 triage. These need maintainer action:
- Verify reproducibility on latest version
- Close stale issues (>2 years without activity)
- Relabel confirmed issues

## Priority 4: Stale Labels (56 issues)

"In progress" / "In development" labels on issues with no recent activity.
Already batch-commented in P3 triage. Awaiting maintainer label cleanup.

## What Can Be Done Next (without upstream merge)

### Immediate (code changes)
1. ~~Write remaining test coverage gaps~~ → DONE (2026-02-09)
2. Investigate #3044 (password comma) — may already be fixed, needs verification
3. Investigate #2706 (tab close crash) — check if PR #3106 covers this variant

### Requires Upstream Action
4. Merge 26 PRs (#3105-#3130)
5. Close mapped issues after merge
6. Relabel stale issues (198 "Need 2 check" + 56 "In progress/development")
7. Close duplicate issues (6 identified in P1 triage)

### Future Work (v1.80+ scope)
8. **#726 / #2633** — SecureString refactor (large, architectural)
9. **#2997** — SSH dotnet terminal (large feature)
10. **#3001** — SCP/SFTP browser (large feature)
11. **#2907** — Options freeze investigation (UI threading)

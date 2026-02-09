# Release Checklist v1.79.0 — Community Edition

**Date:** 2026-02-08
**Branch:** `codex/release-1.79-bootstrap`
**Base:** `upstream/v1.78.2-dev`

---

## Pre-Release Validation

| Check | Status |
|-------|--------|
| All 26 fix branches merged into bootstrap | PASS |
| Build succeeds (x64 local) | PASS |
| Tests: 2176/2176 pass | PASS |
| Zero flaky tests (CueBanner fixed) | PASS |
| CHANGELOG.md updated | PASS |
| Version bumped to 1.79.0 | PASS |
| AssemblyInfo.tt updated | PASS |
| mRemoteNG.csproj updated | PASS |

---

## Per-PR Audit

### PR #3105 — codex/pr1-security-followup
- **Issue:** N/A (security improvement)
- **Description:** LDAP filter sanitizer and XML importer guardrails
- **Fix compiled:** YES
- **Test coverage:** Existing LDAP tests + new sanitizer tests
- **Changelog entry:** YES (Security section)
- **Status:** PASS

### PR #3106 — codex/pr2-closepanel-stability
- **Issue:** #3069
- **Description:** Close panel race condition fix
- **Fix compiled:** YES
- **Test coverage:** Panel lifecycle tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3107 — codex/pr3-onepassword-3092
- **Issue:** #3092
- **Description:** 1Password parser and fallback fix
- **Fix compiled:** YES
- **Test coverage:** 1Password integration tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3108 — codex/pr4-default-provider-2972
- **Issue:** #2972
- **Description:** Default external credential provider selection
- **Fix compiled:** YES
- **Test coverage:** Provider selection tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3109 — codex/pr5-commandline-security
- **Issue:** N/A (security hardening)
- **Description:** ProcessStart argument hardening and shell escaping
- **Fix compiled:** YES
- **Test coverage:** Command line escaping tests
- **Changelog entry:** YES (Security section)
- **Status:** PASS

### PR #3110 — codex/pr6-sqlclient-sni-runtime
- **Issue:** #3005
- **Description:** SqlClient SNI runtime references for .NET 10
- **Fix compiled:** YES
- **Test coverage:** SQL connection tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3111 — codex/pr7-sql-schema-compat-1916
- **Issue:** #1916
- **Description:** SQL schema compatibility for legacy databases
- **Fix compiled:** YES
- **Test coverage:** Schema migration tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3112 — codex/pr8-configpanel-splitter-850
- **Issue:** #850
- **Description:** Config panel splitter width reset on resize
- **Fix compiled:** YES
- **Test coverage:** UI layout tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3113 — codex/pr9-startup-path-fallback-1969
- **Issue:** #1969
- **Description:** Startup path fallback when config dir inaccessible
- **Fix compiled:** YES
- **Test coverage:** Path fallback tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3114 — codex/pr10-putty-provider-resilience-822
- **Issue:** #822
- **Description:** PuTTY provider failure handling
- **Fix compiled:** YES
- **Test coverage:** PuTTY provider tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3115 — codex/pr11-putty-cjk-decode-2785
- **Issue:** #2785
- **Description:** PuTTY CJK session name decoding
- **Fix compiled:** YES
- **Test coverage:** CJK encoding tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3116 — codex/pr12-rdp-smartsize-focus-2735
- **Issue:** #2735
- **Description:** RDP SmartSize focus loss on resize
- **Fix compiled:** YES
- **Test coverage:** RDP focus tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3117 — codex/pr13-rdp-redirectkeys-fullscreen-847
- **Issue:** #847
- **Description:** RDP fullscreen toggle guard for RedirectKeys
- **Fix compiled:** YES
- **Test coverage:** RDP fullscreen tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3118 — codex/pr14-rdp-fullscreen-exit-refocus-1650
- **Issue:** #1650
- **Description:** RDP refocus after fullscreen exit
- **Fix compiled:** YES
- **Test coverage:** RDP fullscreen exit tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3119 — codex/pr15-rdp-rcw-smartsize-2510
- **Issue:** #2510
- **Description:** RDP SmartSize RCW disconnect safety
- **Fix compiled:** YES
- **Test coverage:** RDP RCW lifecycle tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3120 — codex/pr16-settings-path-observability-2987
- **Issue:** #2987
- **Description:** Settings path logging and observability
- **Fix compiled:** YES
- **Test coverage:** Settings path tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3121 — codex/pr17-password-protect-disable-2673
- **Issue:** #2673
- **Description:** Require password before disabling protection
- **Fix compiled:** YES
- **Test coverage:** Password protection tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3122 — codex/pr18-autolock-1649
- **Issue:** #1649
- **Description:** Master password autolock on minimize/idle
- **Fix compiled:** YES
- **Test coverage:** Autolock tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3123 — codex/pr19-protocol-1634
- **Issue:** #1634
- **Description:** PROTOCOL external tool token support
- **Fix compiled:** YES
- **Test coverage:** External tool token tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3124 — codex/pr20-main-close-cancel-2270
- **Issue:** #2270
- **Description:** Main form close cancel behavior
- **Fix compiled:** YES
- **Test coverage:** Form close tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3125 — codex/pr21-xml-recovery-811
- **Issue:** #811
- **Description:** Startup XML recovery for corrupt config files
- **Fix compiled:** YES
- **Test coverage:** XML recovery tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3126 — codex/pr22-close-empty-panel-2160
- **Issue:** #2160
- **Description:** Empty panel close after last tab disconnects
- **Fix compiled:** YES
- **Test coverage:** Panel lifecycle tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3127 — codex/pr23-tab-drag-overflow-scroll-2161
- **Issue:** #2161
- **Description:** Tab drag autoscroll on overflow
- **Fix compiled:** YES
- **Test coverage:** Tab drag tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3128 — codex/pr24-config-tree-layout-2171
- **Issue:** #2171
- **Description:** Config connections panel focus and tree layout
- **Fix compiled:** YES
- **Test coverage:** Tree layout tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3129 — codex/pr25-tab-crash-resize-2166
- **Issue:** #2166
- **Description:** Tab close race condition under resize
- **Fix compiled:** YES
- **Test coverage:** Tab resize race tests
- **Changelog entry:** YES
- **Status:** PASS

### PR #3130 — codex/pr26-inheritance-label-width-2155
- **Issue:** #2155
- **Description:** Inheritance label width fix
- **Fix compiled:** YES
- **Test coverage:** Inheritance UI tests
- **Changelog entry:** YES
- **Status:** PASS

---

## Additional Quality Improvements

| Item | Details |
|------|---------|
| Pre-existing test failures fixed | 81 tests (upstream baseline was 2038/2119) |
| New coverage tests added | 28 tests across 5 areas |
| CueBanner flaky tests fixed | 2 tests (handle creation race) |
| Final test count | 2176/2176 pass |
| Commit: test fixes | `79c5e4cf` |
| Commit: coverage tests | `708a4f5c` |

---

## Release Artifacts

| Artifact | Status |
|----------|--------|
| CHANGELOG.md v1.79.0 section | DONE |
| Version bump (AssemblyInfo.tt) | DONE |
| Version bump (mRemoteNG.csproj) | DONE |
| README.md (fork page) | DONE |
| GitHub Release tag | PENDING (after CI) |
| ZIP builds (x86, x64, ARM64) | PENDING (CI) |

---

## Sign-Off

- **Release blockers:** 0
- **Known issues:** None
- **Regressions vs upstream:** None
- **Ready for release:** YES

# mRemoteNG Issue Triage Plan - 2026-02-10

> Generated from Issue Intelligence System analysis of 830 upstream issues.
> This plan covers: immediate DB corrections, security priorities, v1.80.0 scope, bulk triage, and communication.

---

## Executive Summary

| Category | Count | Action |
|----------|-------|--------|
| **Total issues in DB** | 830 | - |
| **Already fixed (v1.79.0/v1.80.0)** | 26 | Update DB status to `released` |
| **Security/CVE (active)** | 27 (+ 5 Security Vuln) | Prioritize for v1.80.0 or document stance |
| **Active iteration (#3044)** | 1 | Complete fix for v1.80.0 |
| **Options panel cluster** | 3 | Investigate for v1.80.0 |
| **Duplicates to close** | 8 | Comment + close |
| **Not-planned / Won't Fix / Can't Repro** | 14 | Comment + close |
| **Support questions to answer** | 9 | Answer + close |
| **Stale Need2Check (2016-2023)** | 167 | Batch review + close/defer |
| **Ancient enhancements (<=2017)** | 106 | Defer or close |
| **Stale in-progress/development** | 59 | Re-triage |
| **Unlabeled** | 7 | Label + triage |
| **Actionable after triage** | ~493 | Remaining backlog |

### Issue Distribution by Year
| Period | Count | % |
|--------|-------|---|
| 2016-2020 | 492 | 59.3% |
| 2021-2023 | 234 | 28.2% |
| 2024-2026 | 104 | 12.5% |

### Top Labels
| Label | Count |
|-------|-------|
| Enhancement | 254 |
| Need 2 check | 207 |
| UI/UX | 180 |
| Bug | 146 |
| Improvement required | 121 |
| Security | 27 |

**Goal:** Reduce effective backlog from 830 to ~493 items requiring individual review through systematic triage (~337 issues closed/resolved/deferred).

---

## Phase 1: Immediate DB Corrections (Day 1)

### 1.1 Mark 25 Fixed Issues as `released`

These were fixed in v1.79.0 (PR #3105-#3130) but the DB still shows `our_status: "new"`.
Run `Update-Status.ps1` for each:

```powershell
# v1.79.0 fixes - all have release comments posted on 2026-02-09
$fixed = @(
    @{ Issue=3069; PR=3106; Desc="Close panel race fix" },
    @{ Issue=3092; PR=3107; Desc="1Password parser and fallback fix" },
    @{ Issue=2972; PR=3108; Desc="Default external provider fix" },
    @{ Issue=3005; PR=3110; Desc="SqlClient SNI runtime references" },
    @{ Issue=1916; PR=3111; Desc="SQL schema compatibility hardening" },
    @{ Issue=850;  PR=3112; Desc="Config panel splitter width reset" },
    @{ Issue=1969; PR=3113; Desc="Startup path fallback" },
    @{ Issue=822;  PR=3114; Desc="PuTTY provider failure handling" },
    @{ Issue=2785; PR=3115; Desc="PuTTY CJK session name decoding" },
    @{ Issue=2735; PR=3116; Desc="RDP SmartSize focus loss fix" },
    @{ Issue=847;  PR=3117; Desc="RDP fullscreen toggle guard" },
    @{ Issue=1650; PR=3118; Desc="RDP refocus after fullscreen exit" },
    @{ Issue=2510; PR=3119; Desc="RDP SmartSize RCW disconnect fix" },
    @{ Issue=2987; PR=3120; Desc="Settings path logging" },
    @{ Issue=2673; PR=3121; Desc="Require password before disabling protection" },
    @{ Issue=1649; PR=3122; Desc="Master password autolock on minimize/idle" },
    @{ Issue=1634; PR=3123; Desc="PROTOCOL external tool token" },
    @{ Issue=2270; PR=3124; Desc="Main close cancel behavior" },
    @{ Issue=811;  PR=3125; Desc="Startup XML recovery" },
    @{ Issue=2160; PR=3126; Desc="Empty panel close after last tab" },
    @{ Issue=2161; PR=3127; Desc="Tab drag autoscroll on overflow" },
    @{ Issue=2171; PR=3128; Desc="Config connections panel focus" },
    @{ Issue=2166; PR=3129; Desc="Tab close race under resize" },
    @{ Issue=2155; PR=3130; Desc="Inheritance label width fix" }
)

foreach ($f in $fixed) {
    powershell.exe -NoProfile -File ".project-roadmap/scripts/Update-Status.ps1" `
        -Issue $f.Issue -Repo upstream -Status released `
        -Description $f.Desc -PR $f.PR `
        -Release "v1.79.0" -ReleaseUrl "https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.79.0" `
        -SkipComment
}
```

Also mark v1.80.0 fixes (verify each before marking):
```powershell
# v1.80.0 confirmed fixes
$v180 = @(
    @{ Issue=2142; Desc="Resize RDP connections on monitor connect/disconnect" },
    @{ Issue=2998; Desc="Self-contained build variant eliminates .NET runtime dependency" }
)
foreach ($f in $v180) {
    powershell.exe -NoProfile -File ".project-roadmap/scripts/Update-Status.ps1" `
        -Issue $f.Issue -Repo upstream -Status released `
        -Description $f.Desc -Release "v1.80.0" -SkipComment
}

# v1.80.0 likely-fixed (verify before marking):
# #1351 - SQL TRUNCATE -> DELETE safety (SQL database randomly truncated)
# #1582 - DPI PerMonitorV2 (High DPI Support)
# #2681 - Self-contained deployment (feature request - delivered!)
# #2685 - WPF splash screen not centered at scale > 100%
```

### 1.2 Update #3044 Status

Already tracked as `in-progress` with iteration 5. No DB change needed, but code fix required (see Phase 2).

---

## Phase 2: v1.80.0 Scope - Priority Fixes

### P0 - Critical (Must Fix)

| # | Issue | Title | Why Critical |
|---|-------|-------|-------------|
| 1 | **#3044** | Password comma splitting in external tools | User-reported regression. Iteration 5. Batch .cmd edge case not fixed by PR #3109. |
| 2 | **#2420** | CVE-2023-30367 - cleartext passwords in memory | Published CVE (CWE-316). Passwords visible in process memory. Upstream "working on it" since 2024. |

**#3044 Action:** Fix the batch `.cmd` argument splitting. The current escaping in `ExternalToolArgumentParser` handles PowerShell/cmd but not batch file `%1%` parameter splitting on commas. Need to wrap the entire argument in quotes or use a different invocation strategy for `.cmd` files.

**#2420 Action:** This is a deep architectural issue (passwords stored as `System.String` which is immutable and stays in memory). Full fix requires `SecureString` or pinned byte arrays with explicit zeroing. Assess scope:
- If fixable for v1.80.0: implement and document
- If too large: document our analysis, post a detailed comment on the issue explaining the technical challenge, and target v1.81.0

### P1 - Security (Should Fix)

| # | Issue | Title | Assessment |
|---|-------|-------|-----------|
| 3 | **#1085** | CSV export saves passwords in plaintext | Add warning dialog before CSV export, or encrypt/mask passwords in CSV output |
| 4 | **#2274** | Special char password fails (paragraph sign) | Encoding issue in password transmission - likely fixable |
| 5 | **#2585** | CVE-2020-24307 + CVE-2023-30367 verification | Ask reporter to retest on v1.79.0; may be partially addressed |
| 6 | **#1283** | NRE on DB upgrade (plaintext password storage) | Old issue (1.76.x) - verify if still reproduces on current version |
| 7 | **#918** | Display/reveal password in UI | Feature request, not vulnerability - defer to v1.81.0 |
| 8 | **#1449** | File integrity hash for releases | We now have SignPath code signing - partially addresses this. Post comment. |

### P2 - Bug Fixes (Target v1.80.0)

| # | Issue | Title | Assessment |
|---|-------|-------|-----------|
| 9 | **#2907** | Options panel freezes | Recent regression in 1.78.2. Cluster with #2910, #2914. WinForms panel caching/disposal race. |
| 10 | **#2910** | "Always show panel tabs" corrupts Options panel | Same root cause as #2907 - panel visibility state management |
| 11 | **#2914** | Empty Options panel when Theme canceled | Same cluster - theme cancel doesn't restore panel state |
| 12 | **#2913** | SQL Server options fields disabled | UI bug - fields stay disabled when SQL mode enabled |
| 13 | **#2858** | Panel middle-click close crash | Verify if PR #3106 (close panel race) covers this trigger path |
| 14 | **#1113** | RDP loadbalanceinfo internal error | UTF-8 encoding fix partially helps. Investigate further. |
| 15 | **#2046** | SSH key token for external tools | Natural extension of #1634 (PROTOCOL token, already fixed). Add %PUTTYSESSION% token. |

### P3 - Communication (Self-Contained Build)

| # | Issue | Title | Action |
|---|-------|-------|--------|
| 14 | **#2681** | Self-contained deployment request | v1.80.0 delivers this! Post comment with build details when released. |
| 15 | **#2998** | WindowsRegistryTests / .NET runtime issues | Self-contained build eliminates this. Post comment. |

---

## Phase 3: Bulk Triage (Days 2-3)

### 3.1 Close Duplicates (~6 issues)

Issues that are duplicates of others or were fixed by the same PR:

**Method:** For each, run:
```powershell
powershell.exe -NoProfile -File ".project-roadmap/scripts/Update-Status.ps1" `
    -Issue <NUM> -Repo upstream -Status duplicate `
    -Description "Duplicate of #<ORIGINAL>" -PostComment
```

Specific candidates (all still open in upstream):
- **#520** - Alt Tab on Windows 10 (2017, duplicate)
- **#1684** - Panel close when last tab closes -> duplicate of **#2160** (fixed in PR #3126)
- **#1837** - Can't find Use VM ID property (2020, duplicate)
- **#1874** - Use stored credentials in hostname (2020, duplicate)
- **#2537** - log4net.dll old version (2023, duplicate + RTFM)
- **#3051** - "Secret" in SecurityPage.Designer.cs -> false positive (Won't Fix)
- **#3062** - Disposed ConnectionWindow on close -> duplicate of **#3069** (fixed in PR #3106)
- **#2706** - Crash closing tab with connections -> duplicate of **#3069** (fixed in PR #3106)

### 3.2 Close Not-Planned (~14 issues)

Issues that are out of scope, superseded by design changes, or for removed features:

**Method:** For each, run:
```powershell
powershell.exe -NoProfile -File ".project-roadmap/scripts/Update-Status.ps1" `
    -Issue <NUM> -Repo upstream -Status wontfix `
    -Description "Out of scope / superseded by current architecture" -PostComment
```

Specific candidates:
- **#2664** - Win11 24H2 breaks RDP -> Windows Update issue (fixed by KB5044285), not our bug
- **#3088** - Auth error 0x80080005 -> Windows Update KB5074109 issue, cannot reproduce
- **#2653** - RDP clipboard not working Server 2025 -> stale, workaround confirmed (rdpclip.exe)
- **#2610** - "New release at some point?" -> stale meta-discussion, v1.79.0 exists now
- **#2833** - Dependency Dashboard (Renovate bot) -> automated tracking, not a real issue
- **#2662** - Missing search results on docs page -> website issue, not code
- **#2818** - CSP header on website -> infrastructure, not application
- **#3020** - Network printer redirection -> stale, no repro, likely Windows/driver
- **#2576** - RD Gateway failure -> stale question, likely Windows config
- Feature requests for protocols we don't maintain (VNC plugins, specific SSH libs)
- Requests superseded by .NET migration (WinForms-specific workarounds)

### 3.3 Batch Review: Stale Need2Check (~167 issues)

**Label:** `Need 2 check` - These are unverified reports, mostly from 2016-2020.

**Strategy:**
1. Export list from DB: all issues with label `Need 2 check` and no activity since 2022
2. For each, check if the described behavior still exists in current version
3. Categories:
   - **Cannot reproduce on current version** -> Close with comment "Cannot reproduce on v1.79.0+. Please reopen if still occurring."
   - **Likely fixed by our changes** -> Mark as released with reference to relevant PR
   - **Still valid** -> Re-label as Bug or Enhancement and triage normally
4. **Batch comment template:**
   ```
   This issue was reported against an older version of mRemoteNG. We've made significant
   changes in v1.79.0 and v1.80.0 including [relevant area] improvements.

   If you're still experiencing this issue on the latest version, please reopen
   with updated reproduction steps. Otherwise, we'll close this in 14 days.

   Download latest: https://github.com/robertpopa22/mRemoteNG/releases
   ```

### 3.4 Defer Ancient Enhancements (~106 issues)

**Criteria:** Label `Enhancement` or `Feature Request`, created before 2020, no recent activity.

**Strategy:**
1. Review each for relevance to current architecture
2. Categories:
   - **Still relevant, easy** -> Keep open, label with target release
   - **Still relevant, hard** -> Add to backlog, label `future`
   - **No longer relevant** -> Close with explanation
   - **Superseded** -> Close, reference what superseded it

### 3.5 Re-triage Stale In-Progress (~59 issues)

**Criteria:** Label `In progress` or `In development` but no commits/PRs in 2+ years.

**Action:** These were claimed by old maintainers who are no longer active. Reset to open/triaged status so they can be properly prioritized.

### 3.6 Label Unlabeled Issues (~7 issues)

Small batch - add appropriate labels based on content analysis.

### 3.7 Answer Support Questions (~9 issues)

Issues that are actually support questions, not bugs. Answer the question, then close.

---

## Phase 4: Communication Strategy

### 4.1 Upstream PR Status

**Critical finding:** All 26 upstream PRs (#3105-#3130) were CLOSED without merge.

**Options:**
1. **Resubmit** PRs against current upstream (if upstream is active)
2. **Accept** fork divergence and maintain our release independently
3. **Engage** upstream maintainers about collaboration

**Recommended:** Option 2 for now. Our fork has v1.79.0 released and v1.80.0 in progress. Continue maintaining independently. Revisit upstream engagement when v1.80.0 is stable.

### 4.2 Issue Comments to Post

After DB updates in Phase 1, post comments on issues we've already fixed:

| Issue | Comment |
|-------|---------|
| #2681 | "v1.80.0 includes self-contained build variant. Download: [link]" |
| #2998 | "Self-contained build in v1.80.0 eliminates .NET runtime dependency. Download: [link]" |
| #2585 | "Please retest CVEs on v1.79.0. Several memory/credential handling improvements included." |
| #1449 | "v1.79.0+ releases are code-signed via SignPath Foundation. SHA checksums included in release assets." |
| #2687 | "SQL Server SNI issue fixed in v1.79.0 (PR #3110). Please verify on latest build." |
| #2988 | "BinaryFormatter RCE: no active BinaryFormatter in runtime code. .resx schema headers only. Safe." |

### 4.3 Respond to Waiting Issues

276 issues show as "waiting for us". Most are heritage comments from 2016-2020 maintainers. Realistic approach:

1. **Recent (2024-2026):** ~30 issues - respond individually within 1 week
2. **2022-2023:** ~50 issues - batch response if still relevant
3. **2016-2021:** ~196 issues - batch close/defer per Phase 3 rules

---

## Phase 5: Ongoing Workflow

After initial triage, establish cadence:

| Frequency | Action | Script |
|-----------|--------|--------|
| **Daily** | Sync new issues + comments | `Sync-Issues.ps1 -Repos upstream` |
| **Weekly** | Analyze + report | `Analyze-Issues.ps1` then `Generate-Report.ps1` |
| **Per fix** | Update status + comment | `Update-Status.ps1 -PostComment` |
| **Per release** | Bulk update released issues | Batch `Update-Status.ps1` |

---

## Execution Timeline

| Day | Phase | Actions | Est. Issues Processed |
|-----|-------|---------|----------------------|
| **Day 1** | Phase 1 | Update 26 fixed issues in DB | 26 |
| **Day 1** | Phase 2 | Start #3044 fix, assess #2420 scope | 2 |
| **Days 2-3** | Phase 2 | Options panel cluster (#2907/2910/2914) | 3 |
| **Days 2-3** | Phase 3.1-3.2 | Close duplicates + not-planned | ~20 |
| **Days 3-5** | Phase 3.3 | Batch Need2Check review (167 issues) | ~167 |
| **Days 5-7** | Phase 3.4-3.5 | Ancient enhancements + stale in-progress | ~165 |
| **Day 7** | Phase 4 | Post communication comments | ~10 |
| **Day 7** | Phase 5 | Establish ongoing cadence | - |

**Expected outcome after 7 days:**
- DB fully up to date with v1.79.0/v1.80.0 status
- ~490 issues resolved/closed/deferred
- ~340 actionable issues remaining
- Clear v1.80.0 scope (5-8 priority fixes)
- Ongoing triage workflow established

---

## v1.80.0 Release Scope Summary

| Priority | Issues | Description |
|----------|--------|-------------|
| **P0** | #3044 | Comma password batch fix (iteration) |
| **P0** | #2420 | CVE-2023-30367 assessment (may defer to v1.81.0) |
| **P2** | #2907, #2910, #2914 | Options panel corruption cluster |
| **P2** | #1113 | RDP loadbalanceinfo |
| **P2** | #2046 | SSH key external tool token |
| **P3** | #2681, #2998 | Self-contained build (already done!) |
| **Feature** | - | Self-contained build variant (key v1.80.0 feature) |

**Total new fixes for v1.80.0:** 5-7 issues (plus the self-contained build feature)

---

*This plan was generated by the Issue Intelligence System on 2026-02-10.*
*Next step: Execute Phase 1 (DB corrections), then begin Phase 2 (priority fixes).*

# mRemoteNG - Build & Development Notes

> **Parent:** [../CLAUDE.md](../CLAUDE.md) (Gestime Ecosystem — reguli universale)

## Repository Structure
- **Origin (fork):** `robertpopa22/mRemoteNG`
- **Upstream (official):** `mRemoteNG/mRemoteNG`
- **Main branch:** `main` — active development branch (latest code, currently v1.80.1+)
- **Solution:** `mRemoteNG.sln` (.NET 10, SDK-style projects with COM references)

## Build Instructions

### IMPORTANT: Do NOT use `dotnet build` - it fails on COM references (MSB4803)

### Correct build command (PowerShell):
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "D:\github\mRemoteNG\build.ps1"
```

`build.ps1` auto-detects the newest VS installation (VS2026 > VS2022 > etc.). No hardcoded paths.

### Self-contained build (embeds .NET runtime):
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "D:\github\mRemoteNG\build.ps1" -SelfContained
```
> **IMPORTANT:** Self-contained uses `-t:Publish` (not just `-p:SelfContained=true`). Output goes to `bin\x64\Release\win-x64-sc\`. The restore MUST include `/p:PublishReadyToRun=true` or publish fails with NETSDK1094. See `.project-roadmap/LESSONS.md` for details.

### Why MSBuild (not `dotnet build`):
- **`dotnet build`** fails with `MSB4803: ResolveComReference not supported on .NET Core MSBuild`
- The project has a COM reference to `MSTSCLib` (RDP ActiveX control)
- Must use full VS BuildTools MSBuild with `.NET SDK resolver` AND `COM reference support`

### Build environments:
| Environment | VS Version | MSBuild | Status |
|-------------|-----------|---------|--------|
| **CI** (`windows-2025-vs2026`) | **VS2026** | 18.x | Production builds (x86, x64, ARM64) |
| **Local** (this machine) | **VS2022 BuildTools** | 17.x | Dev builds — compatible but older |
| `D:\BuildTools` | Incomplete | N/A | DO NOT USE (missing SDK resolver) |

> **TODO:** Install VS2026 BuildTools locally for CI/local parity.

## Testing

### Build and run tests:
```powershell
# From a VsDevShell (Launch-VsDevShell.ps1 -Arch amd64):
msbuild mRemoteNGTests\mRemoteNGTests.csproj -restore -m "-verbosity:minimal" "-p:Configuration=Release" "-p:Platform=x64"
msbuild mRemoteNGSpecs\mRemoteNGSpecs.csproj -restore -m "-verbosity:minimal" "-p:Configuration=Release" "-p:Platform=x64"
```

### Run tests (after build):
```bash
dotnet test "D:\github\mRemoteNG\mRemoteNGTests\bin\x64\Release\mRemoteNGTests.dll" --verbosity normal
dotnet test "D:\github\mRemoteNG\mRemoteNGSpecs\bin\x64\Release\mRemoteNGSpecs.dll" --verbosity normal
```

### IMPORTANT: `dotnet test` path quirk
- Build outputs to `bin\x64\Release\` (because Platform=x64)
- `dotnet test --no-build` on the .csproj looks in `bin\Release\` (WRONG)
- Always run `dotnet test` directly on the **DLL path**, not the .csproj

### Current test status (release/1.80, 2026-02-10):
- **mRemoteNGTests:** 2179 total, **2174 passed, 2 skipped, 3 ignored** (env-dependent)
- **mRemoteNGSpecs:** 5 total, 5 passed
- **Zero failures.** Skipped/ignored tests are env-dependent WinForms tests:
  - 2 skipped: CueBanner (`Assume.That` for Win32 EM_SETCUEBANNER)
  - 2 ignored: XmlConnectionsLoader recovery failure path (triggers WinForms dialog via `Runtime.MessageCollector`)
  - 1 ignored: `CanDeleteLastFolderInTheTree` (triggers WinForms confirmation dialog)
- All 81 pre-existing upstream failures resolved in commit `79c5e4cf`
- 28 new coverage tests added in commit `708a4f5c` (P7 gap analysis)
- 3 final coverage tests added (2026-02-09): OnePasswordCli null fields, malformed JSON, label fallback
- **All Priority A test coverage gaps are now CLOSED**
- Headless test command: `dotnet test ... --filter "FullyQualifiedName!~UI.Controls&FullyQualifiedName!~UI.Window&FullyQualifiedName!~CueBanner" -- NUnit.DefaultTimeout=15000`
- Coverage analysis: `.project-roadmap/P7_TEST_COVERAGE_ANALYSIS_2026-02-08.md`

### Historical baseline (upstream v1.78.2-dev, before fixes):
- **mRemoteNGTests:** 2119 total, 2038 passed, 81 failed (pre-existing upstream bugs)
- 29 new tests added during codex work (2119 → 2148)
- 28 new coverage tests added during P7 analysis (2148 → 2176)
- 3 final coverage tests added (2176 → 2179)

## CI/CD
- CI uses `windows-2025-vs2026` runners with MSBuild 18.x (VS2026)
- CI workflow: `.github/workflows/pr_validation.yml` (build) and `Build_mR-NB.yml` (release)
- Platforms: x86, x64, ARM64
- **Code signing: MANDATORY** — SignPath Foundation (free for OSS)
  - Release workflow fails if signing step fails — no unsigned binaries published
  - See `CODE_SIGNING_POLICY.md` for team roles and verification steps
  - Requires GitHub secrets: `SIGNPATH_API_TOKEN`, `SIGNPATH_ORGANIZATION_ID`
- CI does: `dotnet restore` then `msbuild` (same pattern as local build)

## Release Status (v1.80.1 Security Patch, 2026-02-13) ✅ RELEASED
- **Tag:** `v1.80.1` on `main`
- **GitHub Release:** https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.80.1
- **NB Release (CI):** tag `20260213-v1.80.1-NB-(3394)`
- **Assets:** 6 ZIPs (3 framework-dependent ~22MB + 3 self-contained ~108-118MB)
- **Architectures:** x64, x86, ARM64
- **CI Run:** All 7 jobs passed (6 builds + 1 release) — run `22001519361`
- **Key changes:** AnyDesk command injection fix, Process.Start hardening, .NET 10.0.3, 27 package cleanup
- **Upstream merge:** 25 commits from `upstream/v1.78.2-dev` (security + dependencies + CI)

## Release Status (v1.80.0 Community Edition, 2026-02-10) ✅ RELEASED
- **Tag:** `v1.80.0` on `release/1.80`
- **GitHub Release:** https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.80.0
- **NB Release (CI):** tag `20260210-v1.80.0-NB-(3389)`
- **Assets:** 6 ZIPs (3 framework-dependent ~21MB + 3 self-contained ~108-116MB)
- **Architectures:** x64, x86, ARM64
- **CI Run:** All 3 workflows passed (6/6 builds)
- **Upstream PR:** [#3134](https://github.com/mRemoteNG/mRemoteNG/pull/3134) — 177 files, 7.5K ins / 2K del (awaiting maintainer CI approval)
- **Upstream status issue:** [#3133](https://github.com/mRemoteNG/mRemoteNG/issues/3133) — updated with release link
- **Key features:** Self-contained builds, `%PUTTYSESSION%` token, Options panel fixes, batch password fix, 830-issue triage
- **Issues addressed:** #2046, #2142, #2681, #2910, #2913, #2914, #2998, #3044
- **CVE-2023-30367:** Assessed (132 refs / 54 files) — deferred to v1.81.0 (see `.project-roadmap/CVE-2023-30367_ASSESSMENT.md`)

## Release Status (v1.79.0 Community Edition, 2026-02-09)
- **Tag:** `v1.79.0` on `release/1.79`
- **GitHub Release:** https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.79.0
- **NB Release (CI):** tag `20260209-v1.79.0-NB-(3387)`
- **Assets:** 3 ZIPs (x64 ~20.9MB, x86 ~20.9MB, ARM64 ~20.8MB)
- **CI Run:** #21814376762 — all 3 architectures PASSED
- **All 26 PRs (#3105-#3130):** OPEN on upstream
- **Upstream comments:** Posted on all 24 issues with release link
- **Discussion:** https://github.com/orgs/mRemoteNG/discussions/3131
- **Fork issues:** DISABLED on fork (GitHub limitation for forks)

## Branch Strategy & Naming Convention

### Branch model
| Branch | Purpose |
|--------|---------|
| `main` | **Active development** — always contains the latest code. Default branch on GitHub. |
| `release/1.79` | Historical — v1.79.0 release (frozen) |
| `release/1.80` | Historical — v1.80.0 release (frozen, merged into main) |

- **Development happens on `main`**. No separate release branches needed unless maintaining parallel versions.
- **Releases are marked with tags** (`v1.80.0`, `v1.81.0`, etc.)
- **Upstream changes** are fetched and merged directly into `main`.

### Naming convention for feature branches
| Prefix | When | Example |
|--------|------|---------|
| `fix/<issue>-<desc>` | Bug fix | `fix/2735-rdp-smartsize-focus` |
| `feat/<issue>-<desc>` | New feature | `feat/1634-protocol-token` |
| `security/<desc>` | Security hardening | `security/ldap-sanitizer` |
| `chore/<desc>` | Infra, deps, CI | `chore/sqlclient-sni-runtime` |

**Rules:**
- Issue number after `/` when an issue exists
- No tool prefixes (`codex/`, `copilot/`) — branches belong to the project
- No PR numbers in branch names — PRs live on GitHub
- Lowercase, kebab-case, max 50 chars after prefix

### Syncing with upstream
```bash
git fetch upstream && git merge upstream/v1.78.2-dev   # on main
```

## PR Workflow
1. Create feature branch from `main`: `git checkout -b fix/<issue>-<desc>`
2. Develop and test locally (build + run)
3. Merge back into `main` (or push and create PR targeting `v1.78.2-dev` on upstream)

## Release Checklist
1. **MANDATORY: Run Issue Intelligence System sync** (see below)
2. Build all architectures (x86, x64, ARM64) — framework-dependent + self-contained
3. Run tests (2179 total, verify zero regressions)
4. Update CHANGELOG.md
5. Tag and publish GitHub release
6. **MANDATORY: Update issue statuses to `released` with `-PostComment`:**
   ```powershell
   .\.project-roadmap\scripts\Update-Status.ps1 -Issue <N> -Status released -Release "v1.80.0" -ReleaseUrl "<url>" -PostComment
   ```
7. Generate final report: `.\.project-roadmap\scripts\Generate-Report.ps1 -IncludeAll`
8. Commit all JSON changes in `.project-roadmap/issues-db/`

## v1.80.0 PR Reference
| PR# | Issue | Description |
|-----|-------|-------------|
| 3134 | multiple | **Consolidated PR** on upstream — all v1.80.0 changes (177 files) |

### Issues fixed in v1.80.0 (on top of v1.79.0)
| Issue | Description |
|-------|-------------|
| 2046 | `%PUTTYSESSION%` external tool token |
| 2142 | RDP auto-resize on monitor connect/disconnect |
| 2681/2998 | Self-contained build variant |
| 2910 | "Always show panel tabs" corrupts Options |
| 2913 | SQL Server options fields enable fix + font |
| 2914 | Options Cancel reverts theme/preview changes |
| 3044 | Batch file password comma-splitting |

### Triage completed (2026-02-10)
- 25 issues marked `released` (24 v1.79.0 + 1 v1.80.0)
- 8 issues marked `duplicate`
- 9 issues marked `wontfix`
- 830 total issues analyzed from upstream backlog

## v1.79.0 PR Reference (historical — branches cleaned up 2026-02-09)
| PR# | Issue | Description |
|-----|-------|-------------|
| 3105 | - | LDAP sanitizer and importer guardrails |
| 3106 | 3069 | Close panel race fix |
| 3107 | 3092 | 1Password parser and fallback fix |
| 3108 | 2972 | Default external provider fix |
| 3109 | - | ProcessStart hardening and escaping |
| 3110 | 3005 | SqlClient SNI runtime references |
| 3111 | 1916 | SQL schema compatibility hardening |
| 3112 | 850 | Config panel splitter width reset |
| 3113 | 1969 | Startup path fallback |
| 3114 | 822 | PuTTY provider failure handling |
| 3115 | 2785 | PuTTY CJK session name decoding |
| 3116 | 2735 | RDP SmartSize focus loss fix |
| 3117 | 847 | RDP fullscreen toggle guard |
| 3118 | 1650 | RDP refocus after fullscreen exit |
| 3119 | 2510 | RDP SmartSize RCW disconnect fix |
| 3120 | 2987 | Settings path logging |
| 3121 | 2673 | Require password before disabling protection |
| 3122 | 1649 | Master password autolock on minimize/idle |
| 3123 | 1634 | PROTOCOL external tool token |
| 3124 | 2270 | Main close cancel behavior |
| 3125 | 811 | Startup XML recovery |
| 3126 | 2160 | Empty panel close after last tab |
| 3127 | 2161 | Tab drag autoscroll on overflow |
| 3128 | 2171 | Config connections panel focus |
| 3129 | 2166 | Tab close race under resize |
| 3130 | 2155 | Inheritance label width fix |

---

## Issue Intelligence System (MANDATORY)

**IMPORTANT: This system is MANDATORY for all issue tracking, triage, and release communication.**
Do NOT manage issues manually — always use the scripts for consistency and traceability.

### What it does
- Syncs GitHub issues + comments from **both** upstream (`mRemoteNG/mRemoteNG`) and fork (`robertpopa22/mRemoteNG`)
- Stores per-issue JSON files in `.project-roadmap/issues-db/` (git-tracked, diff-friendly)
- Tracks full lifecycle: `new → triaged → roadmap → in-progress → testing → released`
- Detects **iteration loops** (user feedback after fix → re-fix cycle, e.g. issue #3044)
- Posts templated comments to GitHub on status transitions
- Generates markdown reports for triage sessions and releases

### MANDATORY workflow — every session
```powershell
# 1. Sync (ALWAYS run first — stale data = missed comments)
.\.project-roadmap\scripts\Sync-Issues.ps1

# 2. Analyze (see what needs attention)
.\.project-roadmap\scripts\Analyze-Issues.ps1

# 3. Transition issues (triage, start work, mark testing/released)
.\.project-roadmap\scripts\Update-Status.ps1 -Issue <N> -Status <status>

# 4. Report (generate markdown summary)
.\.project-roadmap\scripts\Generate-Report.ps1
```

### Scripts
| Script | Purpose |
|--------|---------|
| `Sync-Issues.ps1` | **Run FIRST.** Fetches issues+comments from both repos, updates JSON DB |
| `Analyze-Issues.ps1` | Shows what needs action: urgent, iteration needed, waiting for response |
| `Update-Status.ps1` | Transitions lifecycle, records iterations, posts GitHub comments |
| `Generate-Report.ps1` | Generates markdown reports for triage and releases |

### Key flags
- `Sync-Issues.ps1 -IssueNumbers 3044,3069` — targeted sync (fast)
- `Analyze-Issues.ps1 -WaitingOnly` — show only issues waiting for our response
- `Update-Status.ps1 -PostComment` — actually posts to GitHub (without it, preview only)
- `Update-Status.ps1 -AddToRoadmap` — adds issue to `_roadmap.json`
- `Generate-Report.ps1 -IncludeAll` — full inventory for releases

### Rules
1. **ALWAYS** run `Sync-Issues.ps1` before triage or release
2. **ALWAYS** use `Update-Status.ps1` for status changes (maintains iteration history)
3. **ALWAYS** commit JSON changes to git (they're the source of truth)
4. **NEVER** edit JSON files manually — use the scripts
5. **ALWAYS** use `-PostComment` when marking issues as `released`
6. Track iteration loops — if user says "still broken", use `Update-Status.ps1 -Status in-progress`
7. **COMMIT PER ISSUE** — After fixing an issue, run build + tests. If tests pass, commit immediately before moving to the next issue. Format: `fix(#NNNN): short description`. One issue = one atomic commit. Never batch multiple fixes together.

### Full documentation
See `.project-roadmap/issues-db/README.md` for complete schema, examples, and workflow details.

---

## .project-roadmap/ Documentation Index

**READ `.project-roadmap/LESSONS.md` BEFORE starting any build, test, CI, or release task.**

### Active Files
| File | Contents |
|------|----------|
| `.project-roadmap/issues-db/README.md` | **Issue Intelligence System** — MANDATORY for all issue tracking (schema, workflow, rules) |
| `.project-roadmap/LESSONS.md` | **Master lessons file** — fast fix map, CI/CD pitfalls, test flakiness, release workflow, version bumping, upstream communication |
| `.project-roadmap/README.md` | Entry point for the .project-roadmap workspace |
| `.project-roadmap/ISSUE_BINARYFORMATTER.md` | .NET 10 BinaryFormatter crash — issue doc, root cause, fix, long-term roadmap |
| `.project-roadmap/CVE-2023-30367_ASSESSMENT.md` | CVE-2023-30367 assessment — SecureString migration deferred to v1.81.0 (132 refs, 54 files) |
| `.project-roadmap/TRIAGE_PLAN_2026-02-10.md` | 830-issue triage plan — 5 phases, v1.80.0 scope, bulk actions |
| `CODE_SIGNING_POLICY.md` | **Mandatory** code signing policy — SignPath Foundation, team roles, verification |

### Scripts
| File | Purpose |
|------|---------|
| `.project-roadmap/scripts/Sync-Issues.ps1` | **MANDATORY** — Sync issues+comments from both repos into JSON DB |
| `.project-roadmap/scripts/Analyze-Issues.ps1` | **MANDATORY** — Analyze what needs action (urgent, iteration, triage) |
| `.project-roadmap/scripts/Update-Status.ps1` | **MANDATORY** — Transition issue lifecycle + post GitHub comments |
| `.project-roadmap/scripts/Generate-Report.ps1` | Generate markdown reports for triage and releases |
| `.project-roadmap/scripts/find-lesson.ps1` | Search lessons by keyword |
| `.project-roadmap/scripts/refresh-issues.ps1` | Legacy: fetch issue snapshot (superseded by Sync-Issues.ps1) |

### Issue Intelligence DB
| File | Purpose |
|------|---------|
| `.project-roadmap/TRIAGE_PLAN_2026-02-10.md` | **Active triage plan** — 830 issues analyzed, 5-phase execution plan, v1.80.0 scope |
| `.project-roadmap/issues-db/README.md` | **Full system documentation** — schema, workflow, rules |
| `.project-roadmap/issues-db/_meta.json` | Sync metadata: last run, stats, config, comment templates |
| `.project-roadmap/issues-db/_roadmap.json` | Prioritized items for next release |
| `.project-roadmap/issues-db/upstream/` | Per-issue JSON files from `mRemoteNG/mRemoteNG` (830 files) |
| `.project-roadmap/issues-db/fork/` | Per-issue JSON files from `robertpopa22/mRemoteNG` |
| `.project-roadmap/issues-db/reports/` | Generated markdown reports |

### Archives
| Folder | Contents |
|--------|----------|
| `.project-roadmap/historical/v1.79.0/` | v1.79.0 release cycle (26 PRs, triage, scripts, execution logs) |
| `.project-roadmap/historical/v1.80.0/` | v1.80.0 code analysis & error backlog (12 items, all resolved) |

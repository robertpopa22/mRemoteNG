# mRemoteNG - Build & Development Notes

> **Parent:** [../CLAUDE.md](../CLAUDE.md) (Gestime Ecosystem — reguli universale)

## >>> FIRST: Read the Current Plan <<<

**INAINTE de orice altceva, citeste planul curent:**
`.project-roadmap/CURRENT_PLAN.md`

Contine: obiectivul activ, ce s-a facut, unde am ramas, lectii critice, reguli de executie.
Daca nu exista acest fisier, intreaba user-ul ce plan urmam.

## Repository Structure
- **Origin (fork):** `robertpopa22/mRemoteNG`
- **Upstream (official):** `mRemoteNG/mRemoteNG`
- **Main branch:** `main` — active development branch (latest code, currently v1.81.0-beta.2)
- **Solution:** `mRemoteNG.sln` (.NET 10, SDK-style projects with COM references)

## Build Instructions

### IMPORTANT: Do NOT use `dotnet build` - it fails on COM references (MSB4803)

### Correct build command (PowerShell):
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "D:\github\mRemoteNG\build.ps1"
```

`build.ps1` auto-detects the newest VS installation (VS2026 > VS2022 > etc.). No hardcoded paths.

### Fast incremental build (skip restore):
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "D:\github\mRemoteNG\build.ps1" -NoRestore
```

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

### Build performance (Threadripper 3960X, 48 threads)
| Scenario | Time | Command |
|----------|------|---------|
| Full (restore + compile) | ~15s | `build.ps1` |
| Incremental (no restore) | ~14s | `build.ps1 -NoRestore` |
| No-op (Roslyn warm) | ~9s | `build.ps1 -NoRestore` |

**Why 48 cores don't help more:** MSBuild `-m` parallelizes at **project** level (only 3 projects in solution). Roslyn already parallelizes file compilation internally. The bottleneck is the single 587-file main project, not CPU count.

**Optimizations applied (`Directory.Build.props`):**
- `NoWarn=CA1416` — suppresses 1,795 platform compatibility warnings (app is 100% Windows-only)
- `UseSharedCompilation=true` — keeps Roslyn server warm between builds

## Testing

### Build and run tests:
```powershell
# From a VsDevShell (Launch-VsDevShell.ps1 -Arch amd64):
msbuild mRemoteNGTests\mRemoteNGTests.csproj -restore -m "-verbosity:minimal" "-p:Configuration=Release" "-p:Platform=x64"
msbuild mRemoteNGSpecs\mRemoteNGSpecs.csproj -restore -m "-verbosity:minimal" "-p:Configuration=Release" "-p:Platform=x64"
```

### Run tests (PREFERRED — parallel multi-process):
```powershell
# Headless (CI/orchestrator) — 4 parallel processes, ~46s on 24-core:
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "D:\github\mRemoteNG\run-tests.ps1" -Headless

# Full (all tests including UI):
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "D:\github\mRemoteNG\run-tests.ps1"

# Sequential (old behavior, single process):
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "D:\github\mRemoteNG\run-tests.ps1" -Sequential

# Skip build (use existing binaries):
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "D:\github\mRemoteNG\run-tests.ps1" -Headless -NoBuild
```

### Run tests (manual — single process):
```bash
dotnet test "D:\github\mRemoteNG\mRemoteNGTests\bin\x64\Release\mRemoteNGTests.dll" --verbosity normal
dotnet test "D:\github\mRemoteNG\mRemoteNGSpecs\bin\x64\Release\mRemoteNGSpecs.dll" --verbosity normal
```

### CRITICAL RULE: No Interactive Tests
**NEVER create tests that open GUI dialogs, message boxes, or require user input.**
- Tests MUST be 100% automated — no human interaction
- If a test hangs for more than 2 seconds waiting for input, it is BROKEN
- Mock or stub all UI dependencies (MessageBox, dialogs, WinForms popups)
- Use `NUnit.DefaultTimeout=15000` (15s) to catch hanging tests early
- Any test that opens `notepad.exe`, shows a password prompt, or displays a save dialog is INVALID
- Kill stale processes after test runs: `taskkill //F //IM notepad.exe 2>/dev/null`

### IMPORTANT: `dotnet test` path quirk
- Build outputs to `bin\x64\Release\` (because Platform=x64)
- `dotnet test --no-build` on the .csproj looks in `bin\Release\` (WRONG)
- Always run `dotnet test` directly on the **DLL path**, not the .csproj

### Current test status (v1.81.0-beta.3, 2026-02-15):
- **Full parallel run:** 2228/2231 passed, 3 skipped, **0 failed** — 5 processes, ~2 minutes
- **mRemoteNGSpecs:** 2/5 passed, 3 failed (pre-existing BouncyCastle GCM decryption issue)
- **No headless filter needed** — all UI tests redesigned with RunWithMessagePump pattern
- **3 [Ignore] tests** (need production code refactoring, not test exclusion):
  - `ChangingOptionMarksPageAsChanged` — ObjectListView deadlock in OptionsPage (needs RunWithMessagePump refactoring of OptionsForm)
  - `SelectingSQLPageLoadsSettings` — ObjectListView deadlock on SQL page activation
  - `OpenConnection_RetriesSshTunnel_OnFailure` — requires FrmMain/PanelAdder DI
- **RunWithMessagePump pattern**: For ObjectListView-based tests, creates dedicated STA thread with `Application.Run(form)` message pump. Tests run inside form's Load event. Required because .NET 10 NUnit `[Apartment(STA)]` doesn't provide message pump, causing `Invoke()` deadlock.
- **Run command:** `powershell.exe -NoProfile -ExecutionPolicy Bypass -File run-tests.ps1 -NoBuild`
- **IMPORTANT: Do NOT use `[assembly: Parallelizable]`** in the test project — causes race conditions on `DefaultConnectionInheritance.Instance`, `Runtime.ConnectionsService`, `Runtime.EncryptionKey` (shared mutable singletons). Use multi-process parallelism via `run-tests.ps1` instead.
- Coverage analysis: `.project-roadmap/P7_TEST_COVERAGE_ANALYSIS_2026-02-08.md`

### Historical baseline (upstream v1.78.2-dev, before fixes):
- **mRemoteNGTests:** 2119 total, 2038 passed, 81 failed (pre-existing upstream bugs)
- 29 new tests added during codex work (2119 → 2148)
- 28 new coverage tests added during P7 analysis (2148 → 2176)
- 3 final coverage tests added (2176 → 2179)
- UI tests redesigned with RunWithMessagePump in commit `5a16e801` (2179 → 2231)

## CI/CD
- CI uses `windows-2025-vs2026` runners with MSBuild 18.x (VS2026)
- CI workflow: `.github/workflows/pr_validation.yml` (build) and `Build_mR-NB.yml` (release)
- Platforms: x86, x64, ARM64
- **Code signing: MANDATORY** — SignPath Foundation (free for OSS)
  - Release workflow fails if signing step fails — no unsigned binaries published
  - See `CODE_SIGNING_POLICY.md` for team roles and verification steps
  - Requires GitHub secrets: `SIGNPATH_API_TOKEN`, `SIGNPATH_ORGANIZATION_ID`
- CI does: `dotnet restore` then `msbuild` (same pattern as local build)
- **CI reads version from `mRemoteNG.csproj`** (`<Version>` element) — no more hardcoded versions in workflow. Supports prerelease suffixes (e.g. `-beta.2`).

## Release Status (v1.81.0-beta.2, 2026-02-15) ✅ RELEASED
- **Tag:** `v1.81.0-beta.2` on `main`
- **GitHub Release:** https://github.com/robertpopa22/mRemoteNG/releases/tag/20260215-v1.81.0-beta.2-NB-(3396)
- **NB Release (CI):** tag `20260215-v1.81.0-beta.2-NB-(3396)`
- **Assets:** 6 ZIPs (3 framework-dependent + 3 self-contained)
- **Architectures:** x64, x86, ARM64
- **CI Run:** All 7 jobs passed (6 builds + 1 release) — run `22031139133`
- **Key changes:** Zero nullable warnings (2,554 fixed across 242 files), IIS Orchestrator, System.Drawing.Common security bump, upstream sync
- **Nullable cleanup:** 2,338 → 0 warnings (100%), 247 commits, 353 files changed, 4 orchestrator sessions
- **Tests:** 1926/1926 passed (headless filter)

## Release Status (v1.81.0-beta.1, 2026-02-14) ✅ RELEASED
- **Tag:** `v1.81.0-beta.1` on `main` (no CI release — superseded by beta.2)
- **Key changes:** Beta-first release strategy, reveal password feature (#918), batch password fix (#3044), CSV export security (#1085)

## Release Status (v1.80.2, 2026-02-14) ✅ RELEASED
- **Tag:** `v1.80.2` on `main`
- **Key changes:** AlwaysShowPanelTabs initialization fix (#3142)

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
2. Bump version in `mRemoteNG/mRemoteNG.csproj` (`<Version>` element) — CI reads this automatically
3. Update `CHANGELOG.md`
4. Build all architectures (x86, x64, ARM64) — framework-dependent + self-contained
5. Run tests (1926+ headless, verify zero regressions)
6. Commit, tag (`v1.XX.Y`), push — CI auto-builds 6 variants and creates GitHub release
7. **MANDATORY: Update issue statuses to `released` with `--post-comment`:**
   ```bash
   python .project-roadmap/scripts/iis_orchestrator.py update --issue <N> --status released --release "vX.Y.Z" --release-url "<url>" --post-comment
   ```
8. Generate final report: `python .project-roadmap/scripts/iis_orchestrator.py report --include-all`
9. Commit all JSON changes in `.project-roadmap/issues-db/`

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
```bash
# 1. Sync (ALWAYS run first — stale data = missed comments)
python .project-roadmap/scripts/iis_orchestrator.py sync

# 2. Analyze (see what needs attention)
python .project-roadmap/scripts/iis_orchestrator.py analyze

# 3. Transition issues (triage, start work, mark testing/released)
python .project-roadmap/scripts/iis_orchestrator.py update --issue <N> --status <status>

# 4. Report (generate markdown summary)
python .project-roadmap/scripts/iis_orchestrator.py report
```

### Subcommands
| Subcommand | Purpose |
|------------|---------|
| `sync` | **Run FIRST.** Fetches issues+comments from both repos, updates JSON DB |
| `analyze` | Shows what needs action: urgent, iteration needed, waiting for response |
| `update` | Transitions lifecycle, records iterations, posts GitHub comments |
| `report` | Generates markdown reports for triage and releases |

### Key flags
- `sync --issues 3044,3069` — targeted sync (fast)
- `analyze --waiting-only` — show only issues waiting for our response
- `update --post-comment` — actually posts to GitHub (without it, preview only)
- `update --add-to-roadmap` — adds issue to `_roadmap.json`
- `report --include-all` — full inventory for releases

### Rules
1. **ALWAYS** run `iis_orchestrator.py sync` before triage or release
2. **ALWAYS** use `iis_orchestrator.py update` for status changes (maintains iteration history)
3. **ALWAYS** commit JSON changes to git (they're the source of truth)
4. **NEVER** edit JSON files manually — use the scripts
5. **ALWAYS** use `--post-comment` when marking issues as `released`
6. Track iteration loops — if user says "still broken", use `update --issue N --status in-progress`
7. **COMMIT PER ISSUE** — After fixing an issue, run build + tests. If tests pass, commit immediately before moving to the next issue. Format: `fix(#NNNN): short description`. One issue = one atomic commit. Never batch multiple fixes together.

### Full documentation
See `.project-roadmap/issues-db/README.md` for complete schema, examples, and workflow details.

---

## Multi-Agent Orchestrator (3 Agents)

The project uses a Python orchestrator (`iis_orchestrator.py`) that drives 3 AI agents to automate issue resolution and code cleanup. Each agent has a specific role; the orchestrator verifies independently (build + test) after every change.

### Architecture
```
iis_orchestrator.py (Python — controller)
│
├── Agent 1: CODEX (OpenAI) — fast triage + simple fixes
│   ├── Triages issues: implement / wontfix / needs-info / duplicate
│   ├── Implements simple bug fixes (single-file, clear scope)
│   └── ~15-30s per triage, 3-10 min per fix
│
├── Agent 2: GEMINI CLI (Google) — bulk code transformations
│   ├── Nullable warning cleanup (CS8618, CS8602) across many files
│   ├── Handles cascading type changes (field → all usages)
│   └── Processed 466/852 CS8618 in one session
│
├── Agent 3: CLAUDE CODE (Anthropic) — complex fixes + review
│   ├── Multi-file fixes requiring architectural understanding
│   ├── UI/WinForms fixes (RunWithMessagePump, COM interop)
│   ├── Final review and correction of other agents' work
│   └── Handles edge cases other agents miss
│
└── VERIFICATION (independent, no AI)
    ├── build.ps1 → compile check
    ├── run-tests.ps1 → 2349 tests
    ├── git commit (only on green) / git restore (on failure)
    └── gh issue comment (post to upstream)
```

### Workflow
1. **Sync** — pull issues from upstream GitHub
2. **Triage** (Codex) — classify each issue, estimate files to change
3. **Implement** (Codex → Gemini fallback → Claude fallback) — fix the issue
4. **Verify** (orchestrator) — build + test independently of the agent
5. **Commit** — atomic commit per issue (`fix(#NNNN): description`)
6. **Notify** — post comment on upstream issue with beta download link

### Key rules
- Orchestrator NEVER trusts agent output — always verifies with build + test
- On failure: `git restore`, log error, skip to next issue
- One agent at a time per issue (no parallel agents on same files)
- All state tracked in `.project-roadmap/issues-db/` JSON files

### Results (v1.81.0-beta.2)
- **2,554 nullable warnings** fixed (100% clean) across 4 orchestrator sessions
- **830 issues** triaged from upstream backlog
- **3 agents** used: Codex (triage), Gemini (bulk nullable), Claude (complex fixes + review)

### Files
| File | Purpose |
|------|---------|
| `.project-roadmap/scripts/iis_orchestrator.py` | Main orchestrator script |
| `.project-roadmap/scripts/orchestrator.log` | Internal log (auto-flushed, always read THIS) |
| `.project-roadmap/scripts/orchestrator-status.json` | Machine-readable state |
| `.project-roadmap/scripts/chain-context/` | Per-session context files |

---

## .project-roadmap/ Documentation Index

**READ `.project-roadmap/LESSONS.md` BEFORE starting any build, test, CI, or release task.**

### Active Files
| File | Contents |
|------|----------|
| **`.project-roadmap/CURRENT_PLAN.md`** | **>>> PLANUL CURENT <<<** — citeste PRIMUL la fiecare sesiune! |
| `.project-roadmap/issues-db/README.md` | **Issue Intelligence System** — MANDATORY for all issue tracking (schema, workflow, rules) |
| `.project-roadmap/LESSONS.md` | **Master lessons file** — fast fix map, CI/CD pitfalls, test flakiness, release workflow, version bumping, upstream communication |
| `.project-roadmap/README.md` | Entry point for the .project-roadmap workspace |
| `.project-roadmap/ISSUE_BINARYFORMATTER.md` | .NET 10 BinaryFormatter crash — issue doc, root cause, fix, long-term roadmap |
| `.project-roadmap/CVE-2023-30367_ASSESSMENT.md` | CVE-2023-30367 assessment — SecureString migration deferred to v1.81.0 (132 refs, 54 files) |
| `CODE_SIGNING_POLICY.md` | **Mandatory** code signing policy — SignPath Foundation, team roles, verification |

### Scripts
| File | Purpose |
|------|---------|
| `.project-roadmap/scripts/iis_orchestrator.py` | **IIS** — Issue Intelligence System (sync, analyze, update, report) + AI-driven orchestrator |
| `.project-roadmap/scripts/find-lesson.ps1` | Search lessons by keyword |

### Issue Intelligence DB
| File | Purpose |
|------|---------|
| `.project-roadmap/TRIAGE_PLAN_2026-02-10.md` | Historical — 830-issue triage plan for v1.80.0 (completed) |
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

# mRemoteNG - Build & Development Notes

## Repository Structure
- **Origin (fork):** `robertpopa22/mRemoteNG`
- **Upstream (official):** `mRemoteNG/mRemoteNG`
- **Main branch:** `main` (syncs with upstream `v1.78.2-dev`)
- **Release branch:** `release/1.80` (v1.80.0 — self-contained builds + security/perf/features)
- **Previous release:** `release/1.79` (v1.79.0 cumulative release)
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

### Current test status (release/1.79, 2026-02-09):
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

## Release Status (v1.80.0 Community Edition — In Progress)
- **Branch:** `release/1.80`
- **Key feature:** Self-contained (.NET embedded) build variant
- **CI:** Dual-build matrix — 6 builds (3 framework-dependent + 3 self-contained)
- **Build variants:** Framework-dependent (~21MB) and self-contained (~80-120MB)

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

### Active branches
| Branch | Purpose |
|--------|---------|
| `main` | Default branch, syncs with `upstream/v1.78.2-dev` |
| `release/1.80` | v1.80.0 — self-contained builds + security/perf/features |
| `release/1.79` | v1.79.0 cumulative release (all 26 fixes + tests + docs) |

### Naming convention for new branches
| Prefix | When | Example |
|--------|------|---------|
| `fix/<issue>-<desc>` | Bug fix | `fix/2735-rdp-smartsize-focus` |
| `feat/<issue>-<desc>` | New feature | `feat/1634-protocol-token` |
| `security/<desc>` | Security hardening | `security/ldap-sanitizer` |
| `chore/<desc>` | Infra, deps, CI | `chore/sqlclient-sni-runtime` |
| `release/<version>` | Release branch | `release/1.80` |

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
1. Create branch from `main`: `git checkout -b fix/<issue>-<desc>`
2. Test locally (build + run)
3. Push and create PR targeting `v1.78.2-dev` on upstream `mRemoteNG/mRemoteNG`

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

## .project-roadmap/ Documentation Index

**READ `.project-roadmap/LESSONS.md` BEFORE starting any build, test, CI, or release task.**

### Active Files
| File | Contents |
|------|----------|
| `.project-roadmap/LESSONS.md` | **Master lessons file** — fast fix map, CI/CD pitfalls, test flakiness, release workflow, version bumping, upstream communication |
| `.project-roadmap/README.md` | Entry point for the .project-roadmap workspace |
| `.project-roadmap/ISSUE_PACKAGES.md` | Triage methodology and issue package definitions |
| `.project-roadmap/P7_TEST_COVERAGE_ANALYSIS_2026-02-08.md` | Test coverage gaps — drives next test work |
| `.project-roadmap/UPSTREAM_PR_PACKAGES_2026-02-07.md` | Catalog of all 26 upstream PRs |
| `.project-roadmap/ISSUE_BINARYFORMATTER.md` | .NET 10 BinaryFormatter crash — issue doc, root cause, fix, long-term roadmap |
| `.project-roadmap/COMMAND_FEEDBACK_LOG.md` | Command failure history for troubleshooting |
| `CODE_SIGNING_POLICY.md` | **Mandatory** code signing policy — SignPath Foundation, team roles, verification |

### Scripts (reusable automation)
| File | Purpose |
|------|---------|
| `.project-roadmap/scripts/nx.cmd` | Command wrapper with PATH fixes |
| `.project-roadmap/scripts/log-command-feedback.ps1` | Log command failures |
| `.project-roadmap/scripts/find-lesson.ps1` | Search lessons by keyword |
| `.project-roadmap/scripts/refresh-command-feedback-metrics.ps1` | Refresh error metrics |
| `.project-roadmap/scripts/refresh-issues.ps1` | Fetch fresh issue snapshot from upstream |
| `.project-roadmap/scripts/refresh-p1-p5.ps1` | Generate P1-P5 package snapshots |

### Archived (v1.79.0 release cycle)
All completed execution logs, triage records, and release checklists are in:
**`.project-roadmap/historical/v1.79.0/`** (13 files + comments/)

# Evidence Log — AI-Assisted Software Modernization

> **Purpose:** Comprehensive evidence trail for scientific paper documenting
> the hybrid AI+human approach to modernizing a legacy open-source project.
> All data points are verifiable through git history and CI artifacts.

## Project Overview

| Metric | Value |
|--------|-------|
| Project | mRemoteNG — multi-protocol remote connections manager |
| Original framework | .NET Framework 4.8 (WinForms) |
| Target framework | .NET 10 (WinForms, SDK-style projects) |
| Repository | `robertpopa22/mRemoteNG` (fork of `mRemoteNG/mRemoteNG`) |
| Upstream branch | `v1.78.2-dev` |
| Fork branch | `main` → `release/1.81` |

---

## Phase 1: Issue Triage & Automated Fix (2026-02-08 to 2026-02-25)

### Setup
- **Orchestrator**: Python script (`iis_orchestrator.py`) coordinating 3 AI agents
- **Agents**: Codex (OpenAI gpt-5.3-codex-spark), Gemini CLI, Claude Code (Opus/Sonnet)
- **Supervisor**: Separate process monitoring agent health, heartbeat, rate limits

### Metrics

| Metric | Value | Evidence |
|--------|-------|----------|
| Issues triaged | 838 | `.project-roadmap/issues-db/` (993 JSON files) |
| Issues addressed in code | 585 (70%) | git log + issue-db status fields |
| New tests added | +468 (2,100 → 2,568 → 6,123) | `test-config.json` |
| Commits in triage/fix phase | ~744 | `git log --oneline` count |
| Nullable warnings eliminated | 2,554 → 0 (CS8xxx) | `git log --grep="nullable"` |
| Agent fallback chains | Codex → Gemini → Claude | orchestrator logs |
| Supervisor auto-recoveries | 12 failure modes handled | `orchestrator_supervisor.py` |

### Agent Role Distribution

| Agent | Role | Strengths | Weaknesses |
|-------|------|-----------|------------|
| **Codex Spark** | Triage + single-file fixes | Speed (1000+ tok/s), cheap | No multi-file, no build verify |
| **Gemini CLI** | Bulk transformations | Large context, pattern-matching | Hallucinated completions |
| **Claude Code** | Complex multi-file + review | Reasoning, COM interop | Slower, higher cost |

### Known Failures & Regressions

| Type | Count | Detection | Examples |
|------|-------|-----------|----------|
| AI-introduced regressions | 7 | Manual testing (beta.5) | Focus steal, PuTTY root save, tab hang |
| Codex repo wipe | 1 | Supervisor detected | Full `git checkout .` wiped all changes |
| Gemini hallucinated edits | ~15 | Build verification | Invalid C# syntax, wrong namespaces |
| Failed parallelization | 3 attempts | All failed | NuGet locks, merge conflicts, build contention |

---

## Phase 2: Code Quality — Zero Warnings (2026-02-28 to 2026-03-01)

### Starting State
- **5,247 analyzer warnings** (CA/MA/RCS rules from Roslynator 4.12.11 + Meziantou 2.0.194)
- Analyzers enabled via `Directory.Build.props`: `EnforceCodeStyleInBuild=true`, `AnalysisLevel=latest-recommended`

### Execution (6 batches in ~8 hours)

| Batch | Rules | Files | Warnings Fixed | Method |
|-------|-------|-------|----------------|--------|
| 1 | CA1507 (nameof) | 1 | 478 → 0 | Claude Code agent |
| 2 | CA1822 (static) | 80+ | 351 → 0 | Claude Code agent |
| 3 | CA1805, CA1510, CA2263, CA1825 | 40+ | 468 → 0 | Claude Code agent |
| 4 | CA1305, MA0006, CA1309, CA1310, MA0074, CA1806, CA2201, CA1069 | 60+ | ~1,200 → 0 | Claude Code agent |
| 5 | MA0002, MA0015, MA0016, RCS1075 | 30+ | ~500 → 0 | Claude Code agent + .editorconfig |
| 6 | Remaining (CA1513, CA2249, CA1868, CA1872, CA1850, CA1869, CA2215, RCS1075, CA2208) | 15 | ~119 → 0 | Claude Code agent |

### Suppression Strategy
- **46 rules suppressed** in `.editorconfig` (severity = none or suggestion)
- Criteria: architectural patterns inherent to WinForms legacy code (e.g., ConfigureAwait, IFormatProvider in UI code)
- **Zero suppressions in test code** (separate editorconfig)

### Final State
- **0 analyzer warnings** in main project (`mRemoteNG/`)
- **0 analyzer warnings** in test project (`mRemoteNGTests/`)
- **6,123 tests** passing, 0 failures

### Key Technical Discoveries

1. **NUnit `!~` filter operator is broken** — `FullyQualifiedName!~Name` silently ignored by NUnit3TestAdapter. Fix: use `Name!=TestName`. Commit: `5b2fea157`
2. **Lambda parameter `_` shadows discard** — `(hWnd, _) => { _ = Method(); }` refers to lambda param, not discard. Fix: rename to `lParam`. Commit: `0491cfea7`
3. **IList<T> lacks AddRange/Sort** — changing `List<T>` properties to `IList<T>` breaks callers. Commit: `c7100e10f`
4. **RCS1075 catch blocks** — `catch { _ = 0; // comment }` — closing brace inside comment. Commit: `c7100e10f`

---

## Phase 3: CI Hardening (2026-03-01)

### Issues Fixed

| Issue | Root Cause | Fix | Commit |
|-------|-----------|-----|--------|
| Nightly x64 smoke test crash | `dotnet restore` separate from `msbuild` doesn't handle COM refs | `msbuild /restore` combined | `181bc8782` |
| Nightly x64 empty exit code | WinExe app crashes with `&` operator (no console) | `Start-Process -Wait -PassThru` | `9073cbb5d` |
| PR_Validation x86 failure | No x86 .NET Desktop Runtime on 64-bit CI runner | Skip smoke test for x86 | `181bc8782` |
| SonarCloud secrets in run | `${{ secrets.* }}` expanded inline | Use `$env:` env vars | `bcc60b9bd` |
| SonarCloud SHA pinning | Mutable version tags (`@v2`) | Full commit SHA + version comment | `bcc60b9bd` |
| SonarCloud bugs | Redundant null check, dead code, empty methods | Code fixes | `bcc60b9bd` |

### Final CI State (all SUCCESS)

| Workflow | Status | Time |
|----------|--------|------|
| PR_Validation (x64, ARM64) | ✅ | ~9 min |
| PR_Validation (x86 build only) | ✅ | ~5 min |
| Nightly Build (x64 + tests + release) | ✅ | ~13 min |
| SonarCloud Analysis | ✅ | ~11 min |
| CodeQL Security Analysis | ✅ | ~12 min |
| Secret Scanning (gitleaks) | ✅ | ~20 sec |

---

## Phase 4: Upstream PR (2026-03-01)

### Branch Strategy
- `release/1.81` created from `main`, excludes `.project-roadmap/` (internal orchestrator)
- Same branch used for release AND upstream PR

### PR #3189 to mRemoteNG/mRemoteNG
- **Base**: `v1.78.2-dev`
- **Head**: `robertpopa22:release/1.81`
- **Stats**: 761 files changed, 64,008 insertions, 16,765 deletions
- **URL**: https://github.com/mRemoteNG/mRemoteNG/pull/3189

### Previous PR #3188 (closed)
- Maintainer **Kvarkas** requested: "please review sonarqubecloud mentioned issues"
- SonarCloud Quality Gate failed: 25 security hotspots (mostly from `.project-roadmap/` scripts)
- **Resolution**: Excluded internal files, fixed source code issues, created clean branch

---

## Phase 5: Post-Release Quality Consolidation (2026-03-02)

### Release State
- **v1.81.0 released** — no longer beta, stable tag on `release/1.81`
- **v1.82.0-beta.1** — active development on `main`

### Metrics

| Metric | Value | Evidence |
|--------|-------|----------|
| Tests | 6,123 passed, 0 failures | `run-tests.ps1 -Headless` |
| Analyzer warnings | 0 (5,247 eliminated) | `msbuild` clean build |
| CI workflows | 6/6 GREEN | GitHub Actions dashboard |
| SonarCloud Quality Gate | PASSED (A/A/A) | SonarCloud dashboard |
| Coverage (new code) | 80.7% | SonarCloud |
| Duplication | 1.6% | SonarCloud |
| Upstream PR | #3189 open (release/1.81 → v1.78.2-dev) | GitHub |

### Post-Release Activities
1. Test count growth: 5,963 (beta.5) → 6,123 (post-release) — 160 additional tests
2. TreatWarningsAsErrors enforcement for safe compiler rules
3. Bulk verification: 179 of 195 `testing`-status issues verified via commit hash validation → promoted to `released`
4. Manual verification: Final 3 `testing` issues (#1354, #1796, #1822) verified by code review → promoted to `released`
5. Scientific paper metrics updated to reflect final state

### Human Correction Phase (2026-03-02, ~2 hours)

**Wontfix correction pass — all 123 wontfix issues reviewed individually:**

| Before | After | Delta |
|--------|-------|-------|
| 123 wontfix | 116 wontfix | -7 (already implemented in codebase) |
| 699 released | 702 released | +3 (testing→released) |
| 3 testing | 0 testing | -3 (all verified fixed) |

**Triage accuracy breakdown:**
- 76/123 (62%) correctly classified as wontfix
- 40/123 (33%) implementable — reclassified and implemented during the session
- 7/123 (5%) already implemented — AI failed to recognize existing fixes

**Key timestamps:**
- Session start: 2026-03-02 ~14:00
- Wontfix repass complete: 2026-03-02 ~15:30
- Bulk promotion (177 testing→released): commit `086d5c967`
- Wontfix repass (7 released, 116 justified): commit `63cce1d71`
- Statistics updated: commit `1f1d4ad5d`

**Efficiency comparison:**
- Orchestrator: days of autonomous operation, 182 issues left unverified in `testing`
- Human review session: ~2 hours, all 182 verified + 47 wontfix corrections
- Ratio: human session was ~1 order of magnitude more efficient for classification/verification tasks

---

## Phase 6: Qodo Code Review & VirusTotal Integration (2026-03-01 to 2026-03-06)

### Qodo Code Review — 5th Quality Level

**Setup:** GitHub App `qodo-code-review` installed on `robertpopa22/mRemoteNG`. On-demand via `scripts/qodo-review.sh`.

**PR #3189 review findings (5 issues):**

| # | Finding | Category | Static analysis caught? | Status |
|---|---------|----------|------------------------|--------|
| 1 | XmlConnectionsDeserializer rethrows XmlException | Error handling | No | Already fixed |
| 2 | SQL INSERT missing 6 columns (schema mismatch) | **Logic bug** | **No** (SonarCloud + CodeQL missed) | **Fixed** `c362601a9` |
| 3 | External tool credentials in plaintext | Security | No (DPAPI wrapper) | Already fixed |
| 4 | CertificateCryptographyProvider bounds check | Validation | No | Already fixed |
| 5 | KdfIterations unbounded (DoS risk) | Security | No | Already fixed |

**Key evidence:** Finding #2 demonstrates that AI code review catches cross-file semantic bugs that rule-based static analyzers miss.

### VirusTotal — Antivirus False Positive Resolution

**Hardening commit:** `c8194595b` (2026-02-27) — `keybd_event`→`SendInput`, `DefaultDllImportSearchPaths(System32)`, removed `WH_KEYBOARD_LL`, constrained `AssemblyResolve`.

**VirusTotal scan timeline:**

| Date | Scan | Result | Evidence |
|------|------|--------|----------|
| 2026-03-03 | Nightly x64 20260304 | **8/66 flagged** | All BitDefender engine family (`IL:Trojan.MSILZilla`) + Xcitium |
| 2026-03-05 | Same build | Xcitium confirmed fix | Email response from Xcitium threat labs |
| 2026-03-06 | Same build, rescanned | **0/75 clean** | [VT link](https://www.virustotal.com/gui/file/026b8a161db68b88e5fff3b734d7d5c7c34168384327e0bf3c53b11d26df5881) |

**Vendor cascade evidence:** BitDefender engine licenses to 6+ OEM vendors. Single FP report to BitDefender → 7/9 detections resolved in 24-48h:
- **BitDefender fix:** ALYac, Arcabit, Emsisoft, GData, MicroWorld-eScan, VIPRE, CTX (7 vendors)
- **Xcitium fix:** independent engine, separate submission required (1 vendor)

**ZIP SHA256:** `02817ffbbd2f8995095a44ba2ef2a16f7c03a9b9e84205e50510f83e46d5b62d`

**CI integration:** VirusTotal scan step added to nightly release workflow (`Build_mR-NB.yml`). VT API (free tier, 4 req/min).

---

## Cost & Performance Data

### Session Timeline (2026-03-01, this session)

| Time | Activity | Tool Calls |
|------|----------|------------|
| Start | Continue batch 4/5 analyzer fixes | Read, Edit |
| +1h | Batch 5 — all warnings → 0 | Agent (Claude), Build, Test |
| +2h | Test filter fix (NUnit !~ bug) | Edit, Bash |
| +3h | CI investigation: Nightly + x86 failures | gh CLI, Read |
| +4h | CI fixes: dotnet restore → msbuild /restore | Edit |
| +5h | SonarCloud fixes: SHA pinning, code smells | Edit (11 files) |
| +6h | Version bump beta.6, CHANGELOG | Edit |
| +7h | Branch release/1.81, exclude .project-roadmap | git rm, push |
| +8h | Close PR #3188, create PR #3189 | gh CLI |
| +9h | WinExe smoke test fix (Start-Process) | Edit |
| +10h | All 6 CI workflows GREEN | Verify |

### Token Economics (estimated from CLAUDE.md rules)
- Output tokens are 97% of API cost (5x input)
- Agent tool used for complex multi-file fixes (parallel subagents)
- Edit tool preferred over Write (diff-only = fewer output tokens)

---

## Artifacts for Verification

| Artifact | Location |
|----------|----------|
| Git history | `git log upstream/v1.78.2-dev..release/1.81` |
| Issue database | `.project-roadmap/issues-db/` (on `main` branch) |
| Orchestrator code | `.project-roadmap/scripts/iis_orchestrator.py` |
| Supervisor code | `.project-roadmap/scripts/orchestrator_supervisor.py` |
| CI workflows | `.github/workflows/` (7 workflows) |
| Test configuration | `test-config.json` |
| Analyzer config | `Directory.Build.props`, `.editorconfig`, `mRemoteNG/.editorconfig` |
| Nightly release | https://github.com/robertpopa22/mRemoteNG/releases/tag/nightly |
| Upstream PR | https://github.com/mRemoteNG/mRemoteNG/pull/3189 |
| Claude session transcript | `~/.claude/projects/D--github-mRemoteNG/*.jsonl` |

---

## Methodology Notes for Paper

### Hybrid AI+Human Model
1. **Human role**: Architecture decisions, manual testing, regression analysis, maintainer communication
2. **AI role**: Code generation, pattern application, triage, build verification
3. **Orchestrator role**: Coordination, fallback chains, rate limiting, progress tracking

### Key Findings
1. AI agents fix 83.3% of issues (702/843) but introduce 1.2% regression rate (7/585)
2. Automated tests catch 0% of UX/focus/COM regressions — manual testing essential
3. Parallelization of AI agents fails on shared resources (NuGet, git, build)
4. Code quality tools (analyzers) are best applied after feature work, not during
5. CI pipeline issues (COM refs, WinExe vs Exe, x86 runtime) require human debugging
6. Upstream PR acceptance requires addressing SonarCloud Quality Gate — AI orchestrator artifacts create noise
7. **AI triage is 38% imprecise on exclusion decisions** — 47/123 wontfix classifications were incorrect (33% implementable, 5% already implemented). Human correction phase is essential for classification accuracy.

### Reproducibility
- All code is in public repository
- Orchestrator scripts are self-documenting
- CI/CD is fully automated and verifiable
- Session transcripts capture every tool call and decision point

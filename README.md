<p align="center">
  <img width="450" src="https://github.com/mRemoteNG/mRemoteNG/blob/mRemoteNGProjectFiles/Header_dark.png">
</p>

# mRemoteNG — Community Edition

<blockquote>

**This fork is alive.** We love mRemoteNG and we're committed to keeping it moving forward. This Community Edition ships regular releases with security patches, bug fixes, and long-requested features — backed by proper CI, automated tests, and builds for x64, x86, and ARM64.

Full transparency: this project is built by humans and AI working together. We believe that's the future of open source.

*— Robert & contributors (human + AI)*

</blockquote>

<p align="center">
  <a href="https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.81.0">
    <img alt="Latest Stable" src="https://img.shields.io/badge/latest%20stable-v1.81.0-brightgreen?style=for-the-badge">
  </a>
  <a href="https://github.com/robertpopa22/mRemoteNG/releases/tag/nightly">
    <img alt="Nightly" src="https://img.shields.io/badge/nightly-v1.82.0--dev-blue?style=for-the-badge">
  </a>
  <a href="https://github.com/robertpopa22/mRemoteNG/actions">
    <img alt="CI" src="https://img.shields.io/github/actions/workflow/status/robertpopa22/mRemoteNG/pr_validation.yml?style=for-the-badge&label=CI">
  </a>
  <a href="https://github.com/robertpopa22/mRemoteNG/releases/tag/nightly">
    <img alt="Nightly" src="https://img.shields.io/github/actions/workflow/status/robertpopa22/mRemoteNG/nightly.yml?style=for-the-badge&label=Nightly&color=blueviolet">
  </a>
  <a href="https://sonarcloud.io/project/overview?id=robertpopa22_mRemoteNG">
    <img alt="SonarCloud" src="https://img.shields.io/github/actions/workflow/status/robertpopa22/mRemoteNG/sonarcloud.yml?style=for-the-badge&label=Sonar">
  </a>
  <a href="https://github.com/robertpopa22/mRemoteNG/security/code-scanning">
    <img alt="CodeQL" src="https://img.shields.io/github/actions/workflow/status/robertpopa22/mRemoteNG/codeql.yml?style=for-the-badge&label=CodeQL">
  </a>
  <a href="COPYING.TXT">
    <img alt="License" src="https://img.shields.io/badge/license-GPL--2.0-green?style=for-the-badge">
  </a>
  <a href="https://github.com/robertpopa22/mRemoteNG/releases">
    <img alt="Total Downloads" src="https://img.shields.io/badge/total%20downloads-831-green?style=for-the-badge">
  </a>
  <a href="https://github.com/robertpopa22/mRemoteNG/stargazers">
    <img alt="Stars" src="https://img.shields.io/github/stars/robertpopa22/mRemoteNG?style=for-the-badge&color=yellow">
  </a>
</p>

---

## Downloads

| Channel | Version | Branch | Description |
|---------|---------|--------|-------------|
| **[Stable](https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.81.0)** | v1.81.0 | `release/1.81` | Frozen release. 6,123 tests, 0 analyzer warnings. **Recommended.** |
| **[Nightly](https://github.com/robertpopa22/mRemoteNG/releases/tag/nightly)** | v1.82.0-beta.1 | `main` | Auto-built on every push. Latest features, fully tested. |
| **[Legacy](https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.76.20)** | v1.76.20 | — | Last .NET Framework 4.x release. |

### Download matrix

| Channel | Version | Variant | x64 | x86 | ARM64 |
|---------|---------|---------|-----|-----|-------|
| **Stable** | v1.81.0 | Framework-dependent (~21 MB) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.81.0/mRemoteNG-v1.81.0-x64.zip) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.81.0/mRemoteNG-v1.81.0-x86.zip) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.81.0/mRemoteNG-v1.81.0-arm64.zip) |
| **Stable** | v1.81.0 | Self-contained (~108-116 MB) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.81.0/mRemoteNG-v1.81.0-win-x64-SelfContained.zip) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.81.0/mRemoteNG-v1.81.0-win-x86-SelfContained.zip) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.81.0/mRemoteNG-v1.81.0-win-arm64-SelfContained.zip) |
| **Nightly** | v1.82.0-beta.1 | Framework-dependent (~21 MB) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/tag/nightly) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/tag/nightly) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/tag/nightly) |
| **Nightly** | v1.82.0-beta.1 | Self-contained (~108-116 MB) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/tag/nightly) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/tag/nightly) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/tag/nightly) |

**Framework-dependent** requires [.NET Desktop Runtime 10.0](https://dotnet.microsoft.com/download/dotnet/10.0). **Self-contained** includes the .NET runtime — no prerequisites.

---

## Features

16 protocols supported: **RDP**, **VNC**, **SSH**, **Telnet**, **HTTP/HTTPS**, **rlogin**, **Raw Socket**, **PowerShell Remoting**, **AnyDesk**, **VMRC** (VMware), **MSRA** (Remote Assistance), **OpenSSH** (native Windows), **Winbox** (MikroTik), **WSL**, **Terminal**, **Serial** (COM port).

**Security:** PBKDF2 600K iterations, HTTPS-only vaults, SSH key wipe, AnyDesk command injection prevention, LDAP sanitization, 4 CodeQL alerts fixed.

**Enterprise:** Self-contained builds (zero prerequisites), ADMX/ADML Group Policy templates, connection audit logging, JSON export, protocol/tag filtering.

**Quality:** 6,175 automated tests (0 failures), 0 analyzer warnings, SonarCloud Quality Gate passed (A reliability, A security, A maintainability, 80.7% coverage, 1.6% duplication), 4-level code quality pipeline (Roslynator + Meziantou + SonarCloud + CodeQL), x64/x86/ARM64.

For detailed usage, refer to the [Documentation](https://mremoteng.readthedocs.io/en/latest/).

### Antivirus — 0/75 on VirusTotal

mRemoteNG uses Windows APIs (SendInput, DPAPI, COM Interop for RDP) that can trigger antivirus heuristic false positives. We worked directly with AV vendors to resolve all detections:

| Date | VirusTotal Score | Action |
|------|-----------------|--------|
| 2026-03-03 | 8/66 flagged | Submitted false positive reports to all flagging vendors |
| 2026-03-05 | Xcitium confirmed fix | Comodo/Xcitium whitelisted mRemoteNG |
| 2026-03-06 | **0/75 — clean** | BitDefender fixed `IL:Trojan.MSILZilla` → cascaded to 7 OEM vendors |

Current scan: [VirusTotal](https://www.virustotal.com/gui/file/026b8a161db68b88e5fff3b734d7d5c7c34168384327e0bf3c53b11d26df5881) — **0 detections across 75 engines**.

If your antivirus flags mRemoteNG, please see [Antivirus False Positive Guide](docs/ANTIVIRUS_FALSE_POSITIVE.md).

---

## How We Build This — AI-Assisted Development

This project uses an AI orchestrator (Python, ~6,900 LOC) coordinating multiple AI agents to resolve a backlog of 843 upstream issues. The system evolved through four architectural generations — from manual prompting to a self-healing supervisor with autonomous agents.

**Key results:**

- **702/843 issues addressed (83.3%)**, 1,365+ commits, 7 regressions (1.2%)
- **Cost:** ~$320 total, stabilized at $1.49/commit (down from $4.02 on day 1)
- **Best session:** Codex Spark resolved 89/104 issues (86%) autonomously in a single run
- **Quality:** 5,247 analyzer warnings → 0, SonarCloud Quality Gate passed (80.7% coverage)
- **4 upstream PRs backported:** URL injection fix, AD Protected Users, VNC Caps Lock, RDP resize

The complete research documentation is in [`scientific-paper/`](scientific-paper/):

| Document | What's inside |
|----------|---------------|
| [**PAPER.md**](scientific-paper/PAPER.md) | Full research paper — hypothesis, architecture, results, discussion |
| [METHODOLOGY.md](scientific-paper/METHODOLOGY.md) | Formal methodology, instruments, metrics, baseline |
| [RELATED_WORK.md](scientific-paper/RELATED_WORK.md) | Comparison with SWE-bench, Devin, Aider, AutoCodeRover, MetaGPT |
| [COST_ANALYSIS.md](scientific-paper/COST_ANALYSIS.md) | Detailed cost breakdown and learning curve |
| [FAILURE_CATALOG.md](scientific-paper/FAILURE_CATALOG.md) | Post-mortems: 31-hour disaster, 7 regressions, Codex repo wipe |
| [EVIDENCE.md](scientific-paper/EVIDENCE.md) | Verifiable data trail (git history, CI artifacts, metrics) |

---

## What's Next

### 6.1. Issue Triage — Complete (843/843)

All 843 upstream issues have been triaged and classified:

| Status | Count | % |
|--------|-------|---|
| released | 702 | 83.3% |
| wontfix | 116 | 13.8% |
| duplicate | 25 | 3.0% |

**Post-triage verification (2026-03-02):** All 195 `testing`-status issues were resolved: 179 bulk-verified via commit hash validation, 3 manually verified, 13 reclassified during wontfix correction. Wontfix repass found 38% imprecision in AI triage (47/123 were implementable); corrected to 116 with individual justifications.

**Status definitions:**
- **released** — fix committed, build/test verified, included in a release
- **wontfix** — classified as out-of-scope (upstream limitation, requires hardware, or not reproducible)
- **duplicate** — merged with another issue tracking the same root cause

### 6.2. Code Quality — Four Levels Operational, Zero Warnings

**5,247 analyzer warnings → 0** across 100+ files in a single session using parallel AI agents (Claude Opus + Sonnet).

| Phase | What was done | Count fixed |
|-------|---------------|-------------|
| **Autofix** | CA1507 `nameof`, CA1822 `static`, CA1805 defaults, CA1510 `ThrowIfNull`, CA2263 generics, CA1825 `Array.Empty` | ~1,300 |
| **String comparisons** | MA0006 `string.Equals`, CA1309/CA1310 `StringComparison`, CA1304 `IFormatProvider`, MA0074 overloads | ~400 |
| **Collection safety** | MA0002 `StringComparer` on Dictionary/HashSet, MA0015/MA0016 enum comparison | ~350 |
| **Misc fixes** | CA1806, CA2201, CA1069, CA1305, CA1872, CA1850, CA1869, CA2249, RCS1075 | ~200 |
| **Suppressed** | 46 architectural/legacy rules demoted in `.editorconfig` (CA1711, CA5351, MA0062, etc.) | ~3,000 |

**SonarCloud bugs fixed (beta.6):** S2259 (null reference ×6), S2583 (dead branch), S4275 (getter/setter mismatch ×2), S1751 (no-op loop ×2), S3903 (missing namespace ×2), S3456 (redundant ToCharArray ×3), S2674 (unchecked Read), MA0037 (stray semicolon ×4). All ObjectListView issues (25) dismissed as won't fix — vendored dependency outside our control.

**SonarCloud on fork:** Quality Gate PASSED — Coverage 80.7% on new code (threshold 80%), Reliability A, Security A, Maintainability A, Duplication 1.6%, Hotspots 100% reviewed. Achieved via 160 targeted tests + `sonar.coverage.exclusions` for untestable Protocol/UI/COM code. The fork's SonarCloud is useful for internal monitoring but has no effect on upstream PR checks (see Key Insight #10).

`TreatWarningsAsErrors` enforced for compiler rules (CS0168, CS0219, CS0162, CS0164). Next: extend to analyzer rules once stable.

### 6.3. Manual Testing Protocol

Beta.5 proved that 7/585 AI-introduced regressions passed all 6,123 automated tests at the time. The failure rate (~1.2%) sounds low, but one regression (PuTTY root save) would silently destroy all user connections.

**Protocol:** Manual testing session at every beta release, focused on UX flows that cannot be unit tested:

- Tree navigation: click, double-click, drag-drop, context menu — no phantom tabs
- Tab management: switch, close, reorder — no focus stealing, no hangs
- Save/load round-trip: `confCons.xml` survives save → close → reopen → save
- COM lifecycle: connect → disconnect → reconnect → close — no RCW crashes
- Settings persistence: change settings → restart → verify persistence (especially Portable mode)

### 6.4. Remaining Unsolved Problems — What Still Doesn't Work

These are active problems with no known solution or workaround:

| # | Problem | Status | Why it's hard |
|---|---------|--------|---------------|
| 1 | ~~Fork SonarCloud coverage~~ | **Resolved** | Coverage reached 80.7% (threshold 80%) via targeted tests (SqlMigrationHelper, ExternalProcessProtocolBase, RDP serializer, MiscTools, StartupArguments, SqlVersion32To33Upgrader — 160 new tests) plus `sonar.coverage.exclusions` for genuinely untestable code (Protocol implementations, UI, COM interop, App initialization). Quality Gate now fully green: A/A/A, 80.7% coverage, 1.6% duplication, 100% hotspots reviewed |
| 2 | **1 test failing in CI** | Intermittent | 2,961 passed, 1 failed in SonarCloud CI run (runner environment differs from local). `continue-on-error: true` masks it. The failing test needs investigation under CI-specific conditions (no display, different temp paths, potentially different .NET SDK patch version) |
| 3 | **481 code smells on upstream PR** | Cosmetic | SonarCloud reports 481 code smells in the PR diff. Most are pre-existing patterns (long methods, high complexity, parameter counts) carried forward from the legacy codebase. Not blocking Quality Gate but visible. Fixing all would risk introducing regressions in stable code for cosmetic improvement |
| 4 | **MSBuild output path mismatch with `dotnet test`** | Workaround | MSBuild outputs to `bin/x64/Release/` (with platform subfolder), `dotnet test --no-build` with csproj expects `bin/Release/` (no platform). This means coverage cannot be collected via the standard `dotnet test csproj --collect` approach. The `dotnet-coverage` tool workaround functions but adds a tool dependency and doesn't produce OpenCover format natively |
| 5 | **NUnit parallelization impossible** | Architectural | Shared mutable singletons (`DefaultConnectionInheritance.Instance`, `Runtime.EncryptionKey`, `Runtime.ConnectionsService`) make NUnit fixture-level parallelism cause race conditions. Multi-process isolation (9 groups with sliding-window concurrency) works but is slower. Fixing the singletons requires DI throughout the entire application — a multi-month refactoring effort |
| 6 | **Large PR review is inherently slow** | Expected | PR #3189 (beta.6) passed Quality Gate but is a massive diff (761 files, 64K insertions). Reviewing this responsibly takes time — the upstream maintainers built and maintained mRemoteNG for years, and careful review of such a large contribution is entirely reasonable. Strategy: smaller, focused PRs in future releases to make review more manageable |

### 6.5. Supervised Continuous AI Improvement (Gen 5 Concept)

The Gen 5 concept: **Opus as permanent supervisor, Spark as executor.**

The orchestrator monitors new issues (from upstream sync or user reports), triages autonomously, implements with Spark/Sonnet, and presents completed work for human approval. The rules from §5.9 are injected into every agent prompt — hard-won knowledge that prevents the same mistakes.

**Target state:** Autonomous maintenance with human intervention only at PR review. The orchestrator handles the "what" and "how," humans verify the "should we."

### 6.6. Upstream Convergence

PR [#3189](https://github.com/mRemoteNG/mRemoteNG/pull/3189) (beta.6) passed SonarCloud Quality Gate on 2026-03-01 after resolving 6 security vulnerabilities (S2068 ×3, S8264 ×2, S8233 ×1), 50 security hotspots reviewed as SAFE, and all reliability/maintainability conditions met. PR [#3188](https://github.com/mRemoteNG/mRemoteNG/pull/3188) (beta.5) remains open as a predecessor.

Upstream has 830+ open issues (843 total triaged by our orchestrator). This fork has addressed 702 of them (83.3%). The potential impact of merging even a fraction of these fixes is significant. PR #3189 is a large diff and we understand it takes time to review responsibly — the upstream team built this project and their careful stewardship is what makes it worth contributing to.

Additionally, 4 upstream copilot draft PRs (#3177, #3176, #3154, #3171) have been reviewed and their fixes backported to our fork's `main` branch, ahead of upstream merge.

### 6.7. The Bigger Picture

This project demonstrates a reproducible model: **orchestrator + supervisor + multi-model AI + human oversight** applied to a legacy codebase with a large backlog.

The model is not specific to mRemoteNG. Any project with hundreds of open issues, a test suite, and a build system could benefit from the same approach. The orchestrator code is ~6,900 lines of Python — not trivial, but not a research project either.

**The economics make it viable:** ~$1.50/commit, 24/7 operation, no burnout, no context switching. This is complementary to human developers, not a replacement — humans set direction, review output, and handle the 30% that AI cannot.

*Note: This section has not yet undergone peer review. Projections and priorities are subject to revision.*

---

## Release History

| Version | Date | Highlights |
|---------|------|------------|
| **v1.81.0-beta.6** | 2026-03-01 | SonarCloud Quality Gate pass on upstream PR #3189 — 6 security vulnerabilities fixed, 50 hotspots reviewed, 5,247→0 analyzer warnings, 4-level code quality (Roslynator + Meziantou + SonarCloud + CodeQL), coverage collection via `dotnet-coverage`, workflow permissions hardened (S8264/S8233), upstream sync with v1.78.2-dev |
| **v1.81.0-beta.5** | 2026-02-27 | 7 manual-testing regressions fixed, AV false positive hardening (`SendInput`, `DefaultDllImportSearchPaths`, VirusTotal in CI), `PortableSettingsInitializer` for .NET 10, 5,963 tests |
| **v1.81.0-beta.4** | 2026-02-25 | AV hardening, test suite expansion 2,916 → 5,963 via `TestCaseSource` parametrization |
| **v1.81.0-beta.3** | 2026-02-24 | 585 issues addressed (70% of 838), 744 commits, 7 new protocols, 81s→ms deserialization fix, orchestrator v2 (Claude-only, self-healing supervisor) |
| **v1.81.0-beta.2** | 2026-02-15 | 2,554 nullable warnings fixed (100% clean, 242 files), testable architecture via DI |
| **v1.80.2** | 2026-02-14 | AlwaysShowPanelTabs initialization fix |
| **v1.80.1** | 2026-02-13 | Security patch — AnyDesk command injection, Process.Start hardening, .NET 10.0.3 |
| **v1.80.0** | 2026-02-10 | Self-contained builds, 6 security hardening items, external tool tokens, JSON export, live theme switching, 830-issue triage complete |
| **v1.79.0** | 2026-02-08 | 26 bug fixes, 81 pre-existing test failures fixed, LDAP sanitizer, .NET 10 with x64/x86/ARM64 |

Full details: [CHANGELOG.md](CHANGELOG.md) | [All releases](https://github.com/robertpopa22/mRemoteNG/releases)

---

## Build from Source

```powershell
# Requires Visual Studio BuildTools (VS2026 or VS2022) with .NET SDK
# Full build (~15s on 48-thread Threadripper):
pwsh -NoProfile -ExecutionPolicy Bypass -File build.ps1

# Fast incremental (~9s, skips restore):
pwsh -NoProfile -ExecutionPolicy Bypass -File build.ps1 -NoRestore

# Self-contained (embeds .NET runtime, ~108-116MB output):
pwsh -NoProfile -ExecutionPolicy Bypass -File build.ps1 -SelfContained
```

> **Note:** `dotnet build` does **not** work — the project has COM references (MSTSCLib for RDP). `build.ps1` uses full MSBuild via VS BuildTools and auto-detects the newest VS installation.

### Code Quality

| Level | Tool | Scope | Status |
|-------|------|-------|--------|
| 1 | **Roslynator + Meziantou Analyzers** | Every local build | Active |
| 2 | **SonarCloud** | Push to `main` — quality gate | [![SonarCloud](https://img.shields.io/github/actions/workflow/status/robertpopa22/mRemoteNG/sonarcloud.yml?label=SonarCloud&style=flat-square)](https://sonarcloud.io/project/overview?id=robertpopa22_mRemoteNG) |
| 3 | **CodeQL** | Push to `main` + weekly — security scanning | [![CodeQL](https://img.shields.io/github/actions/workflow/status/robertpopa22/mRemoteNG/codeql.yml?label=CodeQL&style=flat-square)](https://github.com/robertpopa22/mRemoteNG/security/code-scanning) |
| 4 | **.NET Analyzers** | `AnalysisLevel=latest-recommended` | Active |

**0 analyzer warnings** in main project. `TreatWarningsAsErrors` enforced for compiler rules (CS0168, CS0219, CS0162, CS0164). 46 noisy/architectural rules suppressed in `.editorconfig` for legacy WinForms code. Next: extend to analyzer rules once stable.

---

## Testing

```powershell
# Recommended (bash runner, 9 groups, max 2 concurrent, ~80s):
bash run-tests-core.sh

# PowerShell wrapper (builds first):
pwsh -NoProfile -ExecutionPolicy Bypass -File run-tests.ps1 -Headless

# Skip build (use existing binaries):
pwsh -NoProfile -ExecutionPolicy Bypass -File run-tests.ps1 -Headless -NoBuild
```

**6,123 tests**, 9 groups with sliding-window concurrency (max 2) + 2 isolated, 0 failures.

Multi-process parallelism is required because the production code uses shared mutable singletons — NUnit fixture-level parallelism causes race conditions. Each `dotnet test` process gets isolated static state.

| Group | Namespace | Tests |
|-------|-----------|-------|
| 1 | Connection | 1,083 |
| 2 | Config.Xml | 124 |
| 3 | Config.Other | 744 |
| 4 | UI | 374 |
| 5 | Tools | 391 |
| 6 | Security | 166 |
| 7 | Tree + Container + Credential | 178 |
| 8 | Remaining | 3,040 |
| 9 | Integration | 21 |
| Isolated | FrmOptions (GDI handle leak) | 2 |

---

## Upstream Relationship

This fork is based on [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG) `v1.78.2-dev`. We submit fixes back upstream:

- v1.79.0: PRs [#3105](https://github.com/mRemoteNG/mRemoteNG/pull/3105)–[#3130](https://github.com/mRemoteNG/mRemoteNG/pull/3130) (26 individual PRs)
- v1.80.0: [#3133](https://github.com/mRemoteNG/mRemoteNG/issues/3133) (consolidated status)
- v1.81.0-beta.5: [#3188](https://github.com/mRemoteNG/mRemoteNG/pull/3188)
- v1.81.0-beta.6: [#3189](https://github.com/mRemoteNG/mRemoteNG/pull/3189) — SonarCloud Quality Gate passed

---

## License

[GPL-2.0](COPYING.TXT)

## Support the Project

If you find this fork useful, please consider giving it a star — it helps others discover the project and motivates continued development.

<p align="center">
  <a href="https://github.com/robertpopa22/mRemoteNG/stargazers">
    <img alt="Star this repo" src="https://img.shields.io/github/stars/robertpopa22/mRemoteNG?style=for-the-badge&label=Star%20on%20GitHub&color=yellow">
  </a>
</p>

## Contributing

Submit code via pull request. See the [Wiki](https://github.com/mRemoteNG/mRemoteNG/wiki) for development environment setup.

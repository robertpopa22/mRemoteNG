# Methodology

## 1. Research Design

This study employs a **single-case observational design** applied to a real-world open-source software project. The unit of analysis is the complete issue backlog of a legacy codebase undergoing AI-assisted modernization and maintenance. The observation period spans **2026-02-08 to 2026-03-02** (23 calendar days).

The design is observational rather than experimental: we did not manipulate independent variables or assign issues to treatment/control groups. Instead, we recorded the outcomes of a fully operational AI orchestration system processing a complete issue backlog under production conditions.

A **Gen 1 baseline** (human-assisted AI development without orchestration) provides a historical comparison point, though it does not constitute a true experimental control.

## 2. Subject

The subject of this case study is **mRemoteNG**, an open-source multi-protocol remote connections manager for Windows.

| Property | Value |
|----------|-------|
| Repository | [github.com/mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG) |
| License | GPL-2.0 |
| Original framework | .NET Framework 4.8 (WinForms) |
| Current framework | .NET 10 (migrated as part of this study) |
| Source files | ~635 (main project, C#) |
| Lines of code | ~114,000 (C#, WinForms) |
| Test suite | NUnit, 6,123 tests at study conclusion |
| Protocols supported | RDP, VNC, SSH, Telnet, rlogin, HTTP/S, ICA, PowerShell |
| First release | 2008 |
| Contributors (upstream) | 80+ |

The codebase exhibits characteristics typical of long-lived open-source projects: accumulated technical debt, mixed coding styles across contributor generations, legacy Win32 interop via P/Invoke and COM references, and a large backlog of unresolved issues spanning multiple years.

## 3. Population

The study population consists of **all 843 open issues** from the upstream mRemoteNG repository at the start of the observation period.

- **Enumeration method**: Complete enumeration (census), not sampling.
- **No exclusion criteria**: Every open issue was included regardless of age, type, or perceived difficulty.
- **Issue types**: Bug reports, feature requests, enhancement suggestions, user questions, and documentation requests.
- **Age range**: Issues spanned from 2015 to 2026, with the oldest being approximately 11 years old.

Because the study processes the entire population rather than a sample, sampling bias is eliminated. However, the population is defined by the upstream project's issue management practices (which issues were opened, which were previously closed), introducing a selection effect outside the researchers' control.

## 4. Classification Scheme

Each issue was assigned exactly one disposition status upon triage completion. The classification scheme was designed to be mutually exclusive and collectively exhaustive.

| Status | Definition | Criteria | Count | % |
|--------|------------|----------|------:|----:|
| `released` | Fix committed, build and test verified, included in release branch | Green build + all tests pass + merged to release branch | 702 | 83.3% |
| `wontfix` | Out of scope for resolution | Upstream limitation, external hardware/driver requirement, not reproducible on current framework, or explicitly declined by maintainer | 116 | 13.8% |
| `duplicate` | Same root cause as another issue | Linked to a primary issue; resolution of the primary resolves the duplicate | 25 | 3.0% |

### Classification rules

1. An issue classified as `released` must have a corresponding git commit with a passing build and test suite.
2. The `testing` status was a transient state for issues whose automated fix attempts failed. All 195 issues that entered `testing` were eventually resolved: 179 bulk-verified via commit hash validation, 3 manually verified, and 13 reclassified during the wontfix correction pass (see Section 5.8 of PAPER.md, Section 7.6).
3. `wontfix` was applied by the AI triage, then corrected by human review. The initial AI classification was 38% imprecise on `wontfix` (47/123 were implementable). After human correction: 116 issues remain as `wontfix` with individual justifications.
4. `duplicate` required identification of a specific primary issue sharing the same root cause.

## 5. Resolution Pipeline

Each issue passed through a deterministic pipeline with clearly separated AI and non-AI stages:

```
GitHub Sync → AI Triage → AI Implementation → Build Verify → Test Verify → Commit → Human Review
```

### 5.1 GitHub Sync

The orchestrator synchronizes the upstream issue database using the GitHub REST API (`gh api`). Issues are pulled into a local JSON database, preserving title, body, labels, comments, and metadata. This step is fully deterministic and involves no AI.

### 5.2 AI Triage

An AI model classifies each issue by:
- **Priority**: P0 (critical) through P3 (low)
- **Estimated files**: Which source files are likely affected
- **Approach**: Suggested resolution strategy
- **Disposition**: Whether the issue can be resolved by code changes or falls into `wontfix`/`duplicate`

The triage model receives the issue text, relevant source file contents, and project context. Triage outputs are stored in the local JSON database for audit.

### 5.3 AI Implementation

An AI agent modifies the source code to resolve the issue. The orchestrator selects the agent based on task complexity:
- **Simple fixes** (single-file, well-defined): Codex Spark (fastest, lowest cost)
- **Complex fixes** (multi-file, architectural): Claude Sonnet or Claude Opus
- **Bulk transformations** (repetitive changes across many files): Gemini Pro

The agent receives: the issue description, triage output, relevant source files, and project-specific instructions (from `CLAUDE.md` / `AGENTS.md`). The agent produces source code modifications.

### 5.4 Build Verify

The modified codebase is compiled using `build.ps1`, a deterministic MSBuild wrapper. This step involves **no AI** — it is a binary pass/fail gate.

- Toolchain: MSBuild 18.x (Visual Studio 2026 Build Tools)
- Configuration: Release, x64
- Special handling: COM references (MSTSCLib) require full MSBuild, not `dotnet build`

A build failure at this stage causes the pipeline to reject the implementation and either retry with a different agent or mark the issue for human review.

### 5.5 Test Verify

The test suite is executed using `run-tests-core.sh`, running 6,123 NUnit tests organized into 9 parallel groups plus a sequential remainder group. This step involves **no AI** — it is a deterministic pass/fail gate.

- Framework: NUnit 3 with .NET test runner
- Parallelism: 9 groups run concurrently (sliding window)
- Isolation: Test results written outside the repository to prevent cascading failures
- Verbosity: `normal` (required; `quiet`/`minimal` crash the test host on .NET 10)

A test failure at this stage causes the pipeline to reject the implementation. The orchestrator may retry with a different agent or escalate to human review.

### 5.6 Commit

If both build and test verification pass, the orchestrator creates an atomic git commit with a structured commit message referencing the issue number. If either verification fails, all changes are reverted with `git restore`.

This step is fully deterministic and involves no AI.

### 5.7 Human Review

A human developer periodically reviews the accumulated commits. Review activities include:
- Reading diffs for correctness and code quality
- Manual testing of protocol-specific features at release milestones
- Reverting or amending commits that pass automated checks but contain logical errors
- Classifying `testing`-status issues as `released` after verification

Human review is not applied to every commit in real-time. Instead, it operates as a periodic quality gate, typically at the end of each working day or before release milestones. The original manual testing protocol is documented in [`MANUAL_TESTING_PROTOCOL.md`](MANUAL_TESTING_PROTOCOL.md).

### 5.8 Human Correction Phase (Post-Triage)

A systematic human review phase was added after discovering significant imprecision in the AI triage's exclusion decisions. This phase, conducted on 2026-03-02 (~2 hours), consisted of three activities:

1. **Bulk verification of `testing` issues.** 179 of 195 `testing`-status issues were verified by validating commit hashes against git history and promoted to `released`. The final 3 were verified manually by reviewing the codebase for existing fixes.

2. **Wontfix correction pass.** All 123 `wontfix` classifications were reviewed individually. 47 were found to be implementable (38% error rate): 40 were implemented during the session, 7 were already present in the codebase. The corrected `wontfix` count: 116 (each with documented justification).

3. **Statistical reconciliation.** All issue counts, resolution rates, and percentages were recalculated from the corrected data and propagated to paper, CLAUDE.md, and README.

This phase revealed that the AI triage's classification accuracy was significantly lower for exclusion decisions (`wontfix`) than for implementation decisions (`released`). The finding is documented in detail in PAPER.md Section 7.6.

## 6. Instruments

The following software instruments were used to execute and measure the resolution pipeline:

| Instrument | Version | Role | Approximate LOC |
|------------|---------|------|----------------:|
| `iis_orchestrator.py` | Gen 4 | Main orchestration loop: issue sync, triage dispatch, agent selection, build/test gating, commit management | ~6,100 |
| `orchestrator_supervisor.py` | v1 | Health monitoring with detection of 12 failure modes: hung agents, build loops, stale heartbeats, resource exhaustion | ~800 |
| `build.ps1` | — | MSBuild wrapper with auto-detection of Visual Studio installation (VS2026 preferred over VS2022) | ~50 |
| `run-tests-core.sh` | — | Test runner with 9 parallel groups, sliding window execution, coverage gap detection | ~100 |
| `cost_analysis.py` | — | Cost calculation from orchestrator logs and API billing data | ~200 |

### Supporting infrastructure

- **GitHub Actions CI**: 5 workflows (PR validation, release build, SonarCloud, CodeQL, nightly); code-signing steps are wired into the release and nightly workflows but inactive, because the SignPath Foundation application was declined
- **Local machine**: Threadripper 3960X (24 cores / 48 threads), 32 GB RAM — used for all orchestrator runs
- **Git**: Version control and audit trail for all code changes

## 7. AI Models

The orchestrator coordinated four distinct AI model families across the study period. Model selection was based on task complexity, cost constraints, and empirical performance.

| Model | Provider | Identifier | Primary Role | Cost Tier | Context Window |
|-------|----------|------------|-------------|-----------|----------------|
| Codex Spark | OpenAI | `gpt-5.3-codex-spark` | Primary implementation agent; fast single-file fixes | Low | 192K |
| Claude Sonnet | Anthropic | `claude-sonnet-4-6` | Complex multi-file implementation; fallback from Codex | Medium | 200K |
| Claude Opus | Anthropic | `claude-opus-4-6` | Supervision, complex architectural review, final review | High | 200K |
| Gemini Pro | Google | `gemini-3-pro-preview` | Bulk transformations across many files (Gen 2 role) | Medium | 2M |
| GPT-4.1 | OpenAI | `gpt-4.1` | Fast triage (deprecated mid-study in favor of Codex Spark) | Low | 128K |

### Agent selection logic

The orchestrator selects agents using a cascading strategy:
1. **Codex Spark** is attempted first for all implementation tasks (lowest cost, fastest response).
2. If Codex fails (build error, test failure, timeout), the task is **escalated to Claude Sonnet**.
3. If Claude Sonnet fails, the task is **escalated to Claude Opus** or flagged for human review.
4. **Gemini Pro** is invoked specifically for bulk transformation tasks (e.g., eliminating nullable warnings across hundreds of files).

### Model versioning note

All model identifiers refer to the specific versions available during the study period (February-March 2026). AI model capabilities evolve rapidly; results obtained with these specific model versions may not be reproducible with earlier or later versions.

## 8. Baseline

### Gen 1: Human-Assisted AI Development (Control Condition)

Before the orchestrator was operational, the project used a **manual workflow** where a human developer directed AI tools interactively:

| Property | Gen 1 (Baseline) | Gen 4 (Study) |
|----------|-------------------|---------------|
| Issue selection | Human chooses next issue | Orchestrator selects by priority |
| Triage | Human reads issue, decides approach | AI triage with structured output |
| Implementation | Human prompts AI, reviews each change | AI agent works autonomously |
| Build/test | Human triggers manually | Automated pipeline |
| Throughput | ~26 issues per sprint (~1 week) | Measured in this study |
| Human effort | ~8 hours/day active supervision | Periodic review only |

The Gen 1 baseline was measured during the initial phase of the project (first two weeks of February 2026) before the orchestrator reached operational maturity. It serves as the comparison point for evaluating the productivity impact of full orchestration.

### Limitations of the baseline comparison

The Gen 1 baseline is a historical comparison, not a concurrent control. Several confounding factors exist:
- The developer's familiarity with the codebase increased over time.
- The tooling improved incrementally (better build scripts, more tests).
- Issue difficulty is not uniformly distributed; early issues may have been systematically easier or harder than later ones.

## 9. Metrics

| Metric | Definition | Unit | Data Source |
|--------|------------|------|-------------|
| Resolution rate | `issues_resolved / total_issues` | Percentage | Issue database (JSON) |
| Regression rate | `regressions_detected / implementations_committed` | Percentage | Manual testing log, git revert history |
| Cost per commit | `total_api_spend / total_commits` | USD | API billing dashboards + `git log --oneline \| wc -l` |
| Cost per issue | `total_api_spend / issues_attempted` | USD | API billing dashboards + issue database |
| Time per task | Wall-clock seconds from agent invocation to completion (success or failure) | Seconds | `_timeout_history.json` |
| Success rate | `first_attempt_successes / total_attempts` | Percentage | Orchestrator decision logs |
| Test count | Total NUnit tests in suite at measurement time | Count | `test-config.json` |
| Build time | Wall-clock seconds for full build | Seconds | `build.ps1` output |
| Test time | Wall-clock seconds for full test suite | Seconds | `run-tests-core.sh` output |

### Metric collection frequency

- **Resolution rate**: Updated after each orchestrator run (typically daily).
- **Regression rate**: Updated after each human review session.
- **Cost metrics**: Calculated at study conclusion from cumulative billing data.
- **Time per task**: Recorded automatically for every agent invocation (220 measurements).
- **Test count**: Recorded at each milestone (monotonically increasing).

## 10. Data Collection

All data was collected through automated instrumentation built into the orchestrator and CI pipeline. No manual data entry was required except for human review observations.

### Primary data sources

| Source | Format | Contents | Retention |
|--------|--------|----------|-----------|
| Orchestrator logs | JSON | Every decision: triage result, agent selection, build/test outcome, timing | Local filesystem, committed to repo |
| `_timeout_history.json` | JSON | 220 agent invocation measurements: duration, outcome, model used | Local filesystem |
| Git history | Git objects | Every code change with timestamp, author, commit message | Permanent (git repository) |
| GitHub Actions artifacts | CI logs | Build logs, test results, static analysis reports | 90-day retention on GitHub |
| API billing dashboards | Web UI | Token usage, cost per model, daily breakdown | Provider dashboards (OpenAI, Anthropic, Google) |
| Issue database | JSON | Local mirror of all 843 issues with triage annotations | Local filesystem, committed to repo |
| `test-config.json` | JSON | Test counts, group configuration, expected results | Committed to repo |

### Data integrity

- **Orchestrator logs** are append-only during operation; no log entries are modified or deleted.
- **Git history** provides a tamper-evident audit trail via cryptographic hashing.
- **CI artifacts** are generated by GitHub's infrastructure, independent of the local environment.
- **Timing data** is collected automatically by the orchestrator with millisecond precision.

### Reproducibility

The orchestrator code, build scripts, test runner, and configuration files are all committed to the repository. Given the same model versions and API access, the pipeline could be re-executed. However, exact reproducibility is limited by:
- Non-deterministic AI model outputs (temperature > 0 in most configurations).
- Model version updates by providers (the specific model snapshots used may no longer be available).
- External dependencies (GitHub API rate limits, CI runner availability).

## 11. Limitations of Methodology

### Internal validity threats

1. **No randomized control group.** The study compares Gen 4 (orchestrated) against Gen 1 (manual) using historical data. Confounding variables — developer learning, tooling maturation, issue difficulty distribution — cannot be isolated.

2. **Observer effect.** Human supervision quality directly affects regression detection. More attentive review catches more AI-introduced bugs; less attentive review allows them to persist. The reported regression rate is a lower bound on actual regressions.

3. **Non-independent observations.** Issues are not independent: fixing one issue may make related issues easier or harder to fix. The resolution rate for later issues may be inflated by infrastructure improvements made while fixing earlier issues.

### External validity threats

4. **Single case study.** Results are derived from one project (mRemoteNG) with specific characteristics: WinForms UI, COM interop, legacy codebase, Windows-only. Generalization to other project types (web applications, microservices, mobile apps) is not supported by this data.

5. **Technology snapshot.** All AI models used are specific to February-March 2026 versions. Model capabilities change rapidly; results may not hold for earlier or later model versions.

6. **Codebase characteristics.** mRemoteNG's ~100K LOC and WinForms architecture may respond differently to AI assistance than codebases of different sizes, languages, or architectural paradigms.

### Measurement limitations

7. **Cost measurement granularity.** API billing data is aggregated daily, not per-request. Cost-per-issue calculations are estimates derived from total spend divided by issue count, not precise per-issue tracking.

8. **Regression detection lag.** Some regressions may only manifest during manual protocol testing (e.g., RDP session behavior), which occurs at release milestones rather than continuously. The true regression rate may be higher than measured.

9. **Issue difficulty is subjective.** The classification of issues as "simple" vs. "complex" is based on the AI triage output, which has not been independently validated against human expert assessment.

### Ethical considerations

10. **Open-source contribution.** All code changes produced by AI agents are contributed to an open-source project under GPL-2.0. The AI-generated nature of changes is disclosed in commit messages and project documentation.

## 12. Study Period Update (Post-Publication)

The primary study period ended on 2026-03-02. Post-study activities include:

| Metric | Study End (beta.5) | Post-Release (v1.81.0+) |
|--------|--------------------:|------------------------:|
| Tests | 5,963 | 6,123 |
| Analyzer warnings | 0 | 0 |
| CI workflows green | 6/6 | 6/6 |
| SonarCloud Quality Gate | PASSED | PASSED |
| TreatWarningsAsErrors | not enforced | enforced (CS0168, CS0219, CS0162, CS0164) |
| Analyzer rules as error | none | 4 (CA1507, CA1822, CA1805, CA1510) |

### Post-release quality improvements

1. **Test suite growth:** 160 additional tests (5,963 → 6,123)
2. **WarningsAsErrors enforcement:** Safe compiler rules promoted to errors
3. **Analyzer error promotion:** 4 clean rules promoted from warning to error severity
4. **Issue verification:** All 195 `testing`-status issues resolved — 179 bulk-verified via commit hash validation, 3 manually verified, 13 reclassified during wontfix correction (see [`MANUAL_TESTING_PROTOCOL.md`](MANUAL_TESTING_PROTOCOL.md))
5. **Wontfix correction:** 123 → 116 after human review (7 already implemented, 40 implemented during review). AI triage was 38% imprecise on exclusion decisions.
6. **Final resolution rate:** 702/843 (83.3%), 0 testing issues remaining

These changes do not affect the primary study findings (hypothesis testing, cost analysis, regression rates) which are based on data from the study period. However, the triage accuracy finding (Section 7.6 of PAPER.md) is a significant post-study observation that informs future methodology.

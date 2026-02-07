# mRemoteNG Fork Modernization Master Plan

Owner: `robertpopa22` fork  
Execution branch: `codex/release-1.79-bootstrap`  
Primary objective: ship a stable release line with security fixes and controlled backlog.

## 1. Strategy

Fork strategy is valid and recommended as a "release fork":
- Keep sync with upstream `v1.78.2-dev`.
- Prioritize shipping quality/stability over large feature merges.
- Introduce predictable release cadence and CI quality gates.

Target outcome:
- Stable `v1.79.0` release from fork.
- Zero open P0/P1 security issues in fork scope.
- Backlog triaged to actionable set (duplicate/stale/no-repro cleaned).

## 2. Workstreams

## WS-A: Build and CI Stabilization

Scope:
- Resolve framework mismatches.
- Ensure x64 and arm64 build reliability.
- Ensure tests are executable in CI.

Entry criteria:
- Current baseline captured in `WORK_STATE.md`.

Exit criteria:
- `dotnet build`/`msbuild` passes for target matrix used by release.
- Test projects restore and run (or are explicitly excluded with rationale).
- CI workflow enforces build on PRs.

## WS-B: Security and Hardening

Scope:
- Integrate or port pending security PRs.
- Validate high-risk findings and close false positives with evidence.

Entry criteria:
- WS-A baseline green enough for safe merge and verification.

Exit criteria:
- Critical security issues in scope are fixed or formally closed with evidence.
- Security-focused regression tests added where feasible.

## WS-C: Release Readiness

Scope:
- Freeze large risky features.
- Produce candidate build, smoke test, and release notes.

Exit criteria:
- RC passes smoke test checklist.
- `v1.79.0` published with changelog and known limitations.

## WS-D: Backlog Triage and Cleanup

Scope:
- Group open issues into intervention packages.
- Close duplicates/stale/no-repro.
- Reclassify old version-tagged issues.

Exit criteria:
- No obvious duplicate issues open.
- "Need 2 check" reduced to active reproducible set.
- "In progress"/"In development" labels reflect real current work.

## 3. Phased Execution

## Phase 0 - Baseline and Governance

Actions:
- Create persistent docs/scripts workspace (`NEXTUP/*`).
- Snapshot issues and package lists to local analysis path.
- Define acceptance gates for each phase.

Deliverables:
- `NEXTUP/` folder complete and usable.
- Reproducible issue snapshot script.

## Phase 1 - Technical Foundation

Actions:
- Align target frameworks to remove restore incompatibilities.
- Resolve arm64 build failure.
- Add/repair PR CI workflow (build + tests).

Acceptance:
- Tests and specs restore successfully.
- x64 build green.
- arm64 status explicit: green or blocked with tracked action.

## Phase 2 - Security Integration

Actions:
- Integrate PR #3038 and PR #3054 (or equivalent fixes).
- Review critical issues #3080, #2989, #2988 with code-level evidence.

Acceptance:
- Security package closed in fork.
- New regression tests for fixed vectors.

## Phase 3 - Release Candidate

Actions:
- Select minimal safe change-set for `v1.79.0`.
- Produce release candidate artifacts.
- Execute smoke protocol matrix.

Acceptance:
- RC sign-off checklist complete.
- Release notes finalized.

## Phase 4 - Backlog Reduction

Actions:
- Execute issue packages in waves.
- Enforce triage template for reproducibility.

Acceptance:
- Duplicate package closed.
- Old stale "Need 2 check" substantially reduced.
- Label hygiene restored.

## 4. Rules of Execution

- No large feature merges before stable release (notably PR #2997/#3001).
- Each phase must produce verifiable evidence (command output, links, commit).
- Any blocker must be recorded in `WORK_STATE.md` with next action owner.
- Keep branch linear and auditable: small focused commits.

## 5. Definition of Done

Project considered "brought up to date" for this fork when:
- Stable release published from fork.
- Security-critical set resolved.
- CI quality gates active and green.
- Backlog triaged and actively maintained (not abandoned).

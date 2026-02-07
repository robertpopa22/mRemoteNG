# Work State Tracker

Last updated: 2026-02-07 (session 4)  
Branch: `codex/release-1.79-bootstrap`

## Current Objective

Phase 1: technical foundation stabilization (build/test/CI).

## Baseline Evidence

- Fork: `https://github.com/robertpopa22/mRemoteNG`
- Upstream remote configured to `mRemoteNG/mRemoteNG`.
- Open issues observed upstream: `830`.
- x64 solution build: passes with warnings.
- arm64 solution build: passes with warnings.
- `dotnet build` is not sufficient for this solution because of COMReference (`MSB4803`); full MSBuild is required for reliable validation.

## Completed

- [x] Create persistent modernization workspace in `NEXTUP/`.
- [x] Maintain local analysis home at `D:\github\LOCAL\analysis\mRemoteNG`.
- [x] Capture issue package baseline and counts.
- [x] Add reproducible issue snapshot script: `NEXTUP/scripts/refresh-issues.ps1`.
- [x] Align TFM to `net10.0-windows10.0.26100.0` in:
  - `mRemoteNGTests/mRemoteNGTests.csproj`
  - `mRemoteNGSpecs/mRemoteNGSpecs.csproj`
  - `ExternalConnectors/ExternalConnectors.csproj`
  - `ObjectListView/ObjectListView.NetCore.csproj`
- [x] Remove direct WiX installer project dependency from tests.
- [x] Preserve installer unit coverage by compiling linked `InstalledWindowsUpdateChecker.cs` in tests.
- [x] Reintroduce SpecFlow package references for specs.
- [x] Fix test compile errors exposed after framework alignment.
- [x] Verify with full MSBuild (Release|x64):
  - `mRemoteNGTests` compiles
  - `mRemoteNGSpecs` compiles
  - `mRemoteNG.sln` builds
- [x] Resolve arm64 satellite resource build blocker by forcing core satellite assembly generation for arm64:
  - `mRemoteNG/mRemoteNG.csproj`
- [x] Verify with full MSBuild (Release|ARM64):
  - `mRemoteNG/mRemoteNG.csproj` builds
  - `mRemoteNG.sln` builds
- [x] Add PR validation workflow with full MSBuild gates:
  - `.github/workflows/pr_validation.yml`
  - build matrix for solution (`x64`, `ARM64`)
  - explicit test/spec build job (`x64`)
- [x] Validate first GitHub Actions execution for PR validation workflow:
  - Run: `PR_Validation` #`21781330218`
  - Result: `success`
  - URL: `https://github.com/robertpopa22/mRemoteNG/actions/runs/21781330218`

## In Progress

- [ ] Start security package P0 execution.

## Blockers

- Current release workflow is often skipped on regular pushes (trigger condition dependency).
- High warning volume remains (nullable/platform analyzer warnings), though x64 build is green.

## Immediate Next Actions

1. Start security package P0 execution:
   - PR #3038
   - PR #3054
   - issues #2988, #2989, #3080
2. Start duplicate cleanup package P1 (6 currently open).
3. Review and prioritize warning reduction backlog (focus on high-signal nullability and package compatibility).

## Decision Log

- 2026-02-07: Chosen strategy is release-focused fork, with upstream sync but independent stable cadence.
- 2026-02-07: Deferred large feature PR merges (#2997, #3001) until after stable release.
- 2026-02-07: Kept .NET baseline on `net10` (current stable major).
- 2026-02-07: Full MSBuild standardized as validation path for this repo.
- 2026-02-07: Fixed arm64 ALINK blocker by enabling `GenerateSatelliteAssembliesForCore` under arm64 platform condition.
- 2026-02-07: PR validation workflow is live and green on first run.

## Resume Checklist (after reboot)

1. `cd /d D:\github\mRemoteNG`
2. `call D:\github\LOCAL\env.cmd`
3. `git status -sb`
4. Open this file and execute the first unchecked item in `Immediate Next Actions`.

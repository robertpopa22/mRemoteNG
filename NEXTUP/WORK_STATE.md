# Work State Tracker

Last updated: 2026-02-07 (session 3)  
Branch: `codex/release-1.79-bootstrap`

## Current Objective

Phase 1: technical foundation stabilization (build/test/CI).

## Baseline Evidence

- Fork: `https://github.com/robertpopa22/mRemoteNG`
- Upstream remote configured to `mRemoteNG/mRemoteNG`.
- Open issues observed upstream: `830`.
- x64 solution build: passes with warnings.
- arm64 build: fails with `ALINK : error AL1012: 'ARM64' is not a valid setting for option 'platform'`.
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

## In Progress

- [ ] Arm64 build remediation (`ALINK` platform issue).
- [ ] PR CI workflow hardening (full MSBuild + explicit test jobs).

## Blockers

- arm64 build not releasable due to ALINK platform error.
- Current release workflow is often skipped on regular pushes (trigger condition dependency).
- High warning volume remains (nullable/platform analyzer warnings), though x64 build is green.

## Immediate Next Actions

1. Add/adjust PR CI workflow to use full MSBuild (Windows) and include test/spec build jobs.
2. Diagnose and isolate arm64 ALINK fix candidate.
3. Start security package P0 execution:
   - PR #3038
   - PR #3054
   - issues #2988, #2989, #3080
4. Start duplicate cleanup package P1 (6 currently open).

## Decision Log

- 2026-02-07: Chosen strategy is release-focused fork, with upstream sync but independent stable cadence.
- 2026-02-07: Deferred large feature PR merges (#2997, #3001) until after stable release.
- 2026-02-07: Kept .NET baseline on `net10` (current stable major).
- 2026-02-07: Full MSBuild standardized as validation path for this repo.

## Resume Checklist (after reboot)

1. `cd /d D:\github\mRemoteNG`
2. `call D:\github\LOCAL\env.cmd`
3. `git status -sb`
4. Open this file and execute the first unchecked item in `Immediate Next Actions`.

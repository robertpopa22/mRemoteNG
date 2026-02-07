# Work State Tracker

Last updated: 2026-02-07 (session 2)  
Branch: `codex/release-1.79-bootstrap`

## Current Objective

Phase 1: technical foundation stabilization (build/test/CI).

## Baseline Evidence

- Fork: `https://github.com/robertpopa22/mRemoteNG`
- Upstream remote configured to `mRemoteNG/mRemoteNG`.
- Open issues observed upstream: `830`.
- x64 build: passes with warnings.
- arm64 build: fails with `ALINK : error AL1012: 'ARM64' is not a valid setting for option 'platform'`.
- Target framework mismatch was removed by aligning test-related projects to `net10.0-windows10.0.26100.0`.
- Remaining test/spec build blockers are now compile/dependency related (details below).

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
- [x] Decouple test build from WiX project dependency by linking installer checker source file in tests.

## In Progress

- [ ] Stabilize `mRemoteNGTests` compile after TFM alignment.
- [ ] Stabilize `mRemoteNGSpecs` dependencies after TFM alignment.

## Blockers

- `arm64` build not releasable due to ALINK platform error.
- CI release workflow mostly condition-skipped on normal pushes.
- `dotnet build` is not valid for this solution due COMReference (`MSB4803`); full MSBuild must be used for core/test/spec builds.
- `mRemoteNGTests` compile errors (current set):
  - inaccessible `Language` type in connection tests
  - ambiguous `CategoryAttribute` (NUnit vs System.ComponentModel)
  - missing `ArgumentException` type qualification/import in `ExternalToolTests`
  - `XmlDocument.XmlResolver` getter usage issue in `SecureXmlHelperTests`
- `mRemoteNGSpecs` missing SpecFlow binding types (`TechTalk.*`) — package/reference gap.

## Immediate Next Actions

1. Fix `mRemoteNGTests` compile errors listed above and verify with:
   - `MSBuild.exe mRemoteNGTests\mRemoteNGTests.csproj /restore /p:Configuration=Release /p:Platform=x64 /clp:ErrorsOnly`
2. Restore SpecFlow dependencies for `mRemoteNGSpecs` and verify with:
   - `MSBuild.exe mRemoteNGSpecs\mRemoteNGSpecs.csproj /restore /p:Configuration=Release /p:Platform=x64 /clp:ErrorsOnly`
3. Add/adjust PR CI workflow to use full MSBuild (not dotnet-only) for Windows job.
4. Start arm64 diagnosis and isolate minimal fix for ALINK platform error.

## Decision Log

- 2026-02-07: Chosen strategy is release-focused fork, with upstream sync but independent stable cadence.
- 2026-02-07: Deferred large feature PR merges (#2997, #3001) until after stable release.
- 2026-02-07: Kept .NET baseline on `net10` (current stable major).
- 2026-02-07: Tests no longer reference installer project directly; installer checker source is compiled into test assembly for unit coverage without WiX dependency.

## Resume Checklist (after reboot)

1. `cd /d D:\github\mRemoteNG`
2. `call D:\github\LOCAL\env.cmd`
3. `git status -sb`
4. Open this file and execute the first unchecked item in `Immediate Next Actions`.

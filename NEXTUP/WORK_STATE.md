# Work State Tracker

Last updated: 2026-02-07 (session 10)  
Branch: `codex/release-1.79-bootstrap`

## Current Objective

Phase 2: P0 security integration and critical issue burn-down.

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
- [x] Validate second GitHub Actions execution for PR validation workflow:
  - Run: `PR_Validation` #`21781383069`
  - Result: `success`
  - URL: `https://github.com/robertpopa22/mRemoteNG/actions/runs/21781383069`
- [x] Integrate upstream security-oriented PR content into fork branch:
  - PR #3038 (Process.Start command-injection hardening)
  - PR #3054 (password delimiter escaping for external tools)
- [x] Fix post-integration test regression in `ExternalToolsArgumentParserTests` (`CS1022` due to extra brace).
- [x] Revalidate after security PR integration:
  - `mRemoteNG.sln` (Release|x64) builds
  - `mRemoteNG.sln` (Release|ARM64) builds
  - `mRemoteNGTests` (Release|x64) builds
  - `mRemoteNGSpecs` (Release|x64) builds

## In Progress

- [x] Implement LDAP hardening pass for `#3080`:
  - centralized LDAP path sanitization in `LdapPathSanitizer`
  - blocked LDAP URL query/fragment characters (`?`, `#`)
  - removed duplicated sanitizer logic from:
    - `Config/Serializers/MiscSerializers/ActiveDirectoryDeserializer.cs`
    - `Tools/ADhelper.cs`
- [x] Implement importer guardrail pass for `#2988` attack surface:
  - `MRemoteNGCsvImporter` now returns early on missing file.
  - `MRemoteNGXmlImporter` now returns early on missing file.
- [x] Added targeted regression/security tests:
  - `mRemoteNGTests/Security/LdapPathSanitizerTests.cs`
  - `mRemoteNGTests/Config/Connections/XmlConnectionsLoaderTests.cs`
  - `mRemoteNGTests/Config/Import/MRemoteNGImportersTests.cs`
- [x] CI validation green after patchset push:
  - failed first run due missing `using System` in test file
  - fixed with follow-up commit and rerun
  - run: `https://github.com/robertpopa22/mRemoteNG/actions/runs/21781854896`
- [x] Built automated P1-P5 package snapshot tooling:
  - script: `NEXTUP/scripts/refresh-p1-p5.ps1`
  - report: `NEXTUP/P1_P5_EXECUTION_2026-02-07.md`
  - JSON artifacts: `D:\github\LOCAL\analysis\mRemoteNG\packages\*.json`
- [ ] P5 fix candidate #3069 implemented locally, pending CI confirmation:
  - file: `mRemoteNG/UI/Window/ConnectionWindow.cs`
  - hardening: dispose-safe + thread-safe tab close callback in `Prot_Event_Closed`
  - reconnect path simplified to avoid redundant `Invoke`.
- [ ] P0 issue closure workflow still pending (issue comments/closure + mapping notes).

## Blockers

- Local validation blocker in this environment:
  - `dotnet test` hits `MSB4803` (`ResolveComReference` not supported on .NET Core MSBuild).
  - full framework `MSBuild.exe` exists, but SDK resolver bridge to portable `dotnet` runtime is incomplete in this shell image.
- Current release workflow is often skipped on regular pushes (trigger condition dependency).
- High warning volume remains (nullable/platform analyzer warnings), though baseline CI previously passed.

## Immediate Next Actions

1. Create mapping evidence note: upstream issue -> fork commit(s) for `#2988/#2989/#3080`.
2. Push and validate P5 fix candidate for issue `#3069`.
3. Start duplicate cleanup package P1 (6 currently open).
4. Open upstream-ready PR draft for P0 + P5 patchset split.

## Decision Log

- 2026-02-07: Chosen strategy is release-focused fork, with upstream sync but independent stable cadence.
- 2026-02-07: Deferred large feature PR merges (#2997, #3001) until after stable release.
- 2026-02-07: Kept .NET baseline on `net10` (current stable major).
- 2026-02-07: Full MSBuild standardized as validation path for this repo.
- 2026-02-07: Fixed arm64 ALINK blocker by enabling `GenerateSatelliteAssembliesForCore` under arm64 platform condition.
- 2026-02-07: PR validation workflow is live and green on first run.
- 2026-02-07: Security hardening PR content (#3038, #3054) is integrated in fork and locally validated.
- 2026-02-07: Implemented additional fork-side P0 hardening for LDAP path validation and import-missing-file guardrails; awaiting environment-compatible full validation.
- 2026-02-07: P0 hardening patchset is now CI-green on fork after one follow-up fix for missing test import (`using System`).
- 2026-02-07: P1-P5 triage baseline is now generated automatically and versioned in `NEXTUP`.

## Resume Checklist (after reboot)

1. `cd /d D:\github\mRemoteNG`
2. `call D:\github\LOCAL\env.cmd`
3. `git status -sb`
4. Open this file and execute the first unchecked item in `Immediate Next Actions`.

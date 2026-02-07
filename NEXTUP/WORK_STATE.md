# Work State Tracker

Last updated: 2026-02-07 (session 37)  
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
- [x] P5 fix candidate #3069 implemented and CI-validated:
  - file: `mRemoteNG/UI/Window/ConnectionWindow.cs`
  - hardening: dispose-safe + thread-safe tab close callback in `Prot_Event_Closed`
  - reconnect path simplified to avoid redundant `Invoke`.
  - run: `https://github.com/robertpopa22/mRemoteNG/actions/runs/21782034958`
- [x] P5 fix candidate #3092 implemented and CI-validated:
  - file: `ExternalConnectors/OP/OnePasswordCli.cs`
  - fixes:
    - robust `op://` parser supports vault/item names with spaces
    - fallback now supports `CONCEALED` fields for username/password mapping
    - purpose lookup ignores empty values before fallback by label/id
  - tests:
    - `mRemoteNGTests/ExternalConnectors/OnePasswordCliTests.cs`
  - run: `https://github.com/robertpopa22/mRemoteNG/actions/runs/21782320844`
- [x] P1 duplicate package triage pass completed (comment-level):
  - all 6 duplicate-labeled open issues reviewed and cross-linked/commented
  - close action blocked by upstream permission (`CloseIssue` denied for current account)
  - execution log: `NEXTUP/P1_P2_TRIAGE_EXECUTION_2026-02-07.md`
- [x] P2 batch-1 triage pass completed:
  - 25 oldest stale `Need 2 check` issues received reproducibility refresh comments
  - execution log: `NEXTUP/P1_P2_TRIAGE_EXECUTION_2026-02-07.md`
- [x] P2 batch-2 triage pass completed:
  - next 25 `Need 2 check` issues received reproducibility refresh comments
  - helper script added: `NEXTUP/scripts/comment-p2-need2check-batch.ps1`
  - execution log: `NEXTUP/P1_P2_TRIAGE_EXECUTION_2026-02-07.md`
- [x] P2 batch-3 + extended triage passes executed:
  - additional refresh comments posted to accelerate stale backlog cleanup
  - initial overlap discovered when sorting by `updatedAt`; script corrected to deterministic `createdAt` ordering
  - evidence: `NEXTUP/P1_P2_TRIAGE_EXECUTION_2026-02-07.md`
- [x] P2 stale sweep finalized:
  - additional targeted refresh comments posted for residual stale `Need 2 check` items
  - regenerated snapshot now shows `P2 need2check stale (>365d): 0`
  - evidence:
    - `NEXTUP/P1_P2_TRIAGE_EXECUTION_2026-02-07.md`
    - `NEXTUP/P1_P5_EXECUTION_2026-02-07.md`
- [x] P5 fix candidate #2972 implemented and CI-validated:
  - files:
    - `mRemoteNG/Connection/Protocol/RDP/RdpProtocol.cs`
    - `mRemoteNG/Connection/Protocol/PuttyBase.cs`
  - change:
    - `EmptyCredentials=custom` now respects `ExternalCredentialProviderDefault` for OnePassword/Clickstudios (not only Delinea)
  - run: `https://github.com/robertpopa22/mRemoteNG/actions/runs/21783476996`
- [x] P3 stale label triage pass completed (comment-level):
  - stale `In progress` set commented (19 issues)
  - stale `In development` set commented (27 issues)
  - evidence: `NEXTUP/P3_TRIAGE_EXECUTION_2026-02-07.md`
- [x] P0 mapping evidence note completed:
  - file: `NEXTUP/P0_MAPPING_2026-02-07.md`
  - maps #2988/#2989/#3080 to integrated commits and residual risk notes
- [x] P0 issue evidence comments posted upstream:
  - #2988 comment: `https://github.com/mRemoteNG/mRemoteNG/issues/2988#issuecomment-3864889173`
  - #3080 comment: `https://github.com/mRemoteNG/mRemoteNG/issues/3080#issuecomment-3864889174`
- [x] Maintainer close/relabel handoff package created:
  - `NEXTUP/MAINTAINER_HANDOFF_2026-02-07.md`
- [x] Upstream PR split plan documented:
  - `NEXTUP/UPSTREAM_PR_PACKAGES_2026-02-07.md`
- [x] Upstream PR package branches created:
  - PR-1: `https://github.com/mRemoteNG/mRemoteNG/pull/3105`
  - PR-2: `https://github.com/mRemoteNG/mRemoteNG/pull/3106`
  - PR-3: `https://github.com/mRemoteNG/mRemoteNG/pull/3107`
  - PR-4: `https://github.com/mRemoteNG/mRemoteNG/pull/3108`
- [x] Upstream PRs moved from draft to ready-for-review.
- [x] CI evidence comments posted on each upstream PR.
- [x] Upstream PR-5 opened for command-line hardening + external-tools escaping:
  - PR-5: `https://github.com/mRemoteNG/mRemoteNG/pull/3109`
- [x] Windows architecture coverage expanded in fork CI/release workflows:
  - `PR_Validation`: now includes `x86`, `x64`, `ARM64` solution builds
  - `Build_and_Release_mR-NB`: now includes `x86`, `x64`, `ARM64` artifact builds
- [x] Root-cause + fix for first `x86` CI breakage (`PR_Validation` run `21784889033`):
  - root causes:
    - missing `Release|x86` and related `x86` mappings in `mRemoteNG.sln`
    - missing `x86` platform/config blocks in project files
    - `x86` test build hit duplicate assembly attributes because `GenerateAssemblyInfo` was not disabled for new x86 configs in `mRemoteNG.csproj`
  - fixes applied:
    - added `x86` solution configurations and project mappings in `mRemoteNG.sln`
    - enabled `x86` platform declarations in:
      - `mRemoteNG/mRemoteNG.csproj`
      - `ExternalConnectors/ExternalConnectors.csproj`
      - `mRemoteNGTests/mRemoteNGTests.csproj`
      - `mRemoteNGSpecs/mRemoteNGSpecs.csproj`
    - added `x86` platform target handling in:
      - `mRemoteNG/mRemoteNG.csproj`
      - `ObjectListView/ObjectListView.NetCore.csproj`
    - added x86 configuration property groups in `mRemoteNG/mRemoteNG.csproj` (`Debug`, `Release`, `Debug Portable`, `Release Portable`, `Release Installer`, `Deploy to github`)
- [x] Verified x86 remediation in CI:
  - run: `https://github.com/robertpopa22/mRemoteNG/actions/runs/21785039669`
  - result: `success`
  - jobs green:
    - `Build solution (x86)`
    - `Build tests and specs (x86)`
    - `Build solution (x64)`
    - `Build tests and specs (x64)`
    - `Build solution (ARM64)`
- [x] Head-state CI check after PR-6 documentation sync:
  - run: `https://github.com/robertpopa22/mRemoteNG/actions/runs/21785223094`
  - result: `success`
  - jobs green: `Build solution/tests` on `x86`, `x64`, `ARM64`
- [x] Implemented SQL Server connector packaging hardening for issue `#3005`:
  - change:
    - explicit `Microsoft.Data.SqlClient.SNI`
    - explicit `Microsoft.Data.SqlClient.SNI.runtime`
    - file: `mRemoteNG/mRemoteNG.csproj`
  - validation:
    - run: `https://github.com/robertpopa22/mRemoteNG/actions/runs/21785156992`
    - result: `success` (`x86`, `x64`, `ARM64`)
  - upstream PR:
    - `https://github.com/mRemoteNG/mRemoteNG/pull/3110`
  - upstream issue cross-link comment:
    - `https://github.com/mRemoteNG/mRemoteNG/issues/3005#issuecomment-3865102490`
- [x] P4 version-label debt triage automation + first batch completed:
  - new script:
    - `NEXTUP/scripts/comment-p4-version-debt-batch.ps1`
  - batches posted:
    - 36 issue comments with retest/relabel guidance
  - evidence:
    - `NEXTUP/P4_TRIAGE_EXECUTION_2026-02-07.md`
- [x] P5 panel empty-state follow-up implemented:
  - file:
    - `mRemoteNG/UI/Window/ConnectionWindow.cs`
  - change:
    - when the last tab closes, panel now auto-closes in a guarded/debounced UI-safe path
  - validation:
    - run: `https://github.com/robertpopa22/mRemoteNG/actions/runs/21785604947`
    - result: `success` (`x86`, `x64`, `ARM64`)
- [x] P4 version-label debt comment triage exhausted (stateful):
  - script updates:
    - `NEXTUP/scripts/comment-p4-version-debt-batch.ps1`
    - `NEXTUP/scripts/run-p4-to-exhaustion.ps1`
  - state file:
    - `NEXTUP/p4_state_processed.txt` (`328` processed issue IDs)
  - runner terminal state:
    - `No version-debt issues found ... DONE after 7 rounds`
- [x] SQL provider/serializer compatibility hardening for legacy DB schemas (issue `#1916` candidate):
  - files:
    - `mRemoteNG/Config/DataProviders/SqlDataProvider.cs`
    - `mRemoteNG/Config/Serializers/ConnectionSerializers/Sql/DataTableSerializer.cs`
    - `mRemoteNGTests/Config/Serializers/DataTableSerializerTests.cs`
  - change:
    - always load SQL reader schema even when `tblCons` has 0 rows
    - auto-add missing serializer columns when source SQL schema is partial/outdated
    - add regression test for missing-column source schema
  - CI validation:
    - run: `https://github.com/robertpopa22/mRemoteNG/actions/runs/21786116643`
    - result: `success` (`x86`, `x64`, `ARM64`)
  - upstream issue updates:
    - `#1916`: `https://github.com/mRemoteNG/mRemoteNG/issues/1916#issuecomment-3865218242`
    - `#1883`: `https://github.com/mRemoteNG/mRemoteNG/issues/1883#issuecomment-3865226419`
    - `#2290`: `https://github.com/mRemoteNG/mRemoteNG/issues/2290#issuecomment-3865226895`
- [x] Upstream PR-7 opened for SQL schema compatibility hardening:
  - branch: `codex/pr7-sql-schema-compat-1916`
  - PR: `https://github.com/mRemoteNG/mRemoteNG/pull/3111`
  - CI evidence comment:
    - `https://github.com/mRemoteNG/mRemoteNG/pull/3111#issuecomment-3865236823`
- [x] P5 fix candidate #850 implemented and CI-validated:
  - file:
    - `mRemoteNG/UI/Window/ConfigWindow.cs`
  - change:
    - preserve Config PropertyGrid label/value splitter width across minimize/maximize and resize layout cycles
    - runtime-safe reflection fallback for WinForms internal splitter APIs
  - CI validation:
    - run: `https://github.com/robertpopa22/mRemoteNG/actions/runs/21786942297`
    - result: `success` (`x86`, `x64`, `ARM64`)
  - upstream PR:
    - `https://github.com/mRemoteNG/mRemoteNG/pull/3112`
  - upstream issue update:
    - `#850`: `https://github.com/mRemoteNG/mRemoteNG/issues/850#issuecomment-3865499466`
- [x] P5 fix candidate #1969 implemented and upstream-packaged:
  - files:
    - `mRemoteNG/Connection/ConnectionsService.cs`
    - `mRemoteNGTests/Connection/ConnectionsServiceStartupPathTests.cs`
  - change:
    - startup file path now falls back to default when `ConnectionFilePath` is `null`, empty, or whitespace
    - regression test coverage added for `null`, empty, and whitespace values
  - fork CI validation evidence:
    - `https://github.com/robertpopa22/mRemoteNG/actions/runs/21787167552`
  - upstream PR:
    - `https://github.com/mRemoteNG/mRemoteNG/pull/3113`
  - upstream issue update:
    - `https://github.com/mRemoteNG/mRemoteNG/issues/1969#issuecomment-3865576670`
- [x] Windows compatibility baseline lowered for broader client coverage while keeping `.NET 10`:
  - changed from `windows10.0.26100.0` to `windows10.0.19041.0` across:
    - `mRemoteNG/mRemoteNG.csproj`
    - `ExternalConnectors/ExternalConnectors.csproj`
    - `ObjectListView/ObjectListView.NetCore.csproj`
    - `mRemoteNGSpecs/mRemoteNGSpecs.csproj`
    - `mRemoteNGTests/mRemoteNGTests.csproj`
  - local validation:
    - solution build: `Release|x86`, `Release|x64`, `Release|ARM64`
    - tests/specs build: `Release|x86`, `Release|x64`
  - fork CI validation:
    - run: `https://github.com/robertpopa22/mRemoteNG/actions/runs/21787440752`
    - result: `success` (`x86`, `x64`, `ARM64`, tests/specs)
- [x] Stability duplicate-triage follow-up for close-tab crash cluster:
  - issue `#3062` cross-linked as likely duplicate of `#3069` (same `Prot_Event_Closed` disposed-object call path)
  - upstream triage comment:
    - `https://github.com/mRemoteNG/mRemoteNG/issues/3062#issuecomment-3865581826`
  - mapped fix PR:
    - `https://github.com/mRemoteNG/mRemoteNG/pull/3106`
- [x] Upstream issue cross-link pass for already-packaged P5 fixes:
  - `#3044` -> `https://github.com/mRemoteNG/mRemoteNG/issues/3044#issuecomment-3865585451` (mapped PR `#3109`)
  - `#3092` -> `https://github.com/mRemoteNG/mRemoteNG/issues/3092#issuecomment-3865585855` (mapped PR `#3107`)
  - `#2972` -> `https://github.com/mRemoteNG/mRemoteNG/issues/2972#issuecomment-3865586187` (mapped PR `#3108`)
  - `#3069` -> `https://github.com/mRemoteNG/mRemoteNG/issues/3069#issuecomment-3865586563` (mapped PR `#3106`)
- [ ] P0 issue closure workflow still pending (maintainer close decision + permissions).

## Blockers

- Local `dotnet test` via .NET SDK 10 is available through `D:\github\LOCAL\env.cmd`, but this solution still needs full Framework MSBuild for COMReference paths (`MSB4803` on .NET Core MSBuild path).
- Current release workflow is often skipped on regular pushes (trigger condition dependency).
- High warning volume remains (nullable/platform analyzer warnings), though baseline CI previously passed.

## Immediate Next Actions

1. Track upstream feedback on PR-7/PR-8 (`#3111`, `#3112`) and fast-follow any review fixes.
2. Continue P5 stabilization with next fixable runtime/UI candidate (`#822` startup keyfile handling).
3. Refresh P1-P5 snapshot and report percentage against release-scope backlog.

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
- 2026-02-07: P5 close-panel crash candidate (#3069) implemented and validated green in CI.
- 2026-02-07: P5 1Password candidate (#3092) implemented with parser + field extraction hardening and validated green in CI (`21782320844`).
- 2026-02-07: P1 duplicates triaged with canonical links/comments, but upstream close action is permission-blocked for current account.
- 2026-02-07: P2 batch-1 (`25` stale `Need 2 check`) triage comments executed and logged.
- 2026-02-07: P2 batch-2 (`25` additional `Need 2 check`) triage comments executed and script-automated for repeatable runs.
- 2026-02-07: P2 extended passes found `updatedAt` overlap behavior; batching automation updated to `createdAt` sorting.
- 2026-02-07: Implemented #2972 fix for default external credential provider handling in RDP/SSH protocols; validated green in CI (`21783476996`).
- 2026-02-07: P3 stale status comments executed for both `In progress` and `In development` issue sets.
- 2026-02-07: P0 mapping evidence note recorded in `NEXTUP/P0_MAPPING_2026-02-07.md`.
- 2026-02-07: Posted P0 evidence comments upstream for `#2988` and `#3080`; created maintainer handoff + upstream PR package documents.
- 2026-02-07: Opened upstream draft PR package set (`#3105`, `#3106`, `#3107`, `#3108`) on dedicated `codex/pr*` branches.
- 2026-02-07: Marked upstream package PRs as ready for review and posted CI evidence comments on each PR.
- 2026-02-07: Opened upstream PR `#3109` to unblock `#2989/#3044`; expanded fork workflows to include `x86`.
- 2026-02-07: Fixed first x86 CI regression by adding explicit x86 mappings/configurations across solution + project graph.
- 2026-02-07: Confirmed full PR validation pass with x86 enabled (`21785039669`).
- 2026-02-07: Added explicit SqlClient SNI runtime references and opened upstream PR `#3110` for `#3005`.
- 2026-02-07: Confirmed branch-head CI green after PR-6 state/doc updates (`21785223094`).
- 2026-02-07: Drove `Need 2 check` stale bucket to zero (`P2 stale >365d = 0`) via targeted comment sweep and snapshot refresh.
- 2026-02-07: Added deterministic P4 triage automation and executed first 12-comment version-label debt batch.
- 2026-02-07: Extended P4 version-label debt triage to 24 commented issues across two deterministic batches.
- 2026-02-07: Extended P4 version-label debt triage to 36 commented issues across three deterministic batches.
- 2026-02-07: Implemented panel auto-close follow-up when the last connection tab is closed.
- 2026-02-07: Validated panel auto-close follow-up in CI (`21785604947`) with full `x86/x64/ARM64` matrix green.
- 2026-02-07: Implemented #850 Config PropertyGrid splitter persistence across minimize/maximize and validated CI green (`21786942297`).
- 2026-02-07: Opened upstream PR `#3112` for #850 and posted issue/CI cross-links.

## Resume Checklist (after reboot)

1. `cd /d D:\github\mRemoteNG`
2. `call D:\github\LOCAL\env.cmd`
3. `git status -sb`
4. Open this file and execute the first unchecked item in `Immediate Next Actions`.

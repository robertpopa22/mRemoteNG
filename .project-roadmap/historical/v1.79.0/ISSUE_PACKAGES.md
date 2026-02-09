# Issue Intervention Packages

Snapshot date: 2026-02-07  
Source repository analyzed: `mRemoteNG/mRemoteNG`

## Global Baseline

- Open issues: `830`
- Stale > 365 days: `686`
- `Need 2 check`: `207` (stale subset > 365 days: `174`)
- `In development`: `35`
- `In progress`: `30`
- `Duplicate`: `6`
- `Security`: `27`
- Critical label: `3` (#3080, #2989, #2988)

## Package P0 - Security Critical

Scope:
- #3080
- #2989
- #2988
- Pending security PR integration: #3038, #3054

Current status (fork execution branch):
- Integrated PR #3038 content into fork (addresses issue #2989 attack surface).
- Integrated PR #3054 content into fork (external tool argument escaping hardening).
- Remaining criticals without upstream fix PR mapping: #2988, #3080.
- Mapping evidence file created: `.project-roadmap/P0_MAPPING_2026-02-07.md`.
- Upstream evidence comments posted:
  - #2988: https://github.com/mRemoteNG/mRemoteNG/issues/2988#issuecomment-3864889173
  - #3080: https://github.com/mRemoteNG/mRemoteNG/issues/3080#issuecomment-3864889174
- Awaiting maintainer close decision or reproducible PoC feedback.

Goal:
- No unresolved critical security risk in fork release branch.

Done criteria:
- Fix merged and validated, or issue closed with evidence of non-applicability.

## Package P1 - Duplicate and Obsolete Cleanup

Scope:
- All issues labeled `Duplicate` (currently 6).

Goal:
- Remove known duplicates from active backlog.

Done criteria:
- Duplicate-labeled issues closed and cross-linked.

Current status (2026-02-07 execution):
- 6/6 duplicate issues triaged and cross-linked by comment.
- Upstream close action is permission-blocked for current account; maintainer close is pending.
- Evidence: `.project-roadmap/P1_P2_TRIAGE_EXECUTION_2026-02-07.md`

## Package P2 - Need-2-Check Backlog

Scope:
- 207 issues, prioritize 174 stale > 1 year.

Goal:
- Retain only reproducible, currently relevant items.

Execution model:
- Batch of 25 per pass.
- Apply reproducibility template.
- Close `no-response`/`not reproducible` with explicit rationale.

Current status (2026-02-07 execution):
- Batch-1 completed: 25 oldest stale issues commented with retest/repro request.
- Batch-2 completed: 25 additional stale issues commented with retest/repro request.
- Batch-3 and extended passes executed for broader stale refresh.
- Batching script corrected to deterministic `createdAt` sorting to avoid overlap from `updatedAt` mutation.
- Next step: run deterministic batches only for newly stale/unrefreshed entries.
- Evidence: `.project-roadmap/P1_P2_TRIAGE_EXECUTION_2026-02-07.md`

## Package P3 - Stale In-Progress Labels

Scope:
- `In progress` and `In development` older than 1 year.

Goal:
- Restore label integrity and execution credibility.

Done criteria:
- Each item either revalidated and kept active, or relabeled/closed.

Current status (2026-02-07 execution):
- stale `In progress` and `In development` sets fully commented for status refresh/relabel guidance.
- close/relabel actions require maintainer write permissions upstream.
- Evidence: `.project-roadmap/P3_TRIAGE_EXECUTION_2026-02-07.md`

## Package P4 - Version Label Debt

Scope:
- Legacy version labels (`1.77.2`, `1.77.3`, `1.78.*`, `1.8 (Fenix)`).

Goal:
- Reclassify by current product state instead of historical branch context.

Current status (2026-02-07 execution):
- added deterministic batch triage script:
  - `.project-roadmap/scripts/comment-p4-version-debt-batch.ps1`
- added exhaustion runner:
  - `.project-roadmap/scripts/run-p4-to-exhaustion.ps1`
- executed stateful P4 waves to exhaustion:
  - `328` issues comment-triaged (tracked in `.project-roadmap/p4_state_processed.txt`)
  - runner reached terminal state (`No version-debt issues found`)
- evidence:
  - `.project-roadmap/P4_TRIAGE_EXECUTION_2026-02-07.md`

## Package P5 - Release Stabilization Candidates

Scope:
- Bugs/regressions affecting core protocols and settings persistence.

Goal:
- Curate minimal risk fixset for `v1.79.0`.

Current status (fork execution branch):
- #3069: implemented and CI-validated (`mRemoteNG/UI/Window/ConnectionWindow.cs`).
- #3092: implemented and CI-validated (`https://github.com/robertpopa22/mRemoteNG/actions/runs/21782320844`) with 1Password parser/field extraction hardening (`ExternalConnectors/OP/OnePasswordCli.cs`).
  - upstream issue cross-link:
    - `https://github.com/mRemoteNG/mRemoteNG/issues/3092#issuecomment-3865585855`
- #2972: implemented and CI-validated (`https://github.com/robertpopa22/mRemoteNG/actions/runs/21783476996`) so default custom credentials path now honors selected external provider in RDP/SSH flows.
  - upstream issue cross-link:
    - `https://github.com/mRemoteNG/mRemoteNG/issues/2972#issuecomment-3865586187`
- #1916/#1883 SQL save compatibility hardening implemented and CI-validated (`https://github.com/robertpopa22/mRemoteNG/actions/runs/21786116643`):
  - `mRemoteNG/Config/DataProviders/SqlDataProvider.cs`
  - `mRemoteNG/Config/Serializers/ConnectionSerializers/Sql/DataTableSerializer.cs`
  - `mRemoteNGTests/Config/Serializers/DataTableSerializerTests.cs`
  - upstream PR:
    - `https://github.com/mRemoteNG/mRemoteNG/pull/3111`
- #850 config panel splitter persistence implemented and CI-validated (`https://github.com/robertpopa22/mRemoteNG/actions/runs/21786942297`):
  - `mRemoteNG/UI/Window/ConfigWindow.cs`
  - upstream PR:
    - `https://github.com/mRemoteNG/mRemoteNG/pull/3112`
  - upstream issue update:
    - `https://github.com/mRemoteNG/mRemoteNG/issues/850#issuecomment-3865499466`
- #1969 startup connection path null/empty fallback implemented:
  - `mRemoteNG/Connection/ConnectionsService.cs`
  - `mRemoteNGTests/Connection/ConnectionsServiceStartupPathTests.cs`
  - upstream PR:
    - `https://github.com/mRemoteNG/mRemoteNG/pull/3113`
  - upstream issue update:
    - `https://github.com/mRemoteNG/mRemoteNG/issues/1969#issuecomment-3865576670`
- #822 startup resilience candidate implemented (defensive hardening):
  - `mRemoteNG/Config/Putty/PuttySessionsManager.cs`
  - provider failures while enumerating PuTTY sessions now log warning and no longer abort connection-file loading
  - regression test:
    - `mRemoteNGTests/Connection/ConnectionsServicePuttySessionsResilienceTests.cs`
- #811 startup XML exception resilience candidate implemented:
  - files:
    - `mRemoteNG/Config/Connections/XmlConnectionsLoader.cs`
    - `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsDeserializer.cs`
    - `mRemoteNGTests/Config/Connections/XmlConnectionsLoaderTests.cs`
  - behavior:
    - malformed startup XML now attempts deterministic recovery from the newest valid `*.backup`
    - successful recovery auto-restores `confCons.xml` from backup
    - parser now surfaces explicit `XmlException` for unparsable documents
- #2785 PuTTY CJK session-name decode resilience candidate implemented:
  - files:
    - `mRemoteNG/Config/Putty/PuttySessionNameDecoder.cs`
    - `mRemoteNG/Config/Putty/PuttySessionsRegistryProvider.cs`
    - `mRemoteNG/Config/Putty/AbstractPuttySessionsProvider.cs`
    - `mRemoteNGTests/Config/Putty/PuttySessionNameDecoderTests.cs`
  - behavior:
    - centralized UTF-8-first decode with deterministic legacy-encoding fallback for percent-encoded PuTTY registry session names
    - shared normalization path now used both when listing and pruning session entries
  - validation:
    - fork CI: `https://github.com/robertpopa22/mRemoteNG/actions/runs/21789090820`
  - upstream package:
    - PR: `https://github.com/mRemoteNG/mRemoteNG/pull/3115`
    - issue update: `https://github.com/mRemoteNG/mRemoteNG/issues/2785#issuecomment-3865800159`
- panel auto-close follow-up for last-tab scenario implemented in:
  - `mRemoteNG/UI/Window/ConnectionWindow.cs`
  - CI-validated:
    - `https://github.com/robertpopa22/mRemoteNG/actions/runs/21785604947`
- #3069 upstream issue cross-link:
  - `https://github.com/mRemoteNG/mRemoteNG/issues/3069#issuecomment-3865586563`
- #3062 triage updated as likely duplicate/alias of #3069 close-path crash:
  - upstream cross-link comment:
    - `https://github.com/mRemoteNG/mRemoteNG/issues/3062#issuecomment-3865581826`
  - mapped fix PR:
    - `https://github.com/mRemoteNG/mRemoteNG/pull/3106`
- compatibility hardening for older client machines completed:
  - baseline lowered to `windows10.0.19041.0` while staying on `.NET 10`
  - CI-validated:
    - `https://github.com/robertpopa22/mRemoteNG/actions/runs/21787440752`
- #3044 upstream issue cross-link for external-tool escaping fix:
  - `https://github.com/mRemoteNG/mRemoteNG/issues/3044#issuecomment-3865585451`

## Package P6 - Pre-existing Test Failures

Scope:
- 81 pre-existing test failures across 12 categories in upstream `v1.78.2-dev`.

Goal:
- 2148/2148 tests green; zero test debt before release.

Current status (2026-02-08, COMPLETED):
- commit: `79c5e4cf` on `release/1.79`
- 25 files changed (13 source, 12 test)
- detailed per-file changelog: `.project-roadmap/P6_TEST_FIX_CHANGELOG_2026-02-08.md`

Fix categories:
1. CSV serializer header bug + missing properties (~28 tests)
2. CSV deserializer missing mappings + UserViaAPI bug (~4 tests)
3. XML serializer missing Color, RDGatewayAccessToken, InheritColor (2 tests)
4. XML deserializer wrong attribute name for InheritRedirectAudioCapture (1 test)
5. XSD schema missing attributes Color, RDGatewayAccessToken (1 test)
6. ContainerInfo comparer NRE on null property values (2 tests)
7. RDP resize: minimize state not tracked → restore not detected (1 test)
8. ConfigWindow property grid: IntApp missing Username in expected list (~18 tests)
9. ExternalTool regex assertion bug (1 test)
10. RootNodeInfo SetUICulture timing in test setup (1 test)
11. OptionsForm: control names, expected text, visibility check (4 tests)
12. Test helpers: TabColorConverter, FileBackupCreator, CredentialRecordTypeConverter, ConnectionInitiator (~18 tests)

Done criteria:
- All 2148 tests pass locally and in CI.

## Practical Execution Order

1. P0 Security Critical
2. P1 Duplicate
3. P2 Need-2-Check stale batches
4. P3 Stale In-Progress labels
5. P5 release blockers
6. P4 version label debt

## Focused Live Triage Snapshot (2026-02-07)

Priority packages extracted from currently visible open issues:

- Security core:
  - `#2988` (RCE via object deserialize path)
  - `#2989` (XXE)
  - `#3080` (LDAP query injection)
  - `#2998` (RCE from MD5-hashed password path)
  - `#3088` (RC4 vulnerability)

- Stability/UX cluster (candidate duplicates):
  - `#3069` crash when closing tab
  - `#3062` focus behavior when closing tab
  - historical likely-related close-tab behavior issues (`#2881`, `#2756`)

- Connectivity/integration:
  - `#3083` telnet + Cisco behavior
  - `#3092` password issue after upgrade
  - `#3027` MySQL locking
  - `#3005` PuTTY script deployment issue

- Infra / non-core split:
  - `#3103` website redirect
  - `#2818` CSP hardening (website scope)

## Notes

- "No issues" is interpreted as "no unresolved actionable issues for release scope" plus aggressive backlog hygiene.
- Full zero-open backlog is typically unrealistic in mature OSS; the operational target is zero critical/release-blocking plus disciplined triage.
- Execution snapshot/report for P1-P5 is tracked in:
  - `.project-roadmap/P1_P5_EXECUTION_2026-02-07.md`

# Upstream PR Packages

Date: 2026-02-07  
Execution branch: `release/1.79`

Purpose:
- split fork changes into small, reviewable PRs for upstream maintainers.

> **Note (2026-02-09):** The `codex/pr*` branch names referenced below are historical.
> All 26 branches were merged into `release/1.79` and deleted during branch cleanup.
> The PRs on GitHub (#3105-#3130) remain valid references.

## PR-1 Security Follow-up (P0)

Scope:
- LDAP sanitizer centralization and call-site hardening.
- importer missing-file guardrails.
- related tests.

Primary commits:
- `3c419ded` (`p0-ldap-import-hardening`)
- `8680c53f` (test compile follow-up)

Core files:
- `mRemoteNG/Security/LdapPathSanitizer.cs`
- `mRemoteNG/Config/Serializers/MiscSerializers/ActiveDirectoryDeserializer.cs`
- `mRemoteNG/Tools/ADhelper.cs`
- `mRemoteNG/Config/Import/MRemoteNGCsvImporter.cs`
- `mRemoteNG/Config/Import/MRemoteNGXmlImporter.cs`
- tests under `mRemoteNGTests/Security` and `mRemoteNGTests/Config`

Execution status:
- branch: `codex/pr1-security-followup`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3105
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3105#issuecomment-3865040700

## PR-2 Stability Fix: Close-Panel Race (`#3069`) (P5)

Primary commit:
- `c12abbe1` (includes this fix plus docs; split commit for PR if needed)

Core file:
- `mRemoteNG/UI/Window/ConnectionWindow.cs`

Notes:
- if upstream wants minimal diff, cherry-pick only relevant hunk from `c12abbe1`.

Execution status:
- branch: `codex/pr2-closepanel-stability`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3106
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3106#issuecomment-3865040687
- related issue-family cross-links:
  - `#2118`: https://github.com/mRemoteNG/mRemoteNG/issues/2118#issuecomment-3867046750
  - `#2163`: https://github.com/mRemoteNG/mRemoteNG/issues/2163#issuecomment-3867046749
  - `#2459`: https://github.com/mRemoteNG/mRemoteNG/issues/2459#issuecomment-3867046753
  - `#2706`: https://github.com/mRemoteNG/mRemoteNG/issues/2706#issuecomment-3867046751
  - `#3062`: https://github.com/mRemoteNG/mRemoteNG/issues/3062#issuecomment-3867046757

## PR-3 1Password Parsing and Field Fallback (`#3092`) (P5)

Primary commit:
- `821eaad6`

Core files:
- `ExternalConnectors/OP/OnePasswordCli.cs`
- `mRemoteNGTests/ExternalConnectors/OnePasswordCliTests.cs`

Execution status:
- branch: `codex/pr3-onepassword-3092`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3107
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3107#issuecomment-3865040688

## PR-4 Default External Credential Provider Handling (`#2972`) (P5)

Primary commit:
- `35831bb5` (contains additional triage/docs; split commit for PR if needed)

Core files:
- `mRemoteNG/Connection/Protocol/RDP/RdpProtocol.cs`
- `mRemoteNG/Connection/Protocol/PuttyBase.cs`

Notes:
- for clean upstream PR, isolate only protocol changes from this mixed commit.

Execution status:
- branch: `codex/pr4-default-provider-2972`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3108
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3108#issuecomment-3865040693

## PR-5 Command-Line Hardening + External Tool Escaping (`#2989`, `#3044`)

Scope:
- Process.Start invocation hardening.
- AnyDesk validation tests.
- external-tools argument escaping fixes for comma/semicolon in credentials.

Execution status:
- branch: `codex/pr5-commandline-security`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3109
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3109#issuecomment-3865049494

## PR-6 SQL Server Connector Runtime Packaging Hardening (`#3005`)

Scope:
- explicit SqlClient native SNI runtime package references for platform-targeted builds.

Primary commit:
- `8623d978`

Core file:
- `mRemoteNG/mRemoteNG.csproj`

Execution status:
- branch: `codex/pr6-sqlclient-sni-runtime`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3110
- issue link comment: https://github.com/mRemoteNG/mRemoteNG/issues/3005#issuecomment-3865102490
- CI evidence (fork): https://github.com/robertpopa22/mRemoteNG/actions/runs/21785156992

## PR-7 SQL Schema Compatibility Hardening (`#1916`, `#1883`)

Scope:
- resilient SQL schema handling for empty and partially outdated `tblCons` schemas.

Primary commit:
- `ed9ac050`

Core files:
- `mRemoteNG/Config/DataProviders/SqlDataProvider.cs`
- `mRemoteNG/Config/Serializers/ConnectionSerializers/Sql/DataTableSerializer.cs`
- `mRemoteNGTests/Config/Serializers/DataTableSerializerTests.cs`

Execution status:
- branch: `codex/pr7-sql-schema-compat-1916`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3111
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3111#issuecomment-3865236823
- fork CI evidence: https://github.com/robertpopa22/mRemoteNG/actions/runs/21786116643

## PR-8 Config Panel Splitter Persistence (`#850`)

Scope:
- preserve Config PropertyGrid splitter width after minimize/maximize and resize layout cycles.

Primary commits:
- `774fb246`
- `130db77c`

Core file:
- `mRemoteNG/UI/Window/ConfigWindow.cs`

Execution status:
- branch: `codex/pr8-configpanel-splitter-850`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3112
- issue update comment: https://github.com/mRemoteNG/mRemoteNG/issues/850#issuecomment-3865499466
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3112#issuecomment-3865499667
- fork CI evidence: https://github.com/robertpopa22/mRemoteNG/actions/runs/21786942297

## PR-9 Startup Connection Path Null/Empty Fallback (`#1969`)

Scope:
- avoid startup-load failure when `ConnectionFilePath` is null/empty/whitespace.
- add regression tests for null/empty/whitespace startup path cases.

Primary commits:
- `0c3d2ae1`
- `bc4bb565`

Core files:
- `mRemoteNG/Connection/ConnectionsService.cs`
- `mRemoteNGTests/Connection/ConnectionsServiceStartupPathTests.cs`

Execution status:
- branch: `codex/pr9-startup-path-fallback-1969`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3113
- issue update comment: https://github.com/mRemoteNG/mRemoteNG/issues/1969#issuecomment-3865576670
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3113#issuecomment-3865577217
- fork CI evidence (same fix validated on release branch): https://github.com/robertpopa22/mRemoteNG/actions/runs/21787167552

## PR-10 PuTTY Provider Startup Resilience (`#822`)

Scope:
- avoid startup aborts when PuTTY sessions provider fails (for example missing key file / invalid provider state).
- keep loading the main connections file and log provider failure as warning.
- add regression coverage for provider failure path in startup loading flow.

Primary commit:
- `55a61ba0`

Core files:
- `mRemoteNG/Config/Putty/PuttySessionsManager.cs`
- `mRemoteNGTests/Connection/ConnectionsServicePuttySessionsResilienceTests.cs`

Execution status:
- branch: `codex/pr10-putty-provider-resilience-822`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3114
- issue update comment: https://github.com/mRemoteNG/mRemoteNG/issues/822#issuecomment-3865638580
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3114#issuecomment-3865639177
- fork CI evidence (release branch validation): https://github.com/robertpopa22/mRemoteNG/actions/runs/21787754981

## PR-11 PuTTY CJK Session Name Decoding (`#2785`)

Scope:
- improve decoding of percent-encoded PuTTY registry session names for CJK/legacy non-UTF8 encodings.
- keep plus-sign behavior stable and unify decode logic across provider paths.

Primary commit:
- `78544cf3`

Core files:
- `mRemoteNG/Config/Putty/PuttySessionNameDecoder.cs`
- `mRemoteNG/Config/Putty/PuttySessionsRegistryProvider.cs`
- `mRemoteNG/Config/Putty/AbstractPuttySessionsProvider.cs`
- `mRemoteNGTests/Config/Putty/PuttySessionNameDecoderTests.cs`

Execution status:
- branch: `codex/pr11-putty-cjk-decode-2785`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3115
- issue update comment: https://github.com/mRemoteNG/mRemoteNG/issues/2785#issuecomment-3865800159
- fork CI evidence (release branch validation): https://github.com/robertpopa22/mRemoteNG/actions/runs/21789090820

## PR-12 RDP SmartSize Focus Resilience (`#2735`)

Scope:
- restore active-session focus when app re-activates (Alt+Tab path).
- harden active-tab lookup fallback for activation flow.

Primary commit:
- `734c8a72`

Core file:
- `mRemoteNG/UI/Forms/frmMain.cs`

Execution status:
- branch: `codex/pr12-rdp-smartsize-focus-2735`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3116
- issue update comment: https://github.com/mRemoteNG/mRemoteNG/issues/2735#issuecomment-3865832228
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3116#issuecomment-3865832482
- fork CI evidence (release branch validation): https://github.com/robertpopa22/mRemoteNG/actions/runs/21789349478

## PR-13 RDP Fullscreen/Redirect-Keys Guardrail (`#847`)

Scope:
- prevent unstable fullscreen-exit toggle path when redirect-keys mode is active.

Primary commit:
- `c93ac33f`

Core files:
- `mRemoteNG/Connection/Protocol/RDP/RdpProtocol.cs`
- `mRemoteNG/UI/Window/ConnectionWindow.cs`

Execution status:
- branch: `codex/pr13-rdp-redirectkeys-fullscreen-847`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3117
- issue update comment: https://github.com/mRemoteNG/mRemoteNG/issues/847#issuecomment-3865842009
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3117#issuecomment-3865842198
- fork CI evidence (release branch validation): https://github.com/robertpopa22/mRemoteNG/actions/runs/21789456797

## PR-14 RDP Fullscreen Exit Refocus (`#1650`)

Scope:
- reduce background/focus-loss behavior after leaving RDP fullscreen via connection bar.

Primary commit:
- `6df1e7f5`

Core file:
- `mRemoteNG/Connection/Protocol/RDP/RdpProtocol.cs`

Execution status:
- branch: `codex/pr14-rdp-fullscreen-exit-refocus-1650`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3118
- issue update comment: https://github.com/mRemoteNG/mRemoteNG/issues/1650#issuecomment-3865854500
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3118#issuecomment-3865854782
- fork CI evidence (release branch validation): https://github.com/robertpopa22/mRemoteNG/actions/runs/21789549099

## PR-15 RDP SmartSize RCW/COM Disconnection Resilience (`#2510`)

Scope:
- harden SmartSize read/write against invalid/disconnected COM RCW states.
- remove unsafe RCW recreation fallback and handle COM access failures gracefully.
- skip resize path when RDP controls are unavailable/disposed.

Primary commit:
- `1cf71834`

Core files:
- `mRemoteNG/Connection/Protocol/RDP/RdpProtocol.cs`
- `mRemoteNG/Connection/Protocol/RDP/RdpProtocol8.cs`

Execution status:
- branch: `codex/pr15-rdp-rcw-smartsize-2510`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3119
- issue update comment: https://github.com/mRemoteNG/mRemoteNG/issues/2510#issuecomment-3866311674
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3119#issuecomment-3866311780
- fork CI evidence (release branch validation): https://github.com/robertpopa22/mRemoteNG/actions/runs/21793657159

## PR-16 Settings Path Observability + Troubleshooting Clarification (`#2987`)

Scope:
- expose effective user settings file path via `SettingsFileInfo.UserSettingsFilePath`.
- log startup line `User settings file: <full path>` for deterministic support/recovery.
- update troubleshooting documentation for installed vs portable settings file locations.

Primary commit:
- `a47e43ec`

Core files:
- `mRemoteNG/App/Info/SettingsFileInfo.cs`
- `mRemoteNG/App/Initialization/StartupDataLogger.cs`
- `mRemoteNGDocumentation/troubleshooting.rst`

Execution status:
- branch: `codex/pr16-settings-path-observability-2987`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3120
- issue update comment: https://github.com/mRemoteNG/mRemoteNG/issues/2987#issuecomment-3866329467
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3120#issuecomment-3866338725

## PR-17 Require Current Password Before Disabling Protection (`#2673`)

Scope:
- require current password verification before allowing root-node `Password protect` to be disabled.
- keep protection enabled when the password dialog is cancelled or an incorrect password is entered.
- harden `FrmPassword` new-password flow so `OK` does not continue when password verification fails.
- add root password-matching regression tests.

Primary commit:
- `a44d210f`

Core files:
- `mRemoteNG/Tree/Root/RootNodeInfo.cs`
- `mRemoteNG/UI/Controls/ConnectionInfoPropertyGrid/ConnectionInfoPropertyGrid.cs`
- `mRemoteNG/UI/Forms/FrmPassword.cs`
- `mRemoteNGTests/Tree/RootNodeInfoTests.cs`

Execution status:
- branch: `codex/pr17-password-protect-disable-2673`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3121
- issue update comment: https://github.com/mRemoteNG/mRemoteNG/issues/2673#issuecomment-3866347072
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3121#issuecomment-3866359364

## PR-18 Master-Password Autolock on Minimize/Idle (`#1649`)

Scope:
- add root-level `Auto lock on minimize` option, shown only when password protection is enabled.
- persist autolock setting in root XML attribute `AutoLockOnMinimize`.
- lock app when minimized and after 5 minutes of inactivity when autolock is enabled.
- require current master password to unlock when restoring from tray/taskbar.
- add regression coverage for root default and XML serialize/deserialize of autolock flag.

Primary commit:
- `f24380b0`

Core files:
- `mRemoteNG/Tree/Root/RootNodeInfo.cs`
- `mRemoteNG/UI/Controls/ConnectionInfoPropertyGrid/ConnectionInfoPropertyGrid.cs`
- `mRemoteNG/UI/Forms/frmMain.cs`
- `mRemoteNG/Tools/NotificationAreaIcon.cs`
- `mRemoteNG/App/NativeMethods.cs`
- `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlRootNodeSerializer.cs`
- `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsDeserializer.cs`
- tests under `mRemoteNGTests/Config/Serializers/ConnectionSerializers/Xml` and `mRemoteNGTests/Tree`

Execution status:
- branch: `codex/pr18-autolock-1649`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3122
- issue update comment: https://github.com/mRemoteNG/mRemoteNG/issues/1649#issuecomment-3866405356
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3122#issuecomment-3866406010

## PR-19 External Tool Protocol Token (`#1634`)

Scope:
- add `PROTOCOL` variable support in external tool argument parsing.
- keep escaping behavior unchanged for token variants (`%`, `%-`, `%!` prefixes).
- add parser regression coverage for protocol token parsing.

Primary commit:
- `aae9005e`

Core files:
- `mRemoteNG/Tools/ExternalToolArgumentParser.cs`
- `mRemoteNGTests/Tools/ExternalToolsArgumentParserTests.cs`

Execution status:
- branch: `codex/pr19-protocol-1634`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3123
- issue update comment: https://github.com/mRemoteNG/mRemoteNG/issues/1634#issuecomment-3866512854

## PR-20 Main Close Cancel Behavior With Open Panels (`#2270`)

Scope:
- fix main-window shutdown flow to respect cancelled child panel closes.
- avoid hiding main form before child panel close confirmations complete.
- keep app visible when user chooses `No` in panel-close confirmation during app close.

Primary commit:
- `c8dfd10a`

Core file:
- `mRemoteNG/UI/Forms/frmMain.cs`

Execution status:
- branch: `codex/pr20-main-close-cancel-2270`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3124
- issue update comment: https://github.com/mRemoteNG/mRemoteNG/issues/2270#issuecomment-3866527936

## PR-21 Startup XML Recovery From Newest Valid Backup (`#811`)

Scope:
- recover startup from malformed primary XML by loading the newest valid `*.backup`.
- restore primary connections file from the recovered backup.
- ensure invalid XML surfaces as `XmlException` so recovery path is deterministic and testable.
- add regression tests for empty path, XXE rejection, and backup fallback behavior.

Primary commit:
- `dd794c43`

Core files:
- `mRemoteNG/Config/Connections/XmlConnectionsLoader.cs`
- `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsDeserializer.cs`
- `mRemoteNGTests/Config/Connections/XmlConnectionsLoaderTests.cs`

Execution status:
- branch: `codex/pr21-xml-recovery-811`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3125
- issue update comment: https://github.com/mRemoteNG/mRemoteNG/issues/811#issuecomment-3867038036

## PR-22 Close Empty Panel After Last Tab Closes (`#2160`)

Scope:
- auto-close panel when its last tab closes and no documents remain.
- queue panel close on next UI tick to avoid re-entrancy while tab close is in progress.
- guard auto-close flow against shutdown/dispose/invalid-handle paths.

Primary commit:
- `96b4a0b2`

Core file:
- `mRemoteNG/UI/Window/ConnectionWindow.cs`

Execution status:
- branch: `codex/pr22-close-empty-panel-2160`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3126
- issue update comment: https://github.com/mRemoteNG/mRemoteNG/issues/2160#issuecomment-3867237778

## PR-23 Document Tab Drag Auto-Scroll On Overflow (`#2161`)

Scope:
- restore tab-strip auto-scroll while dragging document tabs in an overflowed strip.
- trigger scroll when pointer reaches left/right tab-strip edges during drag.
- throttle auto-scroll step rate to keep drag behavior stable.

Primary commit:
- `c028a0d6`

Core file:
- `mRemoteNG/UI/Tabs/DockPaneStripNG.cs`

Execution status:
- branch: `codex/pr23-tab-drag-overflow-scroll-2161`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3127
- issue update comment: https://github.com/mRemoteNG/mRemoteNG/issues/2161#issuecomment-3867296728

## PR-24 Preserve Restored Config/Connections Panel Focus (`#2171`)

Scope:
- preserve dock-layout-restored active panel state for Config/Connections on startup.
- stop forcing `Connections` panel focus immediately after `pnlLayout.xml` restore.
- keep a safe fallback: focus `Connections` only when no active dock content exists.

Primary commit:
- `cc294d2b`

Core file:
- `mRemoteNG/UI/Forms/frmMain.cs`

Execution status:
- branch: `codex/pr24-config-tree-layout-2171`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3128
- issue update comment: https://github.com/mRemoteNG/mRemoteNG/issues/2171#issuecomment-3867317211

## PR-25 Tab Close/Resize Crash Hardening (`#2166`)

Scope:
- harden document-tab close path during resize/reconnect layout churn.
- validate tab index/content before close and queue close on UI message loop.
- avoid unhandled tab-close crashes from transient dock layout race states.

Primary commit:
- `f95e6ff3`

Core file:
- `mRemoteNG/UI/Tabs/DockPaneStripNG.cs`

Execution status:
- branch: `codex/pr25-tab-crash-resize-hardening-2166`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3129
- issue update comment: https://github.com/mRemoteNG/mRemoteNG/issues/2166#issuecomment-3867358613
- note: form-closing enumeration trace from `#2166` is covered by PR `#3124`

## PR-26 Inheritance Property Label Auto-Size (`#2155`)

Scope:
- auto-size Config/Inheritance property-name column width using visible display names.
- re-apply label-column sizing after config state transitions and property-sort updates.
- cap name-column width to preserve a minimum value column.

Primary commit:
- `c0eae51a`

Core file:
- `mRemoteNG/UI/Window/ConfigWindow.cs`

Execution status:
- branch: `codex/pr26-inheritance-label-width-2155`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3130
- issue update comment: https://github.com/mRemoteNG/mRemoteNG/issues/2155#issuecomment-3867428399

## Excluded From Upstream PRs

- local triage automation/docs:
  - `.project-roadmap/*`
  - local execution logs and report artifacts
- comment-level issue triage evidence is already posted directly on upstream issues.

## Recommended Merge Order

1. PR-1 (security)
2. PR-5 (command-line/process hardening + external-tools escaping)
3. PR-2 (close-panel stability)
4. PR-3 (1Password regression)
5. PR-4 (default external provider handling)
6. PR-6 (SQL Server SNI runtime packaging)
7. PR-7 (SQL schema compatibility hardening)
8. PR-8 (config panel splitter persistence)
9. PR-9 (startup path null/empty fallback)
10. PR-10 (PuTTY provider startup resilience)
11. PR-11 (PuTTY CJK session-name decode)
12. PR-12 (SmartSize focus resilience)
13. PR-13 (fullscreen/redirect-keys guardrail)
14. PR-14 (fullscreen-exit refocus)
15. PR-15 (SmartSize RCW/COM resilience)
16. PR-16 (settings path observability + troubleshooting clarification)
17. PR-17 (require current password before disabling protection)
18. PR-18 (master-password autolock on minimize/idle)
19. PR-19 (external-tool protocol token)
20. PR-20 (main close cancel behavior with open panels)
21. PR-21 (startup XML backup recovery)
22. PR-22 (close empty panel after last tab closes)
23. PR-23 (document tab drag auto-scroll on overflow)
24. PR-24 (preserve restored Config/Connections panel focus)
25. PR-25 (tab close/resize crash hardening)
26. PR-26 (inheritance property label auto-size)

## Operational Guidance

- Create one branch per PR package (`codex/pr1-security-followup`, etc.).
- Use `git cherry-pick -n <commit>` then stage only package files.
- Keep each PR tied to one issue cluster to reduce review cycle time.


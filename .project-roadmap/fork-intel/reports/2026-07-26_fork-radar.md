# Fork Radar - 2026-07-26

Upstream `mRemoteNG/mRemoteNG` - forks scanned for changes worth importing into `robertpopa22/mRemoteNG`.

| Tier | Count |
|---|---|
| Tier A - ready to cherry-pick | 1 |
| Tier B - worth porting by hand | 13 |
| Tier C - watch list | 1 |
| Quarantine - security review required before anything else | 20 |
| Tier D - rejected | 262 |
| not yet triaged | 1 |

## Tier A - ready to cherry-pick

### `7349e5a6aa` Fix main window stuck behind other windows after startup

- **fork:** [k-meeks/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7349e5a6aa3b85440a6f934e5269555c476fbb04) by Kyle Meeks
- **size:** 1 files (+0/-5)
- **score 13** - ready to cherry-pick
- **triage:** bugfix | value 4 | effort 1 | risk 1 | applies likely | IMPORT
- **why:** Removes redundant window activation code that causes inconsistent WinForms state when blocked by Windows on startup. Highly beneficial UX fix.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / grok:REJECT)
  - dissent - codex: REJECT - The diagnosis is unproven here, the lifecycle diverged after the shared code, and no tests or reproduction justify deleting a deliberate focus safeguard.
  - dissent - grok: REJECT - Diff contradicts claimed fix; startup focus is delicate and unverified here.

## Tier B - worth porting by hand

### `dd54616a2e` Fix NullReferenceException + recursive dialog cascade on failed decrypt

- **fork:** [k-meeks/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/dd54616a2e47bdb94e18b2fbafbd2a30764a3728) by Kyle Meeks
- **size:** 1 files (+12/-0)
- **score 10** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 1 | risk 1 | applies conflict | REIMPLEMENT
- **why:** Our XML null guard already prevents the NRE, but still throws into Runtime’s recursive reload path; adapt the null-return behavior to current nullable code.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / grok:APPROVE / claude:REJECT)
  - dissent - codex: REJECT - This fork already prevents the null dereference and duplicate file dialog through guarded validation and explicit-file loading; importing this patch is redundant and regressive.
  - dissent - claude: REJECT - Fork already prevents the crash differently; null-return would only slightly change which error dialog shows for legacy decrypt cancel — marginal value.

### `eb03e059b2` Add configurable interface font (Options > Appearance)

- **fork:** [k-meeks/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/eb03e059b2ecc1a1b00dc70056b70cdb348a2195) by Kyle Meeks
- **size:** 8 files (+224/-4)
- **score 9** - worth doing, needs work
- **triage:** feature | value 4 | effort 3 | risk 2 | applies conflict | IMPORT
- **why:** Adds a highly useful, clean accessibility feature allowing user-customized interface fonts without restarting. Worth importing.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / grok:NEEDS_HUMAN)
  - dissent - codex: REJECT - The accessibility idea is useful, but this untested global override conflicts with existing font behavior and requires target-specific redesign, not direct import.
  - dissent - grok: NEEDS_HUMAN - Nice accessibility tweak, but side effects on panels/DPI and leaks need maintainer review first.

### `0d8b8f6c56` Add "Copy All to Clipboard" to PuTTY connection tab context menu

- **fork:** [k-meeks/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/0d8b8f6c56485861217abdc30a25ae0420827ccf) by Kyle Meeks
- **size:** 5 files (+67/-2)
- **score 7** - port the idea, the patch will not apply
- **triage:** feature | value 3 | effort 2 | risk 2 | applies rewrite | REIMPLEMENT
- **why:** The backend exists, but the requested tab action does not. Add only UI wiring using the existing method and resource, avoiding duplicate backend and localization.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Menu exposure is useful, but only its UI wiring should be reimplemented against the existing method; this commit is not directly landable.
  - dissent - gemini: REJECT - The backend method already exists in our fork. This commit would cause merge conflicts and code duplication, requiring a clean manual reimplementation.

### `2d1411667e` 修复：容器的ID现保持与文件中一致

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2d1411667e60c4e001933d60f8de825dd2ac9213) by Hovn
- **size:** 1 files (+9/-1)
- **score 7** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 2 | risk 2 | applies rewrite | REIMPLEMENT
- **why:** Current XML loading discards serialized container IDs because CopyFrom cannot set get-only ConstantID. Reimplement constructor-based preservation with malformed-ID and round-trip tests.
- **pre-approval:** **MANUAL-REVIEW** (codex:NEEDS_HUMAN / grok:NEEDS_HUMAN)
  - dissent - codex: NEEDS_HUMAN - The defect is real and absent here, but land a tested fork-aware reimplementation covering Container and Entity instead of this stale patch.
  - dissent - grok: NEEDS_HUMAN - Real container ID-stability fix, but needs clean reimplementation and fork check.

### `3f94a2c239` Dark mode: follow the OS, honor the theming setting, dark title bars

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/3f94a2c23980a384cbf15386ae7ffc506a92e6e5) by vindict6
- **size:** 10 files (+298/-11)
- **score 7** - port the idea, the patch will not apply
- **triage:** feature | value 4 | effort 3 | risk 3 | applies conflict | REIMPLEMENT
- **our issue:** #47
- **why:** Follow-OS dark mode + DWM dark title bars addresses open #47. Clean idea, but flips ThemingActive default and our ThemeManager/settings diverged; re-derive carefully.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - OS matching is valuable, but this untested patch conflicts with live-switch and high-contrast theming, assumes restart-only behavior, and requires a scoped reimplementation.
  - dissent - gemini: NEEDS_HUMAN - Valuable dark mode UX improvements matching modern Windows settings, but requires careful refactoring of settings and ThemeManager initialization to prevent regressions.

### `9c4b85f18a` fix: set temp key-file attribute via File.SetAttributes

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9c4b85f18ab51f04b455d198cbf86b284dd6c3f8) by Eran Markus
- **size:** 1 files (+1/-1)
- **score 7** - port the idea, the patch will not apply
- **triage:** bugfix | value 2 | effort 1 | risk 1 | applies rewrite | REIMPLEMENT
- **why:** Replaces redundant throwaway FileInfo instantiation with clean, direct File.SetAttributes call in two PuttyBase temp key generation paths.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / grok:REJECT)
  - dissent - codex: REJECT - It provides no correctness or stability gain; reimplementation would be churn because both APIs set the same attribute and current code has zero warnings.
  - dissent - grok: REJECT - Original object-initializer already sets attributes on disk; pure idiom tweak, not a real fix.

### `a677fae337` Fix ObjectDisposedException when closing a connection tab

- **fork:** [k-meeks/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a677fae337a8c49890c6a0e2d87b9739d708d25d) by Kyle Meeks
- **size:** 1 files (+21/-2)
- **score 7** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 2 | risk 2 | applies conflict | REIMPLEMENT
- **our issue:** #11
- **why:** Closes TOCTOU race in Prot_Event_Closed Invoke; our guards (IsDisposed check) exist but not the try/catch + marshaled re-check. Small defensive win; code diverged.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / grok:APPROVE / claude:REJECT)
  - dissent - codex: REJECT - Current HandleProtocolClosed already has stronger handle, marshaling, disposal-race, and close guards, so this commit offers no unique value and conflicts with intentional semantics.
  - dissent - claude: REJECT - Fork diverged: same race already fixed better (non-blocking BeginInvoke re-marshal, ConnectionWindow.cs:2223-2246). Import adds nothing, code no longer matches.

### `d500a8e9dd` CustomConsPath为相对路径时，主窗口标题也能正确显示全路径（上一提交引入）

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d500a8e9dda08af453e3e69f3d891e2be4145686) by Hovn
- **size:** 1 files (+1/-1)
- **score 7** - port the idea, the patch will not apply
- **triage:** bugfix | value 2 | effort 1 | risk 1 | applies rewrite | REIMPLEMENT
- **why:** Displays absolute path in main window title when loaded with relative path. Simple and safe UX bugfix, needs minor adjustment for our namespaces.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / grok:NEEDS_HUMAN)
  - dissent - codex: REJECT - Current paths are already normalized and CustomConsPath is unused; remaining relative inputs should be normalized at load time, not during rendering.
  - dissent - grok: NEEDS_HUMAN - Small useful title fix for relative paths; confirm null safety and no local equivalent first

### `d6f4872b8b` 标签右键中增加关闭菜单

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d6f4872b8bd1a73e4f293a78243ed424b7347e3e) by Hovn
- **size:** 1 files (+29/-1)
- **score 7** - port the idea, the patch will not apply
- **triage:** feature | value 2 | effort 1 | risk 1 | applies rewrite | REIMPLEMENT
- **why:** Adds Close item to panel-tab context menu; minor UX win. Old mRemoteV1 paths, trivial to redo in our PanelAdder if wanted.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / grok:NEEDS_HUMAN)
  - dissent - codex: REJECT - Importing this stale duplicate adds no capability and risks conflicts or regressions against the maintained implementation already present.
  - dissent - grok: NEEDS_HUMAN - Small useful tab UX, but verify duplication and correct ConnectionWindow close semantics first.

### `0045263765` Show auto-detected PuTTY path on Advanced options page

- **fork:** [k-meeks/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/004526376515164a858c98a9a1c782d04a28c33c) by Kyle Meeks
- **size:** 4 files (+79/-11)
- **score 4** - port the idea, the patch will not apply
- **triage:** feature | value 2 | effort 2 | risk 2 | applies conflict | REIMPLEMENT
- **why:** Small UX win: shows auto-detected PuTTY path in options. Our Designer/options pages diverged heavily; re-do by hand, not cherry-pick.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The fork already exposes the custom override and otherwise always launches bundled PuTTYNG.exe, so this UI is redundant, misleading, and upstream-specific.
  - dissent - gemini: REJECT - Our fork bundles `PuTTYNG.exe` and has not imported the unbundling candidate. This change is redundant and will break the build due to missing auto-detection dependencies.

### `8f39c112b5` fix(rdp): reapply performance flags and input finalizer on all reconnect paths

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/8f39c112b57865efa6c34cd735c1b35394c203dc) by Claude Code
- **size:** 3 files (+18/-5)
- **score 1** - port the idea, the patch will not apply
- **triage:** bugfix | value 2 | effort 3 | risk 3 | applies rewrite | WATCH
- **why:** Reapplying performance flags on mstscax auto-reconnect is a plausible real fix, but patch depends on fork-only view-only/input-finalizer infrastructure we lack. Note idea, not code.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / grok:NEEDS_HUMAN)
  - dissent - codex: REJECT - Exact commit cannot land: RdpProtocol6 was deleted, passive helpers are absent, RdpProtocol8 is refactored, and no tests or reproducible evidence are provided.
  - dissent - grok: NEEDS_HUMAN - Reapplying pFlags on reconnect is useful, but diff is fork-specific and needs local path checks.

### `2a693c85c2` Added SSH Tunnel via SSH_DotNet

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2a693c85c2525ff21e0f35968f9a0745dc612022) by Dawie Joubert
- **size:** 14 files (+1778/-89)
- **score -2** - port the idea, the patch will not apply
- **triage:** feature | value 3 | effort 5 | risk 5 | applies rewrite | WATCH
- **why:** Native SSH.NET forwarding is potentially valuable, but this untested patch depends on an absent protocol and rewrites obsolete tunnel logic; monitor, do not port.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - It adds an untested parallel SSH stack and omits SQL/MariaDB persistence, conflicting with stability, storage consistency, and quick verification requirements.
  - dissent - gemini: REJECT - We do not have the SSH_DotNet protocol implemented. Importing this will break the build and introduces excessive complexity.

### `932e6f6116` Enhance connection handling and UI features

- **fork:** [lthobois/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/932e6f611674e6227db18d977f67a1b577af25a2) by Loïc THOBOIS
- **size:** 15 files (+732/-138)
- **score -2** - port the idea, the patch will not apply
- **triage:** feature | value 2 | effort 4 | risk 4 | applies rewrite | WATCH
- **why:** Mixed bag: new inheritance props (ExternalAddressProvider, RDP StartProgram, gateway token), notification detail, plus personal junk (.vscode, WorldOfFanXP.xml). Partly overlaps our upstream ports; cherry-pick only if users ask.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - A 732-line mixed, untested commit also bypasses notification filters, leaks writer subscriptions, duplicates shipped UI/retry features, adds untranslated labels, and uses noncanonical tooling.
  - dissent - gemini: REJECT - This is a mixed bag of personal settings, French locale scripts, and features already integrated or overlapping with our upstream ports. Not suitable for import.

## Quarantine - security review required before anything else

### `707aa11f89` Fix critical bugs identified in codebase review

- **fork:** [MyLabs-LLC/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/707aa11f897caf10b49b97ce6f6887328aae3ba7) by Cursor Agent
- **size:** 6 files (+31/-17)
- **score 10** - security review required (high)
- **triage:** bugfix | value 3 | effort 1 | risk 1 | applies conflict | REIMPLEMENT
- **why:** Most fixes already ours (MySQL builder, factory throw, Dispose logic, PuttyBase rewritten). Real gap: EncryptedSecureString disposes static _machineKey + string-concat key. Reimplement just that.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Current HEAD has stronger MySQL, disposal, factory, and PuTTY fixes; reimplement only the EncryptedSecureString shared-key lifetime correction.
  - dissent - gemini: REJECT - Our fork already solved these bugs cleanly with non-blocking async window searches, a secure connection builder, and proper disposal. Importing this would cause regressions.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/EncryptedSecureString.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/RandomGenerator.cs` - credential and crypto paths need human review regardless of intent

### `da67bb7bad` Fix PowerShell credential exposure and increase PBKDF2 iterations

- **fork:** [MyLabs-LLC/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/da67bb7bad76f1fe41528297583dc5eba6bd006a) by Cursor Agent
- **size:** 2 files (+16/-5)
- **score 8** - security review required (high)
- **triage:** security | value 5 | effort 3 | risk 4 | applies rewrite | REIMPLEMENT
- **why:** PBKDF2 is already 600,000, but PowerShell passwords remain in argv. Reimplement per-child secret transfer; this process-global environment patch races and leaks across launches.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / grok:REJECT)
  - dissent - codex: REJECT - Current code already uses 600,000 iterations; the [candidate diff](https://github.com/MyLabs-LLC/mRemoteNG/commit/da67bb7bad76f1fe41528297583dc5eba6bd006a) needs a target-specific, per-child credential channel instead of direct import.
  - dissent - grok: REJECT - Security intent fits, but direct import is unsafe; needs redesign, not this patch.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/SymmetricEncryption/AeadCryptographyProvider.cs` - credential and crypto paths need human review regardless of intent

### `7241b81225` Add a workflow to build PuTTYNG from a pinned PuTTY tag

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7241b81225eefc24e07cc6830fdb818b638a785d) by vindict6
- **size:** 1 files (+217/-0)
- **score 7** - security review required (critical)
- **triage:** security | value 3 | effort 2 | risk 2 | applies likely | IMPORT
- **why:** Pinned, anchor-verified PuTTYNG build workflow (dispatch-only) improves supply-chain reproducibility of shipped PuTTYNG.exe; standalone file, no conflict with our CI.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:APPROVE / grok:NEEDS_HUMAN)
  - dissent - codex: REJECT - Updating PuTTY is valuable, but this workflow breaks the fork’s detection contract and also interpolates untrusted input into PowerShell.
  - dissent - grok: NEEDS_HUMAN - Useful pinned PuTTYNG build CI for SSH stability, but patch parity and binary drop need maintainer judgment.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_PuTTYNG.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `4e5bc44024` Update bundled PuTTYNG to 0.84

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4e5bc4402415db3b351bdb4845d3b70613796856) by vindict6
- **size:** 2 files (+12/-3)
- **score 5** - security review required (critical)
- **triage:** chore | value 3 | effort 2 | risk 3 | applies conflict | REIMPLEMENT
- **why:** PuTTYNG 0.84 bump worthwhile, but never import third-party binary. Rebuild via our Build_PuTTYNG.yml from tag; tag-check workflow tweak worth porting.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / grok:NEEDS_HUMAN)
  - dissent - codex: REJECT - The binary regresses verified provenance, and its workflow file does not exist here; rebuild version 0.84 internally and preserve signing.
  - dissent - grok: NEEDS_HUMAN - 0.84 bump helps security but binary swap and detector quirks need maintainer test judgement.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_PuTTYNG.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `binary-artifact` (critical) in `mRemoteNG/PuTTYNG.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)

### `197f6fbc91` Fix SonarCloud vulnerabilities and critical bugs on develop branch

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/197f6fbc91c7053fa489096293df61e7909985fd) by Eran Markus
- **size:** 5 files (+8/-12)
- **score 4** - security review required (critical)
- **triage:** bugfix | value 2 | effort 2 | risk 2 | applies conflict | REIMPLEMENT
- **why:** CI workflow already scoped better; RSA 2048 cosmetic. Reimplement: ToolTipControl.HasBorder missing backing field write and PuttyBase insecure temp file paths.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Reject wholesale: workflow hardening already exists, RSA sizing is ineffective after immediate import, and only the tooltip fixes merit isolated reimplementation.
  - dissent - gemini: REJECT - This commit blindly fixes static analyzer warnings, introducing critical regressions for non-2048-bit keys and weaker temporary file creation.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `security-code` (high) in `ExternalConnectors/CPS/PasswordstateInterface.cs` - credential and crypto paths need human review regardless of intent

### `58ebf6e3fa` Read and write machine-bound connection files

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/58ebf6e3faf513390fbcdbfb1dd033651dd00475) by vindict6
- **size:** 7 files (+479/-5)
- **score 4** - security review required (high)
- **triage:** security | value 4 | effort 4 | risk 4 | applies conflict | WATCH
- **our issue:** #128
- **why:** DPAPI-wrapped master key fixes mR3m weak-default (our #128/incident #92 concern). Real value, but new file-format attribute, compat + serializer conflicts with our 1600-commit divergence. Reimplement deliberately if pursued.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - The fork already solves KDF cost through 600,000-iteration caching, while this stale patch conflicts with certificate, TOTP, and serializer changes.
  - dissent - gemini: NEEDS_HUMAN - Addresses critical default password weakness, but breaking file portability and causing serializer conflicts requires deliberate human decision and custom reimplementation.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/Factories/CryptoProviderFactoryFromXml.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Factories/MasterKeyProviderFactory.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/SymmetricEncryption/MasterKeyCryptographyProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Security/MasterKeyCryptographyProviderTests.cs` - credential and crypto paths need human review regardless of intent

### `62252cb2ee` Add RDP multi-monitor spanning and credential resolver

- **fork:** [MyLabs-LLC/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/62252cb2ee79b0ba3bceb83b2dee84186b41db1d) by Cursor Agent
- **size:** 5 files (+383/-7)
- **score 4** - security review required (high)
- **triage:** feature | value 3 | effort 3 | risk 3 | applies conflict | REIMPLEMENT
- **why:** RDP span-all-screens is real user value; reimplement spanning atop our ported RDP code. Skip CredentialResolver refactor — security-sensitive churn, no bug it fixes.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - Our fork already implements persisted, tested RDP multimonitor behavior; this patch adds a broken parallel path, dead resolver code, unsupported providers, and no tests.
  - dissent - gemini: NEEDS_HUMAN - RDP multi-monitor spanning is highly valuable, but the credential resolution code must be manually adapted to include our fork's existing providers.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Connection/CredentialResolver.cs` - credential and crypto paths need human review regardless of intent

### `9db653a73c` fix: create the temporary PuTTY private-key file atomically

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9db653a73c383a46b094c337b77a53418e5c179c) by Eran Markus
- **size:** 2 files (+37/-13)
- **score 4** - security review required (critical)
- **triage:** security | value 2 | effort 2 | risk 2 | applies rewrite | REIMPLEMENT
- **why:** Target reopens an already-created key path; reimplement exclusive creation while preserving secure wipe. Omit superseded CI permissions; this still does not close PuTTY handoff race.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / grok:REJECT)
  - dissent - codex: REJECT - Current code already atomically reserves temp files and securely wipes them; the CI permission already exists, making this stale patch duplicative and regressive.
  - dissent - grok: REJECT - Idea fits credential safety, but mixed commit and tiny gain—reimplement only if still missing.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `d9db37352a` Add option: Open all selected connections with Enter

- **fork:** [julesbobb/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d9db37352a78e91564552f20b585aa31309da446) by Jules Bobb
- **size:** 11 files (+353/-8)
- **score 4** - security review required (high)
- **triage:** feature | value 2 | effort 2 | risk 2 | applies rewrite | REIMPLEMENT
- **why:** Multi-select Connect already works via the context menu; Enter still opens one node. Reimplement atop GetSelectedNodes and ConnectionInitiator; discard divergent folder logic and GUI-only wrapper.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / grok:REJECT)
  - dissent - codex: REJECT - The feature is useful, but this patch bypasses existing multi-selection/opening logic, adds obsolete artifacts, and requires a focused reimplementation for current architecture.
  - dissent - grok: REJECT - Low-priority UX, not core stability work; skip or rewrite without version noise.
- **security flags:**
  - `opaque-file` (high) in `mRemoteNG/Language/Language.resources` - added file has no reviewable text diff

### `b597f6ee1b` Add SCP/SFTP file browser with dual-pane transfer interface

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b597f6ee1bb94081f2a0e62b7a46a672d5969696) by Dawie Joubert
- **size:** 15 files (+3376/-5)
- **score 3** - security review required (critical)
- **triage:** feature | value 4 | effort 5 | risk 4 | applies rewrite | WATCH
- **why:** Dual-pane SCP/SFTP browser is genuinely new and user-visible, but 3.4k lines built on that fork's serializer/property layout; would need reimplementation plus security review of transfer code.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / grok:REJECT)
  - dissent - codex: REJECT - It duplicates existing SSHTransferWindow/SecureTransfer functionality with 3,376 untested lines, and is neither small nor quickly verifiable despite some UX value.
  - dissent - grok: REJECT - Large speculative feature outside fork priorities; not small/clear enough for quick maintainer landing.
- **security flags:**
  - `network-download` (critical) in `mRemoteNG/Connection/Protocol/SCP/ScpTransferManager.cs` - added code fetches remote content at build or run time
  - `network-download` (critical) in `mRemoteNG/UI/Controls/SCP/ScpFileTransferControl.cs` - added code fetches remote content at build or run time

### `1534a261c8` Heavy refactor & move to fork

- **fork:** [Zarlengo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/1534a261c8d547df36f73588f9e37c924e314abd) by Chris Zarlengo
- **size:** 14 files (+1152/-0)
- **score 1** - security review required (critical)
- **triage:** feature | value 3 | effort 4 | risk 4 | applies rewrite | WATCH
- **why:** Bitwarden external credential connector — absent in our fork, plausible user value, but 1150+ lines with process-exec/security surface and fork-specific refactor; watch upstream maturity.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The untested 1,152-line import is functionally broken, collides with PasswordSafe's enum value 5, and requires redesign against current credential-provider plumbing.
  - dissent - gemini: REJECT - We avoid large speculative rewrites and external dependency integrations (like Bitwarden) that increase attack surface and maintenance overhead.
- **security flags:**
  - `process-exec` (critical) in `ExternalConnectors/BW/BitwardenCommandRunner.cs` - added code spawns a process or evaluates a string as code
  - `security-code` (high) in `mRemoteNG/Connection/ExternalCredentialProviderSelector.cs` - credential and crypto paths need human review regardless of intent

### `bcc65a861a` Add passphrase key derivation for exported connection files

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bcc65a861a88d82f44168135e83d974422c2c058) by vindict6
- **size:** 4 files (+376/-1)
- **score 1** - security review required (high)
- **triage:** security | value 3 | effort 4 | risk 4 | applies rewrite | WATCH
- **why:** Argon2id passphrase export keys is sound, but builds on vindict6-only ConnectionFileProtection scheme absent from our fork (verified via grep); would need full redesign atop our PBKDF2/MasterPasswordGate stack.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The security goal fits, but this commit is an unwired stacked fragment whose required master-key infrastructure is absent from the target fork.
  - dissent - gemini: REJECT - Relies on vindict6's custom KeyProtection / ConnectionFileProtection architecture absent from our fork, requiring a major redesign to integrate.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/Factories/MasterKeyProviderFactory.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/KeyDerivation/Argon2idKeyGenerator.cs` - credential and crypto paths need human review regardless of intent

### `eeb7944d3d` Add SFTP connection to Linux remote server for file transfer functionality

- **fork:** [raohj1987/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/eeb7944d3d6d973e20390cfb1067c10c7adeefff) by raohj1987
- **size:** 8 files (+1963/-1)
- **score 1** - security review required (critical)
- **triage:** feature | value 3 | effort 4 | risk 4 | applies rewrite | WATCH
- **why:** SFTP browser could add value, but 2K-line drop with password-only auth, plaintext handling, duplicate of SSHTransferWindow scope. Reimplement properly only if users request it.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - It duplicates existing SFTP support with 1,963 untested lines; fake session reuse, cross-thread UI access, and nullable warnings make quick verification impossible.
  - dissent - gemini: REJECT - Violates guidelines against introducing external dependencies, massive unverified UI rewrites, and features overlapping with existing SSH file transfer functionality.
- **security flags:**
  - `network-download` (critical) in `mRemoteNG/UI/Window/SftpFileManagerWindow.cs` - added code fetches remote content at build or run time

### `4cded1cc37` Add master password feature with startup unlock, hint, and settings migration

- **fork:** [yosale2011/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4cded1cc376680b686a5fd219c982e16920e7fd4) by Yosale2011
- **size:** 19 files (+1226/-18)
- **score -1** - security review required (high)
- **triage:** feature | value 3 | effort 4 | risk 5 | applies rewrite | WATCH
- **our issue:** #128
- **why:** App-level master password with settings re-encryption. Overlaps our MasterPasswordGate + WebAuthn/Entra ID hardening. Homegrown key-hierarchy migration needs deep security review; touches Runtime.EncryptionKey.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - It overlaps the existing master-password gate, adds a forbidden dependency/build-path change, breaks a current EncryptionKey assignment, and leaves critical migration/startup behavior untested.
  - dissent - gemini: REJECT - The commit includes an unwanted external dependency (AxMSTSCLib) that risks breaking the custom MSBuild setup for COM references, violating explicit fork guidelines.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `MASTER_PASSWORD_FEATURE.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/App/MasterPasswordService.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/XmlConnectionsDecryptor.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/XmlKeyValidator.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/UI/Forms/FrmPassword.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/UI/Forms/MasterPasswordManager.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `7c4c9d891f` Refactor RDP protocol initialization and enhance error handling; update launch configurations

- **fork:** [lthobois/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7c4c9d891f81a8d2fbae959ed4c5cc010cf94363) by Loïc THOBOIS
- **size:** 5 files (+163/-89)
- **score -1** - security review required (high)
- **triage:** refactor | value 2 | effort 3 | risk 4 | applies conflict | WATCH
- **why:** Mixed bag: useful init/connect failure logging, but interop assembly-preload hack dubious on .NET 10; our RdpProtocol heavily diverged. Cherry-pick logging only if needed.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The commit conflicts with the fork’s async path, existing diagnostics, assembly layout, exception semantics, and canonical build tooling.
  - dissent - gemini: REJECT - The change conflicts with our .NET 10 SDK-style project, lacks InitializeAsync integration, and contains localized/VS Code-specific launch configurations.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `2c92360d63` Add master key primitives for machine-bound connection files

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2c92360d63d7190e125cb6194ba1611856fe48a5) by vindict6
- **size:** 8 files (+426/-3)
- **score -2** - security review required (critical)
- **triage:** security | value 2 | effort 4 | risk 4 | applies conflict | WATCH
- **why:** DPAPI-bound master key is sound but unwired primitives; breaks file portability, CI change gates tests to Security only. Our KDF-cost issue already fixed (#120). Revisit if integrated.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - The primitives are unused, their KDF-performance rationale is already solved by cached 600k PBKDF2, and the divergent base makes import nontrivial.
  - dissent - gemini: NEEDS_HUMAN - The DPAPI master key security primitives are highly valuable for credential protection, but the workflow changes must be discarded so all tests remain active.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `security-code` (high) in `mRemoteNG/Security/KeyProtection/DpapiMasterKeyProtector.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/KeyProtection/IMasterKeyProtector.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/KeyProtection/MasterKey.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/SymmetricEncryption/AeadCryptographyProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Security/KeyedCryptographyTests.cs` - credential and crypto paths need human review regardless of intent

### `8ae75ec5d2` postregsql database support

- **fork:** [wolverine2k/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8ae75ec5d2012a91457c3d210a2a7e97724feda6) by Sylvain LAFFONT
- **size:** 7 files (+121/-5)
- **score -2** - security review required (high)
- **triage:** feature | value 2 | effort 4 | risk 4 | applies rewrite | WATCH
- **why:** PostgreSQL backend is genuinely new, but code targets pre-rework SQL layer (old SqlClient, no v3.5 schema/upgrade path, SELECT * CommandBuilder our #145/#148 fixes replaced). Reimplement only if users ask.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - It also adds an obsolete preview dependency, no PostgreSQL schema lifecycle, and no tests; adapting it requires clean reimplementation, not a quick import.
  - dissent - gemini: REJECT - PostgreSQL support uses obsolete CommandBuilder SELECT * patterns we removed, lacks schema upgrades, and adds an unneeded Npgsql preview package.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `c36a1c1028` Retarget to .NET 11

- **fork:** [CancanTang/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c36a1c1028a10331624f782f59a08cbe099bc7a8) by CancanTang
- **size:** 1 files (+1/-1)
- **score -3** - security review required (high)
- **triage:** chore | value 1 | effort 2 | risk 4 | applies likely | WATCH
- **why:** .NET 11 still preview (GA Nov 2026). Our toolchain/CI pinned to net10.0 + VS BuildTools. Revisit at GA, not before.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / grok:REJECT)
  - dissent - codex: REJECT - The fork intentionally standardizes on .NET 10; this isolated framework bump provides no benefit and requires a coordinated solution-wide toolchain migration.
  - dissent - grok: REJECT - Fork is deliberately on .NET 10; one-line retarget is a high-risk platform move, not useful now.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `283a7e7fec` Add experimental SSH_DotNet protocol implementation with Trace logging

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/283a7e7fecdcb0e1665f944996e92ef30a52e7dd) by Dawie Joubert
- **size:** 32 files (+4205/-199)
- **score -5** - security review required (high)
- **triage:** feature | value 2 | effort 5 | risk 5 | applies rewrite | WATCH
- **why:** 4.2k-line experimental SSH.NET managed terminal. Interesting long-term (PuTTY replacement) but immature, trace-heavy, unreviewed. Watch fork maturity, do not import now.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - It cannot land as one commit, duplicates existing SSH, and is far too broad and experimental for stability-first, quickly verifiable maintenance.
  - dissent - gemini: REJECT - Large experimental protocol implementations are high-risk, hard to verify quickly, and diverge from our focus on stability and codebase cleanliness.
- **security flags:**
  - `build-script` (high) in `run_ssh_tests.ps1` - scripts execute on a maintainer machine

### `2c1b08114d` feat(phase-1+2): Settings migration, DI wiring, PuTTY providers, Credential/PortScanner dialogs

- **fork:** [Morgadoo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2c1b08114d737158fd981add1db384b59b99ce4a) by Claude
- **size:** 11 files (+396/-9)
- **score -5** - security review required (high)
- **triage:** feature | value 2 | effort 5 | risk 5 | applies rewrite | WATCH
- **our issue:** #137
- **why:** Avalonia cross-platform rewrite scaffolding (DI, dialogs, Linux PuTTY provider). Whole-architecture divergence; only relevant if we pursue #137 macOS. Track fork progress.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - It cannot compile as imported; dialogs are placeholders, crypto wiring is unused mutable global state, and cross-platform dependencies contradict this fork’s Windows-only direction.
  - dissent - gemini: REJECT - Project explicitly forbids large speculative rewrites and relies on WinForms, making this Avalonia migration unacceptable.
- **security flags:**
  - `security-code` (high) in `mRemoteNG.Avalonia/ViewModels/CredentialManagerViewModel.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG.Avalonia/Views/Dialogs/CredentialManagerDialog.axaml` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG.Avalonia/Views/Dialogs/CredentialManagerDialog.axaml.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/SecureXmlHelper.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

## Tier C - watch list

### `374eb8a34a` fixed WinSCP extended arguments

- **fork:** [wolverine2k/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/374eb8a34ab3e07a26554751cbfc0f67280ec28a) by radiosti
- **size:** 1 files (+1/-1)
- **score 4** - keep an eye on it
- **triage:** docs | value 1 | effort 1 | risk 1 | applies likely | IMPORT
- **why:** One-char docs typo: WinSCP flag is -rawsettings not -rawsetting. Trivial, correct, zero risk if cheat sheet file still exists.

## Tier D - rejected

### `6d3b170f0e` Apply theme changes immediately, no restart required

- **fork:** [k-meeks/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6d3b170f0e4c688deeb3e1ca44c1843184d7e092) by Kyle Meeks
- **size:** 1 files (+4/-4)
- **score 12** - already covered or rejected at triage
- **triage:** bugfix | value 5 | effort 1 | risk 1 | applies likely | REJECT
- **why:** Our fork already supports live theme changes immediately without requiring a restart, making this change redundant.

### `08b37384ed` refactor(ssh_dotnet): canonical IDisposable + fix ProtocolBase.Dispose (S3881/S2930)

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/08b37384ed26c40d7da7970085e7d4fd2b350399) by Dawie Joubert
- **size:** 2 files (+23/-3)
- **score 9** - already covered or rejected at triage
- **triage:** refactor | value 4 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** ProtocolBase.Dispose is already virtual and corrected in our fork. The SSH_DotNet protocol files are not present on our main branch.

### `7ea1eded8c` Add setting for opening multiple connections with Enter

- **fork:** [julesbobb/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7ea1eded8cd1d73bea965ff47b14040bdd93f742) by Jules Bobb
- **size:** 3 files (+17/-7)
- **score 8** - already covered or rejected at triage
- **triage:** feature | value 3 | effort 3 | risk 1 | applies conflict | REJECT
- **why:** Incomplete commit. It is part of a multi-commit feature (not mapping to any open issue) and contains only settings boilerplate without the actual behavior.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `12887e577d` Allow parentheses in executable paths (External Tools, custom PuTTY path)

- **fork:** [k-meeks/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/12887e577db9a75f16d526e0434dd32eedb42a50) by Kyle Meeks
- **size:** 1 files (+6/-2)
- **score 6** - already covered or rejected at triage
- **triage:** bugfix | value 3 | effort 1 | risk 1 | applies likely | REJECT
- **why:** Our PathValidator.cs:65 already excludes parentheses with identical char set and same Program Files (x86) rationale. Fully covered.
- **security flags:**
  - `process-exec` (critical) in `mRemoteNG/Tools/PathValidator.cs` - added code spawns a process or evaluates a string as code

### `06abf2cf01` Revert "Fix RDP mouse capture after fullscreen leave and fine tune scroll edge"

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/06abf2cf01d7361af4b54af00fd0821d6e0009e6) by guvity
- **size:** 2 files (+28/-281)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Reverts custom mouse capture logic for passive RDP monitoring files (RdpProtocol6/RdpInputBlocker) that do not exist in our main branch.

### `108b95a586` refactor(ssh_dotnet): make FormatBytes static (S2325)

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/108b95a586e5197d94123762c21e04f7d25049f2) by Dawie Joubert
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Touches fork-specific ProtocolSshDotNet.cs (joubertdj SSH.NET protocol); file does not exist in our fork. Trivial static modifier.

### `195df32f12` Changed PublicGetDirectChildConnections to static

- **fork:** [julesbobb/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/195df32f12d5e0dc7a0bb88b29a85adad0367581) by Julian Bobbett (DHCW - Software Development)
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Tweaks fork-only test wrapper PublicGetDirectChildConnections; method absent in our ConnectionTreeWindow.cs. Nothing to apply.

### `2b1ed1b8e1` Firefox组件崩溃问题待解决

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2b1ed1b8e12cec4cc37be62c0cbbe41eb3dc407b) by Hovn
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Comment-only mojibake on obsolete Gecko/Xpcom code; our fork removed that engine and the commit fixes no executable behavior.

### `334ad2463c` ci: switch v4 workflow to manual dispatch only (avoid auto-builds during testing)

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/334ad2463ce02fca43475e26c0352202db298576) by Claude Code
- **size:** 2 files (+6/-3)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fork-private CI workflow tweak for their own passive-rdp test branch; file does not exist in our repo, zero relevance.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-1772-v4.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `380c221b56` docs(ssh_dotnet): remove planning/design docs from PR (keep local)

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/380c221b56f2c36bd3b9691c7e20f917daf6f811) by Dawie Joubert
- **size:** 4 files (+0/-5084)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Deletes planning docs specific to joubertdj's SSH_DotNet PR branch; files never existed in our fork.

### `3d580a2c57` Revert "Commit passive RDP auto-scroll position like manual scrollbar movement"

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/3d580a2c5730b090764de673a0946aa493d1bcfb) by guvity
- **size:** 1 files (+0/-180)
- **score 4** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Reverts an experimental scroll-commit feature of the guvity fork; since our codebase does not have that feature, this is not needed.

### `3e249e9198` chore(rdp): stop writing connection bar diagnostics to file (bar fix confirmed)

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/3e249e91986e07c9daa08d1639e28081360067f7) by Claude Code
- **size:** 2 files (+10/-12)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Removes debug file logging from guvity's experimental connection-bar pinning feature, which does not exist in our fork.

### `54a4f01241` changed project framework to .net5

- **fork:** [changsongyang/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/54a4f01241d3c7cc1378a4b556703ff09b2042be) by Faryan Rezagholi
- **size:** 3 files (+3/-3)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** We are already modernized to .NET 10.0; targeting .NET 5.0 is obsolete and a downgrade.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNGSpecs/mRemoteNGSpecs.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNGTests/mRemoteNGTests.csproj` - a new or repointed package can pull arbitrary code at restore time

### `56188a5104` SSH Dot Net Cert and Quality plan implemented

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/56188a510401818cd80a3afbbac237874f402ee1) by Dawie Joubert
- **size:** 1 files (+761/-0)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies likely | REJECT
- **why:** A markdown quality plan document specific to another fork's pending PR. Does not contain actual code or features applicable to our codebase.
- **security flags:**
  - `env-secret-access` (critical) in `SSH_DotNet Cert and Quality Control Plan 20260621.md` - added code reads credentials or CI secrets

### `589a144f9f` removed and re-added COnsoleControle and MySQL.Data nuget packages in hopes to fix appveyor reference error

- **fork:** [stdexception/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/589a144f9fc5c81549febbb82b24b1fc4bccc713) by Faryan Rezagholi
- **size:** 5 files (+9/-5)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Applies to legacy .NET Framework net472 with packages.config. Our fork is on .NET 10 SDK-style and doesn't use these legacy packages.
- **security flags:**
  - `opaque-file` (high) in `mRemoteV1/Console.ico` - added file has no reviewable text diff
  - `dependency-manifest` (high) in `mRemoteV1/mRemoteV1.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteV1/packages.config` - a new or repointed package can pull arbitrary code at restore time

### `62a651edd3` Update CODEBASE_REVIEW.md with implementation status for all fixes and new features

- **fork:** [MyLabs-LLC/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/62a651edd3b87a94bb6fbf68c231ce5985934981) by Cursor Agent
- **size:** 1 files (+33/-15)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Fork-internal CODEBASE_REVIEW.md status update; file does not exist here and tracks their fork's changes.

### `66ad958c47` Fix Visual Studio targets path in passive RDP build

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/66ad958c477e0217554c90af1963098f67c88a65) by guvity
- **size:** 1 files (+2/-2)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fixes a workflow file (passive-rdp-monitor-build.yml) that only exists in that fork; our build.ps1/CI already auto-detect VS. Not applicable.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-build.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `6925ae1c36` Create LICENSE.md

- **fork:** [changsongyang/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6925ae1c367c065d74abbfd6a430ae6c13dd111a) by Faryan Rezagholi
- **size:** 1 files (+339/-0)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies likely | REJECT
- **why:** Redundant. Our repository already contains COPYING.txt, which includes the full GNU General Public License Version 2.
- **security flags:**
  - `license` (medium) in `LICENSE.md` - licence edits change redistribution terms

### `6de1baa622` 修正：拼写错误srtWorkingDirectory修正为strWorkingDirectory

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6de1baa62201788a72a1688a1cf7f4f5f7929bc7) by Hovn
- **size:** 4 files (+5/-5)
- **score 4** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Our fork modernized and replaced the srtWorkingDirectory key with WorkingDirectory entirely, rendering this legacy typo fix obsolete and non-applicable.

### `6efc894af1` fix: disambiguate WinForms message filter type

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/6efc894af1a0e7b4dfe423ca5f6e79231250a537) by guvity
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** One-line type disambiguation for guvity's fork-only message filter in RdpProtocol6.cs; code absent in our fork.

### `731d7a0ad1` Rename LICENSE.md to LICENSE.txt

- **fork:** [changsongyang/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/731d7a0ad12d317aacec95fb1c79fc7d77012829) by Faryan Rezagholi
- **size:** 1 files (+0/-0)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies likely | REJECT
- **why:** LICENSE.md to LICENSE.txt rename; cosmetic, no benefit, breaks existing links.
- **security flags:**
  - `license` (medium) in `LICENSE.txt` - licence edits change redistribution terms

### `7ac6ed40e3` Fix last 2 SonarCloud JS issues: log caught exceptions

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7ac6ed40e317eb11f3fffc313f8723f17cde7e2d) by Eran Markus
- **size:** 1 files (+4/-4)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Edits xterm-terminal.html which does not exist in our fork; fork-specific SSH terminal, SonarCloud JS lint noise.

### `8406f0c02c` Update CHANGELOG with PR #3371

- **fork:** [yosale2011/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8406f0c02cdcce3db7127d4d8bf149a268902ea2) by Yosale2011
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Upstream CHANGELOG line edit for PR #3371; our fork has its own release model and changelog history. No benefit.

### `84b05339ac` docs: add HANDOFF.md with passive RDP v4 roadmap and diagnosis

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/84b05339ac965c97d48990460163df4e9762b3fa) by Claude Code
- **size:** 1 files (+264/-0)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies likely | REJECT
- **why:** Foreign .NET 6 passive-RDP handoff documents nonexistent ViewOnly code and unfinished work; it contains no implementation and does not directly address our open issues.

### `8783ed3eca` Change terminal re-initialization guard to Debug log level

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8783ed3ecaefbd72e7007ddbc4daef9d2aa44a85) by Dawie Joubert
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fork-only SshTerminalControl is absent; SSH1/SSH2 use PuttyBase, so this logging-only tweak has no code path or user-visible benefit.

### `88c7af4a61` docs(ssh_dotnet): changelog entry for private-key auth + SQL limitation

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/88c7af4a61a305d2c0a6a4e90a9e895a4851bb38) by Dawie Joubert
- **size:** 1 files (+1/-0)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Changelog entry for the custom SSH_DotNet feature which is not present or desired in our fork.

### `8a0b324f79` docs: clarify all remaining blocked tasks in MIGRATION_PROGRESS.md

- **fork:** [Morgadoo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8a0b324f79aabf9671133d3e7a6cf099fe84ed6f) by Luís Morgado
- **size:** 1 files (+51/-28)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** File MIGRATION_PROGRESS.md does not exist in our repository. It tracks a different fork's specific Avalonia UI migration status.

### `8f3746d650` 调整：外部工具默认不显示在外部工具栏

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8f3746d650bc67fdee183d84a1e4ba413b1ecb2f) by Hovn
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies likely | REJECT
- **why:** Subjective UX preference change. Standard expected behavior in mRemoteNG is to display newly created external tools on the toolbar by default.

### `92eae2e42e` Fix errors

- **fork:** [Zarlengo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/92eae2e42e1f2805bced75e4d80829ec0f2f47e7) by Chris Zarlengo
- **size:** 3 files (+6/-6)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Fixes compile breakage in that fork's own Bitwarden connector code plus version bump; our fork lacks their broken code, nothing to import.

### `9c48be3008` bump minor version and build number up

- **fork:** [Ahmed-ElHamidy/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9c48be30086691de64e7a4d2a29c3cb241e45f87) by AHMED OMAR ELHAMIDY
- **size:** 3 files (+7/-7)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Fork-local version bump plus cosmetic colon in one log message; our fork has own versioning (1.82.0), zero benefit.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `a09c5e9416` docs: C1 build succeeded on GitHub Actions (portable zip artifact ready)

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/a09c5e94166012e249497d0befc3010a1f65e7ef) by Claude Code
- **size:** 1 files (+9/-2)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fork-internal HANDOFF.md progress journal (Russian, fork-specific CI run notes). Zero relevance to our fork.

### `a3241cac85` Fix ambiguous WinForms Message reference

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/a3241cac85258c371bdc3ce97a7a0d94d76af1f1) by guvity
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fixes ambiguity inside RdpInputBlocker, a class only in that fork's passive-RDP feature; no such code or compile error here.

### `b41ff4f4ec` Ignore local CLAUDE.md project notes

- **fork:** [k-meeks/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b41ff4f4ece4ca3c9ac027423a35bc9d6a3e7617) by Kyle Meeks
- **size:** 1 files (+3/-0)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Our fork commits CLAUDE.md as tracked project canon; ignoring it would break our workflow. Fork-local housekeeping only.

### `bcaa39f4db` windows-agent: add file logging so it runs silently in background

- **fork:** [nickbeentjes/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bcaa39f4db1b25d19f4335019d0b709fc40d0865) by Kees
- **size:** 1 files (+9/-3)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Touches windows-agent/poller.py, fork-private infrastructure tooling unrelated to mRemoteNG; directory absent in our fork.

### `bd8bb8a0d0` 优化：外部工具图标未能从文件获取时，可从Icons目录获取

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bd8bb8a0d08671e7af045f093e887a8c85420bf7) by Hovn
- **size:** 1 files (+12/-1)
- **score 4** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 2 | risk 2 | applies rewrite | REJECT
- **why:** Cosmetic external-tool icon fallback from ancient mRemoteV1 layout; no user demand in our tracker; would need reimplementation for marginal benefit.

### `cbb9828294` Fix 2 remaining SonarCloud bugs for quality gate

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/cbb98282944e4a03c921e68aa6b9deeb6da3634a) by Eran Markus
- **size:** 2 files (+1/-7)
- **score 4** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Touches SSHTerminalBase.cs and SftpBrowserPanel.cs — fork-private features that do not exist in our tree. Nothing to apply.

### `d03faccf48` fix(rdp): resolve ambiguous Message reference in ConnectionBarPinner (build fix)

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/d03faccf484ed801f9c324f0fb17e7aa3510aa4f) by Claude Code
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fixes build ambiguity in guvity's passive-RDP subclass ConnectionBarPinner in RdpProtocol6.cs. Neither the class nor the file exists in our fork.

### `d18ad827cf` Fix all remaining SonarCloud code smells

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d18ad827cff3dc973c3caad1f476421a9de748c3) by Eran Markus
- **size:** 5 files (+74/-81)
- **score 4** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fixes SonarCloud smells on eran132's custom WebView2 SSH terminal and SFTP browser files, which are not present in our fork.

### `dec6fea904` docs(ssh_dotnet): Update Claude todo

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/dec6fea90437ac087750e302995b8c857ab1eaa0) by Dawie Joubert
- **size:** 1 files (+186/-0)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fork-internal TODO/plan text file for their own SSH_DotNet feature branch; no code, no relevance to our fork.

### `e13824b818` add credits

- **fork:** [azet/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e13824b818c76d9c18135663c91ed8b9cb2480f9) by Aaron Zauner
- **size:** 1 files (+2/-1)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies likely | REJECT
- **why:** Fork author adds own name to CREDITS.md; not a contributor to our fork.

### `e53cd9c680` Remove planning document from feature branch

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e53cd9c6809cc5ce380978b6c5199117398a7de4) by Dawie Joubert
- **size:** 1 files (+0/-2913)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Removes planning document from fork-specific feature branch which does not exist in our fork.

### `e77a3ff94e` add Serial UI feature to changelog

- **fork:** [azet/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e77a3ff94e53b37c7e5aa8a9b3c88f59a6247539) by Aaron Zauner
- **size:** 1 files (+1/-0)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Changelog modification specific to another fork's release notes with no functional impact or value.

### `eaa705d76c` ci: pass solution dir to portable project build

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/eaa705d76ca87124d940c045fa176f10cec0cf78) by guvity
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fixes fork-specific CI workflow (passive-rdp-monitor) that does not exist here; our build.ps1/CI pipeline differs entirely.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-1772-files.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `f066add845` Enhance notification message handling and add new settings for RDP gateway access token and start program

- **fork:** [lthobois/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f066add8452cb4221010dffa8a7aa0174277290d) by Loïc THOBOIS
- **size:** 4 files (+277/-223)
- **score 4** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 2 | risk 2 | applies conflict | REJECT
- **why:** Cosmetic notification prefix with 12-hour 'hh' bug, mixed with whitespace churn and fork-specific RDP gateway token settings. Low value against our diverged ConnectionInitiator.

### `f0b1743293` docs: clarify all remaining blocked tasks in MIGRATION_PROGRESS.md

- **fork:** [Morgadoo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f0b1743293736f58374b22abcb0ea6ba68b54cf3) by Claude
- **size:** 1 files (+51/-155)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** MIGRATION_PROGRESS.md tracks another fork's Avalonia migration; neither that file nor those projects exists here, so this documentation has no value.

### `f8db148501` ci: add GitHub Actions workflow for v4 build (Release Portable x64)

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/f8db1485015a13560706a2e423752a2a7a593bb6) by Claude Code
- **size:** 2 files (+54/-1)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies likely | REJECT
- **why:** CI workflow specific to a third-party passive RDP branch; irrelevant to our existing .NET 10 pipeline.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-1772-v4.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `3ed352c27c` Fix SSHDotNetDiagnosticsTests compilation

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/3ed352c27c5b9024811ddb6c2e8f00fed0e32aaf) by Dawie Joubert
- **size:** 2 files (+29/-22)
- **score 3** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 1 | applies rewrite | REJECT
- **why:** Test fix for joubertdj's fork-only SSHDotNet protocol; no SshDotNet code exists in our fork, so tests and InternalsVisibleTo target nothing here.

### `5402fb4007` 增加展开/折叠选中节点的菜单项，支持快捷键

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/5402fb400775e62264abce36399c130fa4767b2a) by Hovn
- **size:** 6 files (+196/-55)
- **score 3** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** Adds expand/collapse selected tree node context menu items. Requires manual rewrite of resource/designer files with low user value.

### `7931524a00` 外部工具增加“启动后等待”的参数，批量执行时可以用此设置执行间隔

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7931524a00bb59babf6468047f117061cac8e4fb) by Hovn
- **size:** 6 files (+260/-123)
- **score 3** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** Adds a 'Wait after start' parameter for external tools. No matching open issue, and requires complete rewrite due to legacy mRemoteV1 path structures.

### `8d20737d70` 语言文件使用带BOM的UTF8，另改用CRLF换行（才可在程序中正常显示换行）

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8d20737d70decd773962e4fd5f8a45ed8a1ae035) by Hovn
- **size:** 3 files (+5/-3)
- **score 3** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 1 | applies conflict | REJECT
- **why:** Obsolete. Modified release channel resources do not exist in our fork, which has simplified update checks and removed channel options.

### `8f69e4cb77` place about form in center of current screen

- **fork:** [stdexception/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8f69e4cb774945580750f99bf0ab6f2fce45f578) by Faryan Rezagholi
- **size:** 1 files (+1/-1)
- **score 3** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 1 | applies rewrite | REJECT
- **why:** Legacy floating-form positioning no longer applies: About is now a docked BaseWindow shown in pnlDock and has no StartPosition.

### `03e163322e` 优化：重命名节点名称时，使用正则表达式提取IP信息并设为主机名

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/03e163322e3c4b6101401436585bcf42ac746a3e) by Hovn
- **size:** 1 files (+10/-1)
- **score 2** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 1 | risk 2 | applies likely | REJECT
- **why:** Faulty logic: comments out the fallback, so renaming to non-IP strings fails to update Hostname entirely. Only matches IPv4, ignoring hostname strings.

### `2fbad345c7` 如果容器节点的【自定义信息】中包含ExecTarget=self(不区分大小写)，则在外部工具将在自身执行，而非所有子节点

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2fbad345c70a4ea1216ab6788a6f74facabd65c1) by Hovn
- **size:** 1 files (+8/-0)
- **score 2** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 2 | risk 3 | applies rewrite | REJECT
- **why:** Magic-string UserField hack (ExecTarget=self) on old mRemoteV1 path; NRE if UserField null; niche behavior better done as a real property if ever requested.

### `4c236d567c` 修复：Icons应仅使用顶层目录的ico图标

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4c236d567c9e042d5d70e527a36e8c3b0c9d3e53) by Hovn
- **size:** 1 files (+1/-1)
- **score 2** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 2 | applies conflict | REJECT
- **why:** Behavior change, not fix: silently drops user icons in subfolders. No matching issue; regression risk for organized icon dirs. Path is old mRemoteV1 layout.

### `4c28bd0a89` test(ssh_dotnet): add Dispose + CreateAdapter unit tests (no fakes)

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4c28bd0a89c1870df29bd5dc01ad1bc2689d4ba8) by Dawie Joubert
- **size:** 2 files (+49/-0)
- **score 2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 1 | applies rewrite | REJECT
- **why:** Tests target fork-specific SSH.NET protocol (ProtocolSshDotNet/SshConnectionManager) absent from our fork; we use PuTTY. Nothing to test here.

### `4d321c7b67` Extract shared InputDialog to eliminate code duplication

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4d321c7b67599fd8ee5defd88ede37abaa417575) by Eran Markus
- **size:** 3 files (+42/-49)
- **score 2** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 3 | risk 1 | applies rewrite | REJECT
- **why:** SftpBrowserPanel and SFTPBrowserWindow do not exist in our fork; we do not have an embedded SFTP browser.

### `52b87c4a60` 快速连接地址框的字体适当缩小，以便控件在缩放场景下能够完整展示。

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/52b87c4a60b803e03f85659369b174e3c54cd06e) by Hovn
- **size:** 1 files (+7/-4)
- **score 2** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 2 | applies likely | REJECT
- **why:** Hardcodes font to 7.5pt Segoe UI to fix combobox heights; our fork uses dynamic high-DPI scaling (_display.ScaleWidth).

### `5bf814447e` Add comprehensive codebase review with improvement recommendations

- **fork:** [MyLabs-LLC/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/5bf814447e401f9a381bcc0d66f4bc896d52051d) by Cursor Agent
- **size:** 1 files (+601/-0)
- **score 2** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 2 | applies likely | REJECT
- **why:** A stale, AI-style v1.78.2 review should not become repository documentation; re-audit any surviving security concerns against current main instead.

### `715d3401a8` frmTaskDialog界面参数微调

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/715d3401a88c1e2fe0785dc0ea3d02ef332ac279) by Hovn
- **size:** 1 files (+2/-2)
- **score 2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 2 | applies likely | REJECT
- **why:** Current code lacks this tweak, while issue #55 is already fixed by measured footer reflow; shrinking unrelated controls has no demonstrated benefit. [source](https://github.com/Hovn/mRemoteNG/commit/715d3401a88c1e2fe0785dc0ea3d02ef332ac279)

### `81b6f28551` feat(ui): enlarge Fullscreen and View Only tab menu items x2

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/81b6f285512edc92d28fc926136e284e368f1c1f) by Claude Code
- **size:** 2 files (+26/-1)
- **score 2** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 1 | risk 2 | applies likely | REJECT
- **why:** Highly specialized UI customization from a niche fork that doubles the font size of specific context menu items, breaking general visual design consistency.

### `b4c4cafc69` 选择面板对话框UI优化

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b4c4cafc697f39f14ee9ef88dcbd19ff660782e0) by Hovn
- **size:** 2 files (+4/-2)
- **score 2** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 1 | risk 2 | applies conflict | REJECT
- **why:** Reverts localization of the New button on the Choose Panel form, leaving it blank. This breaks UI and language support.

### `b4f2405d8d` fix: build only main project to avoid test restore error

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/b4f2405d8d566372b374e886d61b72d953e1b5d6) by guvity
- **size:** 1 files (+13/-6)
- **score 2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 2 | applies rewrite | REJECT
- **why:** Fork-specific CI workflow (passive-rdp-monitor) that doesn't exist here; our CI uses build.ps1/MSBuild full-solution model. Irrelevant.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-1772-direct.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `d33415860a` Enable build on push

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/d33415860ab7244838c42375e5ee223eb4c86bf9) by guvity
- **size:** 1 files (+3/-0)
- **score 2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 2 | applies conflict | REJECT
- **why:** The target workflow and legacy branch do not exist; current main-branch push CI already provides the relevant build coverage.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-1772-build.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `3a249e1b10` 升级选项页面汉化准备，显示和值分开（进行中）

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/3a249e1b106223bf67bdb8d93b1f6f7e7fd9808b) by Hovn
- **size:** 2 files (+8/-0)
- **score 1** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 2 | risk 2 | applies rewrite | REJECT
- **why:** Update-channel localization prep; our fork removed channels entirely (#136, GitHub-only updates). Obsolete, touches deleted code paths.

### `463f3c5ca0` 修复缺失的资源，现可正常编译并打开设置页面

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/463f3c5ca07a58099e5077ae4024dfdc31862d7e) by Hovn
- **size:** 2 files (+27/-0)
- **score 1** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 2 | risk 2 | applies rewrite | REJECT
- **why:** Ancient mRemoteV1 path; CredentialsPage options page with PageIcon resx doesn't exist in our modernized .NET 10 tree. Not applicable.
- **security flags:**
  - `security-code` (high) in `mRemoteV1/UI/Forms/OptionsPages/CredentialsPage.Designer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteV1/UI/Forms/OptionsPages/CredentialsPage.resx` - credential and crypto paths need human review regardless of intent

### `5b7e2ae0fb` Add translations in other languages

- **fork:** [raohj1987/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/5b7e2ae0fb3f5527b5ff2df538ca082be09c7054) by raohj1987
- **size:** 17 files (+116/-39)
- **score 1** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 2 | applies conflict | REJECT
- **why:** Mostly resx whitespace reflow noise; only substantive string is SftpFileManager for fork-specific feature we lack. Guaranteed merge conflicts, no user benefit.

### `836c979e90` 针对RadminConnect外部工具进行特殊集成处理，现在可在mRemoteNG内部窗口显示Radmin了

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/836c979e908c6d57b675ab3090973c0e960d9997) by Hovn
- **size:** 2 files (+48/-14)
- **score 1** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 3 | risk 3 | applies conflict | REJECT
- **why:** A tool-specific hack for RadminConnect that modifies core external tool handling. No open issue exists, and it risks regressing other integrated tools.

### `a0809b0d01` Only require PuTTYNG end anchors to follow their start anchor

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a0809b0d01dbbb247e74c00c12da8e69b2c777f3) by vindict6
- **size:** 1 files (+7/-4)
- **score 1** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 2 | applies rewrite | REJECT
- **why:** Patches Build_PuTTYNG.yml which does not exist in our fork; our PuTTYNG lives in separate repo with own build.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_PuTTYNG.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `aaf816706b` Refactor NotificationPanelMessageWriter to reduce cognitive complexity

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/aaf816706b1e744ac64899daf352f490adaf5ee7) by Eran Markus
- **size:** 1 files (+35/-39)
- **score 1** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 2 | risk 2 | applies conflict | REJECT
- **why:** Cosmetic cognitive-complexity refactor of OUR defensive handle-creation code (fork copied it). No behavior gain; subtly widens exception swallowing. Churn on crash-sensitive path not worth it.

### `ca19ebabdd` 外部工具增加WaitAfterStart字段，启动后的等待时间（阻塞）。修正外部工具尝试集成启动后%name%变量不正确的问题

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ca19ebabdd54cae80ea23f3393aa4bc4d74d8785) by Hovn
- **size:** 3 files (+39/-11)
- **score 1** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 3 | risk 3 | applies rewrite | REJECT
- **why:** Niche WaitAfterStart with blocking Thread.Sleep on UI path, Console.WriteLine debug, mojibake comments, old mRemoteV1 layout. Would need full reimplement for marginal benefit.

### `d311604575` Remove SSH_DotNet specific username/password fields and use generic credentials

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d31160457510532d4738616434392682be89f2eb) by Dawie Joubert
- **size:** 3 files (+1/-3)
- **score 1** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 2 | risk 2 | applies rewrite | REJECT
- **why:** Cleanup for fork-specific SSH_DotNet protocol our fork doesn't have; visible diff is whitespace only. Nothing to import.

### `e188e12904` Config now read tools from the command line config position

- **fork:** [hthvdmeer/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e188e129048b62a027908f8a4bdda754def50522) by takemaker63
- **size:** 5 files (+38/-26)
- **score 1** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 3 | risk 3 | applies rewrite | REJECT
- **why:** Depends on that fork's private --config/--cfg CLI feature; our ProgramRoot has no such arg. Also carries personal launchSettings/AssemblyInfo noise. Loose thematic overlap with #145 portable-path work only.

### `e4c7944632` input.cs UI细节调整

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e4c7944632bfc7e22400599c59739462da6a081e) by Hovn
- **size:** 2 files (+73/-70)
- **score 1** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 2 | applies rewrite | REJECT
- **why:** Cosmetic tweaks: whitespace, hardcoded Segoe UI font, OK/Cancel bounds swap on legacy paths. No bug fixed; our dialog code already diverged.

### `f382cb4210` AdmPwd.E版本更新至7.7.5

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f382cb4210526cc8bb7762af80034724abdfbcbc) by Hovn
- **size:** 4 files (+14/-57)
- **score 1** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 2 | applies rewrite | REJECT
- **why:** AdmPwd.E bump in legacy mRemoteV1 net46 packages.config layout; our .NET 10 fork has no AdmPwd.E dependency.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteV1/mRemoteV1.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteV1/packages.config` - a new or repointed package can pull arbitrary code at restore time

### `006f651ddc` Fix misleading version label

- **fork:** [wolverine2k/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/006f651ddc4f02d36163b53f55197cf696c8632f) by Manuel Thalmann
- **size:** 1 files (+2/-2)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Our UpdateWindow.cs line 76 already shows Language.Version for installed-version label; identical fix present after #136 update-flow rework.

### `0b34726120` Add cross-platform portability analysis report for Linux and macOS

- **fork:** [Morgadoo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/0b34726120a9b20270c490df7f9cb53922edc9d9) by Claude
- **size:** 1 files (+345/-0)
- **score 0** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 3 | risk 2 | applies likely | REJECT
- **our issue:** #137
- **why:** Related to #137, but the report is stale and contradicts current project facts, including WPF removal. Misleading architecture documentation has no durable value.

### `0e37c335f4` add serial connections to factory

- **fork:** [azet/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/0e37c335f4b07bd59b4510cfece97f396754d05d) by Aaron Zauner
- **size:** 1 files (+4/-1)
- **score 0** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 1 | risk 1 | applies likely | REJECT
- **why:** Serial protocol support is already fully implemented in our fork. The proposed commit also contains a syntax typo ('returm').

### `1fba339843` refactor(ssh_dotnet): dispose per-connection CancellationTokenSource (S2930)

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/1fba339843b891009c1cb85702338dfdd4f1d092) by Dawie Joubert
- **size:** 1 files (+6/-0)
- **score 0** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** Targets fork-specific ProtocolSshDotNet.cs; no SshDotNet protocol exists in our fork, so the fix has nothing to apply to.

### `2496cc24b5` removed old project backup files

- **fork:** [changsongyang/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2496cc24b51abab98a50697bc6d6e2506d751394) by Faryan Rezagholi
- **size:** 2 files (+0/-1905)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** No *.csproj.old files exist in our fork; SDK-style migration already removed legacy project backups.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj.old` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNGSpecs/mRemoteNGSpecs.csproj.old` - a new or repointed package can pull arbitrary code at restore time

### `25c8daee4c` removed geckofx nuget

- **fork:** [stdexception/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/25c8daee4ce98178bd0855c793b533f56aebb993) by Faryan Rezagholi
- **size:** 2 files (+0/-9)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Gecko fully removed already (zero Gecko references, no mRemoteV1 project or packages.config); #113 even handles legacy Gecko enum values.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteV1/mRemoteV1.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteV1/packages.config` - a new or repointed package can pull arbitrary code at restore time

### `2c6f20e84e` 修复跳转升级选项页时总是跳转到第一页的问题

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2c6f20e84e95af320c724defdf10165c1f9d553e) by Hovn
- **size:** 1 files (+7/-3)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Already addressed. Our modernized FrmOptions_Load uses SetActivatedPage to select the target page without unconditionally resetting the list view selection to index zero.

### `2d963a43d0` Updated for serena and claude

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2d963a43d070e5e043ba366e3cb9612e57616d62) by Dawie Joubert
- **size:** 1 files (+7/-1)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Already handled. Our customized .gitignore already ignores CLAUDE.md, GEMINI.md, and local Claude settings, while preserving team-shared configuration.

### `303e593421` fix: disambiguate WinForms message filter type

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/303e593421d06f201eedbbb3a0b7b85bb24a0ec9) by guvity
- **size:** 1 files (+1/-1)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Already resolved. RdpProtocol6.cs was modernized and merged into RdpProtocol.cs, which already uses fully qualified System.Windows.Forms.Message to prevent namespace collision.

### `31ddce7885` test(ssh_dotnet): fix Connect tests to construct InterfaceControl (6 failing -> passing)

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/31ddce7885a467910cc0259ed7832b494af5184b) by Dawie Joubert
- **size:** 1 files (+6/-6)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 1 | applies rewrite | REJECT
- **why:** Not applicable. Our fork does not use the SshDotNet protocol or library, so these tests and files do not exist in our codebase.

### `34cf25fa37` Added back System.Configuration.ConfigurationManager nuget

- **fork:** [changsongyang/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/34cf25fa379fe02c63a49041b2dd6f441d20bc65) by Faryan Rezagholi
- **size:** 1 files (+1/-0)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** System.Configuration.ConfigurationManager already referenced at 10.0.5 via Directory.Packages.props; their 4.7.0 add is obsolete for .NET 10 fork.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `37bc079c10` Rename COPYING.TXT to COPYING.txt

- **fork:** [changsongyang/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/37bc079c10e13f2a4fcae9c971374f3f764b3f79) by Faryan Rezagholi
- **size:** 1 files (+0/-0)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Exact case-only rename already exists in efea9f085; current tracked file is COPYING.txt. Reapplying is redundant and conflicts because the destination already exists.
- **security flags:**
  - `license` (medium) in `COPYING.txt` - licence edits change redistribution terms

### `39f0a717bc` removed geckofx from components check

- **fork:** [stdexception/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/39f0a717bc5ee3c6aadc37c12701a588e803bbd6) by Faryan Rezagholi
- **size:** 1 files (+0/-47)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Equivalent Gecko check removal is already in ab9ffb45; Gecko and the entire components-check class are absent, leaving no applicable code.

### `3a009ddb8e` feat(ui): Ctrl+Tab / Ctrl+Shift+Tab to switch session tabs

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/3a009ddb8e81a6fa76f20d6862ac497c935c49ea) by Claude Code
- **size:** 3 files (+53/-2)
- **score 0** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Equivalent tab navigation feature is already implemented in our fork via PR #2941 with NavigateToNextTab/NavigateToPreviousTab.

### `400dfce67c` updated german and french language files

- **fork:** [stdexception/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/400dfce67c2f3a0f956106fecb9f04f4f36bcf42) by Faryan Rezagholi
- **size:** 3 files (+10/-2)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Exact German/French strings already exist under normalized keys; SDK-style resources make LastGenOutput obsolete. The Hungarian issue is unrelated.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteV1/mRemoteV1.csproj` - a new or repointed package can pull arbitrary code at restore time

### `502f42596c` removed obsolete System.web configuration

- **fork:** [changsongyang/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/502f42596cbba2b50938be00b8328bcae73976b5) by Faryan Rezagholi
- **size:** 1 files (+0/-12)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Removes NetFX 4.7.2 System.web providers; our fork is .NET 10, legacy app.config sections already obsolete/gone in modernization.

### `5402b3a892` Document this fork's purpose and changes in README

- **fork:** [k-meeks/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/5402b3a89235fd28d2458bb583985bf6b0df6f90) by Kyle Meeks
- **size:** 1 files (+22/-0)
- **score 0** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Our README already documents this fork more fully; importing another maintainer’s hospital/university context and PuTTY-specific claims would misrepresent our edition.

### `686a23912c` Ignore .resources files and remove Language.resources

- **fork:** [julesbobb/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/686a23912c36d28f472b5084ba08011338a328d7) by Jules Bobb
- **size:** 2 files (+4/-0)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies likely | REJECT
- **why:** Verified: git ls-files shows no tracked .resources files in our fork; stray-file removal already moot, ignore rule adds nothing.

### `6b5c4cfe6e` Fix DockState namespace reference and update build number

- **fork:** [nickbeentjes/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6b5c4cfe6e45f37d9c1fa46a46b83170fe4d8fc2) by Nick Beentjes
- **size:** 2 files (+5/-5)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Cosmetic namespace qualification plus nightly build-number bump on 1.78.2 AssemblyInfo; our .NET 10 fork versions via csproj, no benefit.

### `6cacbb8561` fix(rdp): clear SWP_NOMOVE to defeat mstscax connection bar position lock

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/6cacbb8561cef8be1741f442927a4da9a6a7e233) by Claude Code
- **size:** 1 files (+12/-9)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 2 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** Patches guvity's fork-only ConnectionBarPinner in RdpProtocol6.cs; our fork has neither the class nor that file (verified). No matching issue.

### `732b492533` Fix remaining SonarCloud MAJOR code smells

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/732b4925335a0bd0872a60e9e871de2ff3333b79) by Eran Markus
- **size:** 6 files (+29/-18)
- **score 0** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 1 | applies conflict | REJECT
- **why:** Cleans up SonarCloud smells on files from the xterm/SFTP feature. Since that feature is not in our tree, this commit does not apply.

### `7dbf60c6d8` 调整：查看-新建连接面板菜单可自定义面板名称

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7dbf60c6d8a516e04856c47f5d5c99544d71baf3) by Hovn
- **size:** 1 files (+8/-1)
- **score 0** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Named-panel creation already exists in the chooser, with immediate tab-context renaming elsewhere; this obsolete mRemoteV1 path adds only redundant modal friction.

### `83b66b5d8e` Build passive RDP with VS 2026 runner

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/83b66b5d8e0daa1d1324eb53d4da65b5df871f81) by guvity
- **size:** 1 files (+22/-6)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fork-private workflow file we don't have; our CI already runs windows-2025-vs2026 with MSBuild 18 and our build.ps1 handles VS detection. Nothing to import.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-build.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `8ca850223b` NGTextBox还原更改（修复搜索无响应），增加搜索结果数量提示，部分空值异常修复

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8ca850223b5c5079d47890f0945022590be3d9ad) by Hovn
- **size:** 9 files (+41/-71)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 3 | risk 2 | applies conflict | REJECT
- **why:** Bug with unresponsive search was caused by custom textbox restrictions absent in our fork. Null checks already exist in our code. No need to import.

### `8cf5980b54` refactor(ssh_dotnet): replace reflection pty-resize with public API (S3011 hotspot)

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8cf5980b54389c19162a23091ec9096cac8612e9) by Dawie Joubert
- **size:** 2 files (+16/-42)
- **score 0** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 1 | applies rewrite | REJECT
- **why:** SSH.NET terminal protocol (SSH_DotNet) is not implemented in our fork, which continues to use PuTTY/PuTTYNG for SSH connections.

### `9150b1da4e` Fix ActiveX autoreconnect event signature

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/9150b1da4e0cc50de84b50f99573a805ca865a03) by guvity
- **size:** 1 files (+3/-3)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 5 | risk 1 | applies rewrite | REJECT
- **why:** Not applicable. Our fork does not implement passive RDP monitoring or subscribe to the ActiveX OnAutoReconnecting event.

### `943b6d2eeb` 自动更新设置弹框代码优化

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/943b6d2eebc4f3d370dcde31e72c981551285a50) by Hovn
- **size:** 1 files (+24/-12)
- **score 0** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 3 | risk 2 | applies conflict | REJECT
- **why:** Our fork already modernized settings/updates using Properties.OptionsUpdatesPage.Default and centralized forms. This legacy optimization is obsolete and conflicts with our clean architecture.

### `98c4e7ee4d` Add OpenMultipleConnectionsWithEnter user setting

- **fork:** [julesbobb/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/98c4e7ee4db02ca9a0aa932ad628e03e4354dca8) by Julian Bobbett (DHCW - Software Development)
- **size:** 3 files (+20/-5)
- **score 0** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** Adds only an unused setting; no handler or options UI consumes it, so the claimed feature has no runtime effect and needs an end-to-end redesign.

### `9e05dc8b5a` Remove external resource tags from HTML template to clear S5725

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9e05dc8b5a11e46ca298943eb267550df5840c8c) by Eran Markus
- **size:** 3 files (+18/-37)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** SonarCloud S5725 cleanup for fork-only xterm SSHTerminalBase; our fork has no xterm-based SSH terminal.

### `aa91e45a21` UpdateChannel默认配置值修正

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/aa91e45a21329c22564f495d7ce8e1473e84321d) by Hovn
- **size:** 2 files (+6/-2)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Tweaks UpdateChannel default in legacy mRemoteV1 config. Our fork removed update channels entirely (#136, 8fa29117e/3e9f8a7bc) — GitHub-releases-only. Obsolete.

### `c17a38e28e` Fix last 3 SonarCloud issues

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c17a38e28ee1de0cb190a9e5947fe0a2179adb53) by Eran Markus
- **size:** 2 files (+22/-31)
- **score 0** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** Cleans SonarCloud smells in SSHTerminalBase/SftpFileService — fork-only xterm/SFTP features absent in our tree (verified); we use PuTTY for SSH and hold our own quality gate at A/A/A.

### `c32989deb4` docs: record C1 build blocker (needs .NET Framework MSBuild, not dotnet build)

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/c32989deb44f412d2b77083fbb57898f7007b1ef) by Claude Code
- **size:** 1 files (+11/-2)
- **score 0** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** MSB4803 and full-MSBuild guidance already exists in CLAUDE.md, build.ps1, architecture and troubleshooting docs; HANDOFF.md is absent.

### `d78f3317ec` Allow item name & handle revoked tokens + logging

- **fork:** [Zarlengo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d78f3317ec089aae42c2bf6c320d7ceba7ae5c0a) by Chris Zarlengo
- **size:** 7 files (+178/-39)
- **score 0** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** Patches Zarlengo-only Bitwarden connector (ExternalConnectors/BW absent in our fork); also removes UUID validation and adds noisy notification spam.

### `da48bf817e` 去除一些调试日志输出

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/da48bf817e021a3fb9aa42e446b45c4fce76b27e) by Hovn
- **size:** 2 files (+2/-2)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Raw writes and MakeRelativeIfPossible are gone; current options diagnostics use structured logging. This legacy-path patch has no remaining target.

### `dde69a2821` test(ssh_dotnet): consolidate duplicate test files; add [Category] taxonomy

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/dde69a28219e0817e2eb0d19aa8bff7512a8bf56) by Dawie Joubert
- **size:** 9 files (+6/-771)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** Test consolidation for SSH_DotNet suite that only exists in that fork; no corresponding code or tests in ours.

### `dfdb5fee63` add 'Serial' to Lang

- **fork:** [azet/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/dfdb5fee637e4a6007b0c96b31d60bc1a80afd23) by Aaron Zauner
- **size:** 1 files (+4/-1)
- **score 0** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Modern Language.Serial already exists as “Serial (via PuTTY)” and the Serial protocol is implemented; the commit targets removed mRemoteV1 resources.

### `e3eb690eb6` docs: close B3 (performance flags semantics verified, fixed via A5)

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/e3eb690eb653368366a6b532e751b48372112598) by Claude Code
- **size:** 1 files (+5/-2)
- **score 0** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 5 | risk 1 | applies rewrite | REJECT
- **why:** This updates HANDOFF.md, which is a file specific to the guvity fork's passive RDP development progress tracking and does not exist here.

### `ea0a956dfb` Fix OnPaint null font exception in SshTerminalControl

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ea0a956dfb38fd1cf8b7ff46b1ebc0523d8e9ed0) by Dawie Joubert
- **size:** 1 files (+5/-3)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** SshTerminalControl is fork-only and absent here; our SSH paths use PuTTY/OpenSSH. The null-font paint fix has no applicable code path or mapped issue.

### `edd2421291` Fix critical bug: DataConsumer never initialized due to constructor setting _isInitialized

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/edd2421291eec6647d962d060ac00b0c68aecc4d) by Dawie Joubert
- **size:** 1 files (+4/-25)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** SshTerminalControl.cs is a fork-only VtNetCore SSH terminal; our fork has no such file and uses PuTTY for SSH.

### `f0c571b5cf` Renci.SshNet版本升级至2020.0.2

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f0c571b5cf3c1d7b74b94b78c755be0d56f5c464) by Hovn
- **size:** 2 files (+3/-3)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Our fork already uses a much newer SSH.NET version (2025.1.0) under central package management, fully superseding this change.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteV1/mRemoteV1.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteV1/packages.config` - a new or repointed package can pull arbitrary code at restore time

### `f22a2a12ba` Fix theme palette labels showing Japanese for non-Japanese UI cultures

- **fork:** [k-meeks/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f22a2a12ba9d9a2887f85f700e45d333b2a76aa0) by Kyle Meeks
- **size:** 2 files (+12/-4)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 5 | risk 1 | applies rewrite | REJECT
- **why:** The Japanese theme translation map and display method do not exist in our fork, so the leak bug cannot occur.

### `ff3ed60e88` Disable auto-update + fix Claude API key setup

- **fork:** [nickbeentjes/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ff3ed60e8807fb80b95c7227ee1cf59862127484) by Kees
- **size:** 2 files (+34/-3)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 3 | applies rewrite | REJECT
- **why:** Fork-private hacks: hard-disables update check (ours is GitHub-only by design) and stores Claude API key plaintext in AppData for a ClaudeChatWindow we don't have.

### `3ca395ed8b` refactor(ssh_dotnet): narrow fatal-path Connect catches to ArgumentException (S2221)

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/3ca395ed8b6d02d6fd0b80b3a91685f3e5bfa21d) by Dawie Joubert
- **size:** 1 files (+2/-2)
- **score -1** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 4 | risk 2 | applies rewrite | REJECT
- **why:** SonarCloud catch-narrowing in SSH_DotNet files absent from our fork; nothing to apply.

### `3e43d6855d` Fix passive RDP portable build workflow

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/3e43d6855db0d441befaa429ff56c37a687e0a74) by guvity
- **size:** 1 files (+9/-11)
- **score -1** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 3 | applies rewrite | REJECT
- **why:** Fixes that fork's own passive-rdp workflow; file doesn't exist here. Also downgrades action versions and our CI model differs entirely.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-build.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `55a5d1a8af` increased width of about screen

- **fork:** [stdexception/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/55a5d1a8af2f4b3386281b22471f26803f57f134) by Faryan Rezagholi
- **size:** 2 files (+10/-8)
- **score -1** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 1 | applies rewrite | REJECT
- **why:** Old mRemoteV1 path; our About dialog already redesigned/rebranded (Geseidl Maintained-by). Cosmetic width tweak obsolete.

### `55aed5eb70` CustomConsPath配置项默认会保存相对路径（如可用）

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/55aed5eb7088aa25252181bad5fda41fb70c121a) by Hovn
- **size:** 4 files (+81/-16)
- **score -1** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 4 | risk 2 | applies rewrite | REJECT
- **why:** Our fork refactored paths to use ConnectionFilePath with Environment.ExpandEnvironmentVariables. Handled better without custom Uri/relative-path parsing.

### `641d7e8d1e` test(ssh_dotnet): key-auth matrix with runtime-generated keys (Phase 8)

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/641d7e8d1e40fca96bf332e399fa4c2a9a560252) by Dawie Joubert
- **size:** 2 files (+152/-0)
- **score -1** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 4 | risk 2 | applies rewrite | REJECT
- **why:** Tests for SshAuthenticationProvider/SshDotNet classes that do not exist in our fork; would also add BouncyCastle test dependency.

### `932bc12830` refactor(ssh_dotnet): split PortForwardRuleParser parse/apply + unit tests

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/932bc12830ba4bcd078aa175d55fa588c08f1430) by Dawie Joubert
- **size:** 2 files (+194/-26)
- **score -1** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 4 | risk 2 | applies rewrite | REJECT
- **why:** Refactors fork-specific SshDotNet PortForwardRuleParser; our fork has no SSH.NET protocol (SSH via PuTTY). Nothing to apply.

### `9c9fda8ca5` chore: resolve nullable warnings in mRemoteNGSpecs

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9c9fda8ca5437cbea18c2868f5d0f7b045826027) by Eran Markus
- **size:** 7 files (+7/-7)
- **score -1** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 1 | applies conflict | REJECT
- **why:** Five touched test files do not exist; global CS8618 suppression already covers the two surviving field warnings. This adds no runtime or test behavior.
- **security flags:**
  - `security-code` (high) in `mRemoteNGSpecs/StepDefinitions/CredentialRepositoryListSteps.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGSpecs/StepDefinitions/CredentialRepositorySteps.cs` - credential and crypto paths need human review regardless of intent

### `fe61c73e92` feat(rdp): move RDP connection bar to top-right in fullscreen

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/fe61c73e92c2abe48c7158a94f5b1b2e1aa2566a) by Claude Code
- **size:** 2 files (+163/-2)
- **score -1** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 3 | risk 4 | applies conflict | REJECT
- **why:** Niche cosmetic hack: timer polls for heuristic 'OPWindowClass' window, SetWindowPos moves connection bar. Author admits class unverified. No matching issue; fragile.

### `019d591e7b` Refactored the ExpandCollapseAnimationTimer_Tick to reduce cognitive load.

- **fork:** [julesbobb/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/019d591e7bdf914ff58622379a8f068eb97d4570) by Julian Bobbett (DHCW - Software Development)
- **size:** 1 files (+42/-25)
- **score -2** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 2 | applies conflict | REJECT
- **why:** Pure extraction around an animation subsystem our tree lacks. The target method and state fields are absent, yielding no user-visible benefit and requiring prerequisite feature work.

### `3dc00ad1ae` Add options

- **fork:** [Zarlengo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/3dc00ad1aedd73bd1002e70b02dd5c947a6d29ca) by Chris Zarlengo
- **size:** 14 files (+877/-157)
- **score -2** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 4 | risk 4 | applies conflict | REJECT
- **why:** Fork-personal Bitwarden connector rework; removes SSO/password-file paths (regression for those users). Large Designer churn, no tracked issue, credential-flow risk.

### `6231d4a39e` refactor(ssh_dotnet): rename SSHDotNetPortForwardRules property to PascalCase

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6231d4a39e2f1b0dd48df786af1bc2156adb35db) by Dawie Joubert
- **size:** 7 files (+24/-24)
- **score -2** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 3 | risk 3 | applies rewrite | REJECT
- **why:** Renames property of fork-only SshDotNet protocol absent from our fork; also breaks CSV header compat. Nothing to apply.

### `7dd9011eec` Enhance Prot_Event_Closed with exception handling

- **fork:** [Ahmed-ElHamidy/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7dd9011eec0bb331b14d2f4d5ba4b1535247d665) by Ahmed Omar ElHamidy
- **size:** 1 files (+13/-6)
- **score -2** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 2 | applies conflict | REJECT
- **our issue:** #142
- **why:** Blanket catch that swallows close errors. Our fork already guards this path (disposed checks, deferred QueueCloseTab NRE-guarded, 6c788ae45). Masking exceptions is a regression.

### `9f03e57d3b` Use MSBuild for passive RDP portable publish

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/9f03e57d3b91f45a2976d3f43a1030a828d3ee5e) by guvity
- **size:** 1 files (+5/-2)
- **score -2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 2 | applies conflict | REJECT
- **why:** Switches their custom passive-rdp workflow to MSBuild to dodge MSB4803 COM-ref failure; our build.ps1 and CI already use full MSBuild. Workflow file not in our repo.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-build.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `a71cb57380` input.cs UI细节调整2

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a71cb573806a810ae689bb8b8129ed09e0d15c60) by Hovn
- **size:** 1 files (+2/-2)
- **score -2** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 2 | applies conflict | REJECT
- **why:** Cosmetic adjustments to programmatic legacy form `input.cs` from `mRemoteV1`, which was replaced in our fork by designer-based `FrmInputBox`.

### `bbccbb2a70` Refactor Enter key multi-connection open logic to reduce cognitive load.

- **fork:** [julesbobb/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bbccbb2a701beae624f50f63acf7310cf5770af6) by Jules Bobb
- **size:** 1 files (+56/-33)
- **score -2** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 3 | risk 3 | applies conflict | REJECT
- **why:** Refactors the OpenMultipleConnectionsWithEnter feature, which is not implemented in our fork, causing compilation failures.

### `d2afacd026` feat: passive RDP monitor for v1.77.2-release

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/d2afacd026ba1e5284bded7b304f835b17237792) by guvity
- **size:** 1 files (+37/-0)
- **score -2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 4 | applies conflict | REJECT
- **why:** Fork-private CI workflow for their passive-rdp branch on old 1.77.2/.NET 6 stack. Irrelevant to our 2-release CI model; ci-workflow security flag.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-1772-direct.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `d762ef4de2` log4net版本更新至2.0.17

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d762ef4de265f09ebba6c1ad0f4e4d5f66540524) by Hovn
- **size:** 5 files (+11/-8)
- **score -2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 2 | applies conflict | REJECT
- **why:** Our fork has already modernized to log4net 3.3.2 via central package management and does not use the legacy mRemoteV1 codebase.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNGTests/mRemoteNGTests.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNGTests/packages.config` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteV1/mRemoteV1.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteV1/packages.config` - a new or repointed package can pull arbitrary code at restore time

### `d7e3f2e339` Fix all remaining SonarCloud issues (24 → 0 target)

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d7e3f2e339a84aed843a50b8ef198e3e0400338e) by Eran Markus
- **size:** 3 files (+57/-68)
- **score -2** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 2 | applies rewrite | REJECT
- **why:** This commit fixes SonarCloud issues in WebView2/xterm.js SSH terminal and SFTP browser code, which are custom to this fork and absent from our codebase.

### `e2ae6d139f` 优化批量启动外部工具时从externalTool.WaitAfterStart获取等待间隔

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e2ae6d139f3d788560c42705256ac91b17adc8ea) by Hovn
- **size:** 2 files (+12/-12)
- **score -2** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 3 | risk 3 | applies rewrite | REJECT
- **why:** Tweaks fork-private StartExternalApp_CBH batch-launch methods absent from our code; also comments out WaitAfterStart handling; mojibake comments.

### `fa69de0c07` Remove CLAUDE.md from feature branch

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/fa69de0c077cdd92ed73f7443d94dd4e085f9245) by Dawie Joubert
- **size:** 1 files (+0/-312)
- **score -2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 4 | applies conflict | REJECT
- **why:** Our distinct CLAUDE.md is the canonical build, test, and agent guide. Deleting it is fork-local housekeeping that would damage our workflow.

### `fc076999ca` added missing usings

- **fork:** [changsongyang/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/fc076999ca59f4253638bd172b49e763e1201618) by Faryan Rezagholi
- **size:** 3 files (+5/-1)
- **score -2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 2 | applies conflict | REJECT
- **why:** Both imports already exist, and central package management pins Protobuf 3.34.0; applying this obsolete patch adds nothing and risks dependency-version regression.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNGSpecs/mRemoteNGSpecs.csproj` - a new or repointed package can pull arbitrary code at restore time

### `3bb3a8532b` Fix terminal output rendering by removing unreliable DataAvailable check

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/3bb3a8532b323b7d6013caf20fc9ea3e8a3bd8b3) by Dawie Joubert
- **size:** 1 files (+45/-51)
- **score -3** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** Fixes SSH_DotNet protocol which exists only in joubertdj fork (upstream PR #2997); our fork has no SSH_DotNet code.

### `48bd7e6cf2` feat(ssh_dotnet): add private-key file/passphrase connection properties (XML+CSV)

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/48bd7e6cf264a8cd6f2fdf26ef771cff0a19a01a) by Dawie Joubert
- **size:** 9 files (+196/-2)
- **score -3** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** Serialization for SshDotNet private-key properties; entire SshDotNet protocol absent from our fork (we use PuTTY for SSH). No target for these fields.

### `48f09465be` Implement bidirectional SSH input/output flow to fix blank terminal

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/48f09465be39a72a0100ff5d16ec6b0e4376f90c) by Dawie Joubert
- **size:** 2 files (+97/-3)
- **score -3** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** Fixes blank terminal in fork-only SSH.NET/VtNetCore terminal (ProtocolSSH_DotNet, SshTerminalControl); neither class exists in our fork.

### `51eadbb85e` 多选删除提示语（暂未使用）

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/51eadbb85eb92b7944a2afa60749f45c7ba458c7) by Hovn
- **size:** 1 files (+15/-0)
- **score -3** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 2 | risk 2 | applies rewrite | REJECT
- **why:** Multi-selection deletion already works through GetSelectedNodes; this explicitly unused bulk-confirmation overload has no caller, tests, or current-path compatibility.

### `567f108484` Run the unit tests in CI

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/567f108484aaf48c62cc291f874521f4a79a007e) by vindict6
- **size:** 1 files (+31/-0)
- **score -3** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 2 | applies conflict | REJECT
- **why:** Our CI already runs full test suite (run-tests.ps1, nightly trx collection); vstest.console approach inferior to our headless runner. Nothing to gain.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `57f6569a1b` updated project information (license, icon)

- **fork:** [changsongyang/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/57f6569a1bce59cb4172ee9c6bd1e0dac6fc6f8a) by Faryan Rezagholi
- **size:** 1 files (+92/-243)
- **score -3** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 2 | applies rewrite | REJECT
- **why:** csproj SubType cleanup on old project format; ours is .NET 10 SDK-style, fully restructured. No longer applies.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `84212eb05a` added download handler

- **fork:** [stdexception/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/84212eb05a69cde844e37a909f3b8fa776cf042b) by Faryan Rezagholi
- **size:** 3 files (+1/-80)
- **score -3** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** Targets ancient mRemoteV1 layout with CefSharp/Gecko HTTP stack; our .NET 10 fork removed Gecko entirely. Paths and browser stack don't exist here.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteV1/mRemoteV1.csproj` - a new or repointed package can pull arbitrary code at restore time

### `bf757e5fb2` feat(ssh_dotnet): add file-picker ("...") editor for the private key file property

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bf757e5fb241e1ae7c3fe927f1995c14ac08b5fb) by Dawie Joubert
- **size:** 2 files (+58/-0)
- **score -3** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 2 | risk 2 | applies rewrite | REJECT
- **why:** Current SSH protocols already use PrivateKeyPath plus an OpenFileDialog editor; the candidate targets absent SshDotNet-specific properties.

### `c5606cccf5` Hide username/password

- **fork:** [Zarlengo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c5606cccf5d8b618df63b2b4e7e32bdf84584462) by Chris Zarlengo
- **size:** 4 files (+27/-31)
- **score -3** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** Incremental tweak to Zarlengo's own Bitwarden connector (ExternalConnectors/BW, VaultOpenbao fields, NotificationBridge) — feature absent from our fork; nothing to patch.

### `e6dcefe253` Inline CSS/JS resources to eliminate S5725 security hotspots

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e6dcefe253fd3fc05a9a191db3cd59ad0170b491) by Eran Markus
- **size:** 3 files (+47/-43)
- **score -3** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** SonarCloud S5725 appeasement inside eran132's WebView2/xterm SSH terminal — files absent from our fork. Nothing to import without adopting whole feature.

### `eb4787f9e4` Add additional unlock methods

- **fork:** [Zarlengo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/eb4787f9e4bf591713f4341c6fa21dece2b024d3) by Chris Zarlengo
- **size:** 7 files (+319/-190)
- **score -3** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 5 | risk 4 | applies rewrite | REJECT
- **why:** Extends Bitwarden connector (ExternalConnectors/BW) that exists only in Zarlengo fork; our fork has no BW connector, so nothing to patch. Full-feature import out of scope.

### `0707b71af5` Add passive RDP monitor mode

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/0707b71af5eddbb73d7abf6fbafd26ea6daafaa1) by guvity
- **size:** 5 files (+896/-8)
- **score -4** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 4 | risk 5 | applies rewrite | REJECT
- **why:** Niche passive-monitor fork: input blocker, focus-suppression timers, custom CI workflow. Conflicts with our #118/#143 focus fixes and ViewOnly semantics; unrequested feature.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-build.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `098ae74bf3` Citrix资源更新

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/098ae74bf3b504e09e7dcc2bc193cd5d788ad383) by Hovn
- **size:** 2 files (+0/-0)
- **score -4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 5 | applies rewrite | REJECT
- **why:** Adds Citrix Receiver .exe binaries. Citrix support removed upstream (PR #1763, in our history). Untrusted binaries, zero value.
- **security flags:**
  - `binary-artifact` (critical) in `mRemoteV1/Resources/CitrixReceiver_v4.10.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Resources/CitrixReceiver_v4.12.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)

### `0a5bc7bbd5` Add SSH_DotNet protocol to Username field visibility

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/0a5bc7bbd51d0e755bfdbf716cec2c2b3bc59997) by Dawie Joubert
- **size:** 1 files (+1/-1)
- **score -4** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 3 | applies conflict | REJECT
- **why:** SSH_DotNet no longer exists; OpenSSH is its current equivalent and already exposes Username. Importing the obsolete enum reference would conflict and add no behavior.

### `0bff034d49` added button for import from rdm (#887)

- **fork:** [VantIer/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/0bff034d49b5021c6650200112a6027e39e60076) by Faryan Rezagholi
- **size:** 4 files (+37/-4)
- **score -4** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 1 | risk 3 | applies conflict | REJECT
- **why:** Dedicated RDM import UI and parsing already exist. This older patch uses the generic handler and duplicates a resource key, regressing the current integration.

### `237583d0d7` delete original code

- **fork:** [appcompat-wx/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/237583d0d7ffaa40658899259bf3b49731ee6225) by appcompat-wx
- **size:** 300 files (+0/-200381)
- **score -4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 5 | applies conflict | REJECT
- **why:** Destructive commit that deletes 300 files and the entire codebase.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `ci-workflow` (critical) in `.github/workflows/add_PR_2_chlog.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `ci-workflow` (critical) in `.github/workflows/filter-links.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `ci-workflow` (critical) in `.github/workflows/post_2_Reddit.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `license` (medium) in `COPYING.txt` - licence edits change redistribution terms
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `ExternalConnectors/CPS/PasswordstateInterface.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `ExternalConnectors/ExternalConnectors.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `ExternalConnectors/OP/OnePasswordCli.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `ObjectListView/ObjectListView.NetCore.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `build-script` (high) in `Tools/CreateBulkConnections_ConfCons2_6.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/create_upg_chk_files.ps1` - scripts execute on a maintainer machine
  - `security-code` (high) in `Tools/decrypt.bat` - credential and crypto paths need human review regardless of intent
  - `build-script` (high) in `Tools/decrypt.bat` - scripts execute on a maintainer machine
  - `security-code` (high) in `Tools/encrypt.bat` - credential and crypto paths need human review regardless of intent
  - `build-script` (high) in `Tools/encrypt.bat` - scripts execute on a maintainer machine
  - `binary-artifact` (critical) in `Tools/exes/dumpbin.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/editbin.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/link.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/mspdbcore.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/sigcheck.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `build-script` (high) in `Tools/find_vstool.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/github_functions.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/postbuild.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/postbuild_installer.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/postbuild_portable.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/publish_draft_github_release.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/publish_to_github.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/rename_and_copy_installer.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/set_LargeAddressAware.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/sign_binaries.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/signfiles.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/tidy_files_for_release.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/update_and_upload_assemblyinfocs.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/update_and_upload_website_release_json_file.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/validate_microsoft_tool.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/verify_LargeAddressAware.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/verify_binary_signatures.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/zip_files.ps1` - scripts execute on a maintainer machine
  - `security-code` (high) in `mRemoteNG/App/Info/CredentialsFileInfo.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/CredentialHarvester.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/CredentialRecordLoader.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/CredentialRecordSaver.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/CredentialRepositoryListLoader.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/CredentialRepositoryListSaver.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsDocumentEncryptor.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialProviderSerializer/CredentialRepositoryListDeserializer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialProviderSerializer/CredentialRepositoryListSerializer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialPasswordDecryptorDecorator.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialPasswordEncryptorDecorator.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialRecordDeserializer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialRecordSerializer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/XmlConnectionsDecryptor.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryCredentialsPage.cs` - credential and crypto paths need human review regardless of intent

### `268520fbb4` refactor(ssh_dotnet): use monotonic clock for elapsed-time (S6561)

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/268520fbb41afed4e087b2bd4fa1de023d30f97f) by Dawie Joubert
- **size:** 1 files (+20/-18)
- **score -4** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 3 | applies rewrite | REJECT
- **why:** Target SSH_DotNet protocol does not exist in our fork; its elapsed-time refactor has no applicable code path or mapped issue.

### `4b128e99fd` Enable animated expand/collapse for connection tree

- **fork:** [julesbobb/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4b128e99fd4221c8cd545b85c93f9d81d8b01fab) by Julian Bobbett (DHCW - Software Development)
- **size:** 8 files (+206/-6)
- **score -4** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 4 | risk 5 | applies conflict | REJECT
- **why:** Cosmetic, untested, default-on animation performs repeated whole-tree work every 15 ms, conflicts with password expansion and flicker guards, and risks severe UI stalls.

### `518c7b24e2` Use robust v3 passive RDP patcher

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/518c7b24e28488aab9f9ef0aa22ce27de5208b2a) by guvity
- **size:** 2 files (+86/-15)
- **score -4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 3 | applies rewrite | REJECT
- **why:** Fork-private CI/scripts for a custom 'passive RDP' fork; we don't support or have this specialized monitor feature.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-1772-build.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `build-script` (high) in `Tools/passive-rdp-monitor-1772-v3.ps1` - scripts execute on a maintainer machine

### `6d677a494a` removed WndProc override

- **fork:** [VantIer/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6d677a494a6a017df9185f8121dd08321fa31fa4) by Faryan Rezagholi
- **size:** 1 files (+0/-108)
- **score -4** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 1 | risk 5 | applies conflict | REJECT
- **why:** Deletes essential WndProc logic in frmMain handling OS activation, clipboard chain, and focus restoration, which would cause severe windowing and focus regressions.

### `6f0e93bff8` Fix passive RDP patcher for 1.77.2 release

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/6f0e93bff88b941ebeaf42a777ae17ac469149b0) by guvity
- **size:** 1 files (+18/-3)
- **score -4** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 5 | risk 3 | applies rewrite | REJECT
- **why:** Targets an absent patcher for obsolete 1.77.2 source anchors; current architecture has neither a landing point nor supported need. [source](https://github.com/guvity/mRemoteNG-passive-rdp/commit/6f0e93bff88b941ebeaf42a777ae17ac469149b0)
- **security flags:**
  - `build-script` (high) in `Tools/passive-rdp-monitor-1772.ps1` - scripts execute on a maintainer machine

### `8ce136456b` delete original code

- **fork:** [appcompat-wx/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8ce136456bd71189fa9b16c43cf7664ca3a05723) by appcompat-wx
- **size:** 300 files (+0/-200381)
- **score -4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 5 | applies conflict | REJECT
- **why:** This is a destructive commit that deletes the entire repository codebase. Extremely high risk, no value.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `ci-workflow` (critical) in `.github/workflows/add_PR_2_chlog.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `ci-workflow` (critical) in `.github/workflows/filter-links.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `ci-workflow` (critical) in `.github/workflows/post_2_Reddit.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `license` (medium) in `COPYING.txt` - licence edits change redistribution terms
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `ExternalConnectors/CPS/PasswordstateInterface.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `ExternalConnectors/ExternalConnectors.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `ExternalConnectors/OP/OnePasswordCli.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `ObjectListView/ObjectListView.NetCore.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `build-script` (high) in `Tools/CreateBulkConnections_ConfCons2_6.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/create_upg_chk_files.ps1` - scripts execute on a maintainer machine
  - `security-code` (high) in `Tools/decrypt.bat` - credential and crypto paths need human review regardless of intent
  - `build-script` (high) in `Tools/decrypt.bat` - scripts execute on a maintainer machine
  - `security-code` (high) in `Tools/encrypt.bat` - credential and crypto paths need human review regardless of intent
  - `build-script` (high) in `Tools/encrypt.bat` - scripts execute on a maintainer machine
  - `binary-artifact` (critical) in `Tools/exes/dumpbin.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/editbin.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/link.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/mspdbcore.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/sigcheck.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `build-script` (high) in `Tools/find_vstool.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/github_functions.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/postbuild.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/postbuild_installer.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/postbuild_portable.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/publish_draft_github_release.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/publish_to_github.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/rename_and_copy_installer.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/set_LargeAddressAware.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/sign_binaries.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/signfiles.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/tidy_files_for_release.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/update_and_upload_assemblyinfocs.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/update_and_upload_website_release_json_file.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/validate_microsoft_tool.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/verify_LargeAddressAware.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/verify_binary_signatures.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/zip_files.ps1` - scripts execute on a maintainer machine
  - `security-code` (high) in `mRemoteNG/App/Info/CredentialsFileInfo.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/CredentialHarvester.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/CredentialRecordLoader.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/CredentialRecordSaver.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/CredentialRepositoryListLoader.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/CredentialRepositoryListSaver.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsDocumentEncryptor.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialProviderSerializer/CredentialRepositoryListDeserializer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialProviderSerializer/CredentialRepositoryListSerializer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialPasswordDecryptorDecorator.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialPasswordEncryptorDecorator.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialRecordDeserializer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialRecordSerializer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/XmlConnectionsDecryptor.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryCredentialsPage.cs` - credential and crypto paths need human review regardless of intent

### `9c3be90499` refactor(ssh_dotnet): rename SSH_DotNet classes to PascalCase (S101)

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9c3be904993fe3855cf8aa542d17298ab770fa9b) by Dawie Joubert
- **size:** 16 files (+394/-394)
- **score -4** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 3 | applies conflict | REJECT
- **why:** The SSH_DotNet protocol is an unmerged, obsolete experimental feature that does not exist in our fork; we use OpenSSH instead.

### `a65d960174` ci: pass solution dir to portable project build

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/a65d960174eab918844e9594b96dc75ff5bc73c0) by guvity
- **size:** 1 files (+1/-1)
- **score -4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 3 | applies conflict | REJECT
- **why:** Modifies a custom GitHub workflow file (`passive-rdp-monitor-1772-files.yml`) that is completely absent from our fork.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-1772-files.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `b2507ffcb6` refactor(ssh_dotnet): introduce ISshClientAdapter seam for testability

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b2507ffcb6eb6a841d1cc4b62362bf9530955ab2) by Dawie Joubert
- **size:** 3 files (+91/-8)
- **score -4** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 3 | applies rewrite | REJECT
- **why:** [Source](https://github.com/joubertdj/mRemoteNG/commit/b2507ffcb6eb6a841d1cc4b62362bf9530955ab2): Refactors an absent SSH.NET terminal stack; our SSH.NET usage is file transfer, leaving this untested adapter without a consumer.

### `bbb137f5dd` add serial locale

- **fork:** [azet/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bbb137f5ddce916359b3587fd696501e3a7feec4) by Aaron Zauner
- **size:** 1 files (+4/-1)
- **score -4** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 1 | risk 3 | applies conflict | REJECT
- **why:** Serial localization and support already exist at Serial=9; importing legacy value 11 would collide with current ARD serialization.

### `e06fa85b69` Add passive RDP monitor build for mRemoteNG 1.77.2

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/e06fa85b6915fbef42656440d71095215b0cfc0e) by guvity
- **size:** 3 files (+962/-0)
- **score -4** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 4 | risk 5 | applies rewrite | REJECT
- **why:** Niche passive/view-only RDP monitor delivered as patch script + CI workflow against 1.77.2; incompatible with our .NET 10 codebase, security-flagged CI.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-1772-build.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `build-script` (high) in `Tools/passive-rdp-monitor-1772.ps1` - scripts execute on a maintainer machine

### `e32fc97631` Harden portable settings persistence for cross-platform paths

- **fork:** [Morgadoo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e32fc976314d52f0e97bb821cc659f854e169fc7) by Luís Morgado
- **size:** 11 files (+151/-23)
- **score -4** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 3 | applies rewrite | REJECT
- **why:** Part of a cross-platform refactoring splitting the codebase into mRemoteNG.Core and Avalonia, which is not applicable to our Windows .NET 10 project.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG.Avalonia/mRemoteNG.Avalonia.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNG.Core/mRemoteNG.Core.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `e60338b900` refactor(ssh_dotnet): decompose Connect() into focused helpers (S3776)

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e60338b9008291dc3f02c3c4dd2cb934e02f53a5) by Dawie Joubert
- **size:** 1 files (+236/-211)
- **score -4** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 3 | applies rewrite | REJECT
- **why:** Refactors ProtocolSshDotNet.cs, a fork-specific SSH.NET protocol our fork does not have. Behavior-neutral S3776 decomposition of foreign code.

### `f5858ce19b` Add AI-powered security scanner with multi-LLM support

- **fork:** [MyLabs-LLC/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f5858ce19b7c288e7d124a9ee8151ff89f18157f) by Cursor Agent
- **size:** 4 files (+959/-1)
- **score -4** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 3 | risk 4 | applies likely | REJECT
- **why:** Out-of-scope security scanner that sends sensitive system info to external LLM APIs, presenting privacy risks.
- **security flags:**
  - `process-exec` (critical) in `mRemoteNG/Tools/SecurityScanner/SystemInfoCollector.cs` - added code spawns a process or evaluates a string as code

### `172eda4acf` 搜索结果数量提示优化

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/172eda4acfce3e5f09eb99f7c52969c34f933e60) by Hovn
- **size:** 1 files (+9/-5)
- **score -5** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 4 | risk 4 | applies rewrite | REJECT
- **why:** Old mRemoteV1 path, garbled GBK comments, depends on fork-private NodeSearcher API (GetItemMatchPositionDesc). Our search UI heavily diverged (#143/#144 work).

### `29e00a1a13` Finalize passive RDP monitor v2 workflow and menu sync

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/29e00a1a139794131ca2c34df3c2b4f0db2a8aa4) by guvity
- **size:** 2 files (+15/-10)
- **score -5** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 2 | risk 3 | applies conflict | REJECT
- **why:** Menu state already refreshes from live protocol whenever opened; remaining click-time assignments are transient. The .NET 6 passive-branch workflow is obsolete.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-1772-v2.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `2ab6305e6f` fix(rdp): rebind input blocker on auto-reconnect

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/2ab6305e6f46d55661fc6a655bbea0047c5e180d) by Claude Code
- **size:** 3 files (+37/-1)
- **score -5** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 4 | risk 4 | applies rewrite | REJECT
- **why:** Fix for fork-specific PassiveRdpInputBlocker/view-only infrastructure we don't have. No corresponding subsystem in our fork; not applicable.

### `51ff32883d` UI is working

- **fork:** [Morgadoo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/51ff32883d6c9092e69a2f03163298af931cede1) by Luís Morgado
- **size:** 81 files (+3037/-72)
- **score -5** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 5 | risk 5 | applies rewrite | REJECT
- **our issue:** #137
- **why:** 81-file Avalonia cross-platform experiment with security flags and foreign .claude settings; #137 macOS already wontfix. Unimportable, high risk.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `mRemoteNG.Avalonia/ViewModels/CredentialManagerViewModel.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNG.Avalonia/mRemoteNG.Avalonia.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `mRemoteNG.Core/Connection/ExternalCredentialProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG.Core/Credential/CredentialRecord.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG.Core/Credential/ICredentialRecord.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG.Core/Credential/ICredentialRepository.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG.Core/Credential/ICredentialRepositoryList.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG.Core/Security/EncryptionException.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG.Core/Security/Factories/CryptoProviderFactory.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG.Core/Security/Factories/ICryptoProviderFactory.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG.Core/Security/ICryptographyProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG.Core/Security/PasswordCreation/IPasswordConstraint.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG.Core/Security/SymmetricEncryption/AeadCryptographyProvider.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNG.Core/mRemoteNG.Core.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNG.Platform.Windows/mRemoteNG.Platform.Windows.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNG.Protocols/mRemoteNG.Protocols.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `mRemoteNG.Tests.CrossPlatform/Platform/AesGcmCryptoProviderTests.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNG.Tests.CrossPlatform/mRemoteNG.Tests.CrossPlatform.csproj` - a new or repointed package can pull arbitrary code at restore time

### `769e9ca659` Add embedded SFTP browser and xterm.js SSH terminal

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/769e9ca65955e35437315154a840b7284392d97e) by Eran Markus
- **size:** 24 files (+2986/-44)
- **score -5** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Adds massive xterm.js SSH terminal and SFTP browser. We use PuTTY for SSH and choose to avoid high security risks and dependency bloat.
- **security flags:**
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/SSH/Resources/xterm.min.js` - added file has no reviewable text diff
  - `network-download` (critical) in `mRemoteNG/Tools/SftpFileService.cs` - added code fetches remote content at build or run time
  - `network-download` (critical) in `mRemoteNG/UI/Controls/SftpBrowserPanel.cs` - added code fetches remote content at build or run time
  - `process-exec` (critical) in `mRemoteNG/UI/Controls/SftpBrowserPanel.cs` - added code spawns a process or evaluates a string as code
  - `network-download` (critical) in `mRemoteNG/UI/Window/SFTPBrowserWindow.cs` - added code fetches remote content at build or run time
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `84b6724657` fix(rdp): connection bar - match exact class BBarWindowClass (from diagnostics)

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/84b672465781ad590cdddaa282365610d9da7ffa) by Claude Code
- **size:** 2 files (+34/-46)
- **score -5** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 4 | risk 4 | applies rewrite | REJECT
- **why:** Fixes that fork's custom passive-RDP connection-bar mover (BBarWindowClass hunt). Feature doesn't exist in our fork; fragile undocumented Win32 hack.

### `88cd3609a7` Implement Phase 3: VtNetCore Terminal Integration for SSH_DotNet Protocol

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/88cd3609a7fb13091f30fb869b1b919a232b13f5) by Dawie Joubert
- **size:** 2 files (+534/-14)
- **score -5** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 5 | risk 5 | applies conflict | REJECT
- **why:** Adds a custom-built SSH terminal control using VtNetCore. High complexity, massive maintenance overhead, and inferior to our mature, native PuTTY integration.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `a94861f40b` feat(migration): Phase 1 foundation — platform abstraction layer + Avalonia skeleton

- **fork:** [Morgadoo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a94861f40b3eca704fd69cdbd1b08515d80d8499) by Claude
- **size:** 53 files (+3769/-0)
- **score -5** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 5 | risk 5 | applies rewrite | REJECT
- **our issue:** #137
- **why:** Speculative 53-file Avalonia migration skeleton; #137 (macOS) already wontfix. Huge scope, CI/dependency security flags, incompatible with our COM-ref WinForms build.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/cross-platform.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `dependency-manifest` (high) in `mRemoteNG.Avalonia/mRemoteNG.Avalonia.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `process-exec` (critical) in `mRemoteNG.Platform.Linux/Clipboard/LinuxClipboardService.cs` - added code spawns a process or evaluates a string as code
  - `process-exec` (critical) in `mRemoteNG.Platform.Linux/Notifications/LinuxNotificationService.cs` - added code spawns a process or evaluates a string as code
  - `process-exec` (critical) in `mRemoteNG.Platform.Linux/Process/LinuxProcessService.cs` - added code spawns a process or evaluates a string as code
  - `security-code` (high) in `mRemoteNG.Platform.Linux/Security/LinuxCryptoProvider.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNG.Platform.Linux/mRemoteNG.Platform.Linux.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `process-exec` (critical) in `mRemoteNG.Platform.Mac/Clipboard/MacClipboardService.cs` - added code spawns a process or evaluates a string as code
  - `process-exec` (critical) in `mRemoteNG.Platform.Mac/Notifications/MacNotificationService.cs` - added code spawns a process or evaluates a string as code
  - `process-exec` (critical) in `mRemoteNG.Platform.Mac/Process/MacProcessService.cs` - added code spawns a process or evaluates a string as code
  - `security-code` (high) in `mRemoteNG.Platform.Mac/Security/MacCryptoProvider.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNG.Platform.Mac/mRemoteNG.Platform.Mac.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `process-exec` (critical) in `mRemoteNG.Platform.Windows/Process/WindowsProcessService.cs` - added code spawns a process or evaluates a string as code
  - `security-code` (high) in `mRemoteNG.Platform.Windows/Security/DpapiCryptoProvider.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNG.Platform.Windows/mRemoteNG.Platform.Windows.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `mRemoteNG.Platform/Security/AesGcmCryptoProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG.Platform/Security/ICryptoProvider.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNG.Platform/mRemoteNG.Platform.csproj` - a new or repointed package can pull arbitrary code at restore time

### `b032927087` Modernize remote connection UX while preserving session state

- **fork:** [YuLiangLin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b03292708796b64ab08f2b230ff0521aeefe6208) by Nathan Lin
- **size:** 60 files (+777/-152)
- **score -5** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** 60-file opinionated UX rework: drops VncSharpCore, writes PuTTY registry theme, ships binary icons; conflicts with our RDP/focus fixes, unreviewable risk.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `license` (medium) in `mRemoteNG/Icons/FLUENT-LICENSE.txt` - licence edits change redistribution terms
  - `binary-artifact` (critical) in `mRemoteNG/References/VncSharpCore.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `b2546cebab` focus improvements

- **fork:** [azet/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b2546cebab48e554b94ec2f8b4c611c5b60ea030) by Camilo Alvarez
- **size:** 3 files (+62/-71)
- **score -5** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 4 | risk 4 | applies conflict | REJECT
- **why:** Our fork already has vastly superior .NET 10 focus handling via c27314df2 and e592f8d8f. This outdated version causes regressions.

### `b818e7562b` Fix Qodo review: infinite loop, race condition, SecureString leak, hardcoded string

- **fork:** [yosale2011/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b818e7562b8d0482f34be7167bd96d32223a0977) by Yosale2011
- **size:** 5 files (+44/-5)
- **score -5** - already covered or rejected at triage
- **triage:** security | value 1 | effort 4 | risk 4 | applies rewrite | REJECT
- **why:** Patches fork-specific StartupUnlockService/XmlKeyValidator absent in our tree (verified); our Runtime.EncryptionKey differs; we shipped own MasterPasswordGate hardening (#128). Diff also risks use-after-dispose SecureString.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/XmlKeyValidator.cs` - credential and crypto paths need human review regardless of intent

### `e34ed81035` 自定义字体功能优化，现使用Type="System.Drawing.Font"，系统可自动转化

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e34ed81035e6eb244d930519a439bb7fd99875f8) by Hovn
- **size:** 9 files (+147/-98)
- **score -5** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 4 | risk 4 | applies rewrite | REJECT
- **why:** Personal font customization on legacy mRemoteV1 layout; also resurrects UpdateChannel default we deliberately removed (#136 GitHub-only). Conflicts with our settings model.

### `eac3e4d183` fix(rdp): release input capture after auto-reconnect (fix flying mouse)

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/eac3e4d183ccafb84fb61a4b55c37c2cecbf94e5) by Claude Code
- **size:** 2 files (+122/-24)
- **score -5** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 4 | risk 4 | applies rewrite | REJECT
- **why:** Fix for the niche passive RDP monitoring feature, which we have rejected; relies on non-existent HANDOFF.md.

### `f1b0b667da` Remove bundled PuTTYNG, auto-detect official PuTTY instead

- **fork:** [k-meeks/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f1b0b667da1d4e827c472d901a953e3dfb1a8de7) by Kyle Meeks
- **size:** 11 files (+279/-63)
- **score -5** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 4 | risk 4 | applies conflict | REJECT
- **why:** Removes bundled PuTTYNG; contrary to our design — we maintain robertpopa22/PuTTYNG and ship it intentionally. Also strips fork-specific Vault code.
- **security flags:**
  - `binary-artifact` (critical) in `mRemoteNG/PuTTYNG.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `process-exec` (critical) in `mRemoteNG/UI/Forms/OptionsPages/AdvancedPage.cs` - added code spawns a process or evaluates a string as code
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `installer` (high) in `mRemoteNGInstaller/Installer/Fragments/FilesFragment.wxs` - installer content ships signed to end users

### `04fbeb5d0e` added schema for local help files

- **fork:** [stdexception/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/04fbeb5d0e3a7067f07204377c97c54b6597d25a) by Faryan Rezagholi
- **size:** 1 files (+16/-2)
- **score -6** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 4 | applies rewrite | REJECT
- **why:** Targets obsolete mRemoteV1/CefSharp startup. Current Help opens maintained online documentation, WebView2 replaced CefSharp, and no bundled Help tree exists; only a fresh design could apply.

### `1fdf1db872` feat(phase-2+4): SSH File Transfer dialog, cross-platform install docs in README

- **fork:** [Morgadoo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/1fdf1db872bb16d6cfb602c66c97cdf429d3355f) by Claude
- **size:** 4 files (+301/-2)
- **score -6** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 4 | applies rewrite | REJECT
- **why:** Avalonia-based cross-platform UI code is incompatible with our Windows Forms codebase.

### `234e7f48f0` Finalize passive RDP monitor v2 fullscreen view-only and scroll behavior

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/234e7f48f098cbdf0eccd8a7e4aa718589434296) by guvity
- **size:** 3 files (+563/-46)
- **score -6** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 4 | applies rewrite | REJECT
- **why:** Passive RDP view-only monitor is that fork's niche feature; depends on their RdpInputBlocker.cs, absent here; heavy RDP protocol changes.

### `28b688bee5` Fix passive RDP monitor v2 focus, input blocking and scroll behavior

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/28b688bee56e0e056d891b984c8ac3ac94aed904) by guvity
- **size:** 3 files (+496/-135)
- **score -6** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 5 | risk 4 | applies conflict | REJECT
- **why:** Applies to passive RDP monitoring feature and RdpInputBlocker.cs, both of which are absent in our codebase.

### `5e5122dfe9` added custom action for .net 6 check

- **fork:** [VantIer/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/5e5122dfe900359b84b840aee5a302d509abdce6) by Faryan Rezagholi
- **size:** 1 files (+32/-1)
- **score -6** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 3 | risk 3 | applies rewrite | REJECT
- **why:** Legacy CustomActions installer deleted in our WiX 6 MSI rework; .NET 6 check moot (self-contained builds). Loop logic also buggy (last subkey wins).
- **security flags:**
  - `installer` (high) in `mRemoteNGInstaller/CustomActions/CustomActions.cs` - installer content ships signed to end users

### `613fd67125` test: add end-to-end harness for embedded SFTP browser and xterm.js terminal

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/613fd6712566cb98d92e372681a47d9e485637bb) by Eran Markus
- **size:** 20 files (+835/-451)
- **score -6** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 4 | applies rewrite | REJECT
- **why:** Specs already targets .NET 10; SFTP/xterm implementations are absent. This mixed Reqnroll/Docker/Playwright harness cannot validate current functionality; migrate SpecFlow separately if needed. [source](https://github.com/eran132/mRemoteNG/commit/613fd6712566cb98d92e372681a47d9e485637bb)
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `mRemoteNGSpecs/Features/CredentialRepository.feature.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGSpecs/Features/CredentialRepositoryList.feature.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNGSpecs/Playwright/XtermTerminalTests.TerminalRendering_MatchesVerifiedScreenshot.verified.png` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNGSpecs/StepDefinitions/CredentialRepositoryListSteps.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGSpecs/StepDefinitions/CredentialRepositorySteps.cs` - credential and crypto paths need human review regardless of intent
  - `network-download` (critical) in `mRemoteNGSpecs/StepDefinitions/SftpFileOperationsSteps.cs` - added code fetches remote content at build or run time
  - `process-exec` (critical) in `mRemoteNGSpecs/Support/SftpServerFixture.cs` - added code spawns a process or evaluates a string as code
  - `ci-config` (high) in `mRemoteNGSpecs/docker-compose.sftp.yml` - build pipeline definition runs with credentials
  - `dependency-manifest` (high) in `mRemoteNGSpecs/mRemoteNGSpecs.csproj` - a new or repointed package can pull arbitrary code at restore time

### `6eab38e820` fix(rdp): pin connection bar to top-right via WM_WINDOWPOSCHANGING subclass

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/6eab38e82036ac0c66ea35136aa79e937e2e89ae) by Claude Code
- **size:** 2 files (+109/-8)
- **score -6** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 5 | risk 4 | applies conflict | REJECT
- **why:** Part of guvity's custom passive-RDP RDP connection-bar mover, which doesn't exist in our codebase and relies on fragile, undocumented Win32 hacks.

### `71a7d3faad` Small Improvements

- **fork:** [Morgadoo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/71a7d3faada66bd62b3c049472408feb603082ab) by Luís Morgado
- **size:** 24 files (+1199/-184)
- **score -6** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 4 | applies rewrite | REJECT
- **why:** Targets mRemoteNG.Avalonia project that does not exist in our fork; fork-specific architecture, nothing portable.
- **security flags:**
  - `security-code` (high) in `mRemoteNG.Avalonia/Views/Dialogs/CredentialManagerDialog.axaml.cs` - credential and crypto paths need human review regardless of intent
  - `process-exec` (critical) in `mRemoteNG.Avalonia/Views/MainWindow.axaml.cs` - added code spawns a process or evaluates a string as code
  - `dependency-manifest` (high) in `mRemoteNG.Core/mRemoteNG.Core.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNG.Platform.Windows/mRemoteNG.Platform.Windows.csproj` - a new or repointed package can pull arbitrary code at restore time

### `73267c1fcf` Tag releases with the version that was built; honor release_flag

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/73267c1fcfc95daccb3abd954b2a122c6007bd09) by vindict6
- **size:** 1 files (+18/-24)
- **score -6** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 3 | applies rewrite | REJECT
- **why:** Fixes dated-NB tagging our fork removed; 2-release model (ef6420d9a) tags from csproj Version, no T4 build numbers. Obsolete.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `8cc897b616` Avoid redundant passive RDP tab scroll restores

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/8cc897b61666160199a61bfbdcc6ec9694d843a3) by guvity
- **size:** 1 files (+52/-6)
- **score -6** - already covered or rejected at triage
- **triage:** perf | value 1 | effort 5 | risk 4 | applies rewrite | REJECT
- **why:** Depends on guvity's absent passive-RDP scroll/view-only subsystem and deleted RdpProtocol6; our RDP code has no matching timers or issue. Porting this follow-up alone is meaningless.

### `8daed19163` Fix passive RDP scroll origin and enable view-only after scroll

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/8daed191630d8472f8d48971a914ab64239603d6) by guvity
- **size:** 2 files (+240/-214)
- **score -6** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 5 | risk 4 | applies rewrite | REJECT
- **why:** Iterates that fork's bespoke passive-RDP view-only/scroll machinery, absent from our fork. Niche monitoring use case; not worth porting the whole subsystem.

### `91b1ba820c` fix(rdp): pin connection bar post-mstscax + add after-move/thread diagnostics

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/91b1ba820cc38aff7283964ff37b1e731004039e) by Claude Code
- **size:** 1 files (+18/-1)
- **score -6** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 5 | risk 4 | applies rewrite | REJECT
- **why:** Part of guvity's custom passive-RDP RDP connection-bar mover, which does not exist in our codebase and relies on fragile, undocumented Win32 hacks.

### `969f940146` feat(ui): add 'Work in Fullscreen' tab menu item (drop View Only + go fullscreen)

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/969f94014686a95e0e149249dde3befa29be69ba) by Claude Code
- **size:** 3 files (+60/-1)
- **score -6** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 4 | applies rewrite | REJECT
- **why:** Part of guvity's fork-specific passive-RDP feature and HANDOFF.md, which do not exist in our codebase. Requires a complete rewrite.

### `a8cf98d933` Add reveal password context menu for password fields

- **fork:** [yosale2011/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a8cf98d9337077278cde6c1772ce2e826eda135d) by Yosale2011
- **size:** 4 files (+104/-1)
- **score -6** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 3 | risk 3 | applies rewrite | REJECT
- **our issue:** #128
- **why:** Our fork already ships hardened reveal/copy with MasterPasswordGate + clipboard hygiene (0e7b9c75e, #128). This is weaker duplicate of existing feature.

### `ad8aa8500b` Restore passive RDP scroll after DockPanel tab activation

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/ad8aa8500be164b820972a8ab2ca4106191260e7) by guvity
- **size:** 2 files (+146/-0)
- **score -6** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 5 | risk 4 | applies rewrite | REJECT
- **why:** [Source](https://github.com/guvity/mRemoteNG-passive-rdp/commit/ad8aa8500be164b820972a8ab2ca4106191260e7): Depends on absent passive-scroll machinery; tab-activation retries address no listed issue and cannot be transplanted independently.

### `b1baa89108` Fixed incorrect menu showed on left click

- **fork:** [azet/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b1baa8910846e37ccb8bbfaca648e0925babeed9) by Camilo Alvarez
- **size:** 1 files (+3/-7)
- **score -6** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 4 | applies conflict | REJECT
- **why:** [Source](https://github.com/azet/mRemoteNG/commit/b1baa8910846e37ccb8bbfaca648e0925babeed9): Obsolete mRemoteV1 workaround uses potentially stale global TabHelper; current pane-local lookup also handles floating ActiveContent.

### `b801a78ca0` added custom action to check if .net 6 is installed

- **fork:** [VantIer/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b801a78ca0edaa024ad83bd8a1dfee74d9562cb9) by Faryan Rezagholi
- **size:** 5 files (+15/-24)
- **score -6** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 3 | applies rewrite | REJECT
- **why:** Targets legacy WiX3 installer and .NET 6; our MSI is WiX 6 SDK (Package.wxs) on .NET 10 with self-contained option. Obsolete.
- **security flags:**
  - `installer` (high) in `mRemoteNGInstaller/Installer/CustomActions/CheckPrerequisites.wxs` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/Installer/Includes/Config.wxi` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/Installer/Installer.wixproj` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/Installer/Localizations/en-US.wxl` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/Installer/mRemoteNG.wxs` - installer content ships signed to end users

### `bd70907e65` BouncyCastle.Crypto版本升级至1.8.9

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bd70907e65b0790c80f931e957f4ec03160cd681) by Hovn
- **size:** 4 files (+6/-6)
- **score -6** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 4 | applies rewrite | REJECT
- **why:** Legacy projects are gone, and BouncyCastle.Cryptography 2.6.2 supersedes 1.8.9; translating this would be a security-relevant downgrade.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNGTests/mRemoteNGTests.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNGTests/packages.config` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteV1/mRemoteV1.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteV1/packages.config` - a new or repointed package can pull arbitrary code at restore time

### `bdd7b73b33` Add Subresource Integrity hashes to xterm.js resources

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bdd7b73b33c38df1e08163e9e35b5438b5f30fce) by Eran Markus
- **size:** 1 files (+3/-3)
- **score -6** - already covered or rejected at triage
- **triage:** security | value 1 | effort 5 | risk 4 | applies rewrite | REJECT
- **why:** Our fork has no xterm terminal resource stack, so these fixed SRI attributes have no target or standalone benefit.

### `c99fe4b0ef` removed oboslete settings for rendering engine

- **fork:** [stdexception/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c99fe4b0efae22c17b366ea7042b4bcfe1de5c6f) by Faryan Rezagholi
- **size:** 2 files (+1/-31)
- **score -6** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 5 | applies conflict | REJECT
- **why:** We preserve and use these rendering engine settings configured to EdgeChromium/ExternalBrowser; deleting them breaks HTTP connections.

### `e707915f13` Add reveal password context menu for password fields

- **fork:** [yosale2011/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e707915f130514bd2ed4632902cba9b823b4a604) by Yosale2011
- **size:** 4 files (+104/-1)
- **score -6** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 3 | risk 3 | applies rewrite | REJECT
- **our issue:** #128
- **why:** Our fork already ships stronger gated reveal via PasswordRevealEditor and MasterPasswordGate, plus clipboard hygiene; this duplicate uses an incompatible MasterPasswordService.

### `15635dff6d` added copy password option

- **fork:** [hthvdmeer/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/15635dff6d0d6ad9d2a435430c50a41ba319e6c3) by takemaker63
- **size:** 3 files (+24/-5)
- **score -7** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 2 | risk 4 | applies conflict | REJECT
- **our issue:** #128
- **why:** Plaintext SetText copy with no re-auth gate; our 0e7b9c75e already ships gated copy/reveal with SetSecret clipboard hygiene. Importing would regress security.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `2b9effdf71` updated script

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2b9effdf714afc9a8f25bffdaa8deefa1097eeae) by Faryan Rezagholi
- **size:** 1 files (+6/-4)
- **score -7** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 4 | risk 5 | applies rewrite | REJECT
- **why:** Obsolete legacy cleaner is absent from the SDK-style .NET 10 pipeline; moving all DLLs would break modern probing. Its desired JSON preservation is already native.
- **security flags:**
  - `build-script` (high) in `Tools/clean_ouput_dir.ps1` - scripts execute on a maintainer machine

### `581d55a557` 优化及补全中文翻译

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/581d55a5578492b68f4d3ff57826d6e76e0b3f9f) by Hovn
- **size:** 5 files (+719/-110)
- **score -7** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** Only 18 changed keys survive and all are translated; 223 are obsolete, paths changed, updater channels vanished, and the current 241 gaps are unrelated.

### `aa49442259` Add clean passive RDP monitor build for 1.77.2 release

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/aa494422597547f76a8c934797df4228cdfe6001) by guvity
- **size:** 3 files (+824/-0)
- **score -7** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 4 | risk 5 | applies rewrite | REJECT
- **why:** Fork-private passive/view-only RDP monitor build for old 1.77.2 via patch script + custom CI. Niche use-case, targets .NET 6 codebase, irrelevant to our .NET 10 fork.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-1772-clean.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `build-script` (high) in `Tools/passive-rdp-monitor-1772-clean.ps1` - scripts execute on a maintainer machine

### `bfcf3c26d4` Add NickHQ session controller — register terminal tabs, poll + execute remote commands

- **fork:** [nickbeentjes/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bfcf3c26d484e82baee96cfef319acc1d1c1b572) by Kees
- **size:** 3 files (+572/-0)
- **score -7** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 4 | risk 5 | applies rewrite | REJECT
- **why:** Personal remote-control backdoor: polls private server, executes arbitrary commands, screenshots sessions. Hardcoded owner URL. Security liability, zero user value.
- **security flags:**
  - `process-exec` (critical) in `mRemoteNG/Connection/NickHq/NickHqClient.cs` - added code spawns a process or evaluates a string as code

### `07954f9fdb` 程序配置文件更新

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/07954f9fdbba88874c2f3a6e6c1ffe79fa212f21) by Hovn
- **size:** 4 files (+794/-765)
- **score -8** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Generated settings churn targets deleted mRemoteV1 files and a developer-specific .csproj.user. Current settings are page-split; importing it would regress configuration without a coherent feature.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteV1/mRemoteV1.csproj.user` - a new or repointed package can pull arbitrary code at restore time

### `2722d1ae1e` Prevent current tab edge cases

- **fork:** [azet/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2722d1ae1e98bd068b30d50a06396e550db6a8b7) by Camilo Alvarez
- **size:** 1 files (+4/-1)
- **score -8** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 3 | risk 4 | applies rewrite | REJECT
- **our issue:** #118
- **why:** Old mRemoteV1 codebase; leftover debug spam in diff. Our fork rewrote WM_MOUSEACTIVATE/focus handling extensively (#110/#118/#143) — edge case already covered.

### `38c62c81e3` Implement SSH_DotNet Protocol Phase 1 & 2

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/38c62c81e34e0038c8b0e45f4b0b8d2558b743cc) by Dawie Joubert
- **size:** 16 files (+5504/-1)
- **score -8** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Incomplete prototype: terminal input/output are placeholders, VtNetCore is unused, enum 15 collides with VMRC, and existing SSH/OpenSSH already covers the capability.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `build-script` (high) in `run_ssh_tests.ps1` - scripts execute on a maintainer machine

### `3aa43202ea` Synced from my lab

- **fork:** [CancanTang/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/3aa43202ea30f15390aa84c94cf9d63b03de75df) by CancanTang
- **size:** 300 files (+225/-73835)
- **score -8** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Unauditable lab snapshot: 290 of 300 files are deletions, including ObjectListView, serializers, license, manifests, and build tooling; it fixes no listed issue.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `env-secret-access` (critical) in `.github/workflows/Build_mR-NB.yml` - added code reads credentials or CI secrets
  - `ci-workflow` (critical) in `.github/workflows/filter-links.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `license` (medium) in `COPYING.txt` - licence edits change redistribution terms
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `ExternalConnectors/ExternalConnectors.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `ExternalConnectors/OP/OnePasswordCli.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `ObjectListView/ObjectListView.NetCore.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `build-script` (high) in `Tools/CreateBulkConnections_ConfCons2_6.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/create_upg_chk_files.ps1` - scripts execute on a maintainer machine
  - `security-code` (high) in `Tools/decrypt.bat` - credential and crypto paths need human review regardless of intent
  - `build-script` (high) in `Tools/decrypt.bat` - scripts execute on a maintainer machine
  - `security-code` (high) in `Tools/encrypt.bat` - credential and crypto paths need human review regardless of intent
  - `build-script` (high) in `Tools/encrypt.bat` - scripts execute on a maintainer machine
  - `binary-artifact` (critical) in `Tools/exes/dumpbin.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/editbin.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/link.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/mspdbcore.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/sigcheck.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `build-script` (high) in `Tools/find_vstool.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/github_functions.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/postbuild.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/postbuild_installer.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/postbuild_portable.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/publish_draft_github_release.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/publish_to_github.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/rename_and_copy_installer.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/set_LargeAddressAware.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/sign_binaries.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/signfiles.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/tidy_files_for_release.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/update_and_upload_assemblyinfocs.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/update_and_upload_website_release_json_file.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/validate_microsoft_tool.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/verify_LargeAddressAware.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/verify_binary_signatures.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/zip_files.ps1` - scripts execute on a maintainer machine
  - `security-code` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsDocumentEncryptor.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialProviderSerializer/CredentialRepositoryListDeserializer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialProviderSerializer/CredentialRepositoryListSerializer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialPasswordDecryptorDecorator.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialPasswordEncryptorDecorator.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialRecordDeserializer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialRecordSerializer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/XmlConnectionsDecryptor.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryCredentialsPage.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Connection/ExternalCredentialProviderSelector.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Connection/Protocol/RDP/RDGatewayUseConnectionCredentials.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/CredentialChangedEventArgs.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/CredentialDeletionMsgBoxConfirmer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/CredentialDomainUserComparer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/CredentialInfo.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/CredentialRecord.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/CredentialRecordTypeConverter.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/CredentialServiceFacade.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/CredentialServiceFactory.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/ICredentialRecord.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/ICredentialRepository.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/ICredentialRepositoryList.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/PlaceholderCredentialRecord.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/CompositeRepositoryUnlocker.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/CredentialRepoUnlockerBuilder.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/CredentialRepositoryChangedArgs.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/CredentialRepositoryConfig.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/CredentialRepositoryList.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/ICredentialRepositoryConfig.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/XmlCredentialRepository.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/XmlCredentialRepositoryFactory.cs` - credential and crypto paths need human review regardless of intent

### `3b345ef4a6` added request handler to differentiate between local and remote sites

- **fork:** [stdexception/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/3b345ef4a681623b0e84bc5e88b6f78ebcfae5f2) by Faryan Rezagholi
- **size:** 1 files (+64/-0)
- **score -8** - already covered or rejected at triage
- **triage:** security | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Introduces a critical arbitrary command execution vulnerability via Process.Start and depends on CefSharp, which our codebase does not use.
- **security flags:**
  - `process-exec` (critical) in `mRemoteV1/Connection/Protocol/Http/Connection.Protocol.HTTP.RequestHandler.cs` - added code spawns a process or evaluates a string as code

### `4579d98600` jk theres more

- **fork:** [changsongyang/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4579d98600528691c456315c847b1b913dd497ab) by Faryan Rezagholi
- **size:** 53 files (+11/-125)
- **score -8** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 4 | applies rewrite | REJECT
- **why:** Analyzer-clean SDK modernization supersedes this cleanup; importing it would remove required NSubstitute namespaces and the actively used, centrally pinned ConfigurationManager dependency.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `mRemoteNGTests/Config/CredentialRecordLoaderTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Config/Serializers/CredentialProviderSerializerTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Config/Serializers/CredentialSerializers/XmlCredentialPasswordEncryptorDecoratorTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Credential/CompositeRepositoryUnlockerTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Credential/CredentialChangedEventArgsTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Credential/CredentialDeletionMsgBoxConfirmerTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Credential/CredentialDomainUserComparerTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Credential/CredentialRecordTypeConverterTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Credential/CredentialRepositoryListTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Credential/CredentialServiceFacadeTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/IntegrationTests/XmlCredentialSerializerLifeCycleTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/UI/Forms/PasswordFormTests.cs` - credential and crypto paths need human review regardless of intent

### `457fcad4a0` Fix NB build workflow

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/457fcad4a08d67d7e3975d33577ec158af4515f9) by vindict6
- **size:** 2 files (+75/-38)
- **score -8** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 4 | applies conflict | REJECT
- **why:** Fork-of-our-fork repairing its own NB workflow. Our CI already restructured (windows-2025-vs2026, 2-release model, all GREEN). Fix targets divergent workflow state.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `4e04f5ca22` removed option to hide connection tab when only one connection is open.

- **fork:** [changsongyang/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4e04f5ca22d9fdaab85535c9a3eda6a282854266) by Faryan Rezagholi
- **size:** 32 files (+275/-664)
- **score -8** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** This obsolete mixed commit removes two localized, tested tab-visibility options our fork deliberately supports; its mRemoteV1 project structure no longer exists.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG.Specs/mRemoteNG.Specs.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNG.Specs/packages.config` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNGTests/mRemoteNGTests.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteV1/mRemoteV1.csproj` - a new or repointed package can pull arbitrary code at restore time

### `4eb1833a62` Enhance build instructions and settings; refactor connection handling

- **fork:** [lthobois/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4eb1833a62da203783124f967105d0516b4d416c) by Loïc THOBOIS
- **size:** 12 files (+329/-89)
- **score -8** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 4 | applies conflict | REJECT
- **why:** VS Code tasks/launch config for their dev setup; we build via build.ps1. Bundles unseen connection-handling refactor (truncated diff) — blind import risky, no mapped issue.
- **security flags:**
  - `build-script` (high) in `Tools/invoke_msbuild.ps1` - scripts execute on a maintainer machine

### `5dbc11d851` fix(rdp): connection bar mover v2 (geometry-based) + enlarge new menu item

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/5dbc11d851a0b96dbcd1188f5680fe9c393cefcf) by Claude Code
- **size:** 3 files (+40/-18)
- **score -8** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Unrequested passive-RDP feature from a divergent fork; geometry-only window selection can move the wrong window, remains untested, and requires a ground-up design.

### `64c2de409f` Commit passive RDP auto-scroll position like manual scrollbar movement

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/64c2de409f13f2a48456aa3d950ddbbad6c876f5) by guvity
- **size:** 1 files (+180/-0)
- **score -8** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Depends on an absent passive-RDP patch and deleted RdpProtocol6; intrusive scrollbar messages and timers lack a matching issue or current reproduction. [source](https://github.com/guvity/mRemoteNG-passive-rdp/commit/64c2de409f13f2a48456aa3d950ddbbad6c876f5)

### `6c62af68f9` feat(phase-2+4): PortScannerViewModel, PortScanner codebehind, Snap packaging

- **fork:** [Morgadoo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6c62af68f96d9a8c455a815ae177a9d6eb8e47ec) by Claude
- **size:** 3 files (+193/-0)
- **score -8** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Targets Avalonia UI and Linux packaging (Snap), which are completely out-of-scope and incompatible with our WinForms/.NET 10 architecture.

### `6dbfccb60c` fix(build): enable CLI build by fixing CPM, missing refs, and namespace collision

- **fork:** [Morgadoo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6dbfccb60cfcf34b317d7e41df13852aab714a4f) by Luís Morgado
- **size:** 8 files (+149/-28)
- **score -8** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 5 | applies conflict | REJECT
- **why:** Relates entirely to a cross-platform Avalonia UI migration codebase structure that does not exist in our Windows-focused .NET 10 WinForms fork.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNG.Platform/mRemoteNG.Platform.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNG.Protocols/mRemoteNG.Protocols.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNG.Tests.CrossPlatform/mRemoteNG.Tests.CrossPlatform.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `6e9087ebd5` feat(phase-4): Docs, integration test scaffolding, migration progress update — 94% complete

- **fork:** [Morgadoo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6e9087ebd5830c6d09975b3e0c9f8a106826ebb5) by Claude
- **size:** 6 files (+448/-30)
- **score -8** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Progress docs for Morgadoo's Avalonia cross-platform migration branch; meaningless outside that effort. macOS ask (#137) already wontfix.
- **security flags:**
  - `network-download` (critical) in `docs/contributing-cross-platform.md` - added code fetches remote content at build or run time
  - `dependency-manifest` (high) in `mRemoteNG.Tests.CrossPlatform/mRemoteNG.Tests.CrossPlatform.csproj` - a new or repointed package can pull arbitrary code at restore time

### `6ec578b3b6` added 32 and 64 bit build configs

- **fork:** [changsongyang/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6ec578b3b6e2d668fc042c8eda3d5c3c98788165) by Faryan Rezagholi
- **size:** 2 files (+46/-66)
- **score -8** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 4 | applies rewrite | REJECT
- **why:** Old-format sln x86/x64 configs; our .NET 10 SDK-style solution already builds x86/x64/ARM64 in CI. Obsolete.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `78397c8d48` Fix passive RDP scroll sizing and fullscreen exit safety

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/78397c8d48be385982357a477b632f5073672a8e) by guvity
- **size:** 2 files (+498/-101)
- **score -8** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 5 | risk 5 | applies conflict | REJECT
- **why:** Part of a custom passive RDP monitoring subsystem absent from our repository. Porting it is high-effort and risks breaking standard RDP scrolling behavior.

### `7a1a9bcd7c` Fixed some AI Codepilot items

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7a1a9bcd7c4d26cf6e4b44532b49b97ab6e9e5c0) by Dawie Joubert
- **size:** 13 files (+3048/-198)
- **score -8** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Analyzer cleanup and one null guard atop an unfinished 22% native-SSH branch; prerequisite code is absent and current SSH remains PuTTY-based.

### `7adcba145c` refactor(ssh_dotnet): static terminal helpers + drop dead stream fields (S2325/S1144)

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7adcba145c9b87c92ecf2de7c60fcdc5349c476d) by Dawie Joubert
- **size:** 3 files (+11/-31)
- **score -8** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Refactors SSH_DotNet terminal control. This protocol is an unmerged experimental feature not present in our fork, so this commit does not apply.

### `824b7ef740` Reorganized files to make it easier to apply MSBuild settings per types of projects (src, tests, docs, other)

- **fork:** [savornicesei/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/824b7ef740ca934a5df64665acec03c20bce5623) by Simona Avornicesei
- **size:** 300 files (+205/-27)
- **score -8** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 5 | applies conflict | REJECT
- **why:** Reorganizes entire repository structure into nested folders. Highly disruptive, conflicts with our working MSBuild settings/CI, and offers no user-visible benefit.
- **security flags:**
  - `build-script` (high) in `build.ps1` - scripts execute on a maintainer machine
  - `dependency-manifest` (high) in `documentation/mRemoteNG.Docs.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `installer/CustomActions/CustomActions.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `installer` (high) in `installer/Installer/CustomActions/CheckForInstalledWindowsUpdates.wxs` - installer content ships signed to end users
  - `installer` (high) in `installer/Installer/CustomActions/SaveInstallLocation.wxs` - installer content ships signed to end users
  - `installer` (high) in `installer/Installer/CustomActions/UninstallLegacyVersions.wxs` - installer content ships signed to end users
  - `installer` (high) in `installer/Installer/CustomDialogs/My_CustomizeDlg.wxs` - installer content ships signed to end users
  - `installer` (high) in `installer/Installer/CustomDialogs/My_WixUI_FeatureTree.wxs` - installer content ships signed to end users
  - `installer` (high) in `installer/Installer/Fragments/DirectoriesFragment.wxs` - installer content ships signed to end users
  - `installer` (high) in `installer/Installer/Fragments/FilesFragment.wxs` - installer content ships signed to end users
  - `installer` (high) in `installer/Installer/Fragments/MainExeFragment.wxs` - installer content ships signed to end users
  - `installer` (high) in `installer/Installer/Fragments/MiscTextFilesFragment.wxs` - installer content ships signed to end users
  - `installer` (high) in `installer/Installer/Fragments/PuTTYNGFragment.wxs` - installer content ships signed to end users
  - `installer` (high) in `installer/Installer/Fragments/RegistryEntriesFragment.wxs` - installer content ships signed to end users
  - `installer` (high) in `installer/Installer/Fragments/ShortcutFragment.wxs` - installer content ships signed to end users
  - `license` (medium) in `installer/Installer/Resources/License.rtf` - licence edits change redistribution terms
  - `installer` (high) in `installer/Installer/mRemoteNG.wxs` - installer content ships signed to end users
  - `build-script` (high) in `scripts/dotnet_framework_functions.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `scripts/pwsh_functions.ps1` - scripts execute on a maintainer machine
  - `dependency-manifest` (high) in `src/ExternalConnectors/ExternalConnectors.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `src/mRemoteNG/App/Info/CredentialsFileInfo.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `src/mRemoteNG/Config/CredentialHarvester.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `src/mRemoteNG/Config/CredentialRecordLoader.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `src/mRemoteNG/Config/CredentialRecordSaver.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `src/mRemoteNG/Config/CredentialRepositoryListLoader.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `src/mRemoteNG/Config/CredentialRepositoryListSaver.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `src/mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsDocumentEncryptor.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `src/mRemoteNG/Config/Serializers/CredentialProviderSerializer/CredentialRepositoryListDeserializer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `src/mRemoteNG/Config/Serializers/CredentialProviderSerializer/CredentialRepositoryListSerializer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `src/mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialPasswordDecryptorDecorator.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `src/mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialPasswordEncryptorDecorator.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `src/mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialRecordDeserializer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `src/mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialRecordSerializer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `src/mRemoteNG/Config/Serializers/XmlConnectionsDecryptor.cs` - credential and crypto paths need human review regardless of intent

### `888cc44fda` Add AI layer: Claude chat, session logging, SCP transfers, host-call protocol, Windows agent

- **fork:** [nickbeentjes/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/888cc44fdae9f83a989724966d1816efd17138ed) by Kees
- **size:** 15 files (+1914/-1)
- **score -8** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Personal AI-layer experiment (Claude chat panel, SendKeys command injection, API keys in settings). Out of scope, large attack surface, no user demand in our tracker.

### `8a2140793b` Add unit tests, UX improvements, and runtime SRI hash injection

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8a2140793b56747632cafdf7d2f2ae47e66b4ec1) by Eran Markus
- **size:** 9 files (+394/-23)
- **score -8** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Depends on WebView2 SSH and SFTP implementations which are absent in our fork. Focuses on third-party features we do not support.

### `9680ed90af` fixed help window and about windows partially

- **fork:** [stdexception/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9680ed90af33119bbce923ed10e117a7a1e03b8a) by Faryan Rezagholi
- **size:** 6 files (+97/-134)
- **score -8** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** We completely removed embedded browsers from FrmAbout, utilizing system-default browser links. Introducing CefSharp for this would add heavy, redundant dependencies and bloat.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteV1/mRemoteV1.csproj` - a new or repointed package can pull arbitrary code at restore time

### `9752e65ad9` Address code review findings from SonarCloud, Qodo, and Copilot

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9752e65ad9ad4f717c06a9d2a019abec59a37737) by Eran Markus
- **size:** 11 files (+193/-149)
- **score -8** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Follow-up to rejected xterm/SFTP code absent here; PuTTY remains canonical. Its notification-handle fix is already covered more safely by our #53 buffering; no portable delta.

### `9db5064098` Apply passive RDP monitor file replacement for 1.77.2

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/9db50640988827947b013ead3fcb7ccef7e65be4) by guvity
- **size:** 3 files (+1112/-836)
- **score -8** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Wholesale RdpProtocol6/8 file replacement backporting fork's passive-RDP feature onto 1.77.2, plus new CI workflow (flagged critical). Incompatible with our 1600-commit RDP code.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-1772-files.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `a9f463c540` removed resize events of main form

- **fork:** [VantIer/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a9f463c540eef91e033bff7b4fcbeb9d6598e842) by Faryan Rezagholi
- **size:** 2 files (+2/-67)
- **score -8** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 5 | applies conflict | REJECT
- **why:** Deletes load-bearing resize, tray, auto-lock, title, menu, focus, and RDP propagation paths. Newer targeted fixes depend on this infrastructure; importing would regress behavior.

### `aa036e0c97` Fix RDP mouse capture after fullscreen leave and fine tune scroll edge

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/aa036e0c972a2a60c3cc79374c9ddcfd0540dd40) by guvity
- **size:** 2 files (+281/-28)
- **score -8** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** [Source](https://github.com/guvity/mRemoteNG-passive-rdp/commit/aa036e0c972a2a60c3cc79374c9ddcfd0540dd40): Requires absent fork-only passive-scroll infrastructure; no tracked symptom justifies its timer-heavy fullscreen and input rewrite.

### `ac9f2051ae` Revert "Remember passive RDP scroll position across tab switches"

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/ac9f2051ae4ad55c14088fcf79f9bca8a5dd5b62) by guvity
- **size:** 1 files (+0/-262)
- **score -8** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Revert of fork-private passive-scroll feature that never existed in our tree. RdpProtocol6 code diverged heavily from ours. Nothing to import.

### `b4e8baa3ab` Apply passive RDP monitor file replacement for 1.77.2

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/b4e8baa3abf674b664a856f026bbd2b5fc121e62) by guvity
- **size:** 3 files (+1112/-836)
- **score -8** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 5 | applies conflict | REJECT
- **why:** Unverified passive monitor and workflow based on ancient 1.77.2. Reverts active RDP files and is incompatible with .NET 10.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-1772-files.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `bbc8b5e957` feat(phase-4): Packaging, testing & nightly CI — Linux AppImage/.deb/Flatpak, macOS DMG, Windows MSI, cross-platform tests

- **fork:** [Morgadoo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bbc8b5e9575eddac9c1edef4cebdc56c712008ee) by Claude
- **size:** 17 files (+1749/-14)
- **score -8** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **our issue:** #137
- **why:** Avalonia cross-platform packaging for projects (Platform/Protocols/Avalonia) we don't have; macOS is wontfix (#137); our 2-release CI model already covers Windows MSI/nightly.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/cross-platform.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `ci-workflow` (critical) in `.github/workflows/nightly-build.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `security-code` (high) in `mRemoteNG.Tests.CrossPlatform/Platform/AesGcmCryptoProviderTests.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNG.Tests.CrossPlatform/mRemoteNG.Tests.CrossPlatform.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `build-script` (high) in `packaging/linux/build-appimage.sh` - scripts execute on a maintainer machine
  - `build-script` (high) in `packaging/linux/build-deb.sh` - scripts execute on a maintainer machine
  - `build-script` (high) in `packaging/macos/build-dmg.sh` - scripts execute on a maintainer machine
  - `build-script` (high) in `packaging/windows/build-installer.ps1` - scripts execute on a maintainer machine

### `be53187197` Enhance SCP/SFTP file browser with recursive operations and TreeView refresh

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/be531871977c1f095630999589196cb327d124f0) by Dawie Joubert
- **size:** 13 files (+2020/-62)
- **score -8** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies conflict | REJECT
- **why:** We do not have the embedded SFTP/SCP browser codebase in our fork. This commit is not applicable to our project.
- **security flags:**
  - `network-download` (critical) in `mRemoteNG/Connection/Protocol/SCP/ScpTransferManager.cs` - added code fetches remote content at build or run time
  - `network-download` (critical) in `mRemoteNG/UI/Controls/SCP/ScpFileTransferControl.cs` - added code fetches remote content at build or run time

### `bef31a3ca2` NickHQ multi-server config: settings UI, auto-connect on startup

- **fork:** [nickbeentjes/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bef31a3ca23b4784bfc97be21f022febf983a4f3) by Kees
- **size:** 4 files (+710/-88)
- **score -8** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Personal fork's private NickHQ backend: registers sessions, polls remote server, executes exec/paste/screenshot commands. Effectively a remote-control agent with hardcoded Tailscale URL. Unacceptable.

### `c5506049a6` Refactor methods to reduce cognitive complexity below threshold

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c5506049a62658c40c7896bff495a22cdd87df5b) by Eran Markus
- **size:** 3 files (+175/-180)
- **score -8** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Refactors custom WebView2/xterm.js SSH terminal and SFTP browser files that are absent in our codebase; we still use PuTTY.
- **security flags:**
  - `network-download` (critical) in `mRemoteNG/UI/Controls/SftpBrowserPanel.cs` - added code fetches remote content at build or run time

### `d105cb9e88` 大量调整：mRemoteNG.exe反编译修改的所有内容同步至源码

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d105cb9e8896586b26b265ae163941485a6aee88) by Hovn
- **size:** 39 files (+2436/-1942)
- **score -8** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Massive, unreviewable dump of decompiled code containing opaque binaries and security risks. Completely incompatible with our modern .NET 10 directory structure.
- **security flags:**
  - `opaque-file` (high) in `mRemoteV1/Resources/Images/Drag_Icon_Disable.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteV1/Resources/Images/Drag_Icon_Enable.png` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteV1/UI/Forms/PasswordForm.Designer.cs` - credential and crypto paths need human review regardless of intent

### `d4831bd71f` feat(phase-2): Complete Avalonia UI migration — themes, docking, dialogs, tray

- **fork:** [Morgadoo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d4831bd71f4755fd8d1b0a44cd2c32728e93e9f5) by Claude
- **size:** 97 files (+2824/-132)
- **score -8** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Experimental Avalonia cross-platform rewrite, incompatible with our WinForms fork; #137 (macOS) already wontfix. Opaque binaries flagged.
- **security flags:**
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Admin.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Anti Virus.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Apple.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Backup.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Build Server.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Console.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Database.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Domain Controller.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/ESX.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Fax.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/File Server.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Finance.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Firewall.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Infrastructure.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Linux.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Log.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Mail Server.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/PowerShell.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Production.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/PuTTY.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/RaspberryPi.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Remote Desktop.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Router.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/SSH.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/SharePoint.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Staging.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Switch.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Tel.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Telnet.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Terminal Server.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Test Server.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Virtual Machine.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/WSL.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Web Server.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/WiFi.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Windows.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/Workstation.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/mRemote.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.Avalonia/Assets/Icons/mRemoteNG.ico` - added file has no reviewable text diff
  - `process-exec` (critical) in `mRemoteNG.Avalonia/Views/Dialogs/AboutDialog.axaml.cs` - added code spawns a process or evaluates a string as code
  - `security-code` (high) in `mRemoteNG.Avalonia/Views/OptionsPages/CredentialsSettingsPage.axaml` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG.Avalonia/Views/OptionsPages/CredentialsSettingsPage.axaml.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNG.Avalonia/mRemoteNG.Avalonia.csproj` - a new or repointed package can pull arbitrary code at restore time

### `d49f440d52` Add orchestrator engine + rich notifications panel; drop Telegram

- **fork:** [nickbeentjes/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d49f440d5279c765b6c0412628a22170f98da961) by Kees
- **size:** 16 files (+1865/-3)
- **score -8** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Fork-personal NickHQ orchestrator/notifications infrastructure; depends on NickHqClient we don't have; no user value for our fork.

### `d6fdfcbff9` fix(rdp): connection bar mover v3 (recursive child search + file diagnostics)

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/d6fdfcbff92310f3f958c8cfae85e0afed44270a) by Claude Code
- **size:** 2 files (+67/-38)
- **score -8** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** No matching issue; legacy geometry heuristics can move unrelated windows, repeat UI-thread calls, leak hostnames, and cannot write beside installed executables.

### `db8bc6a87b` moved .net 6 check custom action to its own class

- **fork:** [VantIer/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/db8bc6a87b13f8473d71c821f196f9d69f030c0f) by Faryan Rezagholi
- **size:** 3 files (+64/-30)
- **score -8** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Legacy WiX3/.NET 6 custom actions were removed for WiX 6/.NET 10. The checker also null-dereferences and accepts only exact 6.0.0.
- **security flags:**
  - `installer` (high) in `mRemoteNGInstaller/CustomActions/CustomActions.cs` - installer content ships signed to end users
  - `dependency-manifest` (high) in `mRemoteNGInstaller/CustomActions/CustomActions.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `installer` (high) in `mRemoteNGInstaller/CustomActions/CustomActions.csproj` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/CustomActions/DotnetInstalledChecker.cs` - installer content ships signed to end users

### `ebd3383a1c` Remember passive RDP scroll position across tab switches

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/ebd3383a1c6bc2a2eb4f1dfe7ffcce9fa205f134) by guvity
- **size:** 1 files (+262/-0)
- **score -8** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Part of a fork-private passive RDP monitoring feature that does not exist in our fork and has no matching open issue.

### `f35d0a9a0f` refactor(ssh_dotnet): rename SSH_DotNet enum value + namespace/folder to SshDotNet

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f35d0a9a0fb10aec84f73f53bc290991a2973ffe) by Dawie Joubert
- **size:** 20 files (+44/-44)
- **score -8** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Refactors the unreleased SshDotNet protocol, which does not exist in our fork (we use PuTTY for SSH). No target exists.

### `f865e545b3` feat(phase-3): Protocol replacement layer — SSH, Telnet, RDP, VNC, HTTP, PowerShell, Serial, ExternalApp

- **fork:** [Morgadoo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f865e545b3142d9fb6de529a2965254da651a469) by Claude
- **size:** 24 files (+2926/-109)
- **score -8** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Part of an experimental Avalonia UI cross-platform rewrite; completely incompatible with our WinForms codebase.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG.Avalonia/mRemoteNG.Avalonia.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `process-exec` (critical) in `mRemoteNG.Protocols/External/ExternalAppProtocol.cs` - added code spawns a process or evaluates a string as code
  - `process-exec` (critical) in `mRemoteNG.Protocols/Rdp/RdpProtocol.cs` - added code spawns a process or evaluates a string as code
  - `process-exec` (critical) in `mRemoteNG.Protocols/Shell/PowerShellProtocol.cs` - added code spawns a process or evaluates a string as code
  - `network-download` (critical) in `mRemoteNG.Protocols/Ssh/SftpBrowser.cs` - added code fetches remote content at build or run time
  - `dependency-manifest` (high) in `mRemoteNG.Protocols/mRemoteNG.Protocols.csproj` - a new or repointed package can pull arbitrary code at restore time

### `fb8e2107fa` feat(rdp): ViewOnly policy per checklist (fullscreen=work, exit/reconnect/tab=VO)

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/fb8e2107fa414423fe94b619e01723e8d949f129) by Claude Code
- **size:** 2 files (+26/-16)
- **score -8** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Bespoke A4 policy depends on an absent, still-incomplete passive-RDP state machine; no tracker demand justifies surprising automatic ViewOnly transitions or a risky redesign.

### `fc51c59781` manually added cefsharp dependencies back to project

- **fork:** [changsongyang/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/fc51c5978156146be09a9b86cc5ce1f8c7a5c4c6) by Faryan Rezagholi
- **size:** 2 files (+34/-16)
- **score -8** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Re-adds CefSharp 81 (2020, vulnerable) to legacy csproj format. Our .NET 10 fork dropped CefSharp; sln/csproj diverged completely.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `fcae38c793` Synced from my lab

- **fork:** [CancanTang/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/fcae38c793179fa9493947387fad03c4fc48286f) by CancanTang
- **size:** 300 files (+72803/-0)
- **score -8** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** 300-file bulk dump of stale 1.78.x tree with binary artifacts and critical security flags. Not a reviewable change; nothing importable.
- **security flags:**
  - `process-exec` (critical) in `CHANGELOG.md` - added code spawns a process or evaluates a string as code
  - `license` (medium) in `COPYING.txt` - licence edits change redistribution terms
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `opaque-file` (high) in `ObjectListView/Implementation/TreeDataSourceAdapter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Implementation/VirtualGroups.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Implementation/VirtualListDataSource.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/OLVColumn.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/ObjectListView.DesignTime.cs` - added file has no reviewable text diff
  - `dependency-manifest` (high) in `ObjectListView/ObjectListView.NetCore.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `opaque-file` (high) in `ObjectListView/ObjectListView.NetCore.csproj` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/ObjectListView.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Package.nuspec` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Properties/AssemblyInfo.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Properties/Resources.Designer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Properties/Resources.resx` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Rendering/Adornments.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Rendering/Decorations.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Rendering/Overlays.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Rendering/Renderers.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Rendering/Styles.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Rendering/TreeRenderer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Resources/clear-filter.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Resources/coffee.jpg` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Resources/filter-icons3.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Resources/filter.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Resources/sort-ascending.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Resources/sort-descending.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/SubControls/GlassPanelForm.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/SubControls/HeaderControl.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/SubControls/ToolStripCheckedListBox.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/SubControls/ToolTipControl.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/TreeListView.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Utilities/ColumnSelectionForm.Designer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Utilities/ColumnSelectionForm.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Utilities/ColumnSelectionForm.resx` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Utilities/Generator.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Utilities/OLVExporter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Utilities/TypedObjectListView.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/VirtualObjectListView.cs` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/CreateBulkConnections_ConfCons2_6.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/CreateBulkConnections_ConfCons2_6.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/create_upg_chk_files.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/create_upg_chk_files.ps1` - added file has no reviewable text diff
  - `security-code` (high) in `Tools/decrypt.bat` - credential and crypto paths need human review regardless of intent
  - `build-script` (high) in `Tools/decrypt.bat` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/decrypt.bat` - added file has no reviewable text diff
  - `security-code` (high) in `Tools/encrypt.bat` - credential and crypto paths need human review regardless of intent
  - `build-script` (high) in `Tools/encrypt.bat` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/encrypt.bat` - added file has no reviewable text diff
  - `binary-artifact` (critical) in `Tools/exes/dumpbin.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/editbin.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/link.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/mspdbcore.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/sigcheck.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `build-script` (high) in `Tools/find_vstool.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/find_vstool.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/github_functions.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/github_functions.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/postbuild.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/postbuild.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/postbuild_installer.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/postbuild_installer.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/postbuild_portable.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/postbuild_portable.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/publish_draft_github_release.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/publish_draft_github_release.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/publish_to_github.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/publish_to_github.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/rename_and_copy_installer.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/rename_and_copy_installer.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/set_LargeAddressAware.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/set_LargeAddressAware.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/sign_binaries.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/sign_binaries.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/signfiles.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/signfiles.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/tidy_files_for_release.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/tidy_files_for_release.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/update_and_upload_assemblyinfocs.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/update_and_upload_assemblyinfocs.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/update_and_upload_website_release_json_file.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/update_and_upload_website_release_json_file.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/validate_microsoft_tool.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/validate_microsoft_tool.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/verify_LargeAddressAware.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/verify_LargeAddressAware.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/verify_binary_signatures.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/verify_binary_signatures.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/zip_files.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/zip_files.ps1` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Import/PuttyConnectionManagerImporter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Import/RegistryImporter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Import/RemoteDesktopConnectionImporter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Import/RemoteDesktopConnectionManagerImporter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Import/RemoteDesktopManagerImporter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Import/SecureCRTImporter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Putty/AbstractPuttySessionsProvider.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Putty/PuttySessionChangedEventArgs.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Putty/PuttySessionsManager.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Putty/PuttySessionsRegistryProvider.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConfConsEnsureConnectionsHaveIds.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Csv/CsvConnectionsDeserializerMremotengFormat.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Csv/CsvConnectionsSerializerMremotengFormat.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Csv/RemoteDesktopManager/CsvConnectionsDeserializerRdmFormat.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Csv/RemoteDesktopManager/CsvConnectionsSerializerRdmFormat.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Sql/DataTableDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Sql/DataTableSerializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Sql/LocalConnectionPropertiesModel.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Sql/LocalConnectionPropertiesXmlSerializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Sql/SqlConnectionListMetaData.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Sql/SqlDatabaseMetaDataRetriever.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionNodeSerializer26.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionNodeSerializer27.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionNodeSerializer28.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionSerializerFactory.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsDocumentCompiler.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsDocumentEncryptor.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsDocumentEncryptor.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsSerializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlExtensions.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlRootNodeSerializer.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialProviderSerializer/CredentialRepositoryListDeserializer.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/CredentialProviderSerializer/CredentialRepositoryListDeserializer.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialProviderSerializer/CredentialRepositoryListSerializer.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/CredentialProviderSerializer/CredentialRepositoryListSerializer.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialPasswordDecryptorDecorator.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialPasswordDecryptorDecorator.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialPasswordEncryptorDecorator.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialPasswordEncryptorDecorator.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialRecordDeserializer.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialRecordDeserializer.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialRecordSerializer.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialRecordSerializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/IDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ISecureDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ISecureSerializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ISerializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/MiscSerializers/ActiveDirectoryDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/MiscSerializers/PortScanDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/MiscSerializers/PuttyConnectionManagerDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/MiscSerializers/RemoteDesktopConnectionDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/MiscSerializers/RemoteDesktopConnectionManagerDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/MiscSerializers/SecureCRTFileDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/IVersionUpgrader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/SqlDatabaseVersionVerifier.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/SqlVersion22To23Upgrader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/SqlVersion23To24Upgrader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/SqlVersion24To25Upgrader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/SqlVersion25To26Upgrader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/SqlVersion26To27Upgrader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/SqlVersion27To28Upgrader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/SqlVersion28To29Upgrader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/SqlVersion29To30Upgrader.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/Serializers/XmlConnectionsDecryptor.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/XmlConnectionsDecryptor.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/DockPanelLayoutLoader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/DockPanelLayoutSaver.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/DockPanelLayoutSerializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/ExternalAppsLoader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/ExternalAppsSaver.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/LocalSettingsManager.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Providers/ChooseProvider.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Providers/PortableSettingsProvider.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/CommonRegistrySettings.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryAppearancePage.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryConnectionsPage.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryCredentialsPage.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryCredentialsPage.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryNotificationsPage.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistrySecurityPage.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistrySqlServerPage.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryStartupExitPage.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryTabsPanelsPage.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryUpdatesPage.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/RegistryLoader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Settings.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/SettingsLoader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/SettingsSaver.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/AbstractConnectionRecord.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/ConnectionFrameColor.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/ConnectionIcon.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/ConnectionInfo.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/ConnectionInfoComparer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/ConnectionInfoInheritance.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/ConnectionInitiator.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/ConnectionsService.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Converter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/DefaultConnectionInfo.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/DefaultConnectionInheritance.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/ExternalAddressProviderSelector.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Connection/ExternalCredentialProviderSelector.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Connection/ExternalCredentialProviderSelector.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/IConnectionInitiator.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/IHasParent.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/IInheritable.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/InterfaceControl.Designer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/InterfaceControl.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/ARD/ProtocolARD.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/AnyDesk/ProtocolAnyDesk.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/Http/Connection.Protocol.HTTP.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/Http/Connection.Protocol.HTTPBase.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/Http/Connection.Protocol.HTTPS.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/ISupportsViewOnly.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/IntegratedProgram.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/PowerShell/Connection.Protocol.PowerShell.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/ProtocolBase.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/ProtocolFactory.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/ProtocolList.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/ProtocolType.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/PuttyBase.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RAW/RawProtocol.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/AuthenticationLevel.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/AzureLoadBalanceInfoEncoder.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RDGatewayUsageMethod.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Connection/Protocol/RDP/RDGatewayUseConnectionCredentials.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RDGatewayUseConnectionCredentials.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RDPColors.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RDPDiskDrives.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RDPPerformanceFlags.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RDPResolutions.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RDPSoundQuality.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RDPSounds.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RdGatewayAccessTokenHelper.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RdpErrorCodes.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RdpExtensions.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RdpNetworkConnectionType.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RdpProtocol.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RdpProtocol10.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RdpProtocol11.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RdpProtocol7.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RdpProtocol8.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RdpProtocol9.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RdpProtocolFactory.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/RDP/RdpVersion.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/Rlogin/Connection.Protocol.Rlogin.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/SSH/Connection.Protocol.SSH1.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/SSH/Connection.Protocol.SSH2.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/Serial/Connection.Protocol.Serial.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/Telnet/Connection.Protocol.Telnet.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/Terminal/Connection.Protocol.Terminal.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/VNC/Connection.Protocol.VNC.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/VNC/VNCEnum.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/WSL/Connection.Protocol.WSL.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/PuttySessionInfo.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/VaultOpenbaoSecretEngine.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/WebHelper.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Container/ContainerInfo.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/CredentialChangedEventArgs.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/CredentialChangedEventArgs.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/CredentialDeletionMsgBoxConfirmer.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/CredentialDeletionMsgBoxConfirmer.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/CredentialDomainUserComparer.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/CredentialDomainUserComparer.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/CredentialInfo.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/CredentialInfo.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/CredentialRecord.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/CredentialRecord.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/CredentialRecordTypeConverter.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/CredentialRecordTypeConverter.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/CredentialServiceFacade.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/CredentialServiceFacade.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/CredentialServiceFactory.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/CredentialServiceFactory.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/ICredentialRecord.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/ICredentialRecord.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/ICredentialRepository.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/ICredentialRepository.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/ICredentialRepositoryList.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/ICredentialRepositoryList.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/PlaceholderCredentialRecord.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/PlaceholderCredentialRecord.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/CompositeRepositoryUnlocker.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/Repositories/CompositeRepositoryUnlocker.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/CredentialRepoUnlockerBuilder.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/Repositories/CredentialRepoUnlockerBuilder.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/CredentialRepositoryChangedArgs.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/Repositories/CredentialRepositoryChangedArgs.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/CredentialRepositoryConfig.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/Repositories/CredentialRepositoryConfig.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/CredentialRepositoryList.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/Repositories/CredentialRepositoryList.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/ICredentialRepositoryConfig.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/Repositories/ICredentialRepositoryConfig.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/XmlCredentialRepository.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/Repositories/XmlCredentialRepository.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/XmlCredentialRepositoryFactory.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Credential/Repositories/XmlCredentialRepositoryFactory.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/Admin.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/Anti Virus.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/Apple.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/Backup.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/Build Server.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/Console.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/Database.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/Domain Controller.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/ESX.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/Fax.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/File Server.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/Finance.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/Firewall.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/Infrastructure.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/Kvark pack/sql-server.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/Linux.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/Log.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/Mail Server.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/PowerShell.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/Production.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/PuTTY.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/RaspberryPi.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/Remote Desktop.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/Router.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Icons/SSH.ico` - added file has no reviewable text diff

### `d93fbceeb4` feat: passive RDP monitor for 1.77.2-release (focus suppress + ViewOnly only in fullscreen + scroll bottom-right)

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/d93fbceeb45664868818f1d955ae6441701715b2) by guvity
- **size:** 1 files (+76/-5)
- **score -9** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 5 | risk 5 | applies rewrite | REJECT
- **our issue:** #118
- **why:** Issue #118 focus is already fixed precisely; current IMessageFilter ViewOnly supersedes this flags-only regression. Forced scrolling and global focus suppression are unwanted legacy behavior.

### `faf0f4dd0b` Catch error in RDP tab focus

- **fork:** [azet/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/faf0f4dd0b1ed2e5c2c680e63c4c982fc603839e) by Camilo Alvarez
- **size:** 1 files (+10/-1)
- **score -9** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 4 | risk 4 | applies rewrite | REJECT
- **why:** Superseded: current handler safely pattern-matches the tab and updates tracking without stealing RDP focus; the old broad catch masks faults and targets removed code.

### `df26e434d5` feat(ssh_dotnet): wire private-key authentication (key-first, passphrase, clear errors)

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/df26e434d5eb0c8b3698f99347879bc1d548f5e1) by Dawie Joubert
- **size:** 3 files (+31/-23)
- **score -10** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 4 | applies rewrite | REJECT
- **why:** Private-key SSH is already supported through PrivateKeyPath on PuTTY/OpenSSH; this patch depends on an absent SSH.NET protocol and incompatible properties.

### `fc2d3bb02a` reduced about windows to a simple form

- **fork:** [stdexception/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/fc2d3bb02a08915d865c582baf72572aa0be6866) by Faryan Rezagholi
- **size:** 12 files (+178/-257)
- **score -10** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 4 | applies rewrite | REJECT
- **why:** A later same-author About-screen refactor is already ancestral; current frmAbout uses link labels and Markdig is gone, so this earlier 2020 iteration is fully superseded.
- **security flags:**
  - `process-exec` (critical) in `mRemoteV1/UI/Forms/FrmAbout.cs` - added code spawns a process or evaluates a string as code
  - `dependency-manifest` (high) in `mRemoteV1/mRemoteV1.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteV1/packages.config` - a new or repointed package can pull arbitrary code at restore time

### `18c26d1c33` now uses mariadb and shared user-config

- **fork:** [hthvdmeer/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/18c26d1c33690db02d80f1d7919d5b6acdfae902) by takemaker63
- **size:** 24 files (+883/-77)
- **score -11** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 4 | risk 5 | applies rewrite | REJECT
- **our issue:** #145
- **why:** Personal-environment dump: committed root password, temp logs, private CLAUDE.md. MariaDB/SQL fixes already covered by our #145-#148 series.
- **security flags:**
  - `opaque-file` (high) in `CTempmremote_stderr.txt` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/ConvertXmlToSql.ps1` - scripts execute on a maintainer machine
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `00ad7850b2` Fix RDP fullscreen exit finalizer

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/00ad7850b237dfe6efd1df3fab52699a715bd4a7) by guvity
- **size:** 1 files (+203/-1)
- **score -12** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Fullscreen-exit refocus already exists in RdpProtocol; RdpProtocol6 was deleted. Passive-only timers, HWND-wide focus messages, and ViewOnly changes add risk without matching an open issue.

### `0a998eb5f3` Adding more awesome to build process and documentation

- **fork:** [savornicesei/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/0a998eb5f37d695a15a0f63122941efc993b079f) by Simona Avornicesei
- **size:** 143 files (+651/-226)
- **score -12** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** 1.77-era .NET 6 build overhaul with committed nuget/vswhere exes. Our fork already has superior .NET 10 build.ps1 + Directory.Build.props + analyzers.
- **security flags:**
  - `dependency-manifest` (high) in `ExternalConnectors/ExternalConnectors.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `binary-artifact` (critical) in `Tools/exes/nuget.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/vswhere.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `build-script` (high) in `build.ps1` - scripts execute on a maintainer machine
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNGDocumentation/mRemoteNG.Docs.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `mRemoteNGSpecs/Features/CredentialRepository.feature.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGSpecs/Features/CredentialRepositoryList.feature.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNGSpecs/mRemoteNGSpecs.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNGTests/mRemoteNGTests.csproj` - a new or repointed package can pull arbitrary code at restore time

### `14041a13c9` moved to .net core and fixed all compiler errors

- **fork:** [changsongyang/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/14041a13c9e7f0f0f78519c74e6270ded48ed78c) by Faryan Rezagholi
- **size:** 9 files (+2245/-1373)
- **score -12** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Early .NET Core migration attempt. Our fork fully migrated to .NET 10 SDK-style projects long ago; entirely superseded.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj.old` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNGSpecs/mRemoteNGSpecs.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNGSpecs/mRemoteNGSpecs.csproj.old` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNGTests/mRemoteNGTests.csproj` - a new or repointed package can pull arbitrary code at restore time

### `33fc930f80` removed gecko and ie rendering engines

- **fork:** [stdexception/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/33fc930f809a3fd18da9acf88ac9470e5dff0fc8) by Faryan Rezagholi
- **size:** 60 files (+3946/-6217)
- **score -12** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** We already handle removed Gecko (#113 TryParse fix) and keep RenderingEngine columns intentionally for schema/CSV compat; 60-file removal on mRemoteV1 tree would break our migrations.
- **security flags:**
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/AccessibleHandler.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/AccessibleMarshal.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/IA2Marshal.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/breakpadinjector.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/d3dcompiler_47.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/freebl3.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/lgpllibs.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/libEGL.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/libGLESv2.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/mozavcodec.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/mozavutil.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/mozglue.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/nss3.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/nssckbi.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/nssdbm3.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/plugin-container.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/plugin-hang-ui.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/qipcap.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/softokn3.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `mRemoteV1/Firefox/xul.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `dependency-manifest` (high) in `mRemoteV1/mRemoteV1.csproj` - a new or repointed package can pull arbitrary code at restore time

### `390ec3e076` added postbuild cleanup script

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/390ec3e076cf1343ae8ec7ebcc358cf1fb5fb13e) by Faryan Rezagholi
- **size:** 3 files (+35/-17)
- **score -12** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Superseded by the SDK publish pipeline; deleting root DLL/JSON files and probing only lang\de would break modern runtime and localization loading.
- **security flags:**
  - `build-script` (high) in `Tools/clean_ouput_dir.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `Tools/postbuild_mremoteng.ps1` - scripts execute on a maintainer machine

### `7d7abffdd4` upload code from lab

- **fork:** [appcompat-wx/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7d7abffdd44ee46a34245780cbc1c49969b77055) by appcompat-wx
- **size:** 300 files (+202326/-0)
- **score -12** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 5 | applies conflict | REJECT
- **why:** Wholesale legacy tree upload including stale workflows and binaries; our evolved .NET 10 fork already contains and supersedes its baseline, with severe conflicts.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `env-secret-access` (critical) in `.github/workflows/Build_mR-NB.yml` - added code reads credentials or CI secrets
  - `ci-workflow` (critical) in `.github/workflows/add_PR_2_chlog.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `ci-workflow` (critical) in `.github/workflows/post_2_Reddit.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `network-download` (critical) in `.github/workflows/post_2_Reddit.yml` - added code fetches remote content at build or run time
  - `env-secret-access` (critical) in `.github/workflows/post_2_Reddit.yml` - added code reads credentials or CI secrets
  - `process-exec` (critical) in `CHANGELOG.md` - added code spawns a process or evaluates a string as code
  - `license` (medium) in `COPYING.txt` - licence edits change redistribution terms
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `opaque-file` (high) in `ExternalConnectors/CPS/CPS.ico` - added file has no reviewable text diff
  - `security-code` (high) in `ExternalConnectors/CPS/PasswordstateInterface.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `ExternalConnectors/DSS/DSS.ico` - added file has no reviewable text diff
  - `opaque-file` (high) in `ExternalConnectors/DSS/SecretServerRestClient.cs` - added file has no reviewable text diff
  - `dependency-manifest` (high) in `ExternalConnectors/ExternalConnectors.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `ExternalConnectors/OP/OnePasswordCli.cs` - credential and crypto paths need human review regardless of intent
  - `process-exec` (critical) in `ExternalConnectors/OP/OnePasswordCli.cs` - added code spawns a process or evaluates a string as code
  - `opaque-file` (high) in `ObjectListView/Implementation/GroupingParameters.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Implementation/Groups.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Implementation/Munger.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Implementation/NativeMethods.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Implementation/NullableDictionary.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Implementation/OLVListItem.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Implementation/OLVListSubItem.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Implementation/OlvListViewHitTestInfo.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Implementation/TreeDataSourceAdapter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Implementation/VirtualGroups.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Implementation/VirtualListDataSource.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/OLVColumn.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/ObjectListView.DesignTime.cs` - added file has no reviewable text diff
  - `dependency-manifest` (high) in `ObjectListView/ObjectListView.NetCore.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `opaque-file` (high) in `ObjectListView/ObjectListView.NetCore.csproj` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/ObjectListView.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Package.nuspec` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Properties/AssemblyInfo.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Properties/Resources.Designer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Properties/Resources.resx` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Rendering/Adornments.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Rendering/Decorations.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Rendering/Overlays.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Rendering/Renderers.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Rendering/Styles.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Rendering/TreeRenderer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Resources/clear-filter.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Resources/coffee.jpg` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Resources/filter-icons3.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Resources/filter.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Resources/sort-ascending.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Resources/sort-descending.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/SubControls/GlassPanelForm.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/SubControls/HeaderControl.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/SubControls/ToolStripCheckedListBox.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/SubControls/ToolTipControl.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/TreeListView.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Utilities/ColumnSelectionForm.Designer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Utilities/ColumnSelectionForm.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Utilities/ColumnSelectionForm.resx` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Utilities/Generator.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Utilities/OLVExporter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/Utilities/TypedObjectListView.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `ObjectListView/VirtualObjectListView.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `PANEL_BINDING_FEATURE.md` - added file has no reviewable text diff
  - `opaque-file` (high) in `README.md` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/CreateBulkConnections_ConfCons2_6.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/CreateBulkConnections_ConfCons2_6.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/create_upg_chk_files.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/create_upg_chk_files.ps1` - added file has no reviewable text diff
  - `security-code` (high) in `Tools/decrypt.bat` - credential and crypto paths need human review regardless of intent
  - `build-script` (high) in `Tools/decrypt.bat` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/decrypt.bat` - added file has no reviewable text diff
  - `security-code` (high) in `Tools/encrypt.bat` - credential and crypto paths need human review regardless of intent
  - `build-script` (high) in `Tools/encrypt.bat` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/encrypt.bat` - added file has no reviewable text diff
  - `binary-artifact` (critical) in `Tools/exes/dumpbin.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/editbin.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/link.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/mspdbcore.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `binary-artifact` (critical) in `Tools/exes/sigcheck.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `build-script` (high) in `Tools/find_vstool.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/find_vstool.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/github_functions.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/github_functions.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/postbuild.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/postbuild.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/postbuild_installer.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/postbuild_installer.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/postbuild_portable.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/postbuild_portable.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/publish_draft_github_release.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/publish_draft_github_release.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/publish_to_github.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/publish_to_github.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/rename_and_copy_installer.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/rename_and_copy_installer.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/set_LargeAddressAware.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/set_LargeAddressAware.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/sign_binaries.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/sign_binaries.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/signfiles.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/signfiles.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/tidy_files_for_release.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/tidy_files_for_release.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/update_and_upload_assemblyinfocs.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/update_and_upload_assemblyinfocs.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/update_and_upload_website_release_json_file.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/update_and_upload_website_release_json_file.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/validate_microsoft_tool.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/validate_microsoft_tool.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/verify_LargeAddressAware.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/verify_LargeAddressAware.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/verify_binary_signatures.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/verify_binary_signatures.ps1` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/zip_files.ps1` - scripts execute on a maintainer machine
  - `opaque-file` (high) in `Tools/zip_files.ps1` - added file has no reviewable text diff
  - `opaque-file` (high) in `VISUAL_EXAMPLES.md` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.lutconfig` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG.sln` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/.editorconfig` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App.config` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/AppWindows.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Checks/AppUpdater.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Checks/DotNetRuntimeCheck.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Checks/InternetConnection.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Checks/UpdateFile.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Checks/UpdateInfo.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Checks/VCppRuntimeCheck.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/CompatibilityChecker.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Export.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Import.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Info/ConnectionsFileInfo.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/App/Info/CredentialsFileInfo.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/App/Info/CredentialsFileInfo.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Info/GeneralAppInfo.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Info/SettingsFileInfo.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Info/UpdateChannelInfo.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Info/WindowsRegistryInfo.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Initialization/ConnectionIconLoader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Initialization/CredsAndConsSetup.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Initialization/MessageCollectorSetup.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Initialization/StartupDataLogger.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Logger.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/NativeMethods.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/ProgramRoot.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Runtime.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Screens.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Shutdown.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/Startup.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/App/SupportedCultures.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/ACLPermissions.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/ConfirmCloseEnum.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Connections/ConnectionsBackupFrequencyEnum.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Connections/ConnectionsLoadedEventArgs.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Connections/ConnectionsSavedEventArgs.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Connections/CsvConnectionsSaver.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Connections/IConnectionsLoader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Connections/Multiuser/ConnectionsUpdateAvailableEventArgs.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Connections/Multiuser/ConnectionsUpdateCheckFinishedEventArgs.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Connections/Multiuser/IConnectionsUpdateChecker.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Connections/Multiuser/RemoteConnectionsSyncronizer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Connections/Multiuser/SqlConnectionsUpdateChecker.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Connections/SaveConnectionsOnEdit.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Connections/SaveFormat.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Connections/SqlConnectionsLoader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Connections/SqlConnectionsSaver.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Connections/XmlConnectionsLoader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Connections/XmlConnectionsSaver.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/CredentialHarvester.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/CredentialHarvester.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/CredentialRecordLoader.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/CredentialRecordLoader.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/CredentialRecordSaver.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/CredentialRecordSaver.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/CredentialRepositoryListLoader.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/CredentialRepositoryListLoader.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/CredentialRepositoryListSaver.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/CredentialRepositoryListSaver.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/DataProviders/FileBackupCreator.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/DataProviders/FileBackupPruner.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/DataProviders/FileDataProvider.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/DataProviders/FileDataProviderWithRollingBackup.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/DataProviders/IDataProvider.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/DataProviders/InMemoryStringDataProvider.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/DataProviders/SqlDataProvider.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/DatabaseConnectors/ConnectionTestResult.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/DatabaseConnectors/DatabaseConnectionTester.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/DatabaseConnectors/DatabaseConnectorFactory.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/DatabaseConnectors/IDatabaseConnector.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/DatabaseConnectors/MSSqlDatabaseConnector.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/DatabaseConnectors/MySqlDatabaseConnector.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/ILoader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/ISaver.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Import/ActiveDirectoryImporter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Import/IConnectionImporter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Import/MRemoteNGCsvImporter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Import/MRemoteNGXmlImporter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Import/PortScanImporter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Import/PuttyConnectionManagerImporter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Import/RegistryImporter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Import/RemoteDesktopConnectionImporter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Import/RemoteDesktopConnectionManagerImporter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Import/RemoteDesktopManagerImporter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Import/SecureCRTImporter.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/MachineIdentifier.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Putty/AbstractPuttySessionsProvider.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Putty/PuttySessionChangedEventArgs.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Putty/PuttySessionsManager.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Putty/PuttySessionsRegistryProvider.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConfConsEnsureConnectionsHaveIds.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Csv/CsvConnectionsDeserializerMremotengFormat.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Csv/CsvConnectionsSerializerMremotengFormat.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Csv/RemoteDesktopManager/CsvConnectionsDeserializerRdmFormat.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Csv/RemoteDesktopManager/CsvConnectionsSerializerRdmFormat.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Sql/DataTableDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Sql/DataTableSerializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Sql/LocalConnectionPropertiesModel.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Sql/LocalConnectionPropertiesXmlSerializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Sql/SqlConnectionListMetaData.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Sql/SqlDatabaseMetaDataRetriever.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionNodeSerializer26.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionNodeSerializer27.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionNodeSerializer28.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionSerializerFactory.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsDocumentCompiler.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsDocumentEncryptor.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsDocumentEncryptor.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsSerializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlExtensions.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlRootNodeSerializer.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialProviderSerializer/CredentialRepositoryListDeserializer.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/CredentialProviderSerializer/CredentialRepositoryListDeserializer.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialProviderSerializer/CredentialRepositoryListSerializer.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/CredentialProviderSerializer/CredentialRepositoryListSerializer.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialPasswordDecryptorDecorator.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialPasswordDecryptorDecorator.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialPasswordEncryptorDecorator.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialPasswordEncryptorDecorator.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialRecordDeserializer.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialRecordDeserializer.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialRecordSerializer.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialRecordSerializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/IDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ISecureDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ISecureSerializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/ISerializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/MiscSerializers/ActiveDirectoryDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/MiscSerializers/PortScanDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/MiscSerializers/PuttyConnectionManagerDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/MiscSerializers/RemoteDesktopConnectionDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/MiscSerializers/RemoteDesktopConnectionManagerDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/MiscSerializers/SecureCRTFileDeserializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/IVersionUpgrader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/SqlDatabaseVersionVerifier.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/SqlVersion22To23Upgrader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/SqlVersion23To24Upgrader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/SqlVersion24To25Upgrader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/SqlVersion25To26Upgrader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/SqlVersion26To27Upgrader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/SqlVersion27To28Upgrader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/SqlVersion28To29Upgrader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/Versioning/SqlVersion29To30Upgrader.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/Serializers/XmlConnectionsDecryptor.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/Serializers/XmlConnectionsDecryptor.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/DockPanelLayoutLoader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/DockPanelLayoutSaver.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/DockPanelLayoutSerializer.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/ExternalAppsLoader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/ExternalAppsSaver.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/LocalSettingsManager.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Providers/ChooseProvider.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Providers/PortableSettingsProvider.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/CommonRegistrySettings.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryAppearancePage.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryConnectionsPage.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryCredentialsPage.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryCredentialsPage.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryNotificationsPage.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistrySecurityPage.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistrySqlServerPage.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryStartupExitPage.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryTabsPanelsPage.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryUpdatesPage.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Registry/RegistryLoader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/Settings.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/SettingsLoader.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Config/Settings/SettingsSaver.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/AbstractConnectionRecord.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/ConnectionFrameColor.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/ConnectionIcon.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/ConnectionInfo.cs` - added file has no reviewable text diff
  - `opaque-file` (high) in `mRemoteNG/Connection/ConnectionInfoComparer.cs` - added file has no reviewable text diff

### `e2daf1270f` Use v2 passive RDP patcher for 1.77.2 release

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/e2daf1270f34e569981ef5e2f01babc293f38832) by guvity
- **size:** 4 files (+148/-488)
- **score -12** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Native RDP view-only already blocks input via IMessageFilter; this obsolete 1.77.2 patcher adds forced fullscreen/focus/scroll behavior that would regress normal sessions.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-1772-build.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `build-script` (high) in `Tools/passive-rdp-monitor-1772-v2.ps1` - scripts execute on a maintainer machine

---

Generated by `.project-roadmap/fork-intel/fork_intel.py report`. Nothing here has been imported: every entry needs a human decision.

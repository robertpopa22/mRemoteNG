# Import Queue

Generated 2026-07-26 from the fork radar. **Nothing is applied automatically.**

Both mRemoteNG and its forks are GPL-2.0, so importing is licence-compatible. `git cherry-pick` preserves the original author and `-x` records the source commit; add a `Ported-from:` trailer with the upstream URL so the origin stays visible.

After a decision, record it so future runs stop proposing it:

```bash
python .project-roadmap/fork-intel/fork_intel.py mark --sha <sha> --decision imported|rejected|deferred --note "why"
```

## Tier A - ready to cherry-pick

### `7349e5a6aa` Fix main window stuck behind other windows after startup - needs manual review

Removes redundant window activation code that causes inconsistent WinForms state when blocked by Windows on startup. Highly beneficial UX fix.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/7349e5a6aa3b85440a6f934e5269555c476fbb04

Counter-opinions: codex **REJECT** / grok **REJECT**
- codex: REJECT - The diagnosis is unproven here, the lifecycle diverged after the shared code, and no tests or reproduction justify deleting a deliberate focus safeguard.
- grok: REJECT - Diff contradicts claimed fix; startup focus is delicate and unverified here.

```bash
git remote add fi-k-meeks https://github.com/k-meeks/mRemoteNG.git
git fetch fi-k-meeks --depth=50 7349e5a6aa3b85440a6f934e5269555c476fbb04
git cherry-pick -x 7349e5a6aa3b85440a6f934e5269555c476fbb04
# then: build.ps1 + run-tests.ps1 -Headless before committing anything
```

## Tier B - worth porting by hand

### `dd54616a2e` Fix NullReferenceException + recursive dialog cascade on failed decrypt - needs manual review

Our XML null guard already prevents the NRE, but still throws into Runtime’s recursive reload path; adapt the null-return behavior to current nullable code.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/dd54616a2e47bdb94e18b2fbafbd2a30764a3728

Counter-opinions: codex **REJECT** / grok **APPROVE** / claude **REJECT**
- codex: REJECT - This fork already prevents the null dereference and duplicate file dialog through guarded validation and explicit-file loading; importing this patch is redundant and regressive.
- claude: REJECT - Fork already prevents the crash differently; null-return would only slightly change which error dialog shows for legacy decrypt cancel — marginal value.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `eb03e059b2` Add configurable interface font (Options > Appearance) - needs manual review

Adds a highly useful, clean accessibility feature allowing user-customized interface fonts without restarting. Worth importing.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/eb03e059b2ecc1a1b00dc70056b70cdb348a2195

Counter-opinions: codex **REJECT** / grok **NEEDS_HUMAN**
- codex: REJECT - The accessibility idea is useful, but this untested global override conflicts with existing font behavior and requires target-specific redesign, not direct import.
- grok: NEEDS_HUMAN - Nice accessibility tweak, but side effects on panels/DPI and leaks need maintainer review first.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `0d8b8f6c56` Add "Copy All to Clipboard" to PuTTY connection tab context menu - needs manual review

The backend exists, but the requested tab action does not. Add only UI wiring using the existing method and resource, avoiding duplicate backend and localization.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/0d8b8f6c56485861217abdc30a25ae0420827ccf

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - Menu exposure is useful, but only its UI wiring should be reimplemented against the existing method; this commit is not directly landable.
- gemini: REJECT - The backend method already exists in our fork. This commit would cause merge conflicts and code duplication, requiring a clean manual reimplementation.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `2d1411667e` 修复：容器的ID现保持与文件中一致 - needs manual review

Current XML loading discards serialized container IDs because CopyFrom cannot set get-only ConstantID. Reimplement constructor-based preservation with malformed-ID and round-trip tests.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/2d1411667e60c4e001933d60f8de825dd2ac9213

Counter-opinions: codex **NEEDS_HUMAN** / grok **NEEDS_HUMAN**
- codex: NEEDS_HUMAN - The defect is real and absent here, but land a tested fork-aware reimplementation covering Container and Entity instead of this stale patch.
- grok: NEEDS_HUMAN - Real container ID-stability fix, but needs clean reimplementation and fork check.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `3f94a2c239` Dark mode: follow the OS, honor the theming setting, dark title bars - needs manual review

Follow-OS dark mode + DWM dark title bars addresses open #47. Clean idea, but flips ThemingActive default and our ThemeManager/settings diverged; re-derive carefully.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/3f94a2c23980a384cbf15386ae7ffc506a92e6e5

Counter-opinions: codex **REJECT** / gemini **NEEDS_HUMAN**
- codex: REJECT - OS matching is valuable, but this untested patch conflicts with live-switch and high-contrast theming, assumes restart-only behavior, and requires a scoped reimplementation.
- gemini: NEEDS_HUMAN - Valuable dark mode UX improvements matching modern Windows settings, but requires careful refactoring of settings and ThemeManager initialization to prevent regressions.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `9c4b85f18a` fix: set temp key-file attribute via File.SetAttributes - needs manual review

Replaces redundant throwaway FileInfo instantiation with clean, direct File.SetAttributes call in two PuttyBase temp key generation paths.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/9c4b85f18ab51f04b455d198cbf86b284dd6c3f8

Counter-opinions: codex **REJECT** / grok **REJECT**
- codex: REJECT - It provides no correctness or stability gain; reimplementation would be churn because both APIs set the same attribute and current code has zero warnings.
- grok: REJECT - Original object-initializer already sets attributes on disk; pure idiom tweak, not a real fix.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `a677fae337` Fix ObjectDisposedException when closing a connection tab - needs manual review

Closes TOCTOU race in Prot_Event_Closed Invoke; our guards (IsDisposed check) exist but not the try/catch + marshaled re-check. Small defensive win; code diverged.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/a677fae337a8c49890c6a0e2d87b9739d708d25d

Counter-opinions: codex **REJECT** / grok **APPROVE** / claude **REJECT**
- codex: REJECT - Current HandleProtocolClosed already has stronger handle, marshaling, disposal-race, and close guards, so this commit offers no unique value and conflicts with intentional semantics.
- claude: REJECT - Fork diverged: same race already fixed better (non-blocking BeginInvoke re-marshal, ConnectionWindow.cs:2223-2246). Import adds nothing, code no longer matches.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `d500a8e9dd` CustomConsPath为相对路径时，主窗口标题也能正确显示全路径（上一提交引入） - needs manual review

Displays absolute path in main window title when loaded with relative path. Simple and safe UX bugfix, needs minor adjustment for our namespaces.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/d500a8e9dda08af453e3e69f3d891e2be4145686

Counter-opinions: codex **REJECT** / grok **NEEDS_HUMAN**
- codex: REJECT - Current paths are already normalized and CustomConsPath is unused; remaining relative inputs should be normalized at load time, not during rendering.
- grok: NEEDS_HUMAN - Small useful title fix for relative paths; confirm null safety and no local equivalent first

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `d6f4872b8b` 标签右键中增加关闭菜单 - needs manual review

Adds Close item to panel-tab context menu; minor UX win. Old mRemoteV1 paths, trivial to redo in our PanelAdder if wanted.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/d6f4872b8bd1a73e4f293a78243ed424b7347e3e

Counter-opinions: codex **REJECT** / grok **NEEDS_HUMAN**
- codex: REJECT - Importing this stale duplicate adds no capability and risks conflicts or regressions against the maintained implementation already present.
- grok: NEEDS_HUMAN - Small useful tab UX, but verify duplication and correct ConnectionWindow close semantics first.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `0045263765` Show auto-detected PuTTY path on Advanced options page - needs manual review

Small UX win: shows auto-detected PuTTY path in options. Our Designer/options pages diverged heavily; re-do by hand, not cherry-pick.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/004526376515164a858c98a9a1c782d04a28c33c

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - The fork already exposes the custom override and otherwise always launches bundled PuTTYNG.exe, so this UI is redundant, misleading, and upstream-specific.
- gemini: REJECT - Our fork bundles `PuTTYNG.exe` and has not imported the unbundling candidate. This change is redundant and will break the build due to missing auto-detection dependencies.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `8f39c112b5` fix(rdp): reapply performance flags and input finalizer on all reconnect paths - needs manual review

Reapplying performance flags on mstscax auto-reconnect is a plausible real fix, but patch depends on fork-only view-only/input-finalizer infrastructure we lack. Note idea, not code.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/8f39c112b57865efa6c34cd735c1b35394c203dc

Counter-opinions: codex **REJECT** / grok **NEEDS_HUMAN**
- codex: REJECT - Exact commit cannot land: RdpProtocol6 was deleted, passive helpers are absent, RdpProtocol8 is refactored, and no tests or reproducible evidence are provided.
- grok: NEEDS_HUMAN - Reapplying pFlags on reconnect is useful, but diff is fork-specific and needs local path checks.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `2a693c85c2` Added SSH Tunnel via SSH_DotNet - needs manual review

Native SSH.NET forwarding is potentially valuable, but this untested patch depends on an absent protocol and rewrites obsolete tunnel logic; monitor, do not port.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/2a693c85c2525ff21e0f35968f9a0745dc612022

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - It adds an untested parallel SSH stack and omits SQL/MariaDB persistence, conflicting with stability, storage consistency, and quick verification requirements.
- gemini: REJECT - We do not have the SSH_DotNet protocol implemented. Importing this will break the build and introduces excessive complexity.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `932e6f6116` Enhance connection handling and UI features - needs manual review

Mixed bag: new inheritance props (ExternalAddressProvider, RDP StartProgram, gateway token), notification detail, plus personal junk (.vscode, WorldOfFanXP.xml). Partly overlaps our upstream ports; cherry-pick only if users ask.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/932e6f611674e6227db18d977f67a1b577af25a2

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - A 732-line mixed, untested commit also bypasses notification filters, leaks writer subscriptions, duplicates shipped UI/retry features, adds untranslated labels, and uses noncanonical tooling.
- gemini: REJECT - This is a mixed bag of personal settings, French locale scripts, and features already integrated or overlapping with our upstream ports. Not suitable for import.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.


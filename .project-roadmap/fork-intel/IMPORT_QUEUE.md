# Import Queue

Generated 2026-09-30 from the fork radar. **Nothing is applied automatically.**

Both mRemoteNG and its forks are GPL-2.0, so importing is licence-compatible. `git cherry-pick` preserves the original author and `-x` records the source commit; add a `Ported-from:` trailer with the upstream URL so the origin stays visible.

After a decision, record it so future runs stop proposing it:

```bash
python .project-roadmap/fork-intel/fork_intel.py mark --sha <sha> --decision imported|rejected|deferred --note "why"
```

## Tier A - ready to cherry-pick

### `7349e5a6aa` Fix main window stuck behind other windows after startup - needs manual review

Removes redundant window activation code that causes inconsistent WinForms state when blocked by Windows on startup. Highly beneficial UX fix.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/7349e5a6aa3b85440a6f934e5269555c476fbb04

Counter-opinions: claude **REJECT** / codex **REJECT** / grok **REJECT**
- claude: REJECT - Code present in our fork and intentional; focus handling is a fragile priority area (#110/#118/#143/#168). Unproven premise, no repro — needs lab evidence before touching.
- codex: REJECT - This fork intentionally added these lines for the same symptom; deleting them without an alternative or reproduction conflicts with its current splash lifecycle.
- grok: REJECT - Those three calls were added here on purpose after splash close; importing undoes our own changelogged fix.

```bash
git remote add fi-k-meeks https://github.com/k-meeks/mRemoteNG.git
git fetch fi-k-meeks --depth=50 7349e5a6aa3b85440a6f934e5269555c476fbb04
git cherry-pick -x 7349e5a6aa3b85440a6f934e5269555c476fbb04
# then: build.ps1 + run-tests.ps1 -Headless before committing anything
```

## Tier B - worth porting by hand

### `b7c487412f` fix: restore connection tree state when the search filter is cleared - needs manual review

Our ApplyFilter/RemoveFilter still store live ExpandedObjects and skip RebuildAll; stale row map after filter clear is the same IndexOf/RedrawItems family as #149. Protected RebuildAll(IList,IEnumerable,IList) exists. Tests included.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/b7c487412fc8ee89d9001dcbfad754484a42b31b

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - The snapshot, RebuildAll restoration, and both tests already exist here in 70d545ed9; a93729afa subsequently improves RemoveFilter. This import adds no value.
- gemini: REJECT - Already merged via PR #159 (commit 70d545ed9); our tree has since added further batching and painting performance enhancements.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `c4837b6551` fix: measure task dialog text the way it is drawn, and size buttons to the client area - needs manual review

Our frmTaskDialog still uses Graphics.MeasureString and Width for command buttons (GDI+/GDI mismatch, same class as #163). Take the two code hunks; skip the resx strings, which are jafin's storage-hardening feature.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/c4837b6551b9d8fe8ffc9980159905d0442824f3

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - This fork already implements GDI text measurement and panel-relative button sizing during relayout. The storage-upgrade resource keys are absent, so this commit is not directly applicable.
- gemini: REJECT - Already implemented superior GDI TextRenderer measurement and dynamic layout in commit 35507078f; includes foreign storage upgrade strings.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

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

### `2a9a06a5c7` Translate the menus, dialogs and options pages into Hungarian - needs manual review

Our hu.resx has 88 of 910 keys; this adds 371. Drop keys we removed (update channels, e.g. AskUpdatesContent) and verify against our Language.resx before landing.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/2a9a06a5c7ba6485ca429c9da7ccee0e66831b12

Counter-opinions: codex **REJECT** / gemini **NEEDS_HUMAN**
- codex: REJECT - Adds missing localization, but the [patch](https://github.com/lovaszlaszlo/mRemoteNG/commit/2a9a06a5c7ba6485ca429c9da7ccee0e66831b12.patch) conflicts with this fork’s resource contract and mistranslates the reconnection-dialog option as automatic reconnection.
- gemini: NEEDS_HUMAN - Valuable Hungarian localization additions, but requires manual curation against Language.resx to filter dead keys and resolve resx conflicts.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `2cb2b55cc7` fix: drop the redundant panel-close prompt after a tab disconnect (#46) - needs manual review

Our Connection_FormClosing still counts connDock.Documents.Any(); HasConnectionTabs already exists so the LiveConnectionTabCount refactor drops in. Verify with AutoClosePanelOnLastTabClose UI repro.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/2cb2b55cc723d161a670d4ad41f98ad830871d4f

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - Commit 8149503ee already implements identical live-tab counting, confirmation thresholds, and checkbox behavior. Current source includes the helper and a disposal regression test.
- gemini: REJECT - Already fully implemented in our fork (commit 8149503ee) including LiveConnectionTabCount, close diagnostics, and automated unit and lab acceptance tests.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `4edeaba5c1` Re-run the resize when the panel moved again while it was applied - needs manual review

Size<=0 guard already ours; settle-recheck after UpdateSessionDisplaySettings is not. No GetAvailableContentSize here, use InterfaceControl.Size. Candidate for #177 splitter race.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/4edeaba5c11fe819d1cf1fd37584e6b4fa79c325

Counter-opinions: codex **REJECT** / gemini **NEEDS_HUMAN**
- codex: REJECT - The invalid-size guard already exists, and resize events already schedule debounced passes. The proposed recheck requires adaptation and evidence of additional benefit.
- gemini: NEEDS_HUMAN - Valuable fix for RDP splitter resize races (#177), but cannot be applied directly; requires human judgment and fork-specific reimplementation using InterfaceControl.Size.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `c4d0596f24` Keep the tab in front when its close is cancelled - needs manual review

Our OnFormClosing cancels on No without re-activating; same wrong-tab-in-front applies. Also affects our KeepTabsOpenAfterDisconnect cancel path. Verify in lab UI.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/c4d0596f2485d6493ea30bd27219f40c2a068759

Counter-opinions: codex **REJECT** / gemini **APPROVE** / codex **REJECT** / gemini **REJECT**
- codex: REJECT - DockPaneStripNG.CloseTab already restores the previous active content after cancellation; CancellingATabClose_LeavesTheActiveTabUnchanged covers this behavior. The import duplicates that fix.
- gemini: REJECT - Already implemented and tested in DockPaneStripNG.CloseTab, which restores active tabs on cancellation across all close triggers, making this ConnectionTab change redundant.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `0121f0be4c` fix: port scan reports why a scan can't start instead of doing nothing - needs manual review

Real bug: throw leaves button stuck on Stop, no reason shown. Our PortScanner already diverged (8119ae123 timeout fix), so port the ordering idea, not the diff.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/0121f0be4cea9a485f91f41d2bae56ea2b288f65

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - Current HEAD already constructs before changing scan state, reports validation failures visibly, and omits exception parameter names; this commit adds no useful behavior.
- gemini: REJECT - Commit 0121f0be was already cherry-picked in commit b9bc6490a and further enhanced with centralized input validation and localized error messages.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `07532bce52` Translate the connection properties into Hungarian - needs manual review

Hungarian strings for standard property keys we do have (AudioCapture etc. missing in our hu.resx). Context lines reference Vault/OpenBao keys we lack, so hand-apply the added data entries.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/07532bce526b55e47bc43768c4152bcdca922d2a

Counter-opinions: codex **REJECT** / gemini **APPROVE** / codex **REJECT** / gemini **APPROVE**
- codex: REJECT - All 164 keys are missing, but localization omits Remote Credential Guard domain requirements, Restricted Admin prerequisites, and current zoom guidance. Correct before import.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `0d8b8f6c56` Add "Copy All to Clipboard" to PuTTY connection tab context menu - needs manual review

The backend exists, but the requested tab action does not. Add only UI wiring using the existing method and resource, avoiding duplicate backend and localization.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/0d8b8f6c56485861217abdc30a25ae0420827ccf

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - Menu exposure is useful, but only its UI wiring should be reimplemented against the existing method; this commit is not directly landable.
- gemini: REJECT - The backend method already exists in our fork. This commit would cause merge conflicts and code duplication, requiring a clean manual reimplementation.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `199b1f362b` docs(sftp): reshape the change to a dual-pane file manager with a transfer queue - needs manual review

Spec-only for dual-pane SFTP manager with transfer queue; we have single file-transfer window (ab1be61fd). Idea worth watching once code lands, nothing to import.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/199b1f362b0e4203c5f322b752d1e1169a2802f1

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - This repository lacks OpenSpec, ISftpSession, and ISshCredentialResolver. The documentation patch assumes another fork's unfinished implementation and offers no standalone improvement to our existing transfer code.
- gemini: REJECT - Commit only modifies third-party design specs in non-existent openspec directory; importing unfinished feature planning docs adds repository noise without functional value.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `232fbf32ff` fix: multi-selection Enter opens connections, and task dialog buttons show focus (#52) - needs manual review

Our Enter handler still opens only SelectedNode; task dialog focus fallback and CommandButton focus ring absent. Skip docs-website hunk; verify against our multi-select tree and lab UI.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/232fbf32ffc324b12ceb8b8e251e4637742a8073

Counter-opinions: codex **APPROVE** / gemini **NEEDS_HUMAN** / codex **NEEDS_HUMAN** / gemini **NEEDS_HUMAN**
- codex: NEEDS_HUMAN - Both gaps exist locally, supporting their value. However, the focus hunk targets a superseded layout path; selectively adapt it while preserving our current dialog logic.
- gemini: NEEDS_HUMAN - Fixes align with focus and UX priorities but require separation into individual commits and path adaptation.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `2d1411667e` 修复：容器的ID现保持与文件中一致 - needs manual review

Current XML loading discards serialized container IDs because CopyFrom cannot set get-only ConstantID. Reimplement constructor-based preservation with malformed-ID and round-trip tests.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/2d1411667e60c4e001933d60f8de825dd2ac9213

Counter-opinions: codex **NEEDS_HUMAN** / grok **NEEDS_HUMAN**
- codex: NEEDS_HUMAN - The defect is real and absent here, but land a tested fork-aware reimplementation covering Container and Entity instead of this stale patch.
- grok: NEEDS_HUMAN - Real container ID-stability fix, but needs clean reimplementation and fork check.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `3c2fd1770a` fix: drop the trailing comma from the port scan open/closed port columns - needs manual review

Our ScanHost.cs still has the trailing ', ' loop; string.Join is a trivial cosmetic fix for the port scan grid.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/3c2fd1770ab8b9c2ee1ece2bb5dd6ad255f3bc09

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - HEAD already implements both properties with identical string.Join expressions in ScanHost.cs; importing this commit adds no useful change.
- gemini: REJECT - The exact change is already implemented verbatim in our repository, making this proposed import completely redundant.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `3f94a2c239` Dark mode: follow the OS, honor the theming setting, dark title bars - needs manual review

Follow-OS dark mode + DWM dark title bars addresses open #47. Clean idea, but flips ThemingActive default and our ThemeManager/settings diverged; re-derive carefully.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/3f94a2c23980a384cbf15386ae7ffc506a92e6e5

Counter-opinions: codex **REJECT** / gemini **NEEDS_HUMAN**
- codex: REJECT - OS matching is valuable, but this untested patch conflicts with live-switch and high-contrast theming, assumes restart-only behavior, and requires a scoped reimplementation.
- gemini: NEEDS_HUMAN - Valuable dark mode UX improvements matching modern Windows settings, but requires careful refactoring of settings and ThemeManager initialization to prevent regressions.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `53451f91f5` Default the main window to 90% of the screen - needs manual review

Ours falls back to designer size on first run; #171 MainWindowPlacement restructured this method. Add centred 90% working-area default only when nothing saved.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/53451f91f5195273903d0ac51e92ec1ee45e13ab

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - SettingsLoader already corrects stale restore coordinates through MainWindowPlacement. The proposed replacement omits that correction; default sizing needs an adapted patch preserving existing behavior.
- gemini: REJECT - The 90% heuristic provides questionable value over standard defaults while directly reverting our #171 multi-monitor placement logic.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `556127ef31` fix: keep Options usable when a settings secret cannot be decrypted (#18) - needs manual review

Options page survives undecryptable settings secret; no ErrorOptionsPageSettingsNotLoaded here. Symptom unconfirmed in our fork; jafin's crypto layers differ. Verify before reimplementing.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/556127ef31c31603313145d28cbfc63fec4378fa

Counter-opinions: codex **REJECT** / gemini **NEEDS_HUMAN**
- codex: REJECT - Recovery is useful, but SaveOptions saves every page; decryption failure skips subsequent settings. The patch neither preserves untouched values nor tests this data-loss path.
- gemini: NEEDS_HUMAN - Preventing Options window failure on undecryptable secrets is valuable, but our settings architecture differs; it requires maintainer judgment and local reimplementation rather than direct cherry-pick.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `6c1dbeaf82` fix: make ConnectionsFileResolver sole-candidate test deterministic - needs manual review

Our test still named StartupConnectionPathReturnsSavedPathWhenItIsTheSoleCandidate and scans real OS paths; host-dependent flake. Rewrite against ConnectionsFileResolver.Resolve is deterministic. Test-only.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/6c1dbeaf8209f23145c456805bb5cd72d03cff32

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - ConnectionsFileResolverTests.Resolve_SingleCandidate_ReturnsIt_WithoutPrompting already verifies candidate identity and absence of prompting. The import duplicates that coverage while removing the startup pipeline assertion.
- gemini: REJECT - Resolve with a single candidate is already tested in ConnectionsFileResolverTests. Replacing the service startup test with a redundant unit test provides negative value.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `6e684bc7f9` Fix the spin buttons and the crowded backup page - needs manual review

Our mrngNumericUpDown still hard-codes 96-DPI SetBounds; DPI-aware LayOutButtons is real. Skip Hungarian resx edits and BackupPage designer (ours already resized).  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/6e684bc7f9fe7b5e711598862639629e1e6e815a

Counter-opinions: codex **REJECT** / gemini **NEEDS_HUMAN**
- codex: REJECT - Layout improvements are useful and absent locally, but the shared-control DPI regression makes this commit unsuitable for pre-approval.
- gemini: NEEDS_HUMAN - The DPI-aware spin button layout is valuable for our high-DPI goals, but requires cherry-picking and reimplementation rather than landing the commit directly.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `6eabb55063` fix: remove the dead space between the timeout row and the progress bar - needs manual review

Our PortScanWindow.Designer still has 159F row; 4-line layout fix, verify visually in built app.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/6eabb55063017b51f2a2a0097f9714508ae98b19

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - HEAD already contains pnlIp height 110, pnlMain first-row height 116F, and the identical explanatory comment; importing adds no value.
- gemini: REJECT - Already cherry-picked into main as commit 2c3f49c22; PortScanWindow.Designer.cs already has these exact changes.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `6ecce9b6be` fix(notifications): render the message text on startup messages - needs manual review

We have the #53 _pendingItems deferral but not the posted flush; startup messages likely render timestamp-only here too. Reproduce first.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/6ecce9b6becc1bc5f81b8190157996e009ae292f

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - This fork renders message text in its sole column. The proposed fix targets a different layout and requires prerequisite changes, making direct import unsuitable.
- gemini: REJECT - Our fork uses a single-column notification list where Text is the message itself; the subitem rendering bug does not exist here.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `7d47769f49` Put the units back on the Hungarian spin box labels - needs manual review

Hungarian AutoSaveEvery label lacks units in ours; RdpOverallConnectionTimeout/RdpReconnectCount keys absent in our hu.resx. Cherry-pick only applicable string.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/7d47769f49cd4658beeafc4f3a482ff763784a15

Counter-opinions: codex **NEEDS_HUMAN** / gemini **APPROVE** / codex **NEEDS_HUMAN** / gemini **NEEDS_HUMAN**
- codex: NEEDS_HUMAN - The clarification matches autosave behavior and improves usability, but importing this exact commit requires adapting missing RDP entries; Reviewer A's compatibility objection is confirmed.
- gemini: NEEDS_HUMAN - Helpful UI label clarifications, but patch conflicts with our truncated Language.hu.resx and requires manual adaptation rather than clean import.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `85056294af` fix: stop connection edits from being lost without a word (#10) - needs manual review

Save-failure propagation already ours (b7126a004 TrySave). Still open here: Shutdown.SaveConnections drops Unassigned default to 'never' and never flushes the 2s debounced save. Port those two only.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/85056294af97fa3ad7316a36097e0d38f67a9500

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - Durability improvements are useful, but several fixes already exist here. This broad import requires selective adaptation to preserve newer SQL safeguards; it is unsuitable for preapproval.
- gemini: REJECT - Core save failure propagation is already implemented via TrySave. Do not import; surgically reimplement only the shutdown unassigned frequency fallback and debounce flush.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `8ec20405e6` test: run the WinForms tab tests on a real message loop with a reliable timeout - needs manual review

Our RunWithMessagePump is a bare STA thread with no Application.Run; Interrupt cannot unwind a wedged UI thread. Test-only, same file, clean apply.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/8ec20405e6151c033b6d9193e57cb5cd74bddca2

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - DockPaneStripNGTests.cs already contains the proposed implementation, with no working-tree differences. Importing it adds no value to this fork.
- gemini: REJECT - Commit 8ec20405e is already present in this branch and identical to current code in DockPaneStripNGTests.cs.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `90aa5e8c05` feat: double the default width of the port scan Hostname column (130 -> 260) - needs manual review

Ours still 130; FQDNs truncate. One-line designer change, matches our bolder-UI preference.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/90aa5e8c05dcf7e0c85429a9312def4ca6456279

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - Verified in both HEAD and the working tree. The proposed import adds no behavior or usability benefit.
- gemini: REJECT - Already cherry-picked into this repository in commit 35055177e792f2c6c1d4c909c284a7b1a9074f9e.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `9c4b85f18a` fix: set temp key-file attribute via File.SetAttributes - needs manual review

Replaces redundant throwaway FileInfo instantiation with clean, direct File.SetAttributes call in two PuttyBase temp key generation paths.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/9c4b85f18ab51f04b455d198cbf86b284dd6c3f8

Counter-opinions: codex **REJECT** / grok **REJECT**
- codex: REJECT - It provides no correctness or stability gain; reimplementation would be churn because both APIs set the same attribute and current code has zero warnings.
- grok: REJECT - Original object-initializer already sets attributes on disk; pure idiom tweak, not a real fix.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `9c5c4484ce` perf: stop the port scan flooding the UI thread; add common-ports button - needs manual review

Our MessageCollector still unlocked with per-item RemoveAt trim; scan adds messages per host. Take lock+RemoveRange; common-ports button optional, designer diff will conflict.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/9c5c4484ce6059312dfc4a75dcf2bb89675bf3be

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - This fork already contains the message-list locking and batched scan updates; common ports are available through its newer port-mode selector.
- gemini: REJECT - This commit was already cherry-picked as 83015a272. Its UI changes were subsequently replaced by a dedicated port mode selector in 7f3afcc18.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `a677fae337` Fix ObjectDisposedException when closing a connection tab - needs manual review

Closes TOCTOU race in Prot_Event_Closed Invoke; our guards (IsDisposed check) exist but not the try/catch + marshaled re-check. Small defensive win; code diverged.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/a677fae337a8c49890c6a0e2d87b9739d708d25d

Counter-opinions: codex **REJECT** / grok **APPROVE** / claude **REJECT**
- codex: REJECT - Current HandleProtocolClosed already has stronger handle, marshaling, disposal-race, and close guards, so this commit offers no unique value and conflicts with intentional semantics.
- claude: REJECT - Fork diverged: same race already fixed better (non-blocking BeginInvoke re-marshal, ConnectionWindow.cs:2223-2246). Import adds nothing, code no longer matches.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `b1e1dcfe5d` fix: stop docking the placeholder window a handed-off console leaves behind (#48) - needs manual review

PseudoConsoleWindow absent in our tree; ExternalProcessProtocolBase.cs exists. Win11 Windows Terminal handoff docks 0x0 placeholder. Small, tested, with docs note.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/b1e1dcfe5db54b1acd6d191c57dd3980a38ab3cf

Counter-opinions: codex **REJECT** / gemini **APPROVE** / codex **REJECT** / gemini **APPROVE**
- codex: REJECT - The [patch](https://github.com/jafin/mRemoteNG/commit/b1e1dcfe5db54b1acd6d191c57dd3980a38ab3cf) fixes an absent filter, but needs headless test adaptation and lifecycle coverage proving failed docking preserves the process.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `c535880a14` feat: single address field and port mode selector for the port scan - needs manual review

We still IPAddress.Parse two fields; CIDR/range/IPv6 parser with tests is self-contained. PortScanWindow diverged (timeout fix 8119ae123), expect merge work.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/c535880a14c0395a18dfe24a3c22d4c1853dfe2a

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - Current repository already contains both parsers and the complete single-address and port-mode UI. The proposed commit adds no new functionality.
- gemini: REJECT - Already imported via cherry-pick 7f3afcc18 and further updated in f116697c4; attempting to re-import would cause merge conflicts.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `d37a901670` fix: Options UI polish and task dialog button spacing (#45) - needs manual review

Our BackupPage still has 11 Salmon debug BackColors; ConfigurationPage still 3 columns. Small designer-only cleanup, low risk.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/d37a901670287d284fb9cbffcd73c348d6a87f09

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - This fork already sizes task dialogs dynamically for DPI and content. Removing cosmetic colors does not justify regressing the flexible configuration layout.
- gemini: REJECT - TaskDialog button spacing is already handled dynamically in #198, and fixing TableLayoutPanel widths harms resizing; only the Salmon background removal is valid.

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

### `f634039a17` fix: offer the SSH transfer window for the protocols it can reach (#24) - needs manual review

Our gate still SSH1|SSH2 at 4 call sites (ConnectionWindow:1489/1587, ContextMenu:1022/1062); SSH1 offered, OpenSSH withheld. No ProtocolFeature class here; add helper without SSHNative.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/f634039a177f9204454e5fd8680b3cce3ee50863

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - OpenSSH transfer availability is useful and currently missing, but this commit requires prerequisite features. A small adapted fix should be reviewed separately.
- gemini: REJECT - The fix concept is sound for OpenSSH and SSH1, but cannot land directly; it must be cleanly reimplemented for our fork's existing protocol types.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `3567ecabb1` fix: validate the ports argument in the PortScanner constructor - needs manual review

Our PortScanner ctor still does bare _ports.AddRange(ports) with no null/empty/range check; depends on jafin's PortListParser and Language keys, so re-do with local constants.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/3567ecabb18f190897415f43b4061f06fb270319

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - Already implemented in commit 6bb49727d and present at HEAD, including all proposed tests; this fork additionally validates the port-range overload.
- gemini: REJECT - The exact validation logic and test cases are already committed on main; re-importing is redundant and would cause conflicts.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `7746827c2b` fix: take a command-line switch value whole, whatever it contains (#41) - needs manual review

Bug confirmed here: CmdArgumentsInterpreter splitter regex `=|:` still splits a space-separated value like C:\path, silently dropping --cons. Our CommandLineParser diverged from jafin's; port the logic plus tests, skip openspec/docs-website.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/7746827c2b4af03f16d59fe482cb6f050bef70c9

Counter-opinions: codex **NEEDS_HUMAN** / gemini **NEEDS_HUMAN**
- codex: NEEDS_HUMAN - The parser defect exists here and merits fixing, but the added missing-file behavior needs startup recovery review beyond the supplied unit tests.
- gemini: NEEDS_HUMAN - Fixes real silent drops of drive-letter paths and host:port values, but diff cannot apply cleanly and requires manual porting without foreign documentation files.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `95f621308b` fix: validate the ports in the port-range PortScanner constructor too - needs manual review

Our PortScanner has no port validation at all (no PortListParser/ValidatePorts); the range ctor loop can allocate an absurd range. Small standalone guard worth adding with tests, independent of jafin's parser.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/95f621308b5afe3d59675a5721fc186c663fca40

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - The fix supports stability, but repository inspection confirms it is already implemented and committed; importing this change adds no useful behavior or coverage.
- gemini: REJECT - Already imported as commit 713ada425e375323de65b4556b054df303f0cd0d with identical logic and unit tests active on main.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `9db6e8b09b` feat: put the port scan IP range on one line and tighten the layout - needs manual review

Cosmetic PortScan layout (IP range one line, IPv6-wide boxes). Our Designer lacks pnlIpRange; self-contained, low risk, minor benefit.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/9db6e8b09be0d332ba383b5d3489649ccd577b06

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - This exact commit was already cherry-picked as 2f200077e and subsequently superseded by single-field range parsing; importing it again adds no value.
- gemini: REJECT - Our fork already cherry-picked jafin's subsequent commit replacing separate start/end inputs with unified CIDR/range field, making this layout change completely obsolete.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `08b056f698` Measure the panel, not the RDP control, when resizing the session - needs manual review

Core fix (never measure Control.Size) already ours: we use InterfaceControl.Size. Padding-aware ClientRectangle measurement is new; connection-frame padding may oversize session and re-show scrollbars. Candidate for #177.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/08b056f698587be31a2582070549e24dc7147fcb

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - Our DoResizeClient already measures InterfaceControl.Size, avoiding ActiveX feedback. Padding correction may help, but requires a separate patch compatible with our existing layout.
- gemini: REJECT - Our fork already measures InterfaceControl instead of Control in DoResizeClient, making this patch redundant and structurally incompatible.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `4b2768dcea` fix: claim the foreground for the main window at startup - needs manual review

Our FrmMain_Shown still plain Activate/SetForegroundWindow; AttachThreadInput already declared. Reasonable startup foreground fix, but #143/#168 history: attach must not be blindly detached, verify in lab.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/4b2768dcea09bbec5dc3f857d37f3163dd2ac0c4

Counter-opinions: codex **REJECT** / gemini **NEEDS_HUMAN**
- codex: REJECT - Startup focus fits our priorities, but this patch requires adaptation to existing interop declarations and verification that forced activation preserves protocol input routing.
- gemini: NEEDS_HUMAN - Fixes main window losing foreground at startup, but needs manual reimplementation and lab testing because NativeMethods already defines these APIs.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `9216eeec3f` Never reconnect an RDP 8 session just to resize it - needs manual review

Our RdpProtocol8.DoResizeClient still calls Reconnect on FitToWindow. Real session-drop bug, but RdpVersion=Highest ships RdpProtocol11 so only forced-RDC8 users hit it; our resize path diverged.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/9216eeec3fc53ba08f2ea15804a58983916a8f6e

Counter-opinions: codex **NEEDS_HUMAN** / gemini **REJECT**
- codex: NEEDS_HUMAN - The reconnect guard is absent here and useful, but silently disabling configured automatic resizing needs maintainer judgment and verification of the resulting display behavior.
- gemini: REJECT - Silently disables FitToWindow on RDP 8; our fork already debounces resizes, supports SmartSize, and defaults to RDP 11, making this patch obsolete and regressive.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `0045263765` Show auto-detected PuTTY path on Advanced options page - needs manual review

Small UX win: shows auto-detected PuTTY path in options. Our Designer/options pages diverged heavily; re-do by hand, not cherry-pick.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/004526376515164a858c98a9a1c782d04a28c33c

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - The fork already exposes the custom override and otherwise always launches bundled PuTTYNG.exe, so this UI is redundant, misleading, and upstream-specific.
- gemini: REJECT - Our fork bundles `PuTTYNG.exe` and has not imported the unbundling candidate. This change is redundant and will break the build due to missing auto-detection dependencies.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `23bc5b533a` docs: archive fix-command-line-value-parsing (#42) - needs manual review

openspec archive docs only, no code. But our CmdArgumentsInterpreter.cs:46 still splits on ':' so `--cons C:\path` breaks; track jafin's code commit, not this.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/23bc5b533a708fc2ca070070fbe401246848c486

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - CmdArgumentsInterpreter still splits separate path values on colons. This documentation-only commit provides no implementation fix or regression tests and introduces an unused OpenSpec archive.
- gemini: REJECT - This commit only archives task specifications for a foreign workflow. Any actual parsing bug fix should be ported from code commits directly, not via openspec files.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `404e7291d5` Fix native RDP launch when signing fails - needs manual review

Fixes a file (NativeRdpLauncher.cs) that does not exist here; only relevant if we ever adopt the native mstsc mode from 405520de.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/404e7291d589796fae4332a40cae13b166dd3e7e

Counter-opinions: codex **REJECT** / gemini **APPROVE** / codex **REJECT** / gemini **REJECT**
- codex: REJECT - The repository has no NativeRdpLauncher or rdpsign integration; this isolated patch cannot improve its ActiveX RDP path. Thumbprint compatibility does not justify weakening signing.
- gemini: REJECT - Our fork uses ActiveX RDP rather than external mstsc launching, so this commit targets non-existent infrastructure while silently bypassing configured RDP file signing.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `938611f022` fix: close a PuTTY tab in one step, without PuTTY's own prompt - needs manual review

Our TryClosePuttyGracefully just WaitForExit(1000); PuTTY warn-on-close double prompt real. But diff also touches ConnectionTab close semantics (disconnectOnly) overlapping #61/#172 work; port dialog-dismiss part only.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/938611f02255a6e9bbe40fe59bffd8b588c85dd2

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - The fix supports protocol usability, but importing it adds no value: committed source already contains the PuTTY confirmation dismissal and disconnect-only tab handling.
- gemini: REJECT - Commit 938611f02255 was already merged into our repository's main branch via PR #158.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `a320091188` perf: batch the passes RemoveFilter runs when clearing the tree filter - needs manual review

Our RemoveFilter assigns ExpandedObjects, not RebuildAll; jafin's base fix (EnsureVisible index throw) absent too. Batch+rebuild worth doing; adjacent to #144/#149 row-index family.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/a32009118810e2237c029756814292f30a01343c

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - ConnectionTree.cs already contains every proposed change, committed as a93729afa with the same subject. Importing this commit adds no value.
- gemini: REJECT - This exact change and commentary were already incorporated into ConnectionTree.cs in our fork, making this import completely duplicate.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `ead4062d23` refactor(ui): remove the unused session split host - needs manual review

Removes jafin's own SessionHost/SplitContainer scaffolding (SFTP side panel) that never existed in our fork; nothing to remove here.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/ead4062d23408fdbd127e9c7813ad4a96004c410

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - HEAD already parents InterfaceControl directly to the tab and contains no session split, side-panel APIs, or NotifyHostResized. The OpenSpec path is also absent.
- gemini: REJECT - Our fork already parents InterfaceControl directly to connectionContainer without SessionHost or SplitContainer. There is nothing to import or remove.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `ec9f699a2c` Say which terminal this is while PuTTY is still around - needs manual review

Temporary debug banner in fork-only xterm.js SSH terminal; we have no Resources/Terminal/terminal.html or native SSH.NET terminal.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/ec9f699a2cc3f09e0fa2e036206bc9337ebcf6fe

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - This fork uses PuTTY and Windows OpenSSH for interactive SSH. A temporary comparison banner for another fork's terminal provides no useful import.
- gemini: REJECT - Temporary debug banner for an absent experimental xterm.js/SSH.NET terminal feature; our fork uses PuTTY for SSH connections.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `215eb6c564` Adapt native RDP serializer to fork model - needs manual review

RdpFileSerializer.cs does not exist here; commit adapts it to that fork's model via reflection-style lookups. Native .rdp export is interesting but needs whole feature, typed against our ConnectionInfo.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/215eb6c564eacd35cd55dc124cfd90d9232fad7a

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - Our fork uses RdpConnectionSerializer and already exposes these properties. This compatibility patch offers no demonstrated benefit and requires prerequisite code outside the proposed import.
- gemini: REJECT - Modifies a file absent from our codebase while replacing strongly-typed ConnectionInfo access with string-based reflection shims. Has no standalone value.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `5293862e10` feat: make port scan parallelism configurable from the UI - needs manual review

Depends on jafin's Parallel.ForEachAsync scanner rewrite; our PortScanner is sequential thread with no MaxConcurrentHosts. Nothing to configure without prerequisite.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/5293862e1066e03c86909fccf7d33bdf478f433c

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - Repository already exposes the UI control, passes its value to PortScanner, and clamps concurrency to 1–128 with default 64; importing adds no functionality.
- gemini: REJECT - This commit is already present in our tree as commit f55b7b276, having been cherry-picked previously. No import is needed.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `ba164119c1` feat: localize the port scan panel - needs manual review

Our PortScanWindow still hardcodes English; but commit depends on jafin's IpRangeParser/PortListParser (CIDR/IPv6 rewrite) absent here. Only resx strings portable.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/ba164119c17eac43c6536d4012963df68f6f3f52

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - Current source already contains the localized panel, parser hints, and scanner errors. Reimporting offers no additional benefit.
- gemini: REJECT - Already cherry-picked into main as commit f116697c41de4ef8c4df5a27fca4c7a22382afe9 with identical changes across all 6 files.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `015c152817` Use connection names for temporary RDP files - needs manual review

Temp .rdp filenames from connection name: minor diagnostic nicety, but leaks connection names into LocalAppData filenames. Their TemporaryRdpFileStore is fork-local.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/015c15281728dbf87fc4fb882492ccd0de2ff515

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - This fork connects through embedded RDP ActiveX. Importing this isolated filename change provides no demonstrated benefit to its existing RDP implementation.
- gemini: REJECT - Targets a fork-private external mstsc launcher absent from our repository; cannot compile here and offers no value to our embedded ActiveX RDP architecture.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `98a262b7b7` fix: recognise the embedded PuTTY window as our own foreground - needs manual review

ApplicationIsInForeground does not exist in our frmMain; #168 solved differently (bf99b7ad9, reporter-confirmed). GA_ROOT idea useful only if a PuTTY-foreground regression resurfaces.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/98a262b7b79fc3a6736234e9b6caa31a1ef7e047

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - PuttyBase.Focus already keeps the host foreground and repairs PuTTY-owned foreground state. ApplicationIsInForeground does not exist here, so importing this patch requires adaptation without demonstrated benefit.
- gemini: REJECT - Our fork already solved PuTTY foreground handling in #168 (bf99b7ad9) at the source in PuttyBase.Focus. ApplicationIsInForeground does not exist here.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `c31a3c8161` feat(sftp): make transfers reachable, add mutation commands and theming - needs manual review

Increment on jafin's SFTP browser panel; no FileTransfer/FileManagerTab in our tree. Only importable as the whole feature chain once it stabilises.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/c31a3c8161251515726eaedf6f46933946e9f252

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - The [commit](https://github.com/jafin/mRemoteNG/commit/c31a3c8161251515726eaedf6f46933946e9f252) extends an SFTP architecture absent locally. Its usefulness does not justify pre-approving the larger integration required.
- gemini: REJECT - Commit modifies non-existent files from an unimported experimental SFTP panel branch; outside fork roadmap and cannot compile in isolation.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `d239f5b9e5` feat(sftp): add the pane abstraction, local browser and transfer queue - needs manual review

Dual-pane SFTP browser foundations (browser abstraction, queue, tests) are self-contained and tested, but only half a feature; wait for jafin's series to complete, then evaluate as a whole.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/d239f5b9e5f776df781d2263cb31b63f2185b24e

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - Requires missing prerequisite commits and adds substantial unwired feature groundwork, exceeding quick pre-approval scope. [Diff](https://github.com/jafin/mRemoteNG/commit/d239f5b9e5f776df781d2263cb31b63f2185b24e.patch)
- gemini: REJECT - The change adds a massive new feature which expands the maintenance surface significantly, violating the preference for small, verifiable changes.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `f8b3b93411` Anchor the RDP control to the panel instead of assigning its size - needs manual review

Anchor-instead-of-Size idea targets our #177 symptom, but diff is against their AutoScroll variant; our SetResolution/DoResizeControl differ and lab trace showed resize sizes are correct. Test idea in lab, don't port.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/f8b3b934116f748df84a075f36a9f07d55cb228b

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - Our fork already uses Dock.Fill and debounced session resizing. The patch targets absent FitToWindow code; local benefit is unproven and adaptation requires more than a straightforward import.
- gemini: REJECT - Patch targets lovaszlaszlo's divergent AutoScroll implementation and does not apply. Our fork relies on DockStyle.Fill and UpdateSessionDisplaySettings; anchoring introduces regressions.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `f92a9cc609` security: stop writing every debug message to the log by default - needs manual review

Our TextLogMessageWriterWriteDebugMsgs already defaults False; only new bit is %USERPROFILE% redaction in the log path. Small, but touches diagnostics (tripwire, human review).  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/f92a9cc609c15c3afcbe7edbe958f77406e81d78

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - The [patch](https://github.com/jafin/mRemoteNG/commit/f92a9cc609c15c3afcbe7edbe958f77406e81d78.patch) addresses a real debug-filtering gap, but needs a log4net-native port before approval.
- gemini: REJECT - Debug logging already defaults to false here; the commit cannot apply cleanly, carries alien openspec files, and path sanitization requires dedicated reimplementation.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `8f39c112b5` fix(rdp): reapply performance flags and input finalizer on all reconnect paths - needs manual review

Reapplying performance flags on mstscax auto-reconnect is a plausible real fix, but patch depends on fork-only view-only/input-finalizer infrastructure we lack. Note idea, not code.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/8f39c112b57865efa6c34cd735c1b35394c203dc

Counter-opinions: codex **REJECT** / grok **NEEDS_HUMAN**
- codex: REJECT - Exact commit cannot land: RdpProtocol6 was deleted, passive helpers are absent, RdpProtocol8 is refactored, and no tests or reproducible evidence are provided.
- grok: NEEDS_HUMAN - Reapplying pFlags on reconnect is useful, but diff is fork-specific and needs local path checks.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `7e126049b3` feat(sftp): transfer directories recursively, with one overwrite choice - needs manual review

Builds on jafin's SFTP/FileTransfer subsystem (ISftpSession, SftpSession) which our fork lacks entirely; 2555 lines depend on it. Track jafin's SFTP branch as a whole.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/7e126049b31f49b2100f69e6a20c704154fd7029

Counter-opinions: codex **REJECT** / gemini **NEEDS_HUMAN**
- codex: REJECT - Recursive SFTP transfers are useful, but this commit requires substantial prerequisite architecture and integration work; it is not a small, independently verifiable import.
- gemini: NEEDS_HUMAN - Recursive SFTP transfer is valuable, but the massive 2500+ line footprint requires manual review to ensure it aligns with existing file transfer architecture.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `8ded8c7535` Native SSH: take the appearance from the named PuTTY session - needs manual review

Reads PuTTY session font/colours for an xterm.js SSHNative protocol we lack (no ProtocolSshNative, terminal.html). Idea sound; only relevant if we ever add a WebView2 terminal.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/8ded8c7535559aef80bdcae609b431df7a3eb8d6

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - This fork uses PuTTY-backed SSH and lacks ProtocolSshNative and terminal.html. Importing this appearance change requires broader prerequisite work outside the proposed commit.
- gemini: REJECT - Our fork does not have the WebView2/xterm.js native SSH implementation that this patch modifies, making the change inapplicable and broken.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `952dd3b37c` Native SSH: verify the host key and support private keys - needs manual review

Host-key verification for ProtocolSshNative, which our fork does not have (SSH1/SSH2/OpenSSH only). Valuable only if we ever adopt a native SSH.NET terminal; note reusing password as passphrase and SSHOptions parsing.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/952dd3b37cb18f9a1c39fabb986d150d7940216a

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - This fork has no SSHNative implementation; OpenSSH already supports PrivateKeyPath. Import requires missing infrastructure, duplicates existing functionality, and adds unsafe authentication behavior without tests.
- gemini: REJECT - Our fork uses PuTTY and lacks ProtocolSshNative entirely; this change cannot apply without importing an entire separate native SSH subsystem.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `b0b8020946` fix(sftp): wire the agent provider, and stop mangling server paths - needs manual review

Fix inside jafin's SFTP feature (no mRemoteNG/Connection/Sftp here). Backslash-preservation point is sound; bundle with feature import if SFTP browser ever evaluated.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/b0b80209466e4d6e4b341c767da6f86fad61c402

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - Our SFTP implementation uses SecureTransfer directly. This patch depends on another fork's architecture; importing it requires substantial prerequisite work beyond a small maintenance fix.
- gemini: REJECT - Our fork lacks the SFTP file manager subsystem entirely; this fix cannot apply standalone and should only be evaluated alongside the full feature.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `b939635404` feat(sftp): add the dual-pane file manager tab - needs manual review

Dual-pane SFTP file manager, 1610 lines over 12 files, depends on IFileSystemBrowser and earlier openspec commits absent here. Attractive later, needs full series plus security review of file transfer; not now.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/b939635404025d16afbc64b0f7de2b4de3baf3f1

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - It cannot be imported independently; transfer actions also lack UI wiring. This incomplete 1,610-line feature exceeds a small, quickly verifiable maintenance change.
- gemini: REJECT - Cannot compile standalone without unimported prerequisite commits. It is a massive speculative UI rewrite duplicating our existing file transfer functionality against fork priorities.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `cb423ff6ec` security: stop backup recovery misreporting a key it cannot unwrap (#35) - needs manual review

Depends on jafin's per-file key / KeyProtectionException feature absent here; our loader has no unwrap path, so bug does not exist yet. Revisit if we adopt per-file keys.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/cb423ff6ecf8949d2dd730cacba61f55b082c6ba

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - Its security aim fits, but importing it requires a separate per-file key implementation; this is not a self-contained, quickly verifiable fix.
- gemini: REJECT - The fix addresses an issue in a per-file key encryption subsystem our fork has not adopted; without that underlying architecture, this patch is inapplicable.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `39aa9177e0` fix(sftp): attach the pane's icons where the list actually looks - needs manual review

Fixes jafin's SFTP FilePaneControl, which we do not have; only the OLV SetSmallImageList-vs-property lesson transfers. Watch the file-transfer feature itself.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/39aa9177e059dc7ed421c7a4d151f7a90b23d1a1

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - The ObjectListView fix is technically sound, but this fork uses SSHTransferWindow and SecureTransfer. The proposed patch cannot apply independently or benefit existing transfer functionality.
- gemini: REJECT - Cannot be imported standalone because FilePaneControl belongs to jafin's unmerged SFTP feature branch which is absent from our codebase.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `127570bac6` feat(ui): host connection sessions in a split so a side panel can share the tab - needs manual review

Groundwork for jafin's SFTP side panel: wraps sessions in a split, reparents InterfaceControl. Touches handle/reparent paths relevant to #182/#177; only worth it if we adopt the SFTP browser panel.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/127570bac6d10d312d1461a3a6cb755a16bee039

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - The commit leaves ConnectionWindow unchanged, so its close handler returns early under the new hierarchy. This introduces protocol lifecycle regressions for speculative side-panel infrastructure.
- gemini: REJECT - Speculative UI refactoring for an unmerged SFTP side panel; introduces complex hierarchy changes and high protocol stability risks without delivering standalone user value.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `2a693c85c2` Added SSH Tunnel via SSH_DotNet - needs manual review

Native SSH.NET forwarding is potentially valuable, but this untested patch depends on an absent protocol and rewrites obsolete tunnel logic; monitor, do not port.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/2a693c85c2525ff21e0f35968f9a0745dc612022

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - It adds an untested parallel SSH stack and omits SQL/MariaDB persistence, conflicting with stability, storage consistency, and quick verification requirements.
- gemini: REJECT - We do not have the SSH_DotNet protocol implemented. Importing this will break the build and introduces excessive complexity.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `42f04f6eb6` Native SSH: do not lose the message when something goes wrong - needs manual review

ProtocolSshNative (WebView2 terminal) does not exist here; patch only meaningful if that whole feature is ever imported.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/42f04f6eb66fd55845ae5da9176520579ba13c4d

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - Our SSH2 uses PuttyBase and OpenSSH uses ConsoleControl. This WebView2 terminal patch cannot improve either existing implementation as a standalone import.
- gemini: REJECT - Our fork uses PuTTY and OpenSSH; ProtocolSshNative is absent, so this commit cannot apply without importing an entire unsupported subsystem.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `932e6f6116` Enhance connection handling and UI features - needs manual review

Mixed bag: new inheritance props (ExternalAddressProvider, RDP StartProgram, gateway token), notification detail, plus personal junk (.vscode, WorldOfFanXP.xml). Partly overlaps our upstream ports; cherry-pick only if users ask.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/932e6f611674e6227db18d977f67a1b577af25a2

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - A 732-line mixed, untested commit also bypasses notification filters, leaks writer subscriptions, duplicates shipped UI/retry features, adds untranslated labels, and uses noncanonical tooling.
- gemini: REJECT - This is a mixed bag of personal settings, French locale scripts, and features already integrated or overlapping with our upstream ports. Not suitable for import.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `718f5c28c1` feat(sftp): reconnect a dropped session, and make the listing readable - needs manual review

Builds on jafin's SFTP/FileTransfer subsystem (mRemoteNG/Connection/Sftp, FileTransfer/) which our fork does not have; no standalone import possible.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/718f5c28c1300e2745e3a95700bf5787d8c03f61

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - This fork uses SecureTransfer and SSHTransferWindow; the modified session, controller, pane, and tab classes do not exist here. The commit cannot stand alone.
- gemini: REJECT - The commit modifies non-existent files (+1835 lines) from an external SFTP subsystem that cannot be imported standalone and introduces unverified file transfer complexity.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.

### `df13f1685d` Sign native RDP files in-process with CMS - needs manual review

Rewrites NativeRdpFileSigner.cs we do not have; native mstsc launch + CMS signing is a fork-specific feature. Crypto code, tripwire-gated. Niche demand.  
Source: https://github.com/mRemoteNG/mRemoteNG/commit/df13f1685d1cdcffadd756b73f7210878ad60eaa

Counter-opinions: codex **REJECT** / gemini **REJECT**
- codex: REJECT - Repository searches found no signer or signing call site. Adoption requires prerequisite infrastructure and RDP signature compatibility tests beyond this single-file commit.
- gemini: REJECT - Target file NativeRdpFileSigner.cs does not exist in our fork; we use AxMSTSCLib rather than native mstsc file launching and signing.

Port by hand - the patch will not apply cleanly over our tree. Read the source diff, reimplement, and credit the original author in the commit body.


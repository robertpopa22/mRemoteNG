# Fork Radar - 2026-09-30

Upstream `mRemoteNG/mRemoteNG` - forks scanned for changes worth importing into `robertpopa22/mRemoteNG`.

| Tier | Count |
|---|---|
| Tier A - ready to cherry-pick | 1 |
| Tier B - worth porting by hand | 72 |
| Tier C - watch list | 7 |
| Quarantine - security review required before anything else | 69 |
| Tier D - rejected | 400 |

## Tier A - ready to cherry-pick

### `7349e5a6aa` Fix main window stuck behind other windows after startup

- **fork:** [k-meeks/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7349e5a6aa3b85440a6f934e5269555c476fbb04) by Kyle Meeks
- **size:** 1 files (+0/-5)
- **score 13** - ready to cherry-pick
- **triage:** bugfix | value 4 | effort 1 | risk 1 | applies likely | IMPORT
- **why:** Removes redundant window activation code that causes inconsistent WinForms state when blocked by Windows on startup. Highly beneficial UX fix.
- **pre-approval:** **MANUAL-REVIEW** (claude:REJECT / codex:REJECT / grok:REJECT)
  - dissent - claude: REJECT - Code present in our fork and intentional; focus handling is a fragile priority area (#110/#118/#143/#168). Unproven premise, no repro — needs lab evidence before touching.
  - dissent - codex: REJECT - This fork intentionally added these lines for the same symptom; deleting them without an alternative or reproduction conflicts with its current splash lifecycle.
  - dissent - grok: REJECT - Those three calls were added here on purpose after splash close; importing undoes our own changelogged fix.

## Tier B - worth porting by hand

### `b7c487412f` fix: restore connection tree state when the search filter is cleared

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b7c487412fc8ee89d9001dcbfad754484a42b31b) by Jason Finch
- **size:** 2 files (+102/-8)
- **score 10** - worth doing, needs work
- **triage:** bugfix | value 4 | effort 2 | risk 2 | applies likely | IMPORT
- **our issue:** #149
- **why:** Our ApplyFilter/RemoveFilter still store live ExpandedObjects and skip RebuildAll; stale row map after filter clear is the same IndexOf/RedrawItems family as #149. Protected RebuildAll(IList,IEnumerable,IList) exists. Tests included.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The snapshot, RebuildAll restoration, and both tests already exist here in 70d545ed9; a93729afa subsequently improves RemoveFilter. This import adds no value.
  - dissent - gemini: REJECT - Already merged via PR #159 (commit 70d545ed9); our tree has since added further batching and painting performance enhancements.

### `c4837b6551` fix: measure task dialog text the way it is drawn, and size buttons to the client area

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c4837b6551b9d8fe8ffc9980159905d0442824f3) by Jason Finch
- **size:** 2 files (+30/-12)
- **score 10** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 1 | risk 1 | applies conflict | REIMPLEMENT
- **why:** Our frmTaskDialog still uses Graphics.MeasureString and Width for command buttons (GDI+/GDI mismatch, same class as #163). Take the two code hunks; skip the resx strings, which are jafin's storage-hardening feature.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork already implements GDI text measurement and panel-relative button sizing during relayout. The storage-upgrade resource keys are absent, so this commit is not directly applicable.
  - dissent - gemini: REJECT - Already implemented superior GDI TextRenderer measurement and dynamic layout in commit 35507078f; includes foreign storage upgrade strings.

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

### `2a9a06a5c7` Translate the menus, dialogs and options pages into Hungarian

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2a9a06a5c7ba6485ca429c9da7ccee0e66831b12) by Lovasz Laszlo
- **size:** 1 files (+371/-0)
- **score 8** - worth doing, needs work
- **triage:** docs | value 3 | effort 1 | risk 2 | applies likely | IMPORT
- **why:** Our hu.resx has 88 of 910 keys; this adds 371. Drop keys we removed (update channels, e.g. AskUpdatesContent) and verify against our Language.resx before landing.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - Adds missing localization, but the [patch](https://github.com/lovaszlaszlo/mRemoteNG/commit/2a9a06a5c7ba6485ca429c9da7ccee0e66831b12.patch) conflicts with this fork’s resource contract and mistranslates the reconnection-dialog option as automatic reconnection.
  - dissent - gemini: NEEDS_HUMAN - Valuable Hungarian localization additions, but requires manual curation against Language.resx to filter dead keys and resolve resx conflicts.

### `2cb2b55cc7` fix: drop the redundant panel-close prompt after a tab disconnect (#46)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2cb2b55cc723d161a670d4ad41f98ad830871d4f) by Jason Finch
- **size:** 1 files (+14/-6)
- **score 8** - worth doing, needs work
- **triage:** bugfix | value 3 | effort 1 | risk 2 | applies likely | IMPORT
- **why:** Our Connection_FormClosing still counts connDock.Documents.Any(); HasConnectionTabs already exists so the LiveConnectionTabCount refactor drops in. Verify with AutoClosePanelOnLastTabClose UI repro.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Commit 8149503ee already implements identical live-tab counting, confirmation thresholds, and checkbox behavior. Current source includes the helper and a disposal regression test.
  - dissent - gemini: REJECT - Already fully implemented in our fork (commit 8149503ee) including LiveConnectionTabCount, close diagnostics, and automated unit and lab acceptance tests.

### `4edeaba5c1` Re-run the resize when the panel moved again while it was applied

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4edeaba5c11fe819d1cf1fd37584e6b4fa79c325) by local
- **size:** 1 files (+15/-0)
- **score 8** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 1 | risk 2 | applies conflict | REIMPLEMENT
- **our issue:** #177
- **why:** Size<=0 guard already ours; settle-recheck after UpdateSessionDisplaySettings is not. No GetAvailableContentSize here, use InterfaceControl.Size. Candidate for #177 splitter race.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - The invalid-size guard already exists, and resize events already schedule debounced passes. The proposed recheck requires adaptation and evidence of additional benefit.
  - dissent - gemini: NEEDS_HUMAN - Valuable fix for RDP splitter resize races (#177), but cannot be applied directly; requires human judgment and fork-specific reimplementation using InterfaceControl.Size.

### `c4d0596f24` Keep the tab in front when its close is cancelled

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c4d0596f2485d6493ea30bd27219f40c2a068759) by local
- **size:** 1 files (+10/-0)
- **score 8** - worth doing, needs work
- **triage:** bugfix | value 3 | effort 1 | risk 2 | applies conflict | IMPORT
- **why:** Our OnFormClosing cancels on No without re-activating; same wrong-tab-in-front applies. Also affects our KeepTabsOpenAfterDisconnect cancel path. Verify in lab UI.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:APPROVE / codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - DockPaneStripNG.CloseTab already restores the previous active content after cancellation; CancellingATabClose_LeavesTheActiveTabUnchanged covers this behavior. The import duplicates that fix.
  - dissent - gemini: REJECT - Already implemented and tested in DockPaneStripNG.CloseTab, which restores active tabs on cancellation across all close triggers, making this ConnectionTab change redundant.

### `0121f0be4c` fix: port scan reports why a scan can't start instead of doing nothing

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/0121f0be4cea9a485f91f41d2bae56ea2b288f65) by Jason Finch
- **size:** 2 files (+34/-21)
- **score 7** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 2 | risk 2 | applies conflict | REIMPLEMENT
- **why:** Real bug: throw leaves button stuck on Stop, no reason shown. Our PortScanner already diverged (8119ae123 timeout fix), so port the ordering idea, not the diff.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Current HEAD already constructs before changing scan state, reports validation failures visibly, and omits exception parameter names; this commit adds no useful behavior.
  - dissent - gemini: REJECT - Commit 0121f0be was already cherry-picked in commit b9bc6490a and further enhanced with centralized input validation and localized error messages.

### `07532bce52` Translate the connection properties into Hungarian

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/07532bce526b55e47bc43768c4152bcdca922d2a) by Lovasz Laszlo
- **size:** 1 files (+165/-0)
- **score 7** - worth doing, needs work
- **triage:** chore | value 2 | effort 1 | risk 1 | applies conflict | IMPORT
- **why:** Hungarian strings for standard property keys we do have (AudioCapture etc. missing in our hu.resx). Context lines reference Vault/OpenBao keys we lack, so hand-apply the added data entries.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:APPROVE / codex:REJECT / gemini:APPROVE)
  - dissent - codex: REJECT - All 164 keys are missing, but localization omits Remote Credential Guard domain requirements, Restricted Admin prerequisites, and current zoom guidance. Correct before import.

### `0d8b8f6c56` Add "Copy All to Clipboard" to PuTTY connection tab context menu

- **fork:** [k-meeks/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/0d8b8f6c56485861217abdc30a25ae0420827ccf) by Kyle Meeks
- **size:** 5 files (+67/-2)
- **score 7** - port the idea, the patch will not apply
- **triage:** feature | value 3 | effort 2 | risk 2 | applies rewrite | REIMPLEMENT
- **why:** The backend exists, but the requested tab action does not. Add only UI wiring using the existing method and resource, avoiding duplicate backend and localization.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Menu exposure is useful, but only its UI wiring should be reimplemented against the existing method; this commit is not directly landable.
  - dissent - gemini: REJECT - The backend method already exists in our fork. This commit would cause merge conflicts and code duplication, requiring a clean manual reimplementation.

### `199b1f362b` docs(sftp): reshape the change to a dual-pane file manager with a transfer queue

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/199b1f362b0e4203c5f322b752d1e1169a2802f1) by Jason Finch
- **size:** 2 files (+152/-54)
- **score 7** - port the idea, the patch will not apply
- **triage:** docs | value 2 | effort 1 | risk 1 | applies rewrite | WATCH
- **why:** Spec-only for dual-pane SFTP manager with transfer queue; we have single file-transfer window (ab1be61fd). Idea worth watching once code lands, nothing to import.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This repository lacks OpenSpec, ISftpSession, and ISshCredentialResolver. The documentation patch assumes another fork's unfinished implementation and offers no standalone improvement to our existing transfer code.
  - dissent - gemini: REJECT - Commit only modifies third-party design specs in non-existent openspec directory; importing unfinished feature planning docs adds repository noise without functional value.

### `232fbf32ff` fix: multi-selection Enter opens connections, and task dialog buttons show focus (#52)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/232fbf32ffc324b12ceb8b8e251e4637742a8073) by Jason Finch
- **size:** 6 files (+112/-16)
- **score 7** - worth doing, needs work
- **triage:** bugfix | value 3 | effort 2 | risk 2 | applies conflict | IMPORT
- **why:** Our Enter handler still opens only SelectedNode; task dialog focus fallback and CommandButton focus ring absent. Skip docs-website hunk; verify against our multi-select tree and lab UI.
- **pre-approval:** **MANUAL-REVIEW** (codex:APPROVE / gemini:NEEDS_HUMAN / codex:NEEDS_HUMAN / gemini:NEEDS_HUMAN)
  - dissent - codex: NEEDS_HUMAN - Both gaps exist locally, supporting their value. However, the focus hunk targets a superseded layout path; selectively adapt it while preserving our current dialog logic.
  - dissent - gemini: NEEDS_HUMAN - Fixes align with focus and UX priorities but require separation into individual commits and path adaptation.

### `2d1411667e` 修复：容器的ID现保持与文件中一致

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2d1411667e60c4e001933d60f8de825dd2ac9213) by Hovn
- **size:** 1 files (+9/-1)
- **score 7** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 2 | risk 2 | applies rewrite | REIMPLEMENT
- **why:** Current XML loading discards serialized container IDs because CopyFrom cannot set get-only ConstantID. Reimplement constructor-based preservation with malformed-ID and round-trip tests.
- **pre-approval:** **MANUAL-REVIEW** (codex:NEEDS_HUMAN / grok:NEEDS_HUMAN)
  - dissent - codex: NEEDS_HUMAN - The defect is real and absent here, but land a tested fork-aware reimplementation covering Container and Entity instead of this stale patch.
  - dissent - grok: NEEDS_HUMAN - Real container ID-stability fix, but needs clean reimplementation and fork check.

### `3c2fd1770a` fix: drop the trailing comma from the port scan open/closed port columns

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/3c2fd1770ab8b9c2ee1ece2bb5dd6ad255f3bc09) by Jason Finch
- **size:** 1 files (+2/-26)
- **score 7** - worth doing, needs work
- **triage:** bugfix | value 2 | effort 1 | risk 1 | applies likely | IMPORT
- **why:** Our ScanHost.cs still has the trailing ', ' loop; string.Join is a trivial cosmetic fix for the port scan grid.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - HEAD already implements both properties with identical string.Join expressions in ScanHost.cs; importing this commit adds no useful change.
  - dissent - gemini: REJECT - The exact change is already implemented verbatim in our repository, making this proposed import completely redundant.

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

### `53451f91f5` Default the main window to 90% of the screen

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/53451f91f5195273903d0ac51e92ec1ee45e13ab) by local
- **size:** 1 files (+40/-14)
- **score 7** - port the idea, the patch will not apply
- **triage:** feature | value 2 | effort 1 | risk 1 | applies conflict | REIMPLEMENT
- **why:** Ours falls back to designer size on first run; #171 MainWindowPlacement restructured this method. Add centred 90% working-area default only when nothing saved.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - SettingsLoader already corrects stale restore coordinates through MainWindowPlacement. The proposed replacement omits that correction; default sizing needs an adapted patch preserving existing behavior.
  - dissent - gemini: REJECT - The 90% heuristic provides questionable value over standard defaults while directly reverting our #171 multi-monitor placement logic.

### `556127ef31` fix: keep Options usable when a settings secret cannot be decrypted (#18)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/556127ef31c31603313145d28cbfc63fec4378fa) by Jason Finch
- **size:** 24 files (+158/-31)
- **score 7** - worth doing, needs work
- **triage:** bugfix | value 3 | effort 2 | risk 2 | applies conflict | WATCH
- **why:** Options page survives undecryptable settings secret; no ErrorOptionsPageSettingsNotLoaded here. Symptom unconfirmed in our fork; jafin's crypto layers differ. Verify before reimplementing.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - Recovery is useful, but SaveOptions saves every page; decryption failure skips subsequent settings. The patch neither preserves untouched values nor tests this data-loss path.
  - dissent - gemini: NEEDS_HUMAN - Preventing Options window failure on undecryptable secrets is valuable, but our settings architecture differs; it requires maintainer judgment and local reimplementation rather than direct cherry-pick.

### `6c1dbeaf82` fix: make ConnectionsFileResolver sole-candidate test deterministic

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6c1dbeaf8209f23145c456805bb5cd72d03cff32) by Jason Finch
- **size:** 1 files (+22/-22)
- **score 7** - worth doing, needs work
- **triage:** chore | value 2 | effort 1 | risk 1 | applies likely | IMPORT
- **why:** Our test still named StartupConnectionPathReturnsSavedPathWhenItIsTheSoleCandidate and scans real OS paths; host-dependent flake. Rewrite against ConnectionsFileResolver.Resolve is deterministic. Test-only.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - ConnectionsFileResolverTests.Resolve_SingleCandidate_ReturnsIt_WithoutPrompting already verifies candidate identity and absence of prompting. The import duplicates that coverage while removing the startup pipeline assertion.
  - dissent - gemini: REJECT - Resolve with a single candidate is already tested in ConnectionsFileResolverTests. Replacing the service startup test with a redundant unit test provides negative value.

### `6e684bc7f9` Fix the spin buttons and the crowded backup page

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6e684bc7f9fe7b5e711598862639629e1e6e815a) by Lovasz Laszlo
- **size:** 3 files (+73/-12)
- **score 7** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 2 | risk 2 | applies conflict | REIMPLEMENT
- **why:** Our mrngNumericUpDown still hard-codes 96-DPI SetBounds; DPI-aware LayOutButtons is real. Skip Hungarian resx edits and BackupPage designer (ours already resized).
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - Layout improvements are useful and absent locally, but the shared-control DPI regression makes this commit unsuitable for pre-approval.
  - dissent - gemini: NEEDS_HUMAN - The DPI-aware spin button layout is valuable for our high-DPI goals, but requires cherry-picking and reimplementation rather than landing the commit directly.

### `6eabb55063` fix: remove the dead space between the timeout row and the progress bar

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6eabb55063017b51f2a2a0097f9714508ae98b19) by Jason Finch
- **size:** 1 files (+4/-2)
- **score 7** - worth doing, needs work
- **triage:** bugfix | value 2 | effort 1 | risk 1 | applies likely | IMPORT
- **why:** Our PortScanWindow.Designer still has 159F row; 4-line layout fix, verify visually in built app.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - HEAD already contains pnlIp height 110, pnlMain first-row height 116F, and the identical explanatory comment; importing adds no value.
  - dissent - gemini: REJECT - Already cherry-picked into main as commit 2c3f49c22; PortScanWindow.Designer.cs already has these exact changes.

### `6ecce9b6be` fix(notifications): render the message text on startup messages

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6ecce9b6becc1bc5f81b8190157996e009ae292f) by Jason Finch
- **size:** 4 files (+93/-11)
- **score 7** - worth doing, needs work
- **triage:** bugfix | value 3 | effort 2 | risk 2 | applies likely | IMPORT
- **why:** We have the #53 _pendingItems deferral but not the posted flush; startup messages likely render timestamp-only here too. Reproduce first.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork renders message text in its sole column. The proposed fix targets a different layout and requires prerequisite changes, making direct import unsuitable.
  - dissent - gemini: REJECT - Our fork uses a single-column notification list where Text is the message itself; the subitem rendering bug does not exist here.

### `7d47769f49` Put the units back on the Hungarian spin box labels

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7d47769f49cd4658beeafc4f3a482ff763784a15) by Lovasz Laszlo
- **size:** 1 files (+3/-3)
- **score 7** - port the idea, the patch will not apply
- **triage:** docs | value 2 | effort 1 | risk 1 | applies conflict | REIMPLEMENT
- **why:** Hungarian AutoSaveEvery label lacks units in ours; RdpOverallConnectionTimeout/RdpReconnectCount keys absent in our hu.resx. Cherry-pick only applicable string.
- **pre-approval:** **MANUAL-REVIEW** (codex:NEEDS_HUMAN / gemini:APPROVE / codex:NEEDS_HUMAN / gemini:NEEDS_HUMAN)
  - dissent - codex: NEEDS_HUMAN - The clarification matches autosave behavior and improves usability, but importing this exact commit requires adapting missing RDP entries; Reviewer A's compatibility objection is confirmed.
  - dissent - gemini: NEEDS_HUMAN - Helpful UI label clarifications, but patch conflicts with our truncated Language.hu.resx and requires manual adaptation rather than clean import.

### `85056294af` fix: stop connection edits from being lost without a word (#10)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/85056294af97fa3ad7316a36097e0d38f67a9500) by Jason Finch
- **size:** 17 files (+1067/-99)
- **score 7** - port the idea, the patch will not apply
- **triage:** bugfix | value 4 | effort 3 | risk 3 | applies conflict | REIMPLEMENT
- **why:** Save-failure propagation already ours (b7126a004 TrySave). Still open here: Shutdown.SaveConnections drops Unassigned default to 'never' and never flushes the 2s debounced save. Port those two only.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Durability improvements are useful, but several fixes already exist here. This broad import requires selective adaptation to preserve newer SQL safeguards; it is unsuitable for preapproval.
  - dissent - gemini: REJECT - Core save failure propagation is already implemented via TrySave. Do not import; surgically reimplement only the shutdown unassigned frequency fallback and debounce flush.

### `8ec20405e6` test: run the WinForms tab tests on a real message loop with a reliable timeout

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8ec20405e6151c033b6d9193e57cb5cd74bddca2) by Jason Finch
- **size:** 1 files (+60/-11)
- **score 7** - worth doing, needs work
- **triage:** refactor | value 2 | effort 1 | risk 1 | applies likely | IMPORT
- **why:** Our RunWithMessagePump is a bare STA thread with no Application.Run; Interrupt cannot unwind a wedged UI thread. Test-only, same file, clean apply.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - DockPaneStripNGTests.cs already contains the proposed implementation, with no working-tree differences. Importing it adds no value to this fork.
  - dissent - gemini: REJECT - Commit 8ec20405e is already present in this branch and identical to current code in DockPaneStripNGTests.cs.

### `90aa5e8c05` feat: double the default width of the port scan Hostname column (130 -> 260)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/90aa5e8c05dcf7e0c85429a9312def4ca6456279) by Jason Finch
- **size:** 1 files (+1/-1)
- **score 7** - worth doing, needs work
- **triage:** feature | value 2 | effort 1 | risk 1 | applies likely | IMPORT
- **why:** Ours still 130; FQDNs truncate. One-line designer change, matches our bolder-UI preference.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Verified in both HEAD and the working tree. The proposed import adds no behavior or usability benefit.
  - dissent - gemini: REJECT - Already cherry-picked into this repository in commit 35055177e792f2c6c1d4c909c284a7b1a9074f9e.

### `9c4b85f18a` fix: set temp key-file attribute via File.SetAttributes

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9c4b85f18ab51f04b455d198cbf86b284dd6c3f8) by Eran Markus
- **size:** 1 files (+1/-1)
- **score 7** - port the idea, the patch will not apply
- **triage:** bugfix | value 2 | effort 1 | risk 1 | applies rewrite | REIMPLEMENT
- **why:** Replaces redundant throwaway FileInfo instantiation with clean, direct File.SetAttributes call in two PuttyBase temp key generation paths.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / grok:REJECT)
  - dissent - codex: REJECT - It provides no correctness or stability gain; reimplementation would be churn because both APIs set the same attribute and current code has zero warnings.
  - dissent - grok: REJECT - Original object-initializer already sets attributes on disk; pure idiom tweak, not a real fix.

### `9c5c4484ce` perf: stop the port scan flooding the UI thread; add common-ports button

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9c5c4484ce6059312dfc4a75dcf2bb89675bf3be) by Jason Finch
- **size:** 3 files (+148/-29)
- **score 7** - port the idea, the patch will not apply
- **triage:** perf | value 3 | effort 2 | risk 2 | applies conflict | REIMPLEMENT
- **why:** Our MessageCollector still unlocked with per-item RemoveAt trim; scan adds messages per host. Take lock+RemoveRange; common-ports button optional, designer diff will conflict.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork already contains the message-list locking and batched scan updates; common ports are available through its newer port-mode selector.
  - dissent - gemini: REJECT - This commit was already cherry-picked as 83015a272. Its UI changes were subsequently replaced by a dedicated port mode selector in 7f3afcc18.

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

### `b1e1dcfe5d` fix: stop docking the placeholder window a handed-off console leaves behind (#48)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b1e1dcfe5db54b1acd6d191c57dd3980a38ab3cf) by Jason Finch
- **size:** 5 files (+169/-27)
- **score 7** - worth doing, needs work
- **triage:** bugfix | value 3 | effort 2 | risk 2 | applies likely | IMPORT
- **why:** PseudoConsoleWindow absent in our tree; ExternalProcessProtocolBase.cs exists. Win11 Windows Terminal handoff docks 0x0 placeholder. Small, tested, with docs note.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:APPROVE / codex:REJECT / gemini:APPROVE)
  - dissent - codex: REJECT - The [patch](https://github.com/jafin/mRemoteNG/commit/b1e1dcfe5db54b1acd6d191c57dd3980a38ab3cf) fixes an absent filter, but needs headless test adaptation and lifecycle coverage proving failed docking preserves the process.

### `c535880a14` feat: single address field and port mode selector for the port scan

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c535880a14c0395a18dfe24a3c22d4c1853dfe2a) by Jason Finch
- **size:** 6 files (+627/-324)
- **score 7** - worth doing, needs work
- **triage:** feature | value 3 | effort 2 | risk 2 | applies conflict | IMPORT
- **why:** We still IPAddress.Parse two fields; CIDR/range/IPv6 parser with tests is self-contained. PortScanWindow diverged (timeout fix 8119ae123), expect merge work.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Current repository already contains both parsers and the complete single-address and port-mode UI. The proposed commit adds no new functionality.
  - dissent - gemini: REJECT - Already imported via cherry-pick 7f3afcc18 and further updated in f116697c4; attempting to re-import would cause merge conflicts.

### `d37a901670` fix: Options UI polish and task dialog button spacing (#45)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d37a901670287d284fb9cbffcd73c348d6a87f09) by Jason Finch
- **size:** 4 files (+9/-19)
- **score 7** - worth doing, needs work
- **triage:** chore | value 2 | effort 1 | risk 1 | applies likely | IMPORT
- **why:** Our BackupPage still has 11 Salmon debug BackColors; ConfigurationPage still 3 columns. Small designer-only cleanup, low risk.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork already sizes task dialogs dynamically for DPI and content. Removing cosmetic colors does not justify regressing the flexible configuration layout.
  - dissent - gemini: REJECT - TaskDialog button spacing is already handled dynamically in #198, and fixing TableLayoutPanel widths harms resizing; only the Salmon background removal is valid.

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

### `f634039a17` fix: offer the SSH transfer window for the protocols it can reach (#24)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f634039a177f9204454e5fd8680b3cce3ee50863) by Jason Finch
- **size:** 5 files (+131/-70)
- **score 7** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 2 | risk 2 | applies rewrite | REIMPLEMENT
- **why:** Our gate still SSH1|SSH2 at 4 call sites (ConnectionWindow:1489/1587, ContextMenu:1022/1062); SSH1 offered, OpenSSH withheld. No ProtocolFeature class here; add helper without SSHNative.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - OpenSSH transfer availability is useful and currently missing, but this commit requires prerequisite features. A small adapted fix should be reviewed separately.
  - dissent - gemini: REJECT - The fix concept is sound for OpenSSH and SSH1, but cannot land directly; it must be cleanly reimplemented for our fork's existing protocol types.

### `3567ecabb1` fix: validate the ports argument in the PortScanner constructor

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/3567ecabb18f190897415f43b4061f06fb270319) by Jason Finch
- **size:** 2 files (+86/-1)
- **score 6** - port the idea, the patch will not apply
- **triage:** bugfix | value 2 | effort 2 | risk 1 | applies conflict | REIMPLEMENT
- **why:** Our PortScanner ctor still does bare _ports.AddRange(ports) with no null/empty/range check; depends on jafin's PortListParser and Language keys, so re-do with local constants.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Already implemented in commit 6bb49727d and present at HEAD, including all proposed tests; this fork additionally validates the port-range overload.
  - dissent - gemini: REJECT - The exact validation logic and test cases are already committed on main; re-importing is redundant and would cause conflicts.

### `7746827c2b` fix: take a command-line switch value whole, whatever it contains (#41)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7746827c2b4af03f16d59fe482cb6f050bef70c9) by Jason Finch
- **size:** 13 files (+625/-154)
- **score 6** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 3 | risk 2 | applies conflict | REIMPLEMENT
- **why:** Bug confirmed here: CmdArgumentsInterpreter splitter regex `=|:` still splits a space-separated value like C:\path, silently dropping --cons. Our CommandLineParser diverged from jafin's; port the logic plus tests, skip openspec/docs-website.
- **pre-approval:** **MANUAL-REVIEW** (codex:NEEDS_HUMAN / gemini:NEEDS_HUMAN)
  - dissent - codex: NEEDS_HUMAN - The parser defect exists here and merits fixing, but the added missing-file behavior needs startup recovery review beyond the supplied unit tests.
  - dissent - gemini: NEEDS_HUMAN - Fixes real silent drops of drive-letter paths and host:port values, but diff cannot apply cleanly and requires manual porting without foreign documentation files.

### `95f621308b` fix: validate the ports in the port-range PortScanner constructor too

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/95f621308b5afe3d59675a5721fc186c663fca40) by Jason Finch
- **size:** 2 files (+59/-4)
- **score 6** - port the idea, the patch will not apply
- **triage:** bugfix | value 2 | effort 2 | risk 1 | applies rewrite | REIMPLEMENT
- **why:** Our PortScanner has no port validation at all (no PortListParser/ValidatePorts); the range ctor loop can allocate an absurd range. Small standalone guard worth adding with tests, independent of jafin's parser.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The fix supports stability, but repository inspection confirms it is already implemented and committed; importing this change adds no useful behavior or coverage.
  - dissent - gemini: REJECT - Already imported as commit 713ada425e375323de65b4556b054df303f0cd0d with identical logic and unit tests active on main.

### `9db6e8b09b` feat: put the port scan IP range on one line and tighten the layout

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9db6e8b09be0d332ba383b5d3489649ccd577b06) by Jason Finch
- **size:** 2 files (+75/-34)
- **score 6** - worth doing, needs work
- **triage:** feature | value 2 | effort 2 | risk 1 | applies likely | IMPORT
- **why:** Cosmetic PortScan layout (IP range one line, IPv6-wide boxes). Our Designer lacks pnlIpRange; self-contained, low risk, minor benefit.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This exact commit was already cherry-picked as 2f200077e and subsequently superseded by single-field range parsing; importing it again adds no value.
  - dissent - gemini: REJECT - Our fork already cherry-picked jafin's subsequent commit replacing separate start/end inputs with unified CIDR/range field, making this layout change completely obsolete.

### `08b056f698` Measure the panel, not the RDP control, when resizing the session

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/08b056f698587be31a2582070549e24dc7147fcb) by local
- **size:** 1 files (+25/-7)
- **score 5** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 2 | risk 3 | applies rewrite | REIMPLEMENT
- **our issue:** #177
- **why:** Core fix (never measure Control.Size) already ours: we use InterfaceControl.Size. Padding-aware ClientRectangle measurement is new; connection-frame padding may oversize session and re-show scrollbars. Candidate for #177.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Our DoResizeClient already measures InterfaceControl.Size, avoiding ActiveX feedback. Padding correction may help, but requires a separate patch compatible with our existing layout.
  - dissent - gemini: REJECT - Our fork already measures InterfaceControl instead of Control in DoResizeClient, making this patch redundant and structurally incompatible.

### `4b2768dcea` fix: claim the foreground for the main window at startup

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4b2768dcea09bbec5dc3f857d37f3163dd2ac0c4) by local
- **size:** 2 files (+54/-3)
- **score 5** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 2 | risk 3 | applies conflict | REIMPLEMENT
- **why:** Our FrmMain_Shown still plain Activate/SetForegroundWindow; AttachThreadInput already declared. Reasonable startup foreground fix, but #143/#168 history: attach must not be blindly detached, verify in lab.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - Startup focus fits our priorities, but this patch requires adaptation to existing interop declarations and verification that forced activation preserves protocol input routing.
  - dissent - gemini: NEEDS_HUMAN - Fixes main window losing foreground at startup, but needs manual reimplementation and lab testing because NativeMethods already defines these APIs.

### `9216eeec3f` Never reconnect an RDP 8 session just to resize it

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9216eeec3fc53ba08f2ea15804a58983916a8f6e) by Lovasz Laszlo
- **size:** 2 files (+24/-0)
- **score 5** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 2 | risk 3 | applies conflict | REIMPLEMENT
- **our issue:** #177
- **why:** Our RdpProtocol8.DoResizeClient still calls Reconnect on FitToWindow. Real session-drop bug, but RdpVersion=Highest ships RdpProtocol11 so only forced-RDC8 users hit it; our resize path diverged.
- **pre-approval:** **MANUAL-REVIEW** (codex:NEEDS_HUMAN / gemini:REJECT)
  - dissent - codex: NEEDS_HUMAN - The reconnect guard is absent here and useful, but silently disabling configured automatic resizing needs maintainer judgment and verification of the resulting display behavior.
  - dissent - gemini: REJECT - Silently disables FitToWindow on RDP 8; our fork already debounces resizes, supports SmartSize, and defaults to RDP 11, making this patch obsolete and regressive.

### `0045263765` Show auto-detected PuTTY path on Advanced options page

- **fork:** [k-meeks/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/004526376515164a858c98a9a1c782d04a28c33c) by Kyle Meeks
- **size:** 4 files (+79/-11)
- **score 4** - port the idea, the patch will not apply
- **triage:** feature | value 2 | effort 2 | risk 2 | applies conflict | REIMPLEMENT
- **why:** Small UX win: shows auto-detected PuTTY path in options. Our Designer/options pages diverged heavily; re-do by hand, not cherry-pick.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The fork already exposes the custom override and otherwise always launches bundled PuTTYNG.exe, so this UI is redundant, misleading, and upstream-specific.
  - dissent - gemini: REJECT - Our fork bundles `PuTTYNG.exe` and has not imported the unbundling candidate. This change is redundant and will break the build due to missing auto-detection dependencies.

### `23bc5b533a` docs: archive fix-command-line-value-parsing (#42)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/23bc5b533a708fc2ca070070fbe401246848c486) by Jason Finch
- **size:** 5 files (+106/-3)
- **score 4** - port the idea, the patch will not apply
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | WATCH
- **why:** openspec archive docs only, no code. But our CmdArgumentsInterpreter.cs:46 still splits on ':' so `--cons C:\path` breaks; track jafin's code commit, not this.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - CmdArgumentsInterpreter still splits separate path values on colons. This documentation-only commit provides no implementation fix or regression tests and introduces an unused OpenSpec archive.
  - dissent - gemini: REJECT - This commit only archives task specifications for a foreign workflow. Any actual parsing bug fix should be ported from code commits directly, not via openspec files.

### `404e7291d5` Fix native RDP launch when signing fails

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/404e7291d589796fae4332a40cae13b166dd3e7e) by sanay
- **size:** 1 files (+27/-5)
- **score 4** - port the idea, the patch will not apply
- **triage:** bugfix | value 2 | effort 2 | risk 2 | applies rewrite | WATCH
- **why:** Fixes a file (NativeRdpLauncher.cs) that does not exist here; only relevant if we ever adopt the native mstsc mode from 405520de.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:APPROVE / codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The repository has no NativeRdpLauncher or rdpsign integration; this isolated patch cannot improve its ActiveX RDP path. Thumbprint compatibility does not justify weakening signing.
  - dissent - gemini: REJECT - Our fork uses ActiveX RDP rather than external mstsc launching, so this commit targets non-existent infrastructure while silently bypassing configured RDP file signing.

### `938611f022` fix: close a PuTTY tab in one step, without PuTTY's own prompt

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/938611f02255a6e9bbe40fe59bffd8b588c85dd2) by Jason Finch
- **size:** 6 files (+247/-6)
- **score 4** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 3 | risk 3 | applies conflict | REIMPLEMENT
- **why:** Our TryClosePuttyGracefully just WaitForExit(1000); PuTTY warn-on-close double prompt real. But diff also touches ConnectionTab close semantics (disconnectOnly) overlapping #61/#172 work; port dialog-dismiss part only.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The fix supports protocol usability, but importing it adds no value: committed source already contains the PuTTY confirmation dismissal and disconnect-only tab handling.
  - dissent - gemini: REJECT - Commit 938611f02255 was already merged into our repository's main branch via PR #158.

### `a320091188` perf: batch the passes RemoveFilter runs when clearing the tree filter

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a32009118810e2237c029756814292f30a01343c) by Jason Finch
- **size:** 1 files (+33/-12)
- **score 4** - port the idea, the patch will not apply
- **triage:** perf | value 3 | effort 3 | risk 3 | applies rewrite | REIMPLEMENT
- **why:** Our RemoveFilter assigns ExpandedObjects, not RebuildAll; jafin's base fix (EnsureVisible index throw) absent too. Batch+rebuild worth doing; adjacent to #144/#149 row-index family.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - ConnectionTree.cs already contains every proposed change, committed as a93729afa with the same subject. Importing this commit adds no value.
  - dissent - gemini: REJECT - This exact change and commentary were already incorporated into ConnectionTree.cs in our fork, making this import completely duplicate.

### `ead4062d23` refactor(ui): remove the unused session split host

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ead4062d23408fdbd127e9c7813ad4a96004c410) by Jason Finch
- **size:** 4 files (+6/-118)
- **score 4** - port the idea, the patch will not apply
- **triage:** refactor | value 1 | effort 1 | risk 1 | applies rewrite | None
- **why:** Removes jafin's own SessionHost/SplitContainer scaffolding (SFTP side panel) that never existed in our fork; nothing to remove here.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - HEAD already parents InterfaceControl directly to the tab and contains no session split, side-panel APIs, or NotifyHostResized. The OpenSpec path is also absent.
  - dissent - gemini: REJECT - Our fork already parents InterfaceControl directly to connectionContainer without SessionHost or SplitContainer. There is nothing to import or remove.

### `ec9f699a2c` Say which terminal this is while PuTTY is still around

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ec9f699a2cc3f09e0fa2e036206bc9337ebcf6fe) by local
- **size:** 1 files (+4/-0)
- **score 4** - port the idea, the patch will not apply
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | None
- **why:** Temporary debug banner in fork-only xterm.js SSH terminal; we have no Resources/Terminal/terminal.html or native SSH.NET terminal.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork uses PuTTY and Windows OpenSSH for interactive SSH. A temporary comparison banner for another fork's terminal provides no useful import.
  - dissent - gemini: REJECT - Temporary debug banner for an absent experimental xterm.js/SSH.NET terminal feature; our fork uses PuTTY for SSH connections.

### `215eb6c564` Adapt native RDP serializer to fork model

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/215eb6c564eacd35cd55dc124cfd90d9232fad7a) by sanay
- **size:** 1 files (+60/-29)
- **score 3** - port the idea, the patch will not apply
- **triage:** feature | value 3 | effort 4 | risk 3 | applies rewrite | WATCH
- **why:** RdpFileSerializer.cs does not exist here; commit adapts it to that fork's model via reflection-style lookups. Native .rdp export is interesting but needs whole feature, typed against our ConnectionInfo.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Our fork uses RdpConnectionSerializer and already exposes these properties. This compatibility patch offers no demonstrated benefit and requires prerequisite code outside the proposed import.
  - dissent - gemini: REJECT - Modifies a file absent from our codebase while replacing strongly-typed ConnectionInfo access with string-based reflection shims. Has no standalone value.

### `5293862e10` feat: make port scan parallelism configurable from the UI

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/5293862e1066e03c86909fccf7d33bdf478f433c) by Jason Finch
- **size:** 3 files (+66/-9)
- **score 3** - port the idea, the patch will not apply
- **triage:** feature | value 2 | effort 3 | risk 2 | applies rewrite | WATCH
- **why:** Depends on jafin's Parallel.ForEachAsync scanner rewrite; our PortScanner is sequential thread with no MaxConcurrentHosts. Nothing to configure without prerequisite.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Repository already exposes the UI control, passes its value to PortScanner, and clamps concurrency to 1–128 with default 64; importing adds no functionality.
  - dissent - gemini: REJECT - This commit is already present in our tree as commit f55b7b276, having been cherry-picked previously. No import is needed.

### `ba164119c1` feat: localize the port scan panel

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ba164119c17eac43c6536d4012963df68f6f3f52) by Jason Finch
- **size:** 6 files (+271/-29)
- **score 3** - port the idea, the patch will not apply
- **triage:** feature | value 2 | effort 3 | risk 2 | applies rewrite | WATCH
- **why:** Our PortScanWindow still hardcodes English; but commit depends on jafin's IpRangeParser/PortListParser (CIDR/IPv6 rewrite) absent here. Only resx strings portable.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Current source already contains the localized panel, parser hints, and scanner errors. Reimporting offers no additional benefit.
  - dissent - gemini: REJECT - Already cherry-picked into main as commit f116697c41de4ef8c4df5a27fca4c7a22382afe9 with identical changes across all 6 files.

### `015c152817` Use connection names for temporary RDP files

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/015c15281728dbf87fc4fb882492ccd0de2ff515) by sanay
- **size:** 1 files (+32/-1)
- **score 2** - port the idea, the patch will not apply
- **triage:** feature | value 2 | effort 2 | risk 3 | applies rewrite | WATCH
- **why:** Temp .rdp filenames from connection name: minor diagnostic nicety, but leaks connection names into LocalAppData filenames. Their TemporaryRdpFileStore is fork-local.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork connects through embedded RDP ActiveX. Importing this isolated filename change provides no demonstrated benefit to its existing RDP implementation.
  - dissent - gemini: REJECT - Targets a fork-private external mstsc launcher absent from our repository; cannot compile here and offers no value to our embedded ActiveX RDP architecture.

### `98a262b7b7` fix: recognise the embedded PuTTY window as our own foreground

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/98a262b7b79fc3a6736234e9b6caa31a1ef7e047) by local
- **size:** 2 files (+18/-5)
- **score 2** - port the idea, the patch will not apply
- **triage:** bugfix | value 2 | effort 2 | risk 3 | applies rewrite | WATCH
- **why:** ApplicationIsInForeground does not exist in our frmMain; #168 solved differently (bf99b7ad9, reporter-confirmed). GA_ROOT idea useful only if a PuTTY-foreground regression resurfaces.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - PuttyBase.Focus already keeps the host foreground and repairs PuTTY-owned foreground state. ApplicationIsInForeground does not exist here, so importing this patch requires adaptation without demonstrated benefit.
  - dissent - gemini: REJECT - Our fork already solved PuTTY foreground handling in #168 (bf99b7ad9) at the source in PuttyBase.Focus. ApplicationIsInForeground does not exist here.

### `c31a3c8161` feat(sftp): make transfers reachable, add mutation commands and theming

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c31a3c8161251515726eaedf6f46933946e9f252) by Jason Finch
- **size:** 8 files (+829/-14)
- **score 2** - port the idea, the patch will not apply
- **triage:** feature | value 3 | effort 5 | risk 3 | applies rewrite | WATCH
- **why:** Increment on jafin's SFTP browser panel; no FileTransfer/FileManagerTab in our tree. Only importable as the whole feature chain once it stabilises.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The [commit](https://github.com/jafin/mRemoteNG/commit/c31a3c8161251515726eaedf6f46933946e9f252) extends an SFTP architecture absent locally. Its usefulness does not justify pre-approving the larger integration required.
  - dissent - gemini: REJECT - Commit modifies non-existent files from an unimported experimental SFTP panel branch; outside fork roadmap and cannot compile in isolation.

### `d239f5b9e5` feat(sftp): add the pane abstraction, local browser and transfer queue

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d239f5b9e5f776df781d2263cb31b63f2185b24e) by Jason Finch
- **size:** 11 files (+1656/-12)
- **score 2** - port the idea, the patch will not apply
- **triage:** feature | value 3 | effort 5 | risk 3 | applies rewrite | WATCH
- **why:** Dual-pane SFTP browser foundations (browser abstraction, queue, tests) are self-contained and tested, but only half a feature; wait for jafin's series to complete, then evaluate as a whole.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Requires missing prerequisite commits and adds substantial unwired feature groundwork, exceeding quick pre-approval scope. [Diff](https://github.com/jafin/mRemoteNG/commit/d239f5b9e5f776df781d2263cb31b63f2185b24e.patch)
  - dissent - gemini: REJECT - The change adds a massive new feature which expands the maintenance surface significantly, violating the preference for small, verifiable changes.

### `f8b3b93411` Anchor the RDP control to the panel instead of assigning its size

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f8b3b934116f748df84a075f36a9f07d55cb228b) by local
- **size:** 2 files (+15/-24)
- **score 2** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 3 | risk 4 | applies rewrite | WATCH
- **our issue:** #177
- **why:** Anchor-instead-of-Size idea targets our #177 symptom, but diff is against their AutoScroll variant; our SetResolution/DoResizeControl differ and lab trace showed resize sizes are correct. Test idea in lab, don't port.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Our fork already uses Dock.Fill and debounced session resizing. The patch targets absent FitToWindow code; local benefit is unproven and adaptation requires more than a straightforward import.
  - dissent - gemini: REJECT - Patch targets lovaszlaszlo's divergent AutoScroll implementation and does not apply. Our fork relies on DockStyle.Fill and UpdateSessionDisplaySettings; anchoring introduces regressions.

### `f92a9cc609` security: stop writing every debug message to the log by default

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f92a9cc609c15c3afcbe7edbe958f77406e81d78) by Jason Finch
- **size:** 8 files (+289/-59)
- **score 2** - port the idea, the patch will not apply
- **triage:** security | value 2 | effort 2 | risk 3 | applies conflict | REIMPLEMENT
- **why:** Our TextLogMessageWriterWriteDebugMsgs already defaults False; only new bit is %USERPROFILE% redaction in the log path. Small, but touches diagnostics (tripwire, human review).
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The [patch](https://github.com/jafin/mRemoteNG/commit/f92a9cc609c15c3afcbe7edbe958f77406e81d78.patch) addresses a real debug-filtering gap, but needs a log4net-native port before approval.
  - dissent - gemini: REJECT - Debug logging already defaults to false here; the commit cannot apply cleanly, carries alien openspec files, and path sanitization requires dedicated reimplementation.

### `8f39c112b5` fix(rdp): reapply performance flags and input finalizer on all reconnect paths

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/8f39c112b57865efa6c34cd735c1b35394c203dc) by Claude Code
- **size:** 3 files (+18/-5)
- **score 1** - port the idea, the patch will not apply
- **triage:** bugfix | value 2 | effort 3 | risk 3 | applies rewrite | WATCH
- **why:** Reapplying performance flags on mstscax auto-reconnect is a plausible real fix, but patch depends on fork-only view-only/input-finalizer infrastructure we lack. Note idea, not code.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / grok:NEEDS_HUMAN)
  - dissent - codex: REJECT - Exact commit cannot land: RdpProtocol6 was deleted, passive helpers are absent, RdpProtocol8 is refactored, and no tests or reproducible evidence are provided.
  - dissent - grok: NEEDS_HUMAN - Reapplying pFlags on reconnect is useful, but diff is fork-specific and needs local path checks.

### `7e126049b3` feat(sftp): transfer directories recursively, with one overwrite choice

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7e126049b31f49b2100f69e6a20c704154fd7029) by Jason Finch
- **size:** 26 files (+2555/-22)
- **score 0** - port the idea, the patch will not apply
- **triage:** feature | value 3 | effort 5 | risk 4 | applies rewrite | WATCH
- **why:** Builds on jafin's SFTP/FileTransfer subsystem (ISftpSession, SftpSession) which our fork lacks entirely; 2555 lines depend on it. Track jafin's SFTP branch as a whole.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - Recursive SFTP transfers are useful, but this commit requires substantial prerequisite architecture and integration work; it is not a small, independently verifiable import.
  - dissent - gemini: NEEDS_HUMAN - Recursive SFTP transfer is valuable, but the massive 2500+ line footprint requires manual review to ensure it aligns with existing file transfer architecture.

### `8ded8c7535` Native SSH: take the appearance from the named PuTTY session

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8ded8c7535559aef80bdcae609b431df7a3eb8d6) by local
- **size:** 4 files (+146/-2)
- **score 0** - port the idea, the patch will not apply
- **triage:** feature | value 2 | effort 4 | risk 3 | applies rewrite | WATCH
- **why:** Reads PuTTY session font/colours for an xterm.js SSHNative protocol we lack (no ProtocolSshNative, terminal.html). Idea sound; only relevant if we ever add a WebView2 terminal.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork uses PuTTY-backed SSH and lacks ProtocolSshNative and terminal.html. Importing this appearance change requires broader prerequisite work outside the proposed commit.
  - dissent - gemini: REJECT - Our fork does not have the WebView2/xterm.js native SSH implementation that this patch modifies, making the change inapplicable and broken.

### `952dd3b37c` Native SSH: verify the host key and support private keys

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/952dd3b37cb18f9a1c39fabb986d150d7940216a) by local
- **size:** 3 files (+253/-5)
- **score 0** - port the idea, the patch will not apply
- **triage:** security | value 2 | effort 4 | risk 3 | applies rewrite | WATCH
- **why:** Host-key verification for ProtocolSshNative, which our fork does not have (SSH1/SSH2/OpenSSH only). Valuable only if we ever adopt a native SSH.NET terminal; note reusing password as passphrase and SSHOptions parsing.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork has no SSHNative implementation; OpenSSH already supports PrivateKeyPath. Import requires missing infrastructure, duplicates existing functionality, and adds unsafe authentication behavior without tests.
  - dissent - gemini: REJECT - Our fork uses PuTTY and lacks ProtocolSshNative entirely; this change cannot apply without importing an entire separate native SSH subsystem.

### `b0b8020946` fix(sftp): wire the agent provider, and stop mangling server paths

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b0b80209466e4d6e4b341c767da6f86fad61c402) by Jason Finch
- **size:** 4 files (+79/-3)
- **score 0** - port the idea, the patch will not apply
- **triage:** bugfix | value 1 | effort 3 | risk 2 | applies rewrite | WATCH
- **why:** Fix inside jafin's SFTP feature (no mRemoteNG/Connection/Sftp here). Backslash-preservation point is sound; bundle with feature import if SFTP browser ever evaluated.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Our SFTP implementation uses SecureTransfer directly. This patch depends on another fork's architecture; importing it requires substantial prerequisite work beyond a small maintenance fix.
  - dissent - gemini: REJECT - Our fork lacks the SFTP file manager subsystem entirely; this fix cannot apply standalone and should only be evaluated alongside the full feature.

### `b939635404` feat(sftp): add the dual-pane file manager tab

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b939635404025d16afbc64b0f7de2b4de3baf3f1) by Jason Finch
- **size:** 12 files (+1610/-6)
- **score 0** - port the idea, the patch will not apply
- **triage:** feature | value 3 | effort 5 | risk 4 | applies rewrite | WATCH
- **why:** Dual-pane SFTP file manager, 1610 lines over 12 files, depends on IFileSystemBrowser and earlier openspec commits absent here. Attractive later, needs full series plus security review of file transfer; not now.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - It cannot be imported independently; transfer actions also lack UI wiring. This incomplete 1,610-line feature exceeds a small, quickly verifiable maintenance change.
  - dissent - gemini: REJECT - Cannot compile standalone without unimported prerequisite commits. It is a massive speculative UI rewrite duplicating our existing file transfer functionality against fork priorities.

### `cb423ff6ec` security: stop backup recovery misreporting a key it cannot unwrap (#35)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/cb423ff6ecf8949d2dd730cacba61f55b082c6ba) by Jason Finch
- **size:** 6 files (+322/-11)
- **score 0** - port the idea, the patch will not apply
- **triage:** security | value 2 | effort 4 | risk 3 | applies rewrite | WATCH
- **why:** Depends on jafin's per-file key / KeyProtectionException feature absent here; our loader has no unwrap path, so bug does not exist yet. Revisit if we adopt per-file keys.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Its security aim fits, but importing it requires a separate per-file key implementation; this is not a self-contained, quickly verifiable fix.
  - dissent - gemini: REJECT - The fix addresses an issue in a per-file key encryption subsystem our fork has not adopted; without that underlying architecture, this patch is inapplicable.

### `39aa9177e0` fix(sftp): attach the pane's icons where the list actually looks

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/39aa9177e059dc7ed421c7a4d151f7a90b23d1a1) by Jason Finch
- **size:** 2 files (+112/-7)
- **score -1** - port the idea, the patch will not apply
- **triage:** bugfix | value 1 | effort 4 | risk 2 | applies rewrite | WATCH
- **why:** Fixes jafin's SFTP FilePaneControl, which we do not have; only the OLV SetSmallImageList-vs-property lesson transfers. Watch the file-transfer feature itself.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The ObjectListView fix is technically sound, but this fork uses SSHTransferWindow and SecureTransfer. The proposed patch cannot apply independently or benefit existing transfer functionality.
  - dissent - gemini: REJECT - Cannot be imported standalone because FilePaneControl belongs to jafin's unmerged SFTP feature branch which is absent from our codebase.

### `127570bac6` feat(ui): host connection sessions in a split so a side panel can share the tab

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/127570bac6d10d312d1461a3a6cb755a16bee039) by Jason Finch
- **size:** 8 files (+216/-27)
- **score -2** - port the idea, the patch will not apply
- **triage:** feature | value 2 | effort 4 | risk 4 | applies rewrite | WATCH
- **why:** Groundwork for jafin's SFTP side panel: wraps sessions in a split, reparents InterfaceControl. Touches handle/reparent paths relevant to #182/#177; only worth it if we adopt the SFTP browser panel.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The commit leaves ConnectionWindow unchanged, so its close handler returns early under the new hierarchy. This introduces protocol lifecycle regressions for speculative side-panel infrastructure.
  - dissent - gemini: REJECT - Speculative UI refactoring for an unmerged SFTP side panel; introduces complex hierarchy changes and high protocol stability risks without delivering standalone user value.

### `2a693c85c2` Added SSH Tunnel via SSH_DotNet

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2a693c85c2525ff21e0f35968f9a0745dc612022) by Dawie Joubert
- **size:** 14 files (+1778/-89)
- **score -2** - port the idea, the patch will not apply
- **triage:** feature | value 3 | effort 5 | risk 5 | applies rewrite | WATCH
- **why:** Native SSH.NET forwarding is potentially valuable, but this untested patch depends on an absent protocol and rewrites obsolete tunnel logic; monitor, do not port.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - It adds an untested parallel SSH stack and omits SQL/MariaDB persistence, conflicting with stability, storage consistency, and quick verification requirements.
  - dissent - gemini: REJECT - We do not have the SSH_DotNet protocol implemented. Importing this will break the build and introduces excessive complexity.

### `42f04f6eb6` Native SSH: do not lose the message when something goes wrong

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/42f04f6eb66fd55845ae5da9176520579ba13c4d) by local
- **size:** 1 files (+16/-8)
- **score -2** - port the idea, the patch will not apply
- **triage:** bugfix | value 1 | effort 5 | risk 2 | applies rewrite | WATCH
- **why:** ProtocolSshNative (WebView2 terminal) does not exist here; patch only meaningful if that whole feature is ever imported.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Our SSH2 uses PuttyBase and OpenSSH uses ConsoleControl. This WebView2 terminal patch cannot improve either existing implementation as a standalone import.
  - dissent - gemini: REJECT - Our fork uses PuTTY and OpenSSH; ProtocolSshNative is absent, so this commit cannot apply without importing an entire unsupported subsystem.

### `932e6f6116` Enhance connection handling and UI features

- **fork:** [lthobois/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/932e6f611674e6227db18d977f67a1b577af25a2) by Loïc THOBOIS
- **size:** 15 files (+732/-138)
- **score -2** - port the idea, the patch will not apply
- **triage:** feature | value 2 | effort 4 | risk 4 | applies rewrite | WATCH
- **why:** Mixed bag: new inheritance props (ExternalAddressProvider, RDP StartProgram, gateway token), notification detail, plus personal junk (.vscode, WorldOfFanXP.xml). Partly overlaps our upstream ports; cherry-pick only if users ask.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - A 732-line mixed, untested commit also bypasses notification filters, leaks writer subscriptions, duplicates shipped UI/retry features, adds untranslated labels, and uses noncanonical tooling.
  - dissent - gemini: REJECT - This is a mixed bag of personal settings, French locale scripts, and features already integrated or overlapping with our upstream ports. Not suitable for import.

### `718f5c28c1` feat(sftp): reconnect a dropped session, and make the listing readable

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/718f5c28c1300e2745e3a95700bf5787d8c03f61) by Jason Finch
- **size:** 21 files (+1835/-47)
- **score -3** - port the idea, the patch will not apply
- **triage:** feature | value 2 | effort 5 | risk 4 | applies rewrite | WATCH
- **why:** Builds on jafin's SFTP/FileTransfer subsystem (mRemoteNG/Connection/Sftp, FileTransfer/) which our fork does not have; no standalone import possible.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork uses SecureTransfer and SSHTransferWindow; the modified session, controller, pane, and tab classes do not exist here. The commit cannot stand alone.
  - dissent - gemini: REJECT - The commit modifies non-existent files (+1835 lines) from an external SFTP subsystem that cannot be imported standalone and introduces unverified file transfer complexity.

### `df13f1685d` Sign native RDP files in-process with CMS

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/df13f1685d1cdcffadd756b73f7210878ad60eaa) by sanay
- **size:** 1 files (+265/-329)
- **score -5** - port the idea, the patch will not apply
- **triage:** feature | value 2 | effort 5 | risk 5 | applies rewrite | WATCH
- **why:** Rewrites NativeRdpFileSigner.cs we do not have; native mstsc launch + CMS signing is a fork-specific feature. Crypto code, tripwire-gated. Niche demand.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Repository searches found no signer or signing call site. Adoption requires prerequisite infrastructure and RDP signature compatibility tests beyond this single-file commit.
  - dissent - gemini: REJECT - Target file NativeRdpFileSigner.cs does not exist in our fork; we use AxMSTSCLib rather than native mstsc file launching and signing.

## Quarantine - security review required before anything else

### `692a01f049` security: fix Terminal protocol command injection (#3335)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/692a01f049612fe55bc1fa04902869819b5f3f0f) by Jason Finch
- **size:** 2 files (+301/-34)
- **score 13** - security review required (critical)
- **triage:** security | value 5 | effort 2 | risk 2 | applies conflict | IMPORT
- **why:** Our Terminal protocol still runs cmd /K ssh with raw Hostname/Username (lines 56-73): command injection. Upstream c5ad2eb8a+b731546b0 not on main. Land via tripwire review.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Current HEAD already launches ssh.exe directly, normalizes PATH, and includes these regression tests; shared ConsoleArgument validation additionally rejects hostname '@' and '/'.
  - dissent - gemini: REJECT - Already ported upstream #3335 in commit 6f4e5dceb with extra '@' and '/' validation, plus followup b31c8190b for OpenSSH and WSL.
- **security flags:**
  - `process-exec` (critical) in `mRemoteNGTests/Connection/Protocol/ProtocolTerminalTests.cs` - added code spawns a process or evaluates a string as code

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

### `04a6c9cfc9` fix(notifications): stop losing startup messages and show info, warnings and times

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/04a6c9cfc98b7cefa87bf8396f67c2fa8ed4ea05) by Jason Finch
- **size:** 17 files (+690/-17)
- **score 9** - security review required (high)
- **triage:** bugfix | value 4 | effort 3 | risk 2 | applies conflict | REIMPLEMENT
- **our issue:** #175
- **why:** Startup messages lost before writers attach — exactly why #175/#178 loader errors go unseen. SubscribeAndReplay idea is sound; skip their settings-default and UI churn.
- **pre-approval:** **MANUAL-REVIEW** (codex:NEEDS_HUMAN / gemini:REJECT)
  - dissent - codex: NEEDS_HUMAN - Startup replay addresses a confirmed gap in this fork, but bundled notification defaults need maintainer judgement and focus-path verification before landing.
  - dissent - gemini: REJECT - The SubscribeAndReplay concept solves startup message loss, but direct import is bloated with UI churn and external artifacts; reimplement only the collector backlog logic cleanly.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/Ssh/Agent/SshNetAgentProvider.cs` - credential and crypto paths need human review regardless of intent

### `a921eef2ef` security: verify the SQL sentinel by its contents

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a921eef2ef1c21da952a42e935a2df740d67ac2a) by Jason Finch
- **size:** 10 files (+244/-20)
- **score 8** - security review required (high)
- **triage:** security | value 4 | effort 2 | risk 3 | applies conflict | REIMPLEMENT
- **why:** Real weakness: our PasswordAuthenticator still accepts any non-throwing AES-CBC decrypt (~1/256 wrong keys). Our ConnectionFileDefaults exists but lacks sentinels; tripwire path, human review.
- **pre-approval:** **MANUAL-REVIEW** (codex:APPROVE / gemini:NEEDS_HUMAN / codex:NEEDS_HUMAN / gemini:NEEDS_HUMAN)
  - dissent - codex: NEEDS_HUMAN - The SQL fix closes a real gap without dependencies, but CHARTER.md mandates human security review. Assess XML coverage, exclude openspec documents, and require full build/tests before import.
  - dissent - gemini: NEEDS_HUMAN - Fixes genuine AES-CBC sentinel bypass, but trips security tripwire requiring human override, openspec files must be stripped, and XML coverage should be addressed.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Config/Serializers/XmlConnectionsDecryptor.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Authentication/PasswordAuthenticator.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/ConnectionFileDefaults.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Security/Authentication/PasswordAuthenticatorTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/require-sql-master-password/proposal.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/require-sql-master-password/tasks.md` - credential and crypto paths need human review regardless of intent

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

### `0503c603ff` docs(openspec): propose the SFTP browser panel and the native SSH terminal

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/0503c603ff71ecf0a9a6133a2e254f634b550112) by Jason Finch
- **size:** 8 files (+903/-0)
- **score 7** - security review required (critical)
- **triage:** docs | value 2 | effort 1 | risk 1 | applies rewrite | WATCH
- **why:** Openspec proposal only, no code; we have no openspec dir. Native SSH.NET+xterm.js terminal and SFTP panel worth watching once jafin ships implementation commits.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This 903-line documentation proposal delivers no stability fix, leaves security decisions unresolved, and requires substantial adaptation rather than a small, readily verifiable import.
  - dissent - gemini: REJECT - Commit contains only external OpenSpec planning documents proposing a large speculative rewrite with vendored web dependencies conflicting with repository priorities.
- **security flags:**
  - `network-download` (critical) in `openspec/changes/add-sftp-browser-panel/design.md` - added code fetches remote content at build or run time
  - `network-download` (critical) in `openspec/changes/add-sftp-browser-panel/proposal.md` - added code fetches remote content at build or run time
  - `network-download` (critical) in `openspec/changes/add-sftp-browser-panel/specs/sftp-browser-panel/spec.md` - added code fetches remote content at build or run time
  - `network-download` (critical) in `openspec/changes/add-sftp-browser-panel/tasks.md` - added code fetches remote content at build or run time

### `05b63d011b` docs(openspec): archive the three SQL security changes (#58)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/05b63d011b7c15b3e0dc6fe3137ceff9d3e2f829) by Jason Finch
- **size:** 16 files (+350/-10)
- **score 7** - security review required (high)
- **triage:** docs | value 2 | effort 1 | risk 1 | applies rewrite | WATCH
- **why:** Archive of openspec docs, no code. Underlying SQL AEAD change is real: our SqlConnectionsSaver still hardcodes LegacyRijndael; track jafin's implementation commits instead.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This documentation archive supplies no runtime fixes. Our SQL saver still uses LegacyRijndaelCryptographyProvider, and this repository has no OpenSpec tree to archive.
  - dissent - gemini: REJECT - Internal OpenSpec workflow documentation specific to jafin's fork; contains no executable code or relevant changes for our repository.
- **security flags:**
  - `security-code` (high) in `openspec/changes/archive/2026-08-18-encrypt-sql-backend-with-aead/design.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-18-encrypt-sql-backend-with-aead/proposal.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-18-encrypt-sql-backend-with-aead/specs/sql-backend-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-18-encrypt-sql-backend-with-aead/tasks.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-18-refuse-writes-from-a-cached-fallback/specs/connection-file-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-18-require-sql-master-password/design.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-18-require-sql-master-password/proposal.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-18-require-sql-master-password/specs/sql-backend-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-18-require-sql-master-password/tasks.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/specs/connection-file-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/specs/sql-backend-encryption/spec.md` - credential and crypto paths need human review regardless of intent

### `05c5261ad8` docs(openspec): propose fixes for the upstream security audit

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/05c5261ad8bcae0590a542e7a44bcc11f012c73a) by Jason Finch
- **size:** 20 files (+1354/-0)
- **score 7** - security review required (critical)
- **triage:** docs | value 2 | effort 1 | risk 1 | applies rewrite | WATCH
- **why:** Proposal docs only. Themes (SQL AEAD, KDF hardening, default key mR3m) overlap our #2598 MasterPasswordGate work partially; evaluate the code commits, not these specs.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - SQL hardening is relevant, but this documentation bundle incorrectly claims XML loading decrypts no secrets; ProcessPendingDecrypts decrypts them during load. Adapt before considering import.
  - dissent - gemini: REJECT - The commit only introduces third-party OpenSpec design proposals rather than implementations; our fork does not use OpenSpec and requires its own design for security updates.
- **security flags:**
  - `security-code` (high) in `openspec/changes/encrypt-sql-backend-with-aead/design.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/encrypt-sql-backend-with-aead/proposal.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/encrypt-sql-backend-with-aead/specs/sql-backend-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/encrypt-sql-backend-with-aead/tasks.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/harden-connection-file-kdf/specs/connection-file-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/narrow-connection-password-exposure/proposal.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/narrow-connection-password-exposure/specs/connection-record-secrets/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/narrow-connection-password-exposure/tasks.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/replace-default-connection-file-key/specs/connection-file-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `env-secret-access` (critical) in `openspec/changes/retire-legacy-rijndael-for-settings/proposal.md` - added code reads credentials or CI secrets
  - `security-code` (high) in `openspec/changes/retire-legacy-rijndael-for-settings/specs/settings-secret-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `env-secret-access` (critical) in `openspec/changes/scope-diagnostic-logging/proposal.md` - added code reads credentials or CI secrets

### `08aee3da22` docs(openspec): settle the audit proposals and their shipping order

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/08aee3da22e0ba0442ce12fd4c5710936fa00217) by Jason Finch
- **size:** 24 files (+1382/-179)
- **score 7** - security review required (high)
- **triage:** docs | value 2 | effort 1 | risk 1 | applies rewrite | WATCH
- **why:** jafin openspec planning for upstream audit #3416 (AEAD SQL, KDF PRF, default key mR3m). No code. Watch their implementation commits; we have MasterPasswordGate, not these.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Security goals align, but this broad documentation-only import depends on absent OpenSpec proposals and needs fork-specific redesign before it can guide implementation safely.
  - dissent - gemini: REJECT - Our fork does not use OpenSpec; this adds irrelevant tracking documentation for jafin's roadmap rather than actionable code or applicable documentation for our tree.
- **security flags:**
  - `security-code` (high) in `openspec/changes/encrypt-sql-backend-with-aead/design.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/encrypt-sql-backend-with-aead/proposal.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/encrypt-sql-backend-with-aead/specs/sql-backend-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/encrypt-sql-backend-with-aead/tasks.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/harden-connection-file-kdf/specs/connection-file-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/narrow-connection-password-exposure/tasks.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/replace-default-connection-file-key/specs/connection-file-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/require-sql-master-password/design.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/require-sql-master-password/proposal.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/require-sql-master-password/specs/sql-backend-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/require-sql-master-password/tasks.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/retire-legacy-rijndael-for-settings/specs/settings-secret-encryption/spec.md` - credential and crypto paths need human review regardless of intent

### `0a899291f1` docs(openspec): archive fix-ssh-credential-diagnostics (#12)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/0a899291f1691cbc7695960b7bc480bea31811df) by Jason Finch
- **size:** 5 files (+97/-0)
- **score 7** - security review required (high)
- **triage:** docs | value 2 | effort 1 | risk 1 | applies rewrite | WATCH
- **why:** Spec only: discovered-key failures as info, configured-key as error, missing username as diagnostic not throw. Locate jafin's code commit and check our SSH resolver for the throw.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Our SSH sessions use PuTTY/OpenSSH; ResolvedSshCredential and OpenSpec are absent. This [documentation-only commit](https://github.com/jafin/mRemoteNG/commit/0a899291f1691cbc7695960b7bc480bea31811df) provides no executable fix or tests.
  - dissent - gemini: REJECT - Purely external OpenSpec documentation and archived change proposals for another fork's workflow; introduces unwanted non-code artifacts not used by this fork.
- **security flags:**
  - `security-code` (high) in `openspec/changes/archive/2026-08-10-fix-ssh-credential-diagnostics/design.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-10-fix-ssh-credential-diagnostics/proposal.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-10-fix-ssh-credential-diagnostics/specs/ssh-credential-resolution/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-10-fix-ssh-credential-diagnostics/tasks.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/specs/ssh-credential-resolution/spec.md` - credential and crypto paths need human review regardless of intent

### `2063f83aee` chore(tests): lower the test project platform floor to match the app

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2063f83aee3e032cb9cce6c7c3760624852eb424) by Jason Finch
- **size:** 1 files (+5/-1)
- **score 7** - security review required (high)
- **triage:** chore | value 2 | effort 1 | risk 1 | applies conflict | IMPORT
- **why:** Nullable=annotations already here; only SupportedOSPlatformVersion 26100→17763 missing. Test host on pre-24H2 machines refuses to run; matches app csproj floor.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Current HEAD already sets Nullable to annotations and SupportedOSPlatformVersion to 10.0.17763.0 in mRemoteNGTests.csproj, matching the app’s minimum.
  - dissent - gemini: REJECT - The proposed changes are already implemented in our mRemoteNGTests.csproj via commits ad9d2d70 and acb2c280, making this import completely redundant.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNGTests/mRemoteNGTests.csproj` - a new or repointed package can pull arbitrary code at restore time

### `426cb89c1a` security: validate the connection file sentinel and reserve a third value (#30)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/426cb89c1a37410c0351c44a907e2bb9c53dda03) by Jason Finch
- **size:** 5 files (+177/-12)
- **score 7** - security review required (high)
- **triage:** security | value 4 | effort 3 | risk 3 | applies conflict | REIMPLEMENT
- **why:** Our Authenticate accepts any key that decrypts (AES-CBC/PKCS7 1-in-256 false positive). Sentinel plaintext check is real hardening; skip PerFileKeySentinel/DPAPI feature we lack. Tripwire path.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Sentinel validation addresses a real authentication weakness, but this commit depends on missing earlier changes and requires a separately adapted, verified patch.
  - dissent - gemini: REJECT - Depends on missing PasswordAuthenticator.PlaintextValidator and ConnectionFileDefaults sentinels; imports speculative DPAPI changes from jafin's openspec branch.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Config/Serializers/XmlConnectionsDecryptor.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/ConnectionFileDefaults.cs` - credential and crypto paths need human review regardless of intent

### `5bdd5ae40c` security: refuse a SQL database newer than this build

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/5bdd5ae40cae4322216eff8b95c94b67e74c22f8) by Jason Finch
- **size:** 8 files (+167/-6)
- **score 7** - security review required (high)
- **triage:** security | value 3 | effort 2 | risk 2 | applies conflict | REIMPLEMENT
- **why:** Our verifier still reads a newer-schema DB and yields blank passwords. Small, testable guard; resx/Designer and openspec paths will conflict, so port by hand via tripwire.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - The missing loader guard is useful, but the commit assumes schema 3.5. At least two imported tests contradict this fork’s current implementation; adapt before approval.
  - dissent - gemini: NEEDS_HUMAN - Prevents reading newer unsupported database schemas that cause blank passwords and data loss; logic is valuable but requires manual porting without jafin's openspec artifacts.
- **security flags:**
  - `security-code` (high) in `openspec/changes/encrypt-sql-backend-with-aead/tasks.md` - credential and crypto paths need human review regardless of intent

### `5e52960807` security: encrypt settings secrets with AES-GCM instead of MD5-keyed CBC

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/5e529608076cd68232fd4a0af4385db83fbe35b1) by Jason Finch
- **size:** 16 files (+406/-46)
- **score 7** - security review required (high)
- **triage:** security | value 4 | effort 3 | risk 3 | applies conflict | REIMPLEMENT
- **why:** We still protect settings secrets (SQLPass, proxy, default password) with LegacyRijndael MD5-CBC. AES-GCM with legacy-read fallback is worth porting; needs tripwire review and migration tests.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - This fork lacks SshCredentialResolver. The patch misses PuttyBase.cs:730, which still decrypts DefaultPassword with legacy CBC; re-saving credentials would break that path.
  - dissent - gemini: NEEDS_HUMAN - Upgrades weak MD5-CBC settings encryption to AES-GCM with backward-compatible fallback; valuable security hardening across 16 files, but critical crypto changes require human maintainer review.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Config/Settings/Registry/OptRegistryCredentialsPage.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/SettingsSecretProtector.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/SshCredentialResolver.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/UI/Forms/OptionsPages/CredentialsPage.cs` - credential and crypto paths need human review regardless of intent

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

### `954bff5360` chore: clear every build warning, and take the SSH.NET security fix (#53)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/954bff53602d423ec7e24c100e800a71ba59ba2d) by Jason Finch
- **size:** 7 files (+29/-14)
- **score 7** - security review required (high)
- **triage:** security | value 3 | effort 2 | risk 2 | applies conflict | REIMPLEMENT
- **why:** We pin SSH.NET 2025.1.0; GHSA-q939-rpr3-3284 bump to 2026.0.0 plus RemotePathTransformation.ShellQuote on ScpClient applies to Tools/SecureTransfer.cs. SshNet.Agent, Testcontainers, SFTP tests absent here. Tripwire path.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork already uses SSH.NET 2026.0.0 and ShellQuote, with dedicated path-quoting tests. The proposed commit adds no useful fix here.
  - dissent - gemini: REJECT - Already implemented in our fork: SSH.NET was bumped to 2026.0.0 with ShellQuote, while the SFTP tests and extra dependencies do not exist here.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time

### `aee7131493` docs(openspec): archive add-sftp-browser-panel and retire-legacy-rijndael-for-settings (#26)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/aee71314937252eb27f02a8362c083b363c80b48) by Jason Finch
- **size:** 9 files (+424/-0)
- **score 7** - security review required (critical)
- **triage:** docs | value 2 | effort 1 | risk 1 | applies rewrite | WATCH
- **why:** Spec docs only, no code. Our fork has no openspec dir or SFTP panel. Settings-secret AES-GCM spec is a good idea we still lack (settings use LegacyRijndael at 5 call sites); watch jafin's implementing commits.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Seven renames depend on OpenSpec files absent locally. The added specifications supply no implementation or tests and describe another fork’s architecture.
  - dissent - gemini: REJECT - We do not use OpenSpec; these are external specification documents for another fork's development workflow with zero value to our codebase.
- **security flags:**
  - `security-code` (high) in `openspec/changes/archive/2026-08-11-retire-legacy-rijndael-for-settings/specs/settings-secret-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/specs/settings-secret-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `network-download` (critical) in `openspec/specs/sftp-browser-panel/spec.md` - added code fetches remote content at build or run time

### `cc5fe34a91` chore: stop paying for code-style analyzers on every local build (#54)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/cc5fe34a916f63aeab168fdf304da15bc86293e6) by Jason Finch
- **size:** 5 files (+76/-5)
- **score 7** - security review required (high)
- **triage:** perf | value 3 | effort 2 | risk 2 | applies conflict | REIMPLEMENT
- **why:** Local build ~50s to ~18s by gating IDE/code-style analyzers on CI; touches protected Directory.Build.props/build.ps1, needs human re-verify that diagnostics unchanged.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - This fork lacks the cited IDE0007 suppression. Profiling and diagnostic comparison are needed before importing global analyzer changes justified by another fork’s measurements.
  - dissent - gemini: NEEDS_HUMAN - Significant local build speedup (~30s), but trade-off between build time and local analyzer enforcement requires maintainer decision and adaptation to mRemoteNG.sln.
- **security flags:**
  - `build-script` (high) in `build.ps1` - scripts execute on a maintainer machine

### `ed7495e474` docs: propose decrypting connection secrets on demand (#50)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ed7495e47458a94941c7178af1f305a559385e79) by Jason Finch
- **size:** 3 files (+206/-0)
- **score 7** - security review required (high)
- **triage:** docs | value 2 | effort 1 | risk 1 | applies rewrite | None
- **why:** Proposal only, no code. Lazy per-record decrypt idea is sound but rekey-interaction risk noted by author; revisit if we ever adopt jafin's key-slot store.
- **pre-approval:** **MANUAL-REVIEW** (codex:NEEDS_HUMAN / gemini:REJECT)
  - dissent - codex: NEEDS_HUMAN - Eager batch decryption exists here, so the goal is useful. However, this proposal references absent APIs and prerequisites and needs adaptation to our serializers.
  - dissent - gemini: REJECT - This commit contains only OpenSpec documentation with no implementation code; our repository does not use the openspec directory or workflow.
- **security flags:**
  - `security-code` (high) in `openspec/changes/decrypt-connection-secrets-on-demand/proposal.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/decrypt-connection-secrets-on-demand/specs/connection-record-secrets/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/decrypt-connection-secrets-on-demand/tasks.md` - credential and crypto paths need human review regardless of intent

### `2eefaf10a7` fix: stop the task dialog clipping its own content, and its buttons splitting

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2eefaf10a75409bd65389dafe99bb0e6b11d9c83) by Jason Finch
- **size:** 6 files (+41/-10)
- **score 6** - security review required (high)
- **triage:** bugfix | value 2 | effort 2 | risk 1 | applies rewrite | REIMPLEMENT
- **why:** StorageFormatUpgrade is jafin-only, but frmTaskDialog lbContent/lbExpandedInfo anchor Top|Right|Right (missing Left) is real in our designer; port only the two anchor lines.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The anchor fix is useful and absent here, but the remaining changes require an unrelated storage-upgrade implementation. Consider the two anchor corrections separately.
  - dissent - gemini: REJECT - Coupled to non-existent StorageFormatUpgrade feature. Only the two-line anchor fix in frmTaskDialog is applicable to our fork and must be reimplemented.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/StorageFormatUpgrade.cs` - credential and crypto paths need human review regardless of intent

### `496a3701de` chore: make the test suite three times faster to run (#51)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/496a3701de60df5df8c8a150c9120dba1f9889f0) by Jason Finch
- **size:** 18 files (+198/-54)
- **score 6** - security review required (high)
- **triage:** perf | value 3 | effort 3 | risk 2 | applies conflict | REIMPLEMENT
- **why:** Test-only KDF-iteration shortcut (AtTestSpeed) would cut suite time; our AeadCryptographyProvider already caches iterations, 8 test files touched. Needs own helper, keep RFC 6070 KAT pinned.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - Crypto helpers are useful, but the [complete commit](https://github.com/jafin/mRemoteNG/commit/496a3701de60df5df8c8a150c9120dba1f9889f0) requires selective adaptation of runner, settings, and divergent tests before full validation.
  - dissent - gemini: NEEDS_HUMAN - Test acceleration is valuable, but reducing iteration counts on security primitives requires maintainer judgment to ensure cryptographic validation and KAT coverage remain intact.
- **security flags:**
  - `security-code` (high) in `mRemoteNGTests/Config/CredentialHarvesterTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsDocumentEncryptorTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Config/Serializers/CredentialSerializers/XmlCredentialPasswordDecryptorDecoratorTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/IntegrationTests/XmlCredentialSerializerLifeCycleTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Security/AeadCryptographyProviderTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/TestHelpers/CryptoTestSpeed.cs` - credential and crypto paths need human review regardless of intent
  - `build-script` (high) in `run-tests-core.sh` - scripts execute on a maintainer machine

### `6f1a3428e1` security: refuse writes from a cached fallback, and stop caching secrets under a published key (#56)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6f1a3428e1e59909d2513570d78627d31ad68c1c) by Jason Finch
- **size:** 14 files (+952/-29)
- **score 6** - security review required (high)
- **triage:** security | value 4 | effort 4 | risk 3 | applies rewrite | REIMPLEMENT
- **why:** Our TrySaveSqlConnectionsCache writes via XmlConnectionsSaver under store key (mR3m when no master password); fallback says read-only but allows saves. Real gap; tripwire path, human review.
- **pre-approval:** **MANUAL-REVIEW** (codex:NEEDS_HUMAN / gemini:NEEDS_HUMAN)
  - dissent - codex: NEEDS_HUMAN - The [patch](https://github.com/jafin/mRemoteNG/commit/6f1a3428e1e59909d2513570d78627d31ad68c1c) addresses real gaps, but needs legacy-backup cleanup and adaptation to this fork’s save pipeline before landing.
  - dissent - gemini: NEEDS_HUMAN - Fixes a real security gap in SQL caching and stale overwrites, but touching credential crypto and key swapping requires human security review and reimplementation.
- **security flags:**
  - `security-code` (high) in `openspec/changes/refuse-writes-from-a-cached-fallback/specs/connection-file-encryption/spec.md` - credential and crypto paths need human review regardless of intent

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

### `67a6ef2c32` docs: add the mRemoteNG documentation site (#36)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/67a6ef2c324f8504eda2297bff091895291d3ced) by Jason Finch
- **size:** 146 files (+16539/-3)
- **score 5** - security review required (critical)
- **triage:** docs | value 3 | effort 4 | risk 2 | applies rewrite | WATCH
- **why:** Docusaurus site + Pages workflow + pnpm toolchain; we ship docs/USER-GUIDE.md and Help menu. Mine page content later, never import the workflow.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The repository already has a Fork Features Guide. This 146-file import conflicts with dependency constraints and requires substantial content reconciliation, exceeding quick pre-approval scope.
  - dissent - gemini: REJECT - Massive 16.5k-line documentation overhaul adds a secondary JavaScript ecosystem and Pages deployment out of scope with our core .NET stability focus.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/docs.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `opaque-file` (high) in `docs-website/docs/images/config_top_bar.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/connections_config.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/connections_main.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/connections_open.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/connections_rightclick_menu.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/connections_status.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/connections_test_item.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/connections_top_bar.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/credssp-error.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/credvault01.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/credvault02.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/credvault03.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/cyberark_pam_connection_setup.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/default_properties.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/ec2instance.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/example_et_start_application_01.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/example_et_start_application_02.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/example_et_traceroute_01.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/example_et_traceroute_02.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/example_et_traceroute_03.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/example_et_traceroute_04.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/example_et_traceroute_05.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/external_tools_external_tool_properties_01.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/external_tools_main_ui_01.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/external_tools_toolbar_01.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/external_tools_tools_list_01.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/external_tools_win-resurces.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/folders_and_inheritance_01.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/folders_and_inheritance_02.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/folders_and_inheritance_03.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/folders_and_inheritance_04.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/folders_and_inheritance_05.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/folders_and_inheritance_06.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/import_export_dialog.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/import_from_active_directory.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/import_from_port_scan.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/menus_hide_menu_strip.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/menus_main_menu.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/mremoteng_favicon_32.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/mremoteng_logo.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/mremoteng_main_ui.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/mremoteng_main_ui_connect_win_server.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/notifications_panel.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/notifications_popup.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/oracle_remediation_setting.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/putty.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/quick_connect_01.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/quick_connect_02.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/quick_connect_03.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/screenshot_manager_overview.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/screenshot_manager_rightclick_menu.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/ssh_file_transfer_01.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/ssh_tunnel.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/themes/darculaNG.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/themes/themes.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/themes/vs2012Blue.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/themes/vs2012Dark.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/themes/vs2012Light.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/themes/vs2013Blue.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/themes/vs2013Dark.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/themes/vs2013Light.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/themes/vs2015Blue.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/themes/vs2015Dark.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/themes/vs2015Light.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/themes/vs2015blueNG.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/themes/vs2015darkNG.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/themes/vs2015lightNG.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/user_interface_overview.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/user_interface_panels_01.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/user_interface_panels_02.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/docs/images/user_interface_panels_03.png` - added file has no reviewable text diff
  - `security-code` (high) in `docs-website/docs/registry/credential-settings.md` - credential and crypto paths need human review regardless of intent
  - `process-exec` (critical) in `docs-website/docs/user-interface/external-tools.md` - added code spawns a process or evaluates a string as code
  - `process-exec` (critical) in `docs-website/docs/variables-reference.md` - added code spawns a process or evaluates a string as code
  - `opaque-file` (high) in `docs-website/pnpm-lock.yaml` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/static/img/icon.png` - added file has no reviewable text diff
  - `opaque-file` (high) in `docs-website/static/img/logo.png` - added file has no reviewable text diff

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

### `69e213e600` chore: migrate SpecFlow to Reqnroll

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/69e213e600f3670bff9683d65baa553d9ad86d5b) by Jason Finch
- **size:** 11 files (+16/-453)
- **score 4** - security review required (high)
- **triage:** chore | value 2 | effort 2 | risk 2 | applies conflict | WATCH
- **why:** We still pin SpecFlow 3.9.74 with committed .feature.cs; SpecFlow is EOL so Reqnroll is sensible, but test-only hygiene, no user benefit. Do on our own schedule.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The migration is not duplicated here, but default telemetry violates your explicit requirement. Disable telemetry explicitly before reconsidering import.
  - dissent - gemini: REJECT - SpecFlow is barely used in our suite; adding new dependencies offers no user benefit and should be handled internally on our own schedule.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `mRemoteNGSpecs/Features/CredentialRepository.feature.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGSpecs/Features/CredentialRepositoryList.feature.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGSpecs/StepDefinitions/CredentialRepositoryListSteps.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGSpecs/StepDefinitions/CredentialRepositorySteps.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNGSpecs/mRemoteNGSpecs.csproj` - a new or repointed package can pull arbitrary code at restore time

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

### `f1be4da0de` feat(sftp): add the SFTP session behind an interface

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f1be4da0de7d5ac31265e9708936fa6192bb3009) by Jason Finch
- **size:** 10 files (+1087/-28)
- **score 3** - security review required (critical)
- **triage:** feature | value 3 | effort 4 | risk 3 | applies likely | WATCH
- **why:** Self-contained SFTP session abstraction with tests; useful only once jafin's SFTP browser panel lands. No mRemoteNG/Connection/Sftp in our fork. Wait for panel.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Repository inspection confirms missing prerequisites. Landing requires additional architecture and integration work; this 1,087-line browser foundation is not a small, independently verifiable maintenance improvement.
  - dissent - gemini: REJECT - Isolated foundational fragment of an incomplete multi-commit SFTP browser series; provides no user capability on its own and requires evaluating the complete feature as a whole.
- **security flags:**
  - `network-download` (critical) in `mRemoteNG/Connection/Sftp/SftpSession.cs` - added code fetches remote content at build or run time
  - `network-download` (critical) in `mRemoteNG/Tools/ProgressReportingStream.cs` - added code fetches remote content at build or run time
  - `network-download` (critical) in `openspec/changes/add-sftp-browser-panel/tasks.md` - added code fetches remote content at build or run time

### `6cd3814432` chore: bump dependencies and align transitive pins to .NET 10.0.10

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6cd38144327e342cb392af59bd26e7c58ae43ee1) by Jason Finch
- **size:** 1 files (+22/-24)
- **score 2** - security review required (high)
- **triage:** chore | value 2 | effort 2 | risk 3 | applies conflict | REIMPLEMENT
- **why:** We are still on 10.0.5 pins, MySql.Data 9.6.0, WebView2 4022. Bump ourselves via Dependabot; MySql.Data 26.x major needs #145/#146 MariaDB regression run, drops our ZstdSharp pin.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Dependency maintenance fits, but this import leaves a concrete restore blocker unresolved. See [package dependencies](https://www.nuget.org/packages/MySql.Data/26.7.0#dependencies-body-tab).
  - dissent - gemini: REJECT - Bulk unverified dependency upgrades risk breaking SQL/MariaDB connection storage; packages should be updated granularly via Renovate with full CI verification.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time

### `8e4cc69437` chore(deps): drop redundant direct ZstdSharp.Port references

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8e4cc69437baa09673de475b581af5d1e2b81c85) by Jason Finch
- **size:** 4 files (+1/-3)
- **score 2** - security review required (high)
- **triage:** chore | value 1 | effort 1 | risk 2 | applies likely | WATCH
- **why:** Our tree still has direct refs in all 3 csproj; CentralPackageTransitivePinningEnabled=true so pin holds. Cosmetic cleanup, no user benefit; verify restore graph before touching.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Current HEAD already omits all three direct ZstdSharp.Port references and contains the identical central pinning comment, version 0.8.7, and enabled transitive pinning.
  - dissent - gemini: REJECT - Commit 8e4cc6943 is already present in our branch history; the changes are already applied.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNGSpecs/mRemoteNGSpecs.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNGTests/mRemoteNGTests.csproj` - a new or repointed package can pull arbitrary code at restore time

### `a9da566c6a` chore: update test and analyzer packages

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a9da566c6af58ac37aa9fc2d187fd283b62d8cfb) by Jason Finch
- **size:** 1 files (+11/-12)
- **score 2** - security review required (high)
- **triage:** chore | value 2 | effort 2 | risk 3 | applies likely | WATCH
- **why:** Our pins are the old ones; NSubstitute 6 and Meziantou 3 are major bumps that can reopen the zero-warning backlog. Do via our own Renovate, not import.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Dependency maintenance fits our direction, but this analyzer update is already surpassed locally, and bundled major test-tool upgrades lack demonstrated compatibility or fork-specific benefit.
  - dissent - gemini: REJECT - Conflicts with local versions, downgrades Meziantou, and batches risky major package updates that should be handled individually and tested through Renovate.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time

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

### `298e85218f` feat(ssh): route ProtocolOpenSSH through the credential resolver

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/298e85218f06c87fdfd09e53da6bcfb71a2cbae4) by Jason Finch
- **size:** 7 files (+335/-50)
- **score 1** - security review required (high)
- **triage:** feature | value 3 | effort 4 | risk 4 | applies rewrite | WATCH
- **why:** Part of jafin's multi-commit SSH agent/credential resolver series; our OpenSSH still has FindDefaultSshKey. Evaluate whole series, security tripwire review required.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The resolver subsystem is absent here. This import requires additional prerequisites and would replace existing hostname, username, and key-path safeguards; its tests do not cover those protections.
  - dissent - gemini: REJECT - OpenSSH still cannot accept passwords non-interactively; the patch adds speculative architecture and openspec artifacts while reverting our argument validation.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/Ssh/Adapters/OpenSshArgsAdapter.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/SshCredentialResolutionOptions.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/SshCredentialResolver.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Security/Ssh/SshCredentialResolverTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/add-ssh-agent-credential-resolver/tasks.md` - credential and crypto paths need human review regardless of intent

### `43b28abb7f` security: narrow how long a connection's password exists as plain text (#49)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/43b28abb7f1797b7899d9d33610847149e9567fa) by Jason Finch
- **size:** 16 files (+2899/-1717)
- **score 1** - security review required (critical)
- **triage:** security | value 3 | effort 4 | risk 4 | applies rewrite | WATCH
- **why:** Real hardening (SecurePassword accessor, no plaintext on grid repaint) but 16 files on jafin's diverged AbstractConnectionRecord; tripwire paths. Reimplement idea later, not patch.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - The [patch](https://github.com/jafin/mRemoteNG/commit/43b28abb7f1797b7899d9d33610847149e9567fa) references missing SettingsSecretProtector and hardened-storage test APIs. Its useful security improvements require a scoped port and RDP regression review.
  - dissent - gemini: NEEDS_HUMAN - Valuable credential hardening against plaintext exposure on repaints, but massive divergence from our AbstractConnectionRecord requires human judgment and fork-specific reimplementation rather than direct cherry-pick.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Config/Import/CredentialImportHelper.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Connection/ConnectionSecretDecryptionTimingTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Connection/Protocol/RdpProtocolPasswordSourceTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-15-narrow-connection-password-exposure/proposal.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-15-narrow-connection-password-exposure/specs/connection-record-secrets/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-15-narrow-connection-password-exposure/tasks.md` - credential and crypto paths need human review regardless of intent
  - `env-secret-access` (critical) in `openspec/changes/archive/2026-08-15-narrow-connection-password-exposure/tasks.md` - added code reads credentials or CI secrets
  - `security-code` (high) in `openspec/changes/narrow-connection-password-exposure/specs/connection-record-secrets/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/narrow-connection-password-exposure/tasks.md` - credential and crypto paths need human review regardless of intent

### `9e44de2932` security: derive hardened connection files with PBKDF2-HMAC-SHA256

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9e44de293218ab5718311420ba2ac15e4e38d467) by Jason Finch
- **size:** 15 files (+383/-40)
- **score 1** - security review required (high)
- **triage:** security | value 3 | effort 4 | risk 4 | applies rewrite | WATCH
- **why:** Opt-in PBKDF2-SHA256 needs jafin's StorageFormatLevel series we lack; touches KDF (tripwire, RFC 6070 pin keeps SHA-1). Store-compat break for upstream readers.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Security hardening fits our priorities, but missing format prerequisites and an additional constructor-dependent test require adaptation and compatibility validation before approval.
  - dissent - gemini: REJECT - Cannot compile without missing StorageFormatLevel prerequisites. It is part of an unimported multi-commit storage redesign that breaks format compatibility.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Config/Serializers/XmlConnectionsDecryptor.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/AsymmetricEncryption/CertificateCryptographyProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Factories/CryptoProviderFactoryFromXml.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/ICryptographyProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/KeyDerivation/KeyDerivationPrf.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/KeyDerivation/Pkcs5S2KeyGenerator.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/SymmetricEncryption/AeadCryptographyProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/SymmetricEncryption/LegacyRijndaelCryptographyProvider.cs` - credential and crypto paths need human review regardless of intent

### `bb43414994` security: let a shared connection file unlock for everyone, and offer a rekey that actually revokes (#43)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bb4341499481dcd7d68540a3d0ad11cb3c08db7b) by Jason Finch
- **size:** 23 files (+2084/-149)
- **score 1** - security review required (high)
- **triage:** security | value 4 | effort 5 | risk 5 | applies rewrite | WATCH
- **why:** Builds on jafin's hardened storage format (ConnectionFileKeyProtection etc.) absent from our tree; cannot land alone. Whole feature series needs security review first.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Our tree lacks FileProtection and StorageFormat foundations. Adoption requires prerequisite changes and substantial credential-security validation, exceeding a small, quickly verifiable import.
  - dissent - gemini: REJECT - Huge speculative rewrite (+2084 lines) relying on unported file-protection infrastructure; breaks upstream file compatibility and cannot be cleanly verified or imported alone.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/FileProtection/ConnectionFileKeyProtection.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/FileProtection/ConnectionFileRekey.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/FileProtection/MachineProtectorPolicy.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/FileProtection/MachineSlotSession.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/StorageFormatUpgrade.cs` - credential and crypto paths need human review regardless of intent

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

### `c49832c6fd` feat(ssh): extract credential resolution and add SSH agent support

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c49832c6fd4360b6ded66addce46efde14210bab) by Jason Finch
- **size:** 37 files (+3751/-223)
- **score 1** - security review required (critical)
- **triage:** feature | value 4 | effort 5 | risk 5 | applies rewrite | WATCH
- **why:** SSH agent support is real value, but new pinned dependency in auth path plus PuttyBase rewrite hits tripwire; needs human security review, not pipeline import.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Adds an unwanted authentication dependency and a broad credential rewrite that also clears configured Vault usernames. Existing security protections regress; quick pre-approval is inappropriate.
  - dissent - gemini: REJECT - Violates the explicit rule against new external dependencies by adding SshNet.Agent. The extensive changes to SSH credential resolution also pose a significant security risk.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `process-exec` (critical) in `mRemoteNG/Connection/Protocol/PuttyBase.cs` - added code spawns a process or evaluates a string as code
  - `security-code` (high) in `mRemoteNG/Properties/OptionsCredentialsPage.Designer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Properties/OptionsCredentialsPage.settings` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/Adapters/PuttyArgsAdapter.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/Agent/ISshAgentProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/Agent/SshAgentSettings.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/Agent/SshNetAgentProvider.cs` - credential and crypto paths need human review regardless of intent
  - `opaque-file` (high) in `mRemoteNG/Security/Ssh/Agent/SshNetAgentProvider.cs` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Security/Ssh/IDefaultSshKeyLocator.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/ISshCredentialResolver.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/Providers/ExternalConnectorProviders.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/Providers/ISshCredentialProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/ResolvedSshCredential.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/SshAgentIdentity.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/SshCredentialDiagnostic.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/SshCredentialResolutionOptions.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/SshCredentialResolver.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/UI/Forms/OptionsPages/CredentialsPage.Designer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/UI/Forms/OptionsPages/CredentialsPage.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `mRemoteNGTests/Connection/Protocol/PuttyBaseCredentialArgumentTests.cs` - credential and crypto paths need human review regardless of intent
  - `process-exec` (critical) in `mRemoteNGTests/Connection/Protocol/PuttyBaseCredentialArgumentTests.cs` - added code spawns a process or evaluates a string as code
  - `security-code` (high) in `mRemoteNGTests/Security/Ssh/ResolvedSshCredentialTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Security/Ssh/SshCredentialResolverTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/add-ssh-agent-credential-resolver/.openspec.yaml` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/add-ssh-agent-credential-resolver/design.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/add-ssh-agent-credential-resolver/proposal.md` - credential and crypto paths need human review regardless of intent
  - `network-download` (critical) in `openspec/changes/add-ssh-agent-credential-resolver/proposal.md` - added code fetches remote content at build or run time
  - `security-code` (high) in `openspec/changes/add-ssh-agent-credential-resolver/specs/ssh-agent-authentication/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/add-ssh-agent-credential-resolver/specs/ssh-credential-resolution/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/add-ssh-agent-credential-resolver/tasks.md` - credential and crypto paths need human review regardless of intent

### `d649b37009` security: require a master password for a SQL database using authenticated encryption (#57)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d649b37009c26b745cd393c627f1c5bd4175253b) by Jason Finch
- **size:** 28 files (+1696/-275)
- **score 1** - security review required (high)
- **triage:** security | value 4 | effort 5 | risk 5 | applies rewrite | WATCH
- **why:** SQL AES-GCM + mandatory master password; our SQL path lacks it (no upgrade class). Big, tripwire-gated, needs own design on our v3.6 schema and MasterPasswordGate.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Security goal fits, but SQL crypto prerequisites are absent and version semantics conflict. Requires a dedicated migration rather than importing this [commit](https://github.com/jafin/mRemoteNG/commit/d649b37009c26b745cd393c627f1c5bd4175253b).
  - dissent - gemini: REJECT - Large speculative rewrite (+1696/-275 across 28 files) depending on missing fork-specific encryption upgrades, breaking backwards compatibility for passwordless SQL setups.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Config/Connections/SqlDatabaseEncryptionUpgrade.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/UI/SqlDatabaseEncryptionUpgradePrompt.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Config/Connections/SqlMasterPasswordRequirementTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/IntegrationTests/Sql/SqlDatabaseEncryptionUpgradeTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/IntegrationTests/Sql/SqlSaverEncryptionIntegrationTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/UI/Forms/SqlServerPageEncryptionStatusTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/UI/SqlDatabaseEncryptionUpgradePromptTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/require-sql-master-password/design.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/require-sql-master-password/tasks.md` - credential and crypto paths need human review regardless of intent

### `ed361ef2da` security: encrypt SQL-backed secrets with AES-256-GCM, and let an administrator upgrade (#55)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ed361ef2da069fa63cef493d45f410507d5e48ed) by Jason Finch
- **size:** 49 files (+3706/-997)
- **score 1** - security review required (critical)
- **triage:** security | value 4 | effort 5 | risk 5 | applies rewrite | None
- **our issue:** #165
- **why:** Real gap: SQL secrets still use unsalted legacy AES; we have AeadCryptographyProvider but it is not wired into SQL persistence. 49-file diff tied to jafin's storage-format/key-slot stack; tripwire-protected, needs own design.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - Useful security direction, but the verified version collision requires a redesigned migration and backend validation; this 49-file import cannot be safely pre-approved.
  - dissent - gemini: NEEDS_HUMAN - AES-256-GCM for SQL aligns with credential security goals, but the size and security implications need human judgment.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/ci.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `security-code` (high) in `mRemoteNG/Config/Connections/SqlDatabaseEncryptionUpgrade.cs` - credential and crypto paths need human review regardless of intent
  - `env-secret-access` (critical) in `mRemoteNG/Config/Connections/SqlDatabaseEncryptionUpgrade.cs` - added code reads credentials or CI secrets
  - `security-code` (high) in `mRemoteNG/Security/Factories/CryptoProviderFactoryFromSqlVersion.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/ICryptographyProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/SymmetricEncryption/AeadCryptographyProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/UI/SqlDatabaseEncryptionUpgradePrompt.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Config/Connections/SqlDatabaseMasterPasswordDetectionTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/IntegrationTests/Sql/SqlDatabaseEncryptionUpgradeTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/IntegrationTests/Sql/SqlSaverEncryptionIntegrationTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Security/Factories/CryptoProviderFactoryFromSqlVersionTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/UI/Forms/SqlServerPageEncryptionStatusTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/UI/SqlDatabaseEncryptionUpgradePromptTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/encrypt-sql-backend-with-aead/design.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/encrypt-sql-backend-with-aead/proposal.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/encrypt-sql-backend-with-aead/tasks.md` - credential and crypto paths need human review regardless of intent

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

### `a35051eb33` feat: ask once before hardening a store, and say what it costs

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a35051eb3377f8cd473c1bfdf399ef18c5695a80) by Jason Finch
- **size:** 6 files (+430/-6)
- **score 0** - security review required (high)
- **triage:** feature | value 2 | effort 4 | risk 3 | applies rewrite | WATCH
- **why:** Prompt UI for hardened-store opt-in; meaningless without 9e44de2 and StorageFormatLevel series. Decide together with that series, not alone.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - RootNodeInfo.StorageFormat, StorageFormatLevel, and KeyDerivationPrf are missing locally. The commit supplies none of these prerequisites, so this is not a self-contained, quickly verifiable import.
  - dissent - gemini: REJECT - Incomplete UI prompt for an absent storage hardening subsystem. Our fork preserves upstream file compatibility and pins PBKDF2-HMAC-SHA1 with 600,000 iterations.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/StorageFormatUpgrade.cs` - credential and crypto paths need human review regardless of intent

### `b96e671e8f` chore: remove unused NuGet packages (#7)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b96e671e8f000c5ef5f20bfbd837f592a6c71629) by Jason Finch
- **size:** 300 files (+60396/-60600)
- **score 0** - security review required (high)
- **triage:** chore | value 2 | effort 4 | risk 3 | applies conflict | REIMPLEMENT
- **why:** Useful core is ~7 unused PackageVersion removals we still carry (NUnit.Console, NETStandard.Library, NETCore.Platforms); but 300 files of file-scoped namespace rewrites touch credential connectors and would trip the tripwire. Cherry-pick the props lines only.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The [937-file commit](https://github.com/jafin/mRemoteNG/commit/b96e671e8f000c5ef5f20bfbd837f592a6c71629) cannot be verified quickly against our divergent fork. Extract the useful package cleanup into a separate, narrowly scoped change.
  - dissent - gemini: REJECT - Mislabeled chore commit rewrites 300 files with blanket formatting changes. Package cleanup, if needed, should be surgical and isolated to Directory.Packages.props.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `ExternalConnectors/CPS/PasswordstateInterface.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `ExternalConnectors/OP/OnePasswordCli.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `ExternalConnectors/PasswordSafe/PasswordSafeCli.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/App/Info/CredentialsFileInfo.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/CredentialHarvester.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/CredentialRecordLoader.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/CredentialRecordSaver.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/CredentialRepositoryListLoader.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/CredentialRepositoryListSaver.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Import/CredentialImportHelper.cs` - credential and crypto paths need human review regardless of intent
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

### `d0aa53c920` feat(ssh): authenticate SSH.NET transfers from the resolved credential

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d0aa53c92035162e52ea1aff7bd71d3f6568124d) by Jason Finch
- **size:** 10 files (+1213/-117)
- **score 0** - security review required (high)
- **triage:** feature | value 3 | effort 5 | risk 4 | applies rewrite | WATCH
- **why:** Key/agent auth for SFTP/SCP is real value (ours is password-only) but depends on jafin's ResolvedSshCredential/agent model we lack; security-sensitive, needs whole series not one commit.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - Agent authentication is useful, but this import requires prerequisite architecture, breaks existing SecureTransfer APIs and tests, and exceeds the scope of a quickly verifiable change.
  - dissent - gemini: NEEDS_HUMAN - SSH key and agent authentication for file transfers is valuable, but requires human architectural judgment to resolve missing credential framework dependencies and security implications.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/Ssh/Adapters/SshNetAuthAdapter.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/Agent/SshNetAgentProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/SshAgentIdentity.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/add-ssh-agent-credential-resolver/tasks.md` - credential and crypto paths need human review regardless of intent

### `ef77a9d9ce` feat: offer the storage format upgrade once, and keep it reachable

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ef77a9d9ce738e08a61af03daaab880081ad406f) by Jason Finch
- **size:** 9 files (+406/-6)
- **score 0** - security review required (high)
- **triage:** feature | value 2 | effort 4 | risk 3 | applies rewrite | None
- **why:** Depends on StorageFormatLevel/hardened-store infrastructure absent from our fork; sidecar decline file only meaningful with that feature. Track jafin's stack as a whole.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Credential hardening fits our priorities, but this UI layer requires a separate format migration architecture. It is not a small, independently importable change.
  - dissent - gemini: REJECT - Our fork lacks the underlying StorageFormatLevel infrastructure; importing this feature in isolation causes build failures and pushes incompatible storage format changes.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/StorageFormatOffer.cs` - credential and crypto paths need human review regardless of intent

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

### `6defe1091f` docs: document the hardened connection file, and archive the change (#40)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6defe1091f1a6244d56d4b4ceb9127bb465fef41) by Jason Finch
- **size:** 12 files (+504/-1)
- **score -1** - security review required (high)
- **triage:** docs | value 1 | effort 4 | risk 2 | applies rewrite | WATCH
- **why:** Docs for jafin's hardened-storage feature we do not have; no docs-website/openspec here. Track the feature commit itself, not its docs.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - RootNodeInfo still defaults to the published key, and FileMenu lacks Storage Format. This documentation-only commit requires implementation prerequisites and adaptation to our documentation structure.
  - dissent - gemini: REJECT - The underlying hardened storage format from jafin was not ported; importing docs and changelog entries for absent code will mislead users and pollute the repository.
- **security flags:**
  - `security-code` (high) in `openspec/changes/archive/2026-08-14-replace-default-connection-file-key/specs/connection-file-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/specs/connection-file-encryption/spec.md` - credential and crypto paths need human review regardless of intent

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

### `2badada456` security: portable edition, runtime edition detection, and the §8 verification findings (#38)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2badada456c8210e3b741fbc0da221edb5986f97) by Jason Finch
- **size:** 38 files (+2755/-115)
- **score -2** - security review required (critical)
- **triage:** security | value 3 | effort 5 | risk 5 | applies rewrite | WATCH
- **why:** Runtime portable.flag replaces compile-time PORTABLE; touches CI, build.ps1, credential protector. 38 files against our own PORTABLE design. Tripwire-gated; needs human review.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Runtime edition detection is useful, but this [commit](https://github.com/jafin/mRemoteNG/commit/2badada456c8210e3b741fbc0da221edb5986f97) bundles incompatible security changes. Extract a focused patch for separate review.
  - dissent - gemini: REJECT - Large 38-file speculative rewrite conflicting with our fork's compile-time PORTABLE architecture, altering credential storage and CI packaging without providing maintainable value.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `build-script` (high) in `build.ps1` - scripts execute on a maintainer machine
  - `security-code` (high) in `mRemoteNG/Security/StorageFormatOffer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/StorageFormatUpgrade.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `openspec/changes/replace-default-connection-file-key/specs/connection-file-encryption/spec.md` - credential and crypto paths need human review regardless of intent

### `2c2d846415` feat(ssh): native SSH terminal, rendered in-app instead of embedded PuTTY (#9)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2c2d8464153992c6f905cfa08cf1b0ad1f5d9931) by Jason Finch
- **size:** 69 files (+6529/-219)
- **score -2** - security review required (high)
- **triage:** feature | value 3 | effort 5 | risk 5 | applies rewrite | WATCH
- **why:** 6.5k-line xterm.js/WebView2 SSH terminal with opaque JS assets and new protocol type; heavy security surface. Not importable as-is; revisit if PuTTYNG becomes a burden.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The 69-file, 6,529-line feature exceeds quick pre-approval scope. Existing SSH support is established; security, regression compatibility, and maintenance benefits remain unproven.
  - dissent - gemini: REJECT - The fork explicitly forbids new external dependencies and features that duplicate existing ones. A web-based SSH terminal violates both.
- **security flags:**
  - `license` (medium) in `mRemoteNG/Connection/Protocol/SSH/Native/Assets/xterm-LICENSE.txt` - licence edits change redistribution terms
  - `opaque-file` (high) in `mRemoteNG/Connection/Protocol/SSH/Native/Assets/xterm.js` - added file has no reviewable text diff
  - `security-code` (high) in `mRemoteNG/Security/Ssh/Adapters/SshNetAuthAdapter.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/ResolvedSshCredential.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/SshCredentialResolver.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `mRemoteNGTests/Security/Ssh/SshCredentialDiagnosticSeverityTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/fix-ssh-credential-diagnostics/design.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/fix-ssh-credential-diagnostics/proposal.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/fix-ssh-credential-diagnostics/specs/ssh-credential-resolution/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/fix-ssh-credential-diagnostics/tasks.md` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `spikes/native-ssh-terminal/NativeTerminalSpike/NativeTerminalSpike.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `license` (medium) in `spikes/native-ssh-terminal/NativeTerminalSpike/assets/xterm-LICENSE.txt` - licence edits change redistribution terms
  - `opaque-file` (high) in `spikes/native-ssh-terminal/NativeTerminalSpike/assets/xterm.js` - added file has no reviewable text diff
  - `build-script` (high) in `spikes/native-ssh-terminal/fixture/down.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `spikes/native-ssh-terminal/fixture/up.ps1` - scripts execute on a maintainer machine

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

### `405520de00` feat: integrate native mstsc mode with current upstream

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/405520de0000f5c056b55d7b1fcee1b89f59a521) by sanay
- **size:** 9 files (+1830/-1752)
- **score -2** - security review required (high)
- **triage:** feature | value 3 | effort 5 | risk 5 | applies rewrite | WATCH
- **why:** External mstsc launch mode: new RdpClientMode across XML/SQL serializers, credential cache cleaner, temp .rdp signing; touches security paths and schema (v2.8/SQL 3.x), needs full tripwire review and redesign on our serializers.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This integration depends on earlier third-party feature commits, omits database migrations and tests, and cannot qualify as a small, independently verifiable import.
  - dissent - gemini: REJECT - High-risk speculative rewrite touching security tripwire paths and serialization schemas with massive formatting churn, incompatible with our fork's stability focus and embedded-client architecture.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Connection/Protocol/RDP/RdpCredentialCacheCleaner.cs` - credential and crypto paths need human review regardless of intent

### `5e0ac2d5c8` security: decrypt a connection's password when it is needed, not when the file is opened (#59)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/5e0ac2d5c8aa770797430477c5b72fd334c69267) by Jason Finch
- **size:** 19 files (+1723/-222)
- **score -2** - security review required (high)
- **triage:** security | value 3 | effort 5 | risk 5 | applies rewrite | WATCH
- **why:** Lazy per-connection decrypt spans 19 files and depends on jafin's hardened-store design; our KDF-per-save path (#120) and MasterPasswordGate differ. Wait for it to settle.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The [patch](https://github.com/jafin/mRemoteNG/commit/5e0ac2d5c8aa770797430477c5b72fd334c69267) assumes FileKey, KeyDerivationPrf, SecurePassword and file-protection infrastructure absent here. Useful direction, but requires a substantial validated port, not quick pre-approval.
  - dissent - gemini: REJECT - Violates anti-speculative-rewrite mandate; incompatible with our crypto and MasterPasswordGate architecture, and cannot be applied without importing jafin's entire separate file-protection subsystem.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Config/Import/CredentialImportHelper.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/XmlConnectionsDecryptor.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Connection/ConnectionSecretDecryptionException.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/ConnectionSecretKeyIdentity.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/FileProtection/ConnectionFileRekey.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/IThreadSafeCryptographyProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Connection/ConnectionSecretDecryptionTimingTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/decrypt-connection-secrets-on-demand/tasks.md` - credential and crypto paths need human review regardless of intent

### `5eb1d371a3` Add a native SSH protocol: xterm.js in WebView2 over SSH.NET

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/5eb1d371a338103a556cc4343c0f14ee1ce756c4) by local
- **size:** 9 files (+405/-0)
- **score -2** - security review required (high)
- **triage:** feature | value 3 | effort 5 | risk 5 | applies conflict | WATCH
- **why:** In-process SSH would sidestep PuTTY focus/Alt-Tab class (#143/#168), but bundles opaque xterm.js and adds WebView2+SSH.NET deps; needs tripwire human review, not import.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This untested password-only prototype compromises credential security; SSHNative=15 also collides with this fork’s VMRC protocol, preventing a straightforward import.
  - dissent - gemini: REJECT - Violates repo policy against new external dependencies; this is an incomplete prototype lacking key auth, agent forwarding, and existing PuTTY features.
- **security flags:**
  - `opaque-file` (high) in `mRemoteNG/Resources/Terminal/xterm.js` - added file has no reviewable text diff
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

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

### `8e3e35f7d2` feat: add native mstsc implementation files

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8e3e35f7d28ebe67b3407495e6b6f4c437940b44) by sanay
- **size:** 13 files (+1845/-0)
- **score -2** - security review required (critical)
- **triage:** feature | value 3 | effort 5 | risk 5 | applies rewrite | WATCH
- **why:** 1845-line native mstsc launcher: CredWriteW writes, temp .rdp files, process exec, credential resolver. Security-heavy (tripwire paths), untested here, second RDP path doubles #182/#177 surface. Track, do not import.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - The [commit](https://github.com/AlexanderTimofeev/mRemoteNG/commit/8e3e35f7d28ebe67b3407495e6b6f4c437940b44) omits model, launch wiring, and persistence changes. Its broad credential-handling feature requires substantial integration, beyond quick maintainer verification.
  - dissent - gemini: NEEDS_HUMAN - Native RDP avoids MSTSCLib limitations using secure credential injection, but its size and schema modifications require manual maintainer judgement.
- **security flags:**
  - `process-exec` (critical) in `mRemoteNG/Connection/Protocol/RDP/NativeRdpLauncher.cs` - added code spawns a process or evaluates a string as code
  - `security-code` (high) in `mRemoteNG/Connection/Protocol/RDP/RdpCredentialResolver.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Connection/Protocol/RDP/WindowsCredentialManager.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Connection/Protocol/RDP/RdpCredentialResolverTests.cs` - credential and crypto paths need human review regardless of intent

### `bd8e71adcb` Make modern themes dependable without disrupting live sessions

- **fork:** [YuLiangLin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bd8e71adcbefed2b4515603af09b2f2b33122381) by Nathan Lin
- **size:** 36 files (+2323/-276)
- **score -2** - security review required (high)
- **triage:** feature | value 2 | effort 4 | risk 4 | applies rewrite | WATCH
- **why:** Monolithic 36-file theme/fullscreen/zh-TW bundle; drops MainFormKiosk persistence we still use. Only zh-TW label fixes worth a cherry-pick.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Existing preview/restoration overlaps; this broad import mixes fullscreen and foreign versioning changes, exceeding quick, maintainable verification. [Patch](https://github.com/YuLiangLin/mRemoteNG/commit/bd8e71adcbefed2b4515603af09b2f2b33122381.patch)
  - dissent - gemini: REJECT - Unstable reflection-based theme switching and huge speculative UI rewrite conflict with our stability, maintainability, and zero-warning standards.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `c62427d9ed` feat: give a connection store an explicit storage format level

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c62427d9ed11106668e06237856fc0de3184e02c) by Jason Finch
- **size:** 12 files (+361/-20)
- **score -2** - security review required (high)
- **triage:** security | value 2 | effort 4 | risk 4 | applies rewrite | WATCH
- **why:** Storage-format level opt-in from jafin's SECURITY-AUDIT-3416 series; no StorageFormat.cs here, our hardening is MasterPasswordGate. Serializer/root-node files diverged; tripwire paths.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The [patch](https://github.com/jafin/mRemoteNG/commit/c62427d9ed11106668e06237856fc0de3184e02c.patch) introduces preparatory metadata without actual hardening; its incompatible SQL version contract requires redesign before import.
  - dissent - gemini: REJECT - Our fork uses MasterPasswordGate instead; this isolated commit serves an external multi-part storage rewrite we do not want or use.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/StorageFormat.cs` - credential and crypto paths need human review regardless of intent

### `e204c92845` security: offer the per-file key, and ask for a recovery password (#33)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e204c928452735715805fa90d040fc8a2269ac8a) by Jason Finch
- **size:** 15 files (+917/-23)
- **score -2** - security review required (high)
- **triage:** security | value 3 | effort 5 | risk 5 | applies rewrite | WATCH
- **why:** Per-file key + recovery password is a large storage-format redesign on jafin's FileProtection stack, none of it here. Real security value; would need own design and human security review, not a port.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Credential hardening fits our direction, but ConnectionFileKey, protectors, RootNodeInfo key fields, and StorageFormatUpgrade are absent here. Adoption requires prerequisite architecture and compatibility review.
  - dissent - gemini: REJECT - Massive divergent redesign of connection encryption across 15 files that breaks backward compatibility and relies on missing upstream PR infrastructure.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/FileProtection/ConnectionFileKeyProtection.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/FileProtection/ConnectionFileMigration.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/FileProtection/MachineProtectorPolicy.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/FileProtection/RecoveryPasswordSession.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/StorageFormatUpgrade.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Security/FileProtection/RecoveryPasswordSessionTests.cs` - credential and crypto paths need human review regardless of intent

### `e5f0075d82` fix: close out the hardening changes against a real upstream build

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e5f0075d82b51d456585a03de48e9c4a99e1e38a) by Jason Finch
- **size:** 16 files (+534/-24)
- **score -2** - security review required (high)
- **triage:** security | value 2 | effort 4 | risk 4 | applies rewrite | WATCH
- **why:** Closes jafin's hardened-KDF/StorageFormat/DiagnosticTextSanitizer stack, none of which exists here. Only portable idea: SQL version check before password prompt; our loader still prompts first. Tripwire paths.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Local interfaces lack KeyDerivationPrf and IsNewerThanSupported; DiagnosticTextSanitizer is absent. Useful hardening ideas need separately scoped ports, not pre-approval of this commit.
  - dissent - gemini: REJECT - Tightly coupled to jafin's custom hardening stack absent in this fork; files like DiagnosticTextSanitizer do not exist here and serializer types differ completely.
- **security flags:**
  - `security-code` (high) in `openspec/changes/archive/2026-08-10-harden-connection-file-kdf/specs/connection-file-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/require-sql-master-password/specs/sql-backend-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/specs/connection-file-encryption/spec.md` - credential and crypto paths need human review regardless of intent

### `e7a0619a55` security: give the connection file its own key under two protectors (#31)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e7a0619a552581ffdc37ed7ebbf285450fff3863) by Jason Finch
- **size:** 18 files (+2244/-14)
- **score -2** - security review required (high)
- **triage:** security | value 3 | effort 5 | risk 5 | applies rewrite | WATCH
- **why:** Per-file random key under DPAPI + recovery-password protectors; sound design but changes confCons format, breaks upstream compatibility, needs human security review (tripwire). Track, don't import yet.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - The [patch](https://github.com/jafin/mRemoteNG/commit/e7a0619a552581ffdc37ed7ebbf285450fff3863.patch) requires prerequisite changes and leaves production integration unfinished. Credential security fits our priorities, but this import is not self-contained or quickly verifiable.
  - dissent - gemini: NEEDS_HUMAN - Aligns with credential security priorities, but far too large and complex for automated pre-approval.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/FileProtection/ConnectionFileKey.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/FileProtection/ConnectionFileKeyProtection.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/FileProtection/DpapiKeyProtector.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/FileProtection/KeyProtectionException.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/FileProtection/RecoveryPasswordKeyProtector.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/SymmetricEncryption/PerFileKeyCryptographyProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Security/FileProtection/PerFileKeyCryptographyProviderTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/add-connection-file-key-slots/specs/connection-file-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/replace-default-connection-file-key/specs/connection-file-encryption/spec.md` - credential and crypto paths need human review regardless of intent

### `fcd265e963` security: key a hardened connection file on itself rather than on mR3m (#32)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/fcd265e96346c913dbaba60986a006d3c4dc7838) by Jason Finch
- **size:** 13 files (+642/-34)
- **score -2** - security review required (high)
- **triage:** security | value 3 | effort 5 | risk 5 | applies rewrite | WATCH
- **why:** Per-file random key wrapped to Windows account, on a hardened StorageFormatLevel we lack; deep crypto/file-format redesign, tripwire-gated, breaks upstream compatibility. Track jafin, decide separately.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork lacks StorageFormatLevel, ConnectionFileKeyProtection, and PerFileKeyCryptographyProvider. Adoption requires a broader migration and compatibility review, exceeding a small, quickly verifiable import.
  - dissent - gemini: REJECT - Depends on missing storage architecture, fundamentally alters crypto/format, cannot compile or import in isolation, and conflicts with our focus on stability.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Config/Serializers/XmlConnectionsDecryptor.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/ConnectionFileDefaults.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/FileProtection/ConnectionFileKeyProtection.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/IThreadSafeCryptographyProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/SymmetricEncryption/PerFileKeyCryptographyProvider.cs` - credential and crypto paths need human review regardless of intent

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

### `42f6eaaf62` fix: give the notification panel one ordering point across threads (#11)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/42f6eaaf62bcc6cd83538304a1c0b8184eb50f6c) by Jason Finch
- **size:** 6 files (+545/-74)
- **score 4** - keep an eye on it
- **triage:** bugfix | value 3 | effort 3 | risk 3 | applies likely | IMPORT
- **why:** Builds on our #53 _pendingItems buffering; adds locked queue so UI-thread and worker messages keep order. Base matches our writer; drop openspec docs, keep tests.

### `47c8b9b978` perf: async bounded-parallel port scan that stays responsive and cancels

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/47c8b9b97817123179118e2b401e1486c61ddb2a) by Jason Finch
- **size:** 2 files (+147/-141)
- **score 4** - keep an eye on it
- **triage:** perf | value 3 | effort 3 | risk 3 | applies conflict | WATCH
- **why:** Our PortScanner still thread+Ping fan-out (8119ae123 added connect timeout only). Bounded Parallel.ForEachAsync with cancellable TCP is real gain; conflicts with our unsigned-range and timeout edits. No open issue drives it.

### `4cc0535857` feat: single "Port Range" checkbox with start/end ports on one line

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4cc05358576979d48fdc944d6e309f28189d32d9) by Jason Finch
- **size:** 2 files (+91/-54)
- **score 4** - keep an eye on it
- **triage:** refactor | value 2 | effort 2 | risk 2 | applies conflict | WATCH
- **why:** Our PortScanWindow still has ngCheckFirstPort/ngCheckLastPort. Cosmetic layout merge; needs Language.FirstPort resource cleanup. Low user value, defer.

### `f4ff408956` feat: compact connection tree to reduce horizontal footprint (#5)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f4ff4089563f42252d97a689e57d22a50ca23f68) by Jason Finch
- **size:** 6 files (+240/-18)
- **score 4** - keep an eye on it
- **triage:** feature | value 3 | effort 3 | risk 3 | applies conflict | WATCH
- **why:** Compact tree: hides single root, 12px indent, capped Name column. Touches vendored TreeRenderer static and our #149/#134 tree paths; needs lab UI check before import.

### `647d1dd630` fix: keep the splash screen up until the main window is shown

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/647d1dd6301a65ef58214ba2898c536df10f1c6e) by local
- **size:** 2 files (+8/-7)
- **score 3** - keep an eye on it
- **triage:** bugfix | value 2 | effort 1 | risk 3 | applies likely | WATCH
- **why:** Builds on our Piero-93 CloseSplash plumbing; only password dialog closes splash, compat/load-error dialogs could hide behind it. Cosmetic gain, hang-looking risk.

### `f94eba33b1` feat: port scan accepts full IP in one field and supports IPv6 ranges

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f94eba33b165ad51dfec1db5b1a795022a6b94a0) by Jason Finch
- **size:** 5 files (+112/-520)
- **score 1** - keep an eye on it
- **triage:** feature | value 2 | effort 3 | risk 3 | applies conflict | WATCH
- **why:** Our PortScanner already has the uint/65536 cap this builds on; IPv6 ranges + single-field IP are new but rewrite PortScanWindow UI (-520 lines). Low demand, no issue asks for it.

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

### `fb4478483e` Debounce the resize on a window state change too

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/fb4478483e40bc8dee542c21586afbd7f09e65d8) by local
- **size:** 1 files (+7/-4)
- **score 6** - already covered or rejected at triage
- **triage:** bugfix | value 3 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Our Resize() already debounces every path including window-state changes (4b52f0ebb, #69); no immediate DoResizeClient() remains there.

### `014d719de0` docs(openspec): archive four completed changes and promote their specs

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/014d719de052b298a82950389caa97e96e505e18) by Jason Finch
- **size:** 29 files (+1192/-0)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies likely | REJECT
- **why:** OpenSpec bookkeeping for that fork's SFTP work. No code, no relevance here.

### `06abf2cf01` Revert "Fix RDP mouse capture after fullscreen leave and fine tune scroll edge"

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/06abf2cf01d7361af4b54af00fd0821d6e0009e6) by guvity
- **size:** 2 files (+28/-281)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Reverts custom mouse capture logic for passive RDP monitoring files (RdpProtocol6/RdpInputBlocker) that do not exist in our main branch.

### `0e168f373a` fix: use current connection when validating native RDP tunnel

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/0e168f373ae1499ddeb1f804c27b9db6bc515857) by sanay
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fixes the fork's own NativeMstsc RDP mode (RdpClientMode enum); our fork has no such feature, so the guarded branch does not exist here.

### `108b95a586` refactor(ssh_dotnet): make FormatBytes static (S2325)

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/108b95a586e5197d94123762c21e04f7d25049f2) by Dawie Joubert
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Touches fork-specific ProtocolSshDotNet.cs (joubertdj SSH.NET protocol); file does not exist in our fork. Trivial static modifier.

### `125cfbc912` Shorten native RDP client label

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/125cfbc912215505cab034ac3aea162df3513a64) by sanay
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Label tweak on RdpClientMode.cs, a file that does not exist in our fork (no native mstsc client mode).

### `1454d94661` Remove redundant test adapter

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/1454d94661240cd464e1308b06d7bb5402197cb8) by sanay
- **size:** 1 files (+0/-51)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Deletes a test adapter (OptionsStoreSettingsAdapter) that exists only in that fork; our tests never had it.

### `15ebcd18df` docs(openspec): make the three manual checks a checklist (#60)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/15ebcd18dfe01d45045f27b9aab7cc54427f8d28) by Jason Finch
- **size:** 1 files (+28/-0)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Manual test checklist for jafin's on-demand-decrypt openspec change; no openspec/ dir or that feature here. Our #120 fix already does one derivation per load.
- **security flags:**
  - `security-code` (high) in `openspec/changes/decrypt-connection-secrets-on-demand/tasks.md` - credential and crypto paths need human review regardless of intent

### `195df32f12` Changed PublicGetDirectChildConnections to static

- **fork:** [julesbobb/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/195df32f12d5e0dc7a0bb88b29a85adad0367581) by Julian Bobbett (DHCW - Software Development)
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Tweaks fork-only test wrapper PublicGetDirectChildConnections; method absent in our ConnectionTreeWindow.cs. Nothing to apply.

### `19e1ad0b80` docs(openspec): propose refusing a storage format level this build cannot read

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/19e1ad0b806164121e2bf243460b1f5b724670b2) by Jason Finch
- **size:** 3 files (+168/-0)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Proposal against jafin's StorageFormat/hardened-level feature; no StorageFormat class in our fork, so unknown-level downgrade risk does not exist here.

### `1ab27bc1d2` Document trusting the RDP publisher

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/1ab27bc1d255b6397a82844331b29f5575aa07d6) by sanay
- **size:** 1 files (+33/-0)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Documents publisher trust for that fork's native mstsc signed-.rdp launcher; we have no NativeRdpLauncher or docs/native-rdp-signing.md.

### `1fcc675228` docs(openspec): record that the classic export opens in upstream

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/1fcc6752283c79bc4ee17c82129edd8614f2d6b3) by Jason Finch
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** jafin-only openspec task checkbox for their storage-format feature; no openspec/ tree here.

### `2b1ed1b8e1` Firefox组件崩溃问题待解决

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2b1ed1b8e12cec4cc37be62c0cbbe41eb3dc407b) by Hovn
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Comment-only mojibake on obsolete Gecko/Xpcom code; our fork removed that engine and the commit fixes no executable behavior.

### `2c2e19d4cf` docs: scale build/test verification to the change

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2c2e19d4cf397defb7575f27074bab37f9de5e75) by Jason Finch
- **size:** 1 files (+22/-2)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Loosens our CLAUDE.md full-build + full-suite rule; contradicts project policy and the i9/64GB always-full-run decision.

### `334ad2463c` ci: switch v4 workflow to manual dispatch only (avoid auto-builds during testing)

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/334ad2463ce02fca43475e26c0352202db298576) by Claude Code
- **size:** 2 files (+6/-3)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fork-private CI workflow tweak for their own passive-rdp test branch; file does not exist in our repo, zero relevance.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-1772-v4.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `34547cf36b` Use supported SQL serialization tests for native RDP mode

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/34547cf36b97ef1b48aa6f461dd0e1335d6d7936) by sanay
- **size:** 1 files (+1/-8)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Test tweak for that fork's RdpClientMode/native-mstsc feature; neither the feature nor the test file exists here.

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

### `3e0751ec1c` docs(openspec): close out manual verification for the KDF change

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/3e0751ec1c840af2e86af30cf2ed5051b6da6ee4) by Jason Finch
- **size:** 1 files (+2/-2)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** jafin-only openspec task checklist; we have no openspec/ tree and the KDF change it tracks is jafin's, not ours.

### `3e249e9198` chore(rdp): stop writing connection bar diagnostics to file (bar fix confirmed)

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/3e249e91986e07c9daa08d1639e28081360067f7) by Claude Code
- **size:** 2 files (+10/-12)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Removes debug file logging from guvity's experimental connection-bar pinning feature, which does not exist in our fork.

### `43c0e6c249` security: propose verifying host keys on every SSH connection (#21)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/43c0e6c249e3546c6bee031fb732b99fc0ffcb4f) by Jason Finch
- **size:** 3 files (+201/-0)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Openspec proposal for jafin's native SSH terminal/SFTP panel host-key gate. We use PuTTY; no SftpSession or HostKeyGate exists here.

### `4550ec4519` test: note why the call-site guards need an in-repo build

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4550ec45192913ec441e414eb70dd71cc09afa18) by Jason Finch
- **size:** 1 files (+8/-1)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Comment-only edit on jafin-specific SettingsSecretCallSiteTests (plus stray BOM). File absent in our tree.

### `47a0b7c9c5` Revert "Re-run the resize when the panel moved again while it was applied"

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/47a0b7c9c5cbdda6c5079da353d827139d16c623) by local
- **size:** 1 files (+0/-15)
- **score 4** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **our issue:** #177
- **why:** Revert of that fork's own settled-size re-pass; our DoResizeClient never had it. Size guard already present in ours. Nothing to import.

### `48f219afa8` Fix desktop scale factor serialization compatibility

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/48f219afa80ff240cae45d55e2fa86549837f811) by sanay
- **size:** 1 files (+6/-6)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fork-local shim: our fork has no RdpFileSerializer.cs and DesktopScaleFactor stays typed enum RDPDesktopScaleFactor. String-switch is regression, not fix.

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

### `551716f1c9` docs(openspec): fix requirements that contradict themselves or the code

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/551716f1c9f2eac00ee6d54dbf0197d31fad92a8) by Jason Finch
- **size:** 4 files (+29/-6)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Openspec docs for jafin's own AEAD/master-password/logging changes; no openspec/ dir here and none of those features exist in our fork.
- **security flags:**
  - `security-code` (high) in `openspec/changes/encrypt-sql-backend-with-aead/specs/sql-backend-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/replace-default-connection-file-key/specs/connection-file-encryption/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/require-sql-master-password/specs/sql-backend-encryption/spec.md` - credential and crypto paths need human review regardless of intent

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

### `5f67036c47` docs(sftp): record the maintainer's smoke test of the file manager

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/5f67036c470e41e1ea589d22b2bbd7e0605b5086) by Jason Finch
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fork-private openspec task checklist for jafin's SFTP panel; no code, nothing to import.

### `62a651edd3` Update CODEBASE_REVIEW.md with implementation status for all fixes and new features

- **fork:** [MyLabs-LLC/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/62a651edd3b87a94bb6fbf68c231ce5985934981) by Cursor Agent
- **size:** 1 files (+33/-15)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Fork-internal CODEBASE_REVIEW.md status update; file does not exist here and tracks their fork's changes.

### `64b35d7f04` docs(openspec): record manual format-level verification for the KDF change

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/64b35d7f0496020241b2a894fb3cabb16183124e) by Jason Finch
- **size:** 1 files (+3/-3)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fork-private openspec notes for jafin's KDF format change; our fork pins HMAC-SHA1 (RFC 6070 test) and already fixed per-field KDF.

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

### `6b6e16146d` fix(sftp): widen the Modified column so the time is not clipped

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6b6e16146d55d1858c8ed732c90f0a0ac3c86b84) by Jason Finch
- **size:** 1 files (+3/-1)
- **score 4** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** FilePaneControl.cs does not exist in our fork; column-width tweak on jafin's SFTP file-transfer feature we never imported. Only relevant if that feature lands.

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

### `76b39e5afc` chore: remove migration trigger

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/76b39e5afc647c902a6daba9ab8846a7465124d8) by sanay
- **size:** 1 files (+0/-1)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies likely | REJECT
- **why:** Deletes a fork-private CI trigger file that never existed here.

### `7ac6ed40e3` Fix last 2 SonarCloud JS issues: log caught exceptions

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7ac6ed40e317eb11f3fffc313f8723f17cde7e2d) by Eran Markus
- **size:** 1 files (+4/-4)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Edits xterm-terminal.html which does not exist in our fork; fork-specific SSH terminal, SonarCloud JS lint noise.

### `80b75874c8` task: update gitignore

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/80b75874c838c437f675814f30e98988dfe441d4) by Jason Finch
- **size:** 1 files (+3/-0)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies likely | REJECT
- **why:** Ignores all of .claude/ and .github/prompts; ours intentionally tracks .claude/commands runbooks. Fork-local tooling preference.

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

### `8932b4ac1c` docs(notifications): record maintainer verification of the panel fix

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8932b4ac1ca1c79f8af45b96758b5c2dd6ea0d27) by Jason Finch
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** jafin openspec task checkbox for their own notification-panel fix; internal bookkeeping, not applicable.

### `8a0b324f79` docs: clarify all remaining blocked tasks in MIGRATION_PROGRESS.md

- **fork:** [Morgadoo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8a0b324f79aabf9671133d3e7a6cf099fe84ed6f) by Luís Morgado
- **size:** 1 files (+51/-28)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** File MIGRATION_PROGRESS.md does not exist in our repository. It tracks a different fork's specific Avalonia UI migration status.

### `8d6be4cbd2` Remove unsupported native RDP serializer tests

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8d6be4cbd22389d6489b9459dd558d9b03b98d0b) by sanay
- **size:** 1 files (+0/-50)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Deletes tests for RdpFileSerializer, a class our fork does not have; nothing to apply.

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

### `a7399558fa` chore: remove temporary migration runner

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a7399558fa155a23c2454ce6f11844d8b06fd7d6) by sanay
- **size:** 1 files (+0/-145)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Deletes a fork-private migration workflow we never had; nothing to import. Native mstsc feature itself is separate work, not this commit.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/native-rdp-migration-runner.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `aa127bff0e` Update RDP signing diagnostics documentation

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/aa127bff0e5003b5d3b3e8d31c4f2eb6cb0cf7e9) by sanay
- **size:** 1 files (+7/-6)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Docs for that fork's native-mstsc rdpsign feature, which we do not ship. Nothing to document here.

### `ad24368030` Document SHA-256-only RDP signing configuration

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ad243680308e90b1e0ac839198efd95eb8ec4f30) by sanay
- **size:** 1 files (+91/-104)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Documents SHA-256-only signing for native-mstsc mode absent from our fork. Not applicable.

### `ae33255628` chore: remove temporary migration workflow

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ae33255628c3466c09d1d0c2192b2d8f23e4b940) by sanay
- **size:** 1 files (+0/-148)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Deletes their own temporary migration workflow, which we never had. Nothing to remove here.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/native-rdp-migration.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `b41ff4f4ec` Ignore local CLAUDE.md project notes

- **fork:** [k-meeks/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b41ff4f4ece4ca3c9ac027423a35bc9d6a3e7617) by Kyle Meeks
- **size:** 1 files (+3/-0)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Our fork commits CLAUDE.md as tracked project canon; ignoring it would break our workflow. Fork-local housekeeping only.

### `b4b156fba9` Correct RDP publisher certificate guidance

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b4b156fba903ed7563aba972c4448bcf8cc200d7) by sanay
- **size:** 1 files (+40/-26)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Docs for a fork-only native-mstsc .rdp signing feature (MREMOTENG_RDP_SIGN_CERT_THUMBPRINT); neither the feature nor docs/native-rdp-signing.md exists in our tree.

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

### `be0f0b6deb` Use code-signing certificates for RDP file publishing

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/be0f0b6deb519df86e4b7bd172419fcf522f5a71) by sanay
- **size:** 1 files (+56/-94)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Documents NativeRdpLauncher signing feature that does not exist in our fork.

### `c54eb13b17` docs(openspec): archive connection save durability and notification panel (#14)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c54eb13b1767f84f2614610248a28a20ba677fd0) by Jason Finch
- **size:** 8 files (+210/-2)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** openspec archive docs for jafin's process; no openspec/ here. Save-durability defects themselves already fixed in our #148 flush-path work.

### `c7415e4e45` Remove unsupported desktop scale tests

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c7415e4e4579bb6537392fc6d54de6f22bcf11e9) by sanay
- **size:** 1 files (+0/-31)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Deletes tests for that fork's RdpFileSerializer/DesktopScaleFactor; neither exists in our source tree.

### `c977c657ea` Document managed native RDP signing

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c977c657ea910d3cf9d3a98adacf365a1d402eef) by sanay
- **size:** 1 files (+27/-50)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Documents that fork's native mstsc RDP-file signing; we have no native mstsc launch mode or rdpsign path.

### `ca42b787e8` Native SSH: put the cursor in the terminal when it opens

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ca42b787e80fd2d75879966f7aaf8af66e9f3038) by local
- **size:** 2 files (+21/-2)
- **score 4** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fixes focus in that fork's WebView2/xterm native SSH protocol; ProtocolSshNative.cs and terminal.html do not exist here.

### `cbb9828294` Fix 2 remaining SonarCloud bugs for quality gate

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/cbb98282944e4a03c921e68aa6b9deeb6da3634a) by Eran Markus
- **size:** 2 files (+1/-7)
- **score 4** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Touches SSHTerminalBase.cs and SftpBrowserPanel.cs — fork-private features that do not exist in our tree. Nothing to apply.

### `cee638bbd2` docs(openspec): record manual verification for connection save durability (#13)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/cee638bbd21cc817481f9be689391944481ce37a) by Jason Finch
- **size:** 1 files (+21/-4)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fork-private openspec task checklist for jafin's own save-durability change; no openspec/ here, and our flush/debounce fixes (#148) already landed separately.

### `d03faccf48` fix(rdp): resolve ambiguous Message reference in ConnectionBarPinner (build fix)

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/d03faccf484ed801f9c324f0fb17e7aa3510aa4f) by Claude Code
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fixes build ambiguity in guvity's passive-RDP subclass ConnectionBarPinner in RdpProtocol6.cs. Neither the class nor the file exists in our fork.

### `d0e54e5d7e` Document native RDP signing diagnostics

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d0e54e5d7e3f3e360c32b1ab0d4ff2346efc66b7) by sanay
- **size:** 1 files (+68/-0)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies likely | REJECT
- **why:** Documents the rejected NativeRdpFileSigner feature; meaningless without it.

### `d18ad827cf` Fix all remaining SonarCloud code smells

- **fork:** [eran132/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d18ad827cff3dc973c3caad1f476421a9de748c3) by Eran Markus
- **size:** 5 files (+74/-81)
- **score 4** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fixes SonarCloud smells on eran132's custom WebView2 SSH terminal and SFTP browser files, which are not present in our fork.

### `d46e07d6aa` Update diagnostics for managed RDP signing

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d46e07d6aad354c072fb9221adc53b4a9f01f553) by sanay
- **size:** 1 files (+17/-13)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Docs for that fork's native mstsc RDP-file signing feature; we have no such feature or file. Nothing to import.

### `dec6fea904` docs(ssh_dotnet): Update Claude todo

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/dec6fea90437ac087750e302995b8c857ab1eaa0) by Dawie Joubert
- **size:** 1 files (+186/-0)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fork-internal TODO/plan text file for their own SSH_DotNet feature branch; no code, no relevance to our fork.

### `e088263795` Document rdpsign compatibility fallback

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e0882637954a3afdfa19fdca54dc538d855c9c3d) by sanay
- **size:** 1 files (+33/-6)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Docs for docs/native-rdp-signing.md and rdpsign fallback that do not exist in our fork; also superseded by df13f16 in-process signer.

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

### `e7e7987995` docs(sftp): record section 7 verification results

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e7e798799513674ec37d5bfc7fc283feb2dc87b6) by Jason Finch
- **size:** 1 files (+5/-5)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** openspec task checklist for jafin's SFTP browser panel; no openspec directory or SFTP panel here. Doc-only, nothing to import.

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

### `f0bce8507a` docs(openspec): restore the manual-check steps a merge dropped (#61)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f0bce8507a52ea1e18ffbf28e968746ec60b1cbf) by Jason Finch
- **size:** 1 files (+13/-0)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Manual-check steps for jafin's openspec on-demand secret decryption; we have no openspec/ tree and no such feature. Nothing to import.
- **security flags:**
  - `security-code` (high) in `openspec/changes/decrypt-connection-secrets-on-demand/tasks.md` - credential and crypto paths need human review regardless of intent

### `f2fca17298` Document native RDP file signing setup

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f2fca17298b624ae214e541f9bbad6ef0d8544df) by sanay
- **size:** 1 files (+180/-0)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Documents AlexanderTimofeev's rdpsign.exe signing for native mstsc launch; our fork has no rdpsign code, so doc is meaningless here.

### `f803b2bc9a` chore: trigger native RDP migration patch

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f803b2bc9a5fa26bafd674d07efe4c727421579f) by sanay
- **size:** 1 files (+1/-0)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies likely | REJECT
- **why:** Empty CI trigger file for a fork-only native-RDP patch pipeline; no code, nothing to import.

### `f8db148501` ci: add GitHub Actions workflow for v4 build (Release Portable x64)

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/f8db1485015a13560706a2e423752a2a7a593bb6) by Claude Code
- **size:** 2 files (+54/-1)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies likely | REJECT
- **why:** CI workflow specific to a third-party passive RDP branch; irrelevant to our existing .NET 10 pipeline.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-1772-v4.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `fdc967394a` Revert options store interface change

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/fdc967394a2c1762a168f63f83fb6a5723a485a2) by sanay
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Revert of f962933 in a fork-only OptionsStore; no corresponding code here.

### `050fab1bb3` fix: localize the storage format property grid labels

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/050fab1bb358a14d727b4d53a5561704f1632dd9) by Jason Finch
- **size:** 3 files (+26/-2)
- **score 3** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 1 | applies rewrite | REJECT
- **why:** Localizes labels for jafin's hardened StorageFormat property, which does not exist in our RootNodeInfo. Nothing to localize here.

### `2cc3891742` chore: tidy (#8)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2cc389174231d83d3c1146aa117d832f1eeadcc0) by Jason Finch
- **size:** 8 files (+180/-166)
- **score 3** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 1 | applies conflict | REJECT
- **why:** Pure var-style tidy plus IDE0007 suppression; no behavior change, conflicts with our explicit-type style and analyzer setup.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/UI/Forms/FrmConnectWithCredentials.cs` - credential and crypto paths need human review regardless of intent

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

### `126b08e691` Harden PowerShell 5.1 certificate hash parsing

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/126b08e691f92dfa196ee09bcc15e66a57d7c2c6) by sanay
- **size:** 1 files (+5/-5)
- **score 2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 2 | applies rewrite | REJECT
- **why:** Parenthesization fix in scripts/native-rdp-trust, a fork-only install script for a certificate trust feature we do not ship; nothing to patch here.
- **security flags:**
  - `build-script` (high) in `scripts/native-rdp-trust/Install-MRemoteNgNativeRdpTrust.ps1` - scripts execute on a maintainer machine

### `1fee3561e1` docs: let agents commit their own verified work

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/1fee3561e12ea0864339dd503f420cf7d104920a) by Jason Finch
- **size:** 1 files (+17/-1)
- **score 2** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 2 | applies conflict | REJECT
- **why:** Reverses our orchestrator-owns-commits policy in CLAUDE.md; contradicts tripwire pre-commit design and agent scope rules.

### `20f9fd9d29` Add PKCS package for managed RDP signing

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/20f9fd9d29db41e3f1f04ab2d76986cbb40c92ac) by sanay
- **size:** 1 files (+1/-0)
- **score 2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 2 | applies likely | REJECT
- **why:** Dependency-only for a signed .rdp export feature we don't have; no consumer here. Tripwire path. Revisit only with the feature.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time

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

### `582603cd00` Show connection names in mstsc window titles

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/582603cd001d3a20124954f439d734da213593b7) by sanay
- **size:** 1 files (+2/-1)
- **score 2** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 3 | risk 1 | applies rewrite | REJECT
- **why:** Touches NativeRdpLauncher.cs, which does not exist in our fork; depends on that fork's native mstsc launch feature.

### `59518d6685` fix: offer the SFTP file manager for native SSH connections (#19)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/59518d6685fb8836a3e9fe753936b1813e9ea693) by Jason Finch
- **size:** 3 files (+69/-10)
- **score 2** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 3 | risk 1 | applies rewrite | REJECT
- **why:** Our fork has neither ProtocolType.SSHNative nor an SFTP file manager menu item; nothing to fix.

### `5bf814447e` Add comprehensive codebase review with improvement recommendations

- **fork:** [MyLabs-LLC/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/5bf814447e401f9a381bcc0d66f4bc896d52051d) by Cursor Agent
- **size:** 1 files (+601/-0)
- **score 2** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 2 | applies likely | REJECT
- **why:** A stale, AI-style v1.78.2 review should not become repository documentation; re-audit any surviving security concerns against current main instead.

### `672fb42812` chore: rerun native RDP validation after branch restore

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/672fb42812ab090808bae64f53b43389e278109d) by sanay
- **size:** 1 files (+5/-1)
- **score 2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 2 | applies rewrite | REJECT
- **why:** Fork-specific CI workflow cache-bust env var; no such workflow here, never import issue/fork-sourced CI changes.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/validate-native-rdp.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

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

### `8d609c04c0` Add elevated launcher for native RDP trust setup

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8d609c04c00ea5d5569c868c5fa47bb3d659ddcf) by sanay
- **size:** 1 files (+33/-0)
- **score 2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 2 | applies rewrite | REJECT
- **why:** Elevated .cmd wrapper for a native-mstsc trust script we do not ship; depends on that fork's whole native-RDP feature and an absent .ps1.
- **security flags:**
  - `build-script` (high) in `scripts/native-rdp-trust/Install-MRemoteNgNativeRdpTrust.cmd` - scripts execute on a maintainer machine

### `93fa7c8cca` chore: make the ssh fixture idempotent and install its key without a race (#20)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/93fa7c8cca5e20122f87fc7376bb59fbb8746edc) by Jason Finch
- **size:** 5 files (+188/-36)
- **score 2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 1 | applies rewrite | REJECT
- **why:** Test fixture for jafin's native-SSH/SFTP spike and openspec docs; neither the spike nor openspec exists in our fork. Nothing to import.
- **security flags:**
  - `build-script` (high) in `spikes/native-ssh-terminal/fixture/down.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `spikes/native-ssh-terminal/fixture/keys.ps1` - scripts execute on a maintainer machine
  - `build-script` (high) in `spikes/native-ssh-terminal/fixture/up.ps1` - scripts execute on a maintainer machine

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

### `c566dc4c21` fix: keep search box default text after filter applied

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c566dc4c21b37abc733627d153fcd53a28232f8a) by AlexanderTimofeev
- **size:** 1 files (+1/-1)
- **score 2** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 2 | applies likely | REJECT
- **why:** Compares user text to localized prompt string; our _settingDefaultText flag already guards this path. Masks whoever sets Text externally instead of fixing it.

### `d33415860a` Enable build on push

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/d33415860ab7244838c42375e5ee223eb4c86bf9) by guvity
- **size:** 1 files (+3/-0)
- **score 2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 2 | applies conflict | REJECT
- **why:** The target workflow and legacy branch do not exist; current main-branch push CI already provides the relevant build coverage.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-1772-build.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `d6feb5a219` Document native RDP trust bootstrap

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d6feb5a21919bcd5c6e53757f1d63172d66b0a48) by sanay
- **size:** 1 files (+79/-0)
- **score 2** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 2 | applies rewrite | REJECT
- **why:** README for a trust-bootstrap installer editing machine RDP policy; feature absent here and out of scope for our RDP ActiveX path.
- **security flags:**
  - `build-script` (high) in `scripts/native-rdp-trust/README.md` - scripts execute on a maintainer machine

### `ffc530f785` Test native RDP signing diagnostics helpers

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ffc530f7853ef7c60a4d52471508756a20c36b15) by sanay
- **size:** 1 files (+18/-0)
- **score 2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 1 | applies rewrite | REJECT
- **why:** Tests for NativeRdpFileSigner/NativeRdpLauncher, a fork-only native mstsc launcher we don't have; no classes to test here.

### `08de05d731` chore: update claude to be less restrictive on commits

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/08de05d7314dc42af1aa8fbf65130b059c1af5c1) by Jason Finch
- **size:** 1 files (+0/-29)
- **score 1** - already covered or rejected at triage
- **triage:** chore | value 0 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Deletes our repo/CI rules from a copied CLAUDE.md in jafin's fork. Fork-local, nothing to import.

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

### `5bc3b24234` fix(ci): base the pre-release version on csproj when it is ahead of the tags

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/5bc3b24234192f5d361ec8264169f6f978325489) by Jason Finch
- **size:** 1 files (+27/-10)
- **score 1** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 2 | applies rewrite | REJECT
- **why:** We have no prerelease.yml; release model is rolling nightly plus vX.Y.Z tags. Beta-tag versioning does not apply.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/prerelease.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `7ab0def092` Make remote sessions easier to focus and carry

- **fork:** [YuLiangLin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7ab0def09286e44aad1c61b702668bd2c28d2371) by Nathan Lin
- **size:** 16 files (+125/-63)
- **score 1** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 3 | risk 3 | applies conflict | REJECT
- **why:** Repurposes DoubleClickOnTabClosesIt into fullscreen toggle (breaking setting rename); log4net cwd fix already ours via AppContext.BaseDirectory. Touches frmMain/SqlServerPage unseen.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

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

### `fa7d7056ce` Fix certificate import on Windows PowerShell 5.1

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/fa7d7056ceeb459e2730b73c7ae000d565538e0c) by sanay
- **size:** 1 files (+8/-2)
- **score 1** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 2 | risk 2 | applies rewrite | REJECT
- **why:** Fixes a script (scripts/native-rdp-trust) that exists only in that fork; no such feature here. Trust-store script also security-sensitive.
- **security flags:**
  - `build-script` (high) in `scripts/native-rdp-trust/Install-MRemoteNgNativeRdpTrust.ps1` - scripts execute on a maintainer machine

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

### `0c88565c4c` fix: treat localized search prompt as empty

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/0c88565c4c1130dc31cc99b6db55b70509d3e189) by sanay
- **size:** 1 files (+12/-4)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Our ApplyFiltering already treats Language.SearchPrompt as empty (lines 510/522) and Escape clears text. Only delta is whitespace-only search and Escape RemoveFilter; negligible.

### `0e37c335f4` add serial connections to factory

- **fork:** [azet/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/0e37c335f4b07bd59b4510cfece97f396754d05d) by Aaron Zauner
- **size:** 1 files (+4/-1)
- **score 0** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 1 | risk 1 | applies likely | REJECT
- **why:** Serial protocol support is already fully implemented in our fork. The proposed commit also contains a syntax typo ('returm').

### `1b29a6f585` Test PowerShell RDP signing command escaping

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/1b29a6f585a09fde733e5422e2db847ce7fcd81b) by sanay
- **size:** 1 files (+9/-6)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** Test for NativeRdpFileSigner.BuildPowerShellSigningScript, a class absent from our fork; only meaningful if we ever import the native launcher feature.

### `1d97942a46` Also follow the panel when it is resized without the main window

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/1d97942a461c3784d8b3ee6c4e9a030695ae501a) by local
- **size:** 1 files (+7/-1)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **our issue:** #177
- **why:** Our RdpProtocol8.Resize already calls ScheduleDebouncedResize unconditionally (#69); no ResizeEnd deferral branch exists. #177 root cause is downstream repaint, not this.

### `1fba339843` refactor(ssh_dotnet): dispose per-connection CancellationTokenSource (S2930)

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/1fba339843b891009c1cb85702338dfdd4f1d092) by Dawie Joubert
- **size:** 1 files (+6/-0)
- **score 0** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** Targets fork-specific ProtocolSshDotNet.cs; no SshDotNet protocol exists in our fork, so the fix has nothing to apply to.

### `2332010865` fix: show the connection root again so a master password can be set

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2332010865105a28ce862f24bc8737c55b07ff71) by Jason Finch
- **size:** 3 files (+34/-45)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Reverts jafin's own root-promotion; our ConnectionTree never had _promotedRoot, root is visible and master password stays settable. Nothing to import.

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

### `2a9e7da749` Document PowerShell fallback for RDP signing

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2a9e7da749cfef954df523be9cb503f365bd218b) by sanay
- **size:** 1 files (+7/-29)
- **score 0** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** Documents a native RDP signing feature (rdpsign/mstsc launch) our fork does not have; docs/native-rdp-signing.md absent here. Nothing to apply.

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

### `4b276d68c2` fix(rdp): restore Linux clipboard sync

- **fork:** [YuLiangLin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4b276d68c2b3030ef3d4a0335691b7c4b04ebb56) by Nathan Lin
- **size:** 5 files (+16/-47)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 0 | effort 0 | risk 0 | applies likely | REJECT
- **why:** Already covered: our frmMain uses AddClipboardFormatListener/WM_CLIPBOARDUPDATE and ConDefaultRedirectClipboard default True.

### `4cba693678` fix(build): resolve compiler and analyzer warnings in mRemoteNG

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4cba6936787b77be6745d4ed88fa526b5f5593d8) by Jason Finch
- **size:** 18 files (+30/-24)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 0 | effort 0 | risk 0 | applies conflict | REJECT
- **why:** Warning backlog already zero here (DevLog uses Lock, etc.). Fork-specific analyzer cleanup on divergent files; nothing left to import.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Credential/PlaceholderCredentialRecord.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/KeyDerivation/Pkcs5S2KeyGenerator.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/UI/Controls/Adapters/CredentialRecordListAdaptor.cs` - credential and crypto paths need human review regardless of intent

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

### `67e469d62d` Use dedicated options store contract

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/67e469d62d24b3ced48634753906579dc3ee6ced) by sanay
- **size:** 1 files (+1/-12)
- **score 0** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** Interface swap on fork-only OptionsStore (SQLite options store); mRemoteNG/Config/Settings/Store does not exist here.

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

### `74a588edcf` fix(logging): stop truncating log levels, keep five backups, and close out the Serilog verification (#22)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/74a588edcf8cf223b4da462a412f37f8e4af95f3) by Jason Finch
- **size:** 11 files (+352/-39)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** Fixes jafin's Serilog migration; our fork still uses log4net (log4net.config maxSizeRollBackups=5, no truncation bug). Not applicable.

### `7dbf60c6d8` 调整：查看-新建连接面板菜单可自定义面板名称

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7dbf60c6d8a516e04856c47f5d5c99544d71baf3) by Hovn
- **size:** 1 files (+8/-1)
- **score 0** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Named-panel creation already exists in the chooser, with immediate tab-context renaming elsewhere; this obsolete mRemoteV1 path adds only redundant modal friction.

### `8249501850` Make Release-page publishing start the build

- **fork:** [YuLiangLin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8249501850a795fdb6bb9c8e241f424db76b9f58) by Nathan Lin
- **size:** 1 files (+6/-3)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 3 | applies conflict | REJECT
- **why:** Fork-private release trigger (v*-yll.* tags, release:published). Our model is vX.Y.Z tag push with make_latest; issue-sourced CI changes are off-limits anyway.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `83b66b5d8e` Build passive RDP with VS 2026 runner

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/83b66b5d8e0daa1d1324eb53d4da65b5df871f81) by guvity
- **size:** 1 files (+22/-6)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fork-private workflow file we don't have; our CI already runs windows-2025-vs2026 with MSBuild 18 and our build.ps1 handles VS detection. Nothing to import.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-build.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `868d1417ed` fix: offer the SFTP file manager for native SSH connections (#23)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/868d1417ed3c85687cc72c986d66eabe5f1308e0) by Jason Finch
- **size:** 0 files (+0/-0)
- **score 0** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** Empty diff (merge). Targets jafin's native-SSH protocol which we lack; our SFTP is SSHTransferWindow. Prerequisite absent, nothing to import.

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

### `8ddbfaa464` Default to the vs2015darkNG theme

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8ddbfaa46445e83b33890b035aca312dec4eb183) by local
- **size:** 2 files (+6/-6)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 3 | applies likely | REJECT
- **why:** Fork-owner taste: flips default theme to dark for every fresh install; behavior change with no reporter demand, would surprise existing users.

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

### `95fd90ccbc` chore: validate native RDP with full Visual Studio MSBuild

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/95fd90ccbc72f2311ca3406491abccc4787b711a) by sanay
- **size:** 1 files (+6/-2)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Workflow validate-native-rdp.yml does not exist here; our build.ps1 and CI already use full VS MSBuild because of the MSTSCLib COM reference. Issue-sourced CI changes are out of scope anyway.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/validate-native-rdp.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `960bff595a` chore(tests): silence analyzer warnings in serializer tests

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/960bff595a3c1923c5132f342c276383fbb33748) by Jason Finch
- **size:** 3 files (+10/-9)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Our copies already clean: no duplicate using System, zero `c.Name ==` comparisons left in MobaXTerm tests; warning backlog is 0.

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

### `b0624b55f6` fix(sftp): enable the File Manager menu item

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b0624b55f6db130d8adbab4f0f0c29687a1da7ff) by Jason Finch
- **size:** 1 files (+20/-4)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** Fixes brace bug in jafin's SFTP File Manager menu item; _cMenTreeToolsFileManager does not exist in our fork. Only relevant if SFTP panel imported.

### `b819715128` Support options store in repository tests

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b819715128adc165acebcced2ffacff5f03c7432) by sanay
- **size:** 1 files (+44/-5)
- **score 0** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** Adapter over fork-only OptionsRepository/OptionsStore/ISettingsStore; none of these types exist here. Stub adapter throwing NotSupportedException is test scaffolding, not user value.

### `bed7b51614` Revert "fix: focus the connection on a tab change, without the feedback loop"

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bed7b5161495bb958142d11fa7c4898a2008dad6) by local
- **size:** 2 files (+5/-38)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 3 | applies conflict | REJECT
- **why:** Reverts a commit never in our tree (_lastFocusedDocument/FocusActiveConnection absent); our #143 fix uses ActiveContent identity gate instead.

### `c16ef7b237` Remove unused options store contract

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c16ef7b23750c918222cc63a12907312fa7a8fe3) by sanay
- **size:** 1 files (+0/-22)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** IOptionsStore never existed in our fork (fork-local abstraction); nothing to remove.

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

### `d68681e512` Declare OptionsStore settings interface

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d68681e512011a19b6c4c938c7907ce850a5204d) by sanay
- **size:** 1 files (+104/-475)
- **score 0** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** Refactors a SQLite OptionsStore that does not exist in our fork; part of that fork's separate settings-store experiment.

### `d78f3317ec` Allow item name & handle revoked tokens + logging

- **fork:** [Zarlengo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d78f3317ec089aae42c2bf6c320d7ceba7ae5c0a) by Chris Zarlengo
- **size:** 7 files (+178/-39)
- **score 0** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** Patches Zarlengo-only Bitwarden connector (ExternalConnectors/BW absent in our fork); also removes UUID validation and adds noisy notification spam.

### `d9e9f4671a` build: make the T4 TextTemplating import conditional

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d9e9f4671a69081c3ca68b34f2ca4705b411eb96) by local
- **size:** 1 files (+3/-1)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Identical conditional T4 import already at mRemoteNG.csproj:743-745. Nothing to import.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

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

### `e60db04c2e` Add test adapter for options repository

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e60db04c2ed1a5471c3e54ed526af870213ae2aa) by sanay
- **size:** 1 files (+51/-0)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** Test adapter for fork-private OptionsStore/ISettingsStore abstraction; no such types here. Useless without importing whole settings-store rewrite.

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

### `f887f940a1` Fix RDP signing certificate instructions

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f887f940a13debc9ef199f9ee2197de1c3926319) by sanay
- **size:** 1 files (+72/-12)
- **score 0** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** Documents rdpsign signing for a native-mstsc mode our fork does not have (no rdpsign/native-rdp code here); doc without the feature is useless.

### `f962933575` Add dedicated options store contract

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f96293357511bdb677cb3645db90d9e9d076bd78) by sanay
- **size:** 1 files (+22/-0)
- **score 0** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 3 | risk 2 | applies rewrite | REJECT
- **why:** Interface for a fork-only SQLite OptionsStore we do not have; reverted by that fork itself (fdc9673). Nothing to import.

### `ff3ed60e88` Disable auto-update + fix Claude API key setup

- **fork:** [nickbeentjes/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ff3ed60e8807fb80b95c7227ee1cf59862127484) by Kees
- **size:** 2 files (+34/-3)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 3 | applies rewrite | REJECT
- **why:** Fork-private hacks: hard-disables update check (ours is GitHub-only by design) and stores Claude API key plaintext in AppData for a ClaudeChatWindow we don't have.

### `2014fe6976` feat(sftp): delete a directory with its contents, through the queue

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2014fe697654617b4d98a41144260ece865e8b31) by Jason Finch
- **size:** 15 files (+1246/-5)
- **score -1** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 5 | risk 3 | applies rewrite | REJECT
- **why:** Recursive delete on jafin's SFTP queue (DirectoryTransferExpander, FilePaneCommands); prerequisite panel absent in our fork.

### `28bf981f51` fix: never let refocusing the connection change the foreground window

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/28bf981f51c298af9932caf4dc4d0cae7956d2b0) by local
- **size:** 1 files (+21/-0)
- **score -1** - already covered or rejected at triage
- **triage:** bugfix | value 2 | effort 1 | risk 3 | applies conflict | WATCH
- **why:** Alt-Tab steal already fixed at PuttyBase level (#168, reporter-confirmed). frmMain early-return would blanket-skip refocus; focus paths are fragile (#143). Revisit only if #168 regresses.

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

### `4583efcebb` fix: resolve native RDP migration against current upstream

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4583efcebbab1e4c34a06f67b8ab3eca444000ad) by sanay
- **size:** 1 files (+105/-10)
- **score -1** - already covered or rejected at triage
- **triage:** chore | value 2 | effort 3 | risk 4 | applies rewrite | REJECT
- **why:** CI migration script patching our fork's derivative with RdpClientMode/native mstsc launch. Workflow itself worthless; underlying agent/native-mstsc-launch branch worth separate look for #182.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/native-rdp-migration.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

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

### `68e39d4ece` chore: publish native RDP validation as UTF-8

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/68e39d4ece6726c3c62389c0b6490c3f04ba959a) by sanay
- **size:** 1 files (+22/-30)
- **score -1** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 3 | applies rewrite | REJECT
- **why:** Fork-private validation workflow for agent/native-mstsc-launch branch; commits and pushes from CI. Not our infra.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/validate-native-rdp.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `8967a14434` chore: drop "Connection Manager" branding from product identity (#44)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8967a14434963f67c0a349dfda3db46fabc99e92) by Jason Finch
- **size:** 8 files (+24/-24)
- **score -1** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 3 | applies conflict | REJECT
- **why:** Cosmetic branding rename across CI workflows, AssemblyInfo, WiX installer; touches release infra for zero functional gain. Company/trademark strings are our own choice.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `ci-workflow` (critical) in `.github/workflows/nightly.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `ci-workflow` (critical) in `.github/workflows/prerelease.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `installer` (high) in `mRemoteNGInstaller/Package.wxs` - installer content ships signed to end users

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

### `ba514de601` Native SSH: close the tab when the session ends

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ba514de60148cd127ace217f9a5dce5c8c8f79dd) by local
- **size:** 1 files (+9/-3)
- **score -1** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 4 | risk 2 | applies rewrite | REJECT
- **why:** Targets ProtocolSshNative (WebView2 terminal) that does not exist in our fork; our SSH is PuTTY-based and already closes tab on process exit.

### `bb28c472f9` Test managed RDP file signing

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bb28c472f9dc5803abd248775dbdea234eb3ce92) by sanay
- **size:** 1 files (+30/-11)
- **score -1** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 4 | risk 2 | applies rewrite | REJECT
- **why:** Test-only change for NativeRdpFileSigner/NativeRdpLauncher (mstsc.exe .rdp signing) — feature not in our fork. Would need feature import first.

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

### `67deabc3dd` security: tell portable users the marker is what keeps their settings (#39)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/67deabc3dd793405a7bed915cd1a7f2b417d21d7) by Jason Finch
- **size:** 14 files (+262/-33)
- **score -2** - already covered or rejected at triage
- **triage:** docs | value 2 | effort 4 | risk 4 | applies rewrite | REJECT
- **why:** Release-note text for jafin's runtime portable.flag model; our edition is compile-time PORTABLE. Adopting the marker risks exactly the settings-loss it warns about.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `ci-workflow` (critical) in `.github/workflows/nightly.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `72ecec02c6` feat(session): make sessions focused and portable

- **fork:** [YuLiangLin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/72ecec02c6b27e1a3eb2cc47d1969bb5c7a1d216) by Nathan Lin
- **size:** 18 files (+629/-36)
- **score -2** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 4 | risk 4 | applies conflict | REJECT
- **why:** Our portable mode is compile-time PORTABLE with writable-folder fallback in SettingsFileInfo; Shift+F11 fullscreen already exists. Mixed bag with version bumps and zh resx; RegisterHotKey global hook risky.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `7dd9011eec` Enhance Prot_Event_Closed with exception handling

- **fork:** [Ahmed-ElHamidy/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7dd9011eec0bb331b14d2f4d5ba4b1535247d665) by Ahmed Omar ElHamidy
- **size:** 1 files (+13/-6)
- **score -2** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 2 | applies conflict | REJECT
- **our issue:** #142
- **why:** Blanket catch that swallows close errors. Our fork already guards this path (disposed checks, deferred QueueCloseTab NRE-guarded, 6c788ae45). Masking exceptions is a regression.

### `80e9528681` security: refuse a connection file whose format level is unknown, and scope the log (#29)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/80e95286816efe5af7b42656d50b90b45931cc79) by Jason Finch
- **size:** 30 files (+1296/-137)
- **score -2** - already covered or rejected at triage
- **triage:** security | value 2 | effort 4 | risk 4 | applies rewrite | REJECT
- **why:** Built on jafin-only StorageFormat root attribute (no mRemoteNG/Security/StorageFormat.cs here). Our ValidateConnectionFileVersion already refuses newer ConfVersion files; 30-file diff not portable.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/StorageFormat.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-11-add-ssh-agent-credential-resolver/.openspec.yaml` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-11-add-ssh-agent-credential-resolver/design.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-11-add-ssh-agent-credential-resolver/proposal.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-11-add-ssh-agent-credential-resolver/specs/ssh-agent-authentication/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-11-add-ssh-agent-credential-resolver/specs/ssh-credential-resolution/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/archive/2026-08-11-add-ssh-agent-credential-resolver/tasks.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/specs/ssh-credential-resolution/spec.md` - credential and crypto paths need human review regardless of intent

### `82c11574ec` Reference PKCS package for mRemoteNG

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/82c11574ec7db07fa710f3ded57d28085b80393b) by sanay
- **size:** 1 files (+5/-0)
- **score -2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 4 | applies conflict | REJECT
- **why:** Adds nested mRemoteNG/Directory.Build.props (would shadow root props, break CPM/analyzer config) only to support their rdpsign feature we do not have.

### `9ac896fcca` chore(deps): drop framework-provided System.DirectoryServices reference

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9ac896fcca6ebbc4dbc26c907e97cce54fbbf9f7) by Jason Finch
- **size:** 2 files (+0/-2)
- **score -2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 4 | applies likely | REJECT
- **why:** Premise wrong: System.DirectoryServices is NuGet-only on .NET 10, used by LAPSHelper DirectorySearcher. Removing the reference breaks ExternalConnectors build.
- **security flags:**
  - `dependency-manifest` (high) in `ExternalConnectors/ExternalConnectors.csproj` - a new or repointed package can pull arbitrary code at restore time

### `9f03e57d3b` Use MSBuild for passive RDP portable publish

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/9f03e57d3b91f45a2976d3f43a1030a828d3ee5e) by guvity
- **size:** 1 files (+5/-2)
- **score -2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 2 | applies conflict | REJECT
- **why:** Switches their custom passive-rdp workflow to MSBuild to dodge MSB4803 COM-ref failure; our build.ps1 and CI already use full MSBuild. Workflow file not in our repo.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-build.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `a10b17354a` chore: drop the unused WebView2 WPF assembly, and migrate to slnx (#47)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a10b17354a02fc4c8c93410e2942e2a44f635b3b) by Jason Finch
- **size:** 17 files (+100/-193)
- **score -2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 3 | applies conflict | REJECT
- **why:** slnx rename + 7 workflow edits, no user benefit; our CI/build.ps1 diverged. WebView2 package still referenced here.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `ci-workflow` (critical) in `.github/workflows/ci.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `ci-workflow` (critical) in `.github/workflows/codeql.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `ci-workflow` (critical) in `.github/workflows/nightly.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `ci-workflow` (critical) in `.github/workflows/pr_validation.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `ci-workflow` (critical) in `.github/workflows/prerelease.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `ci-workflow` (critical) in `.github/workflows/sonarcloud.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `build-script` (high) in `build.ps1` - scripts execute on a maintainer machine
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `spikes/native-ssh-terminal/NativeTerminalSpike/NativeTerminalSpike.csproj` - a new or repointed package can pull arbitrary code at restore time

### `a71cb57380` input.cs UI细节调整2

- **fork:** [Hovn/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a71cb573806a810ae689bb8b8129ed09e0d15c60) by Hovn
- **size:** 1 files (+2/-2)
- **score -2** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 2 | applies conflict | REJECT
- **why:** Cosmetic adjustments to programmatic legacy form `input.cs` from `mRemoteV1`, which was replaced in our fork by designer-based `FrmInputBox`.

### `a9c3372e6b` Native SSH: adjust the font size from the terminal

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a9c3372e6b5143547ca1d5f8945d60359359de89) by local
- **size:** 1 files (+42/-2)
- **score -2** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 2 | applies rewrite | REJECT
- **why:** Targets fork-only xterm.js Native SSH terminal (terminal.html absent here). Font zoom useless without that host feature.

### `b41bf24e21` Make "Fit to panel" actually follow the panel for RDP

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b41bf24e218064c3ce7804a2cf11aecb1c187680) by local
- **size:** 1 files (+30/-10)
- **score -2** - already covered or rejected at triage
- **triage:** bugfix | value 2 | effort 2 | risk 3 | applies conflict | REJECT
- **our issue:** #177
- **why:** Our RdpProtocol8 already resizes session for FitToWindow, Fullscreen and SmartSize (DoResizeClient:250). Patch targets older gate. Area under active #177 instrumentation; would conflict.

### `bbccbb2a70` Refactor Enter key multi-connection open logic to reduce cognitive load.

- **fork:** [julesbobb/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bbccbb2a701beae624f50f63acf7310cf5770af6) by Jules Bobb
- **size:** 1 files (+56/-33)
- **score -2** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 3 | risk 3 | applies conflict | REJECT
- **why:** Refactors the OpenMultipleConnectionsWithEnter feature, which is not implemented in our fork, causing compilation failures.

### `be611b897c` Sign temporary native RDP files when certificate is configured

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/be611b897ceac5a5e26ccbfab16303a35346f9e6) by sanay
- **size:** 1 files (+99/-1)
- **score -2** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 4 | risk 4 | applies rewrite | REJECT
- **why:** Depends on absent NativeRdpLauncher (mstsc-launch mode); spawns rdpsign.exe per launch with env-configured thumbprint. No prerequisite here.
- **security flags:**
  - `process-exec` (critical) in `mRemoteNG/Connection/Protocol/RDP/NativeRdpLauncher.cs` - added code spawns a process or evaluates a string as code

### `ce1a212163` ci: validate native RDP changes on the current branch

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ce1a212163c0930ee26e0068d5075eb506d85eaf) by sanay
- **size:** 1 files (+29/-50)
- **score -2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 3 | applies rewrite | REJECT
- **why:** CI workflow for fork-specific native-RDP branch; our pr_validation/nightly already build and test; no such feature here.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/validate-native-rdp.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

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

### `e6f45391e3` Notify users when RDP file signing fails

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e6f45391e35851464b589b153acd24b1508c70ca) by sanay
- **size:** 1 files (+30/-3)
- **score -2** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 3 | risk 3 | applies rewrite | REJECT
- **why:** Depends on fork-private NativeRdpLauncher (mstsc + signed .rdp files) absent here; also adds a modal MessageBox in protocol path, against our no-dialog direction.

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

### `1bd9222e5e` feat(sftp): edit a remote file locally and write it back

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/1bd9222e5e045549f942288d92730e9fe37f8687) by Jason Finch
- **size:** 8 files (+768/-4)
- **score -3** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 5 | risk 4 | applies rewrite | REJECT
- **why:** Builds on jafin's SFTP browser panel (IFileSystemBrowser, FileManagerTab) which our fork lacks; process-exec surface; standalone import impossible.
- **security flags:**
  - `process-exec` (critical) in `mRemoteNG/UI/Controls/FileTransfer/ShellExternalEditor.cs` - added code spawns a process or evaluates a string as code

### `255186dc24` chore: run native RDP migration validation

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/255186dc241db4a6a878ad8c205cfae612e169f6) by sanay
- **size:** 1 files (+59/-0)
- **score -3** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 4 | applies rewrite | REJECT
- **why:** Fork-private CI: bot auto-commits a report to their agent branch, uses dotnet build (MSB4803 on our COM refs), contents:write. Nothing for us.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/validate-native-rdp.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `3bb3a8532b` Fix terminal output rendering by removing unreliable DataAvailable check

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/3bb3a8532b323b7d6013caf20fc9ea3e8a3bd8b3) by Dawie Joubert
- **size:** 1 files (+45/-51)
- **score -3** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** Fixes SSH_DotNet protocol which exists only in joubertdj fork (upstream PR #2997); our fork has no SSH_DotNet code.

### `41016b1f5b` chore: run fast compiler validation for native RDP

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/41016b1f5bc12911866ff8b235685edf7eb6be27) by sanay
- **size:** 1 files (+9/-12)
- **score -3** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 4 | applies rewrite | REJECT
- **why:** Fork-private CI workflow for their native-RDP agent branch; edits csproj at build time and pushes bot commits. Not our pipeline, security-critical path.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/validate-native-rdp.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `4178423f50` chore: validate native RDP with upstream build prerequisites

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4178423f50fe48c07322ba30417ce96232e67010) by sanay
- **size:** 1 files (+16/-2)
- **score -3** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 4 | applies rewrite | REJECT
- **why:** Same fork-private workflow, earlier iteration (choco VS2026 + t4 hack). Our build.ps1 already handles restore/T4; no benefit.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/validate-native-rdp.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `43b1a54773` test: close add-ssh-agent-credential-resolver and cover SecureTransfer's upload (#28)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/43b1a5477328a37cf8b1cc44627bc0952eb27cbb) by Jason Finch
- **size:** 5 files (+207/-5)
- **score -3** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** Tests jafin's ResolvedSshCredential/SecureTransfer rewrite via Docker SFTP fixture we lack; also edits CI workflow. Nothing to import without their SSH stack.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/ci.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `security-code` (high) in `openspec/changes/add-ssh-agent-credential-resolver/tasks.md` - credential and crypto paths need human review regardless of intent

### `45b5de6f95` Route native RDP signing through diagnostics-enabled signer

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/45b5de6f958f0260673631c3d43c4dfa1c287cb1) by sanay
- **size:** 1 files (+1/-327)
- **score -3** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** Refactor of fork-only NativeRdpLauncher/rdpsign signing; neither file exists here. No native-mstsc launch feature in our fork.

### `485ca7dcfc` Fix rdpsign certificate hash compatibility

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/485ca7dcfc228138496b1e60b2022ab59663866f) by sanay
- **size:** 1 files (+103/-15)
- **score -3** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** Fix inside fork-only NativeRdpLauncher (rdpsign SHA-1 fallback). Feature absent from our fork; nothing to apply.

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

### `50244d8ff6` ci: add a build-and-test workflow for dev and pull requests

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/50244d8ff6f6b8b3b47a72640b4599bd61e41b67) by Jason Finch
- **size:** 2 files (+98/-1)
- **score -3** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 4 | applies rewrite | REJECT
- **why:** Fork-sourced workflow change plus CLAUDE.md loosening the workflow guardrail. Our nightly already runs the suite; CI stays human-owned.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/ci.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

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

### `853850d33e` Adapt native RDP launcher to fork model

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/853850d33e315bcd71b84c2442364e8f1ad43f02) by sanay
- **size:** 1 files (+2/-4)
- **score -3** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** Adapts NativeRdpLauncher.cs / RdpFileSerializer, neither exists here; ParseDomainFromUsername lives on RdpProtocol in our fork. Fork-model glue, no standalone value.

### `b36ab7e53f` security: verify host keys on every SSH connection the application opens (#25)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b36ab7e53f25eec53c177bca9d75d152152317b5) by Jason Finch
- **size:** 17 files (+1293/-128)
- **score -3** - already covered or rejected at triage
- **triage:** security | value 2 | effort 5 | risk 4 | applies rewrite | REJECT
- **why:** Builds on jafin native SSH stack (HostKeyGate, SftpSession, SecureTransfer host keys) we do not have. SSH here goes via PuTTYNG which verifies host keys itself.

### `ba1e2cafa9` feat(ssh): enable agent authentication by default and offer hardware keys

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ba1e2cafa9de2a96be8167ad331fa6882cfb5730) by Jason Finch
- **size:** 10 files (+171/-43)
- **score -3** - already covered or rejected at triage
- **triage:** security | value 2 | effort 5 | risk 4 | applies rewrite | REJECT
- **why:** Flips defaults on jafin's Security/Ssh/Agent stack, which we do not have; nothing to apply without importing whole SSH.NET agent feature first.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Properties/OptionsCredentialsPage.Designer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Properties/OptionsCredentialsPage.settings` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/Agent/ISshAgentProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Ssh/Agent/SshNetAgentProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/add-ssh-agent-credential-resolver/design.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/add-ssh-agent-credential-resolver/specs/ssh-agent-authentication/spec.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `openspec/changes/add-ssh-agent-credential-resolver/tasks.md` - credential and crypto paths need human review regardless of intent

### `bf757e5fb2` feat(ssh_dotnet): add file-picker ("...") editor for the private key file property

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bf757e5fb241e1ae7c3fe927f1995c14ac08b5fb) by Dawie Joubert
- **size:** 2 files (+58/-0)
- **score -3** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 2 | risk 2 | applies rewrite | REJECT
- **why:** Current SSH protocols already use PrivateKeyPath plus an OpenFileDialog editor; the candidate targets absent SshDotNet-specific properties.

### `c261d36261` Keep remote sessions dependable in fullscreen

- **fork:** [YuLiangLin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c261d36261268b1d9dad449c4c943795577119cd) by Nathan Lin
- **size:** 11 files (+250/-317)
- **score -3** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 5 | risk 4 | applies rewrite | REJECT
- **why:** Fork-specific fullscreen host + '-yll' version parsing; touches csproj/AssemblyInfo. Our update check is GitHub releases/latest with plain 1.83.0 versions; #177 unrelated.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `c5606cccf5` Hide username/password

- **fork:** [Zarlengo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c5606cccf5d8b618df63b2b4e7e32bdf84584462) by Chris Zarlengo
- **size:** 4 files (+27/-31)
- **score -3** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** Incremental tweak to Zarlengo's own Bitwarden connector (ExternalConnectors/BW, VaultOpenbao fields, NotificationBridge) — feature absent from our fork; nothing to patch.

### `cc4ebc7f31` Improve RDP signing failure diagnostics

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/cc4ebc7f31a2397c5cac7bae3d1b9727e3252b35) by sanay
- **size:** 1 files (+103/-5)
- **score -3** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** NativeRdpLauncher / rdpsign flow does not exist in our fork (we use MSTSC ActiveX); diagnostics for a feature we lack.

### `db4b0d8c81` feat: say on export that the classic copy is protected more weakly

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/db4b0d8c81dc0603babc53a7c5d49df3b4a4c86b) by Jason Finch
- **size:** 5 files (+97/-1)
- **score -3** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** Depends on jafin's StorageFormatLevel/hardened-store feature, absent here. Warning is meaningless without that whole storage format branch.

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

### `00e3567a85` Adapt native RDP credential resolver to fork providers

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/00e3567a8518e0962f6795fe292b904819c1e0c5) by sanay
- **size:** 1 files (+20/-45)
- **score -4** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 3 | risk 4 | applies rewrite | REJECT
- **why:** Adapts resolver to that fork's own provider set: drops hostname param, swaps enum switch for string switch, loses XML docs. Regression for us.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Connection/Protocol/RDP/RdpCredentialResolver.cs` - credential and crypto paths need human review regardless of intent

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

### `5b3cbfb497` Revert the automatic focus on tab change

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/5b3cbfb4972b93380ffd5f97ebba9ea8f4d67a0e) by local
- **size:** 1 files (+5/-32)
- **score -4** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 3 | applies conflict | REJECT
- **why:** Our #143 handler already gates on real content change plus null-bounce detection; removing protocol refocus entirely would regress PuTTY keyboard input (#2237).

### `6b0ebf32fc` chore: add temporary native RDP migration workflow

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6b0ebf32fc909e1f179428430ec7b506514cbcdc) by sanay
- **size:** 1 files (+53/-0)
- **score -4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 5 | applies rewrite | REJECT
- **why:** Temporary CI workflow that auto-commits patches from a third fork with contents:write; fork-specific migration scaffolding, security hazard, nothing to import.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/native-rdp-migration.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `env-secret-access` (critical) in `.github/workflows/native-rdp-migration.yml` - added code reads credentials or CI secrets

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

### `7cc928f656` Fix rdpsign SHA-256 certificate hash handling

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7cc928f6562182236f19b0298c42fc3da36bca42) by sanay
- **size:** 1 files (+23/-24)
- **score -4** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 5 | risk 3 | applies rewrite | REJECT
- **why:** NativeRdpLauncher.cs and rdpsign feature do not exist in our fork; also drops SHA-1 support. Nothing to patch.

### `887562f346` chore: replace log4net with Serilog for file logging

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/887562f3461c907b4c9060bd0203987ff3cab1e8) by Jason Finch
- **size:** 14 files (+203/-57)
- **score -4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 4 | applies conflict | REJECT
- **why:** log4net→Serilog swap changes log line format, touches Options pages, deps manifest; we just fixed log4net.config path (f98b70264). No user benefit, deep review cost.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

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

### `98b4270a6b` ci: add a manual pre-release workflow that packages the binaries

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/98b4270a6b0cbc00faad0bce5b90b7ec69e582a7) by Jason Finch
- **size:** 1 files (+255/-0)
- **score -4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 4 | applies rewrite | REJECT
- **why:** Contradicts our two-release model (rolling nightly + vX.Y.Z tags, channels removed in #136). CI/secret-touching workflow from a fork never imported.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/prerelease.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `env-secret-access` (critical) in `.github/workflows/prerelease.yml` - added code reads credentials or CI secrets

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

### `aab55a75eb` fix: focus PuTTY without touching the window activation

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/aab55a75ebdf320b0e654f96d23c13dee42bd1bd) by local
- **size:** 3 files (+45/-1)
- **score -4** - already covered or rejected at triage
- **triage:** bugfix | value 2 | effort 2 | risk 4 | applies conflict | REJECT
- **why:** Derived from our tree; #168 fixed by bf99b7ad9 and active-content gate by 60eee0ea7. AttachThreadInput finally-detach broke mstscax attachment in #143 round 4.

### `b2507ffcb6` refactor(ssh_dotnet): introduce ISshClientAdapter seam for testability

- **fork:** [joubertdj/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b2507ffcb6eb6a841d1cc4b62362bf9530955ab2) by Dawie Joubert
- **size:** 3 files (+91/-8)
- **score -4** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 5 | risk 3 | applies rewrite | REJECT
- **why:** [Source](https://github.com/joubertdj/mRemoteNG/commit/b2507ffcb6eb6a841d1cc4b62362bf9530955ab2): Refactors an absent SSH.NET terminal stack; our SSH.NET usage is file transfer, leaving this untested adapter without a consumer.

### `bb0ade3af0` test: cover SftpSession against a real SFTP server (#27)

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bb0ade3af07c0fcacd5f502faef9e6aabd6dba3f) by Jason Finch
- **size:** 17 files (+1234/-94)
- **score -4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 3 | applies rewrite | REJECT
- **why:** Tests for jafin's SftpSession, absent here; adds Testcontainers/Docker dependency to test suite. Our live tests use real SQL/MariaDB rigs instead.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `network-download` (critical) in `mRemoteNGTests/IntegrationTests/Sftp/SftpSessionTransferTests.cs` - added code fetches remote content at build or run time
  - `dependency-manifest` (high) in `mRemoteNGTests/mRemoteNGTests.csproj` - a new or repointed package can pull arbitrary code at restore time

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

### `ffc927d181` Hand the keyboard to the connection when the tab changes

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ffc927d1818c5913576d01a67af3386023558995) by Lovasz Laszlo
- **size:** 1 files (+40/-5)
- **score -4** - already covered or rejected at triage
- **triage:** bugfix | value 2 | effort 2 | risk 4 | applies conflict | REJECT
- **why:** Our ConnDockOnActiveContentChanged already refocuses protocol on real content change with null-bounce gate (#143 fix, 60eee0ea7); fork's PuTTY exclusion would regress #2237.

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

### `3c4c8319f1` fix: refocus the active connection on keyboard activation

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/3c4c8319f1c01ae29bf961e6c04466172708cc24) by local
- **size:** 1 files (+45/-1)
- **score -5** - already covered or rejected at triage
- **triage:** bugfix | value 2 | effort 3 | risk 4 | applies conflict | REJECT
- **why:** Our WM_ACTIVATEAPP handler already has a reactivation-gated ActivateConnection retry (#110/#143/#168 work, DisableRefocus honoured); different structure, would conflict and re-risk the #143 focus saga.

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

### `59ef50c9aa` fix: focus the connection on a tab change, without the feedback loop

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/59ef50c9aa84714a86a5c213148a70d0f520f35e) by local
- **size:** 2 files (+38/-5)
- **score -5** - already covered or rejected at triage
- **triage:** bugfix | value 2 | effort 3 | risk 4 | applies conflict | REJECT
- **why:** Same focus feedback loop already solved by our #143 fix (ActiveContent gate + null-bounce). Alternate ActiveDocument/BeginInvoke design would collide with proven, reporter-confirmed code.

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

### `a9f6717b5e` Add one-click native RDP trust bootstrap

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a9f6717b5e02dab63e3cd252718af5c65225aa4a) by sanay
- **size:** 1 files (+292/-0)
- **score -5** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 4 | risk 4 | applies rewrite | REJECT
- **why:** Admin script creating machine-wide self-signed cert plus GPO trust for a native-mstsc mode we do not ship; widens trust surface, security-tripwire class.
- **security flags:**
  - `build-script` (high) in `scripts/native-rdp-trust/Install-MRemoteNgNativeRdpTrust.ps1` - scripts execute on a maintainer machine

### `aefb785309` Point update source to fork GitHub Pages and auto-generate page manifests from Release

- **fork:** [YuLiangLin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/aefb785309d98af283248a1c3bbb52282e50f639) by Nathan Lin
- **size:** 5 files (+229/-6)
- **score -5** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 4 | risk 4 | applies conflict | REJECT
- **why:** Re-adds update.txt/channel feeds via gh-pages. We deliberately collapsed to GitHub releases/latest (#136, 3e9f8a7bc); touches CI + secrets. Contradicts our model.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `env-secret-access` (critical) in `.github/workflows/Build_mR-NB.yml` - added code reads credentials or CI secrets
  - `build-script` (high) in `Tools/Publish-GitHubPagesUpdateManifest.ps1` - scripts execute on a maintainer machine
  - `network-download` (critical) in `Tools/Publish-GitHubPagesUpdateManifest.ps1` - added code fetches remote content at build or run time
  - `env-secret-access` (critical) in `Tools/Publish-GitHubPagesUpdateManifest.ps1` - added code reads credentials or CI secrets

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

### `b3a4a62de0` security: pin the credential file to the classic format

- **fork:** [jafin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b3a4a62de097af39dc8491a1816a163c2430a9ee) by Jason Finch
- **size:** 4 files (+51/-12)
- **score -5** - already covered or rejected at triage
- **triage:** security | value 1 | effort 4 | risk 4 | applies rewrite | REJECT
- **why:** Depends on their KeyDerivationPrf/storage-format series, absent here. Our credential file already stays classic (PBKDF2 HMAC-SHA1 pinned by known-answer test).
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Config/Serializers/CredentialSerializer/XmlCredentialPasswordEncryptorDecorator.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Config/Serializers/CredentialSerializers/XmlCredentialPasswordEncryptorDecoratorTests.cs` - credential and crypto paths need human review regardless of intent

### `b818e7562b` Fix Qodo review: infinite loop, race condition, SecureString leak, hardcoded string

- **fork:** [yosale2011/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b818e7562b8d0482f34be7167bd96d32223a0977) by Yosale2011
- **size:** 5 files (+44/-5)
- **score -5** - already covered or rejected at triage
- **triage:** security | value 1 | effort 4 | risk 4 | applies rewrite | REJECT
- **why:** Patches fork-specific StartupUnlockService/XmlKeyValidator absent in our tree (verified); our Runtime.EncryptionKey differs; we shipped own MasterPasswordGate hardening (#128). Diff also risks use-after-dispose SecureString.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/XmlKeyValidator.cs` - credential and crypto paths need human review regardless of intent

### `c7f1c4ec13` fix: make the connection lookup and focus check survive panel switches

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c7f1c4ec136b138935d5ab95d20f155ecfa5aaa4) by local
- **size:** 4 files (+53/-18)
- **score -5** - already covered or rejected at triage
- **triage:** bugfix | value 2 | effort 3 | risk 4 | applies conflict | REJECT
- **why:** Our frmMain already uses NativeMethods.GetFocus for focus checks (#118/#143 work); GotFocus-detach hack in RdpProtocol.Focus conflicts with our ConnDock identity-gate fix.

### `ce87788b1e` Add detailed native RDP signing diagnostics

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ce87788b1eef25f66cea98ea72420a96bd58e0b9) by sanay
- **size:** 1 files (+596/-0)
- **score -5** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 4 | risk 4 | applies rewrite | REJECT
- **why:** Diagnostics for a native rdpsign.exe temp-file signing feature this fork lacks (we set SignScope/Signature via ActiveX props); spawns processes, env-var config, tripwire-relevant.
- **security flags:**
  - `process-exec` (critical) in `mRemoteNG/Connection/Protocol/RDP/NativeRdpFileSigner.cs` - added code spawns a process or evaluates a string as code

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

### `ad85171bf6` chore: fix and revalidate native RDP migration

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ad85171bf66692535917780b392e4c34a8d7d0c1) by sanay
- **size:** 1 files (+18/-2)
- **score -6** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 5 | applies rewrite | REJECT
- **why:** CI workflow that patches source and pushes to a branch from the runner; agent-branch scaffolding for their migration. Never import issue-adjacent CI mutations.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/validate-native-rdp.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

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

### `82c92214d5` Retry RDP signing through PowerShell

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/82c92214d5ba30269ddf7a3b2ccc549774215965) by sanay
- **size:** 1 files (+121/-72)
- **score -7** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 4 | risk 5 | applies rewrite | REJECT
- **why:** Patches NativeRdpFileSigner, absent in our fork. Spawns PowerShell with string-built script around rdpsign.exe; security-sensitive process exec for a feature nobody requested here.
- **security flags:**
  - `process-exec` (critical) in `mRemoteNG/Connection/Protocol/RDP/NativeRdpFileSigner.cs` - added code spawns a process or evaluates a string as code

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

### `c41ad67f25` fix: focus the connection when the active tab changes

- **fork:** [lovaszlaszlo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c41ad67f2534bb5547318dd5ed1ffc84e42a571a) by local
- **size:** 2 files (+26/-1)
- **score -7** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 2 | risk 4 | applies conflict | REJECT
- **why:** Unconditional Protocol.Focus in ConnDockOnActiveContentChanged is exactly the #143 thief we removed; our gated contentChanged path + #168 foreground guard already cover it.

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

### `471d465b9f` chore: add one-shot native RDP migration runner

- **fork:** [AlexanderTimofeev/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/471d465b9f52a1e0320b1bbf2272d0d46af67159) by sanay
- **size:** 1 files (+145/-0)
- **score -8** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Fork-private CI migration runner with contents:write and source-patching from another repo. Never import issue/fork-sourced CI or workflow changes.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/native-rdp-migration-runner.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `env-secret-access` (critical) in `.github/workflows/native-rdp-migration-runner.yml` - added code reads credentials or CI secrets

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

### `cbfebf71c4` upload retarget code from lab

- **fork:** [appcompat-wx/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/cbfebf71c45b0e5a87a12e41cbbf536ce58f995b) by appcompat-wx
- **size:** 300 files (+202326/-0)
- **score -8** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** 300-file bulk upload of stale upstream tree with binaries and old CI workflows; nothing new, security flags everywhere.
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

### `b137a4de18` feat: .NET 10 升级 + WiX v5 installer with custom UI

- **fork:** [billowliu2/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b137a4de188a8a59f7d733c3e0adf7ce3bf6f659) by 刘涛(用户AI电池产品线)
- **size:** 14 files (+1569/-83)
- **score -12** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies conflict | REJECT
- **why:** We are already .NET 10 with WiX 6 MSI in CI (Package.wxs). Adds parallel InstallerV5, SQLite OptionsStore/ISettingsStore, SpecFlow, opaque binaries, bash build. Huge, redundant, no owner.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `installer` (high) in `mRemoteNGInstaller/Installer/Fragments/FilesFragment.wxs` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/InstallerV5/Components.wxs` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/InstallerV5/Files.wxs` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/InstallerV5/Product.wxs` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/InstallerV5/Resources/AppIcon.ico` - installer content ships signed to end users
  - `opaque-file` (high) in `mRemoteNGInstaller/InstallerV5/Resources/AppIcon.ico` - added file has no reviewable text diff
  - `license` (medium) in `mRemoteNGInstaller/InstallerV5/Resources/License.rtf` - licence edits change redistribution terms
  - `installer` (high) in `mRemoteNGInstaller/InstallerV5/Resources/License.rtf` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/InstallerV5/UI/MyWixUI_FeatureTree.wxs` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/InstallerV5/UI/SimpleCustomizeDlg.wxs` - installer content ships signed to end users
  - `build-script` (high) in `mRemoteNGInstaller/InstallerV5/build.sh` - scripts execute on a maintainer machine
  - `installer` (high) in `mRemoteNGInstaller/InstallerV5/build.sh` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/InstallerV5/tools/generate_files_wxs.py` - installer content ships signed to end users
  - `dependency-manifest` (high) in `mRemoteNGSpecs/mRemoteNGSpecs.csproj` - a new or repointed package can pull arbitrary code at restore time

### `b15a5738c4` Make fork releases self-updating and portable

- **fork:** [YuLiangLin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b15a5738c47fb386e2ca2b7e01f54dabdcbf91ee) by Nathan Lin
- **size:** 11 files (+800/-503)
- **score -12** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Fork-specific release/updater (-yll tags, gh-pages manifest, self-replacing exe). Ours already has GitHub releases/latest update check, nightly+stable CI, MSI. CI/process-exec flags.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `env-secret-access` (critical) in `.github/workflows/Build_mR-NB.yml` - added code reads credentials or CI secrets
  - `build-script` (high) in `Tools/Publish-GitHubPagesUpdateManifest.ps1` - scripts execute on a maintainer machine
  - `process-exec` (critical) in `mRemoteNG/App/Shutdown.cs` - added code spawns a process or evaluates a string as code
  - `process-exec` (critical) in `mRemoteNG/App/Update/PortableUpdateInstaller.cs` - added code spawns a process or evaluates a string as code

### `e2daf1270f` Use v2 passive RDP patcher for 1.77.2 release

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/e2daf1270f34e569981ef5e2f01babc293f38832) by guvity
- **size:** 4 files (+148/-488)
- **score -12** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Native RDP view-only already blocks input via IMessageFilter; this obsolete 1.77.2 patcher adds forced fullscreen/focus/scroll behavior that would regress normal sessions.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-1772-build.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `build-script` (high) in `Tools/passive-rdp-monitor-1772-v2.ps1` - scripts execute on a maintainer machine

# Upstream Radar - 2026-09-30

Commits on `mRemoteNG/mRemoteNG`'s own default branch that `robertpopa22/mRemoteNG` does not have yet. These are not a stranger's fork: the most common correct verdict is a conflict with our own rework of the same area, not "already covered", and a change can be worth taking as direction even when the patch itself cannot apply. Reported for awareness only - nothing here enters the import queue automatically.

| Tier | Count |
|---|---|
| Tier A - ready to cherry-pick | 0 |
| Tier B - worth porting by hand | 38 |
| Tier C - watch list | 1 |
| Quarantine - security review required before anything else | 20 |
| Tier D - rejected | 42 |

## Tier B - worth porting by hand

### `08353d4655` Anchor Options dialog OK/Cancel/Apply buttons to the right

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/08353d4655b784ed048d5633d9656dd029db6d8f) by Piero Biagini
- **size:** 1 files (+3/-0)
- **score 10** - worth doing, needs work
- **triage:** bugfix | value 3 | effort 1 | risk 1 | applies likely | IMPORT
- **our issue:** #198
- **why:** All three anchors are absent; git apply --check succeeds. Improves Options resizing, with limited relevance to #198 rather than resolving its broader DPI symptoms.
- **pre-approval:** **PRE-APPROVED** (codex:APPROVE / gemini:APPROVE)

### `46f82860a8` Revise restricted admin mode description in Language.resx

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/46f82860a8cc238667bbb0ccc2724aa68b80378a) by Dimitrij
- **size:** 1 files (+1/-1)
- **score 10** - worth doing, needs work
- **triage:** docs | value 3 | effort 1 | risk 1 | applies likely | IMPORT
- **why:** Current resource retains the misleading Kerberos-only description. Patch passes application check; matching history entry belongs outside HEAD ancestry.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:APPROVE / codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - UseRestrictedAdmin sets RestrictedLogon; UseRCG separately enables redirected authentication. SetCredentials suppresses passwords for both. Reviewer B's claimed NTLM and supplied-credential correction is absent from this diff.
  - dissent - gemini: REJECT - RdpProtocol.SetCredentials suppresses configured passwords when UseRestrictedAdmin is enabled to protect AD Protected Users, contradicting this tooltip's claim of supplied credential support.

### `0807102c91` test: cover the information_schema lookup that decides table existence

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/0807102c9101de527a835873b6825326a9bcf2b3) by Eran Markus
- **size:** 2 files (+81/-12)
- **score 8** - port the idea, the patch will not apply
- **triage:** refactor | value 5 | effort 3 | risk 4 | applies conflict | REIMPLEMENT
- **our issue:** #165
- **why:** Our probe handles catalog/schema and ODBC independently, but lacks dbo pinning. Adapt provider-specific regression coverage and schema discrimination while preserving direct-select safeguards.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork already handles catalog/schema matching and regression-tests it. The proposed extraction duplicates existing coverage and conflicts with our ODBC-aware implementation.
  - dissent - gemini: REJECT - Our fork already resolved table existence handling differently with ODBC marker support and verification fallback; upstream's query builder is incompatible with our architecture.

### `14d902276d` Update ExternalConnectors/DSS/SecretServerInterface.cs

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/14d902276de0cfbe18409d5908b52776ca15a17e) by Dimitrij
- **size:** 1 files (+7/-1)
- **score 8** - port the idea, the patch will not apply
- **triage:** security | value 4 | effort 2 | risk 3 | applies conflict | REIMPLEMENT
- **why:** Our modulus-derived RSA import lacks a minimum size. Adapt the 2048-bit check; review legacy-key compatibility and swallowed conversion exceptions.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork already derives RSA size from the imported modulus. The proposed minimum adds an unrequested compatibility restriction rather than fixing key generation.
  - dissent - gemini: REJECT - Already solved in commit 75ecd955 by deriving key size from the imported modulus. Upstream change duplicates and conflicts with our existing fix.

### `ac2988542d` fix: use a provider-specific table lookup instead of matching either column

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ac2988542d71b171123115b9518323bca87a4638) by Eran Markus
- **size:** 1 files (+12/-5)
- **score 8** - port the idea, the patch will not apply
- **triage:** bugfix | value 5 | effort 3 | risk 4 | applies conflict | REIMPLEMENT
- **our issue:** #165
- **why:** Our lookup still matches either column without dbo restriction. Adapt provider-specific checks while preserving ODBC positional parameters and the existing direct-query fallback.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork already handles catalog lookup with ODBC support and protective fallbacks. Schema filtering adds value, but this patch needs adaptation and regression coverage.
  - dissent - gemini: REJECT - Already solved better in commit c4da62c; this upstream change regresses ODBC and non-dbo schemas, and breaks our existing test suite.

### `dfb7a14dc3` Turkish translation file update

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/dfb7a14dc3be62e9c7ced9f6645ffcac88d85912) by Rıza Emet
- **size:** 1 files (+1061/-140)
- **score 8** - worth doing, needs work
- **triage:** feature | value 3 | effort 1 | risk 2 | applies likely | IMPORT
- **why:** Our Turkish resource blob exactly matches this commit's parent, confirming the translation update is absent and should apply cleanly. Validate resource keys and placeholders.
- **pre-approval:** **MANUAL-REVIEW** (codex:NEEDS_HUMAN / gemini:NEEDS_HUMAN)
  - dissent - codex: NEEDS_HUMAN - Adds 301 missing translations, but changes 119 existing strings and includes obsolete fork-specific resources; valuable coverage needs Turkish-language review and adaptation before landing.
  - dissent - gemini: NEEDS_HUMAN - Valuable i18n improvement completing 301 missing Turkish strings without code impact, but human maintainer judgement is required to evaluate translation quality and prune obsolete keys.

### `32645f92b6` fix: Japanese translation gaps and AdvancedPage label mis-binding

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/32645f92b652c7ca35170d3662b012585960d77f) by Taro Kawakami
- **size:** 5 files (+47/-17)
- **score 7** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 2 | risk 2 | applies conflict | REIMPLEMENT
- **why:** Our English reconnect bindings already differ, but Japanese gaps and hardcoded panel text remain. Adapt translations to our existing resource keys.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - AdvancedPage already uses CheckboxNoReconnect for automatic retries. Importing the proposed binding regresses English labels; useful Japanese translations require a fork-specific adaptation.
  - dissent - gemini: REJECT - Our fork already resolved the AdvancedPage label binding using CheckboxNoReconnect. Direct import causes schema conflicts and mislabeled UI; only Japanese translations warrant reimplementation.

### `4d62f0a6e5` fix: correct CheckboxAutomaticReconnect English value to match its actual usage

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4d62f0a6e5ccee65625a2ca728414f7a8ff6927e) by Taro Kawakami
- **size:** 2 files (+2/-2)
- **score 7** - worth doing, needs work
- **triage:** bugfix | value 2 | effort 1 | risk 1 | applies likely | IMPORT
- **why:** Our English resource still contains the misleading dialog wording. The patch passes git apply --check and clarifies automatic reconnection.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork's option displays a reconnection panel; automatic retries depend separately on NoReconnect. The proposed wording misrepresents that distinction.
  - dissent - gemini: REJECT - Our fork already resolved this in commit 3b8861c02 using CheckboxNoReconnect; importing upstream's conflicting resource edit creates duplicate labels on both reconnect checkboxes.

### `739847ef5a` Add focus rectangle support to custom buttons

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/739847ef5a331f124a67e09d20f59c478f54adf0) by Jules Bobb
- **size:** 4 files (+70/-5)
- **score 7** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 2 | risk 2 | applies conflict | REIMPLEMENT
- **why:** Our buttons already highlight focus, but themed CommandButton lacks cues. Adapt missing keyboard feedback while preserving our rewritten dialog layout and default-focus behavior.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - MrngButton already highlights focus/default state, and frmTaskDialog already focuses AcceptButton. CommandButton could benefit separately, but this commit overlaps existing behavior and needs adaptation.
  - dissent - gemini: REJECT - Our fork already implemented focus and default button borders cleanly using theme palette colors; upstream's ControlPaint.DrawFocusRectangle and redundant state tracking are unnecessary and clash with theming.

### `918f324072` fix: throw InvalidOperationException instead of bare Exception

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/918f3240721c03e0a8a7dc1506ae01410b53e51c) by Eran Markus
- **size:** 1 files (+1/-1)
- **score 7** - port the idea, the patch will not apply
- **triage:** bugfix | value 2 | effort 1 | risk 1 | applies conflict | REIMPLEMENT
- **our issue:** #165
- **why:** SHA is not an ancestor. Fork initializes missing metadata through HandleFirstRun, whose reread remains unchecked; add InvalidOperationException after initialization without disrupting first-run behavior.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Current Load() calls GetDecryptionKey directly after metadata retrieval; there is no bare exception to replace. Adding the missing guard requires a separately reviewed change.
  - dissent - gemini: REJECT - Our fork already replaced the null check and exception throw with HandleFirstRun, making this upstream change obsolete and incompatible.

### `a645175d54` Revise installation parameters in README

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a645175d54ef8363298fbc8e14cdb7064a9b830e) by Dimitrij
- **size:** 1 files (+3/-3)
- **score 7** - port the idea, the patch will not apply
- **triage:** docs | value 2 | effort 1 | risk 1 | applies rewrite | REIMPLEMENT
- **why:** Our rewritten installer is explicitly per-machine. Preserve the documentation-accuracy direction, but describe our supported INSTALLFOLDER property and elevation requirement instead of upstream dual-scope options.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Package.wxs specifies perMachine and requires Privileged unconditionally. The current README lacks these rows; importing this upstream clarification would introduce misleading installation guidance.
  - dissent - gemini: REJECT - The target README section does not exist in our fork, and our modern WiX installer explicitly targets per-machine installation scope.

### `b4affea30a` Chore: Update CHANGELOG for MSI single package authoring

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b4affea30a10a6c904f4f3fbf4b9821d68959ad2) by Eduardo Mozart de Oliveira
- **size:** 1 files (+1/-0)
- **score 7** - worth doing, needs work
- **triage:** docs | value 2 | effort 1 | risk 1 | applies conflict | WATCH
- **why:** Changelog only; implements no installation behavior. Our MSI explicitly requires administrator privileges. Track SPA direction, then document it after implementing and verifying per-user installation.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This commit changes documentation only. Package.wxs uses Scope="perMachine" and a Privileged launch condition, so the advertised feature is absent.
  - dissent - gemini: REJECT - Pure changelog update for upstream PR #3449; our fork maintains an independent changelog and modern WiX 6 installer.

### `2a90030329` Improve focus rectangle rendering for CommandButton

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2a900303298b1f4583b2c74ad65373d7c4ab140d) by Jules Bobb
- **size:** 1 files (+2/-1)
- **score 6** - port the idea, the patch will not apply
- **triage:** bugfix | value 2 | effort 2 | risk 1 | applies rewrite | REIMPLEMENT
- **why:** Our themed CommandButton lacks explicit focus rendering. Restore an inset keyboard-focus indicator within our painting implementation; the upstream patch does not apply.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Focus visibility aligns with priorities, but this commit cannot apply independently; the fork needs its own focus-rendering fix before an inset adjustment is useful.
  - dissent - gemini: REJECT - Our fork never imported the prerequisite focus tracking implementation. This patch cannot apply or compile on its own and requires reimplementing the full feature.

### `4deffb8cf7` Languages updates

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4deffb8cf79b124e63f8f8325abdc4f28d952635) by Kvarkas
- **size:** 7 files (+314/-5)
- **score 6** - port the idea, the patch will not apply
- **triage:** bugfix | value 2 | effort 2 | risk 1 | applies conflict | REIMPLEMENT
- **why:** Resource patches conflict. Selectively port applicable Lithuanian translations; plugin settings strings describe upstream UI absent from our fork.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Most additions target a Plugins options page absent from this fork. Lithuanian general translations are useful separately, but the whole commit does not merit pre-approval.
  - dissent - gemini: REJECT - Most strings support upstream's unimported plugin UI. Our fork restricts plugins to the install directory and lacks this page, making the strings unused dead weight.

### `7f12c5dc64` Docs: Update README with silent install instructions for per-user mode

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7f12c5dc64326b7cf6d4c0d3532616dcfdd0fbd2) by Eduardo Mozart de Oliveira
- **size:** 1 files (+5/-1)
- **score 6** - port the idea, the patch will not apply
- **triage:** docs | value 2 | effort 2 | risk 1 | applies rewrite | REIMPLEMENT
- **why:** Document silent MSI installation using our INSTALLFOLDER property and administrator requirement. Our installer is per-machine only, so upstream's per-user instructions would mislead users.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - mRemoteNGInstaller/Package.wxs specifies Scope="perMachine", a Privileged launch condition, and HKLM writes. The dual-mode documentation contradicts this fork’s installer.
  - dissent - gemini: REJECT - Our README was redesigned without this legacy section, and our WiX 6 MSI does not support dual-mode or per-user silent installation.

### `e24d23efe9` Fix dark title bar requiring two restarts to apply

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e24d23efe9f31bd2b4eff8037a61a7e99d647c22) by Piero Biagini
- **size:** 2 files (+18/-9)
- **score 6** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 3 | risk 2 | applies rewrite | REIMPLEMENT
- **why:** Our themes apply live and lack upstream's native dark-mode infrastructure. Adapt startup synchronization with that infrastructure; importing restart-based persistence conflicts with our lifecycle.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork already switches themes live. The proposed restart-based fix targets a different implementation and cannot be imported independently without additional changes.
  - dissent - gemini: REJECT - Our codebase triaged title bar theming as wontfix (#2642) and already applies themes live; importing this breaks compilation against non-existent properties.

### `af33a3f833` chore: harden ThemeSerializer.SaveToXmlFile path handling

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/af33a3f833d0e0f129b6518e7f7bbb0db0b22f65) by Eran Markus
- **size:** 1 files (+4/-4)
- **score 5** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 2 | risk 3 | applies conflict | REIMPLEMENT
- **why:** Empty URI rejection exists, but directory fallback and concatenation remain. Adapt directory validation and Path.Combine; reject rooted theme names that could escape the destination.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Empty-URI validation already exists here. The remaining change weakens path containment, and its null-only directory check still accepts directoryless paths.
  - dissent - gemini: REJECT - The null check and directory handling are already implemented in our fork via fe14cb411 and c65fc03db with full analyzer adherence.

### `b50926bfdd` fix: stop dropping SQL Server connections on every load

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b50926bfdd321b4c8f9c69f99ea766f18b3f7056) by Eran Markus
- **size:** 3 files (+24/-1)
- **score 5** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 2 | risk 3 | applies conflict | REIMPLEMENT
- **our issue:** #165
- **why:** Catalog matching already exists via c4da62c39; saver handles missing metadata transactionally. Reimplement the remaining loader null guard while preserving our database safety checks and transactions.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork already implements catalog-aware table detection with ODBC support and handles absent metadata transactionally. Import duplicates existing fixes and weakens save atomicity.
  - dissent - gemini: REJECT - Already implemented in c4da62c39 with proper ODBC positional markers, and #1883 already handles null metadata safely within save transactions.

### `1329450e78` Extend dark theme coverage to floating windows, dialogs and native controls

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/1329450e782556cc4fdc33bd916aedabe9434064) by Piero Biagini
- **size:** 5 files (+39/-6)
- **score 4** - port the idea, the patch will not apply
- **triage:** feature | value 3 | effort 3 | risk 3 | applies conflict | REIMPLEMENT
- **why:** Dark title bars and native-control coverage are missing. Adapt to our startup and theme lifecycle, preserving high-contrast behavior and reviewing global color-mode interactions.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Dark-theme coverage is useful, but this import lacks prerequisite implementation and its startup-only native color mode conflicts with this fork’s live theme switching.
  - dissent - gemini: REJECT - Depends on unmerged upstream title bar commits, will not compile, and eagerly initializes ThemeManager before core startup sequence completes.

### `46548c8c3e` Refactor focus handling in custom button controls

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/46548c8c3e65ee665830fc98e70363240d3c03b1) by Jules Bobb
- **size:** 2 files (+2/-41)
- **score 4** - port the idea, the patch will not apply
- **triage:** refactor | value 2 | effort 2 | risk 2 | applies conflict | REIMPLEMENT
- **why:** Our controls have different focus rendering; CommandButton lacks the upstream indicator. Adapt Focused-based painting and verify repainting during keyboard navigation.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Both controls already lack _hasFocus and focus-event overrides. MrngButton already uses Focused for enhanced borders; the proposed refactor provides no applicable benefit.
  - dissent - gemini: REJECT - Our fork already uses Control.Focused alongside AcceptButton handling and theme-aware borders; the _hasFocus field and ControlPaint logic do not exist in our tree.

### `4953fb981e` Improve default focus handling in TaskDialog form

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4953fb981e8576fe1b59fb2c9ae9881622a1785d) by Jules Bobb
- **size:** 1 files (+13/-13)
- **score 4** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 3 | risk 3 | applies conflict | REIMPLEMENT
- **why:** Our rebuilt dialog still retains _focusControl and focuses it unchecked. Adapt reset and validity guards while preserving command-button selection and safer Cancel defaults.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork already focuses AcceptButton in OnShown. The patch duplicates that behavior, overrides command-button defaults, and assumes rebuild cleanup absent from our implementation.
  - dissent - gemini: REJECT - Our fork already solved default button focus via AcceptButton in OnShown (#55), and task dialog instances are never reused across show calls.

### `553b50c018` AI code review issues: SQL deserialization issues; and frame border paint issues

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/553b50c018e48a16909bf2106c4c95ab21630dc3) by Joe Cefoli
- **size:** 3 files (+27/-9)
- **score 4** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 3 | risk 3 | applies conflict | REIMPLEMENT
- **our issue:** #177
- **why:** Rework border-aware sizing within our RDP layout; investigate alongside #177 without claiming redraw resolution. SQL already tolerates invalid enums and retains the tested legacy preset.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - SQL parsing already tolerates unknown resolutions; RDP sizing uses a different implementation. This import adds a contradictory test and requires adaptation rather than straightforward verification.
  - dissent - gemini: REJECT - DataTableDeserializer already uses Enum.TryParse in our fork, while the RdpProtocol changes revert our modernized dynamic sizing and Dock.Fill frame border handling.

### `7011b30b42` DPI fix for menu

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7011b30b421c755001f67133fc012120c9a31d95) by Kvarkas
- **size:** 2 files (+61/-24)
- **score 4** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 3 | risk 3 | applies conflict | REIMPLEMENT
- **our issue:** #198
- **why:** Conflicts with our content-based DPI layout. Adapt remaining scaling and idempotent control/subscription cleanup; preserve our checkbox layout and avoid rebuilding controls on every DPI change.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork already handles DPI changes through LayoutToContent and already uses unscaled EmulatedFormWidth. Importing this alternative adds state-loss risk and conflicts with existing layout fixes.
  - dissent - gemini: REJECT - Already implemented and superseded in our fork with dynamic layout, DPI-change handling, and comprehensive unit and lab UI tests.

### `8f6df579b1` The designer crash "Object reference not set to an instance of an object" was caused by:

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8f6df579b1cfbf2dd6739593c43ee63f77dd2b5c) by Kvarkas
- **size:** 37 files (+632/-570)
- **score 4** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 3 | risk 3 | applies conflict | REIMPLEMENT
- **why:** Resource anomalies remain. Reimplement normalization and targeted designer initialization fixes against our themes/options rework; full patch conflicts across resources and UI classes.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Designer maintenance fits this fork, but the resource conversion introduces a concrete credential-configuration regression. Remove untranslated entries to preserve fallback; review designer fixes separately.
  - dissent - gemini: REJECT - Direct import clobbers fork-specific UI code and mixes 21-file resx churn with runtime designer heuristics; resource key fixes should be reimplemented surgically instead.

### `903e58ef27` Route splash close through ProgramRoot.CloseSplash() and harden it

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/903e58ef27cbcf2827fe2101b8c1e8107d8d028d) by Piero Biagini
- **size:** 2 files (+30/-13)
- **score 4** - port the idea, the patch will not apply
- **triage:** bugfix | value 2 | effort 2 | risk 2 | applies rewrite | REIMPLEMENT
- **why:** Fork already centralizes WinForms splash closure. Adapt early reference clearing and failure logging; upstream WPF dispatcher shutdown and thread joining do not apply.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - frmMain already calls ProgramRoot.CloseSplash(), which catches cleanup failures and clears state. WPF was removed; this import duplicates existing behavior and targets obsolete infrastructure.
  - dissent - gemini: REJECT - Our fork already routes splash close through ProgramRoot.CloseSplash() and converted FrmSplashScreenNew to WinForms, making WPF Dispatcher shutdown and thread-joining obsolete.

### `b42e782584` adding readme about plug-ins

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b42e782584dfdf47de3fe7f46abe5ddc4d5698a0) by Kvarkas
- **size:** 1 files (+43/-0)
- **score 4** - port the idea, the patch will not apply
- **triage:** docs | value 2 | effort 2 | risk 2 | applies rewrite | WATCH
- **why:** Documents upstream's separate plugin solution and contracts, which our fork lacks. Our charter defers contract adoption until stable; rewrite instructions when adopting that architecture.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork references mRemoteNG.Plugins directly within mRemoteNG.sln and loads from a fixed Plugins directory. Importing upstream instructions would mislead maintainers and provide unusable commands.
  - dissent - gemini: REJECT - Our fork maintains a single solution without separate plugin solutions or assemblies; this documentation is inaccurate and misleading for this repo.

### `cf1709b6d2` Completely refactored Resizing logic and fixed broken functionality; default to SmartSize

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/cf1709b6d2fca51367fde330c5ea2b8c41732979) by Joe Cefoli
- **size:** 18 files (+97/-94)
- **score 4** - port the idea, the patch will not apply
- **triage:** bugfix | value 5 | effort 5 | risk 5 | applies rewrite | REIMPLEMENT
- **our issue:** #177
- **why:** Investigate anchored ActiveX resizing for splitter redraw. Our sizing modes and lifecycle diverge substantially; preserve fixed/custom resolutions, enum compatibility, and existing automatic-resize behavior.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork already implements separate sizing modes, aspect preservation, custom resolutions, and dynamic resizing. The proposed refactor discards supported functionality and conflicts with existing serializers.
  - dissent - gemini: REJECT - Conflicts with our extensive RDP sizing architecture (RDPSizingMode, SmartSizeAspect, dynamic resize) and destroys existing user connection configurations using fixed resolutions.

### `ef770e8edf` feat: localize theme palette element names

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ef770e8edfa6c0211c8bdd5dcffb2ddeba3d57b7) by Taro Kawakami
- **size:** 3 files (+137/-3)
- **score 4** - port the idea, the patch will not apply
- **triage:** feature | value 2 | effort 2 | risk 2 | applies conflict | REIMPLEMENT
- **why:** Separating stable palette keys from display labels is useful. Reimplement through language resources; upstream unconditionally displays Japanese labels and conflicts with our theme-page changes.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Separating display labels from stable keys is sound, but this implementation bypasses the fork's culture-aware resources and makes theme editing harder for non-Japanese users.
  - dissent - gemini: REJECT - Bypasses standard resource-based localization by mapping internal keys directly to Japanese text without culture checks.

### `fb1e3ba7c4` Update ToArgumentList to prefix file names

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/fb1e3ba7c442477c8ef9ea08d180851e018a2e2e) by Dimitrij
- **size:** 1 files (+2/-1)
- **score 4** - port the idea, the patch will not apply
- **triage:** bugfix | value 2 | effort 2 | risk 2 | applies rewrite | WATCH
- **why:** Fork lacks ToArgumentList and launches through ToString, which prefixes filenames. Preserve this protection when adopting upstream’s ArgumentList direction; the isolated patch has no current benefit.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Current process launching uses ToString(), which already prefixes filenames through ProcessArgument. Importing this change provides no demonstrated benefit and requires introducing an unused API.
  - dissent - gemini: REJECT - Upstream bot-fix depends on unimported ArgumentList changes in ProcessController; our codebase relies on ToString and PathValidator instead.

### `4a2c11ab9e` Add validation for ordered roots in ConnectionTreeModel

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4a2c11ab9e5d19a4506df96cf722ed9bd4c08e99) by Dimitrij
- **size:** 1 files (+4/-0)
- **score 3** - port the idea, the patch will not apply
- **triage:** bugfix | value 2 | effort 3 | risk 2 | applies rewrite | WATCH
- **why:** SetRootNodeOrder is absent from our model. Preserve this validation requirement when adopting root reordering; importing the guard alone provides no current benefit.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Repository source and tests contain neither SetRootNodeOrder nor orderedRoots. This validates an upstream ordering API absent here, providing no demonstrated benefit to this fork.
  - dissent - gemini: REJECT - Our fork lacks SetRootNodeOrder entirely. Validating ordered roots applies to an upstream-only feature we never imported or needed.

### `9af77fe7a5` Refactor RDP password assignment condition

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9af77fe7a59f2a02e3d7ffd3120c42de4cbc09bc) by Dimitrij
- **size:** 1 files (+2/-1)
- **score 2** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 3 | risk 4 | applies rewrite | REIMPLEMENT
- **why:** Current credential guard lacks this legacy-version exception. Address unsupported authentication modes explicitly; blindly importing would assign passwords despite Restricted Admin/RCG being requested.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The current fork suppresses password assignment for both modes. This patch silently bypasses that safeguard on legacy paths instead of rejecting unsupported security configurations.
  - dissent - gemini: REJECT - Intermediate, unformatted Copilot commit superseded upstream by 491ba503a. Compromises credential security on legacy RDP by forwarding passwords despite explicit Kerberos-only settings.

### `508e2b32f8` Add support for root group containers in tree UI

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/508e2b32f85998361a2f785452db4476524a46a5) by Julian Bobbett (DHCW - Software Development)
- **size:** 13 files (+245/-8)
- **score 1** - port the idea, the patch will not apply
- **triage:** feature | value 3 | effort 4 | risk 4 | applies rewrite | REIMPLEMENT
- **why:** Our true root folders differ from upstream's parent-preserving root groups. Adapt XML compatibility and grouping semantics to our model; review filtering, drag/drop, inheritance, and SQL persistence.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork already implements New Root Folder through AddRootFolder and IsRoot. The import duplicates existing functionality and adds tree projection complexity without database round-trip coverage.
  - dissent - gemini: REJECT - A fragile UI-only pseudo-root hack that keeps containers nested in data while displaying as peers, complicating model logic without SQL or XSD support.

### `6acf79a056` chore: resolve nullable warnings in Themes and App

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6acf79a0563d790c0268a8b770b394cedf133ea4) by Eran Markus
- **size:** 21 files (+145/-110)
- **score 1** - port the idea, the patch will not apply
- **triage:** chore | value 2 | effort 3 | risk 3 | applies rewrite | REIMPLEMENT
- **why:** Conflicts with our GitHub updater and nullable/theme rework. Selectively adopt remaining guards, especially aborting exports with missing selected targets instead of falling back to root.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Current source already implements nullable annotations and theme guards; UpdateFile is removed and updates use GitHub JSON. The commit offers little applicable benefit.
  - dissent - gemini: REJECT - Our fork already eliminated all nullable warnings in App and Themes and refactored updater classes away from upstream's legacy format.

### `9533f242aa` Avoid full theme load before splash by persisting active-theme dark flag

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9533f242aacc9548b8f646536ebd9a71c111c16a) by Piero Biagini
- **size:** 4 files (+29/-1)
- **score 1** - port the idea, the patch will not apply
- **triage:** perf | value 2 | effort 3 | risk 3 | applies rewrite | WATCH
- **why:** Fork already shows splash before theme loading and lacks SetColorMode. Reconsider caching alongside native dark-control support, preserving live theme switching and high-contrast behavior.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork has no pre-splash SetColorMode call to optimize; FrmMain initializes ThemeManager after splash. The patch depends on absent upstream functionality.
  - dissent - gemini: REJECT - Our fork does not call ThemeManager before splash; this patches upstream-specific Application.SetColorMode integration and depends on missing properties.

### `810b5db5c4` Refactor limit calculation for reveal limits

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/810b5db5c43bb8089f4fb67e02d796f79f27b364) by Dimitrij
- **size:** 1 files (+3/-1)
- **score 0** - port the idea, the patch will not apply
- **triage:** bugfix | value 1 | effort 3 | risk 2 | applies rewrite | WATCH
- **why:** Our tree has no animated reveal limits. Preserve this interrupted-animation safeguard for future animation adoption; the literal patch cannot apply.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Current source exposes children directly and has no reveal-limit dictionary, animation timer, or collapsing handler. This isolated patch cannot apply or improve existing behavior.
  - dissent - gemini: REJECT - The commit refactors animation reveal limits, an experimental upstream UI feature absent from our fork, conflicting with our performance and stability priorities.

### `964d7c2997` fix for Options settings handling

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/964d7c299702ff5adeca3c87f37efeabdea4d284) by Kvarkas
- **size:** 4 files (+547/-5)
- **score 0** - port the idea, the patch will not apply
- **triage:** bugfix | value 3 | effort 5 | risk 4 | applies rewrite | REIMPLEMENT
- **why:** Unified options persistence is useful, but our portable/registry providers lack this SQLite subsystem. Adapt the contract; review shared-connection concurrency and arbitrary SQL import.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork uses ApplicationSettingsBase with portable/local file providers. None of the four target files exists; importing this cannot be a small, independently verifiable fix.
  - dissent - gemini: REJECT - The patch contains invalid SQL parameters, uses Task.Run for synchronous data access, and introduces unwanted speculative 'Development Features' against our fork's priorities.

### `43ccda0675` some fixes

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/43ccda0675e2509b9bd04f410a0d95ad96064c06) by Kvarkas
- **size:** 8 files (+1448/-1582)
- **score -1** - port the idea, the patch will not apply
- **triage:** bugfix | value 2 | effort 3 | risk 4 | applies rewrite | REIMPLEMENT
- **why:** Gateway access-token default initialization is missing. Extract that fix; runtime-check removal is covered, while boolean drive defaults and generated-settings replacement conflict with our implementation.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The startup runtime-check removal is already implemented. Broad settings regeneration adds integration risk without a clear, distinct benefit.
  - dissent - gemini: REJECT - Unfocused grab-bag commit that dumps redundant settings XML into App.config, reverts drive redirection handling, deletes runtime checks, and conflicts with our .NET 10 architecture.

### `50800b8f00` adding import/export options for schema of settings db

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/50800b8f005eb7361c7f21b6b8eeaf55d82a0c81) by Kvarkas
- **size:** 5 files (+232/-5)
- **score -2** - port the idea, the patch will not apply
- **triage:** feature | value 2 | effort 4 | risk 4 | applies rewrite | WATCH
- **why:** Our settings architecture lacks these SQLite repository classes. Schema replacement executes supplied SQL and drops options; revisit settings transfer separately from connection-database issue #165.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork lacks the SQLite OptionsStore, repository, and management page; existing settings providers and LiteDB JSON import/export differ. Adoption requires architectural changes and adds no tests.
  - dissent - gemini: REJECT - Our fork uses XML settings rather than SQLite; the target files and settings database architecture do not exist in this codebase.

## Quarantine - security review required before anything else

### `a8e7788235` Fix XML declaration check to support single and double quotes

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a8e7788235087a4e148e7604509486bfae1d81b7) by Eduardo Mozart de Oliveira
- **size:** 1 files (+1/-1)
- **score 8** - security review required (high)
- **triage:** bugfix | value 3 | effort 1 | risk 2 | applies likely | IMPORT
- **why:** Our legacy decryptor still recognizes only double-quoted declarations. Patch passes apply-check and prevents valid single-quoted XML entering decryption; verify encrypted-file handling and retain security review.
- **pre-approval:** **MANUAL-REVIEW** (codex:APPROVE / gemini:APPROVE)
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Config/Serializers/XmlConnectionsDecryptor.cs` - credential and crypto paths need human review regardless of intent

### `5dc5f58dd4` feat: localize Options pages with hardcoded English labels

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/5dc5f58dd4b40af6284f5d91521c5973a6fffd8f) by Taro Kawakami
- **size:** 9 files (+1861/-255)
- **score 6** - security review required (high)
- **triage:** feature | value 3 | effort 3 | risk 2 | applies conflict | REIMPLEMENT
- **why:** Missing Options translations remain useful. Patch conflicts with our resources, SQL page, startup controls, and live themes; adapt labels and verify translated layouts.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Japanese users’ first click hides advanced settings and restores English instead of expanding. Useful localization, but this concrete regression prevents pre-approval.
  - dissent - gemini: REJECT - Do not import upstream diff directly. Our fork already heavily modified these pages and fixed upstream typos; reimplement local string extraction incrementally if options page localization is desired.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/UI/Forms/OptionsPages/CredentialsPage.cs` - credential and crypto paths need human review regardless of intent

### `d7a5982b2b` Update Process.Start to use ProcessStartInfo

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d7a5982b2b8f7959fc16e87ff1a4647875231a9c) by Dimitrij
- **size:** 1 files (+4/-1)
- **score 5** - security review required (critical)
- **triage:** bugfix | value 3 | effort 2 | risk 3 | applies conflict | REIMPLEMENT
- **why:** Our handler still uses Process.Start(args.Url) without upstream URI validation. Reimplement shell launching with absolute HTTP/HTTPS validation; the literal patch targets different code.
- **pre-approval:** **MANUAL-REVIEW** (codex:NEEDS_HUMAN / gemini:REJECT)
  - dissent - codex: NEEDS_HUMAN - Explicit shell execution enables browser launching, but this commit depends on URI validation absent from our handler. A maintainer must review the prerequisite before importing.
  - dissent - gemini: REJECT - Diff conflicts with our codebase, and ObjectListView hyperlinks are completely unused in mRemoteNG. The change provides no value and cannot be imported cleanly.
- **security flags:**
  - `process-exec` (critical) in `ObjectListView/ObjectListView.cs` - added code spawns a process or evaluates a string as code

### `3c03a6d9f1` fix: dispose RSA provider when rejecting a weak Passwordstate key

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/3c03a6d9f1d7d097d2761c21a50ff41f3c1fa368) by Eran Markus
- **size:** 1 files (+1/-0)
- **score 4** - security review required (high)
- **triage:** bugfix | value 2 | effort 2 | risk 2 | applies rewrite | REIMPLEMENT
- **why:** Our rewritten importer lacks upstream's weak-key rejection branch. Apply the resource-cleanup principle to import failures while preserving accepted-key policy; unrelated to RDP retention.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork derives provider size from the imported modulus and has no 2048-bit rejection path. The disposal-only patch therefore has no applicable cleanup path.
  - dissent - gemini: REJECT - Our fork derives key size from modulus directly (commit 75ecd955); the targeted key rejection check does not exist here and conflicts with our design.
- **security flags:**
  - `security-code` (high) in `ExternalConnectors/CPS/PasswordstateInterface.cs` - credential and crypto paths need human review regardless of intent

### `bd01dfbdb5` Feat: Add option to install without admin privileges

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bd01dfbdb5bedbdfa0584c586b97e4b3cabba428) by Eduardo Mozart de Oliveira
- **size:** 3 files (+23/-8)
- **score 4** - security review required (high)
- **triage:** feature | value 4 | effort 4 | risk 4 | applies rewrite | REIMPLEMENT
- **why:** Our WiX 6 installer still requires administrator privileges; legacy patch paths were removed. Reimplement scope selection, scope-aware registry handling, and installation directories; verify upgrades and uninstall.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Non-admin installation is useful, but this fork compiles Package.wxs and _GeneratedFiles.wxs only. The proposal requires a separate port and installer lifecycle validation.
  - dissent - gemini: REJECT - Files do not exist in our WiX SDK installer, and non-admin users are already supported via portable releases without complex dual-scope MSI logic.
- **security flags:**
  - `installer` (high) in `mRemoteNGInstaller/Installer/CustomDialogs/My_WixUI_FeatureTree.wxs` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/Installer/Fragments/RegistryEntriesFragment.wxs` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/Installer/mRemoteNG.wxs` - installer content ships signed to end users

### `355d88f5e6` Add dark native title bar based on active theme

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/355d88f5e6030cdecbf9f5e18f673994c542f32b) by Piero Biagini
- **size:** 9 files (+90/-0)
- **score 3** - security review required (high)
- **triage:** feature | value 2 | effort 3 | risk 2 | applies conflict | REIMPLEMENT
- **why:** Native dark title bars are absent. Adapt DWM integration to our renamed forms and theme lifecycle; review handle creation and disabled-theme behavior.
- **pre-approval:** **MANUAL-REVIEW** (codex:NEEDS_HUMAN / gemini:REJECT)
  - dissent - codex: NEEDS_HUMAN - Useful functionality is absent here, but the fork's docking architecture requires adaptation. The patch also duplicates an existing System.Windows.Forms using directive.
  - dissent - gemini: REJECT - Triaged as wontfix in #2642; cosmetic change with hard merge conflicts in frmMain and fragile per-form boilerplate rather than a clean centralized architecture.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/UI/Forms/FrmPassword.cs` - credential and crypto paths need human review regardless of intent

### `6520a9ffbf` chore: address review feedback on Security nullable cleanup

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6520a9ffbf5803512b0ce133c5e591c3dff2a7fc) by Eran Markus
- **size:** 3 files (+5/-4)
- **score 2** - security review required (high)
- **triage:** chore | value 1 | effort 1 | risk 2 | applies conflict | REIMPLEMENT
- **why:** Apply Array.Empty and accurate non-negative wording selectively. Preserve our explicit authenticated-password null guard rather than replacing it with upstream's null-forgiving assertion.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Repository inspection confirms authentication is already handled safely. The remaining changes only clarify exception wording and replace equivalent empty arrays, offering insufficient value to justify import.
  - dissent - gemini: REJECT - We already safely solved the nullability issue in XmlConnectionsDecryptor; remaining cosmetic changes touch tripwire crypto paths and provide no tangible value.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Config/Serializers/XmlConnectionsDecryptor.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/PasswordCreation/PasswordIncludesSpecialCharactersConstraint.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/SymmetricEncryption/AeadCryptographyProvider.cs` - credential and crypto paths need human review regardless of intent

### `d7521dbc34` cleaning dead packages

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d7521dbc34c5d5d8b8db92dbfce488de454a7ce5) by Kvarkas
- **size:** 8 files (+55/-16)
- **score 2** - security review required (high)
- **triage:** chore | value 2 | effort 2 | risk 3 | applies conflict | REIMPLEMENT
- **why:** Unused references remain in our fork. Reimplement selective cleanup against current projects; omit upstream-only SQLite additions, master-key resources, and version changes.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Some dependency cleanup is useful, but several removals already exist here. SQLite and settings resources have no consumers; reconsider only a narrowly scoped cleanup.
  - dissent - gemini: REJECT - Mismatched upstream changes bundle new SQLite dependency, speculative settings store resources, and version bumps rather than clean package pruning.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNGTests/mRemoteNGTests.csproj` - a new or repointed package can pull arbitrary code at restore time

### `f801785c0a` removing not used

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f801785c0a74bf64887a35bfa3dc1457c7a0d602) by Kvarkas
- **size:** 4 files (+0/-423)
- **score 2** - security review required (high)
- **triage:** refactor | value 2 | effort 2 | risk 3 | applies conflict | REIMPLEMENT
- **why:** Remove the uncalled CheckLockalDB method and orphaned LocalDBManager after reference verification. Retain LiteDB: our separate repository implementation and tests still depend on it.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Dead-code cleanup fits our direction, but LiteDB remains required by LiteDbRepository.cs and LiteDbRepositoryTests.cs. Importing the commit unchanged would remove a necessary dependency.
  - dissent - gemini: REJECT - Our fork actively maintains LiteDbRepository (#2242). Directly importing this commit removes the required LiteDB package and breaks compilation and tests.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `2ceb92c85a` Fix of embedding puttyng and include signed version

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2ceb92c85ace4e6b21f9658d285645bbf7dc1b51) by Kvarkas
- **size:** 5 files (+279/-26)
- **score 1** - security review required (critical)
- **triage:** bugfix | value 3 | effort 4 | risk 4 | applies rewrite | REIMPLEMENT
- **our issue:** #198
- **why:** Embedding geometry may help PuTTY clipping under #198. Rework our fixed chrome offsets; upstream introduces blocking sleeps, flawed border calculations, and replacement of our newer binary.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Embedding stability is relevant, but sizing is flawed, regression tests are absent, and the patch bundles an unrelated SQLite package addition and executable replacement.
  - dissent - gemini: REJECT - Bundles an untrusted binary and uses brittle cross-process DWM and style manipulations that strip scrollbars and trigger on every resize.
- **security flags:**
  - `binary-artifact` (critical) in `mRemoteNG/PuTTYNG.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `dependency-manifest` (high) in `mRemoteNGTests/mRemoteNGTests.csproj` - a new or repointed package can pull arbitrary code at restore time

### `7bb7ccadb1` updates and small fixes for UI in options page

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7bb7ccadb196a730373d544121c6238f6064c9f9) by Kvarkas
- **size:** 9 files (+812/-52)
- **score 1** - security review required (high)
- **triage:** feature | value 3 | effort 4 | risk 4 | applies rewrite | REIMPLEMENT
- **why:** Plugin enablement and visibility are useful directions. Our PluginManager contract and installer differ; adapt controls while preserving the admin-controlled plugin directory and reviewing reload behavior.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork uses a different PluginManager architecture. Import requires substantial adaptation, while unrelated SQL dependency and installer changes expand verification beyond a small UI fix.
  - dissent - gemini: REJECT - Tangled commit mixing upstream plugin host UI, options debug pages, and SqlClient package changes incompatible with our fork's design.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `installer` (high) in `mRemoteNGInstaller/Installer/Fragments/FilesFragment.wxs` - installer content ships signed to end users

### `ce94b0a5b0` prefix warnings and small fixes

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ce94b0a5b040991f8988f9ff287a6925b12a8dc6) by Kvarkas
- **size:** 24 files (+368/-127)
- **score 1** - security review required (critical)
- **triage:** chore | value 3 | effort 4 | risk 4 | applies rewrite | REIMPLEMENT
- **why:** Extract PuTTY exit cleanup and process-handler detachment into our asynchronous lifecycle. Review argument handling independently; avoid bundled dependency downgrades and weakened temporary-key handling.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The bundled SSH authentication regression blocks approval. Platform annotations add little value here, while the SNI pin and conditional T4 import already exist.
  - dissent - gemini: REJECT - Our projects already target net10.0-windows with zero analyzer warnings; plastering SupportedOSPlatform on ObjectListView classes and enums is unnecessary churn and degrades dependency management.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `process-exec` (critical) in `mRemoteNG/Connection/Protocol/PuttyBase.cs` - added code spawns a process or evaluates a string as code
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `f296afd293` fix for Enable animated expand/collapse for connection tree#3391

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f296afd2936aec3325aa6ce86c64897a7675039f) by Kvarkas
- **size:** 4 files (+226/-11)
- **score 1** - security review required (critical)
- **triage:** bugfix | value 3 | effort 4 | risk 4 | applies rewrite | REIMPLEMENT
- **why:** Our tree lacks upstream's animation machinery. Adapt bounded animation timing and accessibility controls to our tree; exclude the unrelated PuTTY binary and version changes.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Our tree lacks the animation subsystem this patch modifies. Import requires prerequisite architecture changes and includes an unrelated PuTTY executable without source review.
  - dissent - gemini: REJECT - Tree animations contradict our performance priorities and target non-existent infrastructure, while bundling an untrusted binary executable with no source justification.
- **security flags:**
  - `binary-artifact` (critical) in `PuTTYNG.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)

### `f9259873b4` fixing tests

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f9259873b4e8ac98f58c255319c82d26355fc17e) by Kvarkas
- **size:** 7 files (+27/-262)
- **score 1** - security review required (high)
- **triage:** chore | value 2 | effort 3 | risk 3 | applies rewrite | REIMPLEMENT
- **why:** Solution integration, .NET 10, and CategoryAttribute disambiguation exist. Adapt remaining obsolete-package cleanup to our projects; preserve WindowsUpdate input-validation tests instead of deleting them.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Test solution integration, .NET 10 targeting, and CategoryAttribute disambiguation already exist here; deleting maintained coverage and importing obsolete version changes adds no justified value.
  - dissent - gemini: REJECT - Already implemented better in our fork: CategoryAttribute is aliased, InstalledWindowsUpdateCheckerTests are fixed and passing, and sln/project changes are obsolete.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNGSpecs/mRemoteNGSpecs.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNGTests/mRemoteNGTests.csproj` - a new or repointed package can pull arbitrary code at restore time

### `64e060ae12` re-coding - part 2, splitting hardcoded tools into plug-ins service

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/64e060ae12a6cc143d0837ceea8a0f720253c113) by Kvarkas
- **size:** 27 files (+1467/-62)
- **score 0** - security review required (high)
- **triage:** refactor | value 3 | effort 5 | risk 4 | applies rewrite | REIMPLEMENT
- **why:** Tool modularization is useful direction. Reimplement around our existing plugin host; upstream contracts, assembly renames, credential exposure, and packaging require architectural adaptation.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This broad refactor duplicates existing plugin and transfer functionality with incompatible contracts; it also drops SFTP error completion handling. Too risky for quick pre-approval.
  - dissent - gemini: REJECT - Large architectural rewrite splitting core tools into separate plugin assemblies; contradicts fork priorities and duplicates existing stabilized SSH transfer and tooling.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG.PluginContracts/mRemoteNG.PluginContracts.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNG.Plugins.AWS/mRemoteNG.Plugins.AWS.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNG.Plugins.PortScan/mRemoteNG.Plugins.PortScan.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNG.Plugins.SshTransfer/mRemoteNG.Plugins.SshTransfer.csproj` - a new or repointed package can pull arbitrary code at restore time

### `2c956ca9c0` chore: resolve nullable warnings in Tree, Container and Credential

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2c956ca9c0fb85ad214c4dd1b9a9c5d409e20f9a) by Eran Markus
- **size:** 17 files (+77/-70)
- **score -2** - security review required (high)
- **triage:** chore | value 1 | effort 3 | risk 3 | applies conflict | REIMPLEMENT
- **why:** Nullable cleanup overlaps our rewritten tree and credential contracts. Adopt missing annotations selectively, preserving nullable credential fields and repository defaults rather than importing changed semantics.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - Most useful nullable fixes already exist here; Parent is already nullable, and recursive traversal is modernized. Remaining changes offer insufficient benefit to justify importing 17 files.
  - dissent - gemini: REJECT - Our fork already eliminated all analyzer warnings; these files are already annotated differently, and upstream's ContainerInfo changes conflict with our snapshotting fix.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Credential/CredentialDeletionMsgBoxConfirmer.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/CredentialInfo.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/CredentialRecordTypeConverter.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/CredentialServiceFacade.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/ICredentialRepository.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/ICredentialRepositoryList.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/CompositeRepositoryUnlocker.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/CredentialRepositoryList.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/XmlCredentialRepository.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Credential/Repositories/XmlCredentialRepositoryFactory.cs` - credential and crypto paths need human review regardless of intent

### `91ff428273` re-coding - part 1, splitting hardcoded tools into plug-ins services

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/91ff428273e7bc936870d4816a719ffba48cd551) by Kvarkas
- **size:** 27 files (+839/-1172)
- **score -2** - security review required (high)
- **triage:** refactor | value 3 | effort 5 | risk 5 | applies rewrite | WATCH
- **why:** Fork has a different plugin contract and reworked tools, SQL UI, and packaging. Track upstream service extraction; defer alignment until contracts stabilize, per charter.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The commit is internally incomplete, supplies no SSH transfer plugin, and mixes broad SQL, docking, localization, and packaging changes incompatible with quick, reliable import verification.
  - dissent - gemini: REJECT - The charter strictly forbids large speculative rewrites. Splitting tools into a separate plugin solution complicates our unified build process and does not align with our stability goals.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `a7d5503015` fix installer part with necessary package updates

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a7d5503015456c6a02b29879152d97346aafa02e) by Kvarkas
- **size:** 12 files (+81/-302)
- **score -5** - security review required (high)
- **triage:** bugfix | value 2 | effort 5 | risk 5 | applies rewrite | WATCH
- **our issue:** #192
- **why:** Conflicts with our WiX 6 harvesting and upgrade design; removes ExternalConnectors.dll manifest entries, raising #192 concerns. Watch modernization direction pending packaging, compatibility, and EULA review.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork already uses a separate WiX 6 installer with generated payload harvesting. The commit targets obsolete packaging and hardcodes upstream version 1.78.2.
  - dissent - gemini: REJECT - Our fork already replaced the legacy installer with a modern WiX 6 SDK setup in build-msi.ps1; upstream's WiX 7 migration conflicts with our architecture.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNGInstaller/CustomActions/CustomActions.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `installer` (high) in `mRemoteNGInstaller/CustomActions/CustomActions.csproj` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/Installer/CustomDialogs/My_CustomizeDlg.wxs` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/Installer/Fragments/DirectoriesFragment.wxs` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/Installer/Fragments/FilesFragment.wxs` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/Installer/Fragments/MainExeFragment.wxs` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/Installer/Includes/Config.wxi` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/Installer/Installer.wixproj` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/Installer/Localizations/en-US.wxl` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/Installer/mRemoteNG.wxs` - installer content ships signed to end users

### `daab9b0f74` fix settings db access from managing page

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/daab9b0f7402ecbec2525b2e9eefac2f38a19664) by Kvarkas
- **size:** 8 files (+201/-124)
- **score -5** - security review required (high)
- **triage:** bugfix | value 2 | effort 5 | risk 5 | applies rewrite | WATCH
- **why:** Targets upstream-only SQLite settings and keystore architecture; ours uses portable settings providers. Watch shared-store direction and key-provider handling; this does not fix SQL Server issue #165.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - This fork uses XML-backed settings; the repository manager, database stores, management page, and key-management classes targeted by this fix do not exist here.
  - dissent - gemini: REJECT - Our fork does not use SQLite for settings; this fixes an upstream subsystem we never imported and do not want.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNGTests/mRemoteNGTests.csproj` - a new or repointed package can pull arbitrary code at restore time

### `61af705a47` adding management tab in debug mode to maintain db entries for options

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/61af705a4774e17c9934f79a9c3176c2732c3306) by Kvarkas
- **size:** 32 files (+3806/-9)
- **score -8** - security review required (high)
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | WATCH
- **why:** Debug-only management offers little direct user benefit. Bundled SQLite persistence, legacy migration, and keystore architecture require a separate design decision and substantial security review.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The 3,806-line change adds a separate options database without improving existing settings behavior; its scope and dependency violate this fork’s maintenance priorities.
  - dissent - gemini: REJECT - We reject new external dependencies and large speculative features. A 3,800-line debug-only SQLite options tab provides negligible value to users and bloats maintenance.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `mRemoteNG/Security/KeyManagement/DpapiMasterKeyProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/KeyManagement/IMasterKeyProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/KeyManagement/KeystoreData.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/KeyManagement/KeystoreManager.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/KeyManagement/PasswordMasterKeyProvider.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `dependency-manifest` (high) in `mRemoteNGTests/mRemoteNGTests.csproj` - a new or repointed package can pull arbitrary code at restore time

## Tier C - watch list

### `ecc3eee525` Add changelog entry for dark native title bar (#2642)

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ecc3eee525d4b29b6302745e6787b2280bc343ed) by Piero Biagini
- **size:** 1 files (+1/-0)
- **score 4** - keep an eye on it
- **triage:** docs | value 1 | effort 1 | risk 1 | applies likely | WATCH
- **why:** Patch passes applicability checks, but advertises native dark-mode functionality absent from our source. Add an appropriately placed entry when that functionality lands.

## Tier D - rejected

### `c5ad2eb8a8` security: fix command injection in Terminal protocol (#3335)

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c5ad2eb8a83276febcd4100916a1c659c12ad012) by Jason Finch
- **size:** 2 files (+248/-32)
- **score 10** - already covered or rejected at triage
- **triage:** security | value 5 | effort 1 | risk 2 | applies rewrite | REJECT
- **why:** Verified port in 6f4e5dceb already launches ssh.exe directly, validates arguments, and adds stricter hostname checks. Importing again offers no security benefit.
- **security flags:**
  - `process-exec` (critical) in `mRemoteNGTests/Connection/Protocol/ProtocolTerminalTests.cs` - added code spawns a process or evaluates a string as code

### `2d10e7d687` Add LINQ using directive

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2d10e7d6872053db2ef8e303dbba634aaaeff934) by Dimitrij
- **size:** 1 files (+1/-0)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** The target test file does not exist in our fork. Adding its LINQ import provides no benefit without separately adopting the upstream tests.

### `2dad5c614e` chore: address review feedback on CredentialInfo

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2dad5c614e4a9ff7dbf8378b8924f76a7f413de7) by Eran Markus
- **size:** 1 files (+2/-1)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Formatting-only change conflicts with our nullable Password declaration. No runtime or credential-security benefit; normalize spacing during future edits.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Credential/CredentialInfo.cs` - credential and crypto paths need human review regardless of intent

### `81fa54f89c` Update mRemoteNGTests/UI/Controls/ConnectionTreeTests.cs

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/81fa54f89c68b5d252ace3f35e75fa712a95b636) by Dimitrij
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Whitespace-only adjustment targets setup code replaced by our message-pump test harness. Apply check fails; importing provides no behavioral benefit.

### `aafba35264` Add changelog entry for splash screen crash fix (#3367)

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/aafba35264c02b216e6a8598be0e4780c84abec7) by Piero Biagini
- **size:** 1 files (+1/-0)
- **score 4** - already covered or rejected at triage
- **triage:** docs | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Exact entry is absent, but our release notes already document splash lifecycle fixes. Importing an upstream release claim adds little value.

### `b381c0199e` Fix duplicate using directive for System namespace

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b381c0199e29ed3fb340eeceda46b056066c3f5b) by Dimitrij
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies likely | REJECT
- **why:** Actual patch only adds a UTF-8 BOM; it removes no duplicate directive. Our file already has one System import. No functional benefit.

### `b8c92e2ba9` Add Explorer-style slow click rename to connection tree

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b8c92e2ba983bfb0abd95ef61029d9eed7d4d993) by Julian Bobbett (DHCW - Software Development)
- **size:** 1 files (+2/-1)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Actual patch only ignores a personal FavoritesList; that rule is absent but offers no application benefit. Slow-click rename was separately ported in 83ecaf879.

### `77d4af5ea1` Update Swedish translation

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/77d4af5ea114c80a168a67aade43a3f9cb26b0ec) by Jörgen Hedkvist
- **size:** 1 files (+379/-26)
- **score 3** - already covered or rejected at triage
- **triage:** docs | value 2 | effort 1 | risk 1 | applies likely | REJECT
- **why:** Translation was imported in d15e17aab. Current resources match this update except deliberately removed obsolete release-channel strings; importing again would duplicate existing work.

### `cbd1a802cc` Clarify dark-theme heuristic: rename doc to brightness and extract threshold constant

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/cbd1a802cc13dba8879c4f5f0f371fed8b825099) by Piero Biagini
- **size:** 1 files (+8/-4)
- **score 3** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 2 | risk 1 | applies rewrite | REJECT
- **why:** Our ThemeManager lacks the targeted dark-theme heuristic and title-bar API. This behavior-preserving clarification has no applicable user-visible benefit.

### `11226d788a` fix for download url

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/11226d788a986662c87e9d57854f5411e40efe6b) by Kvarkas
- **size:** 1 files (+19/-7)
- **score 1** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 2 | risk 2 | applies rewrite | REJECT
- **why:** Our #130 rework removed this .NET runtime check; managed startup already proves runtime availability. Remaining VC++ download uses a fixed, HTTPS-validated URL.

### `9cde7503fa` Refactor else statement to simplify sound playback

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9cde7503fa9d5ddd99e60a52a217bc53687b1628) by Dimitrij
- **size:** 1 files (+5/-4)
- **score 1** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 2 | risk 2 | applies conflict | REJECT
- **why:** Our hyperlink handler lacks this branch. The added return changes visited-link bookkeeping; this minor refactor offers no demonstrated user benefit.

### `0755cb2b64` Fix XML formatting in Language.tr-TR.resx

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/0755cb2b6419ac500511f5d6debceec8177d641e) by Rıza Emet
- **size:** 1 files (+3/-3)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Current Turkish resource already contains all three formatting changes; verified from source, independently of the similar-history listing.

### `6bd445b628` Update mRemoteNG/UI/Window/ConnectionTreeWindow.cs

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6bd445b628c038f538f74fa660c7cfe3ec29e7d6) by Dimitrij
- **size:** 1 files (+1/-1)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Only removes a comment; our current ConnectionTreeWindow already contains the uncommented SetupSlowClickRename call. No behavioral change.

### `6d810fa121` Fix duplicate using directive for System.Collections.Generic

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6d810fa1214de360bad69ccec85a400f60b43e53) by Dimitrij
- **size:** 1 files (+1/-1)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Actual patch only adds a UTF-8 BOM, despite its subject. Our file already has a BOM and one System.Collections.Generic directive.

### `9634d9fe55` fix removing duplicates

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9634d9fe553cf26226911c3c0c29c0c741f66944) by Kvarkas
- **size:** 3 files (+0/-9)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Current German, French, and Russian resources each contain exactly one UseDefault entry. Desired uniqueness already holds; upstream deletion is unnecessary.

### `97465e65db` Update mRemoteNG/Properties/Settings.settings

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/97465e65db48bbfc02f6acf8914ffd7004fb3613) by Dimitrij
- **size:** 1 files (+2/-1)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Whitespace-only XML cleanup; current Settings.settings already separates the closing elements, with additional fork settings changing the patch context.

### `9ac1f3bd96` Fix license comment formatting in PortableSettingsProvider

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9ac1f3bd969255c33ede2d3ee81031cdbacc5ace) by Dimitrij
- **size:** 1 files (+1/-1)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies likely | REJECT
- **why:** Current PortableSettingsProvider.cs already begins with the corrected license comment. No remaining change or user-visible benefit.

### `a849f697c5` Refactor SlowClickRenameHandler to use interface

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a849f697c508355aa7f9cf05efc3721906136115) by Julian Bobbett (DHCW - Software Development)
- **size:** 4 files (+60/-34)
- **score 0** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Verified equivalent interface, Execute wiring, cancellation, and disposal already exist from our upstream feature port. The SHA is absent, but its implementation is present.

### `af04ff460c` resize fix for DPI

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/af04ff460cc9838331fc1c47e638f9e3ee90df50) by Kvarkas
- **size:** 1 files (+1/-5)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **our issue:** #198
- **why:** Current ConfigWindow already uses DockStyle.Fill and omits the fixed location and size. This exact layout change exists despite divergent surrounding code.

### `c295c52083` test: run the WinForms tests on a real message loop with a reliable timeout

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c295c52083efd67015f31ba85bc497df2b684b45) by Jason Finch
- **size:** 1 files (+57/-10)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Current DockPaneStripNGTests already contains the real STA message loop, readiness signal, bounded shutdown, and background-thread timeout safeguard through 8ec20405e. This changes no production behavior.

### `c73b053097` test: assert the reflection lookups and drop the stray nullable annotation

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c73b0530977fe428d8b96a8c566d667bb5035a51) by Jason Finch
- **size:** 1 files (+4/-1)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Our rewritten tab tests already omit the nullable annotation and assert both reflection lookups; they exercise MiddleClickCloseTab instead of upstream's CloseTab.

### `2325ac37d4` Removed userSetttings section from App.config

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2325ac37d4b97ecb9277d51f6f72058c9163a6c1) by Jules Bobb
- **size:** 2 files (+4/-602)
- **score -2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 2 | applies conflict | REJECT
- **why:** Current App.config already omits configSections and userSettings. This removes configuration added by the rename commit; importing it offers no additional benefit.

### `37f9436bca` fix for testing part

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/37f9436bca152fdf26ef366fd09a9ecac9cda6c6) by Kvarkas
- **size:** 4 files (+20/-21)
- **score -2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 3 | applies rewrite | REJECT
- **why:** Targets upstream-specific settings classes and absent test files. Disabling animations and asserting unchanged model children weakens expansion coverage; our tree tests use a message pump.

### `4a1b38e5dd` Fix cross-thread crash when closing the WPF splash screen

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4a1b38e5dd7b3900b1865c95c0c796de4f0c05ba) by Piero Biagini
- **size:** 3 files (+3/-3)
- **score -2** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 3 | risk 3 | applies rewrite | REJECT
- **why:** Fork uses a same-thread WinForms splash and already centralizes closure. Upstream's separate WPF dispatcher failure path does not exist here.

### `6614328bc1` Refactor setting node retrieval logic

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6614328bc10ab94f32f9469617fb3fb2b742d807) by Dimitrij
- **size:** 1 files (+2/-13)
- **score -2** - already covered or rejected at triage
- **triage:** refactor | value 1 | effort 1 | risk 2 | applies conflict | REJECT
- **why:** Equivalent behavior already exists: our provider selects attached XML nodes directly and preserves null-safe serialization. Upstream removes an XDocument detour absent from our implementation.

### `965cd97645` Add UseRedirectionServerName connection property

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/965cd976453f7e41308a800cd5895829699ff49b) by Taro Kawakami
- **size:** 14 files (+141/-5)
- **score -2** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 1 | risk 2 | applies conflict | REJECT
- **why:** Verified in current source: 83ecaf879 ported the property, inheritance, defaults, serializers, and RDP integration. Reimporting would duplicate functionality across diverged files.

### `b13e8783e4` fix for correct structure

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b13e8783e4f7fa8711f4973362fe6c411374eb8d) by Kvarkas
- **size:** 4 files (+7/-7)
- **score -2** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 3 | risk 3 | applies rewrite | REJECT
- **why:** Targets deleted legacy installer fragments and AppVeyor signing. Our WiX 6 installer uses different authoring without the conflicting WiX SolutionDir constant.
- **security flags:**
  - `installer` (high) in `mRemoteNGInstaller/Installer/Fragments/MainExeFragment.wxs` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/Installer/Fragments/MiscTextFilesFragment.wxs` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/Installer/Installer.wixproj` - installer content ships signed to end users
  - `installer` (high) in `mRemoteNGInstaller/Installer/mRemoteNG.wxs` - installer content ships signed to end users

### `e279b752e1` chore: resolve nullable warnings in the Security namespace

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e279b752e1bf8569180266d8d2aa75f31ce8d51d) by Eran Markus
- **size:** 5 files (+10/-9)
- **score -2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 2 | applies conflict | REJECT
- **why:** Source confirms every substantive change already exists, including constructor hints and nullable annotations. Our decryption additionally clears plaintext buffers; importing adds no benefit.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/Authentication/PasswordAuthenticator.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/EncryptedSecureString.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/PasswordCreation/PasswordIncludesSpecialCharactersConstraint.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/SecureStringExtensions.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/SymmetricEncryption/AeadCryptographyProvider.cs` - credential and crypto paths need human review regardless of intent

### `8f7f87c89b` fix: keep an unparseable colour value when the picker is dismissed

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8f7f87c89bbc85316c8dc7145ffb508be3f2b7db) by Jason Finch
- **size:** 2 files (+48/-8)
- **score -3** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 2 | risk 2 | applies rewrite | REJECT
- **our issue:** #176
- **why:** Our TabColorEditor preserves original text whenever the picked colour is unchanged, including invalid values; cancellation tests exist. Both upstream target files are absent.

### `b731546b0c` security: harden Terminal SSH argument/exe handling (review follow-ups #3335)

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b731546b0c82e7a99d6ff436ae39ba23dd6b8ba0) by Jason Finch
- **size:** 2 files (+59/-8)
- **score -3** - already covered or rejected at triage
- **triage:** security | value 1 | effort 2 | risk 2 | applies conflict | REJECT
- **why:** Verified in current source: quote rejection, sanitized errors, PATH normalization, ordinal localhost matching, and regression tests already exist through 6f4e5dceb and shared ConsoleArgument validation.

### `e4871d4996` lang fix

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e4871d49968afdebc1984d287e80e590c315b919) by Kvarkas
- **size:** 3 files (+530/-518)
- **score -3** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 2 | risk 2 | applies conflict | REJECT
- **why:** Actual diff mainly reorders generated accessors. Our XML is valid and redirection resources exist with tailored wording; upstream version changes and removed diagnostic newline are undesirable.

### `f79b114797` Add Explorer-style slow click rename to ConnectionTree

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f79b114797f959056fdd21e12bcc9e480b799a3d) by Julian Bobbett (DHCW - Software Development)
- **size:** 8 files (+430/-13)
- **score -3** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 2 | risk 2 | applies conflict | REJECT
- **why:** Source confirms slow-click rename already ported with configurable enablement, cancellation, disposal, and dedicated tests. Upstream's literal implementation conflicts with our handler interface and settings.

### `f8b0625d6c` Update regex to filter GitHub links more accurately

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f8b0625d6cc0a34d6c4240c1adbb1d45652e867d) by Dimitrij
- **size:** 1 files (+2/-2)
- **score -4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 3 | applies conflict | REJECT
- **why:** Our workflow already allows upstream, our fork, and all attachment paths. This upstream-specific regex would regress fork-link and diagnostic-file handling.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/filter-links.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `22f14aa1eb` Fix for vulnerability detected by Ravindu Lakmina

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/22f14aa1eb67902d299f90a63bd621e37c144cb8) by Kvarkas
- **size:** 2 files (+6/-7)
- **score -5** - already covered or rejected at triage
- **triage:** security | value 1 | effort 2 | risk 3 | applies conflict | REJECT
- **why:** Current CreatePipe already uses the protected ACL helper and disposes both streams, with timeout handling beyond upstream. Patch conflicts with our transport rework.

### `763fffff45` fix warnings

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/763fffff45d5d2da8aa15a9736bdb9f37236ee91) by Kvarkas
- **size:** 33 files (+196/-178)
- **score -5** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 4 | risk 4 | applies conflict | REJECT
- **our issue:** #200
- **why:** Disables nullable analysis and broadly suppresses warnings. Its handle-safe asynchronous close direction already exists in our rewritten lifecycle; upstream-only settings changes add little benefit.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/EncryptionException.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/UI/Forms/FrmPassword.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `mRemoteNGTests/Security/AeadCryptographyProviderTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Security/EncryptedSecureStringTests.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Security/LegacyRijndaelCryptographyProviderTests.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNGTests/mRemoteNGTests.csproj` - a new or repointed package can pull arbitrary code at restore time

### `ade1744668` Update InstallScopeDlg logic for Next button

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ade1744668a07a81084ac8e363dabf8e28bffe23) by Dimitrij
- **size:** 1 files (+1/-1)
- **score -5** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 4 | risk 4 | applies rewrite | REJECT
- **why:** Legacy dialog was deleted. Our WiX 6 installer explicitly requires privileged per-machine installation; this per-user selection fix has no applicable flow.
- **security flags:**
  - `installer` (high) in `mRemoteNGInstaller/Installer/CustomDialogs/My_WixUI_FeatureTree.wxs` - installer content ships signed to end users

### `bbde106b16` small fix for the designer error

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bbde106b165f4eba3b08fef8f6e2d7359959c633) by Kvarkas
- **size:** 1 files (+191/-103)
- **score -5** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 4 | risk 4 | applies rewrite | REJECT
- **why:** Targets an upstream-only developer options page absent from our fork. Designer rewrite drops docking and grid constraints; no demonstrated benefit to our existing options pages.

### `240ac778e1` fix: apply the colour picked in the property grid Color/TabColor fields

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/240ac778e1b5156975db52e99379847f00151e74) by Jason Finch
- **size:** 5 files (+131/-4)
- **score -6** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 3 | risk 3 | applies conflict | REJECT
- **our issue:** #176
- **why:** aa7c8bac4 and 8f12912c3 implement string-backed editing, empty-color conversion, and string dropdown values, additionally preserving cancellation. Upstream would duplicate our editor.

### `6c0a094112` Add "Clear Cached RDP Credentials" connection action

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6c0a094112e36bffbadf3ac38e42c30f63f58390) by Taro Kawakami
- **size:** 5 files (+252/-1)
- **score -6** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 1 | risk 4 | applies conflict | REJECT
- **why:** Explicitly ported in 83ecaf879: CredDelete helper, RDP-only context action, confirmation and result messages exist. Importing again duplicates credential-deletion functionality.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Connection/Protocol/RDP/RdpCredentialCacheCleaner.cs` - credential and crypto paths need human review regardless of intent

### `8a31c20b4a` Update contents permission in build workflow

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8a31c20b4a305fcd4f356d1fca131903ee714db0) by Dimitrij
- **size:** 1 files (+1/-1)
- **score -7** - already covered or rejected at triage
- **triage:** security | value 1 | effort 2 | risk 4 | applies rewrite | REJECT
- **why:** Least-privilege direction is implemented: build uses contents: read, separate publishing uses contents: write. Upstream's combined-job patch conflicts and could disable releases.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `df292fe86f` Updating Putty to v.84

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/df292fe86fbf78d476eea00cd8e21600c6c8f9ba) by Kvarkas
- **size:** 1 files (+0/-0)
- **score -7** - already covered or rejected at triage
- **triage:** security | value 1 | effort 2 | risk 4 | applies conflict | REJECT
- **why:** Superseded by our verified bundled PuTTYNG 0.85 build in aeea728c7. Importing upstream's 0.84 binary would downgrade the transport and replace our maintained binary.
- **security flags:**
  - `binary-artifact` (critical) in `mRemoteNG/PuTTYNG.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)

### `226ed1abe1` Add option for Explorer-style slow double-click rename

- **fork:** [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/226ed1abe16ce1f3383db4a79c8ebbf893c2f1d1) by Julian Bobbett (DHCW - Software Development)
- **size:** 11 files (+2101/-846)
- **score -8** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 3 | risk 4 | applies conflict | REJECT
- **why:** 83ecaf879 already ports opt-in slow-click rename, settings, registry policy, and live refresh. Literal import conflicts and introduces unnecessary generated configuration churn.

---

Generated by `.project-roadmap/fork-intel/fork_intel.py report`. Nothing here has been imported: every entry needs a human decision.

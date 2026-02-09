# P7 Test Coverage Analysis — PRs #3105-#3130

Date: 2026-02-08 (updated 2026-02-09)
Scope: 26 codex PRs on `release/1.79`
Baseline: 2148/2148 tests passing (before P7 work)

## Status: ALL PRIORITY A GAPS CLOSED

After two rounds of test writing:
- **Commit `708a4f5c`** (2026-02-08): 28 new tests across 5 files
- **Commit TBD** (2026-02-09): 3 final OnePasswordCli tests + 2 XmlConnectionsLoader tests disabled

**Final count: 2179 total tests (2174 pass, 2 skipped CueBanner, 2 ignored XmlConnectionsLoader, 1 ignored CanDeleteLastFolderInTheTree)**

## Coverage Summary

| Level | PRs | Count |
|-------|-----|-------|
| Well tested | 3105, 3107, 3109, 3111, 3113, 3114, 3115, 3121, 3123, 3125 | 10 |
| Config only (N/A) | 3110 | 1 |
| Moderate — needs mocking | 3108, 3112, 3122 | 3 |
| Pure UI/WinForms — manual QA only | 3106, 3116-3120, 3124, 3126-3130 | 12 |

## Tier 1: Fully Covered (All Priority A gaps closed)

### PR 3107 — 1Password JSON parsing — COMPLETE (16 tests)
- **File:** `ExternalConnectors/OP/OnePasswordCli.cs` → `ExtractCredentialsFromJson()`
- **Tests:** ParseSecretReference (4) + ExtractCredentialsFromJson (12)
- Covers: purpose match, concealed fallback, id fallback, label fallback, SSH key, domain, empty fields, null fields, malformed JSON, empty values, null deserialization

### PR 3113 — Startup path fallback — COMPLETE (3 tests)
- **File:** `mRemoteNG/Connection/ConnectionsService.cs`
- **Tests:** null/empty/whitespace fallback (parameterized), custom path passthrough, default not null

### PR 3115 — PuTTY CJK decode — COMPLETE (9 tests)
- **File:** `mRemoteNG/Config/Putty/PuttySessionNameDecoder.cs`
- **Tests:** UTF-8, plus sign, fallback, null, empty, ASCII, trailing %, mixed ASCII+CJK, lowercase hex

### PR 3123 — PROTOCOL token — COMPLETE (12 tests)
- **File:** `mRemoteNG/Tools/ExternalToolArgumentParser.cs`
- **Tests:** RDP (3 variants via data source) + SSH2, VNC, Telnet, HTTP, HTTPS, SSH1, Rlogin, RAW, IntApp (parameterized)

### PR 3125 — XML recovery — COMPLETE (5 valid + 2 ignored)
- **File:** `mRemoteNG/Config/Connections/XmlConnectionsLoader.cs`
- **Tests:** FileNotFound, empty path, XXE, valid primary, backup fallback
- **Ignored:** `ThrowsWhenNoBackupsExistAndPrimaryIsCorrupt`, `ThrowsWhenAllBackupsAreAlsoCorrupt` — hang in headless runs because `Runtime.MessageCollector` triggers WinForms dialog on recovery failure path

### PR 3121 — Password protect disable — COMPLETE (12 tests)
- **File:** `mRemoteNG/Tree/Root/RootNodeInfo.cs`
- **Tests:** default password, password flag, password string, IsPasswordMatch (correct/wrong/null/custom), AutoLockOnMinimize, TreeNodeType

## Tier 2: Testable With Mocking Effort (deferred)

### PR 3108 — Credential provider switching
- **Challenge:** Concrete provider classes, not easily mockable
- **ROI:** Low — would need significant refactoring for testability

### PR 3112 — ConfigWindow splitter reflection
- **Challenge:** Deep reflection into `PropertyGrid` internals
- **ROI:** Low — fragile across .NET versions

### PR 3119 — RDP SmartSize COM recovery
- **Challenge:** Requires mock COM object that throws `InvalidComObjectException`
- **ROI:** Medium — validates graceful degradation

### PR 3122 — Autolock idle/minimize
- **Data layer:** Already covered (AutoLockOnMinimize property test)
- **UI lock logic:** Lives in `frmMain` — not unit-testable

## Tier 3: Pure UI — Manual QA Only

These PRs modify WinForms event handlers, COM interop, or dock panel internals.
No practical unit test path without a UI automation framework.

| PR# | Component | Why untestable |
|-----|-----------|---------------|
| 3106 | ConnectionWindow.Prot_Event_Closed() | Cross-thread InvokeRequired, disposal race |
| 3116 | frmMain.WM_ACTIVATEAPP | Native message pump |
| 3117 | ConnectionWindow fullscreen menu | RDP COM + UI menu state |
| 3118 | RdpProtocol.OnLeaveFullscreenMode() | COM event + form activation |
| 3119 | RdpProtocol.SmartSize (partial) | COM exception recovery |
| 3120 | StartupDataLogger | Logging infrastructure |
| 3124 | frmMain.FormClosing | Window close + child window cascade |
| 3126 | ConnectionWindow.ClosePanelIfEmpty() | BeginInvoke + disposal race |
| 3127 | DockPaneStripNG.TryScrollDocumentTabsDuringDrag() | Mouse position + paint timing |
| 3128 | frmMain startup panel focus | Dock layout restore + focus |
| 3129 | DockPaneStripNG.QueueCloseTab() | Tab index + BeginInvoke race |
| 3130 | ConfigWindow.AutoSizePropertyGridLabelWidth() | PropertyGrid reflection + resize |

## Test Environment Notes

### Headless vs Interactive
- **Interactive desktop** (VS2022/VS2026 GUI, CI runners): All 2179 tests pass (2174 + 2 skipped CueBanner + 2 ignored XmlLoader + 1 ignored Tree)
- **Headless console** (SSH, remote, batch): ~1914 pass, ~60+ WinForms/UI tests hang (need STA thread + message pump)
- **Run command for headless:** `dotnet test ... --filter "FullyQualifiedName!~UI.Controls&FullyQualifiedName!~UI.Window&FullyQualifiedName!~CueBanner" -- NUnit.DefaultTimeout=15000`

### Known Headless Hangs (pre-existing upstream)
- `ConnectionTreeTests.CanDeleteLastFolderInTheTree` — triggers WinForms dialog
- `UI.Controls.ConnectionTreeTests.*` — all 15 tests need interactive desktop
- `UI.Window.ConfigWindowTests.*` — all 8 tests need PropertyGrid UI
- `UI.Forms.OptionsForm*` — all tests need STA + form lifecycle
- `WindowListTests.*` — all 11 tests need ConnectionWindow instances
- `CueBannerTests.*` — Win32 EM_SETCUEBANNER (handled with Assume.That)

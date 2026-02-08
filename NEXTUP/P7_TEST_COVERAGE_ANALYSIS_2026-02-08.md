# P7 Test Coverage Analysis — PRs #3105-#3130

Date: 2026-02-08
Scope: 26 codex PRs on `codex/release-1.79-bootstrap`
Baseline: 2148/2148 tests passing

## Coverage Summary

| Level | PRs | Count |
|-------|-----|-------|
| Well tested | 3105, 3109, 3111, 3114 | 4 |
| Moderate (has tests, gaps fillable) | 3107, 3113, 3115, 3121, 3122, 3123, 3125 | 7 |
| Config only (N/A) | 3110 | 1 |
| Zero coverage — testable logic | 3108, 3112 | 2 |
| Zero coverage — pure UI/WinForms | 3106, 3116-3120, 3124, 3126-3130 | 12 |

## Tier 1: Easily Testable (Priority — Write Now)

### PR 3107 — 1Password JSON parsing
- **File:** `ExternalConnectors/OP/OnePasswordCli.cs` → `ExtractCredentialsFromJson()`
- **Existing:** 5 tests (incl. concealed fallback)
- **Gaps:** missing fields, empty arrays, null values, multiple CONCEALED, malformed JSON
- **Effort:** Low — pure JSON parsing, no mocks needed

### PR 3108 — Credential provider switching
- **File:** `mRemoteNG/Connection/Protocol/PuttyBase.cs` lines 107-179
- **Existing:** 0 tests
- **Gaps:** switch/case for DSS, Passwordstate, 1Password, VaultOpenbao
- **Effort:** Medium — needs mocked external provider interfaces
- **Note:** Provider interfaces are concrete classes, not easily mockable without refactoring

### PR 3113 — Startup path fallback
- **File:** `mRemoteNG/Connection/ConnectionsService.cs`
- **Existing:** 1 test (3 parameterized cases: null, empty, whitespace)
- **Gaps:** valid path passthrough, paths with special chars
- **Effort:** Low — pure string logic

### PR 3115 — PuTTY CJK decode
- **File:** `mRemoteNG/Config/Putty/PuttySessionNameDecoder.cs`
- **Existing:** 3 tests (UTF-8, plus-sign, fallback)
- **Gaps:** empty string, no-encoding, mixed ASCII+CJK, malformed percent-encoding
- **Effort:** Low — pure string logic

### PR 3123 — PROTOCOL token
- **File:** `mRemoteNG/Tools/ExternalToolArgumentParser.cs`
- **Existing:** 3 tests (all RDP)
- **Gaps:** SSH, VNC, Telnet, HTTP, PuTTY protocols
- **Effort:** Low — existing parameterized test pattern

### PR 3125 — XML recovery
- **File:** `mRemoteNG/Config/Connections/XmlConnectionsLoader.cs` → `TryRecoverFromBackup()`
- **Existing:** 4 tests (FileNotFound, empty path, XXE, single backup)
- **Gaps:** multiple backups, all-corrupt scenario, no backups, file copy verification
- **Effort:** Medium — needs temp file setup

### PR 3121 — Password protect disable
- **File:** `mRemoteNG/Tree/Root/RootNodeInfo.cs` → `IsPasswordMatch()`
- **Existing:** 4 tests
- **Gaps:** edge cases (empty string vs null password)
- **Effort:** Low — existing pattern

## Tier 2: Testable With Mocking Effort

### PR 3112 — ConfigWindow splitter reflection
- **Methods:** `TryGetPropertyGridLabelWidth()`, `TrySetPropertyGridLabelWidth()`
- **Challenge:** Deep reflection into `PropertyGrid` internals
- **ROI:** Low — reflection tests are fragile across .NET versions

### PR 3119 — RDP SmartSize COM recovery
- **Property:** `RdpProtocol.SmartSize` getter/setter
- **Challenge:** Requires mock COM object that throws `InvalidComObjectException`
- **ROI:** Medium — validates graceful degradation

### PR 3122 — Autolock idle/minimize
- **Property:** `RootNodeInfo.AutoLockOnMinimize` (already tested)
- **Actual lock logic:** Lives in `frmMain` — pure UI, not testable
- **ROI:** Already covered for data layer

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

## Recommended New Tests

### Priority A — Write immediately (high ROI, low effort)

1. **OnePasswordCli edge cases** — ~8 new tests
2. **PROTOCOL token other protocols** — ~5 new parameterized cases
3. **PuTTY CJK decode edge cases** — ~4 new tests
4. **Startup path valid passthrough** — ~2 new tests
5. **XML recovery scenarios** — ~3 new tests

### Priority B — Write if time permits (medium ROI)

6. **Credential provider switching** — requires interface extraction or wrapper mocking
7. **PropertyGrid reflection** — fragile but validates critical UX fix

### Priority C — Skip (pure UI, manual QA covers)

8. All Tier 3 PRs — document as "manual QA required" in release checklist

## Expected Impact

- **Priority A:** ~22 new tests → coverage jumps for 5 PRs
- **Priority B:** ~10 new tests → coverage for 2 more PRs
- **Total potential:** 2148 → ~2180 tests

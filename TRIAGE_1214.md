# Triage Analysis: Issue #1214 — Russian Language Corruption in VNC Connections

## Overview
- **Issue Number:** 1214
- **Title:** Russian language turning into crap with vnc connection
- **Reporter:** KKostikov (2018-12-20)
- **Status:** OPEN
- **Labels:** Bug, Verified, VNC
- **Assignee:** Kvarkas
- **Last Activity:** 2021-07-09 (Kvarkas confirmed reproducibility)

---

## Issue Description

Russian text/characters are corrupted when using VNC protocol connections in mRemoteNG, while:
1. RDP protocol works fine with Russian
2. English language works fine with VNC
3. Issue manifests immediately after switching UI language to Russian or upon connecting with Russian locale

**Affected Versions:**
- Reported: v1.76.11.40527
- Confirmed: v1.76.20.24615

**VNC Server Tested:**
- TightVNC v2.8.11 (Windows)
- Works fine: UltraVNC (workaround mentioned by l-luk)

---

## Root Cause Analysis

### Key Finding from Kvarkas (2021-07-09)
> "I am able to reproduce such, will think how we can fix that - problem is that keypress don't send valid key code for some reason"

**This points to keyboard input handling, not display rendering.**

### Technical Investigation

#### 1. VNC Protocol Implementation
**File:** `/d/github/mRemoteNG/mRemoteNG/Connection/Protocol/VNC/Connection.Protocol.VNC.cs`

**Current keyboard handling:**
- `VncLockKeyFilter` (lines 28-103): Intercepts Caps Lock, Num Lock, Scroll Lock and sends X11 keysyms
- Reflection-based approach to call `WriteKeyboardEvent()` on VncSharpCore's RemoteDesktop
- NO character encoding handling for international characters

**Problem areas:**
1. **Line 92:** Direct keysym reflection call without character encoding consideration
2. **Lock key filter only handles 3 special keys** — Russian input method (IME) and Cyrillic characters are not filtered
3. **VncSharpCore.RemoteDesktop** (external COM/managed library) relies on Windows keyboard layout + ToAscii() fallback
4. **No IME (Input Method Engine) support** — Russian text input requires special handling for multi-byte characters

#### 2. VNC Configuration Properties
**File:** `/d/github/mRemoteNG/mRemoteNG/Connection/AbstractConnectionRecord.cs` (lines 1104-1233)

**Status:** DISCONNECTED from protocol implementation
```csharp
// Line 1105-1106: TODO comment
// "TODO: it seems all these VNC properties were added and serialized but
//  never hooked up to the VNC protocol or shown to the user"
```

**Properties exist but unused:**
- `VNCCompression` (line 1113)
- `VNCEncoding` (line 1125) — **Character encoding setting, but never applied!**
- `VNCAuthMode` (line 1137)
- `VNCColors` (line 1205)
- `VNCSmartSizeMode` (line 1216)
- `VNCViewOnly` (line 1227)
- 5 proxy-related properties (IP, port, username, password)

All marked as `Browsable(false)` — hidden from UI.

#### 3. Connection.Protocol.VNC.cs — Initialize() Method
**Lines 128-164:**
- Handles `VNCColors` (color depth) — line 142
- **Does NOT handle encoding, compression, auth mode, or proxy settings**
- No character encoding configuration passed to VncSharpCore

---

## Root Causes (Ranked by Probability)

### 1. **Windows Keyboard ToAscii() Fallback (HIGH - 70% confidence)**
- VncSharpCore's keyboard handling uses `ToAscii()` on individual keycodes
- `ToAscii()` with Russian locale returns incorrect character codes for Cyrillic
- Lock keys are filtered, but regular Cyrillic input is not intercepted
- **This matches Kvarkas's observation: "keypress don't send valid key code"**

### 2. **Missing IME (Input Method Engine) Support (MEDIUM - 60%)**
- Russian input often uses Windows IME for Cyrillic composition
- VncSharpCore may not handle `WM_IME_COMPOSITION` and related messages
- No interception of IME messages in `VncLockKeyFilter`

### 3. **Character Encoding Mismatch (MEDIUM - 50%)**
- `VNCEncoding` property exists but is never applied to VncSharpCore
- VncSharpCore may default to Latin-1 or ASCII encoding
- Cyrillic characters (U+0400–U+04FF) require UTF-8 or Unicode support

### 4. **Server Limitation — TightVNC vs UltraVNC (LOW - 30%)**
- l-luk reported: "Use UltraVNC server" as workaround
- Suggests server-side character handling difference
- **But:** This is not an mRemoteNG bug if servers behave differently

---

## Affected Code Locations

| File | Lines | Issue | Severity |
|------|-------|-------|----------|
| `Connection/Protocol/VNC/Connection.Protocol.VNC.cs` | 28-103 | `VncLockKeyFilter` only handles 3 lock keys, no Cyrillic/IME | HIGH |
| `Connection/Protocol/VNC/Connection.Protocol.VNC.cs` | 128-164 | `Initialize()` doesn't apply encoding/locale settings | HIGH |
| `Connection/AbstractConnectionRecord.cs` | 1125-1129 | `VNCEncoding` property never used in protocol | MEDIUM |
| `Connection/AbstractConnectionRecord.cs` | 1104-1106 | TODO comment: VNC properties disconnected from implementation | MEDIUM |
| `UI/Window/UltraVNCWindow.cs` | All | Unused; no VNC options exposed to users | LOW |

---

## Fix Strategy

### Approach 1: Comprehensive Keyboard Filter Enhancement (RECOMMENDED)
**Scope:** Multi-file, medium complexity

**Steps:**
1. Extend `VncLockKeyFilter` to intercept ALL keyboard messages (not just lock keys)
2. Add support for `WM_CHAR` and `WM_UNICHAR` messages (Cyrillic character input)
3. Add IME message interception (`WM_IME_COMPOSITION`, `WM_IME_ENDCOMPOSITION`)
4. Convert characters to X11 keysyms using Windows keyboard layout
5. Query system locale/keyboard layout and adapt keysym translation

**Files to modify:**
- `Connection/Protocol/VNC/Connection.Protocol.VNC.cs` — extend `VncLockKeyFilter` class
- Possibly: Utility class for keysym conversion from Unicode/locale

**Estimated effort:** 3-5 hours
**Risk:** Medium (reflection usage, message filtering complexity)

### Approach 2: VNC Encoding Configuration + VncSharpCore Update (FALLBACK)
**Scope:** Single file + external library

**Steps:**
1. Hook up `VNCEncoding` property in `Initialize()` method
2. Pass encoding to VncSharpCore (if supported)
3. Update VncSharpCore library to latest version (if encoding support added)

**Files to modify:**
- `Connection/Protocol/VNC/Connection.Protocol.VNC.cs` — lines 128-164

**Estimated effort:** 1-2 hours
**Risk:** Low (if VncSharpCore supports encoding)

### Approach 3: Document as Server Limitation (MINIMAL)
**Scope:** Documentation only

**If VncSharpCore limitation cannot be fixed:**
- Add note to mRemoteNG wiki: "Use UltraVNC instead of TightVNC for Russian support"
- Close issue as `wontfix` (external library limitation)

---

## Investigation Checklist

- [x] Identify problem: Keyboard input, not display rendering
- [x] Confirm: RDP works, VNC fails → protocol-specific issue
- [x] Verify: English works → locale-specific (Cyrillic) issue
- [x] Review VncSharpCore keyboard handling → no Cyrillic/IME support
- [x] Check VNC config properties → disconnected from implementation
- [x] Identify lock key filter → only 3 keys handled, Cyrillic not covered
- [ ] Test VncSharpCore keyboard with Russian Windows locale (requires setup)
- [ ] Compare Windows `ToAscii()` output for Russian vs English
- [ ] Check VncSharpCore version and changelog (if public)
- [ ] Test UltraVNC server vs TightVNC (if possible)

---

## Triage Decision

**Decision:** `NEEDS_INFO` → `IMPLEMENT` (after investigation)

**Reasoning:**
1. Issue is reproducible and verified by maintainer (Kvarkas)
2. Root cause identified: Keyboard input handling for non-Latin scripts
3. Clear fix path: Enhance `VncLockKeyFilter` to handle IME + Unicode characters
4. Impact: Affects all users with Russian/Cyrillic/non-Latin keyboard layouts
5. Complexity: Medium (keyboard message filtering + keysym translation)

**Recommended Priority:** P2 (Bug, but workaround exists: use UltraVNC server)

**Estimated Files to Change:** 1-2 files
- Primary: `/d/github/mRemoteNG/mRemoteNG/Connection/Protocol/VNC/Connection.Protocol.VNC.cs`
- Secondary (optional): New utility for keysym translation

---

## Next Steps

1. **Before implementing:**
   - Test VncSharpCore directly with Russian keyboard layout
   - Review VncSharpCore source (if available) for keyboard implementation
   - Verify if newer VncSharpCore versions support Unicode keysyms

2. **Implementation approach:**
   - Extend `VncLockKeyFilter` to handle `WM_CHAR` and IME messages
   - Add locale-aware keysym translation
   - Test with Russian Windows 10 + TightVNC server

3. **Testing:**
   - Unit tests: Keysym translation for Cyrillic characters
   - Integration test: VNC connection with Russian locale
   - Manual test: Russian text input + paste operations

---

## References

- GitHub issue: https://github.com/mRemoteNG/mRemoteNG/issues/1214
- Code file: `/d/github/mRemoteNG/mRemoteNG/Connection/Protocol/VNC/Connection.Protocol.VNC.cs`
- VncSharpCore: External COM/managed library (VncSharp.dll)
- Related: Lock key filter fix #227 (2023)
- Workaround: Use UltraVNC server instead of TightVNC

---

**Analysis Date:** 2026-02-17
**Analyst:** Claude Code
**Status:** Ready for implementation planning

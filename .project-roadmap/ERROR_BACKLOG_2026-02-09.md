# Error Backlog - 2026-02-09

Date: 2026-02-09
Scope: local code and existing local logs in `D:\github\mRemoteNG`.

Note: this analysis used repository inspection and existing test outputs. Fresh `dotnet` execution was blocked in this sandbox.

## Priority A (fix first)

1. VNC menu actions can throw `NotImplementedException` at runtime.
   - `mRemoteNG/Connection/Protocol/VNC/Connection.Protocol.VNC.cs`
   - `mRemoteNG/UI/Window/ConnectionWindow.cs`
   - Current state: UI exposes Chat/File Transfer actions, protocol methods throw.
   - Expected fix: disable unsupported actions in UI or implement graceful "not supported" handling.

2. RDM CSV serializer is incomplete and throws.
   - `mRemoteNG/Config/Serializers/ConnectionSerializers/Csv/RemoteDesktopManager/CsvConnectionsSerializerRdmFormat.cs`
   - Current state: `Serialize(ConnectionInfo model)` throws `NotImplementedException`.
   - Expected fix: implement serializer or remove/guard this code path.

3. Release workflow can race on release publishing (matrix creates same tag/name).
   - `.github/workflows/Build_mR-NB.yml`
   - Current state: matrix build and release in same job; all variants can target same tag.
   - Expected fix: split into build artifacts + single aggregate release job.

4. Local test scripts are not aligned with stability lessons.
   - `run-tests.ps1`
   - `test.ps1`
   - `build-and-test-baseline.ps1`
   - Current state: no mandatory `testhost.exe` cleanup; one script pipes `dotnet test` output via `Out-File`.
   - Expected fix: kill stale `testhost.exe` before build/test and avoid pipe-induced buffering issues.

## Priority B (important, after A)

5. `SupportedCultures` has serialization constructor that throws.
   - `mRemoteNG/App/SupportedCultures.cs`
   - Current state: class is `[Serializable]` but deserialization ctor throws `NotImplementedException`.
   - Expected fix: implement safe constructor or remove serialization contract.

6. Recovery-path tests remain ignored due UI dialog coupling.
   - `mRemoteNGTests/Config/Connections/XmlConnectionsLoaderTests.cs`
   - Current state: two tests ignored because failure path triggers WinForms dialogs.
   - Expected fix: decouple loader error flow from UI side effects, then re-enable tests.

7. Hardcoded default password remains in root node model.
   - `mRemoteNG/Tree/Root/RootNodeInfo.cs`
   - Current state: `DefaultPassword` = `"mR3m"`.
   - Expected fix: move to secure configuration/bootstrap flow and document migration behavior.

## Suggested execution order

1. Fix Priority A items 1 and 2 (runtime crash prevention).
2. Stabilize CI/testing path (Priority A items 3 and 4).
3. Resolve design debt and test coverage blockers (Priority B items 5-7).

## Status

| # | Item | Status | Commit |
|---|------|--------|--------|
| 1 | VNC NotImplementedException | DONE | Replaced throws with graceful messages; hid unsupported StartChat menu |
| 2 | RDM CSV serializer | DONE | Deleted dead partial class CsvConnectionsSerializerRdmFormat.cs |
| 3 | CI release race | DONE | Split into build + aggregate release job |
| 4 | Test script stability | DONE | Added testhost cleanup; replaced Out-File with Tee-Object |
| 5 | SupportedCultures ctor | DONE | Removed [Serializable] + throwing ctor (never serialized, singleton) |
| 6 | Recovery-path test coupling | DONE | Injected MessageCollector into XmlConnectionsLoader; re-enabled 2 tests |
| 7 | Hardcoded default password | DONE | Extracted to ConnectionFileDefaults.LegacyEncryptionKey with docs |

### From Supplement Analysis

| # | Item | Status | Detail |
|---|------|--------|--------|
| S1 | SQL Transactions (data loss) | DONE | Wrapped TRUNCATE/INSERT in DbTransaction in MetaDataRetriever + ConnectionsSaver |
| S2 | OpeningCommand localization TODO | DONE | Replaced placeholder with real description |
| S3 | Dead RDP Gateway decrypt code | DONE | Removed DecryptAuthCookieString + CryptUnprotectData + TsCryptDecryptString |
| S4 | File import type detection | DEFERRED | Medium priority — needs content-sniffing implementation |

### From Deep Dive Analysis

| # | Item | Status | Detail |
|---|------|--------|--------|
| D1 | RDP DoEvents timeout guard | DONE | Added 10s timeout + documented re-entrancy risk |
| D2 | SSH tunnel port TOCTOU | DONE | Documented as known limitation (PuTTY CLI constraint) |
| D3 | async void OpenConnection | DEFERRED | Low risk (full try/catch), high blast radius |
| D4 | God classes / ActiveX isolation | DEFERRED | Architectural, long-term |

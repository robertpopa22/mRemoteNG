# mRemoteNG — Semantic Code Audit (2026-05-31)

> Logic/semantic bug audit of the `main` branch source (~645 C# files, ~105K LOC, .NET 10 WinForms).
> Run as an 8-dimension multi-agent workflow; **every finding was adversarially re-verified against the
> current source** (each verifier re-opened the cited file/line, quoted the live code, and defaulted to
> rejecting on any mismatch).

## Scope & method

- **Targeted only what static analysis cannot catch.** SonarCloud, CodeQL, Roslynator, Meziantou and the
  .NET analyzers already run at **0 warnings**, so nullability, `ConfigureAwait`, culture/`IFormatProvider`
  (MA0004/MA0011/MA0076 are deliberately suppressed), naming and style were explicitly out of scope. The
  audit hunted the Qodo niche: SQL dialect/migration-ordering bugs, TOCTOU/disposal/threading races, COM
  release, crypto **misuse**, injection via dynamically-built strings, protocol-logic errors, off-by-one,
  and swallowed exceptions that change behavior.
- **8 dimensions:** crypto & secrets · injection · SQL data layer & migrations · concurrency & threading ·
  resource lifetime & COM · untrusted file import & parsing · protocol & network logic · settings & registry.
- **Adversarial verification (two passes, unioned):** the verify step is per-finding and **non-deterministic**
  — a resume pass dropped one confirmed finding (**M8**, recovered here via union) and flipped two verdicts.
  Merging both passes' confirmed sets and de-duplicating by file+line yields **20 confirmed** findings against
  current source, **1 rejected** (no reachable trigger). Severities below are the **verifier-corrected** values
  (6 reviewer ratings were downgraded). **H1 has conflicting verdicts across passes and is flagged contested —
  verify with a repro before acting.**

## Severity summary (19 confirmed)

| Severity | Count | Findings |
|----------|-------|----------|
| 🔴 Critical | 1 | C1 |
| 🟠 High (contested) | 1 | H1 |
| 🟡 Medium | 8 | M1–M8 |
| ⚪ Low | 10 | L1–L10 |

Two of the top findings (C1, H1) are in the **SQL version-upgrade path** — the same subsystem behind the
long-running upstream issue **#113** cluster. C1 is the MySQL analog of the MS-SQL bug #113 has been
chasing; H1 is a latent swallow-then-advance defect in `26To27` (note: #113's *specific* repro was earlier
traced to `29To30`/`31To32`/`3.1→3.2`, not `26To27` — H1 is a real but distinct latent bug, not a
contradiction of that triage).

## Implementation status (2026-05-31)

All 20 confirmed findings except the contested **H1** were fixed on branch `fix/audit-2026-05-31`
(16 atomic commits; full build + full test suite green — 6267 — verified before each commit). Findings
fixable by unit test carry a fail-before/pass-after test (C1, M4, M5, L9, L10).

| Finding | Commit | Finding | Commit |
|---------|--------|---------|--------|
| C1 | `1d27a33fa` | M5 | `ef9944f23` |
| M8 | `632688739` | L7/L8/L9 | `db63fb657` |
| M2 | `b9044bb42` | L1/L2 | `d73bfc962` |
| M3 | `4f0f432c1` | L3 | `020ede5a4` |
| M6 | `1dd3f0265` | L4 | `0aba27e73` |
| M7 | `19047d5ef` | L10 | `e729d5f73` |
| M1 | `553e37b95` | L5 | `a34922920` |
| M4 | `a59e015d8` | L6 | `455251816` |
| **H1** | **deferred** — verify with the #113 pre-2.7 MS-SQL repro first (see contested note) | | |

> **Not exercised by CI** (need real-environment verification): **C1** (no MySQL backend in tests),
> **M7** (GPO-provisioned proxy password), **M8** (WebView2 cert-error path). The full green suite proves
> *no regression*, not that these three fixes work end-to-end.

---

## 🔴 Critical

### C1 — MySQL schema upgrade aborts with duplicate-column error (pre-3.0 MySQL DBs cannot be opened)
- **File:** `mRemoteNG/Config/Serializers/Versioning/SqlMigrationHelper.cs:42` · **dim:** sql-migrations
- **Problem:** On opening a MySQL connections DB, `GetDatabaseMetaData → UpgradeSchema → UpgradeMysqlSchema`
  first **forward-ports** every `GetExpectedSchema()` column missing from `tblCons` (so all columns the
  `28To29`/`29To30` upgraders add already exist). The versioned upgraders then run. The MS-SQL branch is
  protected (`MakeMssqlColumnAddsIdempotent` wraps each `ADD` in `IF COL_LENGTH(...) IS NULL`, and
  `ExecuteMigrationIdempotent` catches "Duplicate column"), but the **MySQL branch of `ExecuteMigration`
  runs `mySqlAlter` raw with no idempotency guard and no try/catch** (lines 42–47). The first
  `ALTER TABLE tblCons ADD COLUMN InheritUseRestrictedAdmin ...` (`SqlVersion28To29Upgrader.cs:29`) hits
  MySQL error 1060 → uncaught `MySqlException` → propagates to `SqlDatabaseVersionVerifier` (lines 68–73)
  → logs and returns `false`. **Any pre-3.0 MySQL database fails to load.** The asymmetry is documented in
  `MakeMssqlColumnAddsIdempotent`'s own comment (lines 123–130) — the fix was applied to MS-SQL only.
- **Fix:** Route the MySQL branch through the same per-statement idempotency as `ExecuteMigrationIdempotent`
  (split into individual `ADD COLUMN` statements, catch `Duplicate column`), or have `28To29`/`29To30` call
  `ExecuteMigrationIdempotent`. Preserve the non-`ADD` `MODIFY COLUMN` statements those upgraders also carry.

---

## 🟠 High

### H1 — `SqlVersion26To27Upgrader` runs MS-SQL-only DDL for all backends and swallows failures while still advancing the version
- **File:** `mRemoteNG/Config/Serializers/Versioning/SqlVersion26To27Upgrader.cs:45` · **dim:** sql-migrations
- > ⚠️ **CONTESTED — verify with a repro before acting.** The two verify passes disagreed: one **rejected**
  > this (high confidence), arguing the swallow-then-advance manifests only under specific conditions and that
  > the team's own **#113 error-count investigation already cleared `26To27`** as the repro culprit (traced to
  > `29To30`/`31To32`/`3.1→3.2`). The other confirmed it High. **Discriminator (concrete, not another verify
  > pass):** take the pre-2.7 MS-SQL test DB from the #113 work, run the upgrade, and check whether `26To27`'s
  > `ALTER` actually throws → gets swallowed by `catch (SqlException)` **while `ConfVersion` still advances**.
  > If yes → real; if the forward-port doesn't pre-add those columns on the real DB → the rejection was right.
- **Problem:** `Upgrade()` executes one MS-SQL-flavored `sqlText` with **no backend branching** (`bit` type;
  `VmId/SSHOptions varchar NOT NULL DEFAULT NULL` — no length, self-contradictory). On MySQL the DDL throws
  `MySqlException`, which `catch (SqlException)` does **not** catch → aborts the chain. On MS-SQL, when the
  forward-port already added these columns the `ALTER` fails with error 2705 → swallowed by
  `catch (SqlException) { /* no-op */ }`, yet the method unconditionally `return new Version(2, 7)` —
  **advancing the reported version even though the schema (and the `ConfVersion` `UPDATE` in the same failed
  batch) never changed.** `27To28` repeats the swallow-then-advance pattern. This is exactly the #113-class
  failure mode. (Unlike `27To28`, `26To27` does **not** branch on connector type at all.)
- **Fix:** Branch on connector type (as `28To29`+ do); give `VmId/SSHOptions` an explicit length and drop
  `NOT NULL DEFAULT NULL`; do **not** return the advanced `Version` unless the `ExecuteNonQuery` succeeded
  (or make statements idempotent and let real failures propagate). Catch `DbException`, not only `SqlException`.

---

## 🟡 Medium

### M1 — REST `GET /api/tree` enumerates the live connection tree on a background thread without snapshotting
- **File:** `mRemoteNG/App/RestApiService.cs:375` · **dim:** concurrency
- **Problem:** The HttpListener loop runs on a thread-pool thread. `HandleGetTree` does
  `model.RootNodes.Select(ToTreeNode).ToList()` and `ToTreeNode` recursively enumerates
  `container.Children.Select(...)` — both are plain `List<>` enumerated lazily off-thread while the UI
  thread mutates them (Add/Delete/Paste/drag-drop). A concurrent structural mutation throws
  `InvalidOperationException` ("Collection was modified") → HTTP 500. The **other** read handlers already
  snapshot via `GetRecursiveChildList`'s `.ToArray()` (the documented #102 fix); only the tree-read path was
  left unsnapshotted.
- **Fix:** Iterate `RootNodes.ToArray()` / `container.Children.ToArray()` in `HandleGetTree`/`ToTreeNode`,
  or route the read through `InvokeOnUiThread`. (Opt-in REST API only.)

### M2 — `ProtocolVNC` leaks instance into the static `FrmMain.ClipboardChanged` event on a failed connect
- **File:** `mRemoteNG/Connection/Protocol/VNC/Connection.Protocol.VNC.cs:554` · **dim:** resource-disposal
  · *reviewer said High → corrected Medium*
- **Problem:** `SetEventHandlers()` subscribes the instance to the **static** `FrmMain.ClipboardChanged` at
  the start of `Connect()` (while VNC state is 0). The handler is removed only in `Disconnect()` /
  `VNCEvent_Disconnected()`; `Dispose(bool)` does not detach it. On a **failed connect** (unreachable/refused
  server — common; `VNCClipboardRedirect` defaults true), the `catch` returns without detaching, and
  `Close() → Dispose` with state 0 never raises `ConnectionLost`, so the static event permanently roots the
  `ProtocolVNC` instance + its `ConnectionInfo` (**holding the VNC password**) + the control for the process
  lifetime, and the dead handler fires on every clipboard change. *(The verifier disproved the original
  "still-connected close" and "message-filter leak" sub-claims; the failed-connect leak is the real one.)*
- **Fix:** In `Dispose(bool)` (and/or an override of `Close()`) idempotently detach
  `FrmMain.ClipboardChanged -= VNCEvent_ClipboardChanged` and remove the message filter — removing an
  unsubscribed handler is a safe no-op.

### M3 — `PuttyBase.CreatePipe` blocks a fire-and-forget thread forever, holding the plaintext SSH password
- **File:** `mRemoteNG/Connection/Protocol/PuttyBase.cs:179` · **dim:** resource-disposal
- **Problem:** For PuTTY ≥ 0.81, `Connect()` starts `CreatePipe` on a dedicated thread seeded with
  `{random}{password}`. It creates a `NamedPipeServerStream` and calls the **untimed, blocking**
  `server.WaitForConnection()` — no timeout, no `CancellationToken`, no try/finally. If putty.exe never
  connects (interactive/canceled auth, launch failure, crash, tab closed early), the thread blocks forever,
  permanently holding a live pipe handle and the **resident plaintext password**, and (foreground thread)
  can block process exit. The Vault/OpenBao path 25 lines below already does it correctly
  (`using` + `WaitForConnectionAsync` with a 10s `CancellationTokenSource`).
- **Fix:** Mirror the Vault path: `using` the server, `WaitForConnectionAsync` with a timeout, dispose on
  timeout/exception, and clear the password buffer.

### M4 — `Enum.Parse` (not `TryParse`) on imported CSV `NodeType` aborts the whole import
- **File:** `mRemoteNG/Config/Serializers/ConnectionSerializers/Csv/CsvConnectionsDeserializerMremotengFormat.cs:192`
  · **dim:** import-parsing
- **Problem:** The per-row `NodeType` is parsed with `Enum.Parse<TreeNodeType>(...)`, which throws on any
  invalid value (empty cell, `Conn`, out-of-range number). This is the **only** enum in this ~1380-line
  deserializer not using `Enum.TryParse` (the other ~70 fall back to a default). No row-level try/catch, so
  one bad cell discards the **entire file** (caught only per-file at `App/Import.cs:177`). Short-row padding
  doesn't help — the bad *value*, not a missing column, is the trigger.
- **Fix:** `Enum.TryParse<TreeNodeType>(..., true, out var nodeType)` with `TreeNodeType.Connection` fallback.

### M5 — `Convert.ToInt32` on imported `.rdp` "server port" throws on malformed/overflow value, aborting import
- **File:** `mRemoteNG/Config/Serializers/MiscSerializers/RemoteDesktopConnectionDeserializer.cs:54`
  · **dim:** import-parsing
- **Problem:** `case "server port": connectionInfo.Port = Convert.ToInt32(value, ...)` throws
  `FormatException`/`OverflowException` on non-numeric/out-of-range input; no inner guard, so a single bad
  line kills the whole `.rdp` import. The adjacent "full address" branch already tolerates a port via `Uri`.
- **Fix:** `int.TryParse(value, NumberStyles.Integer, InvariantCulture, out int port)`; assign only on success.

### M6 — Startup autosave reads an orphaned settings key (always 0), so autosave is never armed at launch
- **File:** `mRemoteNG/Config/Settings/SettingsLoader.cs:152` · **dim:** config-settings
- **Problem:** `SetAutoSave()` — the **only** startup path that arms `tmrAutoSave` — reads
  `Properties.OptionsConnectionsPage.Default.AutoSaveEveryMinutes` (default **0**, never written anywhere).
  The real, user-facing setting is `Properties.OptionsBackupPage.Default.AutoSaveEveryMinutes` (default 50),
  which the Options UI and the registry/GPO override both read/write. The `<= 0 return` guard therefore fires
  every launch: **a user-configured (e.g. 50-min) autosave and any GPO-deployed override silently do nothing
  after a restart** — autosave only starts if Options is reopened and OK'd in the same session.
- **Fix:** Read `Properties.OptionsBackupPage.Default.AutoSaveEveryMinutes` for both the guard and the
  interval. Consider deleting the dead `OptionsConnectionsPage.AutoSaveEveryMinutes`.

### M7 — Registry/GPO proxy password stored **decrypted** but every consumer **decrypts** it → broken proxy auth
- **File:** `mRemoteNG/Config/Settings/Registry/OptRegistryUpdatesPage.cs:228` · **dim:** config-settings
- **Problem:** `UpdateProxyAuthPass` is contractually stored **encrypted** (written via `Encrypt` at
  `UpdatesPage.cs:130`; read via `Decrypt` at `UpdatesPage.cs:88` and `AppUpdater.cs:69`). But
  `ApplyAuthentication()` decrypts the registry value and stores the **plaintext** into `UpdateProxyAuthPass`.
  When provisioned via registry/GPO, consumers then call `Decrypt` on already-plaintext text →
  `Convert.FromBase64String` fails → **unhandled `EncryptionException`** (neither consumer wraps it), breaking
  authenticated-proxy update checks and leaving the proxy password at rest in plaintext. The sibling
  `OptRegistrySqlServerPage.ApplySQLPassword:149` correctly stores the *encrypted* value (decrypt is only a
  round-trip validation) — the two copy-paste handlers diverge precisely here, and Updates is wrong.
- **Fix:** Store the encrypted `proxyAuthPass` (not `decryptedPassword`); keep the `Decrypt` call only as a
  discarded validation, mirroring `ApplySQLPassword`.

### M8 — HTTPS (WebView2) connections silently accept **any** invalid TLS certificate for the configured host
- **File:** `mRemoteNG/Connection/Protocol/Http/Connection.Protocol.HTTPBase.cs:347` · **dim:** protocol-network
  · *most security-relevant finding in the audit*
- **Problem:** `CoreWebView2_ServerCertificateErrorDetected` unconditionally sets
  `e.Action = CoreWebView2ServerCertificateErrorAction.AlwaysAllow` for **every** kind of TLS certificate
  error (expired, self-signed, hostname/CN mismatch, untrusted CA, revoked), gated only by string equality
  between the configured host and the request host. The handler is wired up unconditionally in
  `InitializeWebView2Async` (line 247) for every EdgeChromium HTTPS connection — **no per-connection setting,
  no user prompt, no thumbprint pinning, and the decision is `AlwaysAllow` (persistent for the session).**
  Scoping to the configured host does not mitigate the threat: an on-path/MITM attacker intercepting the
  connection to *that very host* is exactly what cert validation exists to catch, and here it is defeated
  silently — the user gets no indication their HTTPS session is being intercepted. (Project-wide grep
  confirms this is the only `ServerCertificateError`/`AlwaysAllow` usage — no gating setting exists.)
- **Fix:** Don't auto-allow. Keep the default (block), or prompt once per host + cert thumbprint behind an
  opt-in per-connection setting (mirroring RDP's `AuthenticationLevel`), and if a bypass is offered, **pin to
  the accepted certificate thumbprint** rather than `AlwaysAllow` for any cert presented for that host.
- > *Recovered via union: confirmed `isReal=true` (high) in pass 1; the non-deterministic resume pass dropped
  > it entirely. Verbatim source quote is in `tasks/wolsl0fay.output`.*

---

## ⚪ Low (confirmed, hardening / niche-trigger)

| ID | File:line | Problem | Fix |
|----|-----------|---------|-----|
| L1 | `App/RestApiService.cs:149` | REST `X-API-Key` checked with non-constant-time `string.Equals` (CWE-208). Real, but localhost-only + thread-pool jitter makes timing recovery infeasible → hardening only. | `CryptographicOperations.FixedTimeEquals` on fixed-length buffers. |
| L2 | `Security/TotpProvider.cs:85` | TOTP code compared with non-constant-time `string.Equals` in the skew loop. Local, in-process, 6-digit, 30s rotation → hardening only. | `FixedTimeEquals` / constant-time match flag across the skew window. |
| L3 | `Security/RandomGenerator.cs:20` | `randomGen.Next(availableChars.Length - 1)` never selects the last alphabet char (off-by-one) in the credential-repo "Auth" canary token; ~0.016 bits/char entropy loss. | Use `Next(availableChars.Length)`. |
| L4 | `Config/DatabaseConnectors/MySqlDatabaseConnector.cs:53` | MySQL connection string built by raw `$"..."` interpolation (no escaping) vs sibling MSSQL `SqlConnectionStringBuilder`; a `;`/`=` in password/db name injects connection options. Local-operator config only. | Use `MySqlConnectionStringBuilder`. |
| L5 | `Connection/Protocol/.../Connection.Protocol.VNC.cs:864` | `TestConnect` keeps connect-state in **static** fields + a static `ManualResetEvent`; a timed-out probe's orphaned `BeginConnect` callback unconditionally `Set()`s the shared event, so a later probe reads stale state → spurious "Could not establish TCP connection"/failed reconnect. | Per-call state object, or `TcpClient.ConnectAsync().WaitAsync(timeout)`. |
| L6 | `Connection/Protocol/ProtocolBase.cs:54` | `ConnectionTab` setter subscribes `ResizeBegin/Resize/ResizeEnd` that are never removed; with KeepTabsOpenAfterDisconnect the reused tab accumulates dead handlers (+ rooted closed protocols) per reconnect. Base handlers are no-ops → bounded slow leak. | Detach the three handlers in `Dispose(bool)`/`Close()`. |
| L7 | `Config/Serializers/MiscSerializers/PuttyConnectionManagerDeserializer.cs:130` | `Convert.ToInt32(<port>)` throws on non-numeric/overflow → aborts whole `.dat` import (siblings use `int.TryParse`). | `int.TryParse` with protocol default. |
| L8 | `Config/Serializers/ConnectionSerializers/Csv/RemoteDesktopManager/CsvConnectionsDeserializerRdmFormat.cs:106` | Guards only on column **count**, then indexes `connectionCsv[headers.IndexOf("Host")]`; a renamed/missing required header → `IndexOf` returns -1 → `IndexOutOfRangeException` aborts the file. | Verify each required header index `>= 0` before indexing. |
| L9 | `Config/Serializers/MiscSerializers/MicrosoftRdClientBackupDeserializer.cs:35` | `EnumerateArray()` called on `Groups`/`Credentials`/`Connections` after only a `TryGetProperty` existence check; a non-array value → `InvalidOperationException` aborts the `.rdb` import (the `GetStringProperty` helper *does* check `ValueKind`). | Guard each with `ValueKind == JsonValueKind.Array`. |
| L10 | `Tools/PortScanner.cs:293` | IPv4 range arithmetic uses **signed** `Int32` (`IpAddressToInt32`); a range straddling `128.0.0.0` inverts min/max and corrupts the host-count/list. All consequence paths are caught by surrounding try/catch (scan silently fails), LAN ranges unaffected. | Do range math in `uint` (`ToUInt32`, `CompareTo`, sanity-cap the count). |

---

## Considered and rejected (1)

- **`RdpProtocol8.cs:69` — "static `SystemEvents.DisplaySettingsChanged` + `_frmMain.ResizeEnd` unsubscribed
  only in `Close()`, not `Dispose()`"** → **rejected (not real).** The structural asymmetry exists on paper,
  but the verifier disproved the trigger: `RdpProtocol8.Close()` runs the unsubscribe **synchronously** before
  `base.Close()` spins the background STA thread (no race), and **every** concrete mid-session disposal path
  routes through `Close()` first (connection document tabs aren't persisted in dock-layout XML, so the
  #121/#110 layout-reload bypass doesn't apply). The only `Close`-bypass is the app-shutdown dispose cascade,
  which is harmless (process exiting). Residual is at most a low-priority defensive cleanup (shared teardown
  method called from both `Close` and `Dispose`). Documented here so it isn't re-reported.

---

## Recommended action order

1. **C1** — `SqlMigrationHelper` MySQL branch. Highest impact (pre-3.0 MySQL DBs cannot open); directly
   adjacent to the active #113 cluster. Next `fix-repo` target.
2. **M8, M2, M3** — security-relevant: silent TLS cert-validation bypass (M8); credential-bearing leaks
   (VNC password rooted on failed connect M2; SSH password held by a blocked pipe thread M3).
3. **M6, M7** — silent feature breakage (autosave never armed; GPO proxy auth broken). User-visible,
   easy fixes.
4. **M1, M4, M5** — robustness (REST 500 race; import aborts on one bad field).
5. **H1 — verify first.** Run the #113 pre-2.7 MS-SQL repro (see the contested note above) before committing
   a fix; do **not** treat it as a confident High until the swallow-then-advance is reproduced.
6. **Low batch** — L1–L10: constant-time compares (L1/L2), `TryParse`/header-guard import hardening
   (L7/L8/L9), and the bounded leaks/off-by-one (L3/L4/L5/L6/L10). Good candidates to bundle.

> **Method note:** the audit ran as two passes. Pass 1 lost the concurrency dimension and ~8 verifier agents
> to `StructuredOutput` failures; a resume (cached agents instant) recovered the concurrency findings — **but
> the resume's protocol-network review re-ran non-deterministically and *dropped* a finding pass 1 had
> confirmed (M8, the HTTPS cert bypass).** The final set is the **union** of both passes' confirmed findings,
> de-duplicated by file+line. Per-finding verdicts proved noisy across passes (M8 dropped; H1 and L1 flipped
> rejected↔confirmed) — hence the union merge and the H1 contested flag. 28 agents, ~3.85M subagent tokens
> total. Every finding carries a verbatim current-source quote in the run artifacts
> (`tasks/wthsaonpt.output` pass 2, `tasks/wolsl0fay.output` pass 1 for M8).

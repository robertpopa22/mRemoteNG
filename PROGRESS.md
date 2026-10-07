# PROGRESS — handover

## Issue #210 — reload loop + idle power optimizations (2026-10-07)

Issue: https://github.com/robertpopa22/mRemoteNG/issues/210

### State at handover
- All code for #210 is committed on `main` (this commit). No branch was used; the fork follows the zero-branch rule now written in `CLAUDE.md` › Branch Strategy.
- Verified in an isolated `git archive HEAD` + changed-files tree: full `build.ps1` 0 errors, 0 new warnings in touched files; `run-tests-core.sh` 7544/7544 passed (phase-1 crashes in 4 groups passed on the sequential retry, runner's known behaviour).
- Follow-up verification: the same staged source was built again in an isolated index snapshot (tree `41e41ab5db04ffaf21ea42dfb4c5a8d054040d64`), with 0 build errors; `run-tests.ps1 -Headless` passed 7564/7564 in 333 s, with 0/9 phase-1 group crashes. The source/index and unrelated WIP were unchanged by verification. Only this handover document was updated afterwards.
- UI check: 8/8 existing StartupAcceptanceTests and ConnectionTreeAcceptanceTests passed in the isolated Windows lab using local build 3733. The app showed its main window and connection tree; File > Open > Replace displayed `replacement-only` and removed the original rows, both normally and after a saved layout reload. This is a startup/file-open regression check, not a reproduction of the battery drain or the external-change loop.
- NOT verified: a battery re-measurement after the fix. The affected laptop was unreachable during follow-up; no process was stopped and no new power result is claimed. Issue #210 is still OPEN; close it only after that measurement.
- The working tree also holds unrelated, uncommitted WIP from another session ("windowed fullscreen": ConnectionInfo.cs, ConnectionInitiator.cs, RdpProtocol*.cs, DockPaneStripNG.cs, FloatWindowNG.cs, ConnectionTreeWindow.cs, ConnectionTree.cs, ConnectionContextMenu.cs, Language.*, WindowedFullscreen*.cs + tests, OvernightRetentionProbe.cs, DPI test fixes in RdpProtocolDesktopScaleFactorTests.cs and FrmTaskDialogLayoutTests.cs). It did not compile at handover time. Not part of #210, left untouched.

### What changed (summary)
- `ConnectionsService.LoadConnections`: file mtime captured before read and stored in `LastFileUpdate` after a successful load (also on decrypt failure); SHA-256 content short-circuit, used only by the synchronizer path (`skipIfContentUnchanged`); settings saved only when `ConnectionFilePath` changed.
- `Startup.CreateConnectionsProvider`: single live `RemoteConnectionsSyncronizer` (file mode reuses, SQL mode disposes then replaces).
- `RemoteConnectionsSyncronizer`: file mode polls every 300 s (safety net); falls back to `SQLReloadInterval` (min 5 s) when the watcher reports `WatcherDegraded`.
- `FileConnectionsUpdateChecker`: exact-tick comparison, `Renamed` + `FileName` filter, `Error` handler, dispose race guards.
- `PortableSettingsProvider`: no write when nothing is dirty (known limitation: non-primitive property reads mark dirty).
- New `App/PowerAwareness.cs` (`OnBattery`, `IsMinimized`, `LowPowerMode`, `Changed`); applied to `HostStatusMonitor` (every 4th cycle in low power, immediate pass when it ends, re-bound to the model after sync reload), `RuntimeDiagnostics` (heartbeat 60 s → 300 s, due time preserved; watchdog/pulse paused while minimized), `frmMain` auto-lock tick made cheap, `PuttyBase` title poll 2 s and skipped while minimized.
- Tests added/extended: FileConnectionsUpdateCheckerTests, RemoteConnectionsSyncronizerTests, PortableSettingsProviderTests, ConnectionsServiceContentHashTests, PowerAwarenessTests, RuntimeDiagnosticsHeartbeatTests, HostStatusMonitorPowerTests.
- Docs: `docs/bp/BP-010-power-aware-background-work.md` + index row; `CLAUDE.md` branch policy.

### Open items
1. Re-measure on a laptop on battery (`powercfg /batteryreport`, `powercfg /srumutil`): target idle drain < 5 W with no connections open; then close #210 with the numbers.
2. Residual: FAT/SMB 2 s mtime granularity can miss a write landing in the same 2 s window as a load. The content hash avoids redundant reloads once a change is detected; it does not detect a change hidden by an equal timestamp.
3. `SingleInstance=True` was applied and read back in the maintainer's portable source settings, with a backup. Propagation to the affected laptop is not confirmed; this does not change application defaults for other users.
4. Optional follow-ups from the review: mark host-status icons stale while probing is throttled; `PortableSettingsProvider` dirty detection for non-primitive properties.
5. Nightly for `a5e97098a10d4efb6b87c205b196d704062cb9a7` was published on 2026-10-07 at 12:47 Europe/Bucharest, with x64 ZIP/MSI assets. The maintainer's portable source was updated from build 3727 to Release 3733 using that ZIP: SHA-256 verified, 71/71 declared runtime assemblies present, 24 settings files preserved, program hashes matched, `SingleInstance=True`, and the hidden `--version` smoke check exited 0. Installation on the affected laptop and battery validation are still pending; no stable release was cut.

### Follow-up checkpoint — 2026-10-07
- The affected laptop remained inaccessible through both registered management routes on the follow-up check. Its old process ID is not assumed to still identify the same instance.
- When access returns: inspect current processes and live connections, close only the confirmed idle duplicate, verify the installed build and `SingleInstance`, then collect comparable battery samples and a fresh battery report. Stopping an old process alone measures a mitigation; validation of the code fix requires the corrected build to be running.
- The separately developed fullscreen change was subsequently committed by its own session. This follow-up changed only the handover document in the repository.

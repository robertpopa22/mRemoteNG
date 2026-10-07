# PROGRESS — handover

Current status: #210 and #211 are closed with recorded test conclusions. The latest published source revision was installed and tested on the affected laptop; the notes below retain the earlier handover history.

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
1. Completed: #210 is CLOSED as fixed on 2026-10-07. The affected laptop was updated and exercised; the final short battery sample gave 4.02 W from capacity loss and 5.54 W from discharge-rate telemetry. The sub-5-W target is met by the capacity calculation only; a long-term endurance result is not claimed.
2. Residual: FAT/SMB 2 s mtime granularity can miss a write landing in the same 2 s window as a load. The content hash avoids redundant reloads once a change is detected; it does not detect a change hidden by an equal timestamp.
3. Completed: `SingleInstance=True` was applied and read back both in the maintainer's portable source and on the affected laptop, with backups. This does not change application defaults for other users.
4. Optional follow-ups from the review: mark host-status icons stale while probing is throttled; `PortableSettingsProvider` dirty detection for non-primitive properties.
5. Nightly for `a5e97098a10d4efb6b87c205b196d704062cb9a7` was published on 2026-10-07 at 12:47 Europe/Bucharest, with x64 ZIP/MSI assets. The maintainer's portable source was updated from build 3727 to Release 3733 using that ZIP: SHA-256 verified, 71/71 declared runtime assemblies present, 24 settings files preserved, program hashes matched, `SingleInstance=True`, and the hidden `--version` smoke check exited 0. Installation on the affected laptop and battery validation are still pending; no stable release was cut.

### Follow-up checkpoint — 2026-10-07
- The affected laptop remained inaccessible through both registered management routes on the follow-up check. Its old process ID is not assumed to still identify the same instance.
- When access returns: inspect current processes and live connections, close only the confirmed idle duplicate, verify the installed build and `SingleInstance`, then collect comparable battery samples and a fresh battery report. Stopping an old process alone measures a mitigation; validation of the code fix requires the corrected build to be running.
- The separately developed fullscreen change was subsequently committed by its own session. This follow-up changed only the handover document in the repository.

### Live verification and closure — 2026-10-07
- Installed the latest published source revision `49145f2808896641a81b7b4e709d4cec05515297` as a self-contained build. Full build and 7592/7592 automated tests passed; package completeness and installed hashes were checked. The existing profile was preserved and `SingleInstance=True` was read back.
- Actual app: the current tree loaded (54 model nodes), the main window and tree were visible, minimize/restore passed, and a second launch left one instance. Touching the connection file produced one unchanged-content skip and zero reloads. A harmless XML content change produced exactly one successful reload over 70 s, without a recurring loop. The original bytes were restored and settings stayed unchanged.
- Battery sample after the manual connection was closed: 18/18 discharging samples over 171.89 s, 192 mWh lost (4.02 W for the whole laptop), discharge-rate mean 5.54 W, application CPU 0.0068%. Baseline looping-process CPU was 7.71%. Brightness differed (80% before, 56% afterwards), so total power differences are not a controlled attribution to the fix. The earlier five-minute sample included a short RDP session and was kept separate.
- A fresh battery report was generated. The laptop subsequently went offline when the maintainer left; the final raw sample and battery-report XML remain there, while their received numeric summary is documented. No long-term battery endurance or post-fix SRUM share was measured.
- The maintainer also confirmed #211's real workflow: Alt+click → fullscreen in a window → work → close → return to the tab. This is human acceptance of that path, distinct from automated test results.
- Closing evidence: https://github.com/robertpopa22/mRemoteNG/issues/210#issuecomment-6036871604. The app was left available for normal use. The old build's post-cleanup process remnant was recorded; shutdown of the new build was not separately retested.
- At the maintainer's explicit final closure request, #211 was also closed with the human acceptance and automated results: https://github.com/robertpopa22/mRemoteNG/issues/211#issuecomment-6036925358. That comment explicitly limits the live acceptance to the reported Alt+click/work/close/return path; the dedicated fullscreen lab scenarios and all keyboard/DPI/taskbar combinations were not rerun in this follow-up.

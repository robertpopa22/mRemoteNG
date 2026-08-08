## 1. Dependencies

- [x] 1.1 Add `Serilog`, `Serilog.Sinks.File`, and `Serilog.Enrichers.Thread` PackageVersion entries to `Directory.Packages.props` (requires explicit approval — off-limits for automated issue-fix agents per repo policy)
- [x] 1.2 Add the corresponding `PackageReference`s to `mRemoteNG\mRemoteNG.csproj`
- [x] 1.3 Remove the `log4net` PackageVersion entry from `Directory.Packages.props`
- [x] 1.4 Remove the `log4net` `PackageReference` from `mRemoteNG\mRemoteNG.csproj`

## 2. Logger core rewrite

- [x] 2.1 Rewrite `mRemoteNG\App\Logger.cs` to build a Serilog `Logger` (rolling file sink: `fileSizeLimitBytes = 10*1024*1024`, `rollOnFileSizeLimit = true`, `retainedFileCountLimit = 5`, `rollingInterval = RollingInterval.Infinite`, `WithThreadId()` enrichment, output template matching `{Timestamp:yyyy-MM-dd HH:mm:ss,fff} [{ThreadId}] {Level:u6}- {Message:lj}{NewLine}{Exception}`) instead of `XmlConfigurator.Configure`
- [x] 2.2 Retype `Logger.Log` from log4net's `ILog?` to Serilog's `Serilog.ILogger?`, keeping the `Instance` accessor shape
- [x] 2.3 Reimplement `SetLogPath(string)` to dispose and rebuild the Serilog logger against the new path, guarding against concurrent log writes during the rebuild
- [x] 2.4 Remove `mRemoteNG\log4net.config` and its `CopyToOutputDirectory` wiring in `mRemoteNG.csproj`

## 3. Call site updates

- [x] 3.1 Update `mRemoteNG\Messages\MessageWriters\TextLogMessageWriter.cs` to call Serilog's `Log.Information/Debug/Warning/Error` instead of log4net's `ILog` API (no exception-object overload needed — `MessageCollector` pre-formats exceptions into plain text before they reach this writer)
- [x] 3.2 Update the direct `Logger.Instance.Log?.*` calls in `mRemoteNG\UI\Forms\frmOptions.cs` and `mRemoteNG\UI\Forms\OptionsPages\StartupExitPage.cs` to the Serilog method names (`Warn`→`Warning`; `Debug`/`Error` unchanged)
- [x] 3.3 Confirmed `mRemoteNG\App\CommandLineParser.cs` only calls `Logger.Instance.SetLogPath(...)` (no `.Log?.*` calls) — signature unchanged, no edit needed
- [x] 3.4 Grep the codebase for any remaining `log4net` imports or `ILog` references to confirm none remain outside the files above

## 4. Documentation

- [x] 4.1 Update `docs\CREDITS.md` to credit Serilog instead of log4net
- [x] 4.2 Add a `CHANGELOG.md` entry describing the log4net → Serilog migration and the log line format change

## 5. Verification

- [x] 5.1 Compile `mRemoteNG\mRemoteNG.csproj` and resolve any remaining log4net-typed references
- [ ] 5.2 Run the app; generate log entries at Debug/Info/Warning/Error and confirm they appear in `mRemoteNG.log` with timestamp, thread id, level, and message — **NOT automated this session**: launching the local build reused the developer's real `%LOCALAPPDATA%\mRemoteNG` profile (live saved connections, real production log file); killed the process immediately without interacting further rather than risk touching live data. Needs manual verification by a human running the app normally.
- [ ] 5.3 Trigger an exception-producing path and confirm the stack trace is written to the log file — manual verification, same reason as 5.2
- [ ] 5.4 In Options → Notifications, change the log file path/directory at runtime and confirm subsequent messages go to the new location with no app restart and no dropped messages — manual verification, same reason as 5.2
- [ ] 5.5 Verify size-based rotation: force (or temporarily lower) the size threshold and confirm the file rolls over and backups are capped at 5 — manual verification, same reason as 5.2
- [ ] 5.6 Verify Debug-class message filtering still suppresses/allows log lines per the existing `LogMessageTypeFilteringOptions` settings — manual verification, same reason as 5.2
- [x] 5.7 Run the full test suite (`run-tests-core.sh`) — 6553/6555 passed; the 2 failures are both the same pre-existing test (`ConnectionsServiceStartupPathTests.StartupConnectionPathReturnsSavedPathWhenItIsTheSoleCandidate`), confirmed unrelated by reproducing it against unmodified `dev` HEAD (stash/rebuild/retest) — fails identically with log4net still in place, so not a regression from this change

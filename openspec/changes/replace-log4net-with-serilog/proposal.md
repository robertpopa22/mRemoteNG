## Why

log4net is unmaintained by any active roadmap beyond compatibility patches, uses a legacy XML configuration model that is awkward to extend (structured/contextual logging, async sinks, enrichers), and is the last piece of app-wide infrastructure still configured via hand-rolled `XmlConfigurator` bootstrapping rather than the modern `Microsoft.Extensions`-aligned ecosystem the rest of the .NET 10 codebase is moving toward. Serilog is the actively maintained, de-facto standard replacement, offers the same rolling-file behavior out of the box via `Serilog.Sinks.File`, and unlocks structured logging and easier future sinks (e.g. console, seq, event log) without custom appender code.

## What Changes

- Replace the `log4net` package reference and `log4net.config` with `Serilog` + `Serilog.Sinks.File` (rolling file sink configured to match current size-based rotation: 10MB per file, 5 backups, fixed base filename).
- Rewrite `mRemoteNG\App\Logger.cs` as a thin Serilog-backed facade preserving its existing public surface (`Log`, `SetLogPath`, initialization) so the ~150 call sites that go through `MessageCollector`/`Messages` never need to change.
- Update `mRemoteNG\Messages\MessageWriters\TextLogMessageWriter.cs` to call Serilog's `ILogger` (`Information/Debug/Warning/Error`) instead of log4net's `ILog`.
- Update the ~30 direct `Logger.Instance.Log?.*` call sites in `UI\Forms\frmOptions.cs` and the 1 call site in `App\CommandLineParser.cs` to the new logger API.
- Runtime log path reconfiguration (`SetLogPath`, driven by the Options → Notifications page) is preserved with equivalent behavior: switching the log directory/file at runtime without an app restart.
- Remove the `log4net` PackageVersion pin from `Directory.Packages.props` and the `PackageReference`/`log4net.config` copy-to-output wiring from `mRemoteNG.csproj`; add the Serilog package references and any replacement config file.
- Update `docs\CREDITS.md` to credit Serilog instead of log4net.
- **BREAKING**: the on-disk log file line format changes from log4net's `PatternLayout` (`%date [%thread] %-6level- %message%newline`) to a Serilog output template. The proposal fixes the new template to closely match the old one (timestamp, thread id, level, message) so existing log-scraping habits/support workflows aren't disrupted, but any external tooling doing strict text-format parsing of `mRemoteNG.log` would need to adjust.

## Capabilities

### New Capabilities
- `application-logging`: mRemoteNG's file-based diagnostic logging pipeline — what gets logged, at what levels, to what location, with what rotation policy, and how the log destination can be reconfigured at runtime. This capability did not previously have a spec; it is being introduced now as part of documenting the Serilog-backed replacement.

### Modified Capabilities
(none — no other existing specs in `openspec/specs/` reference logging behavior)

## Impact

- **Affected code**: `mRemoteNG\App\Logger.cs` (rewritten), `mRemoteNG\Messages\MessageWriters\TextLogMessageWriter.cs`, `mRemoteNG\UI\Forms\frmOptions.cs` (~30 call sites), `mRemoteNG\App\CommandLineParser.cs` (1 call site + `SetLogPath` usage).
- **Config**: `mRemoteNG\log4net.config` removed/replaced; `mRemoteNG.csproj` content-copy wiring updated.
- **Dependencies**: `Directory.Packages.props` — remove `log4net` (3.3.2), add `Serilog` + `Serilog.Sinks.File` (and `Serilog.Sinks.Debug` if the existing `#if DEBUG`-only `DebugConsoleMessageWriter` path is folded into Serilog rather than left as-is — decision deferred to design.md).
- **Not affected**: the ~150 call sites that log via `MessageCollector.AddMessage`/`AddExceptionMessage` — they go through the `IMessageWriter` abstraction and require no code changes, only the `TextLogMessageWriter` implementation underneath changes.
- **Tests**: no existing test in `mRemoteNGTests` asserts on log4net or `Logger` directly; `MessageCollector`/`MessageTypeFilterDecorator`/`OnlyLogMessageFilter` tests are unaffected as long as `TextLogMessageWriter`'s externally observable behavior (one line written per accepted message) is preserved.
- **Docs**: `docs\CREDITS.md` (attribution update), `CHANGELOG.md` (entry for the migration).

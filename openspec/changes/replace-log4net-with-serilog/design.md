## Context

mRemoteNG's file logging is currently backed by log4net 3.3.2, configured from `mRemoteNG\log4net.config` (a single `RollingFileAppender`, 10MB/5-backup rotation, `%date [%thread] %-6level- %message%newline` layout) and bootstrapped manually via `XmlConfigurator.Configure` inside `mRemoteNG\App\Logger.cs`. That file is the *only* place in the codebase that imports `log4net.*` — everything else routes through `mRemoteNG.Messages.MessageCollector`/`IMessageWriter`, with `TextLogMessageWriter` being the sole bridge from that abstraction into the concrete logger. A handful of call sites (`UI\Forms\frmOptions.cs`, ~30 calls; `App\CommandLineParser.cs`, 1 call) hit `Logger.Instance.Log?.*` directly. Runtime log-path reconfiguration (`Logger.SetLogPath`, triggered from the Options → Notifications page) is a real, exercised feature and must keep working without an app restart.

## Goals / Non-Goals

**Goals:**
- Swap the underlying logging engine from log4net to Serilog with no observable regression in rotation policy, log content, or runtime path reconfiguration.
- Keep the change contained to `App\Logger.cs`, `TextLogMessageWriter.cs`, and the handful of direct call sites — the `MessageCollector`/`IMessageWriter` abstraction is not touched.
- Preserve a human-readable log line format close enough to the log4net output that existing support workflows (asking users to attach `mRemoteNG.log`) keep working.

**Non-Goals:**
- Introducing structured/semantic logging (`{Property}` templates with typed values) across the ~150 `MessageCollector` call sites — out of scope for this change; `TextLogMessageWriter` will pass through plain strings, same as today.
- Adding new sinks (console, Seq, EventLog, etc.) beyond the existing rolling file (and the existing `#if DEBUG` console writer, which stays as-is unless folding it in turns out to be trivial — see Open Questions).
- Changing `MessageCollector`/`MessageClass`/filtering behavior (`LogMessageTypeFilteringOptions`) — that logic stays exactly as-is; only what's underneath `TextLogMessageWriter` changes.

## Decisions

**Serilog + Serilog.Sinks.File over Microsoft.Extensions.Logging directly.** ME.Logging is a facade, not an implementation — it still needs a provider, and Serilog is the most direct, actively-maintained drop-in for log4net's file-rotation use case with the least ceremony (no DI container currently wired for logging; `Logger.cs` is a static-ish facade today and stays that way). Considered NLog as an alternative; Serilog was chosen for broader ecosystem adoption and simpler file-sink configuration.

**Preserve `Logger.cs`'s public shape.** Keep `Logger.Instance`, `Log` (retyped from log4net's `ILog` to Serilog's `Serilog.ILogger`), and `SetLogPath(string)` as the public surface, so `TextLogMessageWriter.cs` and the direct call sites in `frmOptions.cs`/`CommandLineParser.cs` need only method-name/argument-shape edits (e.g., `Log.Info(msg)` → `Log.Information(msg)`, `Log.Warn` → `Log.Warning`), not structural rewrites. Rejected alternative: introducing an `ILoggerFactory`/DI-based logger throughout — too large a blast radius for a pure engine swap.

**Rolling file sink configuration mirrors current policy exactly.** `WriteTo.File(path, rollingInterval: RollingInterval.Infinite, fileSizeLimitBytes: 10*1024*1024, rollOnFileSizeLimit: true, retainedFileCountLimit: 5, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss,fff} [{ThreadId}] {Level:u6}- {Message:lj}{NewLine}{Exception}")`. `RollingInterval.Infinite` + `rollOnFileSizeLimit` reproduces log4net's size-based-only rolling (no date-based rolling was in use). Thread id requires `Serilog.Enrichers.Thread` (`WithThreadId()`) since Serilog doesn't capture it by default the way log4net's `%thread` did.

**`SetLogPath` reinitializes the Serilog logger in place.** Serilog loggers are normally immutable once built; `SetLogPath` will dispose the current `Serilog.Core.Logger` and rebuild it against the new path using a `LoggingLevelSwitch`-free rebuild (level filtering already happens above this layer in `LogMessageTypeFilteringOptions`, so the Serilog pipeline itself stays unfiltered/`Verbose` and lets everything through — matching log4net's fixed root level of `ALL`).

**Exception formatting.** Where `TextLogMessageWriter`/direct call sites currently pass a formatted exception string (via log4net's `Log.Error(message, exception)` overload), use Serilog's equivalent `Log.Error(exception, message)` overload so `{Exception}` in the output template renders the full stack trace, matching today's behavior of appending stack traces as separate log lines via `AddExceptionStackTrace`.

## Risks / Trade-offs

- [Output template drift] → Serilog's default timestamp/format tokens don't byte-for-byte match log4net's `PatternLayout`. Mitigation: the output template above was chosen to reproduce the same fields in the same order (timestamp, thread, level, message, exception); verify by diffing a sample log file before/after in manual testing (see tasks.md).
- [Thread-id enrichment adds a new package dependency] (`Serilog.Enrichers.Thread`) → Small, well-maintained, single-purpose package; acceptable addition. Alternative (drop thread id from the format) was rejected because it's a real regression support may rely on.
- [`SetLogPath` rebuild race] → If a log write races with a path-change rebuild, a message could be lost or throw. Mitigation: guard the rebuild with the same lock/pattern `Logger.cs` already uses around repository reconfiguration today (log4net's `XmlConfigurator.Configure` call is likewise not inherently thread-safe against concurrent `Log` calls, so this is not a new class of risk, just needs equivalent care in the rewrite).
- [Central Package Management version pin] → `Directory.Build.props`/`Directory.Packages.props` edits are explicitly off-limits for an ordinary issue-fix agent per this repo's `CLAUDE.md`; the tasks.md implementation step touching `Directory.Packages.props` must be called out for explicit user/orchestrator approval rather than done silently by an automated agent.

## Migration Plan

1. Add `Serilog`, `Serilog.Sinks.File`, `Serilog.Enrichers.Thread` package versions to `Directory.Packages.props` and the reference to `mRemoteNG.csproj` (requires explicit approval — see Risks).
2. Rewrite `App\Logger.cs` to build/rebuild a Serilog `Logger` instead of log4net's repository, preserving `Instance`, `Log`, `SetLogPath`.
3. Update `TextLogMessageWriter.cs` and the direct call sites (`frmOptions.cs`, `CommandLineParser.cs`) to Serilog's `ILogger` method names.
4. Remove `log4net.config`, the log4net `PackageReference`, and its `Directory.Packages.props` entry; remove the `CopyToOutputDirectory` wiring for `log4net.config` from `mRemoteNG.csproj`.
5. Update `docs\CREDITS.md` and add a `CHANGELOG.md` entry.
6. Manual verification: run the app, generate log entries at each level, trigger the Options → Notifications "change log path" flow, and confirm rotation triggers past 10MB (or verify the sink config against a smaller test threshold).

No feature flag or gradual rollout is applicable — this is a build-time dependency swap with no persisted state to migrate (log files themselves are not read back by the app, only written and optionally tailed for the currently-unused `DebugReportBuilder`).

**Rollback**: revert the commit(s); no data migration or schema changes make rollback anything other than a plain git revert.

## Open Questions

- Should `App\Diagnostics\DebugReportBuilder.cs` (currently unused/no UI caller found) be updated in this change, or left as dead code touching the log file path directly via `File.ReadLines` (which remains valid regardless of the logging engine, since it reads the file, not the log4net API)? Proposed: leave untouched — it doesn't call into `log4net` or `Logger` directly, so it's unaffected either way.
- Should the `#if DEBUG`-only `DebugConsoleMessageWriter` be folded into Serilog's console sink for consistency, or left as its own independent writer? Proposed: leave as-is to minimize blast radius; can be a follow-up change.

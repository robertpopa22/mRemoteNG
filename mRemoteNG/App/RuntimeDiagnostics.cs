using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using log4net;

namespace mRemoteNG.App
{
    /// <summary>
    /// Low-volume, privacy-safe runtime telemetry written through the normal rolling log.
    /// Values accepted here are deliberately narrow: connection labels, hostnames, usernames,
    /// paths, credentials and exception messages must never reach a diagnostic event.
    /// </summary>
    internal static class RuntimeDiagnostics
    {
        private const int HeartbeatIntervalMs = 60_000;
        private const int UiWatchdogIntervalMs = 5_000;
        private const int UiStallThresholdMs = 5_000;
        private const int ResumeGapThresholdMs = 30_000;

        private static readonly Stopwatch Uptime = Stopwatch.StartNew();
        private static readonly Process CurrentProcess = Process.GetCurrentProcess();
        private static readonly Lock StateLock = new();
        private static Timer? _heartbeatTimer;
        private static Timer? _uiWatchdogTimer;
        private static long _lastCpuTicks;
        private static long _lastHeartbeatTicks;
        private static long _lastUiPulseTicks;
        private static long _lastWatchdogTicks;
        private static long _uiStallStartedTicks;
        private static int _uiStallReported;
        private static int _initialized;

        internal static void Initialize()
        {
            if (Interlocked.Exchange(ref _initialized, 1) != 0)
                return;

            long now = Stopwatch.GetTimestamp();
            _lastHeartbeatTicks = now;
            _lastCpuTicks = CurrentProcess.TotalProcessorTime.Ticks;

            WriteInfo("process_start",
                Field("version", SafeVersion(Assembly.GetExecutingAssembly().GetName().Version?.ToString())),
                Field("runtime", SafeVersion(Environment.Version.ToString())),
                Field("arch", Environment.Is64BitProcess ? "x64" : "x86"),
                Number("logical_processors", Environment.ProcessorCount));

            LogRemoteDesktopEngineInventory();
            _heartbeatTimer = new Timer(_ => WriteHeartbeat(), null, HeartbeatIntervalMs, HeartbeatIntervalMs);
        }

        internal static void StartUiWatchdog()
        {
            long now = Stopwatch.GetTimestamp();
            Interlocked.Exchange(ref _lastUiPulseTicks, now);
            Interlocked.Exchange(ref _lastWatchdogTicks, now);

            lock (StateLock)
            {
                _uiWatchdogTimer ??= new Timer(_ => CheckUiResponsiveness(), null,
                    UiWatchdogIntervalMs, UiWatchdogIntervalMs);
            }
        }

        internal static void PulseUi() =>
            Interlocked.Exchange(ref _lastUiPulseTicks, Stopwatch.GetTimestamp());

        internal static void Shutdown()
        {
            _heartbeatTimer?.Dispose();
            _uiWatchdogTimer?.Dispose();
            _heartbeatTimer = null;
            _uiWatchdogTimer = null;
            WriteInfo("process_stop", Number("uptime_ms", Uptime.ElapsedMilliseconds));
            LogManager.Flush(2_000);
        }

        internal static string NewCorrelationId() => Guid.NewGuid().ToString("N")[..12];

        internal static void StartupPhase(string phase, long durationMs) =>
            WriteInfo("startup_phase", Field("phase", SafeToken(phase)), Number("duration_ms", durationMs));

        internal static void ConnectionLoad(bool database, bool import, int nodeCount, long durationMs, string outcome) =>
            WriteInfo("connections_load",
                Field("source", database ? "database" : "xml"),
                Boolean("import", import),
                Number("nodes", nodeCount),
                Number("duration_ms", durationMs),
                Field("outcome", SafeOutcome(outcome)));

        internal static void ConnectionSave(bool database, bool propertyTriggered, int nodeCount, long durationMs, string outcome) =>
            WriteInfo("connections_save",
                Field("source", database ? "database" : "xml"),
                Field("trigger", propertyTriggered ? "property_change" : "system_or_manual"),
                Number("nodes", nodeCount),
                Number("duration_ms", durationMs),
                Field("outcome", SafeOutcome(outcome)));

        internal static void RdpPhase(string rdpSession, string phase, long durationMs, string? version = null,
            int? primaryCode = null, uint? extendedCode = null)
        {
            FieldValue[] fields =
            [
                Field("rdp_session", SafeCorrelationId(rdpSession)),
                Field("phase", SafeToken(phase)),
                Number("duration_ms", durationMs),
                Field("version", SafeVersion(version)),
                NullableNumber("code", primaryCode),
                NullableNumber("extended_code", extendedCode)
            ];
            WriteInfo("rdp_phase", fields);
        }

        internal static void RdpCapability(string capability, bool supported) =>
            WriteInfo("rdp_capability", Field("name", SafeToken(capability)), Boolean("supported", supported));

        internal static void SafeException(string source, Exception exception, bool fatal = false)
        {
            string frames = BuildSafeFrames(exception);
            string signatureInput = $"{exception.GetType().FullName}|{exception.HResult:X8}|{frames}";
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(signatureInput));
            string signature = Convert.ToHexString(hash)[..12].ToLowerInvariant();

            WriteError("exception",
                Field("source", SafeToken(source)),
                Field("type", SafeTypeName(exception.GetType())),
                Field("hresult", $"0x{exception.HResult:X8}"),
                Field("signature", signature),
                Boolean("fatal", fatal),
                Quoted("frames", frames));

            if (fatal)
                LogManager.Flush(2_000);
        }

        private static void WriteHeartbeat()
        {
            try
            {
                long now = Stopwatch.GetTimestamp();
                long elapsedMs = ElapsedMilliseconds(Interlocked.Exchange(ref _lastHeartbeatTicks, now), now);
                CurrentProcess.Refresh();
                long cpuTicks = CurrentProcess.TotalProcessorTime.Ticks;
                long previousCpuTicks = Interlocked.Exchange(ref _lastCpuTicks, cpuTicks);
                double cpuPercent = elapsedMs <= 0
                    ? 0
                    : (cpuTicks - previousCpuTicks) / (double)TimeSpan.TicksPerMillisecond /
                      elapsedMs / Math.Max(1, Environment.ProcessorCount) * 100d;

                WriteInfo("heartbeat",
                    Number("uptime_ms", Uptime.ElapsedMilliseconds),
                    Decimal("cpu_percent", Math.Clamp(cpuPercent, 0d, 100d)),
                    Number("working_set_mb", BytesToMiB(CurrentProcess.WorkingSet64)),
                    Number("private_mb", BytesToMiB(CurrentProcess.PrivateMemorySize64)),
                    Number("managed_mb", BytesToMiB(GC.GetTotalMemory(false))),
                    Number("gc0", GC.CollectionCount(0)),
                    Number("gc1", GC.CollectionCount(1)),
                    Number("gc2", GC.CollectionCount(2)),
                    Number("threads", CurrentProcess.Threads.Count),
                    Number("handles", CurrentProcess.HandleCount));
            }
            catch (Exception ex)
            {
                SafeException("heartbeat", ex);
            }
        }

        private static void CheckUiResponsiveness()
        {
            long now = Stopwatch.GetTimestamp();
            long previousWatchdog = Interlocked.Exchange(ref _lastWatchdogTicks, now);
            if (previousWatchdog != 0 && ElapsedMilliseconds(previousWatchdog, now) > ResumeGapThresholdMs)
            {
                // The machine probably slept; resume should not be classified as an application stall.
                Interlocked.Exchange(ref _lastUiPulseTicks, now);
                Interlocked.Exchange(ref _uiStallReported, 0);
                return;
            }

            long lastPulse = Interlocked.Read(ref _lastUiPulseTicks);
            long lagMs = ElapsedMilliseconds(lastPulse, now);
            if (lagMs >= UiStallThresholdMs)
            {
                if (Interlocked.Exchange(ref _uiStallReported, 1) == 0)
                {
                    Interlocked.Exchange(ref _uiStallStartedTicks, now);
                    WriteWarn("ui_stall", Field("state", "detected"), Number("lag_ms", lagMs));
                }
                return;
            }

            if (Interlocked.Exchange(ref _uiStallReported, 0) == 1)
            {
                long started = Interlocked.Read(ref _uiStallStartedTicks);
                WriteInfo("ui_stall", Field("state", "recovered"),
                    Number("duration_ms", ElapsedMilliseconds(started, now)));
            }
        }

        private static void LogRemoteDesktopEngineInventory()
        {
            string systemDirectory = Environment.SystemDirectory;
            string mstscPath = Path.Combine(systemDirectory, "mstsc.exe");
            string mstscAxPath = Path.Combine(systemDirectory, "mstscax.dll");
            string? freeRdpPath = FindOnPath("wfreerdp.exe");

            WriteInfo("rdp_engine_inventory",
                Boolean("mstsc", File.Exists(mstscPath)),
                Field("mstsc_version", SafeVersion(GetFileVersion(mstscPath))),
                Boolean("activex", File.Exists(mstscAxPath)),
                Field("activex_version", SafeVersion(GetFileVersion(mstscAxPath))),
                Boolean("freerdp", freeRdpPath != null),
                Field("freerdp_version", SafeVersion(GetFileVersion(freeRdpPath))));
        }

        private static string? FindOnPath(string executable)
        {
            string? path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(path)) return null;
            foreach (string directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    string candidate = Path.Combine(directory.Trim(), executable);
                    if (File.Exists(candidate)) return candidate;
                }
                catch
                {
                    // Ignore malformed PATH entries; no path is ever written to the log.
                }
            }
            return null;
        }

        private static string? GetFileVersion(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
            try { return FileVersionInfo.GetVersionInfo(path).FileVersion; }
            catch { return null; }
        }

        private static string BuildSafeFrames(Exception exception)
        {
            try
            {
                return string.Join("|", new StackTrace(exception, false).GetFrames()?
                    .Take(8)
                    .Select(frame =>
                    {
                        MethodBase? method = frame.GetMethod();
                        return SafeTypeName(method?.DeclaringType) + "." + SafeToken(method?.Name);
                    }) ?? []);
            }
            catch
            {
                return "unavailable";
            }
        }

        private static long ElapsedMilliseconds(long start, long end) =>
            start <= 0 ? 0 : (long)((end - start) * 1000d / Stopwatch.Frequency);

        private static long BytesToMiB(long bytes) => bytes / (1024 * 1024);

        private static string SafeOutcome(string? value) => value is "success" or "failed" or "cancelled" ? value : "unknown";

        private static string SafeCorrelationId(string? value) =>
            value?.Length == 12 && value.All(Uri.IsHexDigit) ? value.ToLowerInvariant() : "invalid";

        private static string SafeVersion(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? "unknown"
                : new string(value.Where(c => char.IsAsciiDigit(c) || c is '.' or '-' or '+').Take(48).ToArray());

        private static string SafeTypeName(Type? type) => SafeToken(type?.FullName, 160);

        private static string SafeToken(string? value, int maxLength = 64)
        {
            if (string.IsNullOrWhiteSpace(value)) return "unknown";
            return new string(value.Where(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '-').Take(maxLength).ToArray());
        }

        private static FieldValue Field(string key, string value) => new(SafeToken(key), SafeToken(value, 160));
        private static FieldValue Number(string key, long value) => new(SafeToken(key), value.ToString(CultureInfo.InvariantCulture));
        private static FieldValue NullableNumber(string key, long? value) =>
            new(SafeToken(key), value?.ToString(CultureInfo.InvariantCulture) ?? "na");
        private static FieldValue Boolean(string key, bool value) => new(SafeToken(key), value ? "true" : "false");
        private static FieldValue Decimal(string key, double value) => new(SafeToken(key), value.ToString("F2", CultureInfo.InvariantCulture));
        private static FieldValue Quoted(string key, string value) =>
            new(SafeToken(key), "\"" + value.Replace("\"", "'", StringComparison.Ordinal)
                .Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal) + "\"");

        private static void WriteInfo(string eventName, params FieldValue[] fields) => Write(DiagnosticLevel.Info, eventName, fields);
        private static void WriteWarn(string eventName, params FieldValue[] fields) => Write(DiagnosticLevel.Warning, eventName, fields);
        private static void WriteError(string eventName, params FieldValue[] fields) => Write(DiagnosticLevel.Error, eventName, fields);

        private static void Write(DiagnosticLevel level, string eventName, FieldValue[] fields)
        {
            ILog? log = Logger.Instance.Log;
            if (log == null) return;
            string message = "[Perf] event=" + SafeToken(eventName) + " " +
                             string.Join(" ", fields.Select(field => $"{field.Key}={field.Value}"));
            message = message.TrimEnd();
            switch (level)
            {
                case DiagnosticLevel.Info:
                    log.Info(message);
                    break;
                case DiagnosticLevel.Warning:
                    log.Warn(message);
                    break;
                case DiagnosticLevel.Error:
                    log.Error(message);
                    break;
            }
        }

        private readonly record struct FieldValue(string Key, string Value);
        private enum DiagnosticLevel { Info, Warning, Error }
    }
}

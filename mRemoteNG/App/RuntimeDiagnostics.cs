using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using log4net;
using mRemoteNG.App.Diagnostics;
using mRemoteNG.App.Info;

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
        private const int HeartbeatLowPowerIntervalMs = 300_000;
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
        private static bool _heartbeatLowPower;
        private static int _heartbeatPeriodMs;
        private static DateTime _heartbeatNextDueUtc;
        private static bool _uiWatchdogPaused;

        /// <summary>Heartbeat period: 60 s normally, 300 s on battery or while minimized (#210).</summary>
        internal static int GetHeartbeatIntervalMs(bool lowPower) =>
            lowPower ? HeartbeatLowPowerIntervalMs : HeartbeatIntervalMs;

        /// <summary>
        /// Due time for the heartbeat timer after the period changed: the time left until the
        /// already scheduled tick, capped at the new period. Re-arming with the full new period on
        /// every change would starve the heartbeat when the window is minimized/restored repeatedly.
        /// </summary>
        internal static int ComputeHeartbeatDueMs(DateTime nextDueUtc, DateTime nowUtc, int newPeriodMs)
        {
            double remainingMs = (nextDueUtc - nowUtc).TotalMilliseconds;
            if (remainingMs <= 0) return 0;
            return (int)Math.Min(remainingMs, newPeriodMs);
        }

        internal static void Initialize()
        {
            if (Interlocked.Exchange(ref _initialized, 1) != 0)
                return;

            long now = Stopwatch.GetTimestamp();
            _lastHeartbeatTicks = now;
            _lastCpuTicks = CurrentProcess.TotalProcessorTime.Ticks;

            MachineFacts machine = ProcessResourceSnapshot.ReadMachine();
            WriteInfo("process_environment",
                Number("os_major", machine.Major),
                Number("os_minor", machine.Minor),
                Number("os_build", machine.Build),
                Number("os_ubr", machine.Ubr),
                Field("os_product", machine.Product),
                Field("os_display", machine.Display),
                Field("os_install", machine.Install),
                Number("gdi_quota", machine.GdiQuota),
                Number("user_quota", machine.UserQuota));
            WriteInfo("process_start",
                ResourceFields(ProcessResourceSnapshot.Capture(refreshExpensive: true)).Prepend(
                    Number("logical_processors", Environment.ProcessorCount)).Prepend(
                    Field("arch", Environment.Is64BitProcess ? "x64" : "x86")).Prepend(
                    Field("runtime", SafeVersion(Environment.Version.ToString()))).Prepend(
                    Field("edition", Runtime.ShowsAsPortable ? "portable" : "installed")).Prepend(
                    Field("build_id", GeneralAppInfo.BuildId)).Prepend(
                    Field("version", SafeVersion(Assembly.GetExecutingAssembly().GetName().Version?.ToString()))).ToArray());

            LogRemoteDesktopEngineInventory();
            PowerAwareness.Initialize();
            lock (StateLock)
            {
                _heartbeatLowPower = PowerAwareness.LowPowerMode;
                int heartbeatMs = GetHeartbeatIntervalMs(_heartbeatLowPower);
                _heartbeatPeriodMs = heartbeatMs;
                _heartbeatNextDueUtc = DateTime.UtcNow.AddMilliseconds(heartbeatMs);
                _heartbeatTimer = new Timer(_ => WriteHeartbeat(), null, heartbeatMs, heartbeatMs);
            }
            PowerAwareness.Changed += OnPowerStateChanged;
        }

        // Heartbeat slows down on battery/minimized. The UI watchdog is a safety feature, so it
        // follows only the minimized state (while minimized the UI pulse is paused as well).
        private static void OnPowerStateChanged(object? sender, EventArgs e)
        {
            lock (StateLock)
            {
                // Shutdown may have disposed the timers; a late notification must be a no-op.
                if (_heartbeatTimer == null && _uiWatchdogTimer == null)
                    return;

                // Read inside the lock so concurrent notifications apply in a consistent order.
                bool lowPower = PowerAwareness.LowPowerMode;
                bool minimized = PowerAwareness.IsMinimized;

                if (_heartbeatTimer != null && _heartbeatLowPower != lowPower)
                {
                    _heartbeatLowPower = lowPower;
                    int heartbeatMs = GetHeartbeatIntervalMs(lowPower);
                    if (heartbeatMs != _heartbeatPeriodMs)
                    {
                        DateTime nowUtc = DateTime.UtcNow;
                        int dueMs = ComputeHeartbeatDueMs(_heartbeatNextDueUtc, nowUtc, heartbeatMs);
                        _heartbeatPeriodMs = heartbeatMs;
                        _heartbeatNextDueUtc = nowUtc.AddMilliseconds(dueMs);
                        _heartbeatTimer.Change(dueMs, heartbeatMs);
                    }
                }

                if (_uiWatchdogTimer != null && _uiWatchdogPaused != minimized)
                {
                    _uiWatchdogPaused = minimized;
                    if (minimized)
                    {
                        _uiWatchdogTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                    else
                    {
                        long now = Stopwatch.GetTimestamp();
                        Interlocked.Exchange(ref _lastUiPulseTicks, now);
                        Interlocked.Exchange(ref _lastWatchdogTicks, now);
                        Interlocked.Exchange(ref _uiStallReported, 0);
                        _uiWatchdogTimer.Change(UiWatchdogIntervalMs, UiWatchdogIntervalMs);
                    }
                }
            }
        }

        internal static void StartUiWatchdog()
        {
            long now = Stopwatch.GetTimestamp();
            Interlocked.Exchange(ref _lastUiPulseTicks, now);
            Interlocked.Exchange(ref _lastWatchdogTicks, now);

            lock (StateLock)
            {
                _uiWatchdogPaused = PowerAwareness.IsMinimized;
                int dueMs = _uiWatchdogPaused ? Timeout.Infinite : UiWatchdogIntervalMs;
                _uiWatchdogTimer ??= new Timer(_ => CheckUiResponsiveness(), null, dueMs, dueMs);
            }
        }

        internal static void PulseUi() =>
            Interlocked.Exchange(ref _lastUiPulseTicks, Stopwatch.GetTimestamp());

        internal static void Shutdown()
        {
            PowerAwareness.Changed -= OnPowerStateChanged;
            lock (StateLock)
            {
                _heartbeatTimer?.Dispose();
                _uiWatchdogTimer?.Dispose();
                _heartbeatTimer = null;
                _uiWatchdogTimer = null;
            }
            FieldValue[] exitSample;
            try
            {
                exitSample = ResourceFields(ProcessResourceSnapshot.Capture(refreshExpensive: true));
            }
            catch (Exception)
            {
                exitSample = [];
            }

            WriteInfo("process_stop", exitSample.Prepend(Number("uptime_ms", Uptime.ElapsedMilliseconds)).ToArray());
            LogManager.Flush(2_000);
        }

        internal static string NewCorrelationId() => Guid.NewGuid().ToString("N")[..12];

        private static volatile string _lastStartupPhase = "none";

        internal static void StartupPhase(string phase, long durationMs)
        {
            _lastStartupPhase = SafeToken(phase);
            WriteInfo("startup_phase", Field("phase", SafeToken(phase)), Number("duration_ms", durationMs));
        }

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
            int? primaryCode = null, uint? extendedCode = null, bool refreshResources = false, bool reconnect = false)
        {
            WriteInfo("rdp_phase", [..RdpPhaseFields(rdpSession, phase, durationMs, version, primaryCode, extendedCode, reconnect),
                ..ResourceFields(ProcessResourceSnapshot.Capture(refreshResources))]);
        }

        /// <summary>The fields of <see cref="RdpPhase"/> before the resource snapshot, as written.</summary>
        internal static string FormatRdpPhase(string rdpSession, string phase, long durationMs, string? version = null,
            int? primaryCode = null, uint? extendedCode = null, bool reconnect = false) =>
            string.Join(" ", RdpPhaseFields(rdpSession, phase, durationMs, version, primaryCode, extendedCode, reconnect)
                .Select(field => $"{field.Key}={field.Value}"));

        private static FieldValue[] RdpPhaseFields(string rdpSession, string phase, long durationMs, string? version,
            int? primaryCode, uint? extendedCode, bool reconnect)
        {
            // reconnect=true marks a login (or connect) the control reached again on its own, after
            // ARC or a silent reconnection; absent on the first one so older lines read unchanged.
            // disc_class reads a disconnect reason, so only a disconnect carries one. A logon_state
            // line carries the OnLogonError code, which is a logon state (NLA, credential UI, ...),
            // not a disconnect reason: it is written as logon_code so it cannot be read as one.
            bool logonState = phase == "logon_state";
            FieldValue[] fields =
            [
                Field("rdp_session", SafeCorrelationId(rdpSession)),
                Field("phase", SafeToken(phase)),
                Number("duration_ms", durationMs),
                Field("version", SafeVersion(version)),
                NullableNumber(logonState ? "logon_code" : "code", primaryCode),
                NullableNumber("extended_code", extendedCode),
                Field("disc_class", phase == "disconnected" ? ClassifyDisconnect(primaryCode, extendedCode) : "na")
            ];
            return reconnect ? [..fields, Boolean("reconnect", true)] : fields;
        }

        /// <summary>A step of opening a tab that took at least this long is named in <c>slow_step</c>.</summary>
        internal const long SlowOpenStepMs = 1000;

        /// <summary>
        /// One line per RDP tab opened: how long each step took, so a UI stall during an open can
        /// be pinned to its step. Step names and numbers only.
        /// </summary>
        internal static void RdpOpen(string rdpSession, string outcome, bool firstControl, long totalMs,
            IReadOnlyList<KeyValuePair<string, long>> steps) =>
            WriteInfo("rdp_open", RdpOpenFields(rdpSession, outcome, firstControl, totalMs, steps));

        /// <summary>The fields of <see cref="RdpOpen"/> as they appear after the event name.</summary>
        internal static string FormatRdpOpen(string rdpSession, string outcome, bool firstControl, long totalMs,
            IReadOnlyList<KeyValuePair<string, long>> steps) =>
            string.Join(" ", RdpOpenFields(rdpSession, outcome, firstControl, totalMs, steps)
                .Select(field => $"{field.Key}={field.Value}"));

        private static FieldValue[] RdpOpenFields(string rdpSession, string outcome, bool firstControl, long totalMs,
            IReadOnlyList<KeyValuePair<string, long>> steps)
        {
            List<FieldValue> fields =
            [
                Field("rdp_session", SafeCorrelationId(rdpSession)),
                Field("outcome", SafeToken(outcome)),
                Boolean("first_control", firstControl),
                Number("total_ms", totalMs)
            ];
            string? slowest = null;
            long slowestMs = -1;
            foreach (KeyValuePair<string, long> step in steps)
            {
                fields.Add(Number(step.Key + "_ms", step.Value));
                if (step.Value > slowestMs)
                {
                    slowestMs = step.Value;
                    slowest = step.Key;
                }
            }
            if (slowest != null && slowestMs >= SlowOpenStepMs)
                fields.Add(Field("slow_step", slowest));
            return fields.ToArray();
        }

        /// <summary>
        /// One line per open started from a click in the connection tree: how long from the mouse
        /// message to <c>OpenConnection</c>, split into steps. Numbers and step names only.
        /// </summary>
        internal static void TreeClick(string protocol, int clicks, long totalMs,
            IReadOnlyList<KeyValuePair<string, long>> steps, long saveBackgroundMs) =>
            WriteInfo("tree_click", TreeClickFields(protocol, clicks, totalMs, steps, saveBackgroundMs));

        /// <summary>The fields of <see cref="TreeClick"/> as they appear after the event name.</summary>
        internal static string FormatTreeClick(string protocol, int clicks, long totalMs,
            IReadOnlyList<KeyValuePair<string, long>> steps, long saveBackgroundMs) =>
            string.Join(" ", TreeClickFields(protocol, clicks, totalMs, steps, saveBackgroundMs)
                .Select(field => $"{field.Key}={field.Value}"));

        private static FieldValue[] TreeClickFields(string protocol, int clicks, long totalMs,
            IReadOnlyList<KeyValuePair<string, long>> steps, long saveBackgroundMs)
        {
            List<FieldValue> fields =
            [
                Field("protocol", SafeToken(protocol)),
                Number("clicks", clicks),
                Number("total_ms", totalMs)
            ];
            string? slowest = null;
            long slowestMs = -1;
            foreach (KeyValuePair<string, long> step in steps)
            {
                fields.Add(Number(step.Key + "_ms", step.Value));
                if (step.Value > slowestMs)
                {
                    slowestMs = step.Value;
                    slowest = step.Key;
                }
            }
            fields.Add(Number("save_bg_ms", saveBackgroundMs));
            if (slowest != null && slowestMs >= SlowOpenStepMs)
                fields.Add(Field("slow_step", slowest));
            return fields.ToArray();
        }

        /// <summary>
        /// Link quality as the embedded RDP client measured it: quality level, estimated bandwidth
        /// and round-trip time, plus the range seen since the previous line. Numbers only.
        /// </summary>
        internal static void RdpNetwork(string rdpSession, Connection.Protocol.RDP.RdpNetworkSample sample) =>
            WriteInfo("rdp_network",
                Field("rdp_session", SafeCorrelationId(rdpSession)),
                Field("reason", SafeToken(sample.Reason)),
                Number("quality", sample.Quality),
                NullableNumber("previous_quality", sample.PreviousQuality),
                Number("bandwidth_kbps", sample.BandwidthKbps),
                Number("rtt_ms", sample.RttMs),
                Number("window_samples", sample.WindowSamples),
                Number("window_bw_min_kbps", sample.WindowBandwidthMinKbps),
                Number("window_bw_max_kbps", sample.WindowBandwidthMaxKbps),
                Number("window_rtt_min_ms", sample.WindowRttMinMs),
                Number("window_rtt_max_ms", sample.WindowRttMaxMs));

        /// <summary>
        /// The application's own measurement of the session's link, over the last probes: loss,
        /// round-trip range and median, jitter, and the probing method and interval. Numbers only;
        /// the probed address is never written.
        /// </summary>
        internal static void RdpLinkProbe(string rdpSession, Connection.Protocol.RDP.RdpLinkProbeSample sample,
            string method, int intervalS, bool lowPower) =>
            WriteInfo("rdp_link_probe",
                Field("rdp_session", SafeCorrelationId(rdpSession)),
                Field("method", SafeToken(method)),
                Field("reason", SafeToken(sample.Reason)),
                Number("samples", sample.Window.Samples),
                Number("lost", sample.Window.Lost),
                Number("loss_pct", sample.Window.LossPct),
                NullableNumber("rtt_min_ms", sample.Window.RttMinMs),
                NullableNumber("rtt_med_ms", sample.Window.RttMedianMs),
                NullableNumber("rtt_max_ms", sample.Window.RttMaxMs),
                NullableNumber("jitter_ms", sample.Window.JitterMs),
                Number("interval_s", intervalS),
                Boolean("low_power", lowPower));

        /// <summary>No established connection of this process matched the session's port, so nothing is probed.</summary>
        internal static void RdpLinkProbeNoTarget(string rdpSession, int candidates) =>
            WriteInfo("rdp_link_probe",
                Field("rdp_session", SafeCorrelationId(rdpSession)),
                Field("reason", "no_target"),
                Number("candidates", candidates));

        /// <summary>
        /// The whole connection's link record, written once when it ends: sample count, bandwidth
        /// and round-trip range and mean, seconds spent at each quality level, and the outages that
        /// automatic reconnection bridged.
        /// </summary>
        internal static void RdpNetworkSummary(string rdpSession, string point, Connection.Protocol.RDP.RdpNetworkSummary summary) =>
            WriteInfo("rdp_network_summary",
                Field("rdp_session", SafeCorrelationId(rdpSession)),
                Field("point", SafeToken(point)),
                Number("samples", summary.Samples),
                Number("bw_samples", summary.BandwidthSamples),
                Number("rtt_samples", summary.RttSamples),
                Number("bw_min_kbps", summary.BandwidthMinKbps),
                Number("bw_avg_kbps", summary.BandwidthAvgKbps),
                Number("bw_max_kbps", summary.BandwidthMaxKbps),
                Number("rtt_min_ms", summary.RttMinMs),
                Number("rtt_avg_ms", summary.RttAvgMs),
                Number("rtt_max_ms", summary.RttMaxMs),
                List("quality_seconds", summary.QualitySeconds),
                Number("reconnect_attempts", summary.ReconnectAttempts),
                Number("reconnects", summary.Reconnects),
                Number("outage_ms", summary.OutageMs),
                Boolean("outage_open", summary.OutageOpen),
                Number("probe_samples", summary.ProbeSamples),
                NullableNumber("probe_rtt_avg_ms", summary.ProbeRttAvgMs),
                NullableNumber("probe_loss_pct", summary.ProbeLossPct),
                NullableNumber("probe_jitter_ms", summary.ProbeJitterMs));

        /// <summary>
        /// One automatic-reconnection step: an attempt (with the numeric disconnect reason and the
        /// attempt number) or the moment the session came back (with how long it was gone).
        /// </summary>
        internal static void RdpReconnect(string rdpSession, string phase, int? reason, int? attempt, long? outageMs) =>
            WriteInfo("rdp_reconnect",
                Field("rdp_session", SafeCorrelationId(rdpSession)),
                Field("phase", SafeToken(phase)),
                NullableNumber("reason", reason),
                NullableNumber("attempt", attempt),
                NullableNumber("outage_ms", outageMs));

        /// <summary>
        /// What the link-stability monitor decided, with the thresholds it used, so a journey's log
        /// shows when a lighter configuration would have been offered and when the full one back.
        /// </summary>
        internal static void RdpAdapt(string rdpSession, Connection.Protocol.RDP.RdpAdaptDecision decision, bool autoDetect,
            string method = "control") =>
            WriteInfo("rdp_adapt",
                Field("rdp_session", SafeCorrelationId(rdpSession)),
                Field("method", SafeToken(method)),
                Field("decision", SafeToken(decision.Decision)),
                Field("reason", SafeToken(decision.Reason)),
                NullableNumber("rtt_median_ms", decision.RttMedianMs),
                Number("drops_10min", decision.DropsInWindow),
                Boolean("auto_detect", autoDetect),
                Number("unstable_rtt_ms", Connection.Protocol.RDP.RdpLinkStabilityMonitor.UnstableRttMs),
                Number("unstable_drops", Connection.Protocol.RDP.RdpLinkStabilityMonitor.UnstableDrops),
                Number("stable_rtt_ms", Connection.Protocol.RDP.RdpLinkStabilityMonitor.StableRttMs));

        /// <summary>
        /// What the adaptive-resolution controller decided. With the feature off the decision is
        /// still written, so a journey shows when the session would have been rendered smaller.
        /// </summary>
        internal static void RdpResolutionDecision(string rdpSession,
            Connection.Protocol.RDP.RdpAdaptiveResolutionDecision decision, uint baseScale, uint fromScale,
            uint toScale, bool enabled) =>
            WriteInfo("rdp_resolution",
                Field("rdp_session", SafeCorrelationId(rdpSession)),
                Field("phase", "decision"),
                Field("reason", SafeToken(decision.Reason)),
                Number("from_level", decision.FromLevel),
                Number("to_level", decision.ToLevel),
                Number("base_scale", baseScale),
                Number("from_scale", fromScale),
                Number("to_scale", toScale),
                NullableNumber("rtt_median_ms", decision.RttMedianMs),
                NullableNumber("rtt_max_ms", decision.RttMaxMs),
                Boolean("enabled", enabled));

        /// <summary>A remote scale the session was actually switched to, or the switch the control refused.</summary>
        internal static void RdpResolutionApplied(string rdpSession, string phase, uint baseScale, uint fromScale,
            uint toScale, int width, int height, int? hresult) =>
            WriteInfo("rdp_resolution",
                Field("rdp_session", SafeCorrelationId(rdpSession)),
                Field("phase", SafeToken(phase)),
                Number("base_scale", baseScale),
                Number("from_scale", fromScale),
                Number("to_scale", toScale),
                Number("width", width),
                Number("height", height),
                Field("hresult", hresult is int hr ? "0x" + unchecked((uint)hr).ToString("X8", CultureInfo.InvariantCulture) : "na"));

        internal static void RdpShape(RdpShapeInfo shape) =>
            WriteInfo("rdp_shape", [..ShapeFields(shape), ..ResourceFields(ProcessResourceSnapshot.Capture(refreshExpensive: true))]);

        internal static void RdpResources(string point, string session, bool refresh, string trigger,
            int? code, uint? extended, bool? wasConnected, bool loginComplete, bool? hostDisposedFirst,
            long sinceConnectMs, int desktopW, int desktopH, bool? smartSize, bool? fullScreen,
            string disconnect = "pending", int? disconnectHresult = null,
            string dispose = "pending", int? disposeHresult = null) =>
            WriteInfo("rdp_resources",
            [
                Field("point", point),
                Field("rdp_session", SafeCorrelationId(session)),
                Field("trigger", trigger),
                Field("disconnect", disconnect),
                Field("hresult", disconnectHresult is int hr ? "0x" + unchecked((uint)hr).ToString("X8", CultureInfo.InvariantCulture) : "na"),
                Field("dispose", dispose),
                Field("dispose_hresult", disposeHresult is int disposeHr ? "0x" + unchecked((uint)disposeHr).ToString("X8", CultureInfo.InvariantCulture) : "na"),
                NullableNumber("code", code),
                NullableNumber("extended_code", extended.HasValue ? (long)extended.Value : null),
                Field("disc_class", ClassifyDisconnect(code, extended)),
                Field("was_connected", wasConnected is null ? "na" : wasConnected.Value ? "true" : "false"),
                Boolean("login_complete", loginComplete),
                Field("host_disposed_first", hostDisposedFirst is null ? "na" : hostDisposedFirst.Value ? "true" : "false"),
                Number("since_connect_ms", sinceConnectMs),
                Number("desktop_w", desktopW),
                Number("desktop_h", desktopH),
                Field("smart_size", smartSize is null ? "na" : smartSize.Value ? "true" : "false"),
                Field("fullscreen", fullScreen is null ? "na" : fullScreen.Value ? "true" : "false"),
                Boolean("keep_tabs", mRemoteNG.Properties.OptionsTabsPanelsPage.Default.KeepTabsOpenAfterDisconnect),
                Boolean("reconnect_on_disconnect", mRemoteNG.Properties.OptionsAdvancedPage.Default.ReconnectOnDisconnect),
                Number("confirm_close", mRemoteNG.Properties.Settings.Default.ConfirmCloseConnection),
                ..ResourceFields(ProcessResourceSnapshot.Capture(refresh))
            ]);

        internal static string CompactResources(bool refresh)
        {
            ResourceSample sample = ProcessResourceSnapshot.Capture(refresh);
            return string.Create(CultureInfo.InvariantCulture,
                $"private_mb={sample.PrivateMb} working_set_mb={sample.WorkingSetMb} managed_mb={sample.ManagedMb} threads={sample.Threads} handles={sample.Handles} gdi={sample.Gdi} gdi_peak={sample.GdiPeak} user={sample.User} user_peak={sample.UserPeak}");
        }

        /// <summary>
        /// Names the disconnect without repeating the control's display string.
        /// Extended 12 is a user logoff and extended 2 is an API logoff; both make
        /// <c>GetErrorDescription</c> return "An internal error has occurred."
        /// The network classes follow the documented OnDisconnected reason table:
        /// 0x104 (260) DNS name lookup failure, 0x108 (264) connection timed out,
        /// 0x204 (516) WinSock socket connect failure, 0x904 (2308) socket closed.
        /// </summary>
        internal static string ClassifyDisconnect(int? code, uint? extended)
        {
            if (extended is 12) return "user_logoff";
            if (extended is 2) return "api_logoff";
            if (extended is 4) return "logoff";
            return code switch
            {
                null => "na",
                0 => "no_info",
                1 => "local_disconnect",
                2 => "remote_user",
                3 => "remote_server",
                0x104 => "dns",
                0x108 => "timeout",
                0x204 => "tcp_connect",
                0x904 => "socket_closed",
                0xB08 => "normal",
                _ => "other"
            };
        }

        private static FieldValue[] ResourceFields(ResourceSample sample) =>
        [
            Number("private_mb", sample.PrivateMb),
            Number("working_set_mb", sample.WorkingSetMb),
            Number("virtual_mb", sample.VirtualMb),
            Number("managed_mb", sample.ManagedMb),
            Number("gc_heap_mb", sample.GcHeapMb),
            Number("gc_committed_mb", sample.GcCommittedMb),
            Number("gc_fragmented_mb", sample.GcFragmentedMb),
            Number("gc0", sample.Gc0),
            Number("gc1", sample.Gc1),
            Number("gc2", sample.Gc2),
            Number("threads", sample.Threads),
            Number("threadpool", sample.ThreadPool),
            Number("handles", sample.Handles),
            Number("gdi", sample.Gdi),
            Number("gdi_peak", sample.GdiPeak),
            Number("user", sample.User),
            Number("user_peak", sample.UserPeak),
            Number("paged_pool_kb", sample.PagedPoolKb),
            Number("nonpaged_pool_kb", sample.NonPagedPoolKb),
            Number("page_faults", sample.PageFaults),
            Number("rdp_live", ProcessResourceSnapshot.LiveRdp),
            Number("forms", sample.Forms),
            Boolean("remote_session", sample.RemoteSession),
            Number("session_id", sample.SessionId),
            Number("monitors", sample.Monitors),
            Number("screen_w", sample.ScreenW),
            Number("screen_h", sample.ScreenH),
            Number("virtual_w", sample.VirtualW),
            Number("virtual_h", sample.VirtualH),
            Number("dpi", sample.Dpi),
            Number("dpi_context", sample.DpiContext),
            List("handle_types", sample.HandleTypes),
            List("thread_modules", sample.ThreadModules)
        ];

        private static FieldValue[] ShapeFields(RdpShapeInfo shape) =>
        [
            Field("rdp_session", SafeCorrelationId(shape.Session)),
            Field("protocol", shape.Protocol),
            Field("version", SafeVersion(shape.Version)),
            Field("client_version", SafeVersion(shape.ClientVersion)),
            Field("colors", shape.Colors),
            Number("color_depth", shape.ColorDepth),
            Field("resolution", shape.Resolution),
            Field("sizing", shape.Sizing),
            Number("desktop_w", shape.DesktopW),
            Number("desktop_h", shape.DesktopH),
            Number("panel_w", shape.PanelW),
            Number("panel_h", shape.PanelH),
            Boolean("smart_size", shape.SmartSize),
            Boolean("fullscreen", shape.FullScreen),
            Field("scale_requested", shape.ScaleRequested),
            Number("scale_applied", shape.ScaleApplied),
            Number("device_scale", shape.DeviceScale),
            Boolean("bitmap_cache", shape.BitmapCache),
            Boolean("clipboard", shape.Clipboard),
            Boolean("printers", shape.Printers),
            Boolean("ports", shape.Ports),
            Boolean("smart_cards", shape.SmartCards),
            Boolean("webauthn", shape.WebAuthn),
            Boolean("aad", shape.Aad),
            Boolean("credssp", shape.CredSsp),
            Boolean("console", shape.Console),
            Boolean("redirect_keys", shape.RedirectKeys),
            Boolean("view_only", shape.ViewOnly),
            Boolean("multimon", shape.Multimon),
            Boolean("ui_parent", shape.UiParent),
            Field("drives", shape.Drives),
            Boolean("drives_custom", shape.DrivesCustom),
            Field("sound", shape.Sound),
            Field("gateway_usage", shape.GatewayUsage),
            Boolean("gateway_set", shape.GatewaySet),
            Number("perf_flags", shape.PerfFlags),
            Boolean("wallpaper", shape.Wallpaper),
            Boolean("themes", shape.Themes),
            Boolean("font_smoothing", shape.FontSmoothing),
            Boolean("composition", shape.Composition),
            Boolean("full_window_drag", shape.FullWindowDrag),
            Boolean("menu_animations", shape.MenuAnimations),
            Boolean("cursor_shadow", shape.CursorShadow),
            Boolean("cursor_blink", shape.CursorBlink),
            Number("idle_minutes", shape.IdleMinutes),
            Number("server_auth", shape.ServerAuth),
            Number("port", shape.Port),
            Boolean("restricted_admin", shape.RestrictedAdmin),
            Boolean("credential_guard", shape.CredentialGuard),
            Boolean("load_balance_set", shape.LoadBalanceSet),
            Boolean("start_program_set", shape.StartProgramSet),
            Boolean("signature_set", shape.SignatureSet),
            Boolean("keep_tabs", shape.KeepTabs),
            Boolean("reconnect_on_disconnect", shape.ReconnectOnDisconnect),
            Number("confirm_close", shape.ConfirmClose),
            Number("reconnect_max", shape.ReconnectMax),
            Number("perf_flags_requested", shape.PerfFlagsRequested),
            Boolean("net_autodetect", shape.NetAutoDetect),
            Number("net_type", shape.NetworkConnectionType),
            Field("bw_detect", shape.BandwidthDetection),
            Number("compress", shape.Compress),
            Number("bitmap_persist", shape.BitmapPersistence),
            Number("keepalive_ms", shape.KeepAliveMs),
            Number("audio_quality", shape.AudioQualityMode),
            Boolean("audio_capture", shape.AudioCapture)
        ];

        internal static void RdpProbe(string version, bool supported, long durationMs, bool moduleLoadedBefore) =>
            WriteInfo("rdp_probe", Field("version", SafeToken(version)), Boolean("supported", supported),
                Number("duration_ms", durationMs), Boolean("module_loaded_before", moduleLoadedBefore));

        internal static void RdpCapability(string capability, bool supported) =>
            WriteInfo("rdp_capability", Field("name", SafeToken(capability)), Boolean("supported", supported));

        internal static void SafeException(string source, Exception exception, bool fatal = false)
        {
            // An AggregateException from TaskScheduler.UnobservedTaskException is a wrapper the
            // TPL never throws: its own StackTrace is null, its type and HResult are constants.
            // Logging the wrapper produced type=AggregateException hresult=0x80131500 frames=""
            // for every unobserved fault — one signature for every distinct bug (observed live:
            // 42 identical events in a two-week log). Classify by the first real inner exception.
            if (exception is AggregateException aggregate)
                exception = aggregate.Flatten().InnerException ?? exception;

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
                lock (StateLock)
                {
                    if (_heartbeatTimer == null)
                        return;
                    _heartbeatNextDueUtc = DateTime.UtcNow.AddMilliseconds(_heartbeatPeriodMs);
                }

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
                [
                    Number("uptime_ms", Uptime.ElapsedMilliseconds),
                    Decimal("cpu_percent", Math.Clamp(cpuPercent, 0d, 100d)),
                    ..ResourceFields(ProcessResourceSnapshot.Capture(refreshExpensive: true))
                ]);
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
                    WriteWarn("ui_stall", Field("state", "detected"), Number("lag_ms", lagMs),
                        Field("phase", _lastStartupPhase == "total" ? "running" : _lastStartupPhase));
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
        private static FieldValue List(string key, string value) => new(SafeToken(key), SafeList(value, 700));

        private static string SafeList(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return "none";
            string text = new(value.Where(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '-' or ',' or ':' or '+').Take(maxLength).ToArray());
            return text.Length == 0 ? "none" : text;
        }
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

    /// <summary>Applied RDP settings for one connect. Names, paths, and secrets stay out.</summary>
    internal sealed class RdpShapeInfo
    {
        public string Session = "";
        public string Protocol = "unknown";
        public string Version = "unknown";
        public string ClientVersion = "unknown";
        public string Colors = "unknown";
        public int ColorDepth;
        public string Resolution = "unknown";
        public string Sizing = "unknown";
        public int DesktopW;
        public int DesktopH;
        public int PanelW;
        public int PanelH;
        public bool SmartSize;
        public bool FullScreen;
        public string ScaleRequested = "unknown";
        public int ScaleApplied;
        public int DeviceScale;
        public bool BitmapCache;
        public bool Clipboard;
        public bool Printers;
        public bool Ports;
        public bool SmartCards;
        public bool WebAuthn;
        public bool Aad;
        public bool CredSsp;
        public bool Console;
        public bool RedirectKeys;
        public bool ViewOnly;
        public bool Multimon;
        public bool UiParent;
        public string Drives = "unknown";
        public bool DrivesCustom;
        public string Sound = "unknown";
        public string GatewayUsage = "unknown";
        public bool GatewaySet;
        public int PerfFlags;
        public bool Wallpaper;
        public bool Themes;
        public bool FontSmoothing;
        public bool Composition;
        public bool FullWindowDrag;
        public bool MenuAnimations;
        public bool CursorShadow;
        public bool CursorBlink;
        public int IdleMinutes;
        public int ServerAuth = -1;
        public int Port;
        public bool RestrictedAdmin;
        public bool CredentialGuard;
        public bool LoadBalanceSet;
        public bool StartProgramSet;
        public bool SignatureSet;
        public bool KeepTabs;
        public bool ReconnectOnDisconnect;
        public int ConfirmClose;
        public int ReconnectMax;
        public int PerfFlagsRequested = -1;
        public bool NetAutoDetect;
        public int NetworkConnectionType = -1;
        public string BandwidthDetection = "unknown";
        public int Compress = -1;
        public int BitmapPersistence = -1;
        public int KeepAliveMs = -1;
        public int AudioQualityMode = -1;
        public bool AudioCapture;
    }
}

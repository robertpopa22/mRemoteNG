using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace mRemoteNG.Connection.Protocol.RDP
{
    /// <summary>
    /// When to probe. Kept apart from the probe loop so the policy can be tested without a network.
    /// </summary>
    internal static class RdpLinkProbePolicy
    {
        internal const int NormalIntervalMs = 15_000;
        internal const int AfterDropIntervalMs = 5_000;
        internal const long AfterDropWindowMs = 120_000;
        // On battery the radio and CPU wake-ups cost more than the extra resolution is worth; one
        // probe a minute still shows a dead or lossy link within a few lines.
        internal const int LowPowerIntervalMs = 60_000;
        internal const int MaxTimeoutMs = 2_000;
        internal const int TcpFallbackAfterLosses = 3;
        internal const long TargetWaitMs = 10_000;
        internal const int TargetPollMs = 1_000;

        /// <summary>
        /// The delay before the next probe, or <c>null</c> while probing is paused: the window is
        /// minimized, or automatic reconnection was stopped after a resume (#212) and the user has
        /// not decided yet.
        /// </summary>
        internal static int? IntervalMs(long nowMs, long? lastDropMs, bool minimized, bool onBattery, bool reconnectStopped, bool tcpMethod = false)
        {
            if (minimized || reconnectStopped)
                return null;
            if (onBattery)
                return LowPowerIntervalMs;
            // A TCP probe is a half-open connection on the server's RDP (or gateway) port; brute-force
            // guards such as RdpGuard count those. One a minute stays under every such threshold.
            if (tcpMethod)
                return LowPowerIntervalMs;
            if (lastDropMs is long drop && nowMs - drop < AfterDropWindowMs)
                return AfterDropIntervalMs;
            return NormalIntervalMs;
        }

        internal static int TimeoutMs(int intervalMs) => Math.Min(intervalMs, MaxTimeoutMs);

        /// <summary>
        /// What one ICMP probe result means for the session. Many hosts and networks drop ICMP, so
        /// until the host has answered once a lost ping says nothing about the link: it is held back
        /// rather than logged as loss, and three of them while the session is alive mean ICMP is
        /// filtered, so the session switches to timing a TCP connect to the same endpoint, which the
        /// RDP path is known to pass. Once ICMP has answered, the session stays on ICMP: a lost ping
        /// on a lossy link is exactly the loss sample the probe exists to record, and switching to
        /// TCP would hide it and drop the cadence to one probe a minute.
        /// </summary>
        internal static RdpIcmpStep AfterIcmp(bool replied, bool answeredBefore, int lossRun, bool alive)
        {
            if (replied)
                return new RdpIcmpStep(Report: true, SwitchToTcp: false, Answered: true, LossRun: 0);
            if (answeredBefore)
                return new RdpIcmpStep(Report: true, SwitchToTcp: false, Answered: true, LossRun: lossRun + 1);
            int run = alive ? lossRun + 1 : lossRun;
            return new RdpIcmpStep(Report: false, SwitchToTcp: alive && run >= TcpFallbackAfterLosses, Answered: false, LossRun: run);
        }

        /// <summary>
        /// Picks the endpoint the control is connected to from the process's established TCP
        /// connections. Returns <c>null</c> unless exactly one remote endpoint fits; when several do
        /// (two tabs on the same port), endpoints another session already probes are set aside.
        /// </summary>
        internal static IPEndPoint? SelectTarget(IEnumerable<IPEndPoint> remotes, IReadOnlyCollection<int> ports,
            IReadOnlyCollection<IPEndPoint> claimedByOthers, out int candidates)
        {
            List<IPEndPoint> matching = remotes.Where(r => ports.Contains(r.Port)).Distinct().ToList();
            if (matching.Count > 1)
                matching = matching.Where(r => !claimedByOthers.Contains(r)).ToList();
            candidates = matching.Count;
            return matching.Count == 1 ? matching[0] : null;
        }
    }

    internal readonly record struct RdpIcmpStep(bool Report, bool SwitchToTcp, bool Answered, int LossRun);

    internal readonly record struct RdpProbeState(bool Minimized, bool OnBattery, bool ReconnectStopped);

    internal sealed record RdpProbeResult(long AtMs, int? RttMs, string Method, int IntervalMs, bool LowPower);

    /// <summary>
    /// The application's own measurement of the link of one RDP session. The control reports the
    /// link only through <c>OnNetworkStatusChanged</c>, which is event-driven and, with the Modem
    /// connection type, can stay silent for a whole day; on a travel link that leaves no samples.
    ///
    /// The probe measures only the endpoint the control is already connected to, read from the
    /// process's own TCP table. It never resolves a name: a lookup would leak the host name to
    /// whatever resolver the network hands out and would measure the resolver as much as the link.
    /// Neither the address nor the host is logged.
    /// </summary>
    internal sealed class RdpLinkProbe : IDisposable
    {
        private static readonly ConcurrentDictionary<RdpLinkProbe, IPEndPoint> Claimed = new();

        private readonly Func<RdpProbeState> _state;
        private readonly Action<RdpProbeResult> _onResult;
        private readonly Action<int> _onNoTarget;
        private readonly Lock _sync = new();
        private CancellationTokenSource? _cts;
        private volatile bool _alive;
        private long _lastDropMs = long.MinValue;

        internal RdpLinkProbe(Func<RdpProbeState> state, Action<RdpProbeResult> onResult, Action<int> onNoTarget)
        {
            _state = state;
            _onResult = onResult;
            _onNoTarget = onNoTarget;
        }

        /// <summary>The session logged in. Starts the probe loop unless it is already running.</summary>
        internal void Start(IReadOnlyCollection<int> ports)
        {
            lock (_sync)
            {
                _alive = true;
                if (_cts != null)
                    return;
                var cts = new CancellationTokenSource();
                _cts = cts;
                _ = Task.Run(() => RunAsync(ports, cts));
            }
        }

        /// <summary>The link dropped: probe faster for a while, and stop if the session never returns.</summary>
        internal void MarkDrop(long nowMs)
        {
            _alive = false;
            Interlocked.Exchange(ref _lastDropMs, nowMs);
        }

        internal void Reconnected() => _alive = true;

        internal void Stop()
        {
            CancellationTokenSource? cts;
            lock (_sync)
            {
                cts = _cts;
                _cts = null;
            }
            if (cts == null)
                return;
            try { cts.Cancel(); }
            catch (ObjectDisposedException) { }
        }

        public void Dispose() => Stop();

        private long? LastDrop
        {
            get
            {
                long value = Interlocked.Read(ref _lastDropMs);
                return value == long.MinValue ? null : value;
            }
        }

        private async Task RunAsync(IReadOnlyCollection<int> ports, CancellationTokenSource cts)
        {
            CancellationToken ct = cts.Token;
            try
            {
                IPEndPoint? target = await FindTargetAsync(ports, ct).ConfigureAwait(false);
                if (target == null)
                    return;
                Claimed[this] = target;
                await ProbeLoopAsync(target, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                App.RuntimeDiagnostics.SafeException("rdp_link_probe", ex);
            }
            finally
            {
                Claimed.TryRemove(this, out _);
                lock (_sync)
                {
                    if (ReferenceEquals(_cts, cts))
                        _cts = null;
                }
                cts.Dispose();
            }
        }

        private async Task<IPEndPoint?> FindTargetAsync(IReadOnlyCollection<int> ports, CancellationToken ct)
        {
            int pid = Environment.ProcessId;
            int candidates = 0;
            long start = Environment.TickCount64;
            while (true)
            {
                IPEndPoint[] others = Claimed.Where(c => !ReferenceEquals(c.Key, this)).Select(c => c.Value).ToArray();
                IPEndPoint? target = RdpLinkProbePolicy.SelectTarget(
                    TcpConnectionTable.EstablishedRemotes(pid), ports, others, out candidates);
                if (target != null)
                    return target;
                if (Environment.TickCount64 - start >= RdpLinkProbePolicy.TargetWaitMs)
                    break;
                await Task.Delay(RdpLinkProbePolicy.TargetPollMs, ct).ConfigureAwait(false);
            }
            _onNoTarget(candidates);
            return null;
        }

        private async Task ProbeLoopAsync(IPEndPoint target, CancellationToken ct)
        {
            using var ping = new Ping();
            string method = "icmp";
            bool icmpAnswered = false;
            int icmpLossRun = 0;

            while (!ct.IsCancellationRequested)
            {
                long now = Environment.TickCount64;
                long? lastDrop = LastDrop;
                if (!_alive && lastDrop is long drop && now - drop > RdpLinkProbePolicy.AfterDropWindowMs)
                    return; // the session never came back; a disconnected tab is not probed forever

                RdpProbeState state = _state();
                int? interval = RdpLinkProbePolicy.IntervalMs(now, lastDrop, state.Minimized, state.OnBattery, state.ReconnectStopped, method == "tcp");
                if (interval is not int intervalMs)
                {
                    await Task.Delay(RdpLinkProbePolicy.NormalIntervalMs, ct).ConfigureAwait(false);
                    continue;
                }

                int timeout = RdpLinkProbePolicy.TimeoutMs(intervalMs);
                long started = Stopwatch.GetTimestamp();
                string used = method;
                int? rtt = method == "icmp"
                    ? await PingAsync(ping, target.Address, timeout).ConfigureAwait(false)
                    : await TcpConnectAsync(target, timeout, ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                bool report = true;
                if (used == "icmp")
                {
                    RdpIcmpStep step = RdpLinkProbePolicy.AfterIcmp(rtt != null, icmpAnswered, icmpLossRun, _alive);
                    icmpAnswered = step.Answered;
                    icmpLossRun = step.LossRun;
                    report = step.Report;
                    if (step.SwitchToTcp)
                        method = "tcp";
                }

                if (report)
                    _onResult(new RdpProbeResult(Environment.TickCount64, rtt, used, intervalMs, state.OnBattery));

                long elapsedMs = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                long wait = Math.Max(0, intervalMs - elapsedMs);
                if (wait > 0)
                    await Task.Delay(TimeSpan.FromMilliseconds(wait), ct).ConfigureAwait(false);
            }
        }

        private static async Task<int?> PingAsync(Ping ping, IPAddress address, int timeoutMs)
        {
            try
            {
                PingReply reply = await ping.SendPingAsync(address, timeoutMs).ConfigureAwait(false);
                return reply.Status == IPStatus.Success ? (int)Math.Min(int.MaxValue, reply.RoundtripTime) : null;
            }
            catch (PingException)
            {
                return null;
            }
        }

        private static async Task<int?> TcpConnectAsync(IPEndPoint target, int timeoutMs, CancellationToken ct)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(timeoutMs);
            using var client = new TcpClient(target.AddressFamily);
            long started = Stopwatch.GetTimestamp();
            try
            {
                await client.ConnectAsync(target.Address, target.Port, timeout.Token).ConfigureAwait(false);
                return (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return null;
            }
            catch (SocketException)
            {
                return null;
            }
        }
    }

    /// <summary>Remote endpoints of this process's established TCP connections, from the IP helper table.</summary>
    internal static class TcpConnectionTable
    {
        private const int AfInet = 2;
        private const int AfInet6 = 23;
        private const int TcpTableOwnerPidConnections = 4;
        private const uint ErrorInsufficientBuffer = 122;
        private const int StateEstablished = 5;

        [DllImport("iphlpapi.dll")]
        private static extern uint GetExtendedTcpTable(IntPtr table, ref int size, bool order, int addressFamily, int tableClass, uint reserved);

        internal static List<IPEndPoint> EstablishedRemotes(int pid)
        {
            var remotes = new List<IPEndPoint>();
            Read(AfInet, pid, remotes);
            Read(AfInet6, pid, remotes);
            return remotes;
        }

        private static void Read(int family, int pid, List<IPEndPoint> into)
        {
            int size = 0;
            uint rc = GetExtendedTcpTable(IntPtr.Zero, ref size, false, family, TcpTableOwnerPidConnections, 0);
            for (int attempt = 0; attempt < 3 && rc == ErrorInsufficientBuffer; attempt++)
            {
                IntPtr buffer = Marshal.AllocHGlobal(size);
                try
                {
                    rc = GetExtendedTcpTable(buffer, ref size, false, family, TcpTableOwnerPidConnections, 0);
                    if (rc == 0)
                        Parse(buffer, family, pid, into);
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }

        private static void Parse(IntPtr buffer, int family, int pid, List<IPEndPoint> into)
        {
            int count = Marshal.ReadInt32(buffer);
            if (family == AfInet)
            {
                // MIB_TCPROW_OWNER_PID: state, local addr, local port, remote addr, remote port, pid
                const int rowSize = 24;
                for (int i = 0; i < count; i++)
                {
                    IntPtr row = buffer + 4 + i * rowSize;
                    if (Marshal.ReadInt32(row, 0) != StateEstablished || Marshal.ReadInt32(row, 20) != pid)
                        continue;
                    long address = (uint)Marshal.ReadInt32(row, 12);
                    into.Add(new IPEndPoint(new IPAddress(address), NetworkPort(Marshal.ReadInt32(row, 16))));
                }
            }
            else
            {
                // MIB_TCP6ROW_OWNER_PID: local addr[16], scope, port, remote addr[16], scope, port, state, pid
                const int rowSize = 56;
                for (int i = 0; i < count; i++)
                {
                    IntPtr row = buffer + 4 + i * rowSize;
                    if (Marshal.ReadInt32(row, 48) != StateEstablished || Marshal.ReadInt32(row, 52) != pid)
                        continue;
                    byte[] address = new byte[16];
                    Marshal.Copy(row + 24, address, 0, 16);
                    long scope = (uint)Marshal.ReadInt32(row, 40);
                    into.Add(new IPEndPoint(new IPAddress(address, scope), NetworkPort(Marshal.ReadInt32(row, 44))));
                }
            }
        }

        private static int NetworkPort(int raw) => ((raw & 0xFF) << 8) | ((raw >> 8) & 0xFF);
    }
}

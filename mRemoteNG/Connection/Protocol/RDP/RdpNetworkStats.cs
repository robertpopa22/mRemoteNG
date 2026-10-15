using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace mRemoteNG.Connection.Protocol.RDP
{
    /// <summary>
    /// What the embedded RDP client reported about the link during one connection: the quality
    /// level, estimated bandwidth and round-trip time from <c>OnNetworkStatusChanged</c>, and the
    /// outages bridged by automatic reconnection.
    ///
    /// The client can raise the network event many times a minute, so a line is written only on
    /// the first sample, on a change of quality level, or once per <see cref="ThrottleMs"/>; the
    /// minimum and maximum seen since the previous line travel with it, so nothing between two
    /// lines is lost. A summary for the whole connection is written when it ends. Only numbers are
    /// kept: no host, no user, no address.
    /// </summary>
    internal sealed class RdpNetworkStats
    {
        internal const long ThrottleMs = 30_000;
        internal const int ProbeWindowSize = 20;
        internal const long ProbeWindowMs = 300_000;

        private readonly System.Threading.Lock _sync = new();
        private readonly Dictionary<uint, long> _msPerQuality = new();

        private long _samples;
        private long _bandwidthSum;
        private long _rttSum;
        private int _bandwidthMin = int.MaxValue;
        private int _bandwidthMax;
        private int _rttMin = int.MaxValue;
        private int _rttMax;

        private uint? _quality;
        private long _qualitySinceMs;
        private long _lastEmitMs = long.MinValue;

        private int _windowSamples;
        private int _windowBandwidthMin = int.MaxValue;
        private int _windowBandwidthMax;
        private int _windowRttMin = int.MaxValue;
        private int _windowRttMax;

        private int _reconnects;
        private int _attempts;
        private long _outageMs;
        private long? _outageStartMs;

        private readonly Queue<(long At, int? RttMs)> _probeWindow = new();
        private long _probeSamples;
        private long _probeLost;
        private long _probeRttSum;
        private long _probeRttCount;
        private long _probeJitterSum;
        private long _probeJitterCount;
        private int? _probeLastRtt;
        private long _probeLastEmitMs = long.MinValue;
        private int _probeLossBand;

        /// <summary>
        /// Records one network sample. Returns a line to write, or <c>null</c> when the sample is
        /// folded into the next line.
        /// </summary>
        internal RdpNetworkSample? Record(long nowMs, uint quality, int bandwidthKbps, int rttMs)
        {
            lock (_sync)
            {
                bandwidthKbps = Math.Max(0, bandwidthKbps);
                rttMs = Math.Max(0, rttMs);

                _samples++;
                _bandwidthSum += bandwidthKbps;
                _rttSum += rttMs;
                _bandwidthMin = Math.Min(_bandwidthMin, bandwidthKbps);
                _bandwidthMax = Math.Max(_bandwidthMax, bandwidthKbps);
                _rttMin = Math.Min(_rttMin, rttMs);
                _rttMax = Math.Max(_rttMax, rttMs);

                _windowSamples++;
                _windowBandwidthMin = Math.Min(_windowBandwidthMin, bandwidthKbps);
                _windowBandwidthMax = Math.Max(_windowBandwidthMax, bandwidthKbps);
                _windowRttMin = Math.Min(_windowRttMin, rttMs);
                _windowRttMax = Math.Max(_windowRttMax, rttMs);

                bool qualityChanged = _quality != quality;
                uint? previousQuality = _quality;
                if (qualityChanged)
                {
                    CloseQualitySpan(nowMs);
                    _quality = quality;
                    _qualitySinceMs = nowMs;
                }

                bool first = _lastEmitMs == long.MinValue;
                if (!first && !qualityChanged && nowMs - _lastEmitMs < ThrottleMs)
                    return null;

                var sample = new RdpNetworkSample(
                    quality, previousQuality, bandwidthKbps, rttMs, _windowSamples,
                    _windowBandwidthMin, _windowBandwidthMax, _windowRttMin, _windowRttMax,
                    first ? "first" : qualityChanged ? "quality_change" : "interval");

                _lastEmitMs = nowMs;
                _windowSamples = 0;
                _windowBandwidthMin = int.MaxValue;
                _windowBandwidthMax = 0;
                _windowRttMin = int.MaxValue;
                _windowRttMax = 0;
                return sample;
            }
        }

        /// <summary>
        /// Records one result of the application's own link probe; <paramref name="rttMs"/> is
        /// <c>null</c> for a lost probe. Returns a line to write on the first probe, when the loss
        /// band of the window changes (none, under half, half or more), or once per
        /// <see cref="ThrottleMs"/>; otherwise <c>null</c>. The window is the last
        /// <see cref="ProbeWindowSize"/> probes, no older than <see cref="ProbeWindowMs"/>.
        /// </summary>
        internal RdpLinkProbeSample? RecordProbe(long nowMs, int? rttMs)
        {
            lock (_sync)
            {
                if (rttMs is int value)
                {
                    value = Math.Max(0, value);
                    rttMs = value;
                    _probeRttSum += value;
                    _probeRttCount++;
                    if (_probeLastRtt is int previous)
                    {
                        _probeJitterSum += Math.Abs(value - previous);
                        _probeJitterCount++;
                    }
                    _probeLastRtt = value;
                }
                else
                {
                    _probeLost++;
                }
                _probeSamples++;

                _probeWindow.Enqueue((nowMs, rttMs));
                while (_probeWindow.Count > ProbeWindowSize)
                    _probeWindow.Dequeue();
                while (_probeWindow.Count > 1 && nowMs - _probeWindow.Peek().At > ProbeWindowMs)
                    _probeWindow.Dequeue();

                RdpProbeWindow window = ComputeProbeWindow(_probeWindow.Select(p => p.RttMs));
                int band = window.LossPct == 0 ? 0 : window.LossPct < 50 ? 1 : 2;
                bool first = _probeLastEmitMs == long.MinValue;
                bool bandChanged = !first && band != _probeLossBand;
                _probeLossBand = band;
                if (!first && !bandChanged && nowMs - _probeLastEmitMs < ThrottleMs)
                    return null;

                _probeLastEmitMs = nowMs;
                return new RdpLinkProbeSample(first ? "first" : bandChanged ? "loss_change" : "interval", window);
            }
        }

        /// <summary>
        /// Loss, round-trip range and median, and jitter (the mean absolute difference between
        /// consecutive answered probes) over a run of probe results, oldest first.
        /// </summary>
        internal static RdpProbeWindow ComputeProbeWindow(IEnumerable<int?> results)
        {
            int samples = 0, lost = 0;
            var rtts = new List<int>();
            foreach (int? result in results)
            {
                samples++;
                if (result is int rtt) rtts.Add(rtt);
                else lost++;
            }

            int lossPct = samples == 0 ? 0 : (int)Math.Round(lost * 100.0 / samples, MidpointRounding.AwayFromZero);
            if (rtts.Count == 0)
                return new RdpProbeWindow(samples, lost, lossPct, null, null, null, null);

            int? jitter = null;
            if (rtts.Count > 1)
            {
                long diffs = 0;
                for (int i = 1; i < rtts.Count; i++)
                    diffs += Math.Abs(rtts[i] - rtts[i - 1]);
                jitter = (int)Math.Round((double)diffs / (rtts.Count - 1), MidpointRounding.AwayFromZero);
            }

            int[] sorted = rtts.OrderBy(r => r).ToArray();
            return new RdpProbeWindow(samples, lost, lossPct, sorted[0], sorted[sorted.Length / 2], sorted[^1], jitter);
        }

        /// <summary>
        /// The client is trying to reconnect. The outage starts at the first attempt and lasts
        /// until <see cref="Reconnected"/>.
        /// </summary>
        internal void ReconnectAttempt(long nowMs)
        {
            lock (_sync)
            {
                _attempts++;
                _outageStartMs ??= nowMs;
            }
        }

        /// <summary>The session came back. Returns how long it was gone, or 0 when unknown.</summary>
        internal long Reconnected(long nowMs)
        {
            lock (_sync)
            {
                _reconnects++;
                if (_outageStartMs is not long start)
                    return 0;

                long outage = Math.Max(0, nowMs - start);
                _outageMs += outage;
                _outageStartMs = null;
                return outage;
            }
        }

        /// <summary>The whole connection so far. Returns <c>null</c> when nothing was ever recorded.</summary>
        internal RdpNetworkSummary? Summary(long nowMs)
        {
            lock (_sync)
            {
                if (_samples == 0 && _attempts == 0 && _probeSamples == 0)
                    return null;

                var perQuality = new Dictionary<uint, long>(_msPerQuality);
                if (_quality is uint current)
                {
                    perQuality.TryGetValue(current, out long sofar);
                    perQuality[current] = sofar + Math.Max(0, nowMs - _qualitySinceMs);
                }

                long openOutage = _outageStartMs is long start ? Math.Max(0, nowMs - start) : 0;

                return new RdpNetworkSummary(
                    _samples,
                    _samples == 0 ? 0 : _bandwidthMin,
                    _samples == 0 ? 0 : (int)(_bandwidthSum / _samples),
                    _bandwidthMax,
                    _samples == 0 ? 0 : _rttMin,
                    _samples == 0 ? 0 : (int)(_rttSum / _samples),
                    _rttMax,
                    FormatQualityTime(perQuality),
                    _attempts,
                    _reconnects,
                    _outageMs + openOutage,
                    _outageStartMs.HasValue,
                    _probeSamples,
                    _probeRttCount == 0 ? null : (int)(_probeRttSum / _probeRttCount),
                    _probeSamples == 0 ? null : (int)Math.Round(_probeLost * 100.0 / _probeSamples, MidpointRounding.AwayFromZero),
                    _probeJitterCount == 0 ? null : (int)Math.Round((double)_probeJitterSum / _probeJitterCount, MidpointRounding.AwayFromZero));
            }
        }

        private void CloseQualitySpan(long nowMs)
        {
            if (_quality is not uint previous)
                return;

            _msPerQuality.TryGetValue(previous, out long sofar);
            _msPerQuality[previous] = sofar + Math.Max(0, nowMs - _qualitySinceMs);
        }

        /// <summary>"q:seconds" pairs, lowest quality level first, e.g. <c>4:120,6:3600</c>.</summary>
        internal static string FormatQualityTime(IReadOnlyDictionary<uint, long> msPerQuality) =>
            msPerQuality.Count == 0
                ? "none"
                : string.Join(",", msPerQuality.OrderBy(p => p.Key)
                    .Select(p => p.Key.ToString(CultureInfo.InvariantCulture) + ":" +
                                 (p.Value / 1000).ToString(CultureInfo.InvariantCulture)));
    }

    internal sealed record RdpNetworkSample(
        uint Quality,
        uint? PreviousQuality,
        int BandwidthKbps,
        int RttMs,
        int WindowSamples,
        int WindowBandwidthMinKbps,
        int WindowBandwidthMaxKbps,
        int WindowRttMinMs,
        int WindowRttMaxMs,
        string Reason);

    internal sealed record RdpNetworkSummary(
        long Samples,
        int BandwidthMinKbps,
        int BandwidthAvgKbps,
        int BandwidthMaxKbps,
        int RttMinMs,
        int RttAvgMs,
        int RttMaxMs,
        string QualitySeconds,
        int ReconnectAttempts,
        int Reconnects,
        long OutageMs,
        bool OutageOpen,
        long ProbeSamples = 0,
        int? ProbeRttAvgMs = null,
        int? ProbeLossPct = null,
        int? ProbeJitterMs = null);

    internal sealed record RdpProbeWindow(
        int Samples,
        int Lost,
        int LossPct,
        int? RttMinMs,
        int? RttMedianMs,
        int? RttMaxMs,
        int? JitterMs);

    internal sealed record RdpLinkProbeSample(string Reason, RdpProbeWindow Window);
}

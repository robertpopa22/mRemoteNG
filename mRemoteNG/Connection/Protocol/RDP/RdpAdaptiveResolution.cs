using System;
using System.Collections.Generic;
using System.Linq;

namespace mRemoteNG.Connection.Protocol.RDP
{
    /// <summary>
    /// Chooses the share of the panel an RDP session is rendered at, the way adaptive video
    /// streaming chooses a bitrate: step down fast when the link degrades, step back up slowly once
    /// it has been calm, and never switch more often than a remote-desktop re-layout is worth.
    /// <para>
    /// Field data (relayed Tailscale path, motorway with tunnels, 2026-10-20): median round trips
    /// stayed at 106-148 ms and almost no probe was lost, but tunnels showed as round-trip peaks of
    /// 0.5-1.6 s, because a relayed path retransmits instead of losing. Peaks are therefore a
    /// signal of their own here, next to the median, losses and drops.
    /// </para>
    /// Pure and thread-safe: it decides a level, the protocol maps the level to a remote desktop
    /// scale and a session size (<see cref="RemoteScaleLadder"/>).
    /// </summary>
    internal sealed class RdpAdaptiveResolution
    {
        /// <summary>
        /// The desktop scales RDP accepts. Windows offers 225, 350 and 450 as display steps too, but
        /// they are not legal session values.
        /// </summary>
        internal static readonly IReadOnlyList<uint> StandardScales = [100, 125, 150, 175, 200, 250, 300, 400, 500];

        internal const int MaxSteps = 2;

        internal const long WindowMs = 120_000;
        internal const int SpikeRttMs = 500;
        internal const int SpikesToDegrade = 2;
        internal const long SparseGapMs = 45_000;
        internal const int LossesToDegrade = 2;
        internal const int SlowMedianMs = 250;
        internal const int MinMedianSamples = 4;
        internal const long MinMedianSpanMs = 60_000;
        internal const long DownDwellMs = 60_000;
        internal const long CalmForMs = 300_000;
        internal const int CalmPeakMs = SpikeRttMs;
        internal const long UpDwellMs = 300_000;

        private readonly System.Threading.Lock _sync = new();
        private readonly Queue<(long At, int RttMs)> _rtt = new();
        private readonly Queue<long> _losses = new();
        private readonly Queue<long> _drops = new();
        private long _lastSwitchMs = long.MinValue;
        private long _lastBadMs = long.MinValue;
        private int _level;
        private readonly int _levels;

        /// <param name="levels">How many levels the ladder has, the full panel included.</param>
        internal RdpAdaptiveResolution(int levels)
        {
            _levels = Math.Max(1, levels);
        }

        /// <summary>0 is the full panel; each level above it renders the session smaller.</summary>
        internal int Level
        {
            get { lock (_sync) return _level; }
        }

        internal int Levels => _levels;

        /// <summary>
        /// The remote desktop scales a session can step through while keeping what the user sees the
        /// same size: rendering at <c>remote / base</c> of the panel and stretching back up with
        /// SmartSizing cancels out exactly when the remote scale drops by the same ratio. Only
        /// standard scales are used, and none below 100, so a panel at 100 % has no lower level.
        /// </summary>
        internal static IReadOnlyList<uint> RemoteScaleLadder(uint baseScale)
        {
            var ladder = new List<uint>(MaxSteps + 1) { baseScale };
            foreach (uint s in StandardScales.Reverse())
            {
                if (ladder.Count > MaxSteps) break;
                if (s < baseScale) ladder.Add(s);
            }
            return ladder;
        }

        internal RdpAdaptiveResolutionDecision? Sample(long nowMs, int rttMs)
        {
            lock (_sync)
            {
                _rtt.Enqueue((nowMs, Math.Max(0, rttMs)));
                if (rttMs >= CalmPeakMs)
                    _lastBadMs = nowMs;
                return Evaluate(nowMs);
            }
        }

        internal RdpAdaptiveResolutionDecision? Loss(long nowMs)
        {
            lock (_sync)
            {
                _losses.Enqueue(nowMs);
                _lastBadMs = nowMs;
                return Evaluate(nowMs);
            }
        }

        internal RdpAdaptiveResolutionDecision? Drop(long nowMs)
        {
            lock (_sync)
            {
                _drops.Enqueue(nowMs);
                _lastBadMs = nowMs;
                return Evaluate(nowMs);
            }
        }

        /// <summary>A fresh connection starts at the full panel with no history.</summary>
        internal void Reset()
        {
            lock (_sync)
            {
                _rtt.Clear();
                _losses.Clear();
                _drops.Clear();
                _lastSwitchMs = long.MinValue;
                _lastBadMs = long.MinValue;
                _level = 0;
            }
        }

        private RdpAdaptiveResolutionDecision? Evaluate(long nowMs)
        {
            Trim(nowMs);
            string? degraded = DegradedReason();
            bool dwellOk(long dwell) => _lastSwitchMs == long.MinValue || nowMs - _lastSwitchMs >= dwell;

            if (degraded != null)
            {
                if (_level < _levels - 1 && dwellOk(DownDwellMs))
                    return Switch(nowMs, _level + 1, degraded);
                return null;
            }

            if (_level > 0 && dwellOk(UpDwellMs) &&
                (_lastBadMs == long.MinValue || nowMs - _lastBadMs >= CalmForMs))
                return Switch(nowMs, _level - 1, "calm");

            return null;
        }

        private RdpAdaptiveResolutionDecision Switch(long nowMs, int to, string reason)
        {
            int from = _level;
            _level = to;
            _lastSwitchMs = nowMs;
            return new RdpAdaptiveResolutionDecision(from, to, reason, Median(), MaxRtt());
        }

        private void Trim(long nowMs)
        {
            while (_rtt.Count > 0 && nowMs - _rtt.Peek().At > WindowMs) _rtt.Dequeue();
            while (_losses.Count > 0 && nowMs - _losses.Peek() > WindowMs) _losses.Dequeue();
            while (_drops.Count > 0 && nowMs - _drops.Peek() > WindowMs) _drops.Dequeue();
        }

        /// <summary>
        /// Only what was measured after the last switch counts: the peaks that caused a step must
        /// not cause the next one once the dwell has passed.
        /// </summary>
        private bool SinceSwitch(long atMs) => _lastSwitchMs == long.MinValue || atMs > _lastSwitchMs;

        private string? DegradedReason()
        {
            var parts = new List<string>(4);
            bool clustered = _rtt.Count(r => SinceSwitch(r.At) && r.RttMs >= SpikeRttMs) >= SpikesToDegrade;
            if (clustered || SparseSpike()) parts.Add("peaks");
            if (_losses.Count(SinceSwitch) >= LossesToDegrade) parts.Add("loss");
            if (_drops.Any(SinceSwitch)) parts.Add("drop");
            if (MedianOverSpan() is int m && m >= SlowMedianMs) parts.Add("rtt");
            return parts.Count == 0 ? null : string.Join("_and_", parts);
        }

        /// <summary>
        /// At the 60 s battery cadence one tunnel touches one probe, so one peak there counts. At the
        /// 15 s cadence a single peak still waits for a second one.
        /// </summary>
        private bool SparseSpike()
        {
            (long At, int RttMs)[] all = _rtt.ToArray();
            for (int i = 1; i < all.Length; i++)
            {
                if (SinceSwitch(all[i].At) && all[i].RttMs >= SpikeRttMs && all[i].At - all[i - 1].At >= SparseGapMs)
                    return true;
            }
            return false;
        }

        private int? MedianOverSpan()
        {
            var recent = _rtt.Where(r => SinceSwitch(r.At)).ToArray();
            if (recent.Length < MinMedianSamples) return null;
            if (recent[^1].At - recent[0].At < MinMedianSpanMs) return null;
            int[] sorted = recent.Select(r => r.RttMs).OrderBy(r => r).ToArray();
            return sorted[sorted.Length / 2];
        }

        private int? Median()
        {
            if (_rtt.Count == 0) return null;
            int[] sorted = _rtt.Select(r => r.RttMs).OrderBy(r => r).ToArray();
            return sorted[sorted.Length / 2];
        }

        private int? MaxRtt() => _rtt.Count == 0 ? null : _rtt.Max(r => r.RttMs);
    }

    internal sealed record RdpAdaptiveResolutionDecision(int FromLevel, int ToLevel, string Reason, int? RttMedianMs, int? RttMaxMs);
}

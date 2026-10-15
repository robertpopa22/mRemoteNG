using System;
using System.Collections.Generic;
using System.Linq;

namespace mRemoteNG.Connection.Protocol.RDP
{
    /// <summary>
    /// Decides when a session's link has become unstable enough that the user would be offered a
    /// lighter, travel-friendly configuration, and when it has been calm long enough to offer the
    /// full one back.
    ///
    /// It runs in shadow mode: it only says what it would have done, so the thresholds can be
    /// calibrated against real journeys before anything is ever shown to a user. The thresholds
    /// travel with every decision so a log line can be read on its own.
    /// </summary>
    internal sealed class RdpLinkStabilityMonitor
    {
        internal const long RttWindowMs = 120_000;
        internal const long MinRttSpanMs = 60_000;
        internal const int MinRttSamples = 4;
        internal const int UnstableRttMs = 250;
        internal const long DropWindowMs = 600_000;
        internal const int UnstableDrops = 2;
        internal const long PromptCooldownMs = 1_800_000;
        internal const long StableForMs = 900_000;
        internal const int StableRttMs = 100;
        internal const int LossWindowProbes = 5;
        internal const int DegradedLosses = 3;

        private readonly System.Threading.Lock _sync = new();
        private readonly Queue<(long At, int RttMs)> _rtt = new();
        private readonly Queue<long> _drops = new();
        private readonly Queue<bool> _probeLosses = new();
        private bool _unstable;
        private long _lastPromptMs = long.MinValue;
        private long _calmSinceMs = long.MinValue;

        internal bool IsUnstable
        {
            get { lock (_sync) return _unstable; }
        }

        /// <summary>
        /// A round-trip reading, from the control or, with <paramref name="probe"/>, from an
        /// answered link probe.
        /// </summary>
        internal RdpAdaptDecision? Sample(long nowMs, int rttMs, bool probe = false)
        {
            lock (_sync)
            {
                _rtt.Enqueue((nowMs, Math.Max(0, rttMs)));
                if (probe)
                    RecordProbeOutcome(lost: false);
                return Evaluate(nowMs);
            }
        }

        /// <summary>
        /// A link probe went unanswered. Loss is a degraded reading, not a drop: the session may
        /// still be up, so <see cref="DegradedLosses"/> lost of the last
        /// <see cref="LossWindowProbes"/> probes count as an unstable link on their own.
        /// </summary>
        internal RdpAdaptDecision? Loss(long nowMs)
        {
            lock (_sync)
            {
                RecordProbeOutcome(lost: true);
                return Evaluate(nowMs);
            }
        }

        private void RecordProbeOutcome(bool lost)
        {
            _probeLosses.Enqueue(lost);
            while (_probeLosses.Count > LossWindowProbes)
                _probeLosses.Dequeue();
        }

        /// <summary>The link dropped: the control started an automatic reconnection.</summary>
        internal RdpAdaptDecision? Drop(long nowMs)
        {
            lock (_sync)
            {
                _drops.Enqueue(nowMs);
                _calmSinceMs = long.MinValue;
                return Evaluate(nowMs);
            }
        }

        private RdpAdaptDecision? Evaluate(long nowMs)
        {
            while (_rtt.Count > 0 && nowMs - _rtt.Peek().At > RttWindowMs)
                _rtt.Dequeue();
            while (_drops.Count > 0 && nowMs - _drops.Peek() > DropWindowMs)
                _drops.Dequeue();

            int drops = _drops.Count;
            int? rttMedian = RttMedian(nowMs);

            bool slow = rttMedian >= UnstableRttMs;
            bool dropping = drops >= UnstableDrops;
            bool lossy = _probeLosses.Count(lost => lost) >= DegradedLosses;

            if (!_unstable)
            {
                if (!slow && !dropping && !lossy)
                    return null;

                _unstable = true;
                _calmSinceMs = long.MinValue;
                if (_lastPromptMs != long.MinValue && nowMs - _lastPromptMs < PromptCooldownMs)
                    return new RdpAdaptDecision("unstable_suppressed", Reason(slow, dropping, lossy), rttMedian, drops);

                _lastPromptMs = nowMs;
                return new RdpAdaptDecision("would_prompt", Reason(slow, dropping, lossy), rttMedian, drops);
            }

            bool calm = drops == 0 && !lossy && rttMedian is int median && median < StableRttMs;
            if (!calm)
            {
                _calmSinceMs = long.MinValue;
                return null;
            }

            if (_calmSinceMs == long.MinValue)
            {
                _calmSinceMs = nowMs;
                return null;
            }

            if (nowMs - _calmSinceMs < StableForMs)
                return null;

            _unstable = false;
            _calmSinceMs = long.MinValue;
            return new RdpAdaptDecision("would_offer_restore", "calm", rttMedian, drops);
        }

        /// <summary>
        /// The median round trip over the window, or null while there are too few readings, or
        /// they cover too short a time, to call the link slow.
        /// </summary>
        private int? RttMedian(long nowMs)
        {
            if (_rtt.Count < MinRttSamples)
                return null;
            if (nowMs - _rtt.Peek().At < MinRttSpanMs)
                return null;

            int[] sorted = _rtt.Select(r => r.RttMs).OrderBy(r => r).ToArray();
            return sorted[sorted.Length / 2];
        }

        private static string Reason(bool slow, bool dropping, bool lossy)
        {
            var parts = new List<string>(3);
            if (slow) parts.Add("rtt");
            if (dropping) parts.Add("drops");
            if (lossy) parts.Add("loss");
            return string.Join("_and_", parts);
        }
    }

    internal sealed record RdpAdaptDecision(string Decision, string Reason, int? RttMedianMs, int DropsInWindow);
}

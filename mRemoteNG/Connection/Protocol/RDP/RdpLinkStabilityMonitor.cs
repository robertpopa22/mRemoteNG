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

        private readonly System.Threading.Lock _sync = new();
        private readonly Queue<(long At, int RttMs)> _rtt = new();
        private readonly Queue<long> _drops = new();
        private bool _unstable;
        private long _lastPromptMs = long.MinValue;
        private long _calmSinceMs = long.MinValue;

        internal bool IsUnstable
        {
            get { lock (_sync) return _unstable; }
        }

        /// <summary>A round-trip reading from the control.</summary>
        internal RdpAdaptDecision? Sample(long nowMs, int rttMs)
        {
            lock (_sync)
            {
                _rtt.Enqueue((nowMs, Math.Max(0, rttMs)));
                return Evaluate(nowMs);
            }
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

            if (!_unstable)
            {
                if (!slow && !dropping)
                    return null;

                _unstable = true;
                _calmSinceMs = long.MinValue;
                if (_lastPromptMs != long.MinValue && nowMs - _lastPromptMs < PromptCooldownMs)
                    return new RdpAdaptDecision("unstable_suppressed", Reason(slow, dropping), rttMedian, drops);

                _lastPromptMs = nowMs;
                return new RdpAdaptDecision("would_prompt", Reason(slow, dropping), rttMedian, drops);
            }

            bool calm = drops == 0 && rttMedian is int median && median < StableRttMs;
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

        private static string Reason(bool slow, bool dropping) =>
            slow && dropping ? "rtt_and_drops" : slow ? "rtt" : "drops";
    }

    internal sealed record RdpAdaptDecision(string Decision, string Reason, int? RttMedianMs, int DropsInWindow);
}

using System;

namespace mRemoteNG.Connection.Protocol.RDP
{
    /// <summary>
    /// Decides whether the ActiveX automatic reconnection (ARC) may continue (#212).
    /// ARC is meant for short network blips. When the client was suspended (sleep,
    /// Modern Standby, hibernate) the process is frozen, the attempt budget is not
    /// consumed, and the remaining attempts silently resume the session long after
    /// the user moved on — taking it from the console if the same user sat down there.
    /// <para>
    /// The protocol calls <see cref="Touch"/> periodically while the tab is open and
    /// <see cref="ShouldContinue"/> on every ARC attempt. A gap larger than
    /// <see cref="MaxGap"/> between two observations means the process was frozen; ARC
    /// attempts within <see cref="MaxGap"/> of that resume are stopped. Both clocks are
    /// checked because after hibernate the wall clock can be stale until resynchronised,
    /// and the tick count can miss time spent asleep.
    /// </para>
    /// </summary>
    public sealed class RdpAutoReconnectGate
    {
        public static readonly TimeSpan MaxGap = TimeSpan.FromMinutes(2);
        public static readonly TimeSpan TouchInterval = TimeSpan.FromSeconds(30);

        private long? _lastTicksMs;
        private DateTime? _lastUtc;
        private long? _resumeDetectedAtTicksMs;

        public bool StoppedAfterResume { get; private set; }

        /// <summary>Records that the process is running now and notes a resume if the previous observation is too old.</summary>
        public void Touch(long ticksMs, DateTime utcNow)
        {
            bool gap = (_lastTicksMs is long t && ticksMs - t > MaxGap.TotalMilliseconds)
                       || (_lastUtc is DateTime u && (utcNow - u).Duration() > MaxGap);
            if (gap) _resumeDetectedAtTicksMs = ticksMs;
            _lastTicksMs = ticksMs;
            _lastUtc = utcNow;
        }

        /// <summary>Called for each ARC attempt. Returns false when ARC must stop.</summary>
        public bool ShouldContinue(long ticksMs, DateTime utcNow)
        {
            Touch(ticksMs, utcNow);
            bool recentResume = _resumeDetectedAtTicksMs is long r && ticksMs - r <= MaxGap.TotalMilliseconds;
            if (recentResume) StoppedAfterResume = true;
            return !recentResume;
        }

        /// <summary>The session is connected; start a fresh observation window.</summary>
        public void Reset(long ticksMs, DateTime utcNow)
        {
            _lastTicksMs = ticksMs;
            _lastUtc = utcNow;
            _resumeDetectedAtTicksMs = null;
            StoppedAfterResume = false;
        }
    }
}

using System;

namespace mRemoteNG.Connection.Protocol.RDP
{
    /// <summary>
    /// The clock behind <c>rdp_phase duration_ms</c>, and the bookkeeping that tells a first login
    /// from a login after the control reconnected on its own.
    /// <para>
    /// The control can reconnect without going through <c>Connect</c>: automatic reconnection (ARC)
    /// after a network drop, announced by <c>OnAutoReconnecting</c>/<c>OnAutoReconnecting2</c>, or
    /// a reconnection that raises only <c>OnConnected</c>/<c>OnLoginComplete</c> again. A clock
    /// started at <c>Connect</c> alone then reports the age of the session as the login time
    /// (field log: <c>login_complete duration_ms=298964</c> five minutes into a session).
    /// The clock restarts when an ARC outage starts, and on an <c>OnConnected</c> that follows a
    /// completed login without an ARC announcement. The first connect is still timed from
    /// <c>Connect</c>, so first-login durations stay comparable with older logs.
    /// </para>
    /// <para>
    /// Both ARC events may be raised for the same attempt; <see cref="AutoReconnecting"/> reports
    /// each attempt number once per outage so the attempt is logged and counted once.
    /// </para>
    /// </summary>
    internal sealed class RdpPhaseClock
    {
        private readonly Func<long> _nowMs;
        private long _startMs;
        private bool _loggedIn;
        private bool _inArc;
        private int _lastArcAttempt;

        internal RdpPhaseClock(Func<long>? nowMs = null)
        {
            _nowMs = nowMs ?? (() => Environment.TickCount64);
            _startMs = _nowMs();
        }

        internal long ElapsedMs => Math.Max(0, _nowMs() - _startMs);

        internal void Connect()
        {
            _startMs = _nowMs();
            _loggedIn = false;
            _inArc = false;
            _lastArcAttempt = 0;
        }

        /// <summary>
        /// An ARC attempt was announced. Returns <c>true</c> when this attempt number has not been
        /// seen in the current outage, i.e. when it should be logged and counted;
        /// <paramref name="outageStarted"/> is <c>true</c> for the first announcement of an outage.
        /// </summary>
        internal bool AutoReconnecting(int attempt, out bool outageStarted)
        {
            outageStarted = !_inArc;
            if (outageStarted)
            {
                _inArc = true;
                _startMs = _nowMs();
            }
            if (attempt == _lastArcAttempt)
                return false;
            _lastArcAttempt = attempt;
            return true;
        }

        internal void Connected()
        {
            if (_loggedIn && !_inArc)
                _startMs = _nowMs();
        }

        /// <summary>Returns <c>true</c> when this login follows an earlier one of the same connect.</summary>
        internal bool LoginComplete()
        {
            bool again = _loggedIn;
            _loggedIn = true;
            _inArc = false;
            _lastArcAttempt = 0;
            return again;
        }
    }
}

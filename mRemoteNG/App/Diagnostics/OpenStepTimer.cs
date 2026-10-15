using System.Collections.Generic;
using System.Diagnostics;

namespace mRemoteNG.App.Diagnostics
{
    /// <summary>
    /// Times the steps of opening one connection tab, so a stall on the UI thread can be pinned
    /// to the step that caused it. <see cref="Mark"/> charges the time since the previous mark to
    /// the named step; a step marked twice accumulates. Wall-clock time: a step that awaits
    /// includes the time the UI thread spent on other messages meanwhile.
    /// </summary>
    internal sealed class OpenStepTimer
    {
        private readonly long _start;
        private long _last;
        private readonly List<KeyValuePair<string, long>> _steps = [];

        internal OpenStepTimer()
        {
            _start = Stopwatch.GetTimestamp();
            _last = _start;
        }

        /// <summary>True when this open created the first RDP ActiveX control of the process.</summary>
        internal bool FirstControl { get; set; }

        internal IReadOnlyList<KeyValuePair<string, long>> Steps => _steps;

        internal long TotalMs => (long)Stopwatch.GetElapsedTime(_start).TotalMilliseconds;

        internal void Mark(string step)
        {
            long now = Stopwatch.GetTimestamp();
            long ms = (long)Stopwatch.GetElapsedTime(_last, now).TotalMilliseconds;
            _last = now;
            for (int i = 0; i < _steps.Count; i++)
            {
                if (_steps[i].Key != step) continue;
                _steps[i] = new KeyValuePair<string, long>(step, _steps[i].Value + ms);
                return;
            }
            _steps.Add(new KeyValuePair<string, long>(step, ms));
        }
    }
}

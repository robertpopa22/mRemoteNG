using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace mRemoteNG.App.Diagnostics
{
    /// <summary>
    /// Times the path from a mouse click in the connection tree to the moment
    /// <c>ConnectionInitiator.OpenConnection</c> is entered, so a UI-thread stall between the click
    /// and the open is attributed to a step in the log (<c>tree_click</c>). Numbers only.
    /// <para>
    /// The two <c>*_queue_ms</c> steps are how long the mouse message waited in the input queue
    /// before the tree saw it (<c>GetMessageTime</c> against the current tick count). A large value
    /// there means the UI thread was already busy with something else when the user clicked; a
    /// large <c>select</c>, <c>config_load</c> or <c>handler</c> means the click itself did the work.
    /// </para>
    /// Every call except <see cref="SaveRan"/> happens on the UI thread.
    /// </summary>
    internal static class TreeClickTrace
    {
        /// <summary>A click whose open did not follow within this time is forgotten.</summary>
        internal const long MaxAgeMs = 60_000;

        private static long _downTicks;
        private static long _downQueueMs;
        private static long _selectMs;
        private static long _configLoadMs;
        private static long _dispatchTicks;
        private static long _clickQueueMs;
        private static int _clicks;
        private static int _uiThreadId;
        private static long _saveUiMs;
        private static long _saveBackgroundMs;

        /// <summary>Milliseconds a message that the tick count stamped at <paramref name="messageTime"/> waited.</summary>
        internal static long QueueWaitMs(int messageTime, int now)
        {
            uint waited = unchecked((uint)now - (uint)messageTime);
            // A nested pump or a synthesized message can carry an unrelated stamp; ignore nonsense.
            return waited > MaxAgeMs ? 0 : waited;
        }

        internal static long CurrentQueueWaitMs()
        {
            try
            {
                return QueueWaitMs(NativeMethods.GetMessageTime(), Environment.TickCount);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        internal static void MouseDown(long queueMs)
        {
            Reset();
            _uiThreadId = Environment.CurrentManagedThreadId;
            _downQueueMs = queueMs;
            Volatile.Write(ref _downTicks, Stopwatch.GetTimestamp());
        }

        /// <summary>Time spent in the tree's selection handler, split into the property grid load and the rest.</summary>
        internal static void Selected(long selectMs, long configLoadMs)
        {
            if (Volatile.Read(ref _downTicks) == 0) return;
            _selectMs += selectMs;
            _configLoadMs += configLoadMs;
        }

        internal static void Dispatched(int clicks, long queueMs)
        {
            if (Volatile.Read(ref _downTicks) == 0)
                MouseDown(0);
            _clicks = clicks;
            _clickQueueMs = queueMs;
            _dispatchTicks = Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// The click handlers returned. An open they started has already been reported (it is
        /// entered synchronously); an open that comes later from elsewhere is not this click's.
        /// </summary>
        internal static void DispatchEnded() => _dispatchTicks = 0;

        /// <summary>A connections save ran while a click was being traced (any thread).</summary>
        internal static void SaveRan(long durationMs)
        {
            if (Volatile.Read(ref _downTicks) == 0) return;
            if (Environment.CurrentManagedThreadId == Volatile.Read(ref _uiThreadId))
                Interlocked.Add(ref _saveUiMs, durationMs);
            else
                Interlocked.Add(ref _saveBackgroundMs, durationMs);
        }

        /// <summary>Called first thing in <c>OpenConnection</c>. Writes the line when a traced click led here.</summary>
        internal static void ReachedOpenConnection(string protocol)
        {
            try
            {
                long dispatch = _dispatchTicks;
                long down = Volatile.Read(ref _downTicks);
                if (dispatch == 0 || down == 0 || Environment.CurrentManagedThreadId != _uiThreadId)
                    return;

                long now = Stopwatch.GetTimestamp();
                long sinceDownMs = (long)Stopwatch.GetElapsedTime(down, now).TotalMilliseconds;
                if (sinceDownMs > MaxAgeMs)
                {
                    Reset();
                    return;
                }

                long handlerMs = (long)Stopwatch.GetElapsedTime(dispatch, now).TotalMilliseconds;
                long totalMs = _downQueueMs + sinceDownMs;
                long saveUiMs = Interlocked.Read(ref _saveUiMs);
                List<KeyValuePair<string, long>> steps = Steps(totalMs, _downQueueMs, _selectMs, _configLoadMs,
                    saveUiMs, _clickQueueMs, handlerMs);
                RuntimeDiagnostics.TreeClick(protocol, _clicks, totalMs, steps,
                    Interlocked.Read(ref _saveBackgroundMs));
            }
            catch (Exception ex)
            {
                // Diagnostics must never break opening a connection.
                RuntimeDiagnostics.SafeException("tree_click", ex);
            }
            finally
            {
                Reset();
            }
        }

        /// <summary>
        /// The steps in the order they normally happen. <c>other</c> is the rest of the total:
        /// the time between the two clicks of a double-click and anything outside the traced steps.
        /// </summary>
        internal static List<KeyValuePair<string, long>> Steps(long totalMs, long downQueueMs, long selectMs,
            long configLoadMs, long saveUiMs, long clickQueueMs, long handlerMs)
        {
            long other = totalMs - downQueueMs - selectMs - configLoadMs - saveUiMs - clickQueueMs - handlerMs;
            return
            [
                new("down_queue", downQueueMs),
                new("select", selectMs),
                new("config_load", configLoadMs),
                new("save", saveUiMs),
                new("click_queue", clickQueueMs),
                new("handler", handlerMs),
                new("other", Math.Max(0, other))
            ];
        }

        private static void Reset()
        {
            Volatile.Write(ref _downTicks, 0);
            _dispatchTicks = 0;
            _downQueueMs = 0;
            _selectMs = 0;
            _configLoadMs = 0;
            _clickQueueMs = 0;
            _clicks = 0;
            Interlocked.Exchange(ref _saveUiMs, 0);
            Interlocked.Exchange(ref _saveBackgroundMs, 0);
        }
    }
}

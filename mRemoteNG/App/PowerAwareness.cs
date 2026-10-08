using System;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace mRemoteNG.App
{
    /// <summary>
    /// Single source of truth for "should background work slow down": the machine runs on battery
    /// or the main window is minimized (#210). Idle consumers (host probes, diagnostics heartbeat,
    /// title polls) read <see cref="LowPowerMode"/> instead of each querying the OS.
    /// Nothing here changes behavior on AC power while the window is shown.
    /// </summary>
    public static class PowerAwareness
    {
        /// <summary>The battery state is re-read from the OS at most this often when read through <see cref="OnBattery"/>.</summary>
        internal const long BatteryReadIntervalMs = 30_000;

        private static readonly Lock Sync = new();
        private static Func<bool> _batteryProbe = ReadBatteryFromSystem;
        private static Func<long> _clock = static () => Environment.TickCount64;
        private static bool _onBattery;
        private static long _lastReadMs;
        private static bool _hasRead;
        private static bool _isMinimized;
        private static bool _subscribed;

        /// <summary>Raised when <see cref="OnBattery"/> or <see cref="IsMinimized"/> changes. May be raised on any thread.</summary>
        public static event EventHandler? Changed;

        /// <summary>True when the machine is running on battery (cached; re-read from the OS at most every 30 s).</summary>
        public static bool OnBattery
        {
            get
            {
                bool changed = false;
                bool value;
                lock (Sync)
                {
                    long now = _clock();
                    if (!_hasRead || now - _lastReadMs >= BatteryReadIntervalMs)
                        changed = ReadNow(now);
                    value = _onBattery;
                }
                if (changed) RaiseChanged();
                return value;
            }
        }

        /// <summary>True while the main window is minimized (set by the main form; default false).</summary>
        public static bool IsMinimized
        {
            get { lock (Sync) return _isMinimized; }
            set
            {
                lock (Sync)
                {
                    if (_isMinimized == value) return;
                    _isMinimized = value;
                }
                RaiseChanged();
            }
        }

        /// <summary>True when idle background work should back off.</summary>
        public static bool LowPowerMode => OnBattery || IsMinimized;

        /// <summary>Begin listening for OS power-state notifications. Safe to call more than once.</summary>
        public static void Initialize()
        {
            lock (Sync)
            {
                if (_subscribed) return;
                _subscribed = true;
            }

            try
            {
                WithoutInstallingContext(() => SystemEvents.PowerModeChanged += OnPowerModeChanged);
            }
            catch
            {
                // Without the notification the 30 s re-read in OnBattery still keeps the value fresh.
            }
        }

        /// <summary>
        /// Runs <paramref name="action"/> and, if the thread had no synchronization context before,
        /// leaves it without one. Subscribing to <see cref="SystemEvents"/> reads
        /// AsyncOperationManager.SynchronizationContext, which installs a plain
        /// <see cref="SynchronizationContext"/> on a thread that has none. Initialize runs on the
        /// UI thread before the first control exists, so that plain context stayed there for the
        /// whole session and every UI await resumed on the thread pool (#216).
        /// </summary>
        internal static void WithoutInstallingContext(Action action)
        {
            SynchronizationContext? before = SynchronizationContext.Current;
            try
            {
                action();
            }
            finally
            {
                if (before == null && SynchronizationContext.Current != null)
                    SynchronizationContext.SetSynchronizationContext(null);
            }
        }

        private static void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode is not (PowerModes.StatusChange or PowerModes.Resume))
                return;

            bool changed;
            lock (Sync)
            {
                changed = ReadNow(_clock());
            }
            if (changed) RaiseChanged();
        }

        // Caller holds Sync. Returns true when the observable battery state changed.
        private static bool ReadNow(long now)
        {
            bool previous = _onBattery;
            bool hadRead = _hasRead;
            bool value;
            try
            {
                value = _batteryProbe();
            }
            catch
            {
                value = false;
            }

            _onBattery = value;
            _lastReadMs = now;
            _hasRead = true;
            return hadRead ? previous != value : value;
        }

        private static bool ReadBatteryFromSystem() =>
            SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Offline;

        private static void RaiseChanged()
        {
            try
            {
                Changed?.Invoke(null, EventArgs.Empty);
            }
            catch
            {
                // A faulty subscriber must not break the caller's background loop.
            }
        }

        /// <summary>Test hook: replace the battery probe and clock and reset all cached state.</summary>
        internal static void ResetForTests(Func<bool>? batteryProbe = null, Func<long>? clock = null)
        {
            lock (Sync)
            {
                _batteryProbe = batteryProbe ?? ReadBatteryFromSystem;
                _clock = clock ?? (static () => Environment.TickCount64);
                _onBattery = false;
                _lastReadMs = 0;
                _hasRead = false;
                _isMinimized = false;
            }
        }
    }
}

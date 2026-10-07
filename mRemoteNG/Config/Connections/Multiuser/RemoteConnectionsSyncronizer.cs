using mRemoteNG.App;
using mRemoteNG.Messages;
using mRemoteNG.Properties;
using mRemoteNG.UI.Forms;
using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Timers;

// ReSharper disable ArrangeAccessorOwnerBody

namespace mRemoteNG.Config.Connections.Multiuser
{
    [SupportedOSPlatform("windows")]
    public class RemoteConnectionsSyncronizer : IConnectionsUpdateChecker
    {
        /// <summary>
        /// Poll interval used when the checker is a <see cref="FileConnectionsUpdateChecker"/>.
        /// The FileSystemWatcher reports changes within ~1 s, so the timer is only a safety net
        /// for missed watcher events; polling at SQLReloadInterval in file mode was redundant (#210).
        /// </summary>
        public const double FileModeSafetyNetIntervalInMilliseconds = 300000.0;

        /// <summary>Lower bound for the poll interval used after the file watcher degraded.</summary>
        internal const double MinimumFallbackIntervalInMilliseconds = 5000.0;

        private readonly System.Timers.Timer _updateTimer;
        private readonly IConnectionsUpdateChecker _updateChecker;
        private readonly Lock _timerLock = new();
        private bool _disposed;

        public double TimerIntervalInMilliseconds
        {
            get { return _updateTimer.Interval; }
        }

        /// <summary>
        /// Gets the UTC time of the last successful external sync, or null if no sync has occurred yet.
        /// </summary>
        public DateTime? LastExternalSync { get; private set; }

        /// <summary>
        /// Raised when connections have been reloaded due to an external change (file or database).
        /// </summary>
        public event EventHandler? ConnectionsReloadedExternally;

        public RemoteConnectionsSyncronizer(IConnectionsUpdateChecker updateChecker)
        {
            _updateChecker = updateChecker;
            double intervalMs = updateChecker is FileConnectionsUpdateChecker
                ? FileModeSafetyNetIntervalInMilliseconds
                : OptionsDBsPage.Default.SQLReloadInterval * 1000.0;
            _updateTimer = new System.Timers.Timer(intervalMs > 0 ? intervalMs : 30000.0);
            SetEventListeners();
            if (updateChecker is FileConnectionsUpdateChecker fileChecker)
                fileChecker.WatcherDegraded += OnWatcherDegraded;
        }

        /// <summary>
        /// The file watcher failed (typically an unreliable SMB/NAS share): fall back to the
        /// regular reload interval so team sync keeps its old latency.
        /// </summary>
        private void OnWatcherDegraded(object? sender, EventArgs e)
        {
            double fallbackMs = Math.Max(MinimumFallbackIntervalInMilliseconds, OptionsDBsPage.Default.SQLReloadInterval * 1000.0);
            lock (_timerLock)
            {
                if (_disposed)
                    return;
                _updateTimer.Interval = fallbackMs;
            }
            Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg,
                $"Connection file watcher degraded; polling every {fallbackMs / 1000.0:0.#} s", true);
        }

        private void SetEventListeners()
        {
            _updateChecker.UpdateCheckStarted += OnUpdateCheckStarted;
            _updateChecker.UpdateCheckFinished += OnUpdateCheckFinished;
            _updateChecker.ConnectionsUpdateAvailable += (_, args) => ConnectionsUpdateAvailable?.Invoke(this, args);
            _updateTimer.Elapsed += (sender, args) => _updateChecker.IsUpdateAvailableAsync();
            ConnectionsUpdateAvailable += Load;
        }

        private void Load(object sender, ConnectionsUpdateAvailableEventArgs args)
        {
            // A watcher/timer callback already in flight when this instance was replaced must
            // not trigger a reload through the orphaned synchronizer (#210).
            if (_disposed)
                return;

            // Update checkers (SQL polling, file watcher) raise this event from
            // background threads. Marshal the reload onto the UI thread so the
            // tree/model stays single-threaded — otherwise concurrent Children
            // mutations trip enumeration in GetRecursiveChildList. Fixes #102.
            if (FrmMain.IsCreated && FrmMain.Default.IsHandleCreated && FrmMain.Default.InvokeRequired)
            {
                FrmMain.Default.BeginInvoke(new Action(() => Load(sender, args)));
                return;
            }

            bool reloaded = true;
            if (args.DatabaseConnector != null)
            {
                Runtime.ConnectionsService.LoadConnections(true, false, "");
            }
            else
            {
                if (Runtime.ConnectionsService.ConnectionFileName != null)
                    reloaded = Runtime.ConnectionsService.LoadConnections(false, false, Runtime.ConnectionsService.ConnectionFileName, skipIfContentUnchanged: true);
            }
            args.Handled = true;

            // Same bytes as already loaded: nothing was reloaded, so no "team sync" event (#210).
            if (!reloaded)
                return;

            LastExternalSync = DateTime.UtcNow;
            string source = args.DatabaseConnector != null ? "database" : "file";
            Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                $"Connections reloaded from external {source} change (team sync)");
            ConnectionsReloadedExternally?.Invoke(this, EventArgs.Empty);
        }

        public void Enable()
        {
            lock (_timerLock)
            {
                if (!_disposed)
                    _updateTimer.Start();
            }
        }

        public void Disable()
        {
            lock (_timerLock)
            {
                if (!_disposed)
                    _updateTimer.Stop();
            }
        }

        public bool IsUpdateAvailable()
        {
            return _updateChecker.IsUpdateAvailable();
        }

        public void IsUpdateAvailableAsync()
        {
            _updateChecker.IsUpdateAvailableAsync();
        }


        private void OnUpdateCheckStarted(object sender, EventArgs eventArgs)
        {
            lock (_timerLock)
            {
                if (!_disposed)
                    _updateTimer.Stop();
            }
            UpdateCheckStarted?.Invoke(this, eventArgs);
        }

        private void OnUpdateCheckFinished(object sender, ConnectionsUpdateCheckFinishedEventArgs eventArgs)
        {
            lock (_timerLock)
            {
                if (!_disposed)
                    _updateTimer.Start();
            }
            UpdateCheckFinished?.Invoke(this, eventArgs);
        }

        public event EventHandler? UpdateCheckStarted;
        public event UpdateCheckFinishedEventHandler? UpdateCheckFinished;
        public event ConnectionsUpdateAvailableEventHandler? ConnectionsUpdateAvailable;


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool itIsSafeToAlsoFreeManagedObjects)
        {
            if (!itIsSafeToAlsoFreeManagedObjects) return;
            lock (_timerLock)
            {
                if (_disposed) return;
                _disposed = true;
                _updateTimer.Dispose();
            }
            _updateChecker.Dispose();
        }
    }
}

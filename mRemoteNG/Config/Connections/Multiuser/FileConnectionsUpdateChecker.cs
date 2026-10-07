using System;
using System.IO;
using System.Threading;
using mRemoteNG.App;
using mRemoteNG.Messages;

namespace mRemoteNG.Config.Connections.Multiuser
{
    public class FileConnectionsUpdateChecker : IConnectionsUpdateChecker
    {
        private readonly FileSystemWatcher _watcher;
        private readonly string _connectionFilePath;
        private readonly System.Timers.Timer _debounceTimer;

        public FileConnectionsUpdateChecker(string connectionFilePath)
        {
            _connectionFilePath = connectionFilePath;
            string watchDir = Path.GetDirectoryName(connectionFilePath) ?? ".";
            if (!Directory.Exists(watchDir))
                Directory.CreateDirectory(watchDir);
            _watcher = new FileSystemWatcher(watchDir, Path.GetFileName(connectionFilePath));
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size | NotifyFilters.FileName;
            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
            // Saves are written to "<file>.tmp" and then File.Replace/File.Move'd over the target
            // (FileDataProvider.TrySave); such atomic saves surface as a rename (#210).
            _watcher.Renamed += OnFileChanged;
            // Network shares (SMB/NAS) can overflow or drop the watcher; without this handler the
            // team-sync fast path would silently stop and only the long safety-net poll remains.
            _watcher.Error += OnWatcherError;
            _watcher.EnableRaisingEvents = true;

            _debounceTimer = new System.Timers.Timer(1000); // 1s debounce
            _debounceTimer.AutoReset = false;
            _debounceTimer.Elapsed += OnDebounceTimerElapsed;
        }

        private int _watcherErrorLogged;
        private bool _disposed;

        /// <summary>
        /// Raised when the file watcher reported an error (buffer overflow, share unavailable, ...).
        /// Subscribers should fall back to polling at the regular reload interval.
        /// </summary>
        public event EventHandler? WatcherDegraded;

        private void OnWatcherError(object sender, ErrorEventArgs e) => HandleWatcherError(e.GetException());

        internal void HandleWatcherError(Exception? error)
        {
            if (Interlocked.Exchange(ref _watcherErrorLogged, 1) == 0)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg,
                    $"Connection file watcher failed ({error?.GetType().Name ?? "unknown"}); falling back to periodic polling", true);
            }

            try
            {
                // Try to recover the watcher; polling stays as the fallback either way.
                _watcher.EnableRaisingEvents = false;
                _watcher.EnableRaisingEvents = true;
            }
            catch
            {
                // Disposed or share gone: the polling fallback below covers it.
            }

            WatcherDegraded?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Test hook: simulates a FileSystemWatcher error.</summary>
        internal void SimulateWatcherError() => HandleWatcherError(new InternalBufferOverflowException());

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // Watcher callbacks run on thread-pool threads and can still be in flight while the
            // checker is disposed; touching the disposed timer would crash the process.
            if (Volatile.Read(ref _disposed))
                return;

            try
            {
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
            catch (ObjectDisposedException)
            {
                // Disposed concurrently: nothing left to debounce.
            }
        }

        private void OnDebounceTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Volatile.Read(ref _disposed))
                return;

            // Trigger update check
            // Since we are on a timer thread, we should probably invoke safely?
            // But IsUpdateAvailableAsync spins a thread anyway (in Sql implementation).
            // Here, we just want to signal that an update IS available.
            
            // However, the interface expects IsUpdateAvailableAsync to eventually fire UpdateCheckFinished.
            // But RemoteConnectionsSyncronizer calls IsUpdateAvailableAsync periodically.
            
            // If we want immediate updates, we should manually trigger the check.
            IsUpdateAvailableAsync();
        }

        public bool IsUpdateAvailable()
        {
            RaiseUpdateCheckStartedEvent();
            bool updateAvailable = CheckFileUpdate();
            if (updateAvailable)
                RaiseConnectionsUpdateAvailableEvent();
            RaiseUpdateCheckFinishedEvent(updateAvailable);
            return updateAvailable;
        }

        private bool CheckFileUpdate()
        {
            try
            {
                if (!File.Exists(_connectionFilePath))
                    return false;

                DateTime currentLastWrite = File.GetLastWriteTimeUtc(_connectionFilePath);
                DateTime lastKnownUpdate = Runtime.ConnectionsService.LastFileUpdate.ToUniversalTime();

                // Exact UTC tick comparison: LastFileUpdate is stored from File.GetLastWriteTimeUtc
                // unmodified, so no sub-second truncation is needed (truncating hid a second
                // write landing in the same second as the one we loaded, #210).
                // Residual gap: FAT/SMB 2 s mtime granularity can still make two writes look equal;
                // that is mitigated by the content-hash short-circuit on reload, not here.
                return currentLastWrite > lastKnownUpdate;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg, $"Error checking for file updates: {ex.Message}", true);
                return false;
            }
        }

        public void IsUpdateAvailableAsync()
        {
            Thread thread = new(() => IsUpdateAvailable());
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        public event EventHandler? UpdateCheckStarted;

        private void RaiseUpdateCheckStartedEvent()
        {
            UpdateCheckStarted?.Invoke(this, EventArgs.Empty);
        }

        public event UpdateCheckFinishedEventHandler? UpdateCheckFinished;

        private void RaiseUpdateCheckFinishedEvent(bool updateAvailable)
        {
            ConnectionsUpdateCheckFinishedEventArgs args = new() { UpdateAvailable = updateAvailable };
            UpdateCheckFinished?.Invoke(this, args);
        }

        public event ConnectionsUpdateAvailableEventHandler? ConnectionsUpdateAvailable;

        private void RaiseConnectionsUpdateAvailableEvent()
        {
            Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg, "File connection update is available");
            DateTime lastWrite = File.GetLastWriteTimeUtc(_connectionFilePath);
            ConnectionsUpdateAvailableEventArgs args = new(null, lastWrite);
            ConnectionsUpdateAvailable?.Invoke(this, args);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Volatile.Write(ref _disposed, true);
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _debounceTimer.Dispose();
            }
        }
    }
}

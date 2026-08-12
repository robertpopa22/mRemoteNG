using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace mRemoteNG.App
{
    /// <summary>
    /// Verbose diagnostic logging — active only when <c>verbose.log.enable</c>
    /// marker file exists next to the exe. Zero overhead when disabled.
    /// </summary>
    internal static class DevLog
    {
        private static readonly Lock _lock = new();
        private static string? _logPath;
        private static bool _initialized;
        private static bool _enabled;

        /// <summary>
        /// Initialize the dev log. Call once at startup.
        /// </summary>
        internal static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            string exeDir = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) ?? ".";
            string markerPath = Path.Combine(exeDir, "verbose.log.enable");
            _enabled = File.Exists(markerPath);

            if (!_enabled) return;

            _logPath = Path.Combine(exeDir, "mRemoteNG-verbose.log");
            try
            {
                File.WriteAllText(_logPath, $"=== mRemoteNG Verbose Log — {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}");
            }
            catch
            {
                _enabled = false;
            }
        }

        /// <summary>
        /// Returns true when dev mode is active (marker file exists).
        /// </summary>
        internal static bool IsEnabled => _enabled;

        /// <summary>
        /// Write a breadcrumb message to the verbose log.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Write(string message, [CallerMemberName] string? caller = null)
        {
            if (!_enabled) return;
            WriteCore(message, caller);
        }

        private static void WriteCore(string message, string? caller)
        {
            string line = $"{DateTime.Now:HH:mm:ss.fff} [{Environment.CurrentManagedThreadId,3}] {caller}: {message}";
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logPath!, line + Environment.NewLine);
                }
                catch
                {
                    // Silently ignore write failures
                }
            }
        }
    }
}

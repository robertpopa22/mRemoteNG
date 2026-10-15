using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using mRemoteNG.Messages;

namespace mRemoteNG.App
{
    /// <summary>
    /// Startup-only evidence for time that the phase timers cannot attribute.
    ///
    /// On 2026-10-15 a laptop on a lossy mobile link spent ~10 s inside InitializeProgram before its
    /// first statement ran, while every timed sub-phase inside it added up to ~1.2 s. Time spent
    /// before a method's first statement is the runtime preparing it: compiling it and loading the
    /// assemblies its body references. A plain assembly load only becomes slow, and slow in step
    /// with the network, when opening the file has to wait on something remote: a program folder
    /// synced by a cloud client whose files are placeholders (the bytes are downloaded on first
    /// open), or an antivirus cloud lookup on a file it has not seen before.
    ///
    /// This class records both halves of that question so the next log answers it: which program
    /// files were not on the local disk when the process started, and which assembly loads were
    /// preceded by a long silence. It changes no behaviour and stops listening once startup ends.
    /// </summary>
    internal static class StartupLoadTrace
    {
        // Win32 attribute bits that FileAttributes does not name.
        private const FileAttributes RecallOnOpen = (FileAttributes)0x00040000;
        private const FileAttributes RecallOnDataAccess = (FileAttributes)0x00400000;

        internal const long SlowGapMs = 250;
        private const int MaxListed = 20;

        private static readonly ConcurrentQueue<(long Ticks, string Name, string? File)> Loads = new();
        private static readonly HashSet<string> NotLocalFiles = new(StringComparer.OrdinalIgnoreCase);
        private static long _startTicks;
        private static int _started;
        private static int _finished;
        private static int _scannedFiles;
        private static long _scanMs;
        private static string? _scanError;

        /// <summary>True when the attributes say the file's data is not on the local disk.</summary>
        internal static bool IsNotLocal(FileAttributes attributes) =>
            (attributes & (RecallOnOpen | RecallOnDataAccess | FileAttributes.Offline)) != 0;

        /// <summary>Starts listening for assembly loads and records which program files are not local.</summary>
        public static void Start()
        {
            if (Interlocked.Exchange(ref _started, 1) == 1)
                return;

            _startTicks = Stopwatch.GetTimestamp();
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
            ScanProgramFolder();
        }

        private static void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
        {
            try
            {
                Assembly assembly = args.LoadedAssembly;
                string? file = assembly.IsDynamic ? null : assembly.Location;
                Loads.Enqueue((Stopwatch.GetTimestamp(), assembly.GetName().Name ?? "?", string.IsNullOrEmpty(file) ? null : file));
            }
            catch
            {
                // Diagnostics only: never let the trace break an assembly load.
            }
        }

        private static void ScanProgramFolder()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                string baseDirectory = AppContext.BaseDirectory;
                var directories = new List<string> { baseDirectory };
                string assemblies = Path.Combine(baseDirectory, "Assemblies");
                if (Directory.Exists(assemblies))
                    directories.Add(assemblies);

                // Enumeration reads attributes from the directory listing; it does not open the
                // files, so it does not download a placeholder.
                foreach (string directory in directories)
                {
                    foreach (FileInfo file in new DirectoryInfo(directory).EnumerateFiles())
                    {
                        string extension = file.Extension;
                        if (!extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) &&
                            !extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
                            continue;

                        _scannedFiles++;
                        if (IsNotLocal(file.Attributes))
                            NotLocalFiles.Add(file.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                _scanError = ex.GetType().Name;
            }
            _scanMs = sw.ElapsedMilliseconds;
        }

        /// <summary>Logs what the program-folder scan found. Call once the collector has writers.</summary>
        public static void LogProgramFiles(MessageCollector messageCollector)
        {
            if (Volatile.Read(ref _started) == 0)
                return;

            string text = string.Format(CultureInfo.InvariantCulture,
                "[Startup]   ProgramFiles: {0} dll/exe scanned in {1}ms, {2} not on the local disk (cloud placeholders)",
                _scannedFiles, _scanMs, NotLocalFiles.Count);
            if (_scanError != null)
                text += ", scan failed: " + _scanError;
            if (NotLocalFiles.Count > 0)
                text += ": " + string.Join(", ", NotLocalFiles.Select(Path.GetFileName).Take(MaxListed)) +
                        (NotLocalFiles.Count > MaxListed ? ", ..." : string.Empty);
            messageCollector.AddMessage(MessageClass.InformationMsg, text, true);
        }

        /// <summary>
        /// Stops listening and logs every assembly load that came after a silence of at least
        /// <see cref="SlowGapMs"/>. The event fires when a load completes, so the silence before it
        /// holds that load's own time plus any work done since the previous event.
        /// </summary>
        public static void Finish(MessageCollector messageCollector)
        {
            if (Volatile.Read(ref _started) == 0 || Interlocked.Exchange(ref _finished, 1) == 1)
                return;

            AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;

            int count = 0;
            long previous = _startTicks;
            while (Loads.TryDequeue(out var load))
            {
                count++;
                long gapMs = ElapsedMs(previous, load.Ticks);
                previous = load.Ticks;
                if (gapMs < SlowGapMs)
                    continue;

                bool wasPlaceholder = load.File != null && NotLocalFiles.Contains(load.File);
                messageCollector.AddMessage(MessageClass.InformationMsg, string.Format(CultureInfo.InvariantCulture,
                    "[Startup]   AssemblyLoad: {0} at +{1}ms, {2}ms after the previous load{3}",
                    load.Name, ElapsedMs(_startTicks, load.Ticks), gapMs,
                    wasPlaceholder ? ", file was a cloud placeholder at process start" : string.Empty), true);
            }

            messageCollector.AddMessage(MessageClass.InformationMsg, string.Format(CultureInfo.InvariantCulture,
                "[Startup]   AssemblyLoads: {0} during startup", count), true);
        }

        internal static long ElapsedMs(long fromTicks, long toTicks) =>
            (toTicks - fromTicks) * 1000 / Stopwatch.Frequency;
    }
}

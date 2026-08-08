using System;
using System.Globalization;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows.Forms;
using Serilog;

namespace mRemoteNG.App
{
    [SupportedOSPlatform("windows")]
    public class Logger
    {
        private const long MaxFileSizeBytes = 10 * 1024 * 1024;
        private const int MaxRetainedFiles = 5;
        private const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss,fff} [{ThreadId}] {Level:u6}- {Message:lj}{NewLine}{Exception}";

        public static readonly Logger Instance = new();

        private readonly Lock _rebuildLock = new();

        public ILogger? Log { get; private set; }

        public static string DefaultLogPath => BuildLogFilePath();

        private Logger()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (string.IsNullOrEmpty(Properties.OptionsNotificationsPage.Default.LogFilePath))
            {
                Properties.OptionsNotificationsPage.Default.LogFilePath = BuildLogFilePath();
            }

            SetLogPath(Properties.OptionsNotificationsPage.Default.LogToApplicationDirectory ? DefaultLogPath : Properties.OptionsNotificationsPage.Default.LogFilePath);
        }

        public void SetLogPath(string path)
        {
            lock (_rebuildLock)
            {
                ILogger? previous = Log;

                Log = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .Enrich.WithThreadId()
                    .WriteTo.File(
                        path,
                        rollingInterval: RollingInterval.Infinite,
                        fileSizeLimitBytes: MaxFileSizeBytes,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: MaxRetainedFiles,
                        outputTemplate: OutputTemplate,
                        formatProvider: CultureInfo.InvariantCulture)
                    .CreateLogger();

                (previous as IDisposable)?.Dispose();
            }
        }

        private static string BuildLogFilePath()
        {
            string logFilePath = Runtime.IsPortableEdition ? GetLogPathPortableEdition() : GetLogPathNormalEdition();

            string? logFileName = Path.ChangeExtension(Application.ProductName, ".log");

            if (logFileName == null) return "mRemoteNG.log";

            string logFile = Path.Combine(logFilePath, logFileName);

            return logFile;
        }

        private static string GetLogPathNormalEdition()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Application.ProductName ?? "mRemoteNG");
        }

        private static string GetLogPathPortableEdition()
        {
            string startupPath = Application.StartupPath;
            if (IsDirectoryWritable(startupPath))
                return startupPath;
            // Fallback for read-only or WebDAV drives: write log to %LOCALAPPDATA%
            return GetLogPathNormalEdition();
        }

        private static bool IsDirectoryWritable(string dirPath)
        {
            if (string.IsNullOrEmpty(dirPath)) return false;
            try
            {
                string testFile = Path.Combine(dirPath, Path.GetRandomFileName());
                using var fs = File.Create(testFile, 1, FileOptions.DeleteOnClose);
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}

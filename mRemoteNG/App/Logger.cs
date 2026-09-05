using System;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository;
using log4net.Util;

namespace mRemoteNG.App
{
    [SupportedOSPlatform("windows")]
    public class Logger
    {
        public static readonly Logger Instance = new();

        public ILog? Log { get; private set; }

        public static string DefaultLogPath => BuildLogFilePath();

        /// <summary>
        /// log4net.config lives beside the executable. It must be found from there, not from the
        /// process working directory: a shortcut with an empty "Start in", a scheduled task, or a
        /// Start-Process without -WorkingDirectory all start the app somewhere else, and log4net
        /// does not throw when its config is missing - it leaves the repository with no appenders,
        /// so every line the application writes is discarded and no log file appears at all.
        /// </summary>
        public static string ConfigFilePath => Path.Combine(AppContext.BaseDirectory, "log4net.config");

        private Logger()
        {
            Initialize();
        }

        private void Initialize()
        {
            GlobalContext.Properties["ProcessId"] = Environment.ProcessId;
            GlobalContext.Properties["AppSessionId"] = Guid.NewGuid().ToString("N")[..12];
            LogManager.CreateRepository("mRemoteNG");

            if (string.IsNullOrEmpty(Properties.OptionsNotificationsPage.Default.LogFilePath))
            {
                Properties.OptionsNotificationsPage.Default.LogFilePath = BuildLogFilePath();
            }

            SetLogPath(Properties.OptionsNotificationsPage.Default.LogToApplicationDirectory ? DefaultLogPath : Properties.OptionsNotificationsPage.Default.LogFilePath);
        }

        public void SetLogPath(string path)
        {
            ILoggerRepository repository = LogManager.GetRepository("mRemoteNG");

            XmlConfigurator.Configure(repository, new FileInfo(ConfigFilePath));

            IAppender[] appenders = repository.GetAppenders();

            foreach (IAppender appender in appenders)
            {
                RollingFileAppender fileAppender = (RollingFileAppender)appender;
                if (fileAppender is not { Name: "LogFileAppender" }) continue;
                fileAppender.File = path;
                fileAppender.ActivateOptions();
            }

            Log = LogManager.GetLogger("mRemoteNG", "Logger");
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

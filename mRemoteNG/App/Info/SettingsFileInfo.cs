using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Connection;
using mRemoteNG.Properties;

namespace mRemoteNG.App.Info
{
    [SupportedOSPlatform("windows")]
    public static class SettingsFileInfo
    {
        private static readonly string ExePath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(ConnectionInfo))?.Location) ?? string.Empty;
        private static readonly Lazy<string> InstalledSettingsPath = new(GetInstalledSettingsPath);

        internal const string PortableSettingsFolderName = "Settings";

        /// <summary>
        /// #214: the MSI ships the build compiled as portable and drops this file beside the
        /// executable. Its presence makes the copy an installed one.
        /// </summary>
        internal const string InstalledMarkerFileName = "mRemoteNG.installed";

        private static readonly Lazy<bool> _isInstalledCopy = new(() =>
            !string.IsNullOrEmpty(ExePath) && File.Exists(Path.Combine(ExePath, InstalledMarkerFileName)));

        /// <summary>True when the MSI installed this copy (see <see cref="InstalledMarkerFileName"/>).</summary>
        public static bool IsInstalledCopy => _isInstalledCopy.Value;

        // Config files are stored in a dedicated "Settings" subfolder to keep the exe directory clean.
        private static readonly Lazy<string> _portableWritablePath = new(() =>
        {
            string appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Application.ProductName ?? "mRemoteNG");
            string settingsDir = ResolvePortableSettingsDirectory(ExePath, IsInstalledCopy, IsDirectoryWritable, appDataDir);
            if (!string.Equals(settingsDir, appDataDir, StringComparison.OrdinalIgnoreCase))
            {
                if (!Directory.Exists(settingsDir))
                    Directory.CreateDirectory(settingsDir);
                MigratePortableSettings(ExePath, settingsDir);
            }
            else if (IsInstalledCopy && !string.IsNullOrEmpty(ExePath) && IsDirectoryWritable(ExePath))
            {
                // #214: before the installer marker, an elevated run of an installed copy kept its
                // settings in the program folder. That is what this elevated run saw last time.
                MigrateProgramFolderSettings(Path.Combine(ExePath, PortableSettingsFolderName), appDataDir);
            }
            return settingsDir;
        });

        /// <summary>
        /// Where the build compiled as portable keeps its settings. An installed copy always uses
        /// <paramref name="appDataDir"/>: its program folder is writable when it runs elevated, and
        /// choosing by writability split one user's settings between two folders (#214). A portable
        /// copy uses the Settings folder beside the executable, and falls back to
        /// <paramref name="appDataDir"/> on a read-only or WebDAV drive.
        /// </summary>
        internal static string ResolvePortableSettingsDirectory(string exePath, bool installedCopy, Func<string, bool> isWritable, string appDataDir)
        {
            if (installedCopy || string.IsNullOrEmpty(exePath) || !isWritable(exePath))
                return appDataDir;
            return Path.Combine(exePath, PortableSettingsFolderName);
        }

        private static bool IsDirectoryWritable(string dirPath)
        {
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

        public static string DefaultSettingsPath =>
            Runtime.IsPortableEdition
                ? _portableWritablePath.Value
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Application.ProductName ?? string.Empty);

        public static string SettingsPath =>
            Runtime.IsPortableEdition
                ? _portableWritablePath.Value
                : InstalledSettingsPath.Value;

        public static string UserSettingsFilePath =>
            Runtime.IsPortableEdition
                ? Path.Combine(_portableWritablePath.Value, $"{Path.GetFileNameWithoutExtension(Application.ExecutablePath)}.settings")
                : GetInstalledUserSettingsFilePath();

        public static string UserSettingsFolderPath =>
            string.IsNullOrWhiteSpace(UserSettingsFilePath)
                ? string.Empty
                : Path.GetDirectoryName(UserSettingsFilePath) ?? string.Empty;

        public static string LayoutFileName { get; } = "pnlLayout.xml";
        public static string ExtAppsFilesName { get; } = "extApps.xml";

        public static string ExtAppsFilePath
        {
            get
            {
                string customPath = Settings.Default.CustomExtAppsFilePath?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(customPath))
                {
                    try
                    {
                        string expandedPath = Environment.ExpandEnvironmentVariables(customPath);
                        return Path.GetFullPath(expandedPath);
                    }
                    catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
                    {
                    }
                }
                return Path.Combine(SettingsPath, ExtAppsFilesName);
            }
        }
        public static string CmdSnippetsFileName { get; } = "cmdSnippets.xml";
        public static string ThemesFileName { get; } = "Themes.xml";
        public static string LocalConnectionProperties { get; } = "LocalConnectionProperties.xml";
        public static string SqlConnectionsCache { get; } = "SqlConnectionsCache.xml";
        public static string QuickConnectHistoryFileName { get; } = "quickConnectHistory.xml";

        public static string ThemeFolder =>
            string.IsNullOrWhiteSpace(SettingsPath)
                ? string.Empty
                : Path.Combine(SettingsPath, "Themes");

        public static string InstalledThemeFolder =>
            string.IsNullOrWhiteSpace(ExePath)
                ? string.Empty
                : GetInstalledThemeFolder();

        private static string GetInstalledSettingsPath()
        {
            string configuredPath = GetConfiguredSettingsPath();
            return string.IsNullOrWhiteSpace(configuredPath) ? DefaultSettingsPath : configuredPath;
        }

        private static string GetInstalledThemeFolder()
        {
            string themeFolder = Path.Combine(ExePath, "Themes");
            if (Directory.Exists(themeFolder))
                return themeFolder;

            string settingsThemeFolder = Path.Combine(ExePath, PortableSettingsFolderName, "Themes");
            return Directory.Exists(settingsThemeFolder) ? settingsThemeFolder : themeFolder;
        }

        private static string GetConfiguredSettingsPath()
        {
            try
            {
                string configuredPath = Settings.Default.CustomConfigurationPath;
                if (string.IsNullOrWhiteSpace(configuredPath))
                    return string.Empty;

                string expandedPath = Environment.ExpandEnvironmentVariables(configuredPath.Trim());
                return Path.GetFullPath(expandedPath);
            }
            catch (ConfigurationErrorsException)
            {
                return string.Empty;
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return string.Empty;
            }
        }

        private static string GetInstalledUserSettingsFilePath()
        {
            try
            {
                return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            }
            catch (ConfigurationErrorsException)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Migrates portable config files from the exe root to the Settings subfolder.
        /// Only moves files that exist in the old location but not in the new location.
        /// </summary>
        private static void MigratePortableSettings(string oldDir, string newDir)
        {
            string[] configFiles =
            [
                "confCons.xml",
                "confConsNew.xml",
                "extApps.xml",
                "cmdSnippets.xml",
                "pnlLayout.xml",
                "Themes.xml",
                "LocalConnectionProperties.xml",
                "quickConnectHistory.xml",
                "SqlConnectionsCache.xml",
            ];

            // Also migrate the .settings file (e.g. mRemoteNG.settings)
            string settingsFileName = $"{Path.GetFileNameWithoutExtension(Application.ExecutablePath)}.settings";

            try
            {
                foreach (string fileName in configFiles)
                    MigrateFile(oldDir, newDir, fileName);

                MigrateFile(oldDir, newDir, settingsFileName);

                // Migrate confCons backup files (confCons.xml.*.backup)
                foreach (string backupFile in Directory.GetFiles(oldDir, "confCons.xml.*.backup"))
                {
                    string name = Path.GetFileName(backupFile);
                    string dest = Path.Combine(newDir, name);
                    if (!File.Exists(dest))
                        File.Move(backupFile, dest);
                }

                // Migrate Themes subfolder
                string oldThemes = Path.Combine(oldDir, "Themes");
                string newThemes = Path.Combine(newDir, "Themes");
                if (Directory.Exists(oldThemes) && !Directory.Exists(newThemes))
                    Directory.Move(oldThemes, newThemes);
            }
            catch
            {
                // Migration is best-effort; don't crash on failure
            }
        }

        internal const string MigratedProgramFolderSuffix = ".migrated";

        /// <summary>
        /// Whether the program folder's own settings are a candidate location for this run. A
        /// portable copy always uses them. An installed copy saw them only when it ran elevated,
        /// so a standard user is never offered another account's file from there (#214).
        /// </summary>
        public static bool ProgramFolderSettingsApply =>
            ProgramFolderSettingsApplyFor(IsInstalledCopy, !string.IsNullOrEmpty(ExePath) && IsDirectoryWritable(ExePath));

        internal static bool ProgramFolderSettingsApplyFor(bool installedCopy, bool programFolderWritable) =>
            !installedCopy || programFolderWritable;

        /// <summary>
        /// Brings the settings an older elevated run left in the program folder into the per-user
        /// folder. Missing files are copied. When every file in <paramref name="fromDir"/> then has
        /// an identical copy in <paramref name="toDir"/>, the program folder's copy is renamed with
        /// <see cref="MigratedProgramFolderSuffix"/> so it stops being offered as a second connections
        /// file; nothing is deleted. When a file differs, both stay and the connections file picker
        /// lets the user choose. Returns true when the program folder's copy was retired.
        /// </summary>
        internal static bool MigrateProgramFolderSettings(string fromDir, string toDir)
        {
            if (!Directory.Exists(fromDir))
                return false;

            CopyMissingSettings(fromDir, toDir);
            try
            {
                foreach (string source in Directory.GetFiles(fromDir))
                {
                    string target = Path.Combine(toDir, Path.GetFileName(source));
                    if (!File.Exists(target) || !FilesAreIdentical(source, target))
                        return false;
                }

                string retired = fromDir + MigratedProgramFolderSuffix;
                if (Directory.Exists(retired))
                    retired += "-" + DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
                Directory.Move(fromDir, retired);
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static bool FilesAreIdentical(string a, string b)
        {
            var infoA = new FileInfo(a);
            var infoB = new FileInfo(b);
            if (infoA.Length != infoB.Length)
                return false;
            using var hash = System.Security.Cryptography.SHA256.Create();
            using FileStream streamA = File.OpenRead(a);
            using FileStream streamB = File.OpenRead(b);
            return hash.ComputeHash(streamA).AsSpan().SequenceEqual(hash.ComputeHash(streamB));
        }

        /// <summary>
        /// Copies the configuration files that exist in <paramref name="fromDir"/> but not in
        /// <paramref name="toDir"/>. Existing files are never overwritten and the source is left as
        /// it was. Returns the names copied. Best-effort: a failure copies nothing more.
        /// </summary>
        internal static IReadOnlyList<string> CopyMissingSettings(string fromDir, string toDir)
        {
            List<string> copied = [];
            if (!Directory.Exists(fromDir))
                return copied;

            try
            {
                Directory.CreateDirectory(toDir);
                foreach (string source in Directory.GetFiles(fromDir))
                {
                    string name = Path.GetFileName(source);
                    string target = Path.Combine(toDir, name);
                    if (File.Exists(target))
                        continue;
                    File.Copy(source, target);
                    copied.Add(name);
                }

                string fromThemes = Path.Combine(fromDir, "Themes");
                string toThemes = Path.Combine(toDir, "Themes");
                if (Directory.Exists(fromThemes) && !Directory.Exists(toThemes))
                {
                    Directory.CreateDirectory(toThemes);
                    foreach (string source in Directory.GetFiles(fromThemes))
                        File.Copy(source, Path.Combine(toThemes, Path.GetFileName(source)));
                    copied.Add("Themes");
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Best-effort; the user keeps whatever was copied and the source is untouched.
            }

            return copied;
        }

        private static void MigrateFile(string oldDir, string newDir, string fileName)
        {
            string oldPath = Path.Combine(oldDir, fileName);
            string newPath = Path.Combine(newDir, fileName);
            if (File.Exists(oldPath) && !File.Exists(newPath))
                File.Move(oldPath, newPath);
        }
    }
}

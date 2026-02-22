using System;
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

        public static string DefaultSettingsPath =>
            Runtime.IsPortableEdition
                ? ExePath
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Application.ProductName ?? string.Empty);

        public static string SettingsPath =>
            Runtime.IsPortableEdition
                ? ExePath
                : InstalledSettingsPath.Value;

        public static string UserSettingsFilePath =>
            Runtime.IsPortableEdition
                ? Path.Combine(ExePath, $"{Path.GetFileNameWithoutExtension(Application.ExecutablePath)}.settings")
                : GetInstalledUserSettingsFilePath();

        public static string UserSettingsFolderPath =>
            string.IsNullOrWhiteSpace(UserSettingsFilePath)
                ? string.Empty
                : Path.GetDirectoryName(UserSettingsFilePath) ?? string.Empty;

        public static string LayoutFileName { get; } = "pnlLayout.xml";
        public static string ExtAppsFilesName { get; } = "extApps.xml";
        public static string ThemesFileName { get; } = "Themes.xml";
        public static string LocalConnectionProperties { get; } = "LocalConnectionProperties.xml";
        public static string QuickConnectHistoryFileName { get; } = "quickConnectHistory.xml";

        public static string ThemeFolder =>
            string.IsNullOrWhiteSpace(SettingsPath)
                ? string.Empty
                : Path.Combine(SettingsPath, "Themes");

        public static string InstalledThemeFolder =>
            string.IsNullOrWhiteSpace(ExePath)
                ? string.Empty
                : Path.Combine(ExePath, "Themes");

        private static string GetInstalledSettingsPath()
        {
            string configuredPath = GetConfiguredSettingsPath();
            return string.IsNullOrWhiteSpace(configuredPath) ? DefaultSettingsPath : configuredPath;
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
    }
}

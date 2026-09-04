using System;
using System.Collections.Generic;
using mRemoteNG.App;
using mRemoteNG.App.Info;
using mRemoteNG.UI.Forms;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Xml;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Messages;
using mRemoteNG.Security;
using mRemoteNG.Tools;
using mRemoteNG.UI.Controls;
using System.Runtime.Versioning;

namespace mRemoteNG.Config.Settings
{
    [SupportedOSPlatform("windows")]
    public class ExternalAppsLoader
    {
        private readonly FrmMain _mainForm;
        private readonly MessageCollector _messageCollector;
        private readonly ExternalToolsToolStrip _externalToolsToolStrip;

        public ExternalAppsLoader(FrmMain mainForm, MessageCollector messageCollector, ExternalToolsToolStrip externalToolsToolStrip)
        {
            ArgumentNullException.ThrowIfNull(mainForm);
            ArgumentNullException.ThrowIfNull(messageCollector);
            ArgumentNullException.ThrowIfNull(externalToolsToolStrip);
            _mainForm = mainForm;
            _messageCollector = messageCollector;
            _externalToolsToolStrip = externalToolsToolStrip;
        }


        /// <summary>
        /// Reads External Tools from wherever this installation keeps them. ExternalAppsSaver has
        /// always chosen between SQL and extApps.xml on UseSQLServer; loading only ever read the
        /// XML, so in SQL mode the database was a write-only mirror of the local file and every
        /// shutdown wrote that stale file back over it (#179).
        /// </summary>
        public void LoadExternalApps()
        {
            if (!Properties.OptionsDBsPage.Default.UseSQLServer)
            {
                LoadExternalAppsFromXML();
                return;
            }

            try
            {
                LoadExternalAppsFromSql();
                ApplyToolsToToolbar();
            }
            catch (Exception ex)
            {
                // An unreachable server, or a database whose schema predates tblExternalTools.
                // Drop whatever was half-read so the XML load below is the only source, rather
                // than merging two of them.
                Runtime.ExternalToolsService.ExternalTools.Clear();
                _messageCollector.AddExceptionMessage(
                    "Loading External Apps from the database failed. Falling back to extApps.xml.", ex);
                LoadExternalAppsFromXML();
            }
        }

        /// <summary>
        /// The database is authoritative in SQL mode, exactly as it is for connections: a table
        /// with no rows means no tools, not "fall back to the local file". Otherwise deleting
        /// every tool would resurrect them from an extApps.xml that SQL mode never updates.
        /// </summary>
        private void LoadExternalAppsFromSql()
        {
            using IDatabaseConnector dbConnector = DatabaseConnectorFactory.DatabaseConnectorFromSettings();
            dbConnector.Connect();

            _messageCollector.AddMessage(MessageClass.InformationMsg, "Loading External Apps from the database", true);

            foreach (ExternalTool extA in ReadExternalToolsFromSql(dbConnector))
            {
                _messageCollector.AddMessage(MessageClass.InformationMsg,
                                             $"Adding External App: {extA.DisplayName} {extA.FileName} {extA.Arguments}",
                                             true);
                Runtime.ExternalToolsService.ExternalTools.Add(extA);
            }
        }

        /// <summary>
        /// Reads tblExternalTools, the mirror image of <see cref="ExternalAppsSaver"/>'s SQL write.
        /// Public and connector-driven so a round trip through a real database can be exercised
        /// without a main window.
        /// </summary>
        public static List<ExternalTool> ReadExternalToolsFromSql(IDatabaseConnector dbConnector)
        {
            ArgumentNullException.ThrowIfNull(dbConnector);

            List<ExternalTool> tools = [];
            using DbCommand cmd = dbConnector.DbCommand("SELECT * FROM tblExternalTools");
            using DbDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                ExternalTool extA = new()
                {
                    DisplayName = ReadString(reader, "DisplayName"),
                    FileName = ReadString(reader, "FileName"),
                    IconPath = ReadString(reader, "IconPath"),
                    Arguments = ReadString(reader, "Arguments"),
                    WorkingDir = ReadString(reader, "WorkingDir"),
                    WaitForExit = ReadBool(reader, "WaitForExit"),
                    TryIntegrate = ReadBool(reader, "TryIntegrate"),
                    RunElevated = ReadBool(reader, "RunElevated"),
                    ShowOnToolbar = ReadBool(reader, "ShowOnToolbar"),
                    Category = ReadString(reader, "Category"),
                    RunOnStartup = ReadBool(reader, "RunOnStartup"),
                    StopOnShutdown = ReadBool(reader, "StopOnShutdown"),
                    Hidden = ReadBool(reader, "Hidden"),
                    AuthenticationType = ReadString(reader, "AuthType"),
                    AuthenticationUsername = ReadString(reader, "AuthUsername"),
                    AuthenticationPassword = ExternalAppsSaver.UnprotectValue(ReadString(reader, "AuthPassword")),
                    PrivateKeyFile = ReadString(reader, "PrivateKeyFile"),
                    Passphrase = ExternalAppsSaver.UnprotectValue(ReadString(reader, "Passphrase"))
                };

                int hotkey = ReadInt(reader, "Hotkey");
                if (hotkey != 0)
                    extA.Hotkey = (System.Windows.Forms.Keys)hotkey;

                tools.Add(extA);
            }

            return tools;
        }

        // A database upgraded from an older schema can be missing columns a newer build writes,
        // so read by name only when the column is actually there.
        private static bool HasColumn(DbDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string ReadString(DbDataReader reader, string columnName)
        {
            if (!HasColumn(reader, columnName))
                return string.Empty;

            object value = reader[columnName];
            return value is null or DBNull
                ? string.Empty
                : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static bool ReadBool(DbDataReader reader, string columnName)
        {
            if (!HasColumn(reader, columnName))
                return false;

            object value = reader[columnName];
            return value is not (null or DBNull) && Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }

        private static int ReadInt(DbDataReader reader, string columnName)
        {
            if (!HasColumn(reader, columnName))
                return 0;

            object value = reader[columnName];
            return value is null or DBNull ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private void ApplyToolsToToolbar()
        {
            _externalToolsToolStrip.SwitchToolBarText(Properties.Settings.Default.ExtAppsTBShowText);
            _externalToolsToolStrip.AddExternalToolsToToolBar();
        }

        public void LoadExternalAppsFromXML()
        {
            string resolvedPath = SettingsFileInfo.ExtAppsFilePath;
            bool hasCustomPath = !string.IsNullOrWhiteSpace(Properties.Settings.Default.CustomExtAppsFilePath?.Trim());
#if !PORTABLE
            string oldPath =
 Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), GeneralAppInfo.ProductName, SettingsFileInfo.ExtAppsFilesName);
#endif
            XmlDocument? xDom = null;
            bool fallbackToBuiltInShellPresets = false;

            if (File.Exists(resolvedPath))
            {
                _messageCollector.AddMessage(MessageClass.InformationMsg, $"Loading External Apps from: {resolvedPath}",
                                             true);
                xDom = SecureXmlHelper.LoadXmlFromFile(resolvedPath);
            }
#if !PORTABLE
            else if (!hasCustomPath && File.Exists(oldPath))
            {
                _messageCollector.AddMessage(MessageClass.InformationMsg, $"Loading External Apps from: {oldPath}", true);
                xDom = SecureXmlHelper.LoadXmlFromFile(oldPath);
            }
#endif
            else
            {
                _messageCollector.AddMessage(MessageClass.WarningMsg, "Loading External Apps failed: Could not FIND file! Falling back to built-in shell presets.");
                fallbackToBuiltInShellPresets = true;
            }

            if (xDom?.DocumentElement != null)
            {
                foreach (XmlElement xEl in xDom.DocumentElement.ChildNodes)
                {
                    ExternalTool extA = new()
                    {
                        DisplayName = xEl.Attributes["DisplayName"]?.Value ?? string.Empty,
                        FileName = xEl.Attributes["FileName"]?.Value ?? string.Empty,
                        IconPath = xEl.Attributes["IconPath"]?.Value ?? string.Empty,
                        Arguments = xEl.Attributes["Arguments"]?.Value ?? string.Empty
                    };

                    // check before, since old save files won't have this set
                    if (xEl.HasAttribute("WorkingDir"))
                        extA.WorkingDir = xEl.Attributes["WorkingDir"]?.Value ?? string.Empty;
                    if (xEl.HasAttribute("RunElevated"))
                        extA.RunElevated = bool.Parse(xEl.Attributes["RunElevated"]!.Value);

                    if (xEl.HasAttribute("WaitForExit"))
                    {
                        extA.WaitForExit = bool.Parse(xEl.Attributes["WaitForExit"]!.Value);
                    }

                    if (xEl.HasAttribute("TryToIntegrate"))
                    {
                        extA.TryIntegrate = bool.Parse(xEl.Attributes["TryToIntegrate"]!.Value);
                    }

                    if (xEl.HasAttribute("ShowOnToolbar"))
                    {
                        extA.ShowOnToolbar = bool.Parse(xEl.Attributes["ShowOnToolbar"]!.Value);
                    }

                    if (xEl.HasAttribute("Category"))
                        extA.Category = xEl.Attributes["Category"]?.Value ?? string.Empty;
                    if (xEl.HasAttribute("Hidden"))
                        extA.Hidden = bool.Parse(xEl.Attributes["Hidden"]!.Value);
                    if (xEl.HasAttribute("AuthType"))
                        extA.AuthenticationType = xEl.Attributes["AuthType"]?.Value ?? string.Empty;
                    if (xEl.HasAttribute("AuthUsername"))
                        extA.AuthenticationUsername = xEl.Attributes["AuthUsername"]?.Value ?? string.Empty;
                    if (xEl.HasAttribute("AuthPassword"))
                        extA.AuthenticationPassword = ExternalAppsSaver.UnprotectValue(xEl.Attributes["AuthPassword"]?.Value ?? string.Empty);
                    if (xEl.HasAttribute("PrivateKeyFile"))
                        extA.PrivateKeyFile = xEl.Attributes["PrivateKeyFile"]?.Value ?? string.Empty;
                    if (xEl.HasAttribute("Passphrase"))
                        extA.Passphrase = ExternalAppsSaver.UnprotectValue(xEl.Attributes["Passphrase"]?.Value ?? string.Empty);

                    if (xEl.HasAttribute("Hotkey") && int.TryParse(xEl.Attributes["Hotkey"]!.Value, out int hotkeyValue))
                        extA.Hotkey = (System.Windows.Forms.Keys)hotkeyValue;

                    _messageCollector.AddMessage(MessageClass.InformationMsg,
                                                 $"Adding External App: {extA.DisplayName} {extA.FileName} {extA.Arguments}",
                                                 true);
                    Runtime.ExternalToolsService.ExternalTools.Add(extA);
                }
            }
            else
            {
                if (!fallbackToBuiltInShellPresets)
                {
                    _messageCollector.AddMessage(MessageClass.WarningMsg, "Loading External Apps failed: Could not LOAD file! Falling back to built-in shell presets.");
                }
                AddBuiltInShellPresetIfMissing("cmd.exe", "%ComSpec%");
                AddBuiltInShellPresetIfMissing("pwsh.exe", "pwsh.exe");
                AddBuiltInShellPresetIfMissing("wsl.exe", @"%windir%\system32\wsl.exe");
                AddBuiltInShellPresetIfMissing("Ping", "ping.exe", "-t %HOSTNAME%");
                AddBuiltInShellPresetIfMissing("Traceroute", "tracert.exe", "%HOSTNAME%");
            }

            ApplyToolsToToolbar();
        }

        private void AddBuiltInShellPresetIfMissing(string displayName, string fileName, string arguments = "", bool tryIntegrate = true)
        {
            foreach (ExternalTool existingTool in Runtime.ExternalToolsService.ExternalTools)
            {
                if (string.Equals(existingTool.DisplayName, displayName, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            ExternalTool shellPreset = new()
            {
                DisplayName = displayName,
                FileName = fileName,
                Arguments = arguments,
                TryIntegrate = tryIntegrate,
                ShowOnToolbar = false
            };

            Runtime.ExternalToolsService.ExternalTools.Add(shellPreset);
            _messageCollector.AddMessage(MessageClass.InformationMsg,
                                         $"Adding built-in shell preset: {shellPreset.DisplayName} {shellPreset.FileName}",
                                         true);
        }
    }
}
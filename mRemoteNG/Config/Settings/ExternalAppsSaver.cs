using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Xml;
using mRemoteNG.App;
using mRemoteNG.App.Info;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Messages;
using mRemoteNG.Tools;

namespace mRemoteNG.Config.Settings
{
    [SupportedOSPlatform("windows")]
    public class ExternalAppsSaver
    {
        public void Save(IEnumerable<ExternalTool> externalTools)
        {
            if (Properties.OptionsDBsPage.Default.UseSQLServer)
            {
                SaveToSql(externalTools);
            }
            else
            {
                SaveToXml(externalTools);
            }
        }

        private void SaveToXml(IEnumerable<ExternalTool> externalTools)
        {
            try
            {
                if (Directory.Exists(SettingsFileInfo.SettingsPath) == false)
                {
                    Directory.CreateDirectory(SettingsFileInfo.SettingsPath);
                }

                XmlTextWriter xmlTextWriter =
                    new(SettingsFileInfo.SettingsPath + "\\" + SettingsFileInfo.ExtAppsFilesName,
                                      Encoding.UTF8)
                    {
                        Formatting = Formatting.Indented,
                        Indentation = 4
                    };

                xmlTextWriter.WriteStartDocument();
                xmlTextWriter.WriteStartElement("Apps");

                foreach (ExternalTool extA in externalTools)
                {
                    xmlTextWriter.WriteStartElement("App");
                    xmlTextWriter.WriteAttributeString("DisplayName", "", extA.DisplayName);
                    xmlTextWriter.WriteAttributeString("FileName", "", extA.FileName);
                    xmlTextWriter.WriteAttributeString("IconPath", "", extA.IconPath);
                    xmlTextWriter.WriteAttributeString("Arguments", "", extA.Arguments);
                    xmlTextWriter.WriteAttributeString("WorkingDir", "", extA.WorkingDir);
                    xmlTextWriter.WriteAttributeString("WaitForExit", "", Convert.ToString(extA.WaitForExit));
                    xmlTextWriter.WriteAttributeString("TryToIntegrate", "", Convert.ToString(extA.TryIntegrate));
                    xmlTextWriter.WriteAttributeString("RunElevated", "", Convert.ToString(extA.RunElevated));
                    xmlTextWriter.WriteAttributeString("ShowOnToolbar", "", Convert.ToString(extA.ShowOnToolbar));
                    xmlTextWriter.WriteAttributeString("Category", "", extA.Category);
                    if (extA.Hotkey != System.Windows.Forms.Keys.None)
                        xmlTextWriter.WriteAttributeString("Hotkey", "", Convert.ToString((int)extA.Hotkey));
                    xmlTextWriter.WriteEndElement();
                }

                xmlTextWriter.WriteEndElement();
                xmlTextWriter.WriteEndDocument();

                xmlTextWriter.Close();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("SaveExternalAppsToXML failed", ex);
            }
        }

        private void SaveToSql(IEnumerable<ExternalTool> externalTools)
        {
            try
            {
                if (Properties.OptionsDBsPage.Default.SQLReadOnly)
                {
                    Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                        "Skipping external tools save: SQL is read-only.");
                    return;
                }

                using IDatabaseConnector dbConnector = DatabaseConnectorFactory.DatabaseConnectorFromSettings();
                dbConnector.Connect();

                using DbTransaction transaction = dbConnector.DbConnection().BeginTransaction();
                try
                {
                    DbCommand cmd = dbConnector.DbCommand("DELETE FROM tblExternalTools");
                    cmd.Transaction = transaction;
                    cmd.ExecuteNonQuery();

                    foreach (ExternalTool extA in externalTools)
                    {
                        cmd = dbConnector.DbCommand(
                            "INSERT INTO tblExternalTools (DisplayName, FileName, IconPath, Arguments, WorkingDir, WaitForExit, TryIntegrate, RunElevated, ShowOnToolbar, Category, RunOnStartup, StopOnShutdown, Hotkey) " +
                            "VALUES (@DisplayName, @FileName, @IconPath, @Arguments, @WorkingDir, @WaitForExit, @TryIntegrate, @RunElevated, @ShowOnToolbar, @Category, @RunOnStartup, @StopOnShutdown, @Hotkey)");
                        cmd.Transaction = transaction;

                        AddParameter(cmd, "@DisplayName", extA.DisplayName);
                        AddParameter(cmd, "@FileName", extA.FileName);
                        AddParameter(cmd, "@IconPath", extA.IconPath);
                        AddParameter(cmd, "@Arguments", extA.Arguments);
                        AddParameter(cmd, "@WorkingDir", extA.WorkingDir);
                        AddParameter(cmd, "@WaitForExit", extA.WaitForExit);
                        AddParameter(cmd, "@TryIntegrate", extA.TryIntegrate);
                        AddParameter(cmd, "@RunElevated", extA.RunElevated);
                        AddParameter(cmd, "@ShowOnToolbar", extA.ShowOnToolbar);
                        AddParameter(cmd, "@Category", extA.Category);
                        AddParameter(cmd, "@RunOnStartup", extA.RunOnStartup);
                        AddParameter(cmd, "@StopOnShutdown", extA.StopOnShutdown);
                        AddParameter(cmd, "@Hotkey", (int)extA.Hotkey);

                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("SaveExternalAppsToSQL failed", ex);
            }
        }

        private static void AddParameter(DbCommand cmd, string name, object value)
        {
            DbParameter param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            cmd.Parameters.Add(param);
        }
    }
}

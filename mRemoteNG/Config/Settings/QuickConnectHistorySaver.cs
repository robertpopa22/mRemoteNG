using System;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Xml;
using mRemoteNG.App;
using mRemoteNG.App.Info;
using mRemoteNG.UI.Controls;

namespace mRemoteNG.Config.Settings
{
    [SupportedOSPlatform("windows")]
    public class QuickConnectHistorySaver
    {
        public void Save(QuickConnectComboBox comboBox)
        {
            try
            {
                if (!Directory.Exists(SettingsFileInfo.SettingsPath))
                    Directory.CreateDirectory(SettingsFileInfo.SettingsPath);

                string filePath = Path.Combine(SettingsFileInfo.SettingsPath, SettingsFileInfo.QuickConnectHistoryFileName);

                XmlTextWriter writer = new(filePath, Encoding.UTF8)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 4
                };

                writer.WriteStartDocument();
                writer.WriteStartElement("History");

                foreach (QuickConnectComboBox.HistoryItemData item in comboBox.GetHistoryItems())
                {
                    writer.WriteStartElement("Item");
                    writer.WriteAttributeString("Hostname", item.Hostname);
                    writer.WriteAttributeString("Port", Convert.ToString(item.Port));
                    writer.WriteAttributeString("Protocol", item.Protocol.ToString());
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("SaveQuickConnectHistory failed", ex);
            }
        }
    }
}

using System;
using System.IO;
using System.Runtime.Versioning;
using System.Xml;
using mRemoteNG.App;
using mRemoteNG.App.Info;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.UI.Controls;

namespace mRemoteNG.Config.Settings
{
    [SupportedOSPlatform("windows")]
    public class QuickConnectHistoryLoader
    {
        private readonly QuickConnectComboBox _comboBox;

        public QuickConnectHistoryLoader(QuickConnectComboBox comboBox)
        {
            _comboBox = comboBox ?? throw new ArgumentNullException(nameof(comboBox));
        }

        public void Load()
        {
            try
            {
                string filePath = Path.Combine(SettingsFileInfo.SettingsPath, SettingsFileInfo.QuickConnectHistoryFileName);
                if (!File.Exists(filePath))
                    return;

                XmlDocument doc = new();
                doc.Load(filePath);

                XmlNodeList items = doc.SelectNodes("//Item");
                if (items == null) return;

                foreach (XmlNode item in items)
                {
                    string hostname = item.Attributes?["Hostname"]?.Value ?? "";
                    if (string.IsNullOrEmpty(hostname)) continue;

                    int.TryParse(item.Attributes?["Port"]?.Value, out int port);
                    Enum.TryParse(item.Attributes?["Protocol"]?.Value, out ProtocolType protocol);

                    ConnectionInfo connectionInfo = new()
                    {
                        Hostname = hostname,
                        Protocol = protocol
                    };

                    connectionInfo.Port = port > 0 ? port : connectionInfo.GetDefaultPort();

                    _comboBox.Add(connectionInfo);
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("LoadQuickConnectHistory failed", ex);
            }
        }
    }
}

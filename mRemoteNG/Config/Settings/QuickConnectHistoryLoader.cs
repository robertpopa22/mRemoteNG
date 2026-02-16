using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Xml;
using mRemoteNG.App;
using mRemoteNG.App.Info;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Properties;
using mRemoteNG.Resources.Language;
using mRemoteNG.UI.Controls;

namespace mRemoteNG.Config.Settings
{
    [SupportedOSPlatform("windows")]
    public class QuickConnectHistoryLoader
    {
        private readonly QuickConnectComboBox _comboBox;

        private readonly struct QuickConnectHistoryItem(string hostname, int port, ProtocolType protocol, bool connected)
        {
            public string Hostname { get; } = hostname;
            public int Port { get; } = port;
            public ProtocolType Protocol { get; } = protocol;
            public bool Connected { get; } = connected;
        }

        public QuickConnectHistoryLoader(QuickConnectComboBox comboBox)
        {
            _comboBox = comboBox ?? throw new ArgumentNullException(nameof(comboBox));
        }

        public void Load()
        {
            try
            {
                foreach (QuickConnectHistoryItem item in LoadHistoryItems())
                {
                    ConnectionInfo connectionInfo = BuildConnectionInfo(item);
                    _comboBox.Add(connectionInfo);
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("LoadQuickConnectHistory failed", ex);
            }
        }

        public static IEnumerable<ConnectionInfo> LoadPreviouslyConnectedQuickConnectSessions()
        {
            try
            {
                List<ConnectionInfo> reconnectSessions = [];
                foreach (QuickConnectHistoryItem item in LoadHistoryItems())
                {
                    if (!item.Connected)
                        continue;

                    ConnectionInfo connectionInfo = BuildConnectionInfo(item);
                    connectionInfo.PleaseConnect = true;
                    reconnectSessions.Add(connectionInfo);
                }

                return reconnectSessions;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("LoadQuickConnectHistory failed", ex);
                return [];
            }
        }

        private static List<QuickConnectHistoryItem> LoadHistoryItems()
        {
            string filePath = Path.Combine(SettingsFileInfo.SettingsPath, SettingsFileInfo.QuickConnectHistoryFileName);
            if (!File.Exists(filePath))
                return [];

            XmlDocument doc = new();
            doc.Load(filePath);

            XmlNodeList? items = doc.SelectNodes("//Item");
            if (items == null)
                return [];

            List<QuickConnectHistoryItem> historyItems = [];
            foreach (XmlNode item in items)
            {
                string hostname = item.Attributes?["Hostname"]?.Value ?? string.Empty;
                if (string.IsNullOrEmpty(hostname))
                    continue;

                int.TryParse(item.Attributes?["Port"]?.Value, out int port);
                Enum.TryParse(item.Attributes?["Protocol"]?.Value, out ProtocolType protocol);
                bool.TryParse(item.Attributes?["Connected"]?.Value, out bool connected);

                historyItems.Add(new QuickConnectHistoryItem(hostname, port, protocol, connected));
            }

            return historyItems;
        }

        private static ConnectionInfo BuildConnectionInfo(QuickConnectHistoryItem item)
        {
            ConnectionInfo connectionInfo = new()
            {
                Hostname = item.Hostname,
                Protocol = item.Protocol,
                IsQuickConnect = true,
                PleaseConnect = item.Connected
            };

            connectionInfo.Name = OptionsTabsPanelsPage.Default.IdentifyQuickConnectTabs
                ? string.Format(Language.Quick, item.Hostname)
                : item.Hostname;

            connectionInfo.Port = item.Port > 0
                ? item.Port
                : connectionInfo.GetDefaultPort();

            return connectionInfo;
        }
    }
}

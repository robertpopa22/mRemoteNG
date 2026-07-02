using mRemoteNG.Tools;
using System;
using System.Windows.Forms;
using mRemoteNG.Config.Connections;
using mRemoteNG.Config.Putty;
using mRemoteNG.Properties;
using mRemoteNG.UI.Controls;
using mRemoteNG.UI.Forms;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;

// ReSharper disable ArrangeAccessorOwnerBody

namespace mRemoteNG.App
{
    [SupportedOSPlatform("windows")]
    public static class Shutdown
    {
        public static void Quit()
        {
            FrmMain.Default.Close();
            ProgramRoot.CloseSingletonInstanceMutex();
        }

        public static void Cleanup(Control quickConnectToolStrip,
                                   ExternalToolsToolStrip externalToolsToolStrip,
                                   MultiSshToolStrip multiSshToolStrip,
                                   MenuStrip mainMenu,
                                   FrmMain frmMain)
        {
            try
            {
                StopRestApi();
                StopAutoStartedExternalTools();
                StopPuttySessionWatcher();
                DisposeNotificationAreaIcon();
                SaveConnections();
                SaveSettings(quickConnectToolStrip, externalToolsToolStrip, multiSshToolStrip, mainMenu, frmMain);
                UnregisterBrowsers();
                PluginManager.Instance.ShutdownPlugins();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.SettingsCouldNotBeSavedOrTrayDispose, ex);
            }
        }

        private static void StopRestApi()
        {
            Runtime.RestApi?.Dispose();
            Runtime.RestApi = null;
        }

        private static void StopAutoStartedExternalTools()
        {
            foreach (var tool in Runtime.ExternalToolsService.ExternalTools)
            {
                if (tool.StopOnShutdown)
                    tool.StopTrackedProcess();
            }
        }

        private static void StopPuttySessionWatcher()
        {
            PuttySessionsManager.Instance.StopWatcher();
        }

        private static void DisposeNotificationAreaIcon()
        {
            if (Runtime.NotificationAreaIcon != null && Runtime.NotificationAreaIcon.Disposed == false)
                Runtime.NotificationAreaIcon.Dispose();
        }

        private static void SaveConnections()
        {
            DateTime lastUpdate;
            DateTime updateDate;
            DateTime currentDate = DateTime.Now;

            if ((Properties.OptionsBackupPage.Default.SaveConnectionsFrequency == (int)ConnectionsBackupFrequencyEnum.OnExit))
            {
                Runtime.ConnectionsService.SaveConnections();
				return;
            }	
			lastUpdate = Runtime.ConnectionsService.UsingDatabase ? Runtime.ConnectionsService.LastSqlUpdate : Runtime.ConnectionsService.LastFileUpdate;

            switch (Properties.OptionsBackupPage.Default.SaveConnectionsFrequency)
            {
                case (int)ConnectionsBackupFrequencyEnum.Daily:
                    updateDate = lastUpdate.AddDays(1);
                    break;
                case (int)ConnectionsBackupFrequencyEnum.Weekly:
                    updateDate = lastUpdate.AddDays(7);
                    break;
                default:
                    return;
            }

            if (currentDate >= updateDate)
            {
                Runtime.ConnectionsService.SaveConnections();
            }
        }

        private static void SaveSettings(Control quickConnectToolStrip,
                                         ExternalToolsToolStrip externalToolsToolStrip,
                                         MultiSshToolStrip multiSshToolStrip,
                                         MenuStrip mainMenu,
                                         FrmMain frmMain)
        {
            Config.Settings.SettingsSaver.SaveSettings(quickConnectToolStrip, externalToolsToolStrip, multiSshToolStrip,
                                                       mainMenu, frmMain);
        }

        private static void UnregisterBrowsers()
        {
            IeBrowserEmulation.Unregister();
        }
    }
}
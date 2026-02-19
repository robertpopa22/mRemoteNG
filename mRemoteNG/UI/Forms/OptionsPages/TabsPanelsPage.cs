using mRemoteNG.App;
using mRemoteNG.Config.Settings.Registry;
using mRemoteNG.Properties;
using mRemoteNG.Resources.Language;
using System;
using System.Runtime.Versioning;

namespace mRemoteNG.UI.Forms.OptionsPages
{
    [SupportedOSPlatform("windows")]
    public sealed partial class TabsPanelsPage
    {
        #region Private Fields

        private OptRegistryTabsPanelsPage? pageRegSettingsInstance;

        #endregion

        public TabsPanelsPage()
        {
            InitializeComponent();
            ApplyTheme();
            PageIcon = Resources.ImageConverter.GetImageAsIcon(Properties.Resources.Tab_16x);
        }

        public override string PageName
        {
            get => Language.TabsAndPanels.Replace("&&", "&");
            set { }
        }

        public override void ApplyLanguage()
        {
            base.ApplyLanguage();

            chkAlwaysShowPanelTabs.Text = Language.AlwaysShowPanelTabs;
            chkAlwaysShowConnectionTabs.Text = Language.AlwaysShowConnectionTabs;
            chkOpenNewTabRightOfSelected.Text = Language.OpenNewTabRight;
            chkShowLogonInfoOnTabs.Text = Language.ShowLogonInfoOnTabs;
            chkShowProtocolOnTabs.Text = Language.ShowProtocolOnTabs;
            chkShowFolderPathOnTabs.Text = Language.ShowFolderPathOnTabs;
            chkIdentifyQuickConnectTabs.Text = Language.IdentifyQuickConnectTabs;
            chkDoubleClickClosesTab.Text = Language.DoubleClickTabClosesIt;
            chkAlwaysShowPanelSelectionDlg.Text = Language.AlwaysShowPanelSelection;
            chkCreateEmptyPanelOnStart.Text = Language.CreateEmptyPanelOnStartUp;
            chkBindConnectionsAndConfigPanels.Text = "Bind Connections and Config panels together when auto-hidden";
            chkLockPanels.Text = "Lock panels";
            lblPanelName.Text = $@"{Language.PanelName}:";
            lblSplitterSize.Text = "Splitter size:";

            lblRegistrySettingsUsedInfo.Text = Language.OptionsCompanyPolicyMessage;
        }

        public override void LoadSettings()
        {
            chkAlwaysShowPanelTabs.Checked = Properties.OptionsTabsPanelsPage.Default.AlwaysShowPanelTabs;
            chkAlwaysShowConnectionTabs.Checked = Properties.OptionsTabsPanelsPage.Default.AlwaysShowConnectionTabs;

            /* 
             * Comment added: June 16, 2024
             * Properties.OptionsTabsPanelsPage.Default.OpenTabsRightOfSelected nerver used
             *  Set Visible = false
            */
            //chkOpenNewTabRightOfSelected.Checked = Properties.OptionsTabsPanelsPage.Default.OpenTabsRightOfSelected;
            chkOpenNewTabRightOfSelected.Visible = false;

            chkShowLogonInfoOnTabs.Checked = Properties.OptionsTabsPanelsPage.Default.ShowLogonInfoOnTabs;
            chkShowProtocolOnTabs.Checked = Properties.OptionsTabsPanelsPage.Default.ShowProtocolOnTabs;
            chkShowFolderPathOnTabs.Checked = Properties.OptionsTabsPanelsPage.Default.ShowFolderPathOnTabs;
            chkIdentifyQuickConnectTabs.Checked = Properties.OptionsTabsPanelsPage.Default.IdentifyQuickConnectTabs;
            chkDoubleClickClosesTab.Checked = Properties.OptionsTabsPanelsPage.Default.DoubleClickOnTabClosesIt;
            chkAlwaysShowPanelSelectionDlg.Checked = Properties.OptionsTabsPanelsPage.Default.AlwaysShowPanelSelectionDlg;
            chkCreateEmptyPanelOnStart.Checked = Properties.OptionsTabsPanelsPage.Default.CreateEmptyPanelOnStartUp;
            chkBindConnectionsAndConfigPanels.Checked = Properties.OptionsTabsPanelsPage.Default.BindConnectionsAndConfigPanels;
            chkLockPanels.Checked = Properties.OptionsTabsPanelsPage.Default.LockPanels;
            txtBoxPanelName.Text = Properties.OptionsTabsPanelsPage.Default.StartUpPanelName;
            nudSplitterSize.Value = Properties.OptionsTabsPanelsPage.Default.SplitterSize;
            UpdatePanelNameTextBox();
        }

        public override void SaveSettings()
        {
            base.SaveSettings();

            Properties.OptionsTabsPanelsPage.Default.AlwaysShowPanelTabs = chkAlwaysShowPanelTabs.Checked;
            Properties.OptionsTabsPanelsPage.Default.AlwaysShowConnectionTabs = chkAlwaysShowConnectionTabs.Checked;

            // Defer the ShowHidePanelTabs call to avoid corrupting the Options window
            // This ensures the call happens after the Options window is closed
            if (FrmMain.IsCreated)
            {
                FrmMain.Default.BeginInvoke(new System.Windows.Forms.MethodInvoker(() =>
                {
                    FrmMain.Default.ShowHidePanelTabs();
                    FrmMain.Default.ShowHideConnectionTabs();
                    FrmMain.Default.SetPanelLock();
                }));
            }

            /* 
             * Comment added: June 16, 2024
             * Properties.OptionsTabsPanelsPage.Default.OpenTabsRightOfSelected nerver used
            */
            //Properties.OptionsTabsPanelsPage.Default.OpenTabsRightOfSelected = chkOpenNewTabRightOfSelected.Checked;

            Properties.OptionsTabsPanelsPage.Default.ShowLogonInfoOnTabs = chkShowLogonInfoOnTabs.Checked;
            Properties.OptionsTabsPanelsPage.Default.ShowProtocolOnTabs = chkShowProtocolOnTabs.Checked;
            Properties.OptionsTabsPanelsPage.Default.ShowFolderPathOnTabs = chkShowFolderPathOnTabs.Checked;
            Properties.OptionsTabsPanelsPage.Default.IdentifyQuickConnectTabs = chkIdentifyQuickConnectTabs.Checked;
            Properties.OptionsTabsPanelsPage.Default.DoubleClickOnTabClosesIt = chkDoubleClickClosesTab.Checked;
            Properties.OptionsTabsPanelsPage.Default.AlwaysShowPanelSelectionDlg = chkAlwaysShowPanelSelectionDlg.Checked;
            Properties.OptionsTabsPanelsPage.Default.CreateEmptyPanelOnStartUp = chkCreateEmptyPanelOnStart.Checked;
            Properties.OptionsTabsPanelsPage.Default.BindConnectionsAndConfigPanels = chkBindConnectionsAndConfigPanels.Checked;
            Properties.OptionsTabsPanelsPage.Default.LockPanels = chkLockPanels.Checked;
            Properties.OptionsTabsPanelsPage.Default.StartUpPanelName = txtBoxPanelName.Text;
            Properties.OptionsTabsPanelsPage.Default.SplitterSize = (int)nudSplitterSize.Value;
        }

        public override void LoadRegistrySettings()
        {
            Type settingsType = typeof(OptRegistryTabsPanelsPage);
            RegistryLoader.RegistrySettings.TryGetValue(settingsType, out var settings);
            pageRegSettingsInstance = settings as OptRegistryTabsPanelsPage;

            // If registry settings don't exist, create a default instance to prevent null reference exceptions
            if (pageRegSettingsInstance == null)
            {
                pageRegSettingsInstance = new OptRegistryTabsPanelsPage();
                Logger.Instance.Log?.Debug("[TabsPanelsPage.LoadRegistrySettings] pageRegSettingsInstance was null, created default instance");
            }

            RegistryLoader.Cleanup(settingsType);

            // ***
            // Disable controls based on the registry settings.
            //
            if (pageRegSettingsInstance.AlwaysShowPanelTabs.IsSet)
                DisableControl(chkAlwaysShowPanelTabs);

            if (pageRegSettingsInstance.ShowLogonInfoOnTabs.IsSet)
                DisableControl(chkShowLogonInfoOnTabs);

            if (pageRegSettingsInstance.ShowProtocolOnTabs.IsSet)
                DisableControl(chkShowProtocolOnTabs);

            if (pageRegSettingsInstance.IdentifyQuickConnectTabs.IsSet)
                DisableControl(chkIdentifyQuickConnectTabs);

            if (pageRegSettingsInstance.DoubleClickOnTabClosesIt.IsSet)
                DisableControl(chkDoubleClickClosesTab);

            if (pageRegSettingsInstance.AlwaysShowPanelSelectionDlg.IsSet)
                DisableControl(chkAlwaysShowPanelSelectionDlg);

            if (pageRegSettingsInstance.CreateEmptyPanelOnStartUp.IsSet)
                DisableControl(chkCreateEmptyPanelOnStart);

            if (pageRegSettingsInstance.StartUpPanelName.IsSet)
                DisableControl(txtBoxPanelName);

            if (pageRegSettingsInstance.BindConnectionsAndConfigPanels.IsSet)
                DisableControl(chkBindConnectionsAndConfigPanels);

            // Updates the visibility of the information label indicating whether registry settings are used.
            lblRegistrySettingsUsedInfo.Visible = ShowRegistrySettingsUsedInfo();
        }

        /// <summary>
        /// Checks if specific registry settings related to appearence page are used.
        /// </summary>
        private bool ShowRegistrySettingsUsedInfo()
        {
            if (pageRegSettingsInstance == null)
                return false;

            return pageRegSettingsInstance.AlwaysShowPanelTabs.IsSet
                || pageRegSettingsInstance.ShowLogonInfoOnTabs.IsSet
                || pageRegSettingsInstance.ShowProtocolOnTabs.IsSet
                || pageRegSettingsInstance.IdentifyQuickConnectTabs.IsSet
                || pageRegSettingsInstance.DoubleClickOnTabClosesIt.IsSet
                || pageRegSettingsInstance.AlwaysShowPanelSelectionDlg.IsSet
                || pageRegSettingsInstance.CreateEmptyPanelOnStartUp.IsSet
                || pageRegSettingsInstance.StartUpPanelName.IsSet
                || pageRegSettingsInstance.BindConnectionsAndConfigPanels.IsSet;
        }

        private void UpdatePanelNameTextBox()
        {
            if (pageRegSettingsInstance == null || !pageRegSettingsInstance.StartUpPanelName.IsSet)
                txtBoxPanelName.Enabled = chkCreateEmptyPanelOnStart.Checked;
        }

        private void chkCreateEmptyPanelOnStart_CheckedChanged(object sender, System.EventArgs e)
        {
            UpdatePanelNameTextBox();
        }
    }
}
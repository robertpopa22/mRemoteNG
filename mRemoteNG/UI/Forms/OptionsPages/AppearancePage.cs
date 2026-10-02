using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Properties;
using mRemoteNG.Tools;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;
using mRemoteNG.Config.Settings.Registry;
using mRemoteNG.Themes;

namespace mRemoteNG.UI.Forms.OptionsPages
{
    [SupportedOSPlatform("windows")]
    public sealed partial class AppearancePage
    {
        private OptRegistryAppearancePage? pageRegSettingsInstance;
        private string? _pendingFontFamily;
        private float? _pendingFontSize;
        private FontStyle? _pendingFontStyle;
        public AppearancePage()
        {
            InitializeComponent();
            btnChooseInterfaceFont.Click += btnChooseInterfaceFont_Click;
            ApplyTheme();
            PageIcon = Resources.ImageConverter.GetImageAsIcon(Properties.Resources.Panel_16x);
        }

        public override string PageName
        {
            get => Language.Appearance;
            set { }
        }

        public override void ApplyLanguage()
        {
            base.ApplyLanguage();

            lblLanguage.Text = Language.LanguageString;
            lblLanguageRestartRequired.Text =
                string.Format(CultureInfo.CurrentCulture, Language.LanguageRestartRequired, Application.ProductName);
            chkShowDescriptionTooltipsInTree.Text = Language.ShowDescriptionTooltips;
            chkShowFullConnectionsFilePathInTitle.Text = Language.ShowFullConsFilePath;
            chkShowSystemTrayIcon.Text = Language.AlwaysShowSysTrayIcon;
            chkLockWindowSize.Text = Language.LockWindowSize;
            chkMinimizeToSystemTray.Text = Language.MinimizeToSysTray;
            chkCloseToSystemTray.Text = Language.CloseToSysTray;
            lblRegistrySettingsUsedInfo.Text = Language.OptionsCompanyPolicyMessage;
            btnChooseInterfaceFont.Text = Language.ChooseInterfaceFont;
            ShowInterfaceFont();
        }

        public override void LoadSettings()
        {
            cboLanguage.Items.Clear();
            cboLanguage.Items.Add(Language.LanguageDefault);

            foreach (string nativeName in SupportedCultures.CultureNativeNames)
            {
                cboLanguage.Items.Add(nativeName);
            }

            if (!string.IsNullOrEmpty(Settings.Default.OverrideUICulture) &&
                SupportedCultures.IsNameSupported(Settings.Default.OverrideUICulture))
            {
                cboLanguage.SelectedItem = SupportedCultures.GetCultureNativeName(Settings.Default.OverrideUICulture);
            }

            if (cboLanguage.SelectedIndex == -1)
            {
                cboLanguage.SelectedIndex = 0;
            }

            chkShowDescriptionTooltipsInTree.Checked = Properties.OptionsAppearancePage.Default.ShowDescriptionTooltipsInTree;
            chkShowFullConnectionsFilePathInTitle.Checked = Properties.OptionsAppearancePage.Default.ShowCompleteConsPathInTitle;
            chkReplaceIconOnConnect.Checked = Properties.OptionsAppearancePage.Default.ReplaceIconOnConnect;
            chkBoldActiveConnections.Checked = Properties.OptionsAppearancePage.Default.BoldActiveConnections;
            chkLockWindowSize.Checked = Settings.Default.LockWindowSize;
            chkShowSystemTrayIcon.Checked = Properties.OptionsAppearancePage.Default.ShowSystemTrayIcon;
            chkMinimizeToSystemTray.Checked = Properties.OptionsAppearancePage.Default.MinimizeToTray;
            chkCloseToSystemTray.Checked = Properties.OptionsAppearancePage.Default.CloseToTray;
            ShowInterfaceFont();
        }

        public override void SaveSettings()
        {
            var selectedItemStr = Convert.ToString(cboLanguage.SelectedItem, CultureInfo.InvariantCulture) ?? string.Empty;
            if (cboLanguage.SelectedIndex > 0 &&
                SupportedCultures.IsNativeNameSupported(selectedItemStr))
            {
                Settings.Default.OverrideUICulture = SupportedCultures.GetCultureName(selectedItemStr);
            }
            else
            {
                Settings.Default.OverrideUICulture = string.Empty;
            }

            Properties.OptionsAppearancePage.Default.ShowDescriptionTooltipsInTree = chkShowDescriptionTooltipsInTree.Checked;
            Properties.OptionsAppearancePage.Default.ShowCompleteConsPathInTitle = chkShowFullConnectionsFilePathInTitle.Checked;
            if (FrmMain.IsCreated)
                FrmMain.Default.ShowFullPathInTitle = chkShowFullConnectionsFilePathInTitle.Checked;

            Settings.Default.LockWindowSize = chkLockWindowSize.Checked;

            Properties.OptionsAppearancePage.Default.ShowSystemTrayIcon = chkShowSystemTrayIcon.Checked;
            if (Properties.OptionsAppearancePage.Default.ShowSystemTrayIcon)
            {
                if (Runtime.NotificationAreaIcon == null)
                {
                    Runtime.NotificationAreaIcon = new NotificationAreaIcon();
                }
            }
            else
            {
                if (Runtime.NotificationAreaIcon != null)
                {
                    Runtime.NotificationAreaIcon.Dispose();
                    Runtime.NotificationAreaIcon = null;
                }
            }

            Properties.OptionsAppearancePage.Default.MinimizeToTray = chkMinimizeToSystemTray.Checked;
            Properties.OptionsAppearancePage.Default.CloseToTray = chkCloseToSystemTray.Checked;

            Properties.OptionsAppearancePage.Default.ReplaceIconOnConnect = chkReplaceIconOnConnect.Checked;
            Properties.OptionsAppearancePage.Default.BoldActiveConnections = chkBoldActiveConnections.Checked;
            SaveInterfaceFont();
        }

        private void ShowInterfaceFont()
        {
            using Font font = InterfaceFont.Create(
                _pendingFontFamily ?? Properties.OptionsAppearancePage.Default.UIFontFamily,
                _pendingFontSize ?? Properties.OptionsAppearancePage.Default.UIFontSize,
                _pendingFontStyle ?? (FontStyle)Properties.OptionsAppearancePage.Default.UIFontStyle);
            lblInterfaceFont.Text = Language.InterfaceFont + ": " + InterfaceFont.Describe(font);
        }

        private void btnChooseInterfaceFont_Click(object? sender, EventArgs e)
        {
            using Font current = InterfaceFont.Create(
                _pendingFontFamily ?? Properties.OptionsAppearancePage.Default.UIFontFamily,
                _pendingFontSize ?? Properties.OptionsAppearancePage.Default.UIFontSize,
                _pendingFontStyle ?? (FontStyle)Properties.OptionsAppearancePage.Default.UIFontStyle);
            using FontDialog dialog = new()
            {
                Font = current,
                ShowEffects = false,
                MinSize = (int)InterfaceFont.MinSize,
                MaxSize = (int)InterfaceFont.MaxSize,
                AllowScriptChange = false
            };
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            _pendingFontFamily = dialog.Font.FontFamily.Name;
            _pendingFontSize = dialog.Font.SizeInPoints;
            _pendingFontStyle = InterfaceFont.AllowedStyle(dialog.Font.Style);
            ShowInterfaceFont();
        }

        private void SaveInterfaceFont()
        {
            if (_pendingFontFamily == null || _pendingFontSize == null || _pendingFontStyle == null)
                return;

            Properties.OptionsAppearancePage.Default.UIFontFamily = _pendingFontFamily;
            Properties.OptionsAppearancePage.Default.UIFontSize = _pendingFontSize.Value;
            Properties.OptionsAppearancePage.Default.UIFontStyle = (int)InterfaceFont.AllowedStyle(_pendingFontStyle.Value);
            InterfaceFont.ApplyToOpenForms();
        }

        public override void LoadRegistrySettings()
        {
            Type settingsType = typeof(OptRegistryAppearancePage);
            RegistryLoader.RegistrySettings.TryGetValue(settingsType, out var settings);
            pageRegSettingsInstance = settings as OptRegistryAppearancePage;

            // If registry settings don't exist, create a default instance to prevent null reference exceptions
            if (pageRegSettingsInstance == null)
            {
                pageRegSettingsInstance = new OptRegistryAppearancePage();
                Logger.Instance.Log?.Debug("[AppearancePage.LoadRegistrySettings] pageRegSettingsInstance was null, created default instance");
            }

            RegistryLoader.Cleanup(settingsType);

            // ***
            // Disable controls based on the registry settings.
            //
            if (pageRegSettingsInstance.ShowDescriptionTooltipsInConTree.IsSet)
                DisableControl(chkShowDescriptionTooltipsInTree);

            if (pageRegSettingsInstance.ShowCompleteConFilePathInTitle.IsSet)
                DisableControl(chkShowFullConnectionsFilePathInTitle);

            if (pageRegSettingsInstance.AlwaysShowSystemTrayIcon.IsSet)
                DisableControl(chkShowSystemTrayIcon);

            if (pageRegSettingsInstance.MinimizeToTray.IsSet)
                DisableControl(chkMinimizeToSystemTray);

            if (pageRegSettingsInstance.CloseToTray.IsSet)
                DisableControl(chkCloseToSystemTray);

            // Updates the visibility of the information label indicating whether registry settings are used.
            lblRegistrySettingsUsedInfo.Visible = ShowRegistrySettingsUsedInfo();
        }

        /// <summary>
        /// Checks if specific registry settings related to appearence page are used.
        /// </summary>
        public bool ShowRegistrySettingsUsedInfo()
        {
            return pageRegSettingsInstance != null
                && (pageRegSettingsInstance.ShowDescriptionTooltipsInConTree.IsSet
                || pageRegSettingsInstance.ShowCompleteConFilePathInTitle.IsSet
                || pageRegSettingsInstance.AlwaysShowSystemTrayIcon.IsSet
                || pageRegSettingsInstance.MinimizeToTray.IsSet
                || pageRegSettingsInstance.CloseToTray.IsSet);
        }
    }
}
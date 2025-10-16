#region  Usings
using mRemoteNG.UI.Forms.OptionsPages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using mRemoteNG.Themes;
using System.Configuration;
using mRemoteNG.Properties;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;
#endregion

namespace mRemoteNG.UI.Forms
{
    [SupportedOSPlatform("windows")]
    public partial class FrmOptions : Form
    {
        private int _currentIndex = 0;
        private readonly List<OptionsPage> _optionPages = [];
        private string _pageName;
        private readonly DisplayProperties _display = new();
        private readonly List<string> _optionPageObjectNames;
        private bool _isLoading = true;

        public FrmOptions() : this(Language.StartupExit)
        {
        }

        private FrmOptions(string pageName)
        {
            Cursor.Current = Cursors.WaitCursor;
            Application.DoEvents();
            InitializeComponent();
            Icon = Resources.ImageConverter.GetImageAsIcon(Properties.Resources.Settings_16x);
            _pageName = pageName;
            Cursor.Current = Cursors.Default;

            _optionPageObjectNames =
            [
                nameof(StartupExitPage),
                nameof(AppearancePage),
                nameof(ConnectionsPage),
                nameof(TabsPanelsPage),
                nameof(NotificationsPage),
                nameof(CredentialsPage),
                nameof(SqlServerPage),
                nameof(UpdatesPage),
                nameof(ThemePage),
                nameof(SecurityPage),
                nameof(AdvancedPage),
                nameof(BackupPage)
            ];

            InitOptionsPagesToListView();
        }

        private void FrmOptions_Load(object sender, EventArgs e)
        {
            this.Visible = true;
            FontOverrider.FontOverride(this);
            SetActivatedPage();
            //ApplyLanguage();
            // Handle the main page here and the individual pages in
            // AddOptionsPagesToListView()  -- one less foreach loop....
            Text = Language.OptionsPageTitle;
            btnOK.Text = Language._Ok;
            btnCancel.Text = Language._Cancel;
            btnApply.Text = Language.Apply;
            //ApplyTheme();
            //ThemeManager.getInstance().ThemeChanged += ApplyTheme;
            lstOptionPages.SelectedIndexChanged += LstOptionPages_SelectedIndexChanged;
            lstOptionPages.SelectedIndex = 0;
        }

        private void ApplyTheme()
        {
            if (!ThemeManager.getInstance().ActiveAndExtended) return;
            BackColor = ThemeManager.getInstance().ActiveTheme.ExtendedPalette.getColor("Dialog_Background");
            ForeColor = ThemeManager.getInstance().ActiveTheme.ExtendedPalette.getColor("Dialog_Foreground");
        }

#if false
        private void ApplyLanguage()
        {
            Text = Language.OptionsPageTitle;
            foreach (var optionPage in _pages.Values)
            {
                optionPage.ApplyLanguage();
            }
        }
#endif

        private void InitOptionsPagesToListView()
        {
            lstOptionPages.RowHeight = _display.ScaleHeight(lstOptionPages.RowHeight);
            lstOptionPages.AllColumns.First().ImageGetter = ImageGetter;

            InitOptionsPage(_optionPageObjectNames[_currentIndex++]);
            Application.Idle += new EventHandler(Application_Idle);
        }

        private void Application_Idle(object sender, EventArgs e)
        {
            if (_currentIndex >= _optionPageObjectNames.Count)
            {
                Application.Idle -= new EventHandler(Application_Idle);
                // All pages loaded, now start tracking changes
                _isLoading = false;
            }
            else
            {
                InitOptionsPage(_optionPageObjectNames[_currentIndex++]);
            }
        }

        private void InitOptionsPage(string pageName)
        {
            OptionsPage page = null;

            switch (pageName)
            {
                case "StartupExitPage":
                    {
                        if (Properties.OptionsStartupExitPage.Default.cbStartupExitPageInOptionMenu ||
                            Properties.OptionsRbac.Default.ActiveRole == "AdminRole")
                            page = new StartupExitPage { Dock = DockStyle.Fill };
                        break;
                    }
                case "AppearancePage":
                    {
                        if (Properties.OptionsAppearancePage.Default.cbAppearancePageInOptionMenu ||
                            Properties.OptionsRbac.Default.ActiveRole == "AdminRole")
                            page = new AppearancePage { Dock = DockStyle.Fill };
                        break;
                    }
                case "ConnectionsPage":
                    {
                        if (Properties.OptionsConnectionsPage.Default.cbConnectionsPageInOptionMenu ||
                            Properties.OptionsRbac.Default.ActiveRole == "AdminRole")
                            page = new ConnectionsPage { Dock = DockStyle.Fill };
                        break;
                    }
                case "TabsPanelsPage":
                    {
                        if (Properties.OptionsTabsPanelsPage.Default.cbTabsPanelsPageInOptionMenu ||
                            Properties.OptionsRbac.Default.ActiveRole == "AdminRole")
                            page = new TabsPanelsPage { Dock = DockStyle.Fill };
                        break;
                    }
                case "NotificationsPage":
                    {
                        if (Properties.OptionsNotificationsPage.Default.cbNotificationsPageInOptionMenu ||
                            Properties.OptionsRbac.Default.ActiveRole == "AdminRole")
                            page = new NotificationsPage { Dock = DockStyle.Fill };
                        break;
                    }
                case "CredentialsPage":
                    {
                        if (Properties.OptionsCredentialsPage.Default.cbCredentialsPageInOptionMenu ||
                            Properties.OptionsRbac.Default.ActiveRole == "AdminRole")
                            page = new CredentialsPage { Dock = DockStyle.Fill };
                        break;
                    }
                case "SqlServerPage":
                    {
                        if (Properties.OptionsDBsPage.Default.cbDBsPageInOptionMenu ||
                            Properties.OptionsRbac.Default.ActiveRole == "AdminRole")
                            page = new SqlServerPage { Dock = DockStyle.Fill };
                        break;
                    }
                case "UpdatesPage":
                    {
                        if (Properties.OptionsUpdatesPage.Default.cbUpdatesPageInOptionMenu ||
                            Properties.OptionsRbac.Default.ActiveRole == "AdminRole")
                            page = new UpdatesPage { Dock = DockStyle.Fill };
                        break;
                    }
                case "ThemePage":
                    {
                        if (Properties.OptionsThemePage.Default.cbThemePageInOptionMenu ||
                            Properties.OptionsRbac.Default.ActiveRole == "AdminRole")
                            page = new ThemePage { Dock = DockStyle.Fill };
                        break;
                    }
                case "SecurityPage":
                    {
                        if (Properties.OptionsSecurityPage.Default.cbSecurityPageInOptionMenu ||
                            Properties.OptionsRbac.Default.ActiveRole == "AdminRole")
                            page = new SecurityPage { Dock = DockStyle.Fill };
                        break;
                    }
                case "AdvancedPage":
                    {
                        if (Properties.OptionsAdvancedPage.Default.cbAdvancedPageInOptionMenu ||
                            Properties.OptionsRbac.Default.ActiveRole == "AdminRole")
                            page = new AdvancedPage { Dock = DockStyle.Fill };
                        break;
                    }
                case "BackupPage":
                    {
                        if (Properties.OptionsBackupPage.Default.cbBacupPageInOptionMenu ||
                            Properties.OptionsRbac.Default.ActiveRole == "AdminRole")
                            page = new BackupPage { Dock = DockStyle.Fill };
                        break;
                    }
            }

            if (page == null) return;
            page.ApplyLanguage();
            page.LoadRegistrySettings();
            page.LoadSettings();
            _optionPages.Add(page);
            lstOptionPages.AddObject(page);
            
            // Track changes in all controls on the page
            TrackChangesInControls(page);
        }

        private object ImageGetter(object rowobject)
        {
            OptionsPage page = rowobject as OptionsPage;
            return page?.PageIcon == null ? _display.ScaleImage(Properties.Resources.F1Help_16x) : _display.ScaleImage(page.PageIcon);
        }

        public void SetActivatedPage(string pageName = default)
        {
            _pageName = pageName ?? Language.StartupExit;

            bool isSet = false;
            for (int i = 0; i < lstOptionPages.Items.Count; i++)
            {
                if (!lstOptionPages.Items[i].Text.Equals(_pageName)) continue;
                lstOptionPages.Items[i].Selected = true;
                isSet = true;
                break;
            }

            if (!isSet)
                lstOptionPages.Items[0].Selected = true;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            SaveOptions();
            // Clear change flags after saving
            ClearChangeFlags();
            this.Visible = false;
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            SaveOptions();
            // Clear change flags after applying
            ClearChangeFlags();
        }

        private void SaveOptions()
        {
            foreach (OptionsPage page in _optionPages)
            {
                Debug.WriteLine(page.PageName);
                page.SaveSettings();
            }

            Debug.WriteLine((ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)).FilePath);
            Settings.Default.Save();
        }

        private void LstOptionPages_SelectedIndexChanged(object sender, EventArgs e)
        {
            pnlMain.Controls.Clear();

            OptionsPage page = (OptionsPage)lstOptionPages.SelectedObject;
            if (page != null)
                pnlMain.Controls.Add(page);
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            // When Cancel is clicked, we don't check for changes
            // The user explicitly wants to cancel
            this.Visible = false;
        }

        private void FrmOptions_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Check if any page has unsaved changes
            bool hasChanges = _optionPages.Any(page => page.HasChanges);
            
            if (hasChanges)
            {
                DialogResult result = MessageBox.Show(
                    Language.SaveOptionsBeforeClosing,
                    Language.Options,
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);
                
                switch (result)
                {
                    case DialogResult.Yes:
                        SaveOptions();
                        ClearChangeFlags();
                        e.Cancel = true;
                        this.Visible = false;
                        break;
                    case DialogResult.No:
                        // Discard changes
                        ClearChangeFlags();
                        e.Cancel = true;
                        this.Visible = false;
                        break;
                    case DialogResult.Cancel:
                        // Cancel closing - keep the dialog open
                        e.Cancel = true;
                        break;
                }
            }
            else
            {
                e.Cancel = true;
                this.Visible = false;
            }
        }

        private void TrackChangesInControls(Control control)
        {
            foreach (Control childControl in control.Controls)
            {
                // Track changes for common input controls
                if (childControl is TextBox textBox)
                {
                    textBox.TextChanged += (s, e) => MarkPageAsChanged(control);
                }
                else if (childControl is CheckBox checkBox)
                {
                    checkBox.CheckedChanged += (s, e) => MarkPageAsChanged(control);
                }
                else if (childControl is RadioButton radioButton)
                {
                    radioButton.CheckedChanged += (s, e) => MarkPageAsChanged(control);
                }
                else if (childControl is ComboBox comboBox)
                {
                    comboBox.SelectedIndexChanged += (s, e) => MarkPageAsChanged(control);
                }
                else if (childControl is NumericUpDown numericUpDown)
                {
                    numericUpDown.ValueChanged += (s, e) => MarkPageAsChanged(control);
                }
                else if (childControl is ListBox listBox)
                {
                    listBox.SelectedIndexChanged += (s, e) => MarkPageAsChanged(control);
                }
                
                // Recursively track changes in nested controls
                if (childControl.Controls.Count > 0)
                {
                    TrackChangesInControls(childControl);
                }
            }
        }

        private void MarkPageAsChanged(Control control)
        {
            // Don't track changes during initial loading
            if (_isLoading) return;
            
            // Find the parent OptionsPage
            Control current = control;
            while (current != null && !(current is OptionsPage))
            {
                current = current.Parent;
            }
            
            if (current is OptionsPage page)
            {
                page.HasChanges = true;
            }
        }

        private void ClearChangeFlags()
        {
            foreach (OptionsPage page in _optionPages)
            {
                page.HasChanges = false;
            }
        }
    }
}
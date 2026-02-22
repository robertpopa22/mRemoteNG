#region  Usings
using mRemoteNG.App;
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
        private readonly List<OptionsPage> _optionPages = [];
        private string _pageName;
        private readonly DisplayProperties _display = new();
        private readonly List<string> _optionPageObjectNames;
        private bool _isLoading = true;
        private bool _isInitialized = false;
        private bool _isHandlingSelectionChange = false; // Guard flag to prevent recursive event handling

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
                nameof(BackupPage),
                nameof(ConfigurationPage)
            ];

            InitOptionsPagesToListView();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose all option pages to prevent resource leaks (GDI handles, etc.)
                // This is critical as inactive pages are not in the Controls collection
                // and would otherwise not be disposed.
                foreach (var page in _optionPages)
                {
                    if (page != null && !page.IsDisposed)
                    {
                        page.Dispose();
                    }
                }
                _optionPages.Clear();

                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void FrmOptions_Load(object sender, EventArgs e)
        {
            Logger.Instance.Log?.Debug($"[FrmOptions_Load] START - IsInitialized: {_isInitialized}, Visible: {this.Visible}, Handle: {this.Handle}");

            // Only initialize once to prevent multiple event subscriptions and page reloading
            if (_isInitialized)
            {
                Logger.Instance.Log?.Debug($"[FrmOptions_Load] Already initialized - calling ValidateControlState");
                // On subsequent loads, validate and recover control state if needed
                ValidateControlState();
                this.Visible = true;
                Logger.Instance.Log?.Debug($"[FrmOptions_Load] END (already initialized)");
                return;
            }

            Logger.Instance.Log?.Debug($"[FrmOptions_Load] First initialization");
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
            Logger.Instance.Log?.Debug($"[FrmOptions_Load] Selected index set to 0");

            // Mark as initialized
            _isInitialized = true;
            Logger.Instance.Log?.Debug($"[FrmOptions_Load] END (first initialization complete)");
        }

        private void ApplyTheme()
        {
            var themeManager = ThemeManager.getInstance();
            if (!themeManager.ActiveAndExtended) return;
            BackColor = themeManager.ActiveTheme.ExtendedPalette?.getColor("Dialog_Background") ?? BackColor;
            ForeColor = themeManager.ActiveTheme.ExtendedPalette?.getColor("Dialog_Foreground") ?? ForeColor;
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
            Logger.Instance.Log?.Debug($"[InitOptionsPagesToListView] START - Loading {_optionPageObjectNames.Count} pages");

            lstOptionPages.RowHeight = _display.ScaleHeight(lstOptionPages.RowHeight);
            lstOptionPages.AllColumns.First().ImageGetter = ImageGetter;

            // Suspend layout to prevent flickering during batch loading
            lstOptionPages.BeginUpdate();
            try
            {
                // Load all pages synchronously for faster, more responsive loading
                // This is especially important when the form is recreated (second+ open)
                foreach (var pageName in _optionPageObjectNames)
                {
                    Logger.Instance.Log?.Debug($"[InitOptionsPagesToListView] Loading page: {pageName}");
                    InitOptionsPage(pageName);
                }

                // All pages loaded, now start tracking changes
                _isLoading = false;
                Logger.Instance.Log?.Debug($"[InitOptionsPagesToListView] All {_optionPageObjectNames.Count} pages loaded");
            }
            finally
            {
                lstOptionPages.EndUpdate();
            }

            Logger.Instance.Log?.Debug($"[InitOptionsPagesToListView] END");
        }

        private void InitOptionsPage(string pageName)
        {
            OptionsPage? page = null;

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
                case "ConfigurationPage":
                    {
                        page = new ConfigurationPage { Dock = DockStyle.Fill };
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
            OptionsPage? page = rowobject as OptionsPage;
            return page?.PageIcon == null ? _display.ScaleImage(Properties.Resources.F1Help_16x) : _display.ScaleImage(page.PageIcon);
        }

        public void SetActivatedPage(string? pageName = default)
        {
            _pageName = pageName ?? Language.StartupExit;

            // Ensure we have items loaded before trying to access them
            if (lstOptionPages.Items.Count == 0)
            {
                Logger.Instance.Log?.Warn($"[SetActivatedPage] No items in lstOptionPages, cannot set active page to '{_pageName}'");
                return;
            }

            bool isSet = false;
            for (int i = 0; i < lstOptionPages.Items.Count; i++)
            {
                if (!lstOptionPages.Items[i].Text.Equals(_pageName)) continue;
                lstOptionPages.Items[i].Selected = true;
                isSet = true;
                break;
            }

            if (!isSet && lstOptionPages.Items.Count > 0)
                lstOptionPages.Items[0].Selected = true;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            Logger.Instance.Log?.Debug($"[BtnOK_Click] START");
            SaveOptions();
            // Clear change flags after saving
            ClearChangeFlags();
            this.Visible = false;
            Logger.Instance.Log?.Debug($"[BtnOK_Click] END - Visible set to false");
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            Logger.Instance.Log?.Debug($"[BtnApply_Click] START");
            SaveOptions();
            // Clear change flags after applying
            ClearChangeFlags();
            Logger.Instance.Log?.Debug($"[BtnApply_Click] END");
        }

        private void SaveOptions()
        {
            foreach (OptionsPage page in _optionPages)
            {
                Logger.Instance.Log?.Debug($"[SaveOptions] Saving page: {page.PageName}");
                page.SaveSettings();
            }

            Logger.Instance.Log?.Debug($"[SaveOptions] Configuration file: {(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)).FilePath}");
            Settings.Default.Save();
        }

        private void LstOptionPages_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Guard against recursive calls that can cause infinite loops
            if (_isHandlingSelectionChange)
            {
                Logger.Instance.Log?.Warn($"[LstOptionPages_SelectedIndexChanged] RECURSIVE CALL BLOCKED - Preventing infinite loop");
                return;
            }

            _isHandlingSelectionChange = true;
            try
            {
                Logger.Instance.Log?.Debug($"[LstOptionPages_SelectedIndexChanged] START - IsLoading: {_isLoading}, SelectedIndex: {lstOptionPages.SelectedIndex}, Items.Count: {lstOptionPages.Items.Count}");

                pnlMain.Controls.Clear();
                Logger.Instance.Log?.Debug($"[LstOptionPages_SelectedIndexChanged] pnlMain.Controls cleared");

                if (lstOptionPages.SelectedObject is OptionsPage page)
                {
                    Logger.Instance.Log?.Debug($"[LstOptionPages_SelectedIndexChanged] SelectedObject: {page.PageName}");
                }
                else
                {
                    Logger.Instance.Log?.Warn($"[LstOptionPages_SelectedIndexChanged] Page is NULL - cannot display. This may indicate a selection issue.");
                    return;
                }

                if (page.IsDisposed)
                {
                    Logger.Instance.Log?.Error($"[LstOptionPages_SelectedIndexChanged] Page '{page.PageName}' is disposed - cannot display");
                    return;
                }

                // Ensure the page has a valid window handle
                if (!page.IsHandleCreated)
                {
                    Logger.Instance.Log?.Debug($"[LstOptionPages_SelectedIndexChanged] Page '{page.PageName}' has no handle - creating handle");
                    var handle = page.Handle; // This creates the handle
                    Logger.Instance.Log?.Debug($"[LstOptionPages_SelectedIndexChanged] Handle created: {handle}");
                }

                Logger.Instance.Log?.Debug($"[LstOptionPages_SelectedIndexChanged] Adding page '{page.PageName}' to pnlMain");
                pnlMain.Controls.Add(page);
                Logger.Instance.Log?.Debug($"[LstOptionPages_SelectedIndexChanged] Page added successfully. pnlMain.Controls.Count: {pnlMain.Controls.Count}");

                Logger.Instance.Log?.Debug($"[LstOptionPages_SelectedIndexChanged] END");
            }
            finally
            {
                _isHandlingSelectionChange = false;
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            Logger.Instance.Log?.Debug($"[BtnCancel_Click] START");

            foreach (OptionsPage page in _optionPages)
            {
                try
                {
                    page.RevertSettings();
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log?.Warn($"[BtnCancel_Click] RevertSettings failed for {page.GetType().Name}: {ex.Message}");
                }
            }

            ClearChangeFlags();
            this.Visible = false;
            Logger.Instance.Log?.Debug($"[BtnCancel_Click] END - Visible set to false");
        }

        private void FrmOptions_FormClosing(object sender, FormClosingEventArgs e)
        {
            Logger.Instance.Log?.Debug($"[FrmOptions_FormClosing] START - CloseReason: {e.CloseReason}, Cancel: {e.Cancel}");

            // Check if any page has unsaved changes
            bool hasChanges = _optionPages.Any(page => page.HasChanges);
            Logger.Instance.Log?.Debug($"[FrmOptions_FormClosing] HasChanges: {hasChanges}");

            if (hasChanges)
            {
                DialogResult result = MessageBox.Show(
                    Language.SaveOptionsBeforeClosing,
                    Language.Options,
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                Logger.Instance.Log?.Debug($"[FrmOptions_FormClosing] User choice: {result}");

                switch (result)
                {
                    case DialogResult.Yes:
                        SaveOptions();
                        ClearChangeFlags();
                        e.Cancel = true;
                        this.Visible = false;
                        Logger.Instance.Log?.Debug($"[FrmOptions_FormClosing] Saved and hiding");
                        break;
                    case DialogResult.No:
                        // Discard changes
                        ClearChangeFlags();
                        e.Cancel = true;
                        this.Visible = false;
                        Logger.Instance.Log?.Debug($"[FrmOptions_FormClosing] Discarded and hiding");
                        break;
                    case DialogResult.Cancel:
                        // Cancel closing - keep the dialog open
                        e.Cancel = true;
                        Logger.Instance.Log?.Debug($"[FrmOptions_FormClosing] User cancelled close");
                        break;
                }
            }
            else
            {
                e.Cancel = true;
                this.Visible = false;
                Logger.Instance.Log?.Debug($"[FrmOptions_FormClosing] No changes - hiding");
            }

            Logger.Instance.Log?.Debug($"[FrmOptions_FormClosing] END - Cancel: {e.Cancel}, Visible: {this.Visible}");
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
            Control? current = control;
            while (current != null && current is not OptionsPage)
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

        private void ValidateControlState()
        {
            Logger.Instance.Log?.Debug($"[ValidateControlState] START - Items.Count: {lstOptionPages.Items.Count}, pnlMain.Controls.Count: {pnlMain.Controls.Count}");

            // Ensure we have pages loaded
            if (lstOptionPages.Items.Count == 0)
            {
                Logger.Instance.Log?.Debug($"[ValidateControlState] No items loaded - returning");
                return;
            }

            OptionsPage? currentPage = lstOptionPages.SelectedObject as OptionsPage;
            Logger.Instance.Log?.Debug($"[ValidateControlState] Current page: {(currentPage != null ? currentPage.PageName : "NULL")}");

            if (currentPage == null)
            {
                Logger.Instance.Log?.Warn($"[ValidateControlState] SelectedObject is NULL - this may indicate a selection issue");
                // Don't try to fix this - let the normal selection handling deal with it
                return;
            }

            if (currentPage.IsDisposed)
            {
                Logger.Instance.Log?.Warn($"[ValidateControlState] Page '{currentPage.PageName}' is disposed");
                return;
            }

            Logger.Instance.Log?.Debug($"[ValidateControlState] Page '{currentPage.PageName}' is valid - IsHandleCreated: {currentPage.IsHandleCreated}");

            // Ensure the page has a valid window handle
            if (!currentPage.IsHandleCreated)
            {
                Logger.Instance.Log?.Debug($"[ValidateControlState] Creating handle for page '{currentPage.PageName}'");
                // Force handle creation
                var handle = currentPage.Handle;
                Logger.Instance.Log?.Debug($"[ValidateControlState] Handle created: {handle}");
            }

            // Ensure the page is displayed in the panel
            if (!pnlMain.Controls.Contains(currentPage))
            {
                Logger.Instance.Log?.Debug($"[ValidateControlState] Page '{currentPage.PageName}' not in pnlMain - adding it now");
                pnlMain.Controls.Clear();
                pnlMain.Controls.Add(currentPage);
                Logger.Instance.Log?.Debug($"[ValidateControlState] Page added. pnlMain.Controls.Count: {pnlMain.Controls.Count}");
            }
            else
            {
                Logger.Instance.Log?.Debug($"[ValidateControlState] Page '{currentPage.PageName}' already in pnlMain - OK");
            }

            Logger.Instance.Log?.Debug($"[ValidateControlState] END");
        }
    }
}
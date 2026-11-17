#region  Usings
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.App.Diagnostics;
using mRemoteNG.Properties;
using mRemoteNG.Resources.Language;
using mRemoteNG.Themes;
using mRemoteNG.UI.Forms.OptionsPages;
#endregion

namespace mRemoteNG.UI.Forms
{
    [SupportedOSPlatform("windows")]
    public partial class FrmOptions : ResponsiveForm
    {
        private readonly OptionsPageLifecycle _lifecycle;
        private readonly DisplayProperties _display = new();
        private string _pageName;
        private bool _isInitialized;
        private bool _isFontOverrideApplied;
        private bool _isHandlingSelectionChange; // Guard flag to prevent recursive event handling

        /// <summary>
        /// Raised when the user clicks OK or Cancel, signalling the host window to hide.
        /// </summary>
        public event EventHandler CloseRequested;

        public FrmOptions() : this(Language.StartupExit)
        {
        }

        public FrmOptions(string pageName)
        {
            using var construct = new OptionsLoadSpan(OptionsLoadStage.FormCreate);
            Cursor.Current = Cursors.WaitCursor;
            using (new OptionsLoadSpan(OptionsLoadStage.FormInit))
            {
                InitializeComponent();
                Icon = Resources.ImageConverter.GetImageAsIcon(Properties.Resources.Settings_16x);
            }
            _pageName = pageName;
            Cursor.Current = Cursors.Default;
            DoubleBuffered = true;

            _lifecycle = new OptionsPageLifecycle(CreateDefaultDescriptors());
            using (new OptionsLoadSpan(OptionsLoadStage.FormPages))
                InitOptionsPagesToListView();
            lstOptionPages.FontChanged += (_, _) => FitPageNavigation();
            SizeChanged += (_, _) => FitPageNavigation();
            using (new OptionsLoadSpan(OptionsLoadStage.FormFit))
                FitPageNavigation();
        }

        internal OptionsPageLifecycle PageLifecycle => _lifecycle;

        internal OptionsPage? CurrentPage => _lifecycle.ActiveDescriptor?.Page;

        internal void SetInitialPage(string pageName)
        {
            if (string.IsNullOrEmpty(pageName))
            {
                return;
            }

            if (_isInitialized)
            {
                SetActivatedPage(pageName);
            }
            else
            {
                _pageName = pageName;
            }
        }

        internal static List<OptionsPageDescriptor> CreateDefaultDescriptors()
        {
            bool isAdmin = string.Equals(Properties.OptionsRbac.Default.ActiveRole, "AdminRole", StringComparison.Ordinal);
            List<OptionsPageDescriptor> descriptors = new(13);

            if (isAdmin || Properties.OptionsStartupExitPage.Default.cbStartupExitPageInOptionMenu)
            {
                descriptors.Add(new OptionsPageDescriptor(
                    nameof(StartupExitPage),
                    Language.StartupExit,
                    static () => Properties.Resources.StartupProject_16x,
                    static () => new StartupExitPage()));
            }

            if (isAdmin || Properties.OptionsAppearancePage.Default.cbAppearancePageInOptionMenu)
            {
                descriptors.Add(new OptionsPageDescriptor(
                    nameof(AppearancePage),
                    Language.Appearance,
                    static () => Properties.Resources.Panel_16x,
                    static () => new AppearancePage()));
            }

            if (isAdmin || Properties.OptionsConnectionsPage.Default.cbConnectionsPageInOptionMenu)
            {
                descriptors.Add(new OptionsPageDescriptor(
                    nameof(ConnectionsPage),
                    Language.Connections,
                    static () => Properties.Resources.ASPWebSite_16x,
                    static () => new ConnectionsPage()));
            }

            if (isAdmin || Properties.OptionsTabsPanelsPage.Default.cbTabsPanelsPageInOptionMenu)
            {
                descriptors.Add(new OptionsPageDescriptor(
                    nameof(TabsPanelsPage),
                    Language.TabsAndPanels.Replace("&&", "&", StringComparison.Ordinal),
                    static () => Properties.Resources.Tab_16x,
                    static () => new TabsPanelsPage()));
            }

            if (isAdmin || Properties.OptionsNotificationsPage.Default.cbNotificationsPageInOptionMenu)
            {
                descriptors.Add(new OptionsPageDescriptor(
                    nameof(NotificationsPage),
                    Language.Notifications,
                    static () => Properties.Resources.LogError_16x,
                    static () => new NotificationsPage()));
            }

            if (isAdmin || Properties.OptionsCredentialsPage.Default.cbCredentialsPageInOptionMenu)
            {
                descriptors.Add(new OptionsPageDescriptor(
                    nameof(CredentialsPage),
                    Language.Credentials,
                    static () => Properties.Resources.Key_16x,
                    static () => new CredentialsPage()));
            }

            if (isAdmin || Properties.OptionsDBsPage.Default.cbDBsPageInOptionMenu)
            {
                descriptors.Add(new OptionsPageDescriptor(
                    nameof(SqlServerPage),
                    Language.SQLServer.TrimEnd(':'),
                    static () => Properties.Resources.SQLDatabase_16x,
                    static () => new SqlServerPage()));
            }

            if (isAdmin || Properties.OptionsUpdatesPage.Default.cbUpdatesPageInOptionMenu)
            {
                descriptors.Add(new OptionsPageDescriptor(
                    nameof(UpdatesPage),
                    Language.Updates,
                    static () => Properties.Resources.RunUpdate_16x,
                    static () => new UpdatesPage()));
            }

            if (isAdmin || Properties.OptionsThemePage.Default.cbThemePageInOptionMenu)
            {
                descriptors.Add(new OptionsPageDescriptor(
                    nameof(ThemePage),
                    Language.Theme,
                    static () => Properties.Resources.AppearanceEditor_16x,
                    static () => new ThemePage()));
            }

            if (isAdmin || Properties.OptionsSecurityPage.Default.cbSecurityPageInOptionMenu)
            {
                descriptors.Add(new OptionsPageDescriptor(
                    nameof(SecurityPage),
                    Language.TabSecurity,
                    static () => Properties.Resources.Lock_16x,
                    static () => new SecurityPage()));
            }

            if (isAdmin || Properties.OptionsAdvancedPage.Default.cbAdvancedPageInOptionMenu)
            {
                descriptors.Add(new OptionsPageDescriptor(
                    nameof(AdvancedPage),
                    Language.Advanced,
                    static () => Properties.Resources.Settings_16x,
                    static () => new AdvancedPage()));
            }

            if (isAdmin || Properties.OptionsBackupPage.Default.cbBacupPageInOptionMenu)
            {
                descriptors.Add(new OptionsPageDescriptor(
                    nameof(BackupPage),
                    Language.strBackup,
                    static () => Properties.Resources.DocumentsFolder_16x,
                    static () => new BackupPage()));
            }

            descriptors.Add(new OptionsPageDescriptor(
                nameof(ConfigurationPage),
                Language.Config,
                static () => Properties.Resources.Settings_16x,
                static () => new ConfigurationPage()));

            return descriptors;
        }

        private void FitPageNavigation()
        {
            int padding = 40 * DeviceDpi / 96;
            int wanted = _lifecycle.Descriptors
                .Select(descriptor => TextRenderer.MeasureText(descriptor.PageName, lstOptionPages.Font).Width)
                .DefaultIfEmpty(120)
                .Max() + padding;
            lstOptionPages.Width = Math.Min(wanted, Math.Max(120 * DeviceDpi / 96, ClientSize.Width / 3));
            lstOptionPages.RowHeight = Math.Max(25 * DeviceDpi / 96, lstOptionPages.Font.Height + 8 * DeviceDpi / 96);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose all built option pages to prevent resource leaks (GDI handles, etc.)
                // Inactive pages are not in the Controls collection and would otherwise leak.
                _lifecycle.DisposeAll();

                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void FrmOptions_Load(object sender, EventArgs e)
        {
            using var load = new OptionsLoadSpan(OptionsLoadStage.FormLoad);
            Logger.Instance.Log?.Debug($"[FrmOptions_Load] START - IsInitialized: {_isInitialized}, Visible: {Visible}, Handle: {Handle}");

            // Only initialize once to prevent multiple event subscriptions and page reloading
            if (_isInitialized)
            {
                Logger.Instance.Log?.Debug("[FrmOptions_Load] Already initialized - fast path");
                return;
            }

            Logger.Instance.Log?.Debug("[FrmOptions_Load] First initialization");
            Text = Language.OptionsPageTitle;
            btnOK.Text = Language._Ok;
            btnCancel.Text = Language._Cancel;
            btnApply.Text = Language.Apply;
            SetActivatedPage(_pageName);
            Logger.Instance.Log?.Debug("[FrmOptions_Load] Selected page set");

            _isInitialized = true;
            Logger.Instance.Log?.Debug("[FrmOptions_Load] END (first initialization complete)");
        }

        private void FrmOptions_Shown(object sender, EventArgs e)
        {
            if (_isFontOverrideApplied)
            {
                return;
            }

            int parent = OptionsLoadSpan.CurrentId;
            BeginInvoke((MethodInvoker)(() =>
            {
                if (IsDisposed || _isFontOverrideApplied)
                {
                    return;
                }

                using var font = new OptionsLoadSpan(OptionsLoadStage.FormFont, parent: parent);
                FontOverrider.FontOverride(this);
                _isFontOverrideApplied = true;
            }));
        }

        private void InitOptionsPagesToListView()
        {
            Logger.Instance.Log?.Debug($"[InitOptionsPagesToListView] START - Registering {_lifecycle.Descriptors.Count} page descriptors");

            lstOptionPages.RowHeight = _display.ScaleHeight(lstOptionPages.RowHeight);
            lstOptionPages.AllColumns.First().ImageGetter = ImageGetter;

            lstOptionPages.BeginUpdate();
            try
            {
                lstOptionPages.SetObjects(_lifecycle.Descriptors);
            }
            finally
            {
                lstOptionPages.EndUpdate();
            }

            lstOptionPages.SelectedIndexChanged += LstOptionPages_SelectedIndexChanged;
            Logger.Instance.Log?.Debug("[InitOptionsPagesToListView] END");
        }

        private static void PrepareBuiltPage(string pageKey, OptionsPage page)
        {
            using (new OptionsLoadSpan(OptionsLoadStage.PageLayout, pageKey))
            {
                System.Drawing.Font font;
                using (new OptionsLoadSpan(OptionsLoadStage.PageFontCreate, pageKey))
                    font = InterfaceFont.Create(
                        Properties.OptionsAppearancePage.Default.UIFontFamily,
                        Properties.OptionsAppearancePage.Default.UIFontSize,
                        (System.Drawing.FontStyle)Properties.OptionsAppearancePage.Default.UIFontStyle);
                using (font)
                {
                    using (new OptionsLoadSpan(OptionsLoadStage.PageFontApply, pageKey))
                        InterfaceFont.Apply(page, font);
                    using (new OptionsLoadSpan(OptionsLoadStage.PageAdaptive, pageKey))
                        AdaptiveLayout.Prepare(page);
                }
            }

            using (new OptionsLoadSpan(OptionsLoadStage.PageTrack, pageKey))
                TrackChangesInControls(page);
        }

        private object ImageGetter(object rowobject)
        {
            if (rowobject is OptionsPageDescriptor descriptor)
            {
                return descriptor.GetScaledIcon(_display);
            }

            OptionsPage? page = rowobject as OptionsPage;
            return page?.PageIcon == null ? _display.ScaleImage(Properties.Resources.F1Help_16x) : _display.ScaleImage(page.PageIcon);
        }

        public void SetActivatedPage(string? pageName = default)
        {
            _pageName = pageName ?? Language.StartupExit;

            if (lstOptionPages.Items.Count == 0)
            {
                Logger.Instance.Log?.Warn($"[SetActivatedPage] No items in lstOptionPages, cannot set active page to '{_pageName}'");
                return;
            }

            if (lstOptionPages.SelectedObject is OptionsPageDescriptor selectedDescriptor &&
                string.Equals(selectedDescriptor.PageName, _pageName, StringComparison.Ordinal) &&
                selectedDescriptor.IsBuilt &&
                !selectedDescriptor.NeedsReload &&
                pnlMain.Controls.Count > 0 &&
                ReferenceEquals(pnlMain.Controls[0], selectedDescriptor.Page))
            {
                Logger.Instance.Log?.Debug($"[SetActivatedPage] Page '{_pageName}' already selected - skipping");
                return;
            }

            int targetIndex = 0;
            for (int i = 0; i < lstOptionPages.Items.Count; i++)
            {
                if (string.Equals(lstOptionPages.Items[i].Text, _pageName, StringComparison.Ordinal))
                {
                    targetIndex = i;
                    break;
                }
            }

            if (lstOptionPages.SelectedIndex == targetIndex)
            {
                ActivateSelectedPage();
            }
            else
            {
                lstOptionPages.Items[targetIndex].Selected = true;
            }
        }

        /// <summary>
        /// Builds the next page that is not built yet, without showing it, and reports whether
        /// one was built. Called one page per idle moment after startup (#232), so the first
        /// Options open finds its controls ready. The page is left marked for reload: it loads
        /// the stored settings when it is first shown and saves nothing until then.
        /// </summary>
        internal bool PrebuildNextPage()
        {
            if (IsDisposed) return false;
            foreach (OptionsPageDescriptor descriptor in _lifecycle.Descriptors)
            {
                if (descriptor.IsBuilt) continue;
                using (new OptionsLoadSpan(OptionsLoadStage.Prebuild, descriptor.PageKey))
                    descriptor.EnsureReady(builtPage => PrepareBuiltPage(descriptor.PageKey, builtPage));
                // A page built here is not "loaded" for saving: OK saves only built-and-loaded
                // pages, and the first open does not reload anything, so without this the first OK
                // wrote every prebuilt page the user never opened (Theme turned theming on, Updates
                // and SQL re-encrypted their passwords). It loads again when it is first shown.
                descriptor.MarkNeedsReload();
                return true;
            }
            return false;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            Logger.Instance.Log?.Debug("[BtnOK_Click] START");
            SaveAllOptions();
            _lifecycle.InvalidateInactiveBuiltPages();
            CloseRequested?.Invoke(this, EventArgs.Empty);
            Logger.Instance.Log?.Debug("[BtnOK_Click] END");
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            Logger.Instance.Log?.Debug("[BtnApply_Click] START");
            SaveAllOptions();
            Logger.Instance.Log?.Debug("[BtnApply_Click] END");
        }

        private static void PersistSettings()
        {
            if (Logger.Instance.Log?.IsDebugEnabled == true)
            {
                Logger.Instance.Log.Debug($"[SaveOptions] Configuration file: {ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath}");
            }

            Settings.Default.Save();
        }

        private void LstOptionPages_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActivateSelectedPage();
        }

        private void ActivateSelectedPage()
        {
            // Guard against recursive calls that can cause infinite loops
            if (_isHandlingSelectionChange)
            {
                Logger.Instance.Log?.Warn("[LstOptionPages_SelectedIndexChanged] RECURSIVE CALL BLOCKED - Preventing infinite loop");
                return;
            }

            _isHandlingSelectionChange = true;
            try
            {
                Logger.Instance.Log?.Debug($"[LstOptionPages_SelectedIndexChanged] START - SelectedIndex: {lstOptionPages.SelectedIndex}, Items.Count: {lstOptionPages.Items.Count}");

                if (lstOptionPages.SelectedObject is not OptionsPageDescriptor descriptor)
                {
                    Logger.Instance.Log?.Warn("[LstOptionPages_SelectedIndexChanged] Descriptor is NULL - cannot display.");
                    return;
                }

                OptionsPage page = _lifecycle.Activate(
                    descriptor,
                    builtPage => PrepareBuiltPage(descriptor.PageKey, builtPage));

                if (page.IsDisposed)
                {
                    Logger.Instance.Log?.Error($"[LstOptionPages_SelectedIndexChanged] Page '{descriptor.PageName}' is disposed - cannot display");
                    return;
                }

                if (pnlMain.Controls.Count == 1 && ReferenceEquals(pnlMain.Controls[0], page))
                {
                    return;
                }

                bool wasLoading = page.IsLoadingSettings;
                page.IsLoadingSettings = true;
                pnlMain.SuspendLayout();
                try
                {
                    pnlMain.Controls.Clear();
                    // No early page.Handle here: created before the page has a parent, that handle
                    // was destroyed and built again by Controls.Add.
                    pnlMain.Controls.Add(page);
                }
                finally
                {
                    pnlMain.ResumeLayout(true);
                    page.IsLoadingSettings = wasLoading;
                }

                Logger.Instance.Log?.Debug("[LstOptionPages_SelectedIndexChanged] END");
            }
            finally
            {
                _isHandlingSelectionChange = false;
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            Logger.Instance.Log?.Debug("[BtnCancel_Click] START");
            ReloadAllSettings();
            CloseRequested?.Invoke(this, EventArgs.Empty);
            Logger.Instance.Log?.Debug("[BtnCancel_Click] END");
        }

        /// <summary>
        /// Returns true if any built options page has been modified.
        /// </summary>
        internal bool HasUnsavedChanges() => _lifecycle.HasUnsavedChanges();

        /// <summary>
        /// Saves all built and loaded option page settings to disk.
        /// </summary>
        internal void SaveAllOptions()
        {
            _lifecycle.SaveBuiltAndLoaded(PersistSettings);
        }

        /// <summary>
        /// Reverts built pages and reloads the active page from stored settings, marking
        /// inactive built pages for reload on next selection.
        /// </summary>
        internal void ReloadAllSettings()
        {
            _lifecycle.ReloadAll();
        }

        /// <summary>
        /// Discards any pending change flags and reloads control values from stored settings.
        /// </summary>
        internal void DiscardChanges() => ReloadAllSettings();

        private static void TrackChangesInControls(Control control)
        {
            foreach (Control childControl in control.Controls)
            {
                if (childControl is TextBox textBox)
                {
                    textBox.TextChanged += (_, _) => MarkPageAsChanged(control);
                }
                else if (childControl is CheckBox checkBox)
                {
                    checkBox.CheckedChanged += (_, _) => MarkPageAsChanged(control);
                }
                else if (childControl is RadioButton radioButton)
                {
                    radioButton.CheckedChanged += (_, _) => MarkPageAsChanged(control);
                }
                else if (childControl is ComboBox comboBox)
                {
                    comboBox.SelectedIndexChanged += (_, _) => MarkPageAsChanged(control);
                }
                else if (childControl is NumericUpDown numericUpDown)
                {
                    numericUpDown.ValueChanged += (_, _) => MarkPageAsChanged(control);
                }
                else if (childControl is ListBox listBox)
                {
                    listBox.SelectedIndexChanged += (_, _) => MarkPageAsChanged(control);
                }

                if (childControl.Controls.Count > 0)
                {
                    TrackChangesInControls(childControl);
                }
            }
        }

        private static void MarkPageAsChanged(Control control)
        {
            Control? current = control;
            while (current != null && current is not OptionsPage)
            {
                current = current.Parent;
            }

            if (current is OptionsPage { IsLoadingSettings: false } page)
            {
                page.HasChanges = true;
            }
        }
    }
}

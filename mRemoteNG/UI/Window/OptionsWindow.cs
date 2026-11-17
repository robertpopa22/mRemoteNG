using System;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.App.Diagnostics;
using mRemoteNG.Themes;
using mRemoteNG.UI.Forms;
using WeifenLuo.WinFormsUI.Docking;
using mRemoteNG.Resources.Language;

namespace mRemoteNG.UI.Window
{
    [SupportedOSPlatform("windows")]
    public partial class OptionsWindow : BaseWindow
    {
        private FrmOptions? _optionsForm;
        private string? _pendingPageName;
        private bool _isInitialized;
        private bool _isFontOverrideApplied;

        #region Public Methods

        public OptionsWindow() : this(new DockContent())
        {
        }

        public OptionsWindow(DockContent panel)
        {
            WindowType = WindowType.Options;
            DockPnl = panel;
            InitializeComponent();
            Icon = Resources.ImageConverter.GetImageAsIcon(Properties.Resources.Settings_16x);
            DoubleBuffered = true;
        }

        #endregion

        #region Form Stuff

        protected override void OnLoad(EventArgs e)
        {
            // Run BaseWindow.OnLoad before embedding _optionsForm so BaseWindow does not
            // perform a redundant recursive font/layout pass over FrmOptions (which runs
            // its own ResponsiveForm.OnLoad during _optionsForm.Show()).
            base.OnLoad(e);

            using var load = new OptionsLoadSpan(OptionsLoadStage.WindowLoad);
            if (_isInitialized && _optionsForm != null && !_optionsForm.IsDisposed)
                return;

            ThemeManager.getInstance().ThemeChanged += ApplyTheme;
            Disposed += (_, _) => ThemeManager.getInstance().ThemeChanged -= ApplyTheme;

            using (new OptionsLoadSpan(OptionsLoadStage.WindowTheme))
                ApplyTheme();
            ApplyLanguage();
            LoadOptionsForm();

            _isInitialized = true;
        }

        private void Options_Shown(object sender, EventArgs e)
        {
            if (_isFontOverrideApplied)
                return;

            int parent = OptionsLoadSpan.CurrentId;
            BeginInvoke((MethodInvoker)(() =>
            {
                if (IsDisposed || _isFontOverrideApplied)
                    return;

                using var font = new OptionsLoadSpan(OptionsLoadStage.WindowFont, parent: parent);
                FontOverrider.FontOverride(this);
                _isFontOverrideApplied = true;
            }));
        }

        private new void ApplyTheme()
        {
            if (!ThemeManager.getInstance().ActiveAndExtended) return;
            base.ApplyTheme();
        }

        private void ApplyLanguage()
        {
            Text = Language.Options;
            TabText = Language.Options;
        }

        private void LoadOptionsForm()
        {
            using var embed = new OptionsLoadSpan(OptionsLoadStage.EmbedForm);
            // Check if FrmMain.OptionsForm is disposed (source of truth)
            if (FrmMain.OptionsForm != null && FrmMain.OptionsForm.IsDisposed)
                FrmMain.RecreateOptionsForm();

            // If the local reference is disposed, clean up
            if (_optionsForm != null && _optionsForm.IsDisposed)
            {
                if (Controls.Contains(_optionsForm))
                    Controls.Remove(_optionsForm);
                _optionsForm.CloseRequested -= OnOptionsFormCloseRequested;
                _optionsForm = null;
            }

            // Get fresh reference if needed
            if (_optionsForm == null)
            {
                _optionsForm = FrmMain.OptionsForm;

                if (_optionsForm == null || _optionsForm.IsDisposed)
                    return;

                if (_pendingPageName != null)
                {
                    _optionsForm.SetInitialPage(_pendingPageName);
                    _pendingPageName = null;
                }

                _optionsForm.TopLevel = false;
                _optionsForm.FormBorderStyle = FormBorderStyle.None;
                _optionsForm.MinimumSize = System.Drawing.Size.Empty;
                if (ClientSize is { Width: > 0, Height: > 0 })
                    _optionsForm.Size = ClientSize;
                _optionsForm.Dock = DockStyle.Fill;
                _optionsForm.CloseRequested += OnOptionsFormCloseRequested;
                Controls.Add(_optionsForm);
            }

            if (!_optionsForm.Visible)
                using (new OptionsLoadSpan(OptionsLoadStage.FormShow))
                    _optionsForm.Show();
        }

        /// <summary>
        /// Handles OK/Cancel from embedded FrmOptions.
        /// Calls DockHandler.Hide() directly — much faster than Close() because
        /// it skips the entire FormClosing event chain.
        /// </summary>
        private void OnOptionsFormCloseRequested(object sender, EventArgs e)
        {
            DockHandler.Hide();
        }

        /// <summary>
        /// Reloads all option-page controls from the stored (on-disk) settings.
        /// Call this before making the panel visible so that any stale edits from a
        /// previous session (e.g. after a Tab-X hide) are discarded.
        /// Safe to call before first initialization — no-op when _optionsForm is null.
        /// </summary>
        internal void RefreshSettings()
        {
            _optionsForm?.ReloadAllSettings();
        }

        /// <summary>
        /// Handles the case where OptionsWindow is closed explicitly (e.g. from code
        /// calling Close(), or DPS's CloseContent which fires FormClosing before hiding).
        /// Checks for unsaved changes in the embedded FrmOptions.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_optionsForm != null && !_optionsForm.IsDisposed && _optionsForm.HasUnsavedChanges())
            {
                DialogResult result = MessageBox.Show(
                    Language.SaveOptionsBeforeClosing,
                    Language.Options,
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                switch (result)
                {
                    case DialogResult.Yes:
                        _optionsForm.SaveAllOptions();
                        break;
                    case DialogResult.No:
                        _optionsForm.DiscardChanges();
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        return;
                }
            }

            base.OnFormClosing(e);
        }

        public void SetActivatedPage(string pageName)
        {
            if (_optionsForm != null && !_optionsForm.IsDisposed)
            {
                _pendingPageName = null;
                _optionsForm.SetActivatedPage(pageName);
            }
            else
            {
                _pendingPageName = pageName;
            }
        }

        #endregion

        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // OptionsWindow
            // 
            ClientSize = new System.Drawing.Size(800, 600);
            HideOnClose = true;
            Name = "OptionsWindow";
            Text = Language.Options;
            TabText = Language.Options;
            Shown += Options_Shown;
            ResumeLayout(false);
        }
    }
}

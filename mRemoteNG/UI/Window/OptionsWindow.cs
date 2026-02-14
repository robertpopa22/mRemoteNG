using System;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.App;
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
        private bool _isInitialized = false;

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
            FontOverrider.FontOverride(this);
        }

        #endregion

        #region Form Stuff

        private void Options_Load(object sender, EventArgs e)
        {
            Logger.Instance.Log?.Debug($"[OptionsWindow.Options_Load] START - IsInitialized: {_isInitialized}, Visible: {this.Visible}");

            // Only subscribe to ThemeChanged once to prevent multiple subscriptions
            if (!_isInitialized)
            {
                Logger.Instance.Log?.Debug($"[OptionsWindow.Options_Load] First initialization - subscribing to ThemeChanged");
                ThemeManager.getInstance().ThemeChanged += ApplyTheme;
                _isInitialized = true;
            }

            ApplyTheme();
            ApplyLanguage();
            LoadOptionsForm();

            // Ensure all pages are loaded and form is ready
            EnsureOptionsFormReady();

            Logger.Instance.Log?.Debug($"[OptionsWindow.Options_Load] END");
        }

        private void EnsureOptionsFormReady()
        {
            Logger.Instance.Log?.Debug($"[OptionsWindow.EnsureOptionsFormReady] START - OptionsForm: {(_optionsForm != null ? "EXISTS" : "NULL")}, IsDisposed: {_optionsForm?.IsDisposed}");

            if (_optionsForm != null && !_optionsForm.IsDisposed)
            {
                Logger.Instance.Log?.Debug($"[OptionsWindow.EnsureOptionsFormReady] Processing Application.DoEvents");
                // Process any pending UI events to ensure the form is fully rendered
                Application.DoEvents();
            }

            Logger.Instance.Log?.Debug($"[OptionsWindow.EnsureOptionsFormReady] END");
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
            Logger.Instance.Log?.Debug($"[OptionsWindow.LoadOptionsForm] START - _optionsForm: {(_optionsForm != null ? "EXISTS" : "NULL")}, IsDisposed: {_optionsForm?.IsDisposed}");
            Logger.Instance.Log?.Debug($"[OptionsWindow.LoadOptionsForm] FrmMain.OptionsForm: {(FrmMain.OptionsForm != null ? "EXISTS" : "NULL")}, IsDisposed: {FrmMain.OptionsForm?.IsDisposed}");

            // Check if FrmMain.OptionsForm is disposed FIRST (this is the source of truth)
            if (FrmMain.OptionsForm != null && FrmMain.OptionsForm.IsDisposed)
            {
                Logger.Instance.Log?.Warn($"[OptionsWindow.LoadOptionsForm] FrmMain.OptionsForm is DISPOSED - recreating");
                // Force FrmMain to recreate the OptionsForm
                FrmMain.RecreateOptionsForm();
            }

            // If the local reference is disposed, we need to clean up
            if (_optionsForm != null && _optionsForm.IsDisposed)
            {
                Logger.Instance.Log?.Warn($"[OptionsWindow.LoadOptionsForm] Local _optionsForm is DISPOSED - cleaning up");
                if (Controls.Contains(_optionsForm))
                {
                    Controls.Remove(_optionsForm);
                }
                _optionsForm.VisibleChanged -= OptionsForm_VisibleChanged;
                _optionsForm = null;
            }

            // Get fresh reference if needed
            if (_optionsForm == null)
            {
                Logger.Instance.Log?.Debug($"[OptionsWindow.LoadOptionsForm] Getting fresh OptionsForm from FrmMain");
                _optionsForm = FrmMain.OptionsForm;
                Logger.Instance.Log?.Debug($"[OptionsWindow.LoadOptionsForm] OptionsForm retrieved from FrmMain: {(_optionsForm != null ? "SUCCESS" : "FAILED")}");

                if (_optionsForm == null)
                {
                    Logger.Instance.Log?.Error($"[OptionsWindow.LoadOptionsForm] CRITICAL: Failed to get OptionsForm from FrmMain");
                    return;
                }

                // Double-check that the form we just got is not disposed
                if (_optionsForm.IsDisposed)
                {
                    Logger.Instance.Log?.Error($"[OptionsWindow.LoadOptionsForm] CRITICAL: FrmMain.OptionsForm is STILL disposed after recreation attempt!");
                    return;
                }

                _optionsForm.TopLevel = false;
                _optionsForm.FormBorderStyle = FormBorderStyle.None;
                _optionsForm.Dock = DockStyle.Fill;
                _optionsForm.VisibleChanged += OptionsForm_VisibleChanged;
                Controls.Add(_optionsForm);
                Logger.Instance.Log?.Debug($"[OptionsWindow.LoadOptionsForm] OptionsForm added to Controls");
            }

            // Only show if not already visible to prevent redundant event cascades
            Logger.Instance.Log?.Debug($"[OptionsWindow.LoadOptionsForm] OptionsForm.Visible: {_optionsForm.Visible}, IsDisposed: {_optionsForm.IsDisposed}");
            if (!_optionsForm.Visible)
            {
                Logger.Instance.Log?.Debug($"[OptionsWindow.LoadOptionsForm] Calling Show()");
                _optionsForm.Show();
            }
            else
            {
                Logger.Instance.Log?.Debug($"[OptionsWindow.LoadOptionsForm] Already visible - skipping Show()");
            }

            Logger.Instance.Log?.Debug($"[OptionsWindow.LoadOptionsForm] END");
        }

        private void OptionsForm_VisibleChanged(object sender, EventArgs e)
        {
            Logger.Instance.Log?.Debug($"[OptionsWindow.OptionsForm_VisibleChanged] OptionsForm.Visible: {_optionsForm?.Visible}");

            // When the embedded FrmOptions is hidden (OK/Cancel clicked), close this window
            if (_optionsForm != null && !_optionsForm.Visible)
            {
                Logger.Instance.Log?.Debug($"[OptionsWindow.OptionsForm_VisibleChanged] OptionsForm hidden - closing OptionsWindow");
                this.Close();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Logger.Instance.Log?.Debug($"[OptionsWindow.OnFormClosing] START - CloseReason: {e.CloseReason}");
            // With HideOnClose = true, we don't dispose the window
            // so we keep the embedded form in Controls for reuse
            base.OnFormClosing(e);
            Logger.Instance.Log?.Debug($"[OptionsWindow.OnFormClosing] END");
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            Logger.Instance.Log?.Debug($"[OptionsWindow.OnVisibleChanged] START - Visible: {Visible}, OptionsForm: {(_optionsForm != null ? "EXISTS" : "NULL")}");

            base.OnVisibleChanged(e);

            // When the window becomes visible, ensure the embedded form is also shown
            if (Visible && _optionsForm != null && !_optionsForm.Visible)
            {
                Logger.Instance.Log?.Debug($"[OptionsWindow.OnVisibleChanged] Window visible but OptionsForm hidden - calling Show()");
                _optionsForm.Show();
            }

            Logger.Instance.Log?.Debug($"[OptionsWindow.OnVisibleChanged] END");
        }

        public void SetActivatedPage(string pageName)
        {
            _optionsForm?.SetActivatedPage(pageName);
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
            Load += Options_Load;
            ResumeLayout(false);
        }
    }
}

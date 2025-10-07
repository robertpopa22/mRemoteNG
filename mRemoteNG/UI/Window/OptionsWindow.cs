using System;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.Forms;
using WeifenLuo.WinFormsUI.Docking;
using mRemoteNG.Resources.Language;

namespace mRemoteNG.UI.Window
{
    [SupportedOSPlatform("windows")]
    public partial class OptionsWindow : BaseWindow
    {
        private FrmOptions _optionsForm;

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
            ApplyTheme();
            ThemeManager.getInstance().ThemeChanged += ApplyTheme;
            ApplyLanguage();
            LoadOptionsForm();
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
            if (_optionsForm == null || _optionsForm.IsDisposed)
            {
                _optionsForm = FrmMain.OptionsForm;
                _optionsForm.TopLevel = false;
                _optionsForm.FormBorderStyle = FormBorderStyle.None;
                _optionsForm.Dock = DockStyle.Fill;
                _optionsForm.VisibleChanged += OptionsForm_VisibleChanged;
                Controls.Add(_optionsForm);
            }
            _optionsForm.Show();
        }

        private void OptionsForm_VisibleChanged(object sender, EventArgs e)
        {
            // When the embedded FrmOptions is hidden (OK/Cancel clicked), close this window
            if (_optionsForm != null && !_optionsForm.Visible)
            {
                this.Close();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Detach the FrmOptions form so it can be reused
            if (_optionsForm != null)
            {
                _optionsForm.VisibleChanged -= OptionsForm_VisibleChanged;
                Controls.Remove(_optionsForm);
            }
            base.OnFormClosing(e);
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
            Name = "OptionsWindow";
            Text = Language.Options;
            TabText = Language.Options;
            Load += Options_Load;
            ResumeLayout(false);
        }
    }
}

using mRemoteNG.Themes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using mRemoteNG.UI.Controls;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;

namespace mRemoteNG.UI.TaskDialog
{
    [SupportedOSPlatform("windows")]
    public partial class frmTaskDialog : Form
    {
        //--------------------------------------------------------------------------------

        #region PRIVATE members

        //--------------------------------------------------------------------------------

        private string _mainInstruction = "Main Instruction Text";

        private readonly Font _mainInstructionFont =
            new("Segoe UI", 11.75F, FontStyle.Regular, GraphicsUnit.Point, 0);

        private readonly List<MrngRadioButton> _radioButtonCtrls = [];
        private Control? _focusControl;

        private int _mainInstructionLeftMargin;

        #endregion

        //--------------------------------------------------------------------------------

        #region PROPERTIES

        //--------------------------------------------------------------------------------
        public ESysIcons MainIcon { get; set; } = ESysIcons.Question;
        public ESysIcons FooterIcon { get; set; } = ESysIcons.Warning;

        public string Title
        {
            get => Text;
            set => Text = value;
        }

        public string MainInstruction
        {
            get => _mainInstruction;
            set
            {
                _mainInstruction = value;
                Invalidate();
            }
        }

        public string Content
        {
            get => lbContent.Text;
            set => lbContent.Text = value;
        }

        public string ExpandedInfo
        {
            get => lbExpandedInfo.Text;
            set => lbExpandedInfo.Text = value;
        }

        public string Footer
        {
            get => lbFooter.Text;
            set => lbFooter.Text = value;
        }

        public int DefaultButtonIndex { get; set; }

        public string RadioButtons { get; set; } = "";

        public int RadioButtonIndex
        {
            get
            {
                foreach (MrngRadioButton rb in _radioButtonCtrls)
                    if (rb.Checked)
                        return rb.Tag is int index ? index : -1;
                return -1;
            }
        }

        public string CommandButtons { get; set; } = "";
        public int CommandButtonClickedIndex { get; private set; } = -1;

        public ETaskDialogButtons Buttons { get; set; } = ETaskDialogButtons.YesNoCancel;

        public string VerificationText
        {
            get => cbVerify.Text;
            set => cbVerify.Text = value;
        }

        public bool VerificationCheckBoxChecked
        {
            get => cbVerify.Checked;
            set => cbVerify.Checked = value;
        }

        private bool Expanded { get; set; }

        #endregion

        //--------------------------------------------------------------------------------

        #region CONSTRUCTOR

        //--------------------------------------------------------------------------------
        public frmTaskDialog()
        {
            InitializeComponent();
            InitializeDetailsImageList();

            if (CTaskDialog.UseToolWindowOnXp)
                FormBorderStyle = FormBorderStyle.FixedToolWindow;

            MainInstruction = "Main Instruction";
            Content = "";
            ExpandedInfo = "";
            Footer = "";
            VerificationText = "";
        }

        #endregion

        //--------------------------------------------------------------------------------

        #region BuildForm

        // This is the main routine that should be called before .ShowDialog()
        //--------------------------------------------------------------------------------
        private bool _formBuilt;

        public void BuildForm()
        {
            int formHeight = 0;
            // PerMonitorV2 handles DPI scaling automatically — no manual scaling needed

            // Setup Main Instruction
            switch (MainIcon)
            {
                case ESysIcons.Information:
                    imgMain.Image = SystemIcons.Information.ToBitmap();
                    break;
                case ESysIcons.Question:
                    imgMain.Image = SystemIcons.Question.ToBitmap();
                    break;
                case ESysIcons.Warning:
                    imgMain.Image = SystemIcons.Warning.ToBitmap();
                    break;
                case ESysIcons.Error:
                    imgMain.Image = SystemIcons.Error.ToBitmap();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(MainIcon), MainIcon, null);
            }

            lbMainInstruction.Text = _mainInstruction;
            lbMainInstruction.Font = _mainInstructionFont;
            AdjustLabelHeight(lbMainInstruction);
            pnlMainInstruction.Height = Math.Max(41, lbMainInstruction.Height + 16);

            _mainInstructionLeftMargin = imgMain.Left + imgMain.Width + imgMain.Padding.Right;
            formHeight += pnlMainInstruction.Height;

            // Setup Content
            pnlContent.Visible = Content != "";
            if (Content != "")
            {
                AdjustLabelHeight(lbContent);
                pnlContent.Height = lbContent.Height + 4;
                formHeight += pnlContent.Height;
            }

            bool showVerifyCheckbox = cbVerify.Text != "";
            cbVerify.Visible = showVerifyCheckbox;

            // Setup Expanded Info and Buttons panels
            if (ExpandedInfo == "")
            {
                pnlExpandedInfo.Visible = false;
                lbShowHideDetails.Visible = false;
                cbVerify.Top = 12;
                pnlButtons.Height = 40;
            }
            else
            {
                AdjustLabelHeight(lbExpandedInfo);
                pnlExpandedInfo.Height = lbExpandedInfo.Height + 4;
                pnlExpandedInfo.Visible = Expanded;
                lbShowHideDetails.Text = Expanded ? "        Hide details" : "        Show details";
                lbShowHideDetails.ImageIndex = Expanded ? 0 : 3;
                if (!showVerifyCheckbox)
                    pnlButtons.Height = 40;
                if (Expanded)
                    formHeight += pnlExpandedInfo.Height;
            }

            // Setup RadioButtons
            pnlRadioButtons.Visible = RadioButtons != "";
            if (RadioButtons != "")
            {
                string[] arr = RadioButtons.Split('|');
                int pnlHeight = 12;
                for (int i = 0; i < arr.Length; i++)
                {
                    MrngRadioButton rb = new() { Parent = pnlRadioButtons};
                    rb.Location = new Point(60, 4 + i * rb.Height);
                    rb.Text = arr[i];
                    rb.Tag = i;
                    rb.Checked = DefaultButtonIndex == i;
                    rb.Width = Width - rb.Left - 15;
                    pnlHeight += rb.Height;
                    _radioButtonCtrls.Add(rb);
                }

                pnlRadioButtons.Height = pnlHeight;
                formHeight += pnlRadioButtons.Height;
            }

            // Setup CommandButtons
            pnlCommandButtons.Visible = CommandButtons != "";
            if (CommandButtons != "")
            {
                string[] arr = CommandButtons.Split('|');
                int t = 8;
                int pnlHeight = 16;
                for (int i = 0; i < arr.Length; i++)
                {
                    CommandButton btn = new()
                    {
                        Parent = pnlCommandButtons, Location = new Point(50, t)
                    };
                    btn.Text = arr[i];
                    btn.Size = new Size(Width - btn.Left - 15, btn.GetBestHeight());
                    t += btn.Height;
                    pnlHeight += btn.Height;
                    btn.Tag = i;
                    btn.Click += CommandButton_Click;
                    if (i == DefaultButtonIndex)
                        _focusControl = btn;
                }

                pnlCommandButtons.Height = pnlHeight;
                formHeight += pnlCommandButtons.Height;
            }

            // Setup Buttons
            switch (Buttons)
            {
                case ETaskDialogButtons.YesNo:
                    bt1.Visible = false;
                    bt2.Text = Language.Yes;
                    bt2.DialogResult = DialogResult.Yes;
                    bt3.Text = Language.No;
                    bt3.DialogResult = DialogResult.No;
                    AcceptButton = bt2;
                    CancelButton = bt3;
                    break;
                case ETaskDialogButtons.YesNoCancel:
                    bt1.Text = Language.Yes;
                    bt1.DialogResult = DialogResult.Yes;
                    bt2.Text = Language.No;
                    bt2.DialogResult = DialogResult.No;
                    bt3.Text = Language._Cancel;
                    bt3.DialogResult = DialogResult.Cancel;
                    AcceptButton = bt1;
                    CancelButton = bt3;
                    break;
                case ETaskDialogButtons.OkCancel:
                    bt1.Visible = false;
                    bt2.Text = Language._Ok;
                    bt2.DialogResult = DialogResult.OK;
                    bt3.Text = Language._Cancel;
                    bt3.DialogResult = DialogResult.Cancel;
                    AcceptButton = bt2;
                    CancelButton = bt3;
                    break;
                case ETaskDialogButtons.Ok:
                    bt1.Visible = false;
                    bt2.Visible = false;
                    bt3.Text = Language._Ok;
                    bt3.DialogResult = DialogResult.OK;
                    AcceptButton = bt3;
                    CancelButton = bt3;
                    break;
                case ETaskDialogButtons.Close:
                    bt1.Visible = false;
                    bt2.Visible = false;
                    bt3.Text = Language._Close;
                    bt3.DialogResult = DialogResult.Cancel;
                    CancelButton = bt3;
                    break;
                case ETaskDialogButtons.Cancel:
                    bt1.Visible = false;
                    bt2.Visible = false;
                    bt3.Text = Language._Cancel;
                    bt3.DialogResult = DialogResult.Cancel;
                    CancelButton = bt3;
                    break;
                case ETaskDialogButtons.DisconnectCancel:
                    bt1.Visible = false;
                    bt2.Text = Language.Disconnect;
                    bt2.DialogResult = DialogResult.Yes;
                    bt3.Text = Language._Cancel;
                    bt3.DialogResult = DialogResult.No;
                    AcceptButton = bt3; // Cancel is safer default for destructive action
                    CancelButton = bt3;
                    break;
                case ETaskDialogButtons.DeleteCancel:
                    bt1.Visible = false;
                    bt2.Text = Language.Delete;
                    bt2.DialogResult = DialogResult.Yes;
                    bt3.Text = Language._Cancel;
                    bt3.DialogResult = DialogResult.No;
                    AcceptButton = bt3; // Cancel is safer default for destructive action
                    CancelButton = bt3;
                    break;
                case ETaskDialogButtons.None:
                    bt1.Visible = false;
                    bt2.Visible = false;
                    bt3.Visible = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Buttons), Buttons, null);
            }

            ControlBox = Buttons == ETaskDialogButtons.Cancel ||
                         Buttons == ETaskDialogButtons.Close ||
                         Buttons == ETaskDialogButtons.OkCancel ||
                         Buttons == ETaskDialogButtons.YesNoCancel ||
                         Buttons == ETaskDialogButtons.DisconnectCancel ||
                         Buttons == ETaskDialogButtons.DeleteCancel;

            // Reposition buttons right-to-left so longer translations (e.g. Hungarian) fit (#55)
            RepositionButtons();

            if (!showVerifyCheckbox && ExpandedInfo == "" && Buttons == ETaskDialogButtons.None)
                pnlButtons.Visible = false;
            else
                formHeight += pnlButtons.Height;

            pnlFooter.Visible = Footer != "";
            if (Footer != "")
            {
                AdjustLabelHeight(lbFooter);
                pnlFooter.Height = Math.Max(28, lbFooter.Height + 16);
                switch (FooterIcon)
                {
                    case ESysIcons.Information:
                        imgFooter.Image = ResizeBitmap(SystemIcons.Information.ToBitmap(), 16, 16);
                        break;
                    case ESysIcons.Question:
                        imgFooter.Image = ResizeBitmap(SystemIcons.Question.ToBitmap(), 16, 16);
                        break;
                    case ESysIcons.Warning:
                        imgFooter.Image = ResizeBitmap(SystemIcons.Warning.ToBitmap(), 16, 16);
                        break;
                    case ESysIcons.Error:
                        imgFooter.Image = ResizeBitmap(SystemIcons.Error.ToBitmap(), 16, 16);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(FooterIcon), FooterIcon, null);
                }

                formHeight += pnlFooter.Height;
            }

            ClientSize = new Size(ClientSize.Width, formHeight);

            _formBuilt = true;
            ThemeManager.getInstance().ThemeChanged += ApplyTheme;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            if (!ThemeManager.getInstance().ActiveAndExtended) return;

            var palette = ThemeManager.getInstance().ActiveTheme.ExtendedPalette;
            if (palette is null) return;

            pnlButtons.BackColor = palette.getColor("Dialog_Background");
            pnlButtons.ForeColor = palette.getColor("Dialog_Foreground");
            panel2.BackColor = palette.getColor("Dialog_Background");
            panel2.ForeColor = palette.getColor("Dialog_Foreground");
            pnlFooter.BackColor = palette.getColor("Dialog_Background");
            pnlFooter.ForeColor = palette.getColor("Dialog_Foreground");
            panel5.BackColor = palette.getColor("Dialog_Background");
            panel5.ForeColor = palette.getColor("Dialog_Foreground");
            panel3.BackColor = palette.getColor("Dialog_Background");
            panel3.ForeColor = palette.getColor("Dialog_Foreground");
            pnlCommandButtons.BackColor = palette.getColor("Dialog_Background");
            pnlCommandButtons.ForeColor = palette.getColor("Dialog_Foreground");
            pnlMainInstruction.BackColor = palette.getColor("Dialog_Background");
            pnlMainInstruction.ForeColor = palette.getColor("Dialog_Foreground");
            pnlContent.BackColor = palette.getColor("Dialog_Background");
            pnlContent.ForeColor = palette.getColor("Dialog_Foreground");
            pnlExpandedInfo.BackColor = palette.getColor("Dialog_Background");
            pnlExpandedInfo.ForeColor = palette.getColor("Dialog_Foreground");
            pnlRadioButtons.BackColor = palette.getColor("Dialog_Background");
            pnlRadioButtons.ForeColor = palette.getColor("Dialog_Foreground");
        }

        private void InitializeDetailsImageList()
        {
            imageList1.ImageSize = new Size(16, 16);
            imageList1.ColorDepth = ColorDepth.Depth32Bit;
            imageList1.Images.Add("arrow_up_bw.bmp", CreateDetailsArrow(up: true, Color.DimGray, SystemColors.Control));
            imageList1.Images.Add("arrow_up_color.bmp", CreateDetailsArrow(up: true, Color.RoyalBlue, SystemColors.Control));
            imageList1.Images.Add("arrow_up_color_pressed.bmp", CreateDetailsArrow(up: true, Color.Navy, SystemColors.ControlDark));
            imageList1.Images.Add("arrow_down_bw.bmp", CreateDetailsArrow(up: false, Color.DimGray, SystemColors.Control));
            imageList1.Images.Add("arrow_down_color.bmp", CreateDetailsArrow(up: false, Color.RoyalBlue, SystemColors.Control));
            imageList1.Images.Add("arrow_down_color_pressed.bmp", CreateDetailsArrow(up: false, Color.Navy, SystemColors.ControlDark));
            imageList1.Images.Add("green_arrow.bmp", CreateDetailsArrow(up: false, Color.ForestGreen, SystemColors.Control));
        }

        private static Bitmap CreateDetailsArrow(bool up, Color arrowColor, Color backgroundColor)
        {
            Bitmap bitmap = new(16, 16);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.Clear(backgroundColor);

            Point[] points = up
                ? [new Point(8, 4), new Point(3, 10), new Point(13, 10)]
                : [new Point(3, 6), new Point(13, 6), new Point(8, 12)];
            using SolidBrush brush = new(arrowColor);
            graphics.FillPolygon(brush, points);
            return bitmap;
        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Positions buttons right-to-left with widths based on actual text.
        /// Widens the form when buttons don't fit (#55).
        /// </summary>
        private void RepositionButtons()
        {
            const int padding = 6;
            const int rightMargin = 9;
            const int leftMargin = 9;
            const int minWidth = 75;

            // Measure required width for each visible button
            var buttons = new[] { bt3, bt2, bt1 };
            int totalNeeded = rightMargin + leftMargin;
            int visibleCount = 0;
            foreach (var btn in buttons)
            {
                if (!btn.Visible) continue;
                int textWidth = TextRenderer.MeasureText(btn.Text, btn.Font).Width + 16;
                btn.Width = Math.Max(minWidth, textWidth);
                totalNeeded += btn.Width;
                visibleCount++;
            }
            totalNeeded += Math.Max(0, visibleCount - 1) * padding;

            // Widen the form if buttons don't fit
            int panelWidth = pnlButtons.ClientSize.Width;
            if (totalNeeded > panelWidth)
            {
                int grow = totalNeeded - panelWidth;
                Width += grow;
                // Re-center on screen
                if (Owner != null)
                    Left = Owner.Left + (Owner.Width - Width) / 2;
                else
                    CenterToScreen();
                panelWidth = pnlButtons.ClientSize.Width;
            }

            // Position right-to-left
            int x = panelWidth - rightMargin;
            foreach (var btn in buttons)
            {
                if (!btn.Visible) continue;
                btn.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                x -= btn.Width;
                btn.Left = x;
                x -= padding;
            }
        }

        private static Image ResizeBitmap(Image srcImg, int newWidth, int newHeight)
        {
            float percentWidth = newWidth / (float)srcImg.Width;
            float percentHeight = newHeight / (float)srcImg.Height;

            float resizePercent = percentHeight < percentWidth ? percentHeight : percentWidth;

            int w = (int)(srcImg.Width * resizePercent);
            int h = (int)(srcImg.Height * resizePercent);
            Bitmap b = new(w, h);

            using (Graphics g = Graphics.FromImage(b))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(srcImg, 0, 0, w, h);
            }

            return b;
        }

        //--------------------------------------------------------------------------------
        // utility function for setting a Label's height
        private static void AdjustLabelHeight(Control lb)
        {
            string text = lb.Text;
            Font textFont = lb.Font;
            SizeF layoutSize = new(lb.ClientSize.Width, 5000.0F);

            using (Graphics g = Graphics.FromHwnd(lb.Handle))
            {
                SizeF stringSize = g.MeasureString(text, textFont, layoutSize);
                lb.Height = (int)stringSize.Height + 4;
            }
        }

        #endregion

        //--------------------------------------------------------------------------------

        #region EVENTS

        //--------------------------------------------------------------------------------
        private void CommandButton_Click(object sender, EventArgs e)
        {
            CommandButtonClickedIndex = ((CommandButton)sender).Tag is int index ? index : -1;
            DialogResult = DialogResult.OK;
        }


        //--------------------------------------------------------------------------------
        protected override void OnShown(EventArgs e)
        {
            if (!_formBuilt)
                throw new InvalidOperationException("frmTaskDialog : Please call .BuildForm() before showing the TaskDialog");
            // Reposition buttons AFTER the form is fully laid out.
            // BuildForm() calls RepositionButtons() too, but the WinForms layout engine
            // may override button positions between BuildForm() and Show(). This second
            // call ensures buttons are correctly positioned after all layout passes (#55).
            RepositionButtons();
            // Focus the default button so the user can see which one is active.
            if (AcceptButton is Control acceptControl)
                acceptControl.Focus();
            base.OnShown(e);
        }

        //--------------------------------------------------------------------------------
        private void lbDetails_MouseEnter(object sender, EventArgs e)
        {
            lbShowHideDetails.ImageIndex = Expanded ? 1 : 4;
        }

        //--------------------------------------------------------------------------------
        private void lbDetails_MouseLeave(object sender, EventArgs e)
        {
            lbShowHideDetails.ImageIndex = Expanded ? 0 : 3;
        }

        //--------------------------------------------------------------------------------
        private void lbDetails_MouseUp(object sender, MouseEventArgs e)
        {
            lbShowHideDetails.ImageIndex = Expanded ? 1 : 4;
        }

        //--------------------------------------------------------------------------------
        private void lbDetails_MouseDown(object sender, MouseEventArgs e)
        {
            lbShowHideDetails.ImageIndex = Expanded ? 2 : 5;
        }

        //--------------------------------------------------------------------------------
        private void lbDetails_Click(object sender, EventArgs e)
        {
            Expanded = !Expanded;
            pnlExpandedInfo.Visible = Expanded;
            lbShowHideDetails.Text = Expanded ? "        Hide details" : "        Show details";
            if (Expanded)
                Height += pnlExpandedInfo.Height;
            else
                Height -= pnlExpandedInfo.Height;
        }

        //--------------------------------------------------------------------------------
        private void frmTaskDialog_Shown(object sender, EventArgs e)
        {
            if (CTaskDialog.PlaySystemSounds)
            {
                try
                {
                    PlaySystemSound(MainIcon);
                }
                catch (Exception ex) when (ex is System.IO.FileNotFoundException or System.IO.FileLoadException or TypeLoadException)
                {
                    // The sound is cosmetic. SystemSounds lives in System.Windows.Extensions,
                    // a shared-framework assembly that can be missing on broken/mixed portable
                    // installs (#150) — showing the dialog must not crash over the beep.
                    App.Runtime.MessageCollector?.AddMessage(Messages.MessageClass.DebugMsg,
                        $"System sound unavailable: {ex.Message}");
                }
            }

            _focusControl?.Focus();
        }

        // Keep the SystemSounds references out of frmTaskDialog_Shown: the JIT resolves
        // System.Windows.Extensions when compiling the method that uses it, so the guard
        // above only works if this is a separate, never-inlined method.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PlaySystemSound(ESysIcons icon)
        {
            switch (icon)
            {
                case ESysIcons.Error:
                    System.Media.SystemSounds.Hand.Play();
                    break;
                case ESysIcons.Information:
                    System.Media.SystemSounds.Asterisk.Play();
                    break;
                case ESysIcons.Question:
                    System.Media.SystemSounds.Asterisk.Play();
                    break;
                case ESysIcons.Warning:
                    System.Media.SystemSounds.Exclamation.Play();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(icon), icon, "Unexpected MainIcon value.");
            }
        }

        #endregion

        //--------------------------------------------------------------------------------
    }
}

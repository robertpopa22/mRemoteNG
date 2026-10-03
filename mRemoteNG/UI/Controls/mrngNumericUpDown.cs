using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;

// ReSharper disable LocalizableElement

namespace mRemoteNG.UI.Controls
{
    [SupportedOSPlatform("windows")]
    //Repaint of the NumericUpDown, the composite control buttons are replaced because the
    //original ones cannot be themed due to protected inheritance
    internal class MrngNumericUpDown : NumericUpDown
    {
        private readonly ThemeManager _themeManager;
        private MrngButton? Up;
        private MrngButton? Down;

        public MrngNumericUpDown()
        {
            _themeManager = ThemeManager.getInstance();
            ThemeManager.getInstance().ThemeChanged += OnCreateControl;
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            if (!_themeManager.ActiveAndExtended) return;
            var palette = _themeManager.ActiveTheme.ExtendedPalette;
            if (palette is null) return;
            ForeColor = palette.getColor("TextBox_Foreground");
            BackColor = palette.getColor("TextBox_Background");
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

            if (Controls.Count > 0)
            {
                for (int i = 0; i < Controls.Count; i++)
                {
                    //Remove those non-themable buttons
                    if (Controls[i].GetType().ToString().Equals("System.Windows.Forms.UpDownBase+UpDownButtons", StringComparison.Ordinal))
                        Controls.Remove(Controls[i]);

                    /* This is a bit of a hack.
                     * But if we have the buttons that we created already, redraw/return and don't add any more...
                     *
                     * OptionsPages are an example where the control is potentially created twice:
                     * AddOptionsPagesToListView and then LstOptionPages_SelectedIndexChanged
                     */
                    if (!(Controls[i] is MrngButton button)) continue;
                    if (button.Text.Equals("\u25B2", StringComparison.Ordinal))
                        Up = button;
                    else if (button.Text.Equals("\u25BC", StringComparison.Ordinal))
                        Down = button;
                }

                if (Up is not null && Down is not null)
                {
                    LayoutArrowButtons();
                    Invalidate();
                    return;
                }
            }

            //Add new themable buttons
            Up = new MrngButton
            {
                Text = "\u25B2",
                Font = new Font(Font.FontFamily, 5f)
            };
            Up.Click += Up_Click;
            Down = new MrngButton
            {
                Text = "\u25BC",
                Font = new Font(Font.FontFamily, 5f)
            };
            Down.Click += Down_Click;
            Controls.Add(Up);
            Controls.Add(Down);
            LayoutArrowButtons();
            Invalidate();
        }

        /// <summary>
        /// Design-time pixels at 96 DPI, multiplied once by the control's device DPI.
        /// </summary>
        internal static int DevicePixels(int designPixelsAt96Dpi, int deviceDpi)
        {
            int dpi = deviceDpi > 0 ? deviceDpi : 96;
            return Math.Max(1, designPixelsAt96Dpi * dpi / 96);
        }

        private void LayoutArrowButtons()
        {
            if (Up is null || Down is null)
                return;

            int edge = DevicePixels(1, DeviceDpi);
            int width = DevicePixels(16, DeviceDpi);
            int top = DevicePixels(2, DeviceDpi);
            int half = Height / 2;
            Up.SetBounds(Width - width - edge, top, width, Math.Max(1, half - edge));
            Down.SetBounds(Width - width - edge, half + edge, width, Math.Max(1, half - edge));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutArrowButtons();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            // Scale(SizeF) sizes children after ScaleControl returns. This pass
            // runs after that, so the arrows keep a single DeviceDpi factor.
            LayoutArrowButtons();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            LayoutArrowButtons();
        }

        private void Down_Click(object sender, EventArgs e)
        {
            DownButton();
        }

        private void Up_Click(object sender, EventArgs e)
        {
            UpButton();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            if (_themeManager.ActiveAndExtended)
            {
                var palette = _themeManager.ActiveTheme.ExtendedPalette;
                if (palette is null) return;
                if (Enabled)
                {
                    ForeColor = palette.getColor("TextBox_Foreground");
                    BackColor = palette.getColor("TextBox_Background");
                }
                else
                {
                    BackColor = palette.getColor("TextBox_Disabled_Background");
                }
            }

            base.OnEnabledChanged(e);
            Invalidate();
        }


        //Redrawing border
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!_themeManager.ActiveAndExtended) return;
            //Fix Border
            if (BorderStyle != BorderStyle.None)
                if (_themeManager.ActiveTheme.ExtendedPalette is { } borderPalette)
                    e.Graphics.DrawRectangle(
                                             new Pen(borderPalette.getColor("TextBox_Border"),
                                                     1), 0, 0, Width - 1,
                                             Height - 1);
        }

        private void InitializeComponent()
        {
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // NGNumericUpDown
            // 
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular,
                                                System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
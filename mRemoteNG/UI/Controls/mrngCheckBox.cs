using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;

namespace mRemoteNG.UI.Controls
{
    [SupportedOSPlatform("windows")]
    //Extended CheckBox class, the NGCheckBox onPaint completely repaint the control

    //
    // If this causes design issues in the future, may want to think about migrating to
    // CheckBoxRenderer:
    // https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.checkboxrenderer?view=netframework-4.6
    //
    public class MrngCheckBox : CheckBox
    {
        private ThemeManager? _themeManager;

        public MrngCheckBox()
        {
            InitializeComponent();
            ThemeManager.getInstance().ThemeChanged += OnCreateControl;
        }

        /// <summary>
        /// Sizes the glyph and the label for <paramref name="deviceDpi"/>. A per-monitor bounce can
        /// leave this control at the previous DPI, with a glyph-only client and a font whose pixel
        /// height is taller than that client (#198: 30px in a 27px box). A point size would be
        /// measured again at that stale DPI, so the em size is in pixels of the dialog's DPI.
        /// </summary>
        internal void FitToDeviceDpi(int deviceDpi)
        {
            int dpi = Math.Max(1, deviceDpi);
            float emPx = 8.25f * dpi / 72f;
            if (Font.Unit != GraphicsUnit.Pixel || Math.Abs(Font.Size - emPx) >= 0.5f)
                Font = new Font(Font.FontFamily, emPx, Font.Style, GraphicsUnit.Pixel, Font.GdiCharSet, Font.GdiVerticalFont);

            // AutoSize keeps the empty-glyph size from before the text and the DPI were known.
            AutoSize = false;
            int box = Math.Max(1, (int)Math.Round(11d * dpi / 96d));
            Size text = TextRenderer.MeasureText(Text, Font);
            int pad = Math.Max(1, (int)Math.Round(dpi / 96d));
            int height = Math.Max(box, Math.Max(text.Height, Font.Height)) + pad;
            Size = new Size(box + (pad * 4) + text.Width, height);
        }

        public enum MouseState
        {
            HOVER,
            DOWN,
            OUT
        }

#pragma warning disable CA1707 // Designer-generated code uses this name; renaming would break .Designer.cs files
        public MouseState _mice { get; set; }
#pragma warning restore CA1707


        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            _themeManager = ThemeManager.getInstance();
            if (!_themeManager.ThemingActive) return;
            _mice = MouseState.OUT;
            MouseEnter += (sender, args) =>
            {
                _mice = MouseState.HOVER;
                Invalidate();
            };
            MouseLeave += (sender, args) =>
            {
                _mice = MouseState.OUT;
                Invalidate();
            };
            MouseDown += (sender, args) =>
            {
                if (args.Button != MouseButtons.Left) return;
                _mice = MouseState.DOWN;
                Invalidate();
            };
            MouseUp += (sender, args) =>
            {
                _mice = MouseState.OUT;

                Invalidate();
            };

            Invalidate();
        }


        protected override void OnPaint(PaintEventArgs pevent)
        {
            if (_themeManager is null || !_themeManager.ActiveAndExtended)
            {
                base.OnPaint(pevent);
                return;
            }

            var palette = _themeManager.ActiveTheme.ExtendedPalette;
            if (palette is null)
            {
                base.OnPaint(pevent);
                return;
            }

            //Get the colors
            Color fore;
            Color glyph;
            Color checkBorder;

            Color back = palette.getColor("CheckBox_Background");
            if (Enabled)
            {
                glyph = palette.getColor("CheckBox_Glyph");
                fore = palette.getColor("CheckBox_Text");
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (_mice)
                {
                    case MouseState.HOVER:
                        checkBorder = palette.getColor("CheckBox_Border_Hover");
                        break;
                    case MouseState.DOWN:
                        checkBorder = palette.getColor("CheckBox_Border_Pressed");
                        break;
                    default:
                        checkBorder = palette.getColor("CheckBox_Border");
                        break;
                }
            }
            else
            {
                fore = palette.getColor("CheckBox_Text_Disabled");
                glyph = palette.getColor("CheckBox_Glyph_Disabled");
                checkBorder = palette.getColor("CheckBox_Border_Disabled");
            }

            Color parentBack = Parent?.BackColor ?? BackColor;
            pevent.Graphics.Clear(parentBack);

            int box = Math.Max(1, (int)Math.Round(11d * Math.Max(1, DeviceDpi) / 96d));
            box = Math.Min(box, Math.Max(1, ClientSize.Height - 2));
            int y = Math.Max(0, (ClientSize.Height - box) / 2);
            Rectangle boxRect = new(0, y, box, box);
            // The pen is centered on the rectangle edge, so the border stops one pixel inside.
            Rectangle border = new(boxRect.X, boxRect.Y, Math.Max(1, boxRect.Width - 1), Math.Max(1, boxRect.Height - 1));

            using (Pen p = new(checkBorder))
            using (SolidBrush fill = new(back))
            {
                pevent.Graphics.FillRectangle(fill, boxRect);
                pevent.Graphics.DrawRectangle(p, border);
            }

            if (Checked)
            {
                // U+E001 is the tick in Segoe UI Symbol.
                using Font mark = new("Segoe UI Symbol", Math.Max(1, border.Height - 1), FontStyle.Regular, GraphicsUnit.Pixel);
                TextRenderer.DrawText(pevent.Graphics, "\uE001", mark, boxRect, glyph,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }

            int textX = boxRect.Right + 2;
            Rectangle textRect = new(textX, 0, Math.Max(1, ClientSize.Width - textX), ClientSize.Height);
            TextRenderer.DrawText(pevent.Graphics, Text, Font, textRect, fore, parentBack,
                                  TextFormatFlags.VerticalCenter | TextFormatFlags.PathEllipsis);
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // NGCheckBox
            // 
            Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ResumeLayout(false);
        }
    }
}
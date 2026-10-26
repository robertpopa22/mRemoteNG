using System.Drawing;
using System;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;

namespace mRemoteNG.UI.Controls
{
    [SupportedOSPlatform("windows")]
    //Extended ComboBox class, the NGComboBox onPaint completely repaint the control as does the item painting
    //warning: THe DropDown style rendering is glitchy in this control, only use DropDownList or correct the rendering method
    internal class MrngComboBox : ComboBox
    {
        private ThemeManager? _themeManager;

        public enum MouseState
        {
            HOVER,
            DOWN,
            OUT
        }

        public MouseState _mice { get; set; }

        public MrngComboBox()
        {
            ThemeManager.getInstance().ThemeChanged += OnCreateControl;
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            _themeManager = ThemeManager.getInstance();
            if (_themeManager is not { ActiveAndExtended: true }) return;
            var activePalette = _themeManager!.ActiveTheme.ExtendedPalette;
            if (activePalette is null) return;
            BackColor = activePalette.getColor("ComboBox_Background");
            ForeColor = activePalette.getColor("ComboBox_Foreground");
            DrawMode = DrawMode.OwnerDrawFixed;
            FitItemHeight();
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            DrawItem += NG_DrawItem;
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

        private void FitItemHeight()
        {
            if (DrawMode != DrawMode.OwnerDrawFixed || !IsHandleCreated) return;
            using Graphics graphics = CreateGraphics();
            ItemHeight = TextRenderer.MeasureText(graphics, "Ag", Font, Size.Empty,
                TextFormatFlags.SingleLine).Height + 4 * DeviceDpi / 96;
            Height = PreferredHeight;
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            FitItemHeight();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            FitItemHeight();
        }

        private void NG_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (_themeManager?.ActiveTheme?.ExtendedPalette is not { } palette) return;

            int index = e.Index >= 0 ? e.Index : 0;
            using SolidBrush itemBrush = new(palette.getColor("ComboBox_Foreground"));
            Brush activeBrush = itemBrush;

            SolidBrush? selectedBrush = null;
            try
            {
                if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                {
                    selectedBrush = new SolidBrush(
                        palette.getColor("List_Item_Selected_Foreground"));
                    activeBrush = selectedBrush;
                    using SolidBrush selectedBack = new(
                        palette.getColor("List_Item_Selected_Background"));
                    e.Graphics.FillRectangle(selectedBack, e.Bounds);
                }
                else
                {
                    using SolidBrush normalBack = new(
                        palette.getColor("ComboBox_Background"));
                    e.Graphics.FillRectangle(normalBack, e.Bounds);
                }

                if (Items.Count > 0)
                {
                    Font drawFont = e.Font ?? Font;
                    var item = Items[index];
                    if (item is null) return;
                    if (string.IsNullOrEmpty(DisplayMember))
                        e.Graphics.DrawString(item.ToString(), drawFont, activeBrush, e.Bounds,
                                              StringFormat.GenericDefault);
                    else
                    {
                        var prop = item.GetType().GetProperty(DisplayMember);
                        if (prop != null)
                        {
                            e.Graphics.DrawString(
                                prop.GetValue(item, null)?.ToString(),
                                drawFont, activeBrush, e.Bounds, StringFormat.GenericDefault);
                        }
                    }
                }

                e.DrawFocusRectangle();
            }
            finally
            {
                selectedBrush?.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_themeManager is not { ActiveAndExtended: true })
            {
                base.OnPaint(e);
                return;
            }

            //Colors
            var ep = _themeManager!.ActiveTheme.ExtendedPalette;
            if (ep is null) return;
            Color Border = ep.getColor("ComboBox_Border");
            Color Back = ep.getColor("ComboBox_Background");
            Color Fore = ep.getColor("ComboBox_Foreground");
            Color ButtBack = ep.getColor("ComboBox_Button_Background");
            Color ButtFore = ep.getColor("ComboBox_Button_Foreground");

            if (_mice == MouseState.HOVER)
            {
                Border = ep.getColor("ComboBox_MouseOver_Border");
                ButtBack = ep.getColor("ComboBox_Button_MouseOver_Background");
                ButtFore = ep.getColor("ComboBox_Button_MouseOver_Foreground");
            }

            if (DroppedDown)
            {
                Border = ep.getColor("ComboBox_MouseOver_Border");
                ButtBack = ep.getColor("ComboBox_Button_Pressed_Background");
                ButtFore = ep.getColor("ComboBox_Button_Pressed_Foreground");
            }


            e.Graphics.Clear(Back);

            //Border
            using (Pen p = new(Border))
            {
                Rectangle boxRect = new(0, 0, Width - 1, Height - 1);
                e.Graphics.DrawRectangle(p, boxRect);
            }

            //Button
            int edge = Math.Max(1, 2 * DeviceDpi / 96);
            int buttonWidth = 18 * DeviceDpi / 96;
            using (SolidBrush b = new(ButtBack))
            {
                e.Graphics.FillRectangle(b, Width - buttonWidth, edge, buttonWidth - edge, Height - 2 * edge);
            }

            //Arrow
            TextRenderer.DrawText(e.Graphics, "\u25BC", Font,
                new Rectangle(Width - buttonWidth, edge, buttonWidth, Height - 2 * edge), ButtFore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

            //Text
            Rectangle textRect = new(edge, edge, Math.Max(0, Width - buttonWidth - edge), Math.Max(0, Height - 2 * edge));
            TextRenderer.DrawText(e.Graphics, Text, Font, textRect, Fore, Back,
                                  TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // NGComboBox
            // 
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular,
                                                System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ResumeLayout(false);
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.App.Info;

namespace mRemoteNG.UI.Forms
{
    [SupportedOSPlatform("windows")]
    public class FrmSplashScreenNew : Form
    {
        private static FrmSplashScreenNew? instance;
        private PrivateFontCollection? _fontCollection;

        public FrmSplashScreenNew()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;
            // Canvas size was 450x120 — the fixed title/version/subtitle layout ran
            // out of room (longer version strings + AI-assisted tagline were getting
            // clipped on the right). Doubled both dimensions to 900x240 (4x total
            // area) and the painter now centers everything with margins so future
            // tagline/version churn doesn't run off the edge.
            // #208: the canvas and every position below are in 96-DPI units and scaled to the
            // device DPI. The point-sized fonts already grow with the DPI; at 200% an unscaled
            // 900x240 canvas made the version, subtitle and tagline lines overlap.
            Size = CanvasSize(DeviceDpi);
            CenterOnPrimaryScreen();
        }

        internal const int DesignWidth = 900;
        internal const int DesignHeight = 240;

        internal static Size CanvasSize(int dpi) =>
            new((int)Math.Round(DesignWidth * dpi / 96f), (int)Math.Round(DesignHeight * dpi / 96f));

        /// <summary>Top of each text line, in device pixels, for a canvas at <paramref name="dpi"/>.</summary>
        internal static (float Title, float Version, float Subtitle, float Tagline) LineTops(int dpi)
        {
            float s = dpi / 96f;
            return (24f * s, 130f * s, 165f * s, 200f * s);
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            Size = CanvasSize(e.DeviceDpiNew);
            Invalidate();
        }

        private void CenterOnPrimaryScreen()
        {
            Rectangle workArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
            Left = workArea.Left + (workArea.Width - Width) / 2;
            Top = workArea.Top + (workArea.Height - Height) / 2;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            float scale = DeviceDpi / 96f;
            var tops = LineTops(DeviceDpi);

            // Background with rounded corners
            using GraphicsPath path = CreateRoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), (int)Math.Round(20 * scale));
            using SolidBrush bgBrush = new(Color.FromArgb(0x37, 0x3C, 0x42));
            using Pen borderPen = new(Color.FromArgb(0x21, 0x1E, 0x1B), 2);
            g.FillPath(bgBrush, path);
            g.DrawPath(borderPen, path);

            // Title: "m" in blue + "RemoteNG" in white, centered as a group.
            Font? brandFont = LoadBrandFont(80f * scale); // pixel unit: scaled here, not by GDI+
            Font fallbackFont = new("Segoe UI", 60f, FontStyle.Bold);
            Font titleFont = brandFont ?? fallbackFont;

            const string mGlyph = "m";
            const string restGlyph = "RemoteNG";
            SizeF mSize = g.MeasureString(mGlyph, titleFont);
            SizeF restSize = g.MeasureString(restGlyph, titleFont);
            // Kerning fudge — the custom brand font leaves too much air between
            // the 'm' and the 'R' when measured independently, so trim a touch.
            float kern = 16f * scale;
            float titleWidth = mSize.Width + restSize.Width - kern;
            float titleX = (Width - titleWidth) / 2f;
            float titleY = tops.Title;

            using SolidBrush blueBrush = new(Color.FromArgb(0x52, 0x89, 0xF9));
            using SolidBrush whiteBrush = new(Color.FromArgb(0xE8, 0xEB, 0xEE));
            g.DrawString(mGlyph, titleFont, blueBrush, titleX, titleY);
            g.DrawString(restGlyph, titleFont, whiteBrush, titleX + mSize.Width - kern, titleY);

            // Version — centered beneath the title.
            using Font versionFont = new("Segoe UI", 16f);
            string versionText = $"v. {GeneralAppInfo.ApplicationVersion} — Community Edition";
            SizeF versionSize = g.MeasureString(versionText, versionFont);
            g.DrawString(versionText, versionFont, whiteBrush, (Width - versionSize.Width) / 2f, tops.Version);

            // Subtitle
            using Font subtitleFont = new("Segoe UI", 14f, FontStyle.Bold);
            const string subtitle = "Multi-Remote Next Generation Connection Manager";
            SizeF subtitleSize = g.MeasureString(subtitle, subtitleFont);
            g.DrawString(subtitle, subtitleFont, whiteBrush, (Width - subtitleSize.Width) / 2f, tops.Subtitle);

            // Tagline — AI-assisted badge line, smaller than the subtitle.
            using Font taglineFont = new("Segoe UI", 11f, FontStyle.Italic);
            const string tagline = "AI-assisted open source · 16 protocols · .NET 10";
            SizeF taglineSize = g.MeasureString(tagline, taglineFont);
            g.DrawString(tagline, taglineFont, whiteBrush, (Width - taglineSize.Width) / 2f, tops.Tagline);

            if (brandFont != null) brandFont.Dispose();
            fallbackFont.Dispose();
        }

        private Font? LoadBrandFont(float size)
        {
            try
            {
                string fontPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UI", "Font", "HANDELGB.TTF");
                if (!System.IO.File.Exists(fontPath)) return null;
                _fontCollection ??= new PrivateFontCollection();
                if (_fontCollection.Families.Length == 0)
                    _fontCollection.AddFontFile(fontPath);
                return new Font(_fontCollection.Families[0], size, FontStyle.Bold, GraphicsUnit.Pixel);
            }
            catch
            {
                return null;
            }
        }

        private static GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static FrmSplashScreenNew GetInstance()
        {
            instance ??= new FrmSplashScreenNew();
            return instance;
        }

        public new void Show()
        {
            base.Show();
            Application.DoEvents();
        }

        public new void Close()
        {
            base.Close();
            instance = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fontCollection?.Dispose();
                _fontCollection = null;
            }
            base.Dispose(disposing);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                // WS_EX_LAYERED | WS_EX_TOOLWINDOW — allows per-pixel transparency, hidden from taskbar
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00080000 | 0x00000080;
                return cp;
            }
        }
    }
}

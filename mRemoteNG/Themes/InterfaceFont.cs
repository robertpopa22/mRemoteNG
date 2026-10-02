using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Properties;

namespace mRemoteNG.Themes
{
    [SupportedOSPlatform("windows")]
    internal static class InterfaceFont
    {
        public const string DefaultFamily = "Segoe UI";
        public const float DefaultSize = 8.25f;
        public const float MinSize = 6f;
        public const float MaxSize = 24f;

        public static FontStyle AllowedStyle(FontStyle style) =>
            style & (FontStyle.Bold | FontStyle.Italic);

        public static float ClampSize(float size)
        {
            if (float.IsNaN(size) || float.IsInfinity(size))
                return DefaultSize;
            if (size < MinSize)
                return MinSize;
            if (size > MaxSize)
                return MaxSize;
            return size;
        }

        public static Font Create(string? family, float size, FontStyle style)
        {
            float clamped = ClampSize(size);
            FontStyle allowed = AllowedStyle(style);
            string requested = string.IsNullOrWhiteSpace(family) ? DefaultFamily : family.Trim();
            string name = FamilyExists(requested) ? requested : DefaultFamily;
            try
            {
                return new Font(name, clamped, allowed, GraphicsUnit.Point);
            }
            catch (ArgumentException)
            {
                return new Font(DefaultFamily, clamped, FontStyle.Regular, GraphicsUnit.Point);
            }
        }

        private static bool FamilyExists(string name)
        {
            try
            {
                using FontFamily family = new(name);
                return family.IsStyleAvailable(FontStyle.Regular)
                    || family.IsStyleAvailable(FontStyle.Bold)
                    || family.IsStyleAvailable(FontStyle.Italic);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public static string Describe(Font font)
        {
            string text = font.FontFamily.Name + " " + font.Size.ToString("0.##", CultureInfo.InvariantCulture);
            if (font.Bold)
                text += " Bold";
            if (font.Italic)
                text += " Italic";
            return text;
        }

        public static void Apply(Control root, Font font)
        {
            ApplyControl(root, font);
        }

        public static void ApplyToOpenForms()
        {
            using Font font = Create(
                OptionsAppearancePage.Default.UIFontFamily,
                OptionsAppearancePage.Default.UIFontSize,
                (FontStyle)OptionsAppearancePage.Default.UIFontStyle);
            foreach (Form form in Application.OpenForms)
            {
                if (!form.IsDisposed)
                    Apply(form, font);
            }
        }

        private static void ApplyControl(Control control, Font font)
        {
            if (!Same(control.Font, font))
                control.Font = (Font)font.Clone();
            if (control.ContextMenuStrip != null)
                ApplyStrip(control.ContextMenuStrip, font);
            foreach (Control child in control.Controls)
                ApplyControl(child, font);
        }

        private static void ApplyStrip(ToolStrip strip, Font font)
        {
            if (!Same(strip.Font, font))
                strip.Font = (Font)font.Clone();
            foreach (ToolStripItem item in strip.Items)
                ApplyItem(item, font);
        }

        private static void ApplyItem(ToolStripItem item, Font font)
        {
            if (!Same(item.Font, font))
                item.Font = (Font)font.Clone();
            if (item is ToolStripDropDownItem drop)
            {
                foreach (ToolStripItem child in drop.DropDownItems)
                    ApplyItem(child, font);
            }
        }

        private static bool Same(Font current, Font chosen) =>
            string.Equals(current.FontFamily.Name, chosen.FontFamily.Name, StringComparison.Ordinal)
            && Math.Abs(current.SizeInPoints - chosen.SizeInPoints) < 0.05f
            && current.Style == chosen.Style;
    }
}

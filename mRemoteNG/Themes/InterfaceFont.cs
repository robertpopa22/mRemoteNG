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

        public static void Apply(Control root, Font font) => Apply(root, font, layoutWhenUnchanged: true);

        /// <summary>
        /// With <paramref name="layoutWhenUnchanged"/> false, the synchronous recursive layout is
        /// skipped when no font actually changed. The form Load wrappers use that: on every window
        /// open they re-applied the font the designer had already set, then forced PerformLayout on
        /// every control (#232). A DPI change keeps the layout, because sizes change while the
        /// point size stays the same.
        /// </summary>
        public static void Apply(Control root, Font font, bool layoutWhenUnchanged)
        {
            bool changed = ApplyControl(root, font);
            bool layout = changed || layoutWhenUnchanged;
            if (layout)
                LayoutChildren(root);
            if (root is UI.TaskDialog.frmTaskDialog dialog)
                dialog.LayoutToContent();
            if (root.IsHandleCreated)
                root.BeginInvoke((Action)(() =>
                {
                    if (root.IsDisposed) return;
                    // The deferred pass repeats the recursive layout for a font that changed;
                    // with nothing changed it only fits the window, as before.
                    if (layout)
                        LayoutChildren(root);
                    if (root is UI.TaskDialog.frmTaskDialog taskDialog) taskDialog.LayoutToContent();
                    if (root is UI.ResponsiveForm || root is UI.Window.BaseWindow and not UI.Window.ConnectionWindow)
                        UI.AdaptiveLayout.FitWorkingArea((Form)root);
                }));
        }

        private static void LayoutChildren(Control control)
        {
            foreach (Control child in control.Controls)
                LayoutChildren(child);
            if (UI.AdaptiveLayout.IsPreparedInput(control))
                control.MinimumSize = new Size(control.MinimumSize.Width,
                    Math.Max(control.Height, control.GetPreferredSize(Size.Empty).Height));
            control.PerformLayout();
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

        /// <returns>Whether any font in the tree was replaced.</returns>
        private static bool ApplyControl(Control control, Font font)
        {
            bool changed = false;
            bool arrow = control.Parent is NumericUpDown && control is Button { Text: "▲" or "▼" };
            bool symbol = string.Equals(control.Font.FontFamily.Name, "Marlett", StringComparison.OrdinalIgnoreCase);
            if (!arrow && !symbol && !Same(control.Font, font))
            {
                control.Font = (Font)font.Clone();
                changed = true;
            }
            if (control.ContextMenuStrip != null)
                changed |= ApplyStrip(control.ContextMenuStrip, font);
            // PropertyGrid owns the fonts and row metrics of its internal view.
            if (control is PropertyGrid) return changed;
            foreach (Control child in control.Controls)
                changed |= ApplyControl(child, font);
            return changed;
        }

        private static bool ApplyStrip(ToolStrip strip, Font font)
        {
            bool changed = false;
            if (!Same(strip.Font, font))
            {
                strip.Font = (Font)font.Clone();
                changed = true;
            }
            foreach (ToolStripItem item in strip.Items)
                changed |= ApplyItem(item, font);
            return changed;
        }

        private static bool ApplyItem(ToolStripItem item, Font font)
        {
            bool changed = false;
            if (!Same(item.Font, font))
            {
                item.Font = (Font)font.Clone();
                changed = true;
            }
            if (item is ToolStripDropDownItem drop)
            {
                foreach (ToolStripItem child in drop.DropDownItems)
                    changed |= ApplyItem(child, font);
            }
            return changed;
        }

        private static bool Same(Font current, Font chosen) =>
            string.Equals(current.FontFamily.Name, chosen.FontFamily.Name, StringComparison.Ordinal)
            && Math.Abs(current.SizeInPoints - chosen.SizeInPoints) < 0.05f
            && current.Style == chosen.Style;
    }
}

using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace mRemoteNG.UI.Controls
{
    /// <summary>
    /// A per-monitor DPI change leaves some chrome at the line spacing it was built with.
    /// The dock panel follows the window. Chrome that does not is rebuilt from that panel.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class DeviceDpiFontFit
    {
        // 9pt versus 8.25pt is about 9%. A font left on the previous monitor is about half or double.
        private const float Agreement = 0.15f;
        private const int LogicalIcon = 16;

        internal static string Describe(Font font, int deviceDpi)
        {
            int dpi = Math.Max(1, deviceDpi);
            return string.Create(CultureInfo.InvariantCulture,
                $"{font.Unit} size {font.Size:0.##} h@{dpi}={font.GetHeight(dpi):0.#}px");
        }

        internal static Font Match(Font current, Font reference, int deviceDpi)
        {
            int dpi = Math.Max(1, deviceDpi);
            float currentHeight = current.GetHeight(dpi);
            float referenceHeight = reference.GetHeight(dpi);
            if (currentHeight < 0.5f || referenceHeight < 0.5f)
                return current;

            float ratio = referenceHeight / currentHeight;
            if (ratio is > (1f - Agreement) and < (1f + Agreement))
                return current;

            float points = current.Unit == GraphicsUnit.Point
                ? current.Size
                : current.Size * 72f / dpi;
            float emPx = Math.Max(1f, points * ratio * dpi / 72f);
            if (current.Unit == GraphicsUnit.Pixel && Math.Abs(current.Size - emPx) < 0.5f)
                return current;

            return new Font(current.FontFamily, emPx, current.Style, GraphicsUnit.Pixel, current.GdiCharSet, current.GdiVerticalFont);
        }

        internal static bool FitControl(Control control, Font reference, int deviceDpi)
        {
            if (control.IsDisposed)
                return false;

            Font current = control.Font;
            Font fitted = Match(current, reference, deviceDpi);
            if (ReferenceEquals(fitted, current))
                return false;

            control.Font = fitted;
            if (current.Unit == GraphicsUnit.Pixel)
                current.Dispose();
            return true;
        }

        internal static bool FitToolStrip(ToolStrip strip, Font reference, int deviceDpi)
        {
            bool changed = FitControl(strip, reference, deviceDpi);
            int icon = Math.Max(1, (int)Math.Round(LogicalIcon * (Math.Max(1, deviceDpi) / 96d)));
            Size size = new(icon, icon);
            if (strip.ImageScalingSize != size)
            {
                strip.ImageScalingSize = size;
                changed = true;
            }

            return changed;
        }
    }
}

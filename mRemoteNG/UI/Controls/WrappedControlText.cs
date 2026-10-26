using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace mRemoteNG.UI.Controls;

[SupportedOSPlatform("windows")]
internal static class WrappedControlText
{
    public static Size Preferred(Control control, Size proposed, Size native, int textOffset)
    {
        int limit = proposed.Width > 0 ? proposed.Width : int.MaxValue;
        if (control.MaximumSize.Width > 0) limit = Math.Min(limit, control.MaximumSize.Width);
        if (!control.AutoSize && proposed.Width == 0 && control.Width > 0) limit = Math.Min(limit, control.Width);
        int textWidth = limit == int.MaxValue ? 32767 : Math.Max(1, limit - textOffset);
        Size text = TextRenderer.MeasureText(control.Text, control.Font, new Size(textWidth, 32767),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
        int width = Math.Max(native.Width, text.Width + textOffset);
        int height = Math.Max(native.Height, text.Height + 4 * control.DeviceDpi / 96);
        return new Size(Math.Max(control.MinimumSize.Width, width), Math.Max(control.MinimumSize.Height, height));
    }
}

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace mRemoteNG.Themes
{
    [SupportedOSPlatform("windows")]
    internal static class WindowTitleBar
    {
        private const int DwmwaUseImmersiveDarkMode = 20;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        public static void UseDarkCaption(IntPtr handle, bool dark)
        {
            if (handle == IntPtr.Zero)
                return;
            int value = dark ? 1 : 0;
            try
            {
                _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref value, sizeof(int));
            }
            catch (DllNotFoundException)
            {
            }
        }

        public static void ApplyToOpenForms(bool dark)
        {
            try
            {
                Application.SetColorMode(dark ? SystemColorMode.Dark : SystemColorMode.Classic);
            }
            catch (InvalidOperationException)
            {
            }

            foreach (Form form in Application.OpenForms)
            {
                if (!form.IsDisposed && form.IsHandleCreated)
                    UseDarkCaption(form.Handle, dark);
            }
        }
    }
}

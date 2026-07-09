using System;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Resources.Language;

namespace mRemoteNG.UI.Controls
{
    public class MrngSearchBox : MrngTextBox
    {
        private bool _showDefaultText = true;
        private bool _settingDefaultText = true;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            // Managed Focus() no-ops when an embedded protocol host (RDP ActiveX / reparented
            // PuTTY HWND) owns Win32 keyboard focus, so keystrokes keep going to the remote
            // session and the search box can't be typed into while a connection is open (#143).
            // Force Win32 focus onto the box, mirroring the #118 fallback in frmMain.
            if (!HasWin32Focus(this))
                NativeMethods.SetFocus(Handle);
        }

        private static bool HasWin32Focus(Control control)
        {
            if (!control.IsHandleCreated)
                return false;

            IntPtr focused = NativeMethods.GetFocus();
            return focused == control.Handle || NativeMethods.IsChild(control.Handle, focused);
        }

        public MrngSearchBox()
        {
            TextChanged += NGSearchBox_TextChanged;
            LostFocus += FocusLost;
            GotFocus += FocusGot;
        }

        private void FocusLost(object sender, EventArgs e)
        {
            if (!_showDefaultText)
                return;

            _settingDefaultText = true;
            Text = Language.SearchPrompt;
        }

        private void FocusGot(object sender, EventArgs e)
        {
            if (_showDefaultText)
                Text = "";
        }

        private void NGSearchBox_TextChanged(object sender, EventArgs e)
        {
            if (!_settingDefaultText)
            {
                _showDefaultText = string.IsNullOrEmpty(Text);
            }

            _settingDefaultText = false;
        }
    }
}

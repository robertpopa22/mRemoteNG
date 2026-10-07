using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Themes;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNG.UI.Tabs
{
    class FloatWindowNG : FloatWindow
    {
        public FloatWindowNG(DockPanel dockPanel, DockPane pane)
            : base(dockPanel, pane)
        {
            setDefaultProperties();
        }

        public FloatWindowNG(DockPanel dockPanel, DockPane pane, Rectangle bounds)
            : base(dockPanel, pane, bounds)
        {
            setDefaultProperties();
        }

        private void setDefaultProperties()
        {
            FormBorderStyle = FormBorderStyle.Sizable;

            // To enable Alt+Tab between your undocked forms and your main form
            ShowInTaskbar = true;
            Owner = null;

            // Allow the Windows default behavior of maximizing/restoring the window
            DoubleClickTitleBarToDock = true;
        }

        internal void SetWindowedFullscreenWorkingArea(Rectangle workingArea)
        {
            MaximizedBounds = workingArea;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            WindowTitleBar.UseDarkCaption(Handle, OsAppTheme.CaptionIsDark(ThemeManager.getInstance().ActiveTheme));
        }

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern uint SendMessage(IntPtr hWnd, int Msg, uint wParam, uint lParam);

        //[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m)
        {
            int WM_NCLBUTTONDOWN = 0x00A1;
            int WM_SYSCOMMAND = 0x0112;
            int WM_CLOSE = 0x0010;

            int SC_MINIMIZE = 0xF020;
            int SC_RESTORE = 0xF120;

            if (m.Msg == NativeMethods.WM_HOTKEY && WindowedFullscreenManager.HandleHotKey(this, (int)m.WParam))
                return;

            // DockPanelSuite handles WM_CLOSE on its float host before the normal FormClosing
            // path. Return the live tab to its original pane here; application shutdown must
            // continue through the base implementation so the protocol is actually closed.
            if (m.Msg == WM_CLOSE && !mRemoteNG.UI.Forms.FrmMain.Default.IsClosing &&
                WindowedFullscreenManager.TryExit(this))
                return;

            if (m.Msg == WM_NCLBUTTONDOWN)
            {
                if (IsDisposed)
                    return;

                if ((uint)m.WParam == 8) // Check if button down occured in minimize box
                {
                    if (WindowState == FormWindowState.Minimized)
                        _ = FloatWindowNG.SendMessage(Handle, (int)WM_SYSCOMMAND, (uint)SC_RESTORE, 0);
                    else
                        _ = FloatWindowNG.SendMessage(Handle, (int)WM_SYSCOMMAND, (uint)SC_MINIMIZE, 0);

                    return;
                }
            }

            base.WndProc(ref m);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (Owner != null)
            {
                Owner = null;
            }

            if (!ShowInTaskbar)
            {
                ShowInTaskbar = true;
            }

            WindowedFullscreenManager.ConfigureWindow(this);
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            WindowedFullscreenManager.WindowActivated(this);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            WindowedFullscreenManager.WindowDeactivated(this);
            base.OnDeactivate(e);
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            WindowedFullscreenManager.WindowBoundsChanged(this);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            WindowedFullscreenManager.WindowBoundsChanged(this);
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            WindowedFullscreenManager.WindowBoundsChanged(this);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Alt | Keys.Enter) &&
                WindowedFullscreenManager.TryExit(this))
                return true;

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.ApplicationExitCall &&
                !mRemoteNG.UI.Forms.FrmMain.Default.IsClosing &&
                WindowedFullscreenManager.TryExit(this))
            {
                e.Cancel = true;
                return;
            }

            WindowedFullscreenManager.Forget(this);
            base.OnFormClosing(e);
        }
    }
}

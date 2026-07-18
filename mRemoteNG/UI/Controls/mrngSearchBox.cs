using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Messages;
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
            ForceKeyboardFocusAcrossInputQueues();
        }

        // Keyboard focus is per-input-queue state: GetFocus()/SetFocus() only see the UI
        // thread's own queue. While an embedded protocol host owns the input — the RDP
        // ActiveX input window lives on an mstscax worker thread, PuTTY is a reparented
        // window of a separate process — a plain SetFocus() from the UI thread updates a
        // dormant queue and keystrokes keep flowing to the remote session, which is why
        // the earlier same-queue fallbacks (be055a146, 94d21393d) had no effect (#143).
        // Read the system-wide focus window via GetGUIThreadInfo(idThread=0 -> foreground
        // thread) and, when it belongs to a foreign thread, bridge the two input queues
        // with AttachThreadInput for the duration of the SetFocus call.
        private void ForceKeyboardFocusAcrossInputQueues()
        {
            if (!IsHandleCreated)
                return;

            try
            {
                NativeMethods.GUITHREADINFO info = new()
                {
                    cbSize = (uint)Marshal.SizeOf<NativeMethods.GUITHREADINFO>()
                };
                IntPtr globalFocus = NativeMethods.GetGUIThreadInfo(0, ref info) ? info.hwndFocus : IntPtr.Zero;

                if (globalFocus == Handle)
                    return;

                uint ownThread = NativeMethods.GetWindowThreadProcessId(Handle, out _);
                uint focusThread = globalFocus == IntPtr.Zero
                    ? 0
                    : NativeMethods.GetWindowThreadProcessId(globalFocus, out _);

                bool attached = false;
                try
                {
                    if (focusThread != 0 && ownThread != 0 && focusThread != ownThread)
                        attached = NativeMethods.AttachThreadInput(ownThread, focusThread, true);

                    NativeMethods.SetFocus(Handle);
                }
                finally
                {
                    if (attached)
                        NativeMethods.AttachThreadInput(ownThread, focusThread, false);
                }

                NativeMethods.GUITHREADINFO after = new()
                {
                    cbSize = (uint)Marshal.SizeOf<NativeMethods.GUITHREADINFO>()
                };
                IntPtr focusNow = NativeMethods.GetGUIThreadInfo(0, ref after) ? after.hwndFocus : IntPtr.Zero;
                Diag143($"globalFocus=0x{globalFocus.ToInt64():X} focusThread={focusThread} " +
                        $"ownThread={ownThread} attached={attached} reclaimed={focusNow == Handle}");
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector?.AddExceptionStackTrace("Search box focus reclaim failed", ex, MessageClass.WarningMsg, false);
            }
        }

        // TEMP diagnostic for #143 (search box unusable while RDP/PuTTY session active).
        // Remove once the reporter confirms the cross-queue reclaim works.
        // Logs to %LOCALAPPDATA%\mRemoteNG\mRemoteNG.log.
        private static void Diag143(string msg) =>
            Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg, $"[#143-diag] {msg}", true);

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

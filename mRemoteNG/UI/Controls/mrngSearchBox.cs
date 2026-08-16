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
        private bool _focusRetryPending;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            ForceKeyboardFocusAcrossInputQueues();
            QueueDeferredFocusRetry();
        }

        // A protocol host that re-asserts Win32 focus from its own WM_KILLFOCUS handler
        // wins every reclaim attempted from inside the current mouse message, because the
        // reclaim and the defence run in the same synchronous SendMessage. Retry once more
        // after the message loop has drained the click, when the host is no longer
        // defending the transition (#143).
        private void QueueDeferredFocusRetry()
        {
            if (_focusRetryPending || IsDisposed || !IsHandleCreated)
                return;

            _focusRetryPending = true;
            BeginInvoke((MethodInvoker)(() =>
            {
                _focusRetryPending = false;
                if (!IsDisposed && IsHandleCreated && CanFocus)
                    ForceKeyboardFocusAcrossInputQueues();
            }));
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

                // AttachThreadInput is not reference counted: detaching a pair we did not
                // attach tears down the attachment the protocol host established for
                // itself. When our own queue already reports the system-wide focus window,
                // the two queues are shared already and no bridge is needed — building and
                // then tearing one down here actively broke input routing (#143).
                bool queuesAlreadyShared = NativeMethods.GetFocus() == globalFocus;

                bool attached = false;
                try
                {
                    if (!queuesAlreadyShared && focusThread != 0 && ownThread != 0 && focusThread != ownThread)
                        attached = NativeMethods.AttachThreadInput(ownThread, focusThread, true);

                    NativeMethods.SetFocus(Handle);
                }
                finally
                {
                    if (attached)
                        NativeMethods.AttachThreadInput(ownThread, focusThread, false);
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector?.AddExceptionStackTrace("Search box focus reclaim failed", ex, MessageClass.WarningMsg, false);
            }
        }

        // The Win32 trace from the reporter shows SetFocus returning success while the
        // system-wide focus window stays put, and this control's Focused staying false. That
        // rules out the input-queue explanations the previous four attempts were built on and
        // leaves the managed side: WinForms routes keystrokes by the ActiveControl chain hanging
        // off the form, not by the raw focus window, and an ActiveX host that re-asserts itself
        // through its container's ActiveControl is invisible to every Win32 probe. Dump the
        // chain, and the identity of whatever actually holds the focus window, so the next step
        // is decided by evidence instead of a fifth guess (#143).
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

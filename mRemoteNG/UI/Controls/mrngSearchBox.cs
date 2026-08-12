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
        private int _keyDiagBudget = 10;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            ForceKeyboardFocusAcrossInputQueues("click");
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
                    ForceKeyboardFocusAcrossInputQueues("deferred");
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
        private void ForceKeyboardFocusAcrossInputQueues(string phase)
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
                IntPtr setFocusResult;
                int lastError;
                IntPtr focusAfterSetFocus;
                try
                {
                    if (!queuesAlreadyShared && focusThread != 0 && ownThread != 0 && focusThread != ownThread)
                        attached = NativeMethods.AttachThreadInput(ownThread, focusThread, true);

                    setFocusResult = NativeMethods.SetFocus(Handle);
                    lastError = Marshal.GetLastWin32Error();
                    focusAfterSetFocus = NativeMethods.GetFocus();
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
                Diag143($"{phase} own=0x{Handle.ToInt64():X} globalFocus=0x{globalFocus.ToInt64():X} " +
                        $"[{DescribeWindow(globalFocus)}] focusThread={focusThread} " +
                        $"ownThread={ownThread} sharedQueue={queuesAlreadyShared} attached={attached} " +
                        $"setFocusRet=0x{setFocusResult.ToInt64():X} err={lastError} " +
                        $"focusAfterSetFocus=0x{focusAfterSetFocus.ToInt64():X} " +
                        $"enabled={NativeMethods.IsWindowEnabled(Handle)} visible={NativeMethods.IsWindowVisible(Handle)} " +
                        $"foreground=0x{NativeMethods.GetForegroundWindow().ToInt64():X} " +
                        $"reclaimed={focusNow == Handle}");
                Diag143($"{phase} managed {DescribeManagedFocus()}");
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
        private string DescribeManagedFocus()
        {
            Form? form = FindForm();
            string chain = "none";

            if (form != null)
            {
                System.Text.StringBuilder sb = new(form.Name);
                Control? current = form.ActiveControl;
                int depth = 0;
                while (current != null && depth++ < 12)
                {
                    sb.Append(" > ").Append(current.Name).Append('(').Append(current.GetType().Name).Append(')');
                    current = current is ContainerControl container ? container.ActiveControl : null;
                }

                chain = sb.ToString();
            }

            return $"activeChain={chain} thisFocused={Focused} thisContainsFocus={ContainsFocus} " +
                   $"formActive={form?.ContainsFocus} parentActive={Parent?.ContainsFocus}";
        }

        // Maps a window handle back to the managed control that owns it, when there is one, so
        // the focus window in the trace stops being an anonymous number.
        private static string DescribeWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return "null";

            System.Text.StringBuilder className = new(256);
            if (NativeMethods.GetClassName(hWnd, className, className.Capacity) == 0)
                className.Append("<unknown>");

            Control? managed = Control.FromHandle(hWnd) ?? Control.FromChildHandle(hWnd);
            string owner = managed == null
                ? "unmanaged"
                : $"{managed.Name}({managed.GetType().Name})";

            return $"class={className} owner={owner}";
        }

        // Whoever takes focus away does it either through WinForms (a managed frame will be on
        // the stack) or by calling SetFocus from the message pump (no managed frames). That
        // single distinction separates the two remaining explanations.
        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            const int WM_SETFOCUS = 0x0007;
            const int WM_KILLFOCUS = 0x0008;
            const int WM_KEYDOWN = 0x0100;
            const int WM_CHAR = 0x0102;

            // Whether keystrokes reach this control at all is the other half of the answer: the
            // reporter sees an unusable box, which is either "no key messages arrive" (focus
            // really is elsewhere) or "they arrive and are discarded" (something eats them).
            if (m.Msg is WM_KEYDOWN or WM_CHAR && _keyDiagBudget > 0)
            {
                _keyDiagBudget--;
                Diag143($"{(m.Msg == WM_KEYDOWN ? "WM_KEYDOWN" : "WM_CHAR")} key=0x{m.WParam.ToInt64():X} " +
                        DescribeManagedFocus());
            }

            if (m.Msg is WM_SETFOCUS or WM_KILLFOCUS)
            {
                string name = m.Msg == WM_SETFOCUS ? "WM_SETFOCUS" : "WM_KILLFOCUS";
                Diag143($"{name} other=0x{m.WParam.ToInt64():X} [{DescribeWindow(m.WParam)}] {DescribeManagedFocus()}");

                if (m.Msg == WM_KILLFOCUS)
                    Diag143("WM_KILLFOCUS stack: " + new System.Diagnostics.StackTrace(false).ToString()
                                                         .Replace(Environment.NewLine, " | "));
            }

            base.WndProc(ref m);
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

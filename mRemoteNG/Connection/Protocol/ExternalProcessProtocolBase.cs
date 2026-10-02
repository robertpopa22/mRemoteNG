using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Management;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Messages;
using mRemoteNG.Resources.Language;

namespace mRemoteNG.Connection.Protocol
{
    /// <summary>
    /// Base class for protocols that launch and embed an external process window
    /// (IntegratedProgram, Winbox, VMRC, AnyDesk, MSRA) and for console-based
    /// protocols (Terminal, OpenSSH, WSL, PowerShell) that share the same
    /// Resize/MoveWindow logic.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public abstract class ExternalProcessProtocolBase : ProtocolBase
    {
        protected Process? _process;
        protected IntPtr _handle;

        #region Window Finding

        private const string PseudoConsoleWindowClass = "PseudoConsoleWindow";

        /// <summary>
        /// A visible top-level window that can be reparented. A console handed to Windows Terminal
        /// leaves a zero-size PseudoConsoleWindow that reports itself visible.
        /// </summary>
        protected static bool IsEmbeddableWindow(IntPtr hWnd)
        {
            if (!NativeMethods.IsWindowVisible(hWnd))
                return false;

            StringBuilder className = new(256);
            if (NativeMethods.GetClassName(hWnd, className, className.Capacity) <= 0)
                return true;

            return IsDockableWindowClass(className.ToString());
        }

        internal static bool IsDockableWindowClass(string className) =>
            !string.Equals(className, PseudoConsoleWindowClass, StringComparison.Ordinal);

        /// <summary>
        /// Polls Process.MainWindowHandle for up to <paramref name="timeoutMs"/> milliseconds.
        /// Works for direct GUI apps (PuTTY, Notepad++, etc.).
        /// </summary>
        protected static IntPtr PollMainWindowHandle(Process process, int timeoutMs)
        {
            IntPtr handle = IntPtr.Zero;
            int startTicks = Environment.TickCount;
            while (handle == IntPtr.Zero &&
                   Environment.TickCount < startTicks + timeoutMs)
            {
                try
                {
                    if (process.HasExited) break;
                    process.Refresh();
                    if (!string.Equals(process.MainWindowTitle, "Default IME", StringComparison.Ordinal))
                    {
                        IntPtr mainWindow = process.MainWindowHandle;
                        if (mainWindow != IntPtr.Zero && IsEmbeddableWindow(mainWindow))
                            handle = mainWindow;
                    }
                }
                catch (InvalidOperationException)
                {
                    break; // Process exited
                }

                if (handle == IntPtr.Zero)
                    Thread.Sleep(50);
            }
            return handle;
        }

        /// <summary>
        /// Uses EnumWindows + GetWindowThreadProcessId to find a visible top-level window
        /// belonging to the given process ID. Catches windows that .NET's MainWindowHandle misses
        /// (e.g. conhost windows, multi-window apps).
        /// </summary>
        protected static IntPtr FindWindowByProcessId(int processId, int timeoutMs)
        {
            IntPtr found = IntPtr.Zero;
            int startTicks = Environment.TickCount;
            while (found == IntPtr.Zero &&
                   Environment.TickCount < startTicks + timeoutMs)
            {
                NativeMethods.EnumWindows((hWnd, lParam) =>
                {
                    _ = NativeMethods.GetWindowThreadProcessId(hWnd, out uint windowPid);
                    if (windowPid == (uint)processId && IsEmbeddableWindow(hWnd))
                    {
                        found = hWnd;
                        return false; // Stop enumeration
                    }
                    return true;
                }, IntPtr.Zero);

                if (found == IntPtr.Zero)
                    Thread.Sleep(50);
            }
            return found;
        }

        /// <summary>
        /// Searches for visible windows belonging to child processes of the given parent PID.
        /// Handles launcher-style apps (git-bash.exe -> mintty.exe, wt.exe -> child, etc.)
        /// where the launched process spawns a child and may exit.
        /// </summary>
        protected static IntPtr FindWindowInChildProcesses(int parentProcessId, int timeoutMs)
        {
            IntPtr found = IntPtr.Zero;
            int startTicks = Environment.TickCount;
            while (found == IntPtr.Zero &&
                   Environment.TickCount < startTicks + timeoutMs)
            {
                List<int> childPids = GetChildProcessIds(parentProcessId);
                foreach (int childPid in childPids)
                {
                    NativeMethods.EnumWindows((hWnd, lParam) =>
                    {
                        _ = NativeMethods.GetWindowThreadProcessId(hWnd, out uint windowPid);
                        if (windowPid == (uint)childPid && IsEmbeddableWindow(hWnd))
                        {
                            found = hWnd;
                            return false;
                        }
                        return true;
                    }, IntPtr.Zero);

                    if (found != IntPtr.Zero) break;
                }

                if (found == IntPtr.Zero)
                    Thread.Sleep(100);
            }
            return found;
        }

        /// <summary>
        /// Searches for visible windows belonging to descendants of the given root PID.
        /// Useful for launchers that create a multi-level process chain.
        /// </summary>
        protected static IntPtr FindWindowInDescendantProcesses(int rootProcessId, int timeoutMs, int maxDepth)
        {
            IntPtr found = IntPtr.Zero;
            int startTicks = Environment.TickCount;
            while (found == IntPtr.Zero &&
                   Environment.TickCount < startTicks + timeoutMs)
            {
                List<int> descendantPids = GetDescendantProcessIds(rootProcessId, maxDepth);
                foreach (int descendantPid in descendantPids)
                {
                    NativeMethods.EnumWindows((hWnd, lParam) =>
                    {
                        _ = NativeMethods.GetWindowThreadProcessId(hWnd, out uint windowPid);
                        if (windowPid == (uint)descendantPid && IsEmbeddableWindow(hWnd))
                        {
                            found = hWnd;
                            return false;
                        }
                        return true;
                    }, IntPtr.Zero);

                    if (found != IntPtr.Zero) break;
                }

                if (found == IntPtr.Zero)
                    Thread.Sleep(100);
            }
            return found;
        }

        /// <summary>
        /// Gets child process IDs for a given parent process ID via WMI.
        /// </summary>
        protected static List<int> GetChildProcessIds(int parentPid)
        {
            List<int> children = [];
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT ProcessId FROM Win32_Process WHERE ParentProcessId = {parentPid}");
                foreach (ManagementObject obj in searcher.Get())
                {
                    children.Add(Convert.ToInt32(obj["ProcessId"], CultureInfo.InvariantCulture));
                }
            }
            catch
            {
                // WMI query can fail if access is denied or service unavailable — not critical
            }
            return children;
        }

        protected static List<int> GetDescendantProcessIds(int rootProcessId, int maxDepth)
        {
            if (maxDepth <= 0)
                return [];

            List<int> descendants = [];
            HashSet<int> visited = [rootProcessId];
            List<int> currentLevel = [rootProcessId];

            for (int depth = 0; depth < maxDepth && currentLevel.Count > 0; depth++)
            {
                List<int> nextLevel = [];
                foreach (int pid in currentLevel)
                {
                    List<int> children = GetChildProcessIds(pid);
                    foreach (int childPid in children)
                    {
                        if (visited.Add(childPid))
                        {
                            descendants.Add(childPid);
                            nextLevel.Add(childPid);
                        }
                    }
                }

                currentLevel = nextLevel;
            }

            return descendants;
        }

        #endregion

        #region Resize / Focus / Close

        /// <summary>
        /// Standard MoveWindow resize for embedded external process windows.
        /// </summary>
        protected override void Resize(object sender, EventArgs e)
        {
            try
            {
                if (_handle == IntPtr.Zero || InterfaceControl.Size == Size.Empty)
                    return;

                // Use ClientRectangle to account for padding (for connection frame color)
                Rectangle clientRect = InterfaceControl.ClientRectangle;
                NativeMethods.MoveWindow(_handle,
                    clientRect.X - SystemInformation.FrameBorderSize.Width,
                    clientRect.Y - (SystemInformation.CaptionHeight + SystemInformation.FrameBorderSize.Height),
                    clientRect.Width + SystemInformation.FrameBorderSize.Width * 2,
                    clientRect.Height + SystemInformation.CaptionHeight +
                    SystemInformation.FrameBorderSize.Height * 2, true);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector?.AddExceptionMessage(Language.IntAppResizeFailed, ex);
            }
        }

        public override void Focus()
        {
            try
            {
                if (_handle != IntPtr.Zero)
                {
                    NativeMethods.SetForegroundWindow(_handle);
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector?.AddExceptionMessage(Language.IntAppFocusFailed, ex);
            }
        }

        public override void Close()
        {
            if (_process != null)
            {
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.Kill();
                    }
                }
                catch (Exception ex)
                {
                    Runtime.MessageCollector?.AddExceptionMessage(Language.IntAppKillFailed, ex);
                }
                finally
                {
                    _process?.Dispose();
                    _process = null;
                }
            }

            base.Close();
        }

        protected void ProcessExited(object sender, EventArgs e)
        {
            Event_Closed(this);
        }

        #endregion
    }
}

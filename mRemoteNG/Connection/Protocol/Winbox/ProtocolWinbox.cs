using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Messages;
using mRemoteNG.Resources.Language;
using mRemoteNG.Tools;

namespace mRemoteNG.Connection.Protocol.Winbox
{
    [SupportedOSPlatform("windows")]
    public class ProtocolWinbox : ProtocolBase
    {
        #region Private Fields

        private IntPtr _handle;
        private readonly ConnectionInfo _connectionInfo;
        private Process? _process;
        private const string DefaultWinboxPath = "winbox.exe";
        private const string DefaultWinbox64Path = "winbox64.exe";

        #endregion

        #region Constructor

        public ProtocolWinbox(ConnectionInfo connectionInfo)
        {
            _connectionInfo = connectionInfo;
        }

        #endregion

        #region Public Methods

        public override bool Initialize()
        {
            return base.Initialize();
        }

        public override bool Connect()
        {
            try
            {
                Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                    "Attempting to start Winbox connection.", true);

                // Find Winbox executable
                string? winboxPath = FindWinboxExecutable();
                if (string.IsNullOrEmpty(winboxPath))
                {
                    Runtime.MessageCollector?.AddMessage(MessageClass.ErrorMsg,
                        "Winbox executable not found. Please ensure winbox.exe or winbox64.exe is in your PATH or in the same directory as mRemoteNG.", true);
                    return false;
                }

                // Validate the executable path
                PathValidator.ValidateExecutablePathOrThrow(winboxPath, "Winbox");

                // Build arguments
                string arguments = BuildArguments();

                _process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = winboxPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false
                    },
                    EnableRaisingEvents = true
                };

                _process.Exited += ProcessExited;
                _process.Start();

                // Wait for input idle (if applicable)
                try
                {
                    _process.WaitForInputIdle(Properties.OptionsAdvancedPage.Default.MaxPuttyWaitTime * 1000);
                }
                catch (InvalidOperationException)
                {
                    // Expected if Winbox behaves like a console app initially or exits quickly (wrapper)
                }

                int timeoutMs = Properties.OptionsAdvancedPage.Default.MaxPuttyWaitTime * 1000;
                int processId = _process.Id;

                // Strategy 1: Poll Process.MainWindowHandle
                _handle = PollMainWindowHandle(_process, timeoutMs);

                // Strategy 2: EnumWindows to find any visible top-level window owned by the process ID
                if (_handle == IntPtr.Zero)
                {
                    _handle = FindWindowByProcessId(processId, timeoutMs);
                }

                if (_handle == IntPtr.Zero)
                {
                    Runtime.MessageCollector?.AddMessage(MessageClass.WarningMsg,
                        $"Winbox: Could not find a window handle for (PID {processId}). " +
                        "The application may have opened in a separate window or failed to start.");
                    return false;
                }
                
                // Reparent the window
                NativeMethods.SetParent(_handle, InterfaceControl.Handle);
                
                // Notify user
                Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg, Language.IntAppStuff, true);
                Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                                                     string.Format(Language.IntAppHandle, _handle), true);

                Resize(this, new EventArgs());
                base.Connect();
                return true;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector?.AddExceptionMessage(Language.ConnectionFailed, ex);
                return false;
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

        public override void Close()
        {
            try
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
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector?.AddExceptionMessage("Error closing Winbox connection.", ex);
            }

            base.Close();
        }

        #endregion

        #region Private Methods

        private string? FindWinboxExecutable()
        {
            // Check PATH
            string? pathVariable = Environment.GetEnvironmentVariable("PATH");
            if (pathVariable != null)
            {
                var paths = pathVariable.Split(Path.PathSeparator);
                foreach (var path in paths)
                {
                    var exePath = Path.Combine(path.Trim(), DefaultWinboxPath);
                    if (File.Exists(exePath)) return exePath;
                    
                    exePath = Path.Combine(path.Trim(), DefaultWinbox64Path);
                    if (File.Exists(exePath)) return exePath;
                }
            }
            
            // Check current directory
            if (File.Exists(DefaultWinboxPath)) return Path.GetFullPath(DefaultWinboxPath);
            if (File.Exists(DefaultWinbox64Path)) return Path.GetFullPath(DefaultWinbox64Path);

            return null;
        }

        private string BuildArguments()
        {
            // Winbox CLI: <address> <user> <password>
            // Winbox is lenient with arguments.
            string address = _connectionInfo.Hostname;
            string user = _connectionInfo.Username;
            string password = _connectionInfo.Password;

            // Handle port if specified in hostname or port field?
            // Usually Winbox uses address:port.
            if (_connectionInfo.Port > 0 && !address.Contains(":"))
            {
                 address = $"{address}:{_connectionInfo.Port}";
            }

            return $"\"{address}\" \"{user}\" \"{password}\"";
        }

        private void ProcessExited(object sender, EventArgs e)
        {
            Event_Closed(this);
        }

        private static IntPtr PollMainWindowHandle(Process process, int timeoutMs)
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
                    if (process.MainWindowTitle != "Default IME")
                    {
                        handle = process.MainWindowHandle;
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

        private static IntPtr FindWindowByProcessId(int processId, int timeoutMs)
        {
            IntPtr found = IntPtr.Zero;
            int startTicks = Environment.TickCount;
            while (found == IntPtr.Zero &&
                   Environment.TickCount < startTicks + timeoutMs)
            {
                NativeMethods.EnumWindows((hWnd, _) =>
                {
                    NativeMethods.GetWindowThreadProcessId(hWnd, out uint windowPid);
                    if (windowPid == (uint)processId && NativeMethods.IsWindowVisible(hWnd))
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

        #endregion
        #region Enumerations

        public enum Defaults
        {
            Port = 8291
        }

        #endregion
    }
}

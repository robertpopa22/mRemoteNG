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

namespace mRemoteNG.Connection.Protocol.AnyDesk
{
    [SupportedOSPlatform("windows")]
    public class ProtocolAnyDesk : ProtocolBase
    {
        #region Private Fields

        private IntPtr _handle;
        private readonly ConnectionInfo _connectionInfo;
        private Process _process;
        private const string DefaultAnydeskPath = @"C:\Program Files (x86)\AnyDesk\AnyDesk.exe";
        private const string AlternateAnydeskPath = @"C:\Program Files\AnyDesk\AnyDesk.exe";

        #endregion

        #region Constructor

        public ProtocolAnyDesk(ConnectionInfo connectionInfo)
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
                    "Attempting to start AnyDesk connection.", true);

                // Validate AnyDesk installation
                string anydeskPath = FindAnydeskExecutable();
                if (string.IsNullOrEmpty(anydeskPath))
                {
                    Runtime.MessageCollector?.AddMessage(MessageClass.ErrorMsg,
                        "AnyDesk is not installed. Please install AnyDesk to use this protocol.", true);
                    return false;
                }

                // Validate connection info
                if (string.IsNullOrEmpty(_connectionInfo.Hostname))
                {
                    Runtime.MessageCollector?.AddMessage(MessageClass.ErrorMsg,
                        "AnyDesk ID is required in the Hostname field.", true);
                    return false;
                }

                // Start AnyDesk connection
                if (!StartAnydeskConnection(anydeskPath))
                {
                    return false;
                }

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
                // Try to close all AnyDesk processes related to this connection
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

                // Also try to close by window handle if we have it
                if (_handle != IntPtr.Zero)
                {
                    try
                    {
                        NativeMethods.SendMessage(_handle, 0x0010, IntPtr.Zero, IntPtr.Zero); // WM_CLOSE
                    }
                    catch
                    {
                        // Ignore errors when closing by handle
                    }
                    _handle = IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector?.AddExceptionMessage("Error closing AnyDesk connection.", ex);
            }

            base.Close();
        }

        #endregion

        #region Private Methods

        private string FindAnydeskExecutable()
        {
            // Check common installation paths
            if (File.Exists(DefaultAnydeskPath))
            {
                return DefaultAnydeskPath;
            }

            if (File.Exists(AlternateAnydeskPath))
            {
                return AlternateAnydeskPath;
            }

            // Check if it's in PATH
            string pathVariable = Environment.GetEnvironmentVariable("PATH");
            if (pathVariable != null)
            {
                var paths = pathVariable.Split(';');
                foreach (var path in paths)
                {
                    var exePath = Path.Combine(path.Trim(), "AnyDesk.exe");
                    if (File.Exists(exePath))
                    {
                        return exePath;
                    }
                }
            }

            return null;
        }

        private bool StartAnydeskConnection(string anydeskPath)
        {
            try
            {
                // Build AnyDesk arguments
                // Format: AnyDesk.exe [ID|alias@ad] [options]
                // Hostname field contains the AnyDesk ID (e.g., 123456789 or alias@ad)
                // Username field is optional and not used in the CLI (reserved for future use)
                // Password field is piped via stdin when --with-password flag is used
                string anydeskId = _connectionInfo.Hostname.Trim();
                string arguments = $"{anydeskId}";

                // Add --with-password flag if password is provided
                bool hasPassword = !string.IsNullOrEmpty(_connectionInfo.Password);
                if (hasPassword)
                {
                    arguments += " --with-password";
                }

                // Add --plain flag to minimize UI (optional)
                arguments += " --plain";

                Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                    $"Starting AnyDesk with ID: {anydeskId}", true);

                // If password is provided, we need to pipe it to AnyDesk
                if (hasPassword)
                {
                    return StartAnydeskWithPassword(anydeskPath, arguments);
                }
                else
                {
                    return StartAnydeskWithoutPassword(anydeskPath, arguments);
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector?.AddExceptionMessage("Failed to start AnyDesk connection.", ex);
                return false;
            }
        }

        private bool StartAnydeskWithPassword(string anydeskPath, string arguments)
        {
            try
            {
                // Use PowerShell to pipe the password to AnyDesk
                // This is the recommended way according to AnyDesk documentation
                string powershellCommand = $"echo '{_connectionInfo.Password}' | & '{anydeskPath}' {arguments}";

                _process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-WindowStyle Hidden -Command \"{powershellCommand}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                        RedirectStandardInput = false
                    },
                    EnableRaisingEvents = true
                };

                _process.Exited += ProcessExited;
                _process.Start();

                // Wait for the AnyDesk window to appear
                // Note: The window belongs to the AnyDesk process, not PowerShell
                if (!WaitForAnydeskWindow())
                {
                    Runtime.MessageCollector?.AddMessage(MessageClass.WarningMsg,
                        "AnyDesk window did not appear within the expected time.", true);
                    return false;
                }

                base.Connect();
                return true;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector?.AddExceptionMessage("Failed to start AnyDesk with password.", ex);
                return false;
            }
        }

        private bool StartAnydeskWithoutPassword(string anydeskPath, string arguments)
        {
            try
            {
                _process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = anydeskPath,
                        Arguments = arguments,
                        UseShellExecute = true
                    },
                    EnableRaisingEvents = true
                };

                _process.Exited += ProcessExited;
                _process.Start();

                // Wait for the AnyDesk window to appear
                if (!WaitForAnydeskWindow())
                {
                    Runtime.MessageCollector?.AddMessage(MessageClass.WarningMsg,
                        "AnyDesk window did not appear within the expected time.", true);
                    return false;
                }

                base.Connect();
                return true;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector?.AddExceptionMessage("Failed to start AnyDesk connection.", ex);
                return false;
            }
        }

        private bool WaitForAnydeskWindow()
        {
            // Wait up to 10 seconds for AnyDesk window to appear
            int maxWaitTime = 10000; // 10 seconds
            int waitInterval = 100; // 100 ms
            int elapsedTime = 0;

            while (elapsedTime < maxWaitTime)
            {
                // Find AnyDesk process by name
                Process[] anydeskProcesses = Process.GetProcessesByName("AnyDesk");
                
                foreach (Process anydeskProcess in anydeskProcesses)
                {
                    try
                    {
                        anydeskProcess.Refresh();
                        
                        // Try to get the main window handle
                        if (anydeskProcess.MainWindowHandle != IntPtr.Zero)
                        {
                            _handle = anydeskProcess.MainWindowHandle;

                            // Store the actual AnyDesk process for later cleanup
                            // Dispose the PowerShell process if it's different
                            if (_process != null && _process.ProcessName != "AnyDesk")
                            {
                                _process.Exited -= ProcessExited;
                                _process = anydeskProcess;
                                _process.EnableRaisingEvents = true;
                                _process.Exited += ProcessExited;
                            }

                            // Try to integrate the window
                            if (InterfaceControl != null)
                            {
                                NativeMethods.SetParent(_handle, InterfaceControl.Handle);
                                Resize(this, new EventArgs());
                            }

                            return true;
                        }
                    }
                    catch
                    {
                        // Ignore errors for individual processes
                    }
                }

                Thread.Sleep(waitInterval);
                elapsedTime += waitInterval;
            }

            return false;
        }

        private void ProcessExited(object sender, EventArgs e)
        {
            Event_Closed(this);
        }

        #endregion

        #region Enumerations

        public enum Defaults
        {
            Port = 0 // AnyDesk doesn't use a traditional port from the client side
        }

        #endregion
    }
}

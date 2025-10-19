using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Messages;
using mRemoteNG.Resources.Language;

namespace mRemoteNG.Connection.Protocol.Terminal
{
    [SupportedOSPlatform("windows")]
    public class ProtocolTerminal(ConnectionInfo connectionInfo) : ProtocolBase
    {
        #region Private Fields

        private IntPtr _handle;
        private readonly ConnectionInfo _connectionInfo = connectionInfo;
        private ConsoleControl.ConsoleControl _consoleControl;

        #endregion

        #region Public Methods

        public override bool Connect()
        {
            try
            {
                Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg, "Attempting to start Windows Terminal session.", true);

                _consoleControl = new ConsoleControl.ConsoleControl
                {
                    Dock = DockStyle.Fill,
                    BackColor = ColorTranslator.FromHtml("#012456"),
                    ForeColor = Color.White,
                    IsInputEnabled = true,
                    Padding = new Padding(0, 20, 0, 0)
                };

                // Path to Windows Terminal executable
                string terminalExe = @"%LocalAppData%\Microsoft\WindowsApps\wt.exe";
                
                // Expand environment variables
                terminalExe = Environment.ExpandEnvironmentVariables(terminalExe);

                // Setup arguments based on whether hostname is provided
                string arguments = "";
                string hostname = _connectionInfo.Hostname.Trim().ToLower();
                bool useLocalHost = hostname == "" || hostname.Equals("localhost");
                
                if (!useLocalHost)
                {
                    // If hostname is provided, try to connect via SSH
                    string username = _connectionInfo.Username;
                    if (!string.IsNullOrEmpty(_connectionInfo.Domain))
                    {
                        username = $"{_connectionInfo.Domain}\\{username}";
                    }
                    
                    if (!string.IsNullOrEmpty(username))
                    {
                        arguments = $"ssh {username}@{_connectionInfo.Hostname}";
                    }
                    else
                    {
                        arguments = $"ssh {_connectionInfo.Hostname}";
                    }
                }
                // If no hostname or localhost, just open a local terminal session

                _consoleControl.StartProcess(terminalExe, arguments);

                while (!_consoleControl.IsHandleCreated) break;
                _handle = _consoleControl.Handle;
                NativeMethods.SetParent(_handle, InterfaceControl.Handle);

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
                NativeMethods.SetForegroundWindow(_handle);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage(Language.IntAppFocusFailed, ex);
            }
        }

        protected override void Resize(object sender, EventArgs e)
        {
            try
            {
                if (InterfaceControl.Size == Size.Empty) return;
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
                Runtime.MessageCollector.AddExceptionMessage(Language.IntAppResizeFailed, ex);
            }
        }

        #endregion

        #region Enumerations

        public enum Defaults
        {
            Port = 22
        }

        #endregion
    }
}

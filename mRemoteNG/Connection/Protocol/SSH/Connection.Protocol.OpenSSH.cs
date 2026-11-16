using System;
using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Messages;
using mRemoteNG.Resources.Language;

namespace mRemoteNG.Connection.Protocol.SSH
{
    [SupportedOSPlatform("windows")]
    public class ProtocolOpenSSH : ExternalProcessProtocolBase
    {
        #region Private Fields

        private readonly ConnectionInfo _connectionInfo;
        private ConsoleControl.ConsoleControl? _consoleControl;

        #endregion

        #region Public Methods

        public ProtocolOpenSSH(ConnectionInfo connectionInfo)
        {
            _connectionInfo = connectionInfo;
        }

        public override bool Connect()
        {
            try
            {
                string? sshExe = FindSshExe();
                if (sshExe == null)
                {
                    Runtime.MessageCollector?.AddMessage(MessageClass.ErrorMsg,
                        "Windows OpenSSH client (ssh.exe) was not found. " +
                        "Please install the OpenSSH Client optional feature via Settings > Apps > Optional Features.", true);
                    return false;
                }

                Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                    $"Attempting to start OpenSSH session using {sshExe}.", true);

                _consoleControl = new ConsoleControl.ConsoleControl
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.Black,
                    ForeColor = Color.White,
                    IsInputEnabled = true,
                    Padding = new Padding(0, 20, 0, 0)
                };

                string arguments = BuildSshArguments();
                _consoleControl.StartProcess(sshExe, arguments);

                // Wait for the console control to create its handle
                int maxWaitMs = 5000;
                long startTicks = Environment.TickCount64;
                while (!_consoleControl.IsHandleCreated &&
                       Environment.TickCount64 < startTicks + maxWaitMs)
                {
                    System.Threading.Thread.Sleep(50);
                }

                if (!_consoleControl.IsHandleCreated)
                {
                    throw new TimeoutException("Failed to initialize OpenSSH console within 5 seconds.");
                }

                _handle = _consoleControl.Handle;
                NativeMethods.SetParent(_handle, InterfaceControl.Handle);

                Resize(this, EventArgs.Empty);
                base.Connect();
                return true;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector?.AddExceptionMessage(Language.ConnectionFailed, ex);
                return false;
            }
        }

        #endregion

        #region Private Methods

        private static string? FindDefaultSshKey()
        {
            string sshDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh");
            if (!Directory.Exists(sshDir))
                return null;

            // Prefer modern key types (most secure first)
            string[] defaultKeyNames = ["id_ed25519", "id_ecdsa", "id_rsa", "id_dsa"];
            foreach (string keyName in defaultKeyNames)
            {
                string candidate = Path.Combine(sshDir, keyName);
                if (File.Exists(candidate))
                    return candidate;
            }
            return null;
        }

        private static string? FindSshExe()
        {
            // Try the standard Windows OpenSSH location first
            string systemSsh = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "OpenSSH", "ssh.exe");

            if (File.Exists(systemSsh))
                return systemSsh;

            // Fallback: try to find ssh.exe on PATH
            string? pathVar = Environment.GetEnvironmentVariable("PATH");
            if (pathVar != null)
            {
                foreach (string dir in pathVar.Split(Path.PathSeparator))
                {
                    string candidate = Path.Combine(dir.Trim(), "ssh.exe");
                    if (File.Exists(candidate))
                        return candidate;
                }
            }

            return null;
        }

        private string BuildSshArguments() =>
            BuildSshArguments(_connectionInfo.Hostname, _connectionInfo.Username, _connectionInfo.Port,
                              _connectionInfo.SSHOptions, _connectionInfo.PrivateKeyPath);

        /// <summary>
        /// ssh.exe is launched directly, so nothing here can run a shell command; what a
        /// connections file could still do is make a field arrive as an extra argument or as an
        /// option (a hostname of "-oProxyCommand=..." runs a command through ssh itself). Hostname,
        /// username and the key path are therefore validated and refused, never sanitised - the
        /// same rules as the Terminal protocol, in ConsoleArgument.
        ///
        /// SSHOptions is passed through as it always has been: that field exists to carry extra
        /// ssh options and is the user's own. A shared connections file can set it too, which is a
        /// property of the feature rather than of this method, and is recorded as such.
        /// </summary>
        private static string BuildSshArguments(string? rawHostname, string? rawUsername, int port,
                                                string? rawSshOptions, string? rawKeyPath)
        {
            string hostname = (rawHostname ?? string.Empty).Trim();
            string username = (rawUsername ?? string.Empty).Trim();

            if (!ConsoleArgument.IsHost(hostname))
                throw ConsoleArgument.Refuse("OpenSSH", "hostname");

            if (username.Length > 0 && !ConsoleArgument.IsSingleToken(username))
                throw ConsoleArgument.Refuse("OpenSSH", "username");

            string args = "";

            // Add port if not default
            if (port > 0 && port != 22)
            {
                args += $"-p {port} ";
            }

            // Add SSH options (extra flags like -o StrictHostKeyChecking=no)
            string sshOptions = rawSshOptions?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(sshOptions))
            {
                args += $"{sshOptions} ";
            }

            // Add private key if specified; otherwise try auto-discovery of default keys
            string keyPath = rawKeyPath?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(keyPath))
            {
                // Placed inside quotes, so the one character that cannot be in it is the quote.
                if (!ConsoleArgument.IsQuotablePath(keyPath))
                    throw ConsoleArgument.Refuse("OpenSSH", "private key path");

                // Convert PuTTY .ppk to OpenSSH format hint — user should use OpenSSH-format keys
                args += $"-i \"{keyPath}\" ";
            }
            else
            {
                // Auto-discover standard SSH key files from ~/.ssh/ when no explicit key is configured
                string? discoveredKey = FindDefaultSshKey();
                if (discoveredKey != null)
                {
                    Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                        $"No private key configured; auto-discovered SSH key: {discoveredKey}", true);
                    args += $"-i \"{discoveredKey}\" ";
                }
            }

            // Build user@host or just host
            if (!string.IsNullOrEmpty(username))
            {
                args += $"{username}@{hostname}";
            }
            else
            {
                args += hostname;
            }

            return args.Trim();
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

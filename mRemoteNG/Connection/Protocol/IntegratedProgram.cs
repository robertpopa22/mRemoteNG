using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using mRemoteNG.App;
using mRemoteNG.Messages;
using mRemoteNG.Properties;
using mRemoteNG.Tools;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;

namespace mRemoteNG.Connection.Protocol
{
    [SupportedOSPlatform("windows")]
    public class IntegratedProgram : ExternalProcessProtocolBase
    {
        #region Private Fields

        private ExternalTool? _externalTool;

        #endregion

        #region Public Methods

        public override bool Initialize()
        {
            if (InterfaceControl.Info == null)
                return base.Initialize();

            _externalTool = Runtime.ExternalToolsService.GetExtAppByName(InterfaceControl.Info.ExtApp);
            _externalTool ??= CreateBuiltInShellPresetForIntegration(InterfaceControl.Info.ExtApp);

            if (_externalTool == null)
            {
                Runtime.MessageCollector?.AddMessage(MessageClass.ErrorMsg,
                                                     string.Format(CultureInfo.InvariantCulture, Language.CouldNotFindExternalTool,
                                                                   InterfaceControl.Info.ExtApp));
                return false;
            }

            _externalTool.ConnectionInfo = InterfaceControl.Info;

            return base.Initialize();
        }

        public override bool Connect()
        {
            try
            {
                if (_externalTool == null)
                    return false;

                Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                                                     $"Attempting to start: {_externalTool.DisplayName}", true);

                if (_externalTool.TryIntegrate == false)
                {
                    _externalTool.Start(InterfaceControl.Info);
                    /* Don't call close here... There's nothing for the override to do in this case since
                     * _process is not created in this scenario. When returning false, ProtocolBase.Close()
                     * will be called - which is just going to call IntegratedProgram.Close() again anyway...
                     * Close();
                     */
                    Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                                                         $"Assuming no other errors/exceptions occurred immediately before this message regarding {_externalTool.DisplayName}, the next \"closed by user\" message can be ignored",
                                                         true);
                    return false;
                }

                ExternalToolArgumentParser argParser = new(_externalTool.ConnectionInfo);
                string parsedFileName = argParser.ParseArguments(_externalTool.FileName);
                string parsedArguments = argParser.ParseArguments(_externalTool.Arguments);

                // Validate the executable path to prevent command injection
                PathValidator.ValidateExecutablePathOrThrow(parsedFileName, nameof(_externalTool.FileName));

                _process = new Process
                {
                    StartInfo =
                    {
                        // Use UseShellExecute = false for better security
                        // Only use true if we need runas for elevation (which IntegratedProgram doesn't use)
                        UseShellExecute = false,
                        FileName = parsedFileName,
                        Arguments = parsedArguments
                    },
                    EnableRaisingEvents = true
                };


                _process.Exited += ProcessExited;

                _process.Start();

                // WaitForInputIdle throws InvalidOperationException for console-based processes
                // (cmd.exe, powershell.exe, etc.) that have no message loop.
                try
                {
                    _process.WaitForInputIdle(Properties.OptionsAdvancedPage.Default.MaxPuttyWaitTime * 1000);
                }
                catch (InvalidOperationException)
                {
                    // Expected for console apps — continue to handle discovery
                }

                int timeoutMs = Properties.OptionsAdvancedPage.Default.MaxPuttyWaitTime * 1000;
                int processId = _process.Id;

                // Strategy 1: Poll Process.MainWindowHandle (works for direct GUI apps like PuTTY, Notepad++)
                _handle = PollMainWindowHandle(_process, timeoutMs);

                // Strategy 2: EnumWindows to find any visible top-level window owned by the process ID.
                // This catches windows that .NET's MainWindowHandle heuristic misses.
                if (_handle == IntPtr.Zero)
                {
                    _handle = FindWindowByProcessId(processId, timeoutMs);
                }

                // Strategy 3: Check child processes. Launcher apps (e.g. git-bash.exe) spawn a child
                // process and exit — the actual window belongs to the child.
                if (_handle == IntPtr.Zero)
                {
                    _handle = FindWindowInChildProcesses(processId, timeoutMs);
                }

                // Strategy 4: Known shell launchers can spawn deeper process trees
                // (for example wsl.exe -> wslhost.exe -> conhost.exe).
                if (_handle == IntPtr.Zero && IsCommonShellTool(_externalTool, parsedFileName))
                {
                    _handle = FindWindowInDescendantProcesses(processId, timeoutMs, maxDepth: 4);
                }

                if (_handle == IntPtr.Zero)
                {
                    Runtime.MessageCollector?.AddMessage(MessageClass.WarningMsg,
                        BuildNoEmbeddableWindowMessage(_externalTool.DisplayName, processId,
                            IsCommonShellTool(_externalTool, parsedFileName)));

                    // Nothing to embed. Leave the tool running in its own window. Clearing the
                    // process first keeps Close from killing it when this returns false.
                    _process.Exited -= ProcessExited;
                    _process.Dispose();
                    _process = null;
                    return false;
                }

                {
                    _ = NativeMethods.GetWindowThreadProcessId(_handle, out uint windowPid);
                    if (windowPid != (uint)_process.Id)
                    {
                        try
                        {
                            Process windowProcess = Process.GetProcessById((int)windowPid);

                            _process.Exited -= ProcessExited;
                            _process = windowProcess;
                            _process.EnableRaisingEvents = true;
                            _process.Exited += ProcessExited;

                            Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                                $"IntegratedProgram: Tracking process changed from PID {processId} to PID {windowPid}", true);
                        }
                        catch (Exception ex)
                        {
                            Runtime.MessageCollector?.AddExceptionMessage("IntegratedProgram: Failed to attach to window owner process.", ex);
                        }
                    }
                }

                NativeMethods.SetParent(_handle, InterfaceControl.Handle);

                // Give keyboard focus to the embedded window after re-parenting.
                // Required for Java-based apps (e.g. TigerVNC) where the Java AWT focus model
                // does not automatically acquire Win32 keyboard focus after SetParent (#1442).
                if (_handle != IntPtr.Zero)
                    NativeMethods.SetFocus(_handle);

                Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg, Language.IntAppStuff, true);
                Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                                                     string.Format(CultureInfo.InvariantCulture, Language.IntAppHandle, _handle), true);
                Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                                                     string.Format(CultureInfo.InvariantCulture, Language.IntAppTitle, _process.MainWindowTitle),
                                                     true);
                Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                                                     string.Format(CultureInfo.InvariantCulture, Language.PanelHandle,
                                                                   InterfaceControl.Parent?.Handle), true);

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

        public override void Focus()
        {
            try
            {
                NativeMethods.SetForegroundWindow(_handle);
                // SetFocus is required for Java-based embedded apps (e.g. TigerVNC) to receive
                // keyboard input — SetForegroundWindow alone is insufficient for Java AWT windows (#1442).
                if (_handle != IntPtr.Zero)
                    NativeMethods.SetFocus(_handle);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage(Language.IntAppFocusFailed, ex);
            }
        }

        #endregion

        #region Private Methods

        private static ExternalTool? CreateBuiltInShellPresetForIntegration(string extAppName)
        {
            string normalized = NormalizeShellToolName(extAppName);
            switch (normalized)
            {
                case "cmd":
                case "command prompt":
                    return CreateBuiltInShellTool("cmd.exe", "%ComSpec%");
                case "pwsh":
                    return CreateBuiltInShellTool("pwsh.exe", "pwsh.exe");
                case "powershell":
                case "windows powershell":
                    return CreateBuiltInShellTool("powershell.exe", "powershell.exe");
                case "wsl":
                case "bash":
                    return CreateBuiltInShellTool("wsl.exe", @"%windir%\system32\wsl.exe");
                case "ubuntu":
                case "debian":
                case "kali-linux":
                case "kali":
                    // WSL distribution launchers installed via Windows Store
                    return CreateBuiltInShellTool(normalized + ".exe", normalized + ".exe");
                default:
                    // Handle versioned Ubuntu launchers (e.g. ubuntu2004, ubuntu2204, ubuntu2404)
                    if (normalized.Length > 6 && normalized.StartsWith("ubuntu", StringComparison.Ordinal) && char.IsDigit(normalized[6]))
                        return CreateBuiltInShellTool(normalized + ".exe", normalized + ".exe");
                    return null;
            }
        }

        private static ExternalTool CreateBuiltInShellTool(string displayName, string fileName)
        {
            return new ExternalTool(displayName, fileName)
            {
                TryIntegrate = true,
                ShowOnToolbar = false
            };
        }

        internal static string BuildNoEmbeddableWindowMessage(string displayName, int processId, bool isConsoleTool)
        {
            string message =
                $"'{displayName}' (PID {processId}) started, but it has no window that can be docked, " +
                "so it is running in its own window instead of in a panel.";

            if (isConsoleTool)
            {
                message +=
                    " Console tools hand their window to the Windows default terminal application. When that is " +
                    "Windows Terminal, the console lives in a separate process that cannot be docked. To dock this tool, " +
                    "set the default terminal application to Windows Console Host.";
            }

            return message;
        }

        private static bool IsCommonShellTool(ExternalTool externalTool, string parsedFileName)
        {
            return IsShellToolIdentifier(NormalizeShellToolName(externalTool.DisplayName))
                || IsShellToolIdentifier(NormalizeShellToolName(externalTool.FileName))
                || IsShellToolIdentifier(NormalizeShellToolName(parsedFileName));
        }

        private static bool IsShellToolIdentifier(string identifier)
        {
            return string.Equals(identifier, "cmd", StringComparison.Ordinal)
                || string.Equals(identifier, "pwsh", StringComparison.Ordinal)
                || string.Equals(identifier, "powershell", StringComparison.Ordinal)
                || string.Equals(identifier, "windows powershell", StringComparison.Ordinal)
                || string.Equals(identifier, "wsl", StringComparison.Ordinal)
                || string.Equals(identifier, "bash", StringComparison.Ordinal)
                || string.Equals(identifier, "ubuntu", StringComparison.Ordinal)
                || string.Equals(identifier, "debian", StringComparison.Ordinal)
                || string.Equals(identifier, "kali-linux", StringComparison.Ordinal)
                || string.Equals(identifier, "kali", StringComparison.Ordinal)
                // Versioned Ubuntu launchers (e.g. ubuntu2004, ubuntu2204, ubuntu2404)
                || (identifier.Length > 6 && identifier.StartsWith("ubuntu", StringComparison.Ordinal) && char.IsDigit(identifier[6]));
        }

        private static string NormalizeShellToolName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string normalized = value.Trim().Trim('"');
            if (normalized.Contains('\\') || normalized.Contains('/'))
            {
                normalized = Path.GetFileName(normalized);
            }

            normalized = normalized.ToLowerInvariant();
            if (normalized.EndsWith(".exe", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(0, normalized.Length - 4);
            }

            return normalized;
        }

        #endregion

        #region Enumerations

        public enum Defaults
        {
            Port = 0
        }

        #endregion
    }
}
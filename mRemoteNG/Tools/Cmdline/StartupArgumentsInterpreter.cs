using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.App.Info;
using mRemoteNG.Messages;
using mRemoteNG.Properties;
using mRemoteNG.Resources.Language;


namespace mRemoteNG.Tools.Cmdline
{
    [SupportedOSPlatform("windows")]
    public class StartupArgumentsInterpreter
    {
        private readonly MessageCollector _messageCollector;

        /// <summary>
        /// Connection name or ID specified via --connect command-line argument.
        /// Used to auto-open a connection on startup (e.g. for desktop shortcuts).
        /// </summary>
        public static string? ConnectTo { get; private set; }

        public StartupArgumentsInterpreter(MessageCollector messageCollector)
        {
            if (messageCollector == null)
                throw new ArgumentNullException(nameof(messageCollector));

            _messageCollector = messageCollector;
        }

        public void ParseArguments(IEnumerable<string> cmdlineArgs)
        {
            //if (!cmdlineArgs.Any()) return;
            _messageCollector.AddMessage(MessageClass.DebugMsg, "Parsing cmdline arguments");

            try
            {
                CmdArgumentsInterpreter args = new(cmdlineArgs);

                ParseResetPositionArg(args);
                ParseResetPanelsArg(args);
                ParseResetToolbarArg(args);
                ParseNoReconnectArg(args);
                ParseCustomConnectionPathArg(args);
                ParseConnectArg(args);
            }
            catch (Exception ex)
            {
                _messageCollector.AddExceptionMessage(Language.CommandLineArgsCouldNotBeParsed, ex, logOnly: false);
            }
        }

        private void ParseResetPositionArg(CmdArgumentsInterpreter args)
        {
            if (args["resetpos"] == null && args["rp"] == null && args["reset"] == null) return;
            _messageCollector.AddMessage(MessageClass.DebugMsg, "Cmdline arg: Resetting window positions.");
            Properties.App.Default.MainFormKiosk = false;
            int newWidth = 900;
            int newHeight = 600;
            var workingArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, newWidth, newHeight);
            int newX = workingArea.Width / 2 - newWidth / 2;
            int newY = workingArea.Height / 2 - newHeight / 2;
            Properties.App.Default.MainFormLocation = new Point(newX, newY);
            Properties.App.Default.MainFormSize = new Size(newWidth, newHeight);
            Properties.App.Default.MainFormState = FormWindowState.Normal;
        }

        private void ParseResetPanelsArg(CmdArgumentsInterpreter args)
        {
            if (args["resetpanels"] == null && args["rpnl"] == null && args["reset"] == null) return;
            _messageCollector.AddMessage(MessageClass.DebugMsg, "Cmdline arg: Resetting panels");
            Properties.App.Default.ResetPanels = true;
        }

        private void ParseResetToolbarArg(CmdArgumentsInterpreter args)
        {
            if (args["resettoolbar"] == null && args["rtbr"] == null && args["reset"] == null) return;
            _messageCollector.AddMessage(MessageClass.DebugMsg, "Cmdline arg: Resetting toolbar position");
            Properties.App.Default.ResetToolbars = true;
        }

        private void ParseNoReconnectArg(CmdArgumentsInterpreter args)
        {
            if (args["noreconnect"] == null && args["norc"] == null) return;
            _messageCollector.AddMessage(MessageClass.DebugMsg,
                                         "Cmdline arg: Disabling reconnection to previously connected hosts");
            Properties.OptionsAdvancedPage.Default.NoReconnect = true;
        }

        private void ParseCustomConnectionPathArg(CmdArgumentsInterpreter args)
        {
            string consParam = "";
            if (args["cons"] != null)
                consParam = "cons";
            if (args["c"] != null)
                consParam = "c";

            if (string.IsNullOrEmpty(consParam)) return;
            _messageCollector.AddMessage(MessageClass.DebugMsg, "Cmdline arg: loading connections from a custom path");
            string? consValue = args[consParam];
            if (consValue == null) return;

            if (File.Exists(consValue) == false)
            {
                if (File.Exists(Path.Combine(GeneralAppInfo.HomePath, consValue)))
                {
                    Properties.OptionsBackupPage.Default.LoadConsFromCustomLocation = true;
                    Properties.OptionsBackupPage.Default.BackupLocation = Path.Combine(GeneralAppInfo.HomePath, consValue);
                    return;
                }

                if (!File.Exists(Path.Combine(ConnectionsFileInfo.DefaultConnectionsPath, consValue))) return;
                Properties.OptionsBackupPage.Default.LoadConsFromCustomLocation = true;
                Properties.OptionsBackupPage.Default.BackupLocation = Path.Combine(ConnectionsFileInfo.DefaultConnectionsPath, consValue);
            }
            else
            {
                Properties.OptionsBackupPage.Default.LoadConsFromCustomLocation = true;
                Properties.OptionsBackupPage.Default.BackupLocation = consValue;
            }
        }

        private void ParseConnectArg(CmdArgumentsInterpreter args)
        {
            string? connectValue = args["connect"];
            if (string.IsNullOrEmpty(connectValue))
                return;

            _messageCollector.AddMessage(MessageClass.DebugMsg, $"Cmdline arg: auto-connect to \"{connectValue}\"");
            ConnectTo = connectValue;
        }
    }
}
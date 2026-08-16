using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using mRemoteNG.App.Info;
using mRemoteNG.App.Initialization;
using mRemoteNG.App.Update;
using mRemoteNG.Config.Connections.Multiuser;
using mRemoteNG.Config.Settings.Registry;
using mRemoteNG.Connection;
using mRemoteNG.Messages;
using mRemoteNG.Properties;
using mRemoteNG.Tools;
using mRemoteNG.Tools.Cmdline;
using mRemoteNG.UI;
using mRemoteNG.UI.Forms;


using mRemoteNG.Config.DatabaseConnectors; // Added for DatabaseProfileManager

namespace mRemoteNG.App
{
    [SupportedOSPlatform("windows")]
    public class Startup
    {
        private RegistryLoader _RegistryLoader;
        private AppUpdater _appUpdate;
        private readonly ConnectionIconLoader _connectionIconLoader;
        public static Startup Instance { get; } = new Startup();

        public string[]? CommandLineArgs { get; set; }

        private Startup()
        {
            _RegistryLoader = RegistryLoader.Instance; //created instance
            _appUpdate = new AppUpdater(); 
            _connectionIconLoader = new ConnectionIconLoader(GeneralAppInfo.HomePath + "\\Icons\\");
        }

        public void InitializeProgram(MessageCollector messageCollector)
        {
            Debug.Print("---------------------------" + Environment.NewLine + "[START] - " + Convert.ToString(DateTime.Now, CultureInfo.InvariantCulture));
            var sw = Stopwatch.StartNew();

            StartupDataLogger startupLogger = new(messageCollector);
            startupLogger.LogStartupData();
            messageCollector.AddMessage(MessageClass.InformationMsg, $"[Startup]   StartupDataLogger: {sw.ElapsedMilliseconds}ms", true);

            CompatibilityChecker.CheckCompatibility(messageCollector);

            // ObjectListView swallows a failure to update a virtual list's row count, which leaves
            // the control reporting a stale count and is the suspected source of the #149 expand
            // crash. It cannot log on its own -- it has no dependency on this application -- so
            // give it somewhere to report. Silent unless a resize actually fails.
            BrightIdeasSoftware.VirtualObjectListView.SizeChangeDiagnostic = report =>
                messageCollector.AddMessage(MessageClass.WarningMsg, $"[#149-diag] {report}", true);

            // Covers the other half of the same guard: a VirtualListSize assignment that succeeded
            // but against an index that belonged to a different list (model vs filtered view) than
            // GetItemCount() is reporting. SizeChangeDiagnostic above cannot see that case at all.
            BrightIdeasSoftware.TreeListView.RedrawGuardDiagnostic = report =>
                messageCollector.AddMessage(MessageClass.WarningMsg, $"[#149-diag] {report}", true);

            ParseCommandLineArgs(messageCollector);

            // IE Browser Emulation registry writes are only needed when a WebBrowser control
            // initializes (much later). Run on background thread to avoid blocking startup.
            sw.Restart();
            Task.Run(() => IeBrowserEmulation.Register());
            _connectionIconLoader.GetConnectionIcons();
            messageCollector.AddMessage(MessageClass.InformationMsg, $"[Startup]   IconLoader: {sw.ElapsedMilliseconds}ms", true);

            DefaultConnectionInfo.Instance.LoadFrom(Settings.Default, a => "ConDefault" + a);
            DefaultConnectionInheritance.LoadFrom(Settings.Default, a => "InhDefault" + a);

            // Plugin loading involves Assembly.Load + reflection per DLL — defer to background
            // since plugins are not needed until user explicitly uses them.
            // PluginManager.RegisterMenu() already handles InvokeRequired for UI marshaling.
            Task.Run(() => PluginManager.Instance.LoadPlugins());
        }

        private void ParseCommandLineArgs(MessageCollector messageCollector)
        {
            StartupArgumentsInterpreter interpreter = new(messageCollector);
            interpreter.ParseArguments(CommandLineArgs ?? Environment.GetCommandLineArgs());
        }

        public static void CreateConnectionsProvider(MessageCollector messageCollector)
        {
            messageCollector.AddMessage(MessageClass.DebugMsg, "Determining if we need a connections syncronizer");

            if (Properties.OptionsDBsPage.Default.UseSQLServer)
            {
                // Check if profile picker should be shown
                if (Properties.OptionsDBsPage.Default.ShowDatabasePickerOnStartup)
                {
                    using (var picker = new FrmDatabasePicker())
                    {
                        if (picker.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            if (picker.SelectedProfile != null)
                            {
                                DatabaseProfileManager.ApplyProfileToSettings(picker.SelectedProfile);
                            }
                        }
                        else
                        {
                            // User cancelled, do not enable SQL sync
                            return;
                        }
                    }
                }

                messageCollector.AddMessage(MessageClass.DebugMsg, "Creating database syncronizer");
                Runtime.ConnectionsService.RemoteConnectionsSyncronizer = new RemoteConnectionsSyncronizer(new SqlConnectionsUpdateChecker());
                Runtime.ConnectionsService.RemoteConnectionsSyncronizer.Enable();
            }
            else if (Properties.OptionsConnectionsPage.Default.WatchConnectionFile)
            {
                messageCollector.AddMessage(MessageClass.DebugMsg, "Creating file syncronizer");
                string startupFile = ConnectionsService.GetStartupConnectionFileName();
                if (!string.IsNullOrEmpty(startupFile))
                {
                    Runtime.ConnectionsService.RemoteConnectionsSyncronizer = new RemoteConnectionsSyncronizer(new FileConnectionsUpdateChecker(startupFile));
                    Runtime.ConnectionsService.RemoteConnectionsSyncronizer.Enable();
                }
            }
        }

        public async Task CheckForUpdate()
        {
            if (_appUpdate == null)
            {
                _appUpdate = new AppUpdater();
            }
            else if (_appUpdate.IsGetUpdateInfoRunning)
            {
                return;
            }

            DateTime nextUpdateCheck = Convert.ToDateTime(Properties.OptionsUpdatesPage.Default.CheckForUpdatesLastCheck.Add(TimeSpan.FromDays(Convert.ToDouble(Properties.OptionsUpdatesPage.Default.CheckForUpdatesFrequencyDays))));
            if (!Properties.OptionsUpdatesPage.Default.UpdatePending && DateTime.UtcNow < nextUpdateCheck)
            {
                return;
            }

            try
            {
                await _appUpdate.GetUpdateInfoAsync();
                // Update is available, but don't show the panel automatically at startup
                // User can check for updates manually via Help > Check for Updates menu
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("CheckForUpdate() failed.", ex);
            }
        }
    }
}
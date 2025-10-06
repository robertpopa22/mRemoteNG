using Microsoft.IdentityModel.Tokens;

using mRemoteNG.App.Update;
using mRemoteNG.Config.Settings;
using mRemoteNG.DotNet.Update;
using mRemoteNG.DotNet.Update;
using mRemoteNG.UI.Forms;

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace mRemoteNG.App
{
    [SupportedOSPlatform("windows")]
    public static class ProgramRoot
    {
        private static Mutex? _mutex;
        private static FrmSplashScreenNew _frmSplashScreen = null;
        private static string customResourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages");

        private static System.Threading.Thread? _wpfSplashThread;
        private static FrmSplashScreenNew? _wpfSplash;

        [STAThread]
        public static void Main(string[] args)
        {
            // Ensure the real entry point is definitely STA
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static Task MainAsync(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            string? installedVersion = DotNetRuntimeCheck.GetLatestDotNetRuntimeVersion();

            var checkFail = false;

            // Checking .NET Runtime version
            var (latestRuntimeVersion, downloadUrl) = DotNetRuntimeCheck.GetLatestAvailableDotNetVersionAsync().GetAwaiter().GetResult();
            if (string.IsNullOrEmpty(installedVersion))
            {
                try
                {
                    _ = MessageBox.Show(
                        $".NET Desktop Runtime at least {DotNetRuntimeCheck.RequiredDotnetVersion}.0 is required.\n" +
                        "The application will now exit.\n\nPlease download and install latest desktop runtime:\n" + downloadUrl,
                        "Missing .NET " + DotNetRuntimeCheck.RequiredDotnetVersion + " Runtime",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    if (InternetConnection.IsPosible())
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo(fileName: downloadUrl) { UseShellExecute = true });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Unable to open download link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch { }
                checkFail = true;
            }

            // Checking Visual C++ Redistributable version
            if (VCppRuntimeCheck.GetInstalledVcRedistVersions() == null || VCppRuntimeCheck.GetInstalledVcRedistVersions().Count == 0)
            {
                var downloadUrl2 = "https://aka.ms/vs/17/release/vc_redist.x86.exe";
                try
                {
                    _ = MessageBox.Show(
                        $"A Visual C++ (MSVC) runtime library is required.\n" +
                        "The application will now exit.\n\nPlease download and install latest desktop runtime:\n" + downloadUrl2,
                        "Missing Visual C++ Redistributable x86 Runtime",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    if (InternetConnection.IsPosible())
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo(fileName: downloadUrl2) { UseShellExecute = true });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Unable to open download link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch { }
                checkFail = true;
            }

            if (checkFail)
            {
                Environment.Exit(0);
            }

            Lazy<bool> singleInstanceOption = new(() => Properties.OptionsStartupExitPage.Default.SingleInstance);
            if (singleInstanceOption.Value)
                StartApplicationAsSingleInstance();
            else
                StartApplication();

            return Task.CompletedTask;
        }

        // Assembly resolve handler
        private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            try
            {
                string assemblyName = new AssemblyName(args.Name).Name ?? string.Empty;
                if (assemblyName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                    return null;

                string assemblyFile = assemblyName + ".dll";
                string assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assemblies", assemblyFile);

                if (File.Exists(assemblyPath))
                    return Assembly.LoadFrom(assemblyPath);
            }
            catch
            {
                // Suppress resolution exceptions; return null to continue standard probing
            }
            return null;
        }

        private static void CheckLockalDB()
        {
            LocalDBManager settingsManager = new LocalDBManager(dbPath: "mRemoteNG.appSettings", useEncryption: false, schemaFilePath: "");
        }

        private static void StartApplication()
        {
            CatchAllUnhandledExceptions();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ShowSplashOnStaThread();

            Application.Run(FrmMain.Default);
        }

        public static void CloseSingletonInstanceMutex()
        {
            _mutex?.Close();
        }

        private static void StartApplicationAsSingleInstance()
        {
            const string mutexID = "mRemoteNG_SingleInstanceMutex";
            _mutex = new Mutex(false, mutexID, out bool newInstanceCreated);
            if (!newInstanceCreated)
            {
                SwitchToCurrentInstance();
                return;
            }

            StartApplication();
            GC.KeepAlive(_mutex);
        }

        private static void SwitchToCurrentInstance()
        {
            IntPtr singletonInstanceWindowHandle = GetRunningSingletonInstanceWindowHandle();
            if (singletonInstanceWindowHandle == IntPtr.Zero) return;
            if (NativeMethods.IsIconic(singletonInstanceWindowHandle) != 0)
                _ = NativeMethods.ShowWindow(singletonInstanceWindowHandle, (int)NativeMethods.SW_RESTORE);
            NativeMethods.SetForegroundWindow(singletonInstanceWindowHandle);
        }

        private static IntPtr GetRunningSingletonInstanceWindowHandle()
        {
            IntPtr windowHandle = IntPtr.Zero;
            Process currentProcess = Process.GetCurrentProcess();
            foreach (Process enumeratedProcess in Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if (enumeratedProcess.Id != currentProcess.Id &&
                    enumeratedProcess.MainModule.FileName == currentProcess.MainModule.FileName &&
                    enumeratedProcess.MainWindowHandle != IntPtr.Zero)
                    windowHandle = enumeratedProcess.MainWindowHandle;
            }

            return windowHandle;
        }

        private static void CatchAllUnhandledExceptions()
        {
            Application.ThreadException += ApplicationOnThreadException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        }

        private static void ApplicationOnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            CloseSplash();
            if (FrmMain.Default.IsDisposed) return;
            FrmUnhandledException window = new(e.Exception, false);
            window.ShowDialog(FrmMain.Default);
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            FrmUnhandledException window = new(e.ExceptionObject as Exception, e.IsTerminating);
            window.ShowDialog(FrmMain.Default);
        }

        private static void ShowSplashOnStaThread()
        {
            _wpfSplashThread = new System.Threading.Thread(() =>
            {
                _wpfSplash = FrmSplashScreenNew.GetInstance();

                // Center the splash screen on the primary screen before showing it
                _wpfSplash.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

                _wpfSplash.ShowInTaskbar = false;
                _wpfSplash.Show();
                System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(_wpfSplash);
                System.Windows.Threading.Dispatcher.Run(); // WPF message loop
            })
            { IsBackground = true };
            _wpfSplashThread.SetApartmentState(System.Threading.ApartmentState.STA);
            _wpfSplashThread.Start();
        }

        private static void CloseSplash()
        {
            if (_wpfSplash != null)
            {
                _wpfSplash.Dispatcher.Invoke(() => _wpfSplash.Close());
                _wpfSplash = null;
            }
            if (_wpfSplashThread != null)
            {
                _wpfSplashThread.Join();
                _wpfSplashThread = null;
            }
        }
    }
}
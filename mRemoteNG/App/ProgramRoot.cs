using mRemoteNG.Config.Settings;
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
using System.Windows.Forms;
using System.Threading.Tasks;
using mRemoteNG.DotNet.Update;



namespace mRemoteNG.App
{
    [SupportedOSPlatform("windows")]
    public static class ProgramRoot
    {
        private static Mutex _mutex;
        private static FrmSplashScreenNew _frmSplashScreen = null;
        private static string customResourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages");

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                // FIX: Awaited Task<bool> synchronously to obtain bool result
                bool isInstalled = DotNetRuntimeCheck
                    .IsDotnetRuntimeInstalled(DotNetRuntimeCheck.RequiredDotnetVersion)
                    .GetAwaiter()
                    .GetResult();

                if (!isInstalled)
                {
                    Trace.WriteLine($".NET Desktop Runtime {DotNetRuntimeCheck.RequiredDotnetVersion} is NOT installed.");
                    Trace.WriteLine("Please download and install it from:");
                    Trace.WriteLine("https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-9.0.8-windows-x64-installer");

                    try
                    {
                        MessageBox.Show(
                            $".NET Desktop Runtime {DotNetRuntimeCheck.RequiredDotnetVersion} is required.\n" +
                            "The application will now exit.\n\nDownload:\nhttps://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-9.0.8-windows-x64-installer",
                            "Missing .NET Runtime",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    } catch {
                        // Ignore UI issues
                    }

                    Environment.Exit(1);
                    return;
                }

                Trace.WriteLine($".NET Desktop Runtime {DotNetRuntimeCheck.RequiredDotnetVersion} is installed.");
            } catch (Exception ex) {
                Trace.WriteLine("Runtime check failed: " + ex);
                Environment.Exit(1);
                return;
            }

            Trace.WriteLine("!!!!!!=============== TEST ==================!!!!!!!!!!!!!");
            try
            {
                string assemblyFile = "System.Configuration.ConfigurationManager.dll";
                string assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assemblies", assemblyFile);

                if (File.Exists(assemblyPath))
                {
                    Assembly.LoadFrom(assemblyPath);
                }
            }
            catch (FileNotFoundException ex)
            {
                Trace.WriteLine("Error occured: " + ex.Message);
            }

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string runtimeVersion = RuntimeInformation.FrameworkDescription;
                if (runtimeVersion.Contains(".NET 9.0.2"))
                {
                    Console.WriteLine(".NET Desktop Runtime 9.0.2 is already installed.");
                }
                else
                {
                    Console.WriteLine(".NET Desktop Runtime 9.0.2 is not installed. Please download and install it from the following link:");
                    Console.WriteLine("https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-9.0.2-windows-x64-installer");
                    Console.WriteLine("After installation, please restart the application.");
                }
            }
            else
            {
                Console.WriteLine("This application requires the .NET Desktop Runtime 9.0.2 on Windows.");
            }

            CheckLockalDB();

            Lazy<bool> singleInstanceOption = new(() => Properties.OptionsStartupExitPage.Default.SingleInstance);

            if (singleInstanceOption.Value)
            {
                StartApplicationAsSingleInstance();
            }
            else
            {
                StartApplication();
            }
        }

        private static void CheckLockalDB()
        {
            LocalDBManager settingsManager = new LocalDBManager(dbPath: "mRemoteNG.appSettings", useEncryption: false, schemaFilePath: "");
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs resolveArgs)
        {
            string assemblyName = new AssemblyName(resolveArgs.Name).Name.Replace(".resources", string.Empty);
            string assemblyFile = assemblyName + ".dll";
            string assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assemblies", assemblyFile);

            if (File.Exists(assemblyPath))
            {
                return Assembly.LoadFrom(assemblyPath);
            }
            return null;
        }

        private static void StartApplication()
        {
            CatchAllUnhandledExceptions();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _frmSplashScreen = FrmSplashScreenNew.GetInstance();

            Screen targetScreen = Screen.PrimaryScreen;

            Rectangle viewport = targetScreen.WorkingArea;
            _frmSplashScreen.Top = viewport.Top;
            _frmSplashScreen.Left = viewport.Left;
            _frmSplashScreen.Left = viewport.Left + (targetScreen.Bounds.Size.Width - _frmSplashScreen.Width) / 2;
            _frmSplashScreen.Top = viewport.Top + (targetScreen.Bounds.Size.Height - _frmSplashScreen.Height) / 2;
            _frmSplashScreen.ShowInTaskbar = false;
            _frmSplashScreen.Show();

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
            FrmSplashScreenNew.GetInstance().Close();

            if (FrmMain.Default.IsDisposed) return;

            FrmUnhandledException window = new(e.Exception, false);
            window.ShowDialog(FrmMain.Default);
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            FrmUnhandledException window = new(e.ExceptionObject as Exception, e.IsTerminating);
            window.ShowDialog(FrmMain.Default);
        }
    }
}
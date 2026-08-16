using System;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using mRemoteNGSpecs.Drivers;
using mRemoteNGSpecs.Support;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// Base for the acceptance battery: every test gets a freshly deployed application with an
    /// empty portable Settings folder, and every failure leaves behind enough evidence to diagnose
    /// it without re-running.
    ///
    /// A fresh process per test is deliberate. The unit suite already proves that FrmOptions and
    /// ObjectListView leak native handles badly enough to crash a second test in the same process,
    /// which is why two of its fixtures must run isolated. Launching the real executable each time
    /// sidesteps that entirely, at roughly five seconds per test.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public abstract class UiAcceptanceTestBase
    {
        protected AppDriver Driver { get; private set; } = null!;
        protected Window MainWindow { get; private set; } = null!;
        protected IsolatedDeployment Deployment { get; private set; } = null!;

        [OneTimeSetUp]
        public void SweepOldScenarios()
        {
            IsolatedDeployment.SweepStaleScenarios(TimeSpan.FromHours(4));
        }

        [SetUp]
        public void StartApplication()
        {
            Deployment = IsolatedDeployment.Create(TestContext.CurrentContext.Test.Name);
            SeedSettings();

            Driver = new AppDriver(Deployment.ExecutablePath);
            MainWindow = Driver.Start(TimeSpan.FromSeconds(60));
        }

        /// <summary>Override to seed connections or settings before the app starts.</summary>
        protected virtual void SeedSettings()
        {
        }

        [TearDown]
        public void StopApplication()
        {
            bool failed = TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed;

            if (failed)
                CaptureDiagnostics();

            Driver?.Dispose();

            if (!failed)
                Deployment?.Dispose();
            else
                TestContext.Out.WriteLine($"Scenario directory kept for inspection: {Deployment?.Directory}");
        }

        /// <summary>
        /// Closes the application the way a user's keyboard does, and reports whether it exited.
        ///
        /// Not MainWindow.Close(): FlaUI's WindowPattern close throws ElementNotEnabledException on
        /// this window, and it does so identically with and without a session open — measured, so
        /// it is a limitation of asking through UIA rather than the application refusing. Alt+F4
        /// exits reliably in both states, so that is what the battery uses. Filing the pattern
        /// failure as a product bug would have been wrong.
        /// </summary>
        protected bool CloseApplicationAndWaitForExit(TimeSpan? timeout = null)
        {
            try
            {
                MainWindow.Focus();
                FlaUI.Core.Input.Keyboard.TypeSimultaneously(
                    FlaUI.Core.WindowsAPI.VirtualKeyShort.ALT,
                    FlaUI.Core.WindowsAPI.VirtualKeyShort.F4);
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"close keystroke failed: {ex.GetType().Name}");
            }

            return Support.UiWait.Happened(() => Driver.Application.HasExited,
                                           timeout ?? TimeSpan.FromSeconds(25));
        }

        /// <summary>
        /// Asserts the application shut down cleanly after a deliberate close.
        ///
        /// Use this instead of AssertNoCrash once the app has been asked to exit: a crash check on
        /// a process that closed on purpose is meaningless, and reports the closed process itself
        /// as the failure. A non-zero exit code is the real signal that teardown went wrong.
        /// </summary>
        protected void AssertExitedCleanly(bool exited, string context)
        {
            Assert.That(exited, Is.True, $"{context}: the application never exited");

            int? code = null;
            try { code = Driver.Application.ExitCode; } catch (Exception) { }

            if (code is not null)
                Assert.That(code, Is.Zero,
                            $"{context}: the application exited with code {code} — teardown threw "
                            + "rather than shutting down cleanly");
        }

        /// <summary>
        /// Fails the test if the application crashed. Half the issues this battery covers are
        /// crashes, and an unhandled exception leaves the process alive behind a dialog — so
        /// "nothing threw in the test" proves nothing on its own.
        /// </summary>
        protected void AssertNoCrash(string context)
        {
            CrashWatcher.CrashResult crash = CrashWatcher.Check(Driver);
            Assert.That(crash.Occurred, Is.False, $"{context}: {crash.Description}");
        }

        private void CaptureDiagnostics()
        {
            try
            {
                string dir = Path.Combine(Deployment.Directory, "_failure");
                Directory.CreateDirectory(dir);

                // Whole desktop, not just the app window: the thing that broke the test is often a
                // dialog outside the main window's bounds.
                string shot = Path.Combine(dir, "desktop.png");
                Capture.Screen().ToFile(shot);
                TestContext.AddTestAttachment(shot, "desktop at failure");

                string tree = Path.Combine(dir, "uia-tree.txt");
                File.WriteAllText(tree, UiaTreeDumper.DumpDesktop(Driver.Automation));
                TestContext.AddTestAttachment(tree, "UIA tree at failure");

                string? log = Deployment.ReadAppLog();
                if (log is not null)
                {
                    string logPath = Path.Combine(dir, "mRemoteNG.log");
                    File.WriteAllText(logPath, log);
                    TestContext.AddTestAttachment(logPath, "application log");
                }

                File.WriteAllText(Path.Combine(dir, "issues.txt"), string.Join(", ", IssuesForCurrentTest()));
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"diagnostic capture failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private string[] IssuesForCurrentTest()
        {
            try
            {
                string? methodName = TestContext.CurrentContext.Test.MethodName;
                MethodInfo? method = GetType().GetMethod(methodName ?? "");
                string[] issues = method?.GetCustomAttribute<IssuesAttribute>()?.Ids ?? [];
                string[] stress = method?.GetCustomAttribute<StressCoverageAttribute>()?.Ids ?? [];
                return [.. issues, .. stress];
            }
            catch (Exception)
            {
                return [];
            }
        }
    }
}

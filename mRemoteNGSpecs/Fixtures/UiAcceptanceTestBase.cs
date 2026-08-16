using System;
using System.IO;
using System.Linq;
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

            DismissFirstRunPrompt();
        }

        /// <summary>
        /// Answers the first-run "check for updates?" task dialog.
        ///
        /// Every scenario starts from an empty Settings folder, which is the point of the isolation
        /// — but it also means CheckForUpdatesAsked is never set, so frmMain.PromptForUpdatesPreference
        /// shows a modal task dialog on every single start. It holds the keyboard, so clicks and
        /// keystrokes aimed at the main window land in the dialog instead. That silently invalidated
        /// interaction in every test here until it was tracked down: a focus assertion reported the
        /// dialog's caption as the thief and looked exactly like a product focus bug.
        /// </summary>
        private void DismissFirstRunPrompt()
        {
            // Matched on a trimmed prefix: the button's accessible name carries a trailing space,
            // so an exact-name condition silently misses it — which is how this dialog survived a
            // first attempt at dismissing it.
            AutomationElement? prompt = null;
            Support.UiWait.Happened(() =>
            {
                try
                {
                    prompt = Driver.Automation.GetDesktop()
                        .FindAllDescendants()
                        .FirstOrDefault(e =>
                        {
                            try
                            {
                                return e.Properties.ProcessId.ValueOrDefault == Driver.Application.ProcessId
                                       && (e.Name ?? "").Trim().StartsWith("Use the recommended settings",
                                                                           StringComparison.Ordinal);
                            }
                            catch (Exception) { return false; }
                        });
                    return prompt is not null;
                }
                catch (Exception) { return false; }
            }, TimeSpan.FromSeconds(8));

            if (prompt is null)
                return;

            prompt.Click();
            Support.UiWait.Settle(MainWindow);
            TestContext.Out.WriteLine("dismissed the first-run updates prompt");
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
        ///
        /// Closing with sessions open raises the application's own confirmation prompt, which is
        /// correct behaviour and must be answered rather than waited out. Leaving it unanswered is
        /// what made two scenarios report "the application never exited" — the #110 symptom — on the
        /// first run inside a clean guest.
        /// </summary>
        protected bool CloseApplicationAndWaitForExit(TimeSpan? timeout = null)
        {
            SendCloseKeystroke();

            TimeSpan budget = timeout ?? TimeSpan.FromSeconds(25);

            // Give the process a moment to go on its own, then deal with a confirmation prompt if
            // one is holding it open.
            if (Support.UiWait.Happened(() => Driver.Application.HasExited, TimeSpan.FromSeconds(5)))
                return true;

            AnswerExpectedPrompts(TimeSpan.FromSeconds(5));

            if (Support.UiWait.Happened(() => Driver.Application.HasExited, TimeSpan.FromSeconds(8)))
                return true;

            // The first keystroke can be consumed by an embedded session rather than the
            // application: with PuTTY focused inside a tab, Alt+F4 closes the terminal, PuTTY asks
            // to confirm, and once that is answered the session is gone but the application is
            // untouched. Measured — the close failed with the main window still up, two tabs
            // present and focus on the tab. A user in that position presses Alt+F4 again, so that is
            // what this does, rather than reaching for a synthetic WM_CLOSE that would bypass the
            // very close path #110 lives in.
            TestContext.Out.WriteLine("close keystroke was consumed by a session; sending it again");
            SendCloseKeystroke();

            if (Support.UiWait.Happened(() => Driver.Application.HasExited, TimeSpan.FromSeconds(5)))
                return true;

            AnswerExpectedPrompts(TimeSpan.FromSeconds(5));

            if (Support.UiWait.Happened(() => Driver.Application.HasExited, budget))
                return true;

            ReportWhyItIsStillRunning();
            return false;
        }

        private void SendCloseKeystroke()
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
        }

        /// <summary>
        /// Records what the application looked like when it refused to close.
        ///
        /// "The application did not exit" is not a diagnosis, and this project has already spent
        /// four mis-aimed fixes on a focus bug that was reported that way. The interesting question
        /// is where the close keystroke went: with an embedded session holding the keyboard, Alt+F4
        /// can reach the session window and close a tab instead of the application, which looks
        /// identical from outside. Whether the main window is still up, and how many tabs remain,
        /// separates those.
        /// </summary>
        private void ReportWhyItIsStillRunning()
        {
            try
            {
                TestContext.Out.WriteLine("--- close failed; state at that moment ---");

                AutomationElement[] windows = Driver.Automation.GetDesktop()
                    .FindAllChildren()
                    .Where(w =>
                    {
                        try { return w.Properties.ProcessId.ValueOrDefault == Driver.Application.ProcessId; }
                        catch (Exception) { return false; }
                    })
                    .ToArray();

                foreach (AutomationElement w in windows)
                {
                    string name = "";
                    string id = "";
                    try { name = w.Name; } catch (Exception) { }
                    try { id = w.Properties.AutomationId.ValueOrDefault; } catch (Exception) { }
                    TestContext.Out.WriteLine($"  top-level window: id='{id}' name='{name}'");
                }

                try
                {
                    int tabs = MainWindow
                        .FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.TabItem))
                        .Length;
                    TestContext.Out.WriteLine($"  tab items still present: {tabs}");
                }
                catch (Exception ex)
                {
                    TestContext.Out.WriteLine($"  could not count tabs: {ex.GetType().Name}");
                }

                TestContext.Out.WriteLine("  focused: " + DescribeFocusedElement());
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"could not describe the running application: {ex.GetType().Name}");
            }
        }

        /// <summary>
        /// The focused element as "id / name / owning process", or why it could not be read.
        ///
        /// Retried, because reading focus immediately after a modal closes throws
        /// PropertyNotSupportedException while the desktop settles. Treating that first throw as
        /// an answer made a scenario announce that another process held the keyboard when nothing
        /// of the sort had happened.
        /// </summary>
        protected string DescribeFocusedElement()
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    AutomationElement focused = Driver.Automation.FocusedElement();
                    return $"id='{focused.AutomationId}' name='{focused.Name}' "
                           + $"pid={focused.Properties.ProcessId.ValueOrDefault}";
                }
                catch (Exception ex)
                {
                    if (attempt == 4)
                        return $"unreadable ({ex.GetType().Name})";
                    System.Threading.Thread.Sleep(400);
                }
            }

            return "unreadable";
        }

        /// <summary>
        /// Answers prompts that are expected behaviour, and fails on any that are not.
        ///
        /// A dialog left standing invalidates whatever the test does next — clicks and keystrokes go
        /// to the dialog, and focus assertions report its buttons as the thief. Rather than let that
        /// surface as a misleading product symptom, an unrecognised dialog fails the test here, with
        /// its own title and message in the failure.
        /// </summary>
        protected void AnswerExpectedPrompts(TimeSpan? waitFor = null)
        {
            Support.ModalDialogs.Dialog[] unhandled =
                Support.ModalDialogs.AnswerExpectedPrompts(Driver, TestContext.Out.WriteLine, waitFor);

            if (unhandled.Length == 0)
                return;

            Assert.Fail("the application raised a dialog this battery does not recognise, so the "
                        + "rest of the scenario would have been driven against it: "
                        + string.Join("; ", unhandled.Select(d => d.ToString())));
        }

        /// <summary>
        /// Restarts the application against the same deployment, so state written on shutdown is
        /// read back on the next start.
        ///
        /// This is the only way to test persistence honestly. Asserting that a setting object holds
        /// a value proves nothing about whether it survives — #134 and #117 were both cases where
        /// the in-memory state was right and the saved state was not.
        /// </summary>
        protected void RestartApplication()
        {
            bool exited = CloseApplicationAndWaitForExit();
            Assert.That(exited, Is.True, "the application did not exit, so it never wrote its settings");

            Driver.Dispose();
            Driver = new AppDriver(Deployment.ExecutablePath);
            MainWindow = Driver.Start(TimeSpan.FromSeconds(60));
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// Pressing OK in Options must not move the application log.
    ///
    /// Found in a real-use log: after Options > OK the session wrote nothing more to the log it had
    /// opened at startup, not even its shutdown lines. The Notifications page re-pointed the
    /// appender to the stored log path while "log to the application directory" was on. A portable
    /// copy that carries a path from another copy or machine is exactly that state, so the seed
    /// stores such a path.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class OptionsLogPathAcceptanceTests : UiAcceptanceTestBase
    {
        private string ElsewhereLog => Path.Combine(Deployment.Directory, "another-copy", "mRemoteNG Connection Manager.log");

        protected override void SeedSettings()
        {
            Deployment.WriteSettings(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["LogToApplicationDirectory"] = "True",
                ["LogFilePath"] = ElsewhereLog,
            });
        }

        [Test]
        public void PressingOkInOptionsKeepsTheLogBesideTheProgram()
        {
            AutomationElement file = UiWait.FindRequired(
                MainWindow, cf => cf.ByName("File").And(cf.ByControlType(ControlType.MenuItem)), "File menu");
            file.Patterns.ExpandCollapse.Pattern.Expand();
            UiWait.Settle(MainWindow);
            UiWait.FindRequired(Driver.Automation.GetDesktop(),
                                cf => cf.ByName("Options...").And(cf.ByControlType(ControlType.MenuItem)),
                                "File menu entry 'Options...'").Click();
            UiWait.Settle(MainWindow);

            UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("btnOK"), "Options OK button").Click();
            UiWait.Settle(MainWindow);
            AnswerExpectedPrompts(TimeSpan.FromSeconds(3));

            bool exited = CloseApplicationAndWaitForExit();
            AssertExitedCleanly(exited, "after pressing OK in Options");

            string log = Deployment.ReadAppLog() ?? "";
            bool movedLogExists = File.Exists(ElsewhereLog);
            TestContext.Out.WriteLine($"log beside the program: {log.Length} chars, process_stop there: {log.Contains("event=process_stop", StringComparison.Ordinal)}; stored-path log exists: {movedLogExists}");

            Assert.Multiple(() =>
            {
                Assert.That(log, Does.Contain("[BtnOK_Click] START").Or.Contain("Saving page"),
                            "the log beside the program does not show the Options save, so this scenario did not press OK");
                Assert.That(log, Does.Contain("event=process_stop"),
                            "after Options > OK the shutdown lines did not reach the log the application opened at startup");
                Assert.That(movedLogExists, Is.False,
                            "the application wrote a log at the stored path although 'log to the application directory' is on");
            });
        }
    }
}

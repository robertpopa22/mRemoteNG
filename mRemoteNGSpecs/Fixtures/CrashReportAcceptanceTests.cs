using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using mRemoteNG.Connection.Protocol;
using mRemoteNGSpecs.Drivers;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// The unhandled-exception dialog, reached the way #175, #180 and #181 reached it: a portable
    /// folder without one of the shipped assemblies, and a double-click on a connection.
    ///
    /// The unit suite drives the real form through a stand-in tracker and proves what the dialog
    /// does once it is up. What it cannot prove is that the dialog comes up at all from a real
    /// double-click in the real process — WinForms confirms a double-click by hit-testing the
    /// physical cursor, so this needs a desktop with input, which is what the lab guest is for.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class CrashReportAcceptanceTests : UiAcceptanceTestBase
    {
        private const string ConnectionName = "alpha";

        protected override void SeedSettings()
        {
            ConnectionsSeeder seeder = new();
            seeder.Add(ConnectionName, "127.0.0.1", ProtocolType.SSH2, 1);
            Deployment.WriteConnectionsFile(seeder.Build());

            // The reporters' folder: the shipped assembly is not beside the executable. The
            // scenario directory holds hard links, so this touches nothing but this scenario.
            File.Delete(Path.Combine(Deployment.Directory, "ExternalConnectors.dll"));
        }

        private AutomationElement Tree() =>
            UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("ConnectionTree"), "connection tree");

        private static string SafeName(AutomationElement e)
        {
            try { return e.Name; } catch (Exception) { return ""; }
        }

        [Test]
        [Issues("#175", "#178", "#180", "#181")]
        public void OpeningAConnectionWithoutAShippedAssemblyRaisesTheCrashDialogAndNamesTheInstallation()
        {
            UiWait.Until(() => Tree().FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                                     .Any(e => string.Equals(SafeName(e), ConnectionName, StringComparison.Ordinal)),
                         "the seeded connection to appear in the tree", TimeSpan.FromSeconds(20));

            AutomationElement row = Tree()
                .FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                .First(e => string.Equals(SafeName(e), ConnectionName, StringComparison.Ordinal));
            // Not row.DoubleClick(): on the lab guest FlaUI's two waited clicks arrive as two single
            // clicks and the tree starts a rename instead. One SendInput batch is a real double-click.
            Win32Mouse.DoubleClick(row);

            CrashWatcher.CrashResult crash = CrashWatcher.Check(Driver, TimeSpan.FromSeconds(15));
            Assert.That(crash.Occurred, Is.True,
                        "double-clicking a connection with ExternalConnectors.dll missing did not raise the "
                        + "unhandled-exception dialog — the reporters' crash no longer reproduces, or it is "
                        + "being swallowed somewhere");
            Assert.That(Driver.Application.HasExited, Is.False, "the crash is non-fatal and must leave the app running");

            AutomationElement dialog = UiWait.FindRequired(
                Driver.Automation.GetDesktop(),
                cf => cf.ByAutomationId("FrmUnhandledException"),
                "the unhandled-exception dialog");

            string shot = Path.Combine(Deployment.Directory, "crash-dialog.png");
            Capture.Element(dialog).ToFile(shot);
            TestContext.AddTestAttachment(shot, "the crash dialog as shown");

            string message = UiWait.FindRequired(dialog, cf => cf.ByAutomationId("textBoxExceptionMessage"), "exception message")
                                   .AsTextBox().Text;
            string stack = UiWait.FindRequired(dialog, cf => cf.ByAutomationId("textBoxStackTrace"), "stack trace")
                                 .AsTextBox().Text;
            AutomationElement submit = UiWait.FindRequired(dialog, cf => cf.ByAutomationId("buttonCreateBug"), "submit button");
            TestContext.Out.WriteLine("message: " + message.Replace(Environment.NewLine, " | ", StringComparison.Ordinal));
            TestContext.Out.WriteLine("submit button: '" + SafeName(submit) + "' enabled=" + submit.IsEnabled);

            Assert.Multiple(() =>
            {
                // #178: the dialog says the installation is incomplete and which file is missing,
                // above the loader's own message, which is what the reporters saw.
                Assert.That(message, Does.Contain("ExternalConnectors.dll"));
                Assert.That(message, Does.Contain(Deployment.Directory));
                Assert.That(message, Does.Contain("Could not load file or assembly"));
                Assert.That(stack, Does.Contain("OpenConnection"), "the stack is the reporters' stack");
                Assert.That(submit.IsEnabled, Is.True, "the report can be submitted from here");
            });

            // Not clicked: a build without a crash-report token opens the browser instead of the
            // tracker, and the submission itself is proven by the unit suite against a stand-in.
            UiWait.FindRequired(dialog, cf => cf.ByAutomationId("buttonClose"), "close button").AsButton().Invoke();
            UiWait.Until(() => CrashWatcher.Check(Driver, TimeSpan.FromMilliseconds(300)).Occurred == false,
                         "the crash dialog to close", TimeSpan.FromSeconds(10));
            Assert.That(Driver.Application.HasExited, Is.False, "closing a non-fatal crash dialog must not end the app");
        }
    }
}

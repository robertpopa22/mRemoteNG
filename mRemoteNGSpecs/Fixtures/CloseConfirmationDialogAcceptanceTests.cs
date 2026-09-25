using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
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
    /// The close confirmation as a user meets it (#198): a live session, "ask before closing"
    /// switched on, a middle-click on the tab. The reporter saw this dialog squeezed to a fraction
    /// of its width, the question cut off and the "do not show again" check box under the buttons.
    ///
    /// This lab runs at one DPI on one monitor, so it cannot recreate their multi-monitor remote
    /// session; the unit suite narrows the real dialog to their width instead. What this adds is
    /// the dialog inside the running application: that it is laid out cleanly there, that it can
    /// be resized, that both answers still do what they say, that the question is about the
    /// connection rather than a panel, and that closing the only tab asks exactly once.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class CloseConfirmationDialogAcceptanceTests : UiAcceptanceTestBase
    {
        // Long, like the reporter's, so the question has to wrap.
        private const string ConnectionName = "SERVER-PROD-SQL-01 (Local administrator)";

        protected override void SeedSettings()
        {
            ConnectionsSeeder seeder = new();
            seeder.Add(ConnectionName, LabTargets.LinuxHost, ProtocolType.RDP, LabTargets.Rdp,
                       LabTargets.LinuxUser, LabTargets.LinuxPassword);
            Deployment.WriteConnectionsFile(seeder.Build());

            Deployment.WriteSettings(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["KeepTabsOpenAfterDisconnect"] = "False",
                ["ConfirmCloseConnection"] = "4", // ConfirmCloseEnum.All: ask before closing a connection
            });
        }

        private static void SkipUnlessReachable(string host, int port)
        {
            try
            {
                using System.Net.Sockets.TcpClient probe = new();
                if (!probe.ConnectAsync(host, port).Wait(TimeSpan.FromSeconds(3)))
                    Assert.Ignore($"lab RDP target {host}:{port} did not answer.");
            }
            catch (Exception ex)
            {
                Assert.Ignore($"lab RDP target {host}:{port} not reachable: {ex.GetType().Name}");
            }
        }

        private AutomationElement[] SessionTabs() =>
            MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
                      .Where(t =>
                      {
                          try { return (t.Name ?? "").Contains("SERVER-PROD", StringComparison.OrdinalIgnoreCase); }
                          catch (Exception) { return false; }
                      })
                      .ToArray();

        private AutomationElement Row()
        {
            AutomationElement tree = UiWait.FindRequired(
                MainWindow, cf => cf.ByAutomationId("ConnectionTree"), "connection tree");
            return tree.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                       .First(e =>
                       {
                           try { return (e.Name ?? "").Contains("SERVER-PROD", StringComparison.OrdinalIgnoreCase); }
                           catch (Exception) { return false; }
                       });
        }

        private bool LogContains(string phrase) =>
            (Deployment.ReadAppLog() ?? "").Contains(phrase, StringComparison.Ordinal);

        private void OpenSessionAndWaitUntilEstablished()
        {
            Win32Mouse.DoubleClick(Row());
            UiWait.Settle(MainWindow);
            AnswerExpectedPrompts(TimeSpan.FromSeconds(20));
            UiWait.Until(() => SessionTabs().Length > 0, "an RDP session tab to open", TimeSpan.FromSeconds(60));
            UiWait.Until(() => LogContains("established by user"), "the RDP session to connect", TimeSpan.FromSeconds(90));
            AnswerExpectedPrompts(TimeSpan.FromSeconds(5));
            Thread.Sleep(TimeSpan.FromSeconds(3));

            // The close confirmation is only asked for a live session; a session the target already
            // dropped closes without asking, and the run could say nothing about the dialog.
            if (LogContains("Protocol Event Disconnected") || SessionTabs().Length == 0)
                Assert.Ignore("the RDP target ended the session on its own before the tab could be closed");
        }

        private AutomationElement? FindConfirmation()
        {
            int pid = Driver.Application.ProcessId;
            return Driver.Automation.GetDesktop()
                         .FindAllDescendants(cf => cf.ByAutomationId("frmTaskDialog"))
                         .FirstOrDefault(w =>
                         {
                             try { return w.Properties.ProcessId.Value == pid; }
                             catch (Exception) { return false; }
                         });
        }

        private AutomationElement WaitForConfirmation()
        {
            UiWait.Until(() => FindConfirmation() is not null, "the close confirmation", TimeSpan.FromSeconds(15));
            return FindConfirmation()!;
        }

        private AutomationElement? TryWaitForConfirmation(TimeSpan timeout) =>
            UiWait.Happened(() => FindConfirmation() is not null, timeout) ? FindConfirmation() : null;

        private static Rectangle Bounds(AutomationElement dialog, string automationId) =>
            UiWait.FindRequired(dialog, cf => cf.ByAutomationId(automationId), automationId).BoundingRectangle;

        [Test]
        [Issues("#198")]
        public void TheCloseConfirmationIsLaidOutCleanlyCanBeResizedAndBothAnswersWork()
        {
            SkipUnlessReachable(LabTargets.LinuxHost, LabTargets.Rdp);
            Assert.That(LabTargets.LinuxPassword, Is.Not.Empty, "MRNG_LAB_LINUX_PASSWORD is not visible to the battery process");

            OpenSessionAndWaitUntilEstablished();

            // First answer: Cancel. The session must stay.
            Win32Mouse.MiddleClick(SessionTabs().First());
            AutomationElement dialog = WaitForConfirmation();
            AssertCleanLayout(dialog, "close-confirmation.png");
            AssertAsksAboutTheConnection(dialog);
            UiWait.FindRequired(dialog, cf => cf.ByAutomationId("bt3"), "Cancel button").AsButton().Invoke();
            Thread.Sleep(TimeSpan.FromSeconds(2));
            Assert.That(SessionTabs().Length, Is.EqualTo(1), "Cancel must leave the session open");

            // Second answer: Disconnect. The tab must go.
            Win32Mouse.MiddleClick(SessionTabs().First());
            dialog = WaitForConfirmation();
            UiWait.FindRequired(dialog, cf => cf.ByAutomationId("bt2"), "Disconnect button").AsButton().Invoke();

            // Closing the last tab closes its panel. The panel used to ask again at that point,
            // because it still counted the tab being disposed: the user confirmed once and was asked
            // a second time about a connection that was already going. One confirmation is the
            // whole contract, so a second one fails the scenario instead of being answered.
            AutomationElement? followUp = TryWaitForConfirmation(TimeSpan.FromSeconds(8));
            string? followUpText = followUp?.FindFirstDescendant(cf => cf.ByAutomationId("lbMainInstruction"))?.Name;
            if (followUp is not null)
            {
                Capture.Element(followUp).ToFile(Path.Combine(Deployment.Directory, "close-confirmation-second.png"));
                TestContext.AddTestAttachment(Path.Combine(Deployment.Directory, "close-confirmation-second.png"),
                                              "the second confirmation that should not have appeared");
                UiWait.FindRequired(followUp, cf => cf.ByAutomationId("bt3"), "Cancel button").AsButton().Invoke();
            }
            Assert.That(followUp is null, Is.True, $"closing the only tab asked a second time: \"{followUpText}\"");

            UiWait.Until(() => SessionTabs().Length == 0, "the session tab to close", TimeSpan.FromSeconds(45));
        }

        /// <summary>
        /// The confirmation shown for a tab asks about that connection. It used to reuse the
        /// panel's question -- "close the panel, "&lt;tab&gt;"? Any connections that it contains will
        /// also be closed" -- naming a tab as if it were a panel.
        /// </summary>
        private static void AssertAsksAboutTheConnection(AutomationElement dialog)
        {
            string question = UiWait.FindRequired(dialog, cf => cf.ByAutomationId("lbMainInstruction"), "question").Name ?? "";
            TestContext.Out.WriteLine($"the question: \"{question}\"");
            Assert.Multiple(() =>
            {
                Assert.That(question, Does.Contain("SERVER-PROD-SQL-01"), "the question does not name the connection");
                Assert.That(question, Does.Contain("disconnect").IgnoreCase, "the question does not ask to disconnect");
                Assert.That(question, Does.Not.Contain("panel").IgnoreCase, "the question talks about a panel for a single tab");
            });
        }

        private void AssertCleanLayout(AutomationElement dialog, string screenshotName)
        {
            UiWait.Settle(dialog.AsWindow());

            string shot = Path.Combine(Deployment.Directory, screenshotName);
            Capture.Element(dialog).ToFile(shot);
            TestContext.AddTestAttachment(shot, "the confirmation as shown");

            Rectangle window = dialog.BoundingRectangle;
            Rectangle question = Bounds(dialog, "lbMainInstruction");
            Rectangle check = Bounds(dialog, "cbVerify");
            AutomationElement disconnect = UiWait.FindRequired(dialog, cf => cf.ByAutomationId("bt2"), "Disconnect button");
            AutomationElement cancel = UiWait.FindRequired(dialog, cf => cf.ByAutomationId("bt3"), "Cancel button");
            bool canResize = dialog.Patterns.Transform.PatternOrDefault?.CanResize.ValueOrDefault ?? false;

            TestContext.Out.WriteLine($"dialog {window}, question {question}, check box {check}, "
                                      + $"'{disconnect.Name}' {disconnect.BoundingRectangle}, '{cancel.Name}' {cancel.BoundingRectangle}, "
                                      + $"resizable {canResize}");

            Assert.Multiple(() =>
            {
                Assert.That(window.Contains(question), Is.True, "the question runs outside the dialog");
                Assert.That(window.Contains(check), Is.True, "the check box runs outside the dialog");
                Assert.That(window.Contains(disconnect.BoundingRectangle) && window.Contains(cancel.BoundingRectangle), Is.True,
                            "a button runs outside the dialog");
                Assert.That(check.IntersectsWith(disconnect.BoundingRectangle) || check.IntersectsWith(cancel.BoundingRectangle), Is.False,
                            "the check box lies under a button");
                Assert.That(question.Bottom, Is.LessThanOrEqualTo(Math.Min(disconnect.BoundingRectangle.Top, check.Top)),
                            "the question overlaps the button row");
                Assert.That(canResize, Is.True, "the dialog should be resizable");
            });
        }
    }
}

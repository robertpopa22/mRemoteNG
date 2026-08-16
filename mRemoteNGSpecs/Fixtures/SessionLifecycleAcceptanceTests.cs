using System;
using System.Linq;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using mRemoteNG.Connection.Protocol;
using mRemoteNGSpecs.Drivers;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// Opening and closing real sessions — the area that produced the most reported crashes and the
    /// one the unit suite is furthest from reaching.
    ///
    /// These connect to the isolated lab rather than to mocks, because every issue here came from
    /// the interaction between the app and a live protocol client: a tab that would not close, a
    /// window that stayed open after the last tab went, a crash when a session was disposed. None
    /// of that is reachable without something on the other end of the socket.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class SessionLifecycleAcceptanceTests : UiAcceptanceTestBase
    {
        protected override void SeedSettings()
        {
            ConnectionsSeeder seeder = new();

            if (LabTargets.IsReachable(LabTargets.LinuxHost, LabTargets.Rdp))
                seeder.Add("lab-linux-rdp", LabTargets.LinuxHost, ProtocolType.RDP, LabTargets.Rdp,
                           LabTargets.LinuxUser, LabTargets.LinuxPassword);

            if (LabTargets.IsReachable(LabTargets.WindowsHost, LabTargets.Rdp))
                seeder.Add("lab-win-rdp", LabTargets.WindowsHost, ProtocolType.RDP, LabTargets.Rdp,
                           LabTargets.WindowsUser, LabTargets.WindowsPassword);

            if (LabTargets.IsReachable(LabTargets.LinuxHost, LabTargets.Ssh))
                seeder.Add("lab-linux-ssh", LabTargets.LinuxHost, ProtocolType.SSH2, LabTargets.Ssh,
                           LabTargets.LinuxUser, LabTargets.LinuxPassword);

            if (LabTargets.IsReachable(LabTargets.LinuxHost, LabTargets.Vnc))
                seeder.Add("lab-linux-vnc", LabTargets.LinuxHost, ProtocolType.VNC, LabTargets.Vnc,
                           null, LabTargets.LinuxPassword);

            Deployment.WriteConnectionsFile(seeder.Build());
        }

        private AutomationElement Tree() =>
            UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("ConnectionTree"), "connection tree");

        private AutomationElement[] Rows() =>
            Tree().FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                  .Where(e => !string.IsNullOrWhiteSpace(SafeName(e)))
                  .ToArray();

        private static string SafeName(AutomationElement e)
        {
            try { return e.Name; } catch (Exception) { return ""; }
        }

        private AutomationElement RequireRow(string name)
        {
            AutomationElement? row = null;
            UiWait.Until(() =>
            {
                row = Rows().FirstOrDefault(r => SafeName(r).Contains(name, StringComparison.OrdinalIgnoreCase));
                return row is not null;
            }, $"the '{name}' row to appear in the connection tree", TimeSpan.FromSeconds(15));
            return row!;
        }

        private int TabCount()
        {
            AutomationElement[] tabs = MainWindow
                .FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem));
            return tabs.Length;
        }

        private void SkipUnless(string host, int port, string what)
        {
            if (!LabTargets.IsReachable(host, port))
                Assert.Ignore($"{what} not reachable at {LabTargets.Describe(host, port)}.");
        }

        /// <summary>
        /// The seeded connections must actually load. If this fails, every other test in this
        /// fixture is meaningless, so it is asserted separately rather than assumed.
        /// </summary>
        [Test]
        public void TheSeededLabConnectionsLoad()
        {
            // With no lab reachable the seeder writes an empty file, which is not a product defect.
            bool anyTarget = LabTargets.IsReachable(LabTargets.LinuxHost, LabTargets.Rdp)
                             || LabTargets.IsReachable(LabTargets.LinuxHost, LabTargets.Ssh)
                             || LabTargets.IsReachable(LabTargets.LinuxHost, LabTargets.Vnc)
                             || LabTargets.IsReachable(LabTargets.WindowsHost, LabTargets.Rdp);
            if (!anyTarget)
                Assert.Ignore("no lab target reachable; the seeded file is empty by design");

            AutomationElement[] rows = Rows();
            TestContext.Out.WriteLine("tree rows: " + string.Join(", ", rows.Select(SafeName)));

            Assert.That(rows.Any(r => SafeName(r).StartsWith("lab-", StringComparison.OrdinalIgnoreCase)),
                        Is.True,
                        "no lab connection was loaded from the seeded confCons.xml — the fixture is "
                        + "not exercising what it claims to");
        }

        /// <summary>
        /// Touches #110 and #142.
        ///
        /// MEASURED SCOPE: this connects, reconnects and closes the application, asserting the
        /// process exits. It does NOT close a tab, so it cannot prove #110 (close a tab, reopen,
        /// window then refuses to close) or #142 (closing a tab crashed). Proving those needs a tab
        /// actually closed — middle-click or the tab context menu — which is not written yet.
        /// </summary>
        [Test]
        [Touches("#110", "#142")]
        public void ConnectingClosingAndReconnectingStillLetsTheApplicationClose()
        {
            SkipUnless(LabTargets.LinuxHost, LabTargets.Ssh, "lab SSH target");

            AutomationElement row = RequireRow("lab-linux-ssh");
            row.DoubleClick();
            UiWait.Settle(MainWindow);
            UiWait.Until(() => TabCount() > 0, "a session tab to open", TimeSpan.FromSeconds(30));
            AssertNoCrash("after connecting over SSH");

            int afterConnect = TabCount();
            TestContext.Out.WriteLine($"tabs after connect: {afterConnect}");

            row.DoubleClick();   // reconnect
            UiWait.Settle(MainWindow);
            AssertNoCrash("after reconnecting");

            bool exited = CloseApplicationAndWaitForExit();

            Assert.That(exited, Is.True,
                        "the application did not exit after closing its window following a "
                        + "connect/reconnect cycle — the #110 symptom");
        }

        /// <summary>
        /// Stress coverage for #166. Opening a real VNC session and exiting cleanly exercises the
        /// dispose ordering, but #166 is a race between VncSharpCore's polling thread and handle
        /// destruction: one clean exit means "did not reproduce this time", not "the race is gone".
        /// </summary>
        [Test]
        [StressCoverage("#166")]
        public void OpeningAndClosingAVncSessionDoesNotCrash()
        {
            SkipUnless(LabTargets.LinuxHost, LabTargets.Vnc, "lab VNC server");

            AutomationElement row = RequireRow("lab-linux-vnc");
            row.DoubleClick();
            UiWait.Settle(MainWindow);
            UiWait.Until(() => TabCount() > 0, "a VNC session tab to open", TimeSpan.FromSeconds(30));
            AssertNoCrash("after opening a VNC session");

            // The #166 race is between VncSharpCore's polling thread and handle destruction, so
            // the evidence is in HOW the process ends: a clean exit means the dispose ordering
            // held. Checking for a crash dialog after the process is gone proves nothing.
            bool exited = CloseApplicationAndWaitForExit();
            AssertExitedCleanly(exited, "after disposing a VNC session");
        }

        /// <summary>
        /// Covers: #143 (the search box became unusable after connecting to an RDP session, because
        /// the app stole focus back on every activation).
        ///
        /// MEASURED SCOPE — this does NOT yet detect #143, and three attempts are recorded here so
        /// the next person does not repeat them:
        ///
        ///   1. Assigning text via the Value pattern bypasses Win32 focus entirely, so the original
        ///      version could not fail whatever the focus handler did.
        ///   2. Asserting on the box's text is ambiguous: it shows a "Search" placeholder when
        ///      empty, so "focus was stolen" and "nothing typed" look identical.
        ///   3. Asserting that focus lands on txtSearch is the fix's actual contract, and it still
        ///      passes with the identity gate removed from ConnectionWindow — most likely because
        ///      the RDP tab opens without the session connecting and focusing, so the activation
        ///      path the fix gates never runs.
        ///
        /// What it does guard: that clicking the search box with a session tab open leaves focus in
        /// the search box. Covering #143 needs a session that genuinely connects and takes focus.
        /// </summary>
        [Test]
        [Touches("#143")]
        public void TheSearchBoxStaysUsableAfterConnectingToRdp()
        {
            SkipUnless(LabTargets.LinuxHost, LabTargets.Rdp, "lab RDP target");

            AutomationElement row = RequireRow("lab-linux-rdp");
            row.DoubleClick();
            UiWait.Settle(MainWindow);
            UiWait.Until(() => TabCount() > 0, "an RDP session tab to open", TimeSpan.FromSeconds(45));

            AutomationElement search = UiWait.FindRequired(
                MainWindow, cf => cf.ByAutomationId("txtSearch"), "connection tree search box");

            // Clear through the Value pattern first: that writes straight into the control without
            // needing focus, so the box is known-empty before the part that DOES depend on focus.
            search.AsTextBox().Text = "";
            UiWait.Settle(MainWindow);

            MainWindow.Focus();
            search.Click();
            UiWait.Settle(MainWindow);

            // The contract of the #143 fix, stated directly: after clicking the search box with a
            // session open, the search box is what holds keyboard focus. Asserting on typed text
            // instead is weaker and ambiguous — the box shows a "Search" placeholder when empty, so
            // "no text" and "keystrokes went elsewhere" look identical.
            string focusedId = "";
            string focusedName = "";
            int focusedProcess = -1;
            try
            {
                AutomationElement focused = Driver.Automation.FocusedElement();
                focusedId = focused.AutomationId;
                focusedName = focused.Name;
                focusedProcess = focused.Properties.ProcessId;
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"could not read focused element: {ex.GetType().Name}");
            }

            TestContext.Out.WriteLine($"focused after click: id='{focusedId}' name='{focusedName}'");

            // If something outside the application holds the keyboard, this test cannot tell focus
            // theft by the RDP session apart from focus theft by the desktop, and answering anyway
            // would convict the app of another window's behaviour. Measured on this machine: a
            // Windows "Use the recommended settings" dialog took focus mid-test.
            if (focusedProcess != Driver.Application.ProcessId)
            {
                Assert.Inconclusive(
                    $"'{focusedName}' (another process) holds the keyboard, so focus behaviour "
                    + "inside mRemoteNG cannot be judged. Run the battery on an unshared session — "
                    + "this is the reason the lab guest exists.");
            }

            Assert.That(focusedId, Is.EqualTo("txtSearch"),
                        $"clicking the search box did not leave it focused while an RDP session was "
                        + $"open — focus went to '{focusedName}' (id '{focusedId}') instead, which is "
                        + "the #143 symptom: the session steals focus back on every activation");

            AssertNoCrash("after typing into the search box with an RDP session open");
        }
    }
}

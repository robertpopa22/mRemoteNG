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
            AutomationElement[] rows = Rows();
            TestContext.Out.WriteLine("tree rows: " + string.Join(", ", rows.Select(SafeName)));

            Assert.That(rows.Any(r => SafeName(r).StartsWith("lab-", StringComparison.OrdinalIgnoreCase)),
                        Is.True,
                        "no lab connection was loaded from the seeded confCons.xml — the fixture is "
                        + "not exercising what it claims to");
        }

        /// <summary>
        /// Covers: #110 (the window would not close after a tab was closed and a connection
        /// reopened), #142 (closing a tab crashed).
        ///
        /// Connect, close the tab, reconnect, then close the application. The assertion is that the
        /// process actually exits — the #110 symptom was an app that stayed alive with no window a
        /// user could act on.
        /// </summary>
        [Test]
        [Issues("#110", "#142")]
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
        /// Covers: #166 (InvalidOperationException when a VNC session was disposed while its
        /// polling thread was still calling back onto a destroyed handle).
        ///
        /// Needs a real VNC server: the race is between the library's background thread and handle
        /// destruction, and neither happens without a live session.
        /// </summary>
        [Test]
        [Issues("#166")]
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
        /// Needs a real RDP session — the focus theft came from the RDP control's own activation.
        /// </summary>
        [Test]
        [Issues("#143")]
        public void TheSearchBoxStaysUsableAfterConnectingToRdp()
        {
            SkipUnless(LabTargets.LinuxHost, LabTargets.Rdp, "lab RDP target");

            AutomationElement row = RequireRow("lab-linux-rdp");
            row.DoubleClick();
            UiWait.Settle(MainWindow);
            UiWait.Until(() => TabCount() > 0, "an RDP session tab to open", TimeSpan.FromSeconds(45));

            AutomationElement search = UiWait.FindRequired(
                MainWindow, cf => cf.ByAutomationId("txtSearch"), "connection tree search box");

            search.Focus();
            search.AsTextBox().Text = "lab";
            UiWait.Settle(MainWindow);

            Assert.That(search.AsTextBox().Text, Is.EqualTo("lab"),
                        "the search box did not keep typed text while an RDP session was open — "
                        + "focus was stolen back by the session (#143)");

            AssertNoCrash("after typing into the search box with an RDP session open");
        }
    }
}

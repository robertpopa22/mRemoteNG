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

        private static void SkipUnless(string host, int port, string what)
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
            AnswerExpectedPrompts(TimeSpan.FromSeconds(15));
            UiWait.Until(() => TabCount() > 0, "a session tab to open", TimeSpan.FromSeconds(30));
            AssertNoCrash("after connecting over SSH");

            int afterConnect = TabCount();
            TestContext.Out.WriteLine($"tabs after connect: {afterConnect}");

            row.DoubleClick();   // reconnect
            UiWait.Settle(MainWindow);
            AnswerExpectedPrompts(TimeSpan.FromSeconds(10));
            AssertNoCrash("after reconnecting");

            bool exited = CloseApplicationAndWaitForExit();

            Assert.That(exited, Is.True,
                        "the application did not exit after closing its window following a "
                        + "connect/reconnect cycle — the #110 symptom");
        }

        /// <summary>
        /// Covers: #142 for real. The scenario above never closes a tab (documented in its own
        /// comment) so it cannot prove #142 either way; this does, by taking the exact path the bug
        /// was in -- DockPaneStripNG.MiddleClickCloseTab -> QueueCloseTab -- which only a genuine
        /// middle mouse click reaches. Neither AutomationElement.Click() nor the Invoke pattern can
        /// send a middle click, so this goes through FlaUI's raw Mouse input instead of UIA.
        ///
        /// MEASURED SCOPE -- two real defects found and fixed while getting this scenario to run at
        /// all: ModalDialogs did not know the panel-close confirmation's affirmative button is
        /// captioned "Disconnect" (CTaskDialog + ETaskDialogButtons.DisconnectCancel), not "Yes"/"OK",
        /// so it recognised the dialog by text and then failed to click it; and it did not recognise
        /// the confirmation's wording at all until the first live run surfaced it. Both are fixed in
        /// ModalDialogs.cs.
        ///
        /// What is NOT fixed: getting past PuTTY's host-key prompt before the middle-click is
        /// intermittent in this lab -- roughly one run in several times out waiting for it, the
        /// others sail through, and a "no application dialogs on screen" report from ModalDialogs
        /// each time it happens means neither UIA nor the Win32 EnumWindows fallback saw the window,
        /// not that the budget ran out. Same category as #166's VNC race: a clean pass here means the
        /// scenario got past that specific window this time, not that the detection gap is closed.
        /// </summary>
        [Test]
        [Touches("#142")]
        [StressCoverage("#142")]
        public void MiddleClickingATabClosesItWithoutCrashing()
        {
            SkipUnless(LabTargets.LinuxHost, LabTargets.Ssh, "lab SSH target");

            AutomationElement row = RequireRow("lab-linux-ssh");
            row.DoubleClick();
            UiWait.Settle(MainWindow);
            AnswerExpectedPrompts(TimeSpan.FromSeconds(45));
            UiWait.Until(() => TabCount() > 0, "a session tab to open", TimeSpan.FromSeconds(30));
            AssertNoCrash("after connecting over SSH");

            // The tab exists as soon as the connection starts, before PuTTY's host-key prompt is
            // resolved — so TabCount() > 0 above does not mean the screen is clear. First run of this
            // scenario middle-clicked straight into a still-open "PuTTY Security Alert" (visible in
            // the failure screenshot) because the guest was busy running the rest of the battery and
            // the first 15s budget ran out before the prompt appeared. Same reasoning as the #143
            // scenario's second AnswerExpectedPrompts() before it measures focus.
            AnswerExpectedPrompts(TimeSpan.FromSeconds(15));

            int before = TabCount();
            AutomationElement tab = MainWindow
                .FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
                .First(t => SafeName(t).Contains("lab-linux-ssh", StringComparison.OrdinalIgnoreCase));

            Support.Win32Mouse.MiddleClick(tab);
            UiWait.Settle(MainWindow);

            // The application asks before closing a panel that still holds a live connection --
            // legitimate behaviour, not the #142 defect. First live run of this scenario surfaced the
            // exact wording ("Are you sure you want to close the panel... Any connections that it
            // contains will also be closed"), which ModalDialogs did not yet recognise; it does now.
            AnswerExpectedPrompts(TimeSpan.FromSeconds(5));

            AssertNoCrash("after middle-clicking the session tab to close it — the #142 NRE");

            // What "closed" means here is decided by KeepTabsOpenAfterDisconnect, and this battery
            // runs on defaults, where it is TRUE: answering Disconnect closes the PROTOCOL but the
            // tab deliberately stays as a reconnect placeholder (ConnectionTab.OnFormClosing sets
            // e.Cancel under that setting — #61, and #139 documents the default). The original
            // assertion here waited for TabCount() to drop, which that design makes impossible;
            // it failed deterministically on every code/binary combination once the guest ran the
            // scenario to this point (proved 2026-08-31 by A/B against the pre-change snapshot).
            // What #142 actually needs proven is the path through
            // DockPaneStripNG.MiddleClickCloseTab -> QueueCloseTab without the NRE - the
            // AssertNoCrash above - plus the tab landing in its disconnected state instead of a
            // zombie session: the closed-state panel's "Connect" reconnect button.
            UiWait.Until(() => TabCount() == before &&
                               MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                                         .Any(b => SafeName(b).Equals("Connect", StringComparison.Ordinal)),
                         "the tab to stay open showing its disconnected state (Connect button)",
                         TimeSpan.FromSeconds(10));
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
            AnswerExpectedPrompts(TimeSpan.FromSeconds(15));
            UiWait.Until(() => TabCount() > 0, "a VNC session tab to open", TimeSpan.FromSeconds(30));
            AssertNoCrash("after opening a VNC session");

            // The #166 race is between VncSharpCore's polling thread and handle destruction, so
            // the evidence is in HOW the process ends: a clean exit means the dispose ordering
            // held. Checking for a crash dialog after the process is gone proves nothing.
            bool exited = CloseApplicationAndWaitForExit();
            AssertExitedCleanly(exited, "after disposing a VNC session");
        }

        /// <summary>
        /// The control for the #143 measurement: with no session open at all, clicking the search
        /// box must leave it holding keyboard focus.
        ///
        /// Without this, a failure in the RDP scenario is ambiguous — it could mean the session
        /// steals focus, or simply that clicking a text box through UI automation does not focus it
        /// on this machine. Only the pair carries information: control passes and RDP fails means
        /// the session is the difference. Needs no lab, so it runs everywhere the battery runs.
        /// </summary>
        [Test]
        public void ClickingTheSearchBoxFocusesItWhenNoSessionIsOpen()
        {
            AutomationElement search = UiWait.FindRequired(
                MainWindow, cf => cf.ByAutomationId("txtSearch"), "connection tree search box");

            MainWindow.Focus();
            search.Click();
            UiWait.Settle(MainWindow);

            IntPtr focused = Win32Focus.FocusedWindow();
            IntPtr expected = new(search.Properties.NativeWindowHandle.ValueOrDefault.ToInt64());

            TestContext.Out.WriteLine("focus after click : " + Win32Focus.Describe(focused));
            TestContext.Out.WriteLine("search box        : " + Win32Focus.Describe(expected));

            Assert.That(focused, Is.EqualTo(expected),
                        "clicking the search box did not focus it even with no session open, so the "
                        + "measurement itself is unsound and the RDP result proves nothing. Focus is "
                        + "on " + Win32Focus.Describe(focused));
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

            // A first RDP connection to the lab raises the untrusted-certificate prompt, because the
            // guest has never seen xrdp's self-signed certificate. Answering it beats trusting the
            // certificate machine-wide to make a test pass.
            AnswerExpectedPrompts(TimeSpan.FromSeconds(20));

            UiWait.Until(() => TabCount() > 0, "an RDP session tab to open", TimeSpan.FromSeconds(45));

            AutomationElement search = UiWait.FindRequired(
                MainWindow, cf => cf.ByAutomationId("txtSearch"), "connection tree search box");

            // Clear through the Value pattern first: that writes straight into the control without
            // needing focus, so the box is known-empty before the part that DOES depend on focus.
            search.AsTextBox().Text = "";
            UiWait.Settle(MainWindow);

            // Nothing may be covering the window when focus is measured: a standing dialog holds
            // the keyboard and its buttons get reported as the thief, which is precisely how this
            // scenario produced a false #143 symptom on its first run in a clean guest.
            AnswerExpectedPrompts();

            MainWindow.Focus();
            search.Click();
            UiWait.Settle(MainWindow);

            // The contract of the #143 fix, stated directly: after clicking the search box with a
            // session open, the search box is what holds keyboard focus. Asserting on typed text
            // instead is weaker and ambiguous — the box shows a "Search" placeholder when empty, so
            // "no text" and "keystrokes went elsewhere" look identical.
            // Measured through Win32, not UIA. FocusedElement() throws PropertyNotSupportedException
            // on every attempt while the RDP control has focus, which left this scenario permanently
            // inconclusive and unable to say anything about the issue it exists for. GetGUIThreadInfo
            // answers regardless, and it is the same view of focus the fixes for #118 and #143 had to
            // adopt inside the product.
            IntPtr focused = Win32Focus.FocusedWindow();
            IntPtr expected = new(search.Properties.NativeWindowHandle.ValueOrDefault.ToInt64());

            TestContext.Out.WriteLine("focus after click : " + Win32Focus.Describe(focused));
            TestContext.Out.WriteLine("search box        : " + Win32Focus.Describe(expected));

            if (focused == IntPtr.Zero)
            {
                Assert.Inconclusive("nothing on the desktop holds keyboard focus, so focus behaviour "
                                    + "cannot be judged either way.");
            }

            int owner = Win32Focus.ProcessOf(focused);
            if (owner != Driver.Application.ProcessId)
            {
                // PuTTY and the RDP client run in the application's own process or its helpers; a
                // third party holding the keyboard means the desktop is shared and the measurement
                // is not about mRemoteNG at all.
                Assert.Inconclusive($"another process (pid {owner}) holds the keyboard: "
                                    + Win32Focus.Describe(focused)
                                    + ". Run the battery on an unshared session — this is the reason "
                                    + "the lab guest exists.");
            }

            Assert.That(focused, Is.EqualTo(expected),
                        "clicking the search box did not leave it holding keyboard focus while an RDP "
                        + "session was open — focus is on " + Win32Focus.Describe(focused)
                        + ", which is the #143 symptom: the session steals focus back on every "
                        + "activation");

            AssertNoCrash("after typing into the search box with an RDP session open");
        }
    }
}

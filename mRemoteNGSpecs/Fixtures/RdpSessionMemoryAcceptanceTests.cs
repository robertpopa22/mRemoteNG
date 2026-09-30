using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using mRemoteNG.Connection.Protocol;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// #182: finite resource-retention checks against a real Windows RDP target. Ownership
    /// tests and a green build cannot substitute for completed logins and measured closes.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class RdpSessionMemoryAcceptanceTests : UiAcceptanceTestBase
    {
        private const string ConnectionName = "lab-windows-rdp";
        private const int Cycles = RetentionAssessment.RequiredCycles;
        private static readonly JsonSerializerOptions EvidenceJson = new() { WriteIndented = true };

        /// <summary>
        /// Run explicit tab/panel closure with each retained-tab preference. The reporter's
        /// exact preference is not assumed from the application default.
        /// </summary>
        protected virtual bool KeepTabsOpenAfterDisconnect => false;

        protected override void SeedSettings()
        {
            ConnectionsSeeder seeder = new();
            seeder.Add(ConnectionName, LabTargets.WindowsTargetHost, ProtocolType.RDP, LabTargets.Rdp,
                       LabTargets.WindowsUser, LabTargets.WindowsPassword, LabTargets.WindowsTargetName);
            Deployment.WriteConnectionsFile(seeder.Build());

            // Exercise explicit tab/panel closure with both settings. Disconnect-only and its
            // retained reconnect placeholder are separate paths, not implied by these checks.
            Deployment.WriteSettings(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["KeepTabsOpenAfterDisconnect"] = KeepTabsOpenAfterDisconnect ? "True" : "False",
                ["AlwaysShowPanelTabs"] = "True",
                ["ConfirmCloseConnection"] = "1", // ConfirmCloseEnum.Never — nothing to answer per cycle
            });
        }

        private static void SkipUnlessReachable(string host, int port)
        {
            try
            {
                using System.Net.Sockets.TcpClient probe = new();
                if (!probe.ConnectAsync(host, port).Wait(TimeSpan.FromSeconds(3)))
                    Assert.Fail($"lab RDP target {host}:{port} did not answer.");
            }
            catch (Exception ex)
            {
                Assert.Fail($"lab RDP target {host}:{port} not reachable: {ex.GetType().Name}");
            }
        }

        /// <summary>
        /// Session tabs only. An ERROR in the application log pops the Notifications panel, and
        /// that panel is a TabItem too — counting it once made "the tab closed" wait forever on
        /// a run whose only problem was a logged error.
        /// </summary>
        private int TabCount() =>
            SessionTabs().Length;

        private AutomationElement[] SessionTabs() =>
            MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
                      .Where(t =>
                      {
                          try { return (t.Name ?? "").Contains(ConnectionName, StringComparison.OrdinalIgnoreCase); }
                          catch (Exception) { return false; }
                      })
                      .ToArray();

        private AutomationElement Row(string name)
        {
            AutomationElement tree = UiWait.FindRequired(
                MainWindow, cf => cf.ByAutomationId("ConnectionTree"), "connection tree");
            return tree.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                       .First(e =>
                       {
                           try { return (e.Name ?? "").Contains(name, StringComparison.OrdinalIgnoreCase); }
                           catch (Exception) { return false; }
                       });
        }

        /// <summary>How many times the application has logged <paramref name="phrase"/>.</summary>
        private int CountInLog(string phrase)
        {
            string? log = Deployment.ReadAppLog();
            if (log is null)
                return 0;

            int count = 0;
            int at = log.IndexOf(phrase, StringComparison.Ordinal);
            while (at >= 0)
            {
                count++;
                at = log.IndexOf(phrase, at + phrase.Length, StringComparison.Ordinal);
            }

            return count;
        }

        /// <summary>The disconnected placeholder a closed session leaves behind on defaults.</summary>
        private bool HasReconnectButton() =>
            MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                      .Any(b =>
                      {
                          try { return string.Equals(b.Name, "Connect", StringComparison.Ordinal); }
                          catch (Exception) { return false; }
                      });

        private sealed record Sample(DateTimeOffset At, long PrivateBytes, int Handles, uint GdiObjects);

        [DllImport("user32.dll")]
        private static extern uint GetGuiResources(IntPtr process, uint flags);

        private Sample ReadSample()
        {
            using Process process = Process.GetProcessById(Driver.Application.ProcessId);
            uint gdi = GetGuiResources(process.Handle, 0);
            Assert.That(gdi, Is.GreaterThan(0u), "GDI measurement unavailable for the running GUI process");
            return new(DateTimeOffset.UtcNow, process.PrivateMemorySize64, process.HandleCount, gdi);
        }

        private void CloseSession(bool closePanel)
        {
            if (closePanel)
            {
                // Outer document tab, not the nested RDP session tab.
                AutomationElement panel = MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
                    .Single(t => string.Equals(t.Name, "General", StringComparison.Ordinal));
                Win32Mouse.MiddleClick(panel);
            }
            else
                Win32Mouse.MiddleClick(SessionTabs().First());
            AnswerExpectedPrompts(TimeSpan.FromSeconds(5));
            UiWait.Until(() => TabCount() == 0 || HasReconnectButton(), "session close", TimeSpan.FromSeconds(45));
            AutomationElement? placeholder = SessionTabs().FirstOrDefault();
            if (placeholder != null)
            {
                Win32Mouse.MiddleClick(placeholder);
                UiWait.Until(() => TabCount() == 0, "placeholder close", TimeSpan.FromSeconds(30));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        [Issues("#182")]
        public void RepeatedWindowsSessionsStayWithinRetentionBudget(bool closePanel)
        {
            SkipUnlessReachable(LabTargets.WindowsTargetHost, LabTargets.Rdp);
            Assert.That(LabTargets.WindowsPassword, Is.Not.Empty, "lab Windows credential unavailable");
            string evidence = Path.Combine(AppContext.BaseDirectory, "_uiscenarios", "_evidence",
                $"rdp-memory-keep-{KeepTabsOpenAfterDisconnect}-panel-{closePanel}");
            Directory.CreateDirectory(evidence);
            List<Sample> idle = [], open = [], closed = [];
            int logins = 0;
            string verdict = "incomplete";
            try
            {
                // A fixed idle control. Excessive noise makes the run inconclusive, not more permissive.
                for (int i = 0; i < 6; i++)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    idle.Add(ReadSample());
                }
                for (int cycle = 1; cycle <= Cycles; cycle++)
                {
                    Win32Mouse.DoubleClick(Row(ConnectionName));
                    AnswerExpectedPrompts(TimeSpan.FromSeconds(10));
                    int expected = cycle;
                    UiWait.Until(() => CountInLog("phase=login_complete") >= expected,
                        "Windows RDP login completion", TimeSpan.FromSeconds(90));
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                    Assert.That(TabCount(), Is.EqualTo(1), "session vanished before measurement");
                    Assert.That(CountInLog("closed by user"), Is.EqualTo(cycle - 1),
                        "target disconnected before the close under test");
                    logins++;
                    string screenshot = Path.Combine(evidence, $"desktop-{cycle:00}.png");
                    FlaUI.Core.Capturing.Capture.Element(MainWindow).ToFile(screenshot);
                    open.Add(ReadSample());
                    CloseSession(closePanel);
                    Thread.Sleep(TimeSpan.FromSeconds(RetentionAssessment.RequiredSettleSeconds));
                    closed.Add(ReadSample());
                    TestContext.Out.WriteLine($"cycle {cycle}: {JsonSerializer.Serialize(closed[^1])}");
                    File.WriteAllText(Path.Combine(evidence, "progress.json"), JsonSerializer.Serialize(closed));
                }
                var memory = RetentionAssessment.Assess(idle.Select(x => x.PrivateBytes).ToArray(),
                    closed.Select(x => x.PrivateBytes).ToArray(), RetentionAssessment.PrivateBytesBudget, logins, 30);
                var handles = RetentionAssessment.Assess(idle.Select(x => (long)x.Handles).ToArray(),
                    closed.Select(x => (long)x.Handles).ToArray(), RetentionAssessment.HandleBudget, logins, 30);
                var gdi = RetentionAssessment.Assess(idle.Select(x => (long)x.GdiObjects).ToArray(),
                    closed.Select(x => (long)x.GdiObjects).ToArray(), RetentionAssessment.GdiBudget, logins, 30);
                bool withinBudget = new[] { memory, handles, gdi }.All(x => string.Equals(x.Verdict, "within-budget", StringComparison.Ordinal));
                verdict = "requires-investigation";
                TestContext.Out.WriteLine(JsonSerializer.Serialize(new { memory, handles, gdi }));
                Assert.That(withinBudget, Is.True, "retention grew or the control was inconclusive");
                AssertExitedCleanly(CloseApplicationAndWaitForExit(TimeSpan.FromSeconds(45), true), "after 14 Windows sessions");
                verdict = "within-budget";
            }
            finally
            {
                string appDll = Path.Combine(Deployment.Directory, "mRemoteNG.dll");
                File.WriteAllText(Path.Combine(evidence, "receipt.json"), JsonSerializer.Serialize(new
                {
                    schema = 1, verdict, keepTabs = KeepTabsOpenAfterDisconnect, closePanel,
                    completedLogins = logins, settleSeconds = RetentionAssessment.RequiredSettleSeconds,
                    appSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(appDll))),
                    os = Environment.OSVersion.ToString(), idle, open, closed,
                    limitations = "Finite Windows lab run; screenshots require inspection; not proof of leak freedom."
                }, EvidenceJson));
                File.WriteAllText(Path.Combine(evidence, "application.log"), Deployment.ReadAppLog());
            }
        }

        /// <summary>Opens the seeded RDP connection and waits until MSTSC is really connected.</summary>
        private void OpenSessionAndWaitUntilEstablished()
        {
            Win32Mouse.DoubleClick(Row(ConnectionName));
            UiWait.Settle(MainWindow);
            AnswerExpectedPrompts(TimeSpan.FromSeconds(20));
            UiWait.Until(() => TabCount() > 0, "an RDP session tab to open", TimeSpan.FromSeconds(60));
            UiWait.Until(() => CountInLog("established by user") >= 1,
                         "the RDP session to actually connect", TimeSpan.FromSeconds(90));
            AnswerExpectedPrompts(TimeSpan.FromSeconds(5));
            Thread.Sleep(TimeSpan.FromSeconds(4));

            // The first run of these scenarios passed against a target that dropped every session
            // half a second after logon ("An internal error has occurred"): by the time the main
            // window was closed there was no session left to close, so "the process exited" proved
            // nothing about the path under test. A session that is gone before the step being
            // tested makes the run inconclusive, not green.
            if (CountInLog("Protocol Event Disconnected") > 0 || TabCount() == 0)
            {
                Assert.Fail("the RDP target ended the session on its own before the close under test, "
                              + "so this run cannot say anything about closing a live session");
            }
        }

        /// <summary>
        /// Writes the close-path trace (the existing [#182] cleanup line and the [#182-diag]
        /// instrumentation) to the test output, so a run that hangs shows which step never returned.
        /// </summary>
        private void WriteCloseTrace(string context)
        {
            string? log = Deployment.ReadAppLog();
            string[] lines = (log ?? "").Split('\n')
                                        .Where(l => l.Contains("[#182", StringComparison.Ordinal))
                                        .Select(l => l.TrimEnd('\r'))
                                        .ToArray();
            TestContext.Out.WriteLine($"{context}: {lines.Length} close-path line(s)");
            foreach (string line in lines)
                TestContext.Out.WriteLine("  " + line);
        }

        /// <summary>
        /// The second #182 regression: after the memory fix the reporter closed the main window and
        /// mRemoteNG.exe stayed in Task Manager. Their log shows the tab was closed first, so this
        /// scenario closes the main window with the session still open — the path the memory fix
        /// changed most, and the one their log did not cover.
        /// </summary>
        [Test]
        [Issues("#182")]
        public void ClosingTheMainWindowWithAnRdpSessionOpenEndsTheProcess()
        {
            SkipUnlessReachable(LabTargets.WindowsTargetHost, LabTargets.Rdp);
            Assert.That(LabTargets.WindowsPassword, Is.Not.Empty, "MRNG_LAB_WINDOWS_PASSWORD is not visible to the battery process");

            OpenSessionAndWaitUntilEstablished();

            bool exited = CloseApplicationAndWaitForExit(TimeSpan.FromSeconds(45), clickCloseButton: true);
            WriteCloseTrace("main window closed with the session open");
            AssertExitedCleanly(exited, "closing the main window with an RDP session open");
        }

        /// <summary>
        /// The reporter's own sequence from their log: close the session's tab, wait, then close the
        /// main window. Their run never logged the end of the process.
        /// </summary>
        [Test]
        [Issues("#182")]
        public void ClosingTheTabThenTheMainWindowEndsTheProcess()
        {
            SkipUnlessReachable(LabTargets.WindowsTargetHost, LabTargets.Rdp);
            Assert.That(LabTargets.WindowsPassword, Is.Not.Empty, "MRNG_LAB_WINDOWS_PASSWORD is not visible to the battery process");

            OpenSessionAndWaitUntilEstablished();

            Win32Mouse.MiddleClick(SessionTabs().First());
            AnswerExpectedPrompts(TimeSpan.FromSeconds(10));
            UiWait.Until(() => TabCount() == 0 || HasReconnectButton(),
                         "the session to close", TimeSpan.FromSeconds(45));
            AutomationElement? placeholder = SessionTabs().FirstOrDefault();
            if (placeholder != null)
            {
                try { Win32Mouse.MiddleClick(placeholder); }
                catch (Exception ex) { TestContext.Out.WriteLine($"placeholder click skipped: {ex.GetType().Name}"); }
                AnswerExpectedPrompts(TimeSpan.FromSeconds(10));
            }

            // The reporter's log shows about ten seconds between the tab closing and the shutdown.
            Thread.Sleep(TimeSpan.FromSeconds(10));

            bool exited = CloseApplicationAndWaitForExit(TimeSpan.FromSeconds(45), clickCloseButton: true);
            WriteCloseTrace("tab closed, then main window closed");
            AssertExitedCleanly(exited, "closing the main window after closing the RDP tab");
        }
    }

    /// <summary>
    /// Repeat explicit tab/panel closure with retained tabs enabled. This does not establish
    /// the reporter's preference or cover disconnect-only reconnect placeholders.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class RdpSessionMemoryWithPlaceholderTabAcceptanceTests : RdpSessionMemoryAcceptanceTests
    {
        protected override bool KeepTabsOpenAfterDisconnect => true;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using mRemoteNG.Connection.Protocol;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// #182: opening an RDP session adds roughly 300 MB to the process and closing it gives
    /// nothing back, so eight sessions left about 2 GB held with every tab and panel shut.
    ///
    /// The unit suite proves the mechanism — the close path skipped the only call that disposes
    /// the protocol, and disposing the protocol is what releases the MSTSC ActiveX object. It
    /// cannot prove the consequence, because it never loads MSTSC. This does: a real session
    /// against the lab's RDP target, opened and closed several times, watching what the process
    /// actually holds.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class RdpSessionMemoryAcceptanceTests : UiAcceptanceTestBase
    {
        private const string ConnectionName = "lab-linux-rdp";
        private const int Cycles = 3;

        /// <summary>
        /// Below this, a "session" never loaded a desktop and the run proves nothing. An xrdp
        /// session is far smaller than the reporter's Windows ones, so this is not the 300 MB
        /// class — it is the floor for "MSTSC really connected and painted something".
        /// </summary>
        private const long MinimumPlausibleSessionMb = 20;

        /// <summary>
        /// Whether a closed session leaves a reconnect placeholder tab behind, which is the
        /// application's default and therefore the reporter's path. The derived fixture below
        /// runs the same measurement with it on; this one turns it off so a cycle ends with no
        /// tab at all.
        /// </summary>
        protected virtual bool KeepTabsOpenAfterDisconnect => false;

        protected override void SeedSettings()
        {
            // The Linux host, not the Windows lab target. The Windows target refuses every NLA
            // logon because it was cloned from the client guest's disk image and the two share a
            // machine SID; CredSSP resolves "<target>\Administrator" to the client's own account
            // and LSA rejects the logon (4776 success, 4625 0xc000006d sub-status 0). That is a
            // lab-provisioning defect, not something a connection setting can route around, and
            // for what this scenario measures — a live MSTSC session established, closed, and its
            // allocation not accumulating — any real RDP server does.
            //
            // CredSSP and certificate checking stay at their defaults; the self-signed prompt is
            // answered by AnswerExpectedPrompts like every other RDP scenario here.
            ConnectionsSeeder seeder = new();
            seeder.Add(ConnectionName, LabTargets.LinuxHost, ProtocolType.RDP, LabTargets.Rdp,
                       LabTargets.LinuxUser, LabTargets.LinuxPassword);
            Deployment.WriteConnectionsFile(seeder.Build());

            // The reporter closes tabs and panels and expects the memory back, so the tab has to
            // actually go. On defaults it does not: KeepTabsOpenAfterDisconnect leaves a reconnect
            // placeholder behind (#61, #139); with it on, the cycle closes that placeholder too.
            Deployment.WriteSettings(new Dictionary<string, string>
            {
                ["KeepTabsOpenAfterDisconnect"] = KeepTabsOpenAfterDisconnect ? "True" : "False",
                ["ConfirmCloseConnection"] = "1", // ConfirmCloseEnum.Never — nothing to answer per cycle
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

        /// <summary>
        /// Private bytes, after letting the session finish drawing and the runtime hand memory
        /// back. "Connected" is logged before the desktop bitmap is fully up, so a measurement
        /// taken the instant the log line appears still catches the session mid-allocation.
        /// </summary>
        private long PrivateMemoryMb()
        {
            UiWait.Settle(MainWindow);
            System.Threading.Thread.Sleep(4000);
            using System.Diagnostics.Process process =
                System.Diagnostics.Process.GetProcessById(Driver.Application.ProcessId);
            return process.PrivateMemorySize64 / (1024 * 1024);
        }

        [Test]
        [Issues("#182")]
        public void OpeningAndClosingRdpSessionsGivesTheMemoryBack()
        {
            SkipUnlessReachable(LabTargets.LinuxHost, LabTargets.Rdp);

            // The shape of the credential this run will present, never its value. A logon that
            // fails with a password known to be correct is usually a credential that never
            // arrived: the battery reads it from a machine environment variable, and a process
            // started before that variable existed sees nothing at all.
            TestContext.Out.WriteLine(
                $"credential: user '{LabTargets.LinuxUser}' password length {LabTargets.LinuxPassword.Length}");
            Assert.That(LabTargets.LinuxPassword, Is.Not.Empty,
                        "MRNG_LAB_LINUX_PASSWORD is not visible to the battery process, so every "
                        + "logon would fail no matter what the target is configured to accept");

            long baseline = PrivateMemoryMb();
            TestContext.Out.WriteLine($"baseline before any session: {baseline} MB");

            List<long> whileOpen = [];
            List<long> afterEachClose = [];

            for (int cycle = 1; cycle <= Cycles; cycle++)
            {
                Win32Mouse.DoubleClick(Row(ConnectionName));
                UiWait.Settle(MainWindow);
                AnswerExpectedPrompts(TimeSpan.FromSeconds(20));
                UiWait.Until(() => TabCount() > 0, "an RDP session tab to open", TimeSpan.FromSeconds(60));

                // A tab appears the moment the attempt starts, long before MSTSC has authenticated
                // and rendered a desktop — which is the memory this issue is about. Measuring on
                // the tab alone is what made the first run of this scenario report 8 MB a session
                // and pass without proving anything. The application says when it is really
                // connected, so wait for it to say so.
                int established = cycle;
                UiWait.Until(() => CountInLog("established by user") >= established,
                             "the RDP session to actually connect", TimeSpan.FromSeconds(90));
                AnswerExpectedPrompts(TimeSpan.FromSeconds(5));

                // "Established" is the protocol handshake; the desktop, and the memory a session
                // is really made of, arrives over the next seconds. Measured on the handshake
                // alone a session once cost 13 MB and the run declared itself inconclusive.
                Thread.Sleep(TimeSpan.FromSeconds(4));

                long open = PrivateMemoryMb();
                whileOpen.Add(open);

                Win32Mouse.MiddleClick(SessionTabs().First());
                AnswerExpectedPrompts(TimeSpan.FromSeconds(10));

                // With KeepTabsOpenAfterDisconnect off the tab goes; on, a reconnect placeholder is
                // left behind (#61, #139) and is closed the way the reporter closes it, so every
                // cycle ends with no tab either way. The first run of this scenario timed out
                // waiting for zero tabs on the default setting.
                UiWait.Until(() => TabCount() == 0 || HasReconnectButton(),
                             "the session to close, leaving either no tab or a disconnected placeholder",
                             TimeSpan.FromSeconds(45));
                // A tab that is still on its way out can vanish between the count and the click,
                // so the placeholder is looked up once and only clicked if it is really there.
                AutomationElement? placeholder = SessionTabs().FirstOrDefault();
                if (placeholder != null)
                {
                    TestContext.Out.WriteLine($"cycle {cycle}: closing the reconnect placeholder tab");
                    try { Win32Mouse.MiddleClick(placeholder); }
                    catch (Exception ex) { TestContext.Out.WriteLine($"placeholder click skipped: {ex.GetType().Name}"); }
                    AnswerExpectedPrompts(TimeSpan.FromSeconds(10));
                    UiWait.Until(() => TabCount() == 0, "the placeholder tab to close", TimeSpan.FromSeconds(30));
                }

                long closed = PrivateMemoryMb();
                afterEachClose.Add(closed);
                TestContext.Out.WriteLine($"cycle {cycle}: open {open} MB, after close {closed} MB");
            }

            long oneSessionCost = Math.Max(whileOpen[0] - baseline, 1);

            // A session that costs a few MB never loaded a desktop -- the tab opened, the client
            // did not connect and render -- and then the measurement is vacuous: it cannot tell a
            // fixed build from a leaking one, because nothing was allocated to leak. Say so
            // instead of passing; the first run of this scenario did exactly that at 8 MB.
            if (oneSessionCost < MinimumPlausibleSessionMb)
            {
                Assert.Ignore($"an RDP session cost only {oneSessionCost} MB here, so the client never "
                              + "loaded a real session and this measurement proves nothing either way.");
            }

            // The reporter's signal is ACCUMULATION: each session added its own few hundred MB and
            // kept them, so eight sessions cost 2 GB. "Back to baseline" is the wrong thing to
            // demand -- a first connection leaves JIT and caches behind for good reasons, and
            // native heaps often keep pages after a correct release -- so the assertion is on the
            // slope: the second and later sessions must not each add another session-sized
            // chunk. The retained delta between consecutive closes is what a leak makes grow.
            long retainedAfterFirst = afterEachClose[0] - baseline;
            long addedByLaterSessions = afterEachClose[^1] - afterEachClose[0];
            long perLaterSession = addedByLaterSessions / Math.Max(Cycles - 1, 1);
            TestContext.Out.WriteLine(
                $"one session costs about {oneSessionCost} MB; first close retained {retainedAfterFirst} MB; "
                + $"the next {Cycles - 1} session(s) added {addedByLaterSessions} MB in total, "
                + $"{perLaterSession} MB each");

            Assert.That(perLaterSession, Is.LessThan(oneSessionCost / 2),
                        $"every session after the first is adding about {perLaterSession} MB that stays, "
                        + $"against a session cost of {oneSessionCost} MB -- closed sessions are being kept");
        }
    }

    /// <summary>
    /// The same measurement on the application's default: a closed session leaves a reconnect
    /// placeholder tab, and the placeholder is then closed. That is how the reporter's sessions
    /// end, and the first #182 rounds only ever measured the other setting.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class RdpSessionMemoryWithPlaceholderTabAcceptanceTests : RdpSessionMemoryAcceptanceTests
    {
        protected override bool KeepTabsOpenAfterDisconnect => true;
    }
}

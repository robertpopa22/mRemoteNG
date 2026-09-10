using System;
using System.Collections.Generic;
using System.Linq;
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
        private const string ConnectionName = "lab-win-rdp";
        private const int Cycles = 4;

        protected override void SeedSettings()
        {
            ConnectionsSeeder seeder = new();
            seeder.Add(ConnectionName, LabTargets.WindowsTargetHost, ProtocolType.RDP, LabTargets.Rdp,
                       LabTargets.WindowsUser, LabTargets.WindowsPassword);
            Deployment.WriteConnectionsFile(seeder.Build());

            // The reporter closes tabs and panels and expects the memory back, so the tab has to
            // actually go. On defaults it does not: KeepTabsOpenAfterDisconnect leaves a reconnect
            // placeholder behind (#61, #139), which makes each cycle ambiguous and the count never
            // return to zero.
            Deployment.WriteSettings(new Dictionary<string, string>
            {
                ["KeepTabsOpenAfterDisconnect"] = "False",
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

        private int TabCount() =>
            MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem)).Length;

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

        /// <summary>The disconnected placeholder a closed session leaves behind on defaults.</summary>
        private bool HasReconnectButton() =>
            MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                      .Any(b =>
                      {
                          try { return string.Equals(b.Name, "Connect", StringComparison.Ordinal); }
                          catch (Exception) { return false; }
                      });

        /// <summary>Private bytes, after giving the runtime a chance to hand memory back.</summary>
        private long PrivateMemoryMb()
        {
            UiWait.Settle(MainWindow);
            System.Threading.Thread.Sleep(1500);
            using System.Diagnostics.Process process =
                System.Diagnostics.Process.GetProcessById(Driver.Application.ProcessId);
            return process.PrivateMemorySize64 / (1024 * 1024);
        }

        [Test]
        [Issues("#182")]
        public void OpeningAndClosingRdpSessionsGivesTheMemoryBack()
        {
            SkipUnlessReachable(LabTargets.WindowsTargetHost, LabTargets.Rdp);

            long baseline = PrivateMemoryMb();
            TestContext.Out.WriteLine($"baseline before any session: {baseline} MB");

            long firstSessionPeak = 0;
            List<long> afterEachClose = [];

            for (int cycle = 1; cycle <= Cycles; cycle++)
            {
                Win32Mouse.DoubleClick(Row(ConnectionName));
                UiWait.Settle(MainWindow);
                AnswerExpectedPrompts(TimeSpan.FromSeconds(20));
                UiWait.Until(() => TabCount() > 0, "an RDP session tab to open", TimeSpan.FromSeconds(60));

                long open = PrivateMemoryMb();
                if (cycle == 1)
                    firstSessionPeak = open;

                AutomationElement tab = MainWindow
                    .FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
                    .First();
                Win32Mouse.MiddleClick(tab);
                AnswerExpectedPrompts(TimeSpan.FromSeconds(10));

                // The tab really goes, because SeedSettings turned KeepTabsOpenAfterDisconnect off.
                // Left on, it leaves a reconnect placeholder behind (#61, #139) and the count never
                // returns to zero — which is what the first run of this scenario timed out on.
                UiWait.Until(() => TabCount() == 0 || HasReconnectButton(),
                             "the session to close, leaving either no tab or a disconnected placeholder",
                             TimeSpan.FromSeconds(45));

                long closed = PrivateMemoryMb();
                afterEachClose.Add(closed);
                TestContext.Out.WriteLine($"cycle {cycle}: open {open} MB, after close {closed} MB");
            }

            long oneSessionCost = Math.Max(firstSessionPeak - baseline, 1);
            long growth = afterEachClose[^1] - baseline;

            // A real RDP session loads the MSTSC ActiveX control and a desktop bitmap; the report
            // puts that at roughly 300 MB. A session that costs a few MB never got that far -- the
            // tab opened but the client did not connect and render -- and then the whole
            // measurement is vacuous: it cannot distinguish a fixed build from a leaking one,
            // because nothing was ever allocated to leak. Say so instead of passing. The first run
            // of this scenario after the fix landed here, at 8 MB a session.
            if (oneSessionCost < 50)
            {
                Assert.Ignore($"an RDP session cost only {oneSessionCost} MB here, so the client never "
                              + "loaded a real session and this measurement proves nothing either way. "
                              + "The lab RDP target needs to authenticate and render before this scenario "
                              + "can say anything about #182.");
            }
            TestContext.Out.WriteLine($"one session costs about {oneSessionCost} MB; "
                                      + $"after {Cycles} opened and closed, {growth} MB above baseline");

            // If nothing is released, growth lands near Cycles * oneSessionCost -- which is the
            // reporter's 2 GB after eight sessions. Allowing one session's worth of slack covers
            // the caches and JIT a first connection leaves behind for good reasons; anything past
            // two means sessions are accumulating.
            long ceiling = oneSessionCost * 2;
            Assert.That(growth, Is.LessThan(ceiling),
                        $"after {Cycles} RDP sessions were opened and closed the process is still {growth} MB "
                        + $"above where it started, and one session costs about {oneSessionCost} MB -- closed "
                        + "sessions are being kept");
        }
    }
}

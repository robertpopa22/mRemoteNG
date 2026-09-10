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
                UiWait.Until(() => TabCount() == 0, "the session tab to close", TimeSpan.FromSeconds(45));

                long closed = PrivateMemoryMb();
                afterEachClose.Add(closed);
                TestContext.Out.WriteLine($"cycle {cycle}: open {open} MB, after close {closed} MB");
            }

            long oneSessionCost = Math.Max(firstSessionPeak - baseline, 1);
            long growth = afterEachClose[^1] - baseline;
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using mRemoteNG.Connection.Protocol;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// The application's own link probe, against a real Windows RDP session that reaches
    /// OnLoginComplete. The unit tests prove the policy; this proves the probe starts after the
    /// logon, keeps sampling while the session is open, and that the summary written when the tab
    /// closes counts those samples. Assertions use numbers and field names only: no host or address.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class RdpLinkProbeAcceptanceTests : UiAcceptanceTestBase
    {
        private const string ConnectionName = "lab-windows-rdp";

        // ICMP answered: one probe every 15 s from the logon, so 200 s gives about thirteen samples.
        // ICMP filtered: three unanswered pings (45 s) switch to a TCP connect at 60 s cadence, so
        // the first line appears 45-60 s after logon and the next two at +60 s and +120 s, so the hold
        // must reach about 170 s; 150 s measured only two lines. 200 s leaves a margin.
        private static readonly TimeSpan SessionHold = TimeSpan.FromSeconds(200);

        private string _evidenceDir = null!;

        protected override void SeedSettings()
        {
            ConnectionsSeeder seeder = new();
            // The Windows target: only it raises OnLoginComplete, which starts the probe. Domain is
            // the target's own name, or the client qualifies the user with its own hostname.
            seeder.Add(ConnectionName, LabTargets.WindowsTargetHost, ProtocolType.RDP, LabTargets.Rdp,
                       LabTargets.WindowsUser, LabTargets.WindowsPassword, LabTargets.WindowsTargetName);
            Deployment.WriteConnectionsFile(seeder.Build());
            Deployment.WriteSettings(new Dictionary<string, string>
            {
                ["ConfirmCloseConnection"] = "1",
            });

            // Outside the scenario directory, which is deleted on success.
            _evidenceDir = Path.Combine(AppContext.BaseDirectory, "_uiscenarios", "_evidence",
                                        TestContext.CurrentContext.Test.MethodName ?? "link-probe");
            Directory.CreateDirectory(_evidenceDir);
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
                Assert.Ignore($"lab RDP target {host}:{port} is not reachable: {ex.GetType().Name}");
            }
        }

        private int TabCount() =>
            MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
                      .Count(t =>
                      {
                          try { return (t.Name ?? "").Contains(ConnectionName, StringComparison.OrdinalIgnoreCase); }
                          catch (Exception) { return false; }
                      });

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

        private AutomationElement SessionTab() =>
            MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
                      .First(t =>
                      {
                          try { return (t.Name ?? "").Contains(ConnectionName, StringComparison.OrdinalIgnoreCase); }
                          catch (Exception) { return false; }
                      });

        private int CountInLog(string phrase)
        {
            string? log = Deployment.ReadAppLog();
            if (log is null) return 0;
            int count = 0, at = log.IndexOf(phrase, StringComparison.Ordinal);
            while (at >= 0)
            {
                count++;
                at = log.IndexOf(phrase, at + phrase.Length, StringComparison.Ordinal);
            }
            return count;
        }

        private static string[] Lines(string log, string eventName) =>
            log.Split('\n')
               .Where(l => l.Contains("event=" + eventName + " ", StringComparison.Ordinal))
               .Select(l => l.TrimEnd('\r'))
               .ToArray();

        [Test]
        public void ALongRdpSessionProducesLinkProbeSamples()
        {
            SkipUnlessReachable(LabTargets.WindowsTargetHost, LabTargets.Rdp);
            Assert.That(LabTargets.WindowsPassword, Is.Not.Empty,
                        "MRNG_LAB_WINDOWS_PASSWORD is not visible to the battery process");

            Win32Mouse.DoubleClick(Row(ConnectionName));
            UiWait.Settle(MainWindow);
            AnswerExpectedPrompts(TimeSpan.FromSeconds(20));
            UiWait.Until(() => CountInLog("established by user") >= 1, "the RDP session to connect",
                         TimeSpan.FromSeconds(90));
            AnswerExpectedPrompts(TimeSpan.FromSeconds(5));
            UiWait.Until(() => CountInLog("phase=login_complete") >= 1, "the RDP logon to complete",
                         TimeSpan.FromSeconds(90));

            // Hold the session open, checking that it is still there; a target that replaces the
            // session leaves nothing to probe, and that is a provisioning result, not a pass or fail.
            DateTime until = DateTime.UtcNow + SessionHold;
            while (DateTime.UtcNow < until)
            {
                Thread.Sleep(TimeSpan.FromSeconds(10));
                if (CountInLog("closed by user") >= 1 || TabCount() == 0)
                    Assert.Inconclusive("the lab RDP session dropped before the hold elapsed "
                                        + "(target replaced the session); no long session to measure.");
            }

            // Close the way a user does: middle-click the tab, answer the disconnect prompt. The
            // summary is written when the connection ends.
            Win32Mouse.MiddleClick(SessionTab());
            UiWait.Settle(MainWindow);
            AnswerExpectedPrompts(TimeSpan.FromSeconds(8));
            UiWait.Until(() => TabCount() == 0, "the session tab to close", TimeSpan.FromSeconds(30));
            AssertNoCrash("after closing the long RDP session");

            bool exited = CloseApplicationAndWaitForExit();

            string log = Deployment.ReadAppLog() ?? string.Empty;
            File.WriteAllText(Path.Combine(_evidenceDir, "mRemoteNG.log"), log);

            Assert.That(exited, Is.True, "the application did not exit after the session was closed");

            string[] probes = Lines(log, "rdp_link_probe")
                .Where(l => l.Contains(" samples=", StringComparison.Ordinal)).ToArray();
            string[] first = probes.Where(l => l.Contains("reason=first ", StringComparison.Ordinal)).ToArray();
            string[] later = probes.Where(l => Regex.IsMatch(l, @"reason=(interval|loss_change)\b")).ToArray();

            Assert.That(first.Any(l => Regex.IsMatch(l, @"method=(icmp|tcp)\b")), Is.True,
                        "no rdp_link_probe reason=first line with method=icmp|tcp:" + Environment.NewLine
                        + string.Join(Environment.NewLine, Lines(log, "rdp_link_probe")));
            Assert.That(later.Length, Is.GreaterThanOrEqualTo(2),
                        "fewer than two rdp_link_probe lines after the first (interval or loss_change)");

            string[] summaries = Lines(log, "rdp_network_summary");
            int maxProbeSamples = summaries
                .Select(l => Regex.Match(l, @"\bprobe_samples=(\d+)"))
                .Where(m => m.Success)
                .Select(m => int.Parse(m.Groups[1].Value))
                .DefaultIfEmpty(-1)
                .Max();
            Assert.That(maxProbeSamples, Is.GreaterThanOrEqualTo(3),
                        "no rdp_network_summary line with probe_samples>=3 (largest seen: " + maxProbeSamples + ")");

            Assert.That(Lines(log, "rdp_shape").Any(l => l.Contains("net_autodetect=false", StringComparison.Ordinal)
                                                        && Regex.IsMatch(l, @"\bnet_type=1\b")),
                        Is.True, "rdp_shape does not report net_autodetect=false net_type=1");
        }
    }
}

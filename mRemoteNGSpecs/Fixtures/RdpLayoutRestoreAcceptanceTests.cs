using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using mRemoteNG.Connection.Protocol;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// Two RDP tabs reopened from the saved dock layout on startup (#228).
    ///
    /// The reported symptom: after a restart the first restored RDP tab connected and the second
    /// waited ten seconds, then "RDP ActiveX control creation timed out after 10 seconds." The
    /// layout path opens every saved tab without switching to it, so the ActiveX control of a tab
    /// that is not the active one is created while its tab is hidden.
    ///
    /// The first start reopens the seeded connections through the tree path, which switches to
    /// each tab as it opens it; the restart reopens them through the layout path. Comparing the
    /// two starts separates "the second control is slow while the first connects" from "the
    /// second control is never created because its tab is hidden".
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class RdpLayoutRestoreAcceptanceTests : UiAcceptanceTestBase
    {
        private const string Prefix = "lab-rdprestore";
        private const string TimeoutText = "RDP ActiveX control creation timed out";

        protected override void SeedSettings()
        {
            // Two entries on the xrdp target with different user names: the initiator treats the
            // same host, port and user as one open connection (#2414). The login need not succeed;
            // xrdp keeps its login screen up, so the tab stays open until the app exits.
            ConnectionsSeeder seeder = new();
            seeder.Add($"{Prefix}-a", LabTargets.LinuxHost, ProtocolType.RDP, LabTargets.Rdp,
                       LabTargets.LinuxUser, LabTargets.LinuxPassword);
            seeder.Add($"{Prefix}-b", LabTargets.LinuxHost, ProtocolType.RDP, LabTargets.Rdp,
                       "labrestoreb", LabTargets.LinuxPassword);

            // The serializer writes Connected from the live tab count, which is zero here.
            string xml = seeder.Build().Replace("Connected=\"false\"", "Connected=\"true\"", StringComparison.Ordinal);
            Deployment.WriteConnectionsFile(xml);

            Deployment.WriteSettings(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["OpenConsFromLastSession"] = "True",
                ["ConfirmCloseConnection"] = "1", // ConfirmCloseEnum.Never: the restart closes live sessions
                ["KeepTabsOpenAfterDisconnect"] = "True",
                ["CheckForUpdatesAsked"] = "True",
            });
        }

        private static string SafeName(AutomationElement e)
        {
            try { return e.Name; } catch (Exception) { return ""; }
        }

        private int RdpTabCount() =>
            MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
                      .Count(t => SafeName(t).Contains(Prefix, StringComparison.OrdinalIgnoreCase));

        /// <summary>The log lines written by the newest process only; the file is append-only.</summary>
        private string CurrentProcessLog()
        {
            string log = Deployment.ReadAppLog() ?? "";
            int start = log.LastIndexOf("event=process_start", StringComparison.Ordinal);
            return start < 0 ? log : log[start..];
        }

        private static int Count(string text, string pattern) =>
            Regex.Count(text, pattern, RegexOptions.None, TimeSpan.FromSeconds(5));

        /// <summary>
        /// Waits until both RDP sessions of this start reached "initialized", or one of them hit
        /// the creation timeout, and returns this start's log.
        /// </summary>
        private string WaitForBothControls(string start)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(45);
            string log = "";
            while (DateTime.UtcNow < deadline)
            {
                log = CurrentProcessLog();
                if (Count(log, @"event=rdp_phase [^\r\n]*phase=initialized ") >= 2 ||
                    log.Contains(TimeoutText, StringComparison.Ordinal))
                    break;
                System.Threading.Thread.Sleep(1000);
            }

            TestContext.Out.WriteLine(
                $"{start}: rdp tabs={RdpTabCount()} initialized={Count(log, @"event=rdp_phase [^\r\n]*phase=initialized ")} " +
                $"initialize_failed={Count(log, "phase=initialize_failed")} timeout={log.Contains(TimeoutText, StringComparison.Ordinal)}");
            foreach (string line in log.Split('\n').Where(l => l.Contains("rdp_open ", StringComparison.Ordinal) ||
                                                               l.Contains(TimeoutText, StringComparison.Ordinal)))
                TestContext.Out.WriteLine("  " + line.Trim());
            return log;
        }

        [Test]
        [Issues("#228")]
        public void BothRdpTabsRestoredFromTheLayoutCreateTheirControls()
        {
            if (!LabTargets.IsReachable(LabTargets.LinuxHost, LabTargets.Rdp))
                Assert.Ignore($"lab RDP target not reachable at {LabTargets.Describe(LabTargets.LinuxHost, LabTargets.Rdp)}.");

            string first = WaitForBothControls("first start (tree path)");
            Assert.That(first, Does.Not.Contain(TimeoutText),
                        "a control timed out on the first start, where each tab is switched to as it opens");
            UiWait.Until(() => RdpTabCount() >= 2, "both RDP tabs on the first start", TimeSpan.FromSeconds(30));
            AssertNoCrash("first start");

            // The second session's certificate prompt can arrive ~30 s after start; a close sent
            // while it is up is swallowed by that modal. Settle both sessions first.
            DateTime settle = DateTime.UtcNow.AddSeconds(60);
            while (DateTime.UtcNow < settle &&
                   Count(CurrentProcessLog(), @"event=rdp_phase [^\r\n]*phase=connected ") < 2)
                AnswerExpectedPrompts(TimeSpan.FromSeconds(2));
            AnswerExpectedPrompts(TimeSpan.FromSeconds(2));

            // A live RDP session takes Alt+F4 into the remote desktop, so close from the title bar.
            RestartApplication(clickCloseButton: true);

            string second = WaitForBothControls("restart (layout path)");
            Assert.That(second, Does.Not.Contain(TimeoutText),
                        "a tab reopened from the saved layout never got its RDP control (#228)");
            Assert.That(Count(second, @"event=rdp_phase [^\r\n]*phase=initialized "), Is.GreaterThanOrEqualTo(2),
                        "fewer than two RDP sessions reached 'initialized' after the restart");
            AnswerExpectedPrompts(TimeSpan.FromSeconds(3));
            AssertNoCrash("after the restart");
        }
    }
}

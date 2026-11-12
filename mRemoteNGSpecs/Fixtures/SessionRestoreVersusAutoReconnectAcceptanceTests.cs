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
    /// "Reconnect at startup" with the automatic reconnect checkbox unticked (upstream #3178).
    ///
    /// Options, Advanced has two checkboxes about a dropped RDP session. The second one,
    /// "Automatically try to reconnect when disconnected from server", is saved inverted as
    /// NoReconnect. The startup code read that same value as "do not reopen the previous
    /// sessions", so unticking a checkbox about dropped sessions also switched off the restore.
    ///
    /// The seed is the state such a user has: restore on, the reconnect dialog on, automatic
    /// reconnect unticked, and two connections flagged Connected as a file saved with both open.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class SessionRestoreVersusAutoReconnectAcceptanceTests : UiAcceptanceTestBase
    {
        private const string Prefix = "lab-norc";
        private const string SecondUser = "labnorcb";

        protected override void SeedSettings()
        {
            ConnectionsSeeder seeder = new();
            seeder.Add($"{Prefix}-a", LabTargets.LinuxHost, ProtocolType.SSH2, LabTargets.Ssh,
                       LabTargets.LinuxUser, LabTargets.LinuxPassword);
            // A second user name: the initiator treats the same host, port and user as one open
            // connection (#2414). The login need not succeed; the tab stays while PuTTY asks again.
            seeder.Add($"{Prefix}-b", LabTargets.LinuxHost, ProtocolType.SSH2, LabTargets.Ssh,
                       SecondUser, LabTargets.LinuxPassword);

            // The serializer writes Connected from the live tab count, which is zero here.
            string xml = seeder.Build().Replace("Connected=\"false\"", "Connected=\"true\"", StringComparison.Ordinal);
            Deployment.WriteConnectionsFile(xml);

            Deployment.WriteSettings(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["OpenConsFromLastSession"] = "True",
                ["ReconnectOnDisconnect"] = "True",
                ["NoReconnect"] = "True",
                ["ConfirmCloseConnection"] = "0",
                ["KeepTabsOpenAfterDisconnect"] = "False",
            });
        }

        private static string SafeName(AutomationElement e)
        {
            try { return e.Name; } catch (Exception) { return ""; }
        }

        private AutomationElement[] SessionTabs() =>
            MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
                      .Where(t => LabTargets.IsLinuxSshTab(SafeName(t), Prefix)
                                  || SafeName(t).Contains(LabTargets.LinuxHost, StringComparison.OrdinalIgnoreCase)
                                  || SafeName(t).StartsWith(SecondUser + "@", StringComparison.OrdinalIgnoreCase))
                      .ToArray();

        [Test]
        [Touches("upstream#3178")]
        public void PreviousSessionsComeBackWhenAutomaticReconnectIsUnticked()
        {
            if (!LabTargets.IsReachable(LabTargets.LinuxHost, LabTargets.Ssh))
                Assert.Ignore($"lab SSH target not reachable at {LabTargets.Describe(LabTargets.LinuxHost, LabTargets.Ssh)}.");

            // Each restored PuTTY session can raise its own host-key prompt, and the next one does
            // not appear until the previous is answered, so answer while waiting rather than after.
            DateTime deadline = DateTime.UtcNow.AddSeconds(60);
            while (SessionTabs().Length < 2 && DateTime.UtcNow < deadline)
                AnswerExpectedPrompts(TimeSpan.FromSeconds(3));

            int restored = SessionTabs().Length;
            TestContext.Out.WriteLine($"session tabs after start: {restored}");
            Assert.That(restored, Is.EqualTo(2),
                        "the two connections flagged Connected were not reopened at startup although "
                        + "'Reconnect at startup' is on; only the automatic reconnect checkbox is unticked");
            AssertNoCrash("after the restore");
        }
    }
}

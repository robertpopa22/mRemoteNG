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
    /// "Reconnect to previously opened sessions on startup" across a real restart (#172).
    ///
    /// The reported 1.84.0 symptom: a tab closed during the session came back on the next start,
    /// unless the user had saved the connections. Opening or closing a tab is not an edit, so with
    /// the default save setting the Connected flags in confCons.xml stay as they were at the last
    /// save, and the tree path reopened them. The seed reproduces that state directly: both
    /// connections are flagged Connected, as a file saved while both were open would be.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class PreviousSessionRestoreAcceptanceTests : UiAcceptanceTestBase
    {
        private const string Prefix = "lab-restore";

        protected override void SeedSettings()
        {
            ConnectionsSeeder seeder = new();
            seeder.Add($"{Prefix}-a", LabTargets.LinuxHost, ProtocolType.SSH2, LabTargets.Ssh,
                       LabTargets.LinuxUser, LabTargets.LinuxPassword);
            // A second user name: the initiator treats the same host, port and user as one open
            // connection (#2414) and would only switch to the first tab. The login need not succeed;
            // the session tab stays while PuTTY asks again.
            seeder.Add($"{Prefix}-b", LabTargets.LinuxHost, ProtocolType.SSH2, LabTargets.Ssh,
                       "labrestoreb", LabTargets.LinuxPassword);

            // The serializer writes Connected from the live tab count, which is zero here.
            string xml = seeder.Build().Replace("Connected=\"false\"", "Connected=\"true\"", StringComparison.Ordinal);
            Deployment.WriteConnectionsFile(xml);

            Deployment.WriteSettings(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["OpenConsFromLastSession"] = "True",
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
                                  || SafeName(t).StartsWith("labrestoreb@", StringComparison.OrdinalIgnoreCase))
                      .ToArray();

        private int WaitForStableSessionTabs(int atLeast)
        {
            // Each restored PuTTY session can raise its own host-key prompt, and the next one does not
            // appear until the previous is answered, so answer while waiting rather than after.
            DateTime deadline = DateTime.UtcNow.AddSeconds(90);
            while (SessionTabs().Length < atLeast && DateTime.UtcNow < deadline)
                AnswerExpectedPrompts(TimeSpan.FromSeconds(3));
            Assert.That(SessionTabs().Length, Is.GreaterThanOrEqualTo(atLeast),
                        $"fewer than {atLeast} restored session tab(s) appeared within 90 s");

            // Both restore paths run during startup; give a late one time to add its tab before
            // counting, so an extra tab is seen rather than missed.
            int last = -1;
            DateTime until = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < until)
            {
                AnswerExpectedPrompts(TimeSpan.FromSeconds(2));
                last = SessionTabs().Length;
                System.Threading.Thread.Sleep(1000);
            }
            return last;
        }

        [Test]
        [Touches("#172")]
        public void ATabClosedDuringTheSessionDoesNotComeBackAfterARestart()
        {
            if (!LabTargets.IsReachable(LabTargets.LinuxHost, LabTargets.Ssh))
                Assert.Ignore($"lab SSH target not reachable at {LabTargets.Describe(LabTargets.LinuxHost, LabTargets.Ssh)}.");

            int restored = WaitForStableSessionTabs(2);
            TestContext.Out.WriteLine($"first start: {restored} session tab(s) restored from the seeded flags");
            Assert.That(restored, Is.EqualTo(2), "the two flagged connections were not both reopened on the first start");

            Win32Mouse.MiddleClick(SessionTabs().Last());
            UiWait.Settle(MainWindow);
            AnswerExpectedPrompts(TimeSpan.FromSeconds(5));
            UiWait.Until(() => SessionTabs().Length == 1, "the closed session tab to go", TimeSpan.FromSeconds(15));
            AssertNoCrash("after closing one session tab");

            RestartApplication();

            int afterRestart = WaitForStableSessionTabs(1);
            TestContext.Out.WriteLine($"after restart: {afterRestart} session tab(s)");
            Assert.That(afterRestart, Is.EqualTo(1),
                        "the tab closed before exit came back after the restart: the stale Connected flag "
                        + "in confCons.xml was reopened instead of the set saved in the layout (#172)");
        }
    }
}

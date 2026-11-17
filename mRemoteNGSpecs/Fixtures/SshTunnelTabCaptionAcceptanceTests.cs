using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using mRemoteNG.Connection.Protocol;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// #252: a connection opened through an SSH tunnel shares its tab with the hidden PuTTY session
    /// that carries the tunnel, and that session's terminal-title tracking replaced the tab caption
    /// with the tunnel shell's title ("user@host: ~ (user)"). The target here is HTTP, as in the
    /// report; it points at the gateway's own SSH port through the tunnel, because the scenario is
    /// about the caption, not about the page.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class SshTunnelTabCaptionAcceptanceTests : UiAcceptanceTestBase
    {
        private const string TunnelName = "lab-tunnel-gateway";
        private const string TargetName = "web-through-tunnel";

        private string _evidenceDir = null!;

        protected override void SeedSettings()
        {
            ConnectionsSeeder seeder = new();
            seeder.Add(TunnelName, LabTargets.LinuxHost, ProtocolType.SSH2, LabTargets.Ssh,
                       LabTargets.LinuxUser, LabTargets.LinuxPassword);
            seeder.Add(TargetName, "127.0.0.1", ProtocolType.HTTP, LabTargets.Ssh,
                       configure: c => c.SSHTunnelConnectionName = TunnelName);
            Deployment.WriteConnectionsFile(seeder.Build());

            _evidenceDir = Path.Combine(AppContext.BaseDirectory, "_uiscenarios", "_evidence",
                                        TestContext.CurrentContext.Test.MethodName ?? "tunnel");
            Directory.CreateDirectory(_evidenceDir);
        }

        private AutomationElement Row(string name)
        {
            AutomationElement tree = UiWait.FindRequired(
                MainWindow, cf => cf.ByAutomationId("ConnectionTree"), "connection tree");
            return tree.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                       .First(e =>
                       {
                           try { return string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase); }
                           catch (Exception) { return false; }
                       });
        }

        private string[] TabCaptions() => [.. MainWindow
            .FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
            .Select(t => { try { return t.Name ?? ""; } catch (Exception) { return ""; } })];

        private static bool IsSessionTab(string caption) =>
            caption.Contains(TargetName, StringComparison.OrdinalIgnoreCase)
            || caption.StartsWith(LabTargets.LinuxUser + "@", StringComparison.OrdinalIgnoreCase);

        /// <summary>Accepts the throwaway lab host's key once, as PuttyTabCloseAcceptanceTests does.</summary>
        private void AcceptHostKeyAlertIfPresent()
        {
            foreach (System.Diagnostics.Process process in System.Diagnostics.Process.GetProcesses())
            {
                string processName;
                try { processName = process.ProcessName; }
                catch (Exception) { continue; }
                if (!processName.Contains("putty", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach ((IntPtr handle, _, string title) in Win32Windows.WindowsOf(process.Id))
                {
                    if (!title.Contains("Security Alert", StringComparison.OrdinalIgnoreCase))
                        continue;
                    AutomationElement? accept = Driver.Automation.FromHandle(handle)
                        .FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                        .FirstOrDefault(b =>
                        {
                            try { return (b.Name ?? "").Contains("Accept", StringComparison.OrdinalIgnoreCase); }
                            catch (Exception) { return false; }
                        });
                    if (accept is null)
                        continue;
                    TestContext.Out.WriteLine($"host-key alert accepted: '{title}'");
                    Win32Mouse.LeftClick(accept);
                    return;
                }
            }
        }

        [Test]
        [Issues("#252")]
        public void TabOpenedThroughATunnelKeepsTheConnectionCaption()
        {
            if (!LabTargets.IsReachable(LabTargets.LinuxHost, LabTargets.Ssh))
                Assert.Ignore($"lab SSH target {LabTargets.LinuxHost}:{LabTargets.Ssh} is not reachable.");
            Assert.That(LabTargets.LinuxPassword, Is.Not.Empty,
                        "MRNG_LAB_LINUX_PASSWORD is not visible to the battery process");

            Win32Mouse.DoubleClick(Row(TargetName));

            // The tunnel connects first; its host-key alert holds the forwarded port closed until
            // somebody answers it. Keep answering while the session tab settles.
            bool tabUp = UiWait.Happened(() =>
            {
                AcceptHostKeyAlertIfPresent();
                AnswerExpectedPrompts(TimeSpan.FromSeconds(1));
                return TabCaptions().Any(IsSessionTab);
            }, TimeSpan.FromSeconds(90));
            if (!tabUp)
                Assert.Ignore("no session tab appeared, so the caption was never exercised.");

            // The tunnel's shell sets its title right after logon ("mrng@host: ~"); the tracking
            // timer polls once a second. Give both well over that before reading the caption.
            Thread.Sleep(TimeSpan.FromSeconds(15));
            AssertNoCrash("after opening a connection through an SSH tunnel");

            string[] captions = TabCaptions();
            TestContext.Out.WriteLine($"tabs on screen: {string.Join(" | ", captions)}");
            string capture = Path.Combine(_evidenceDir, "main-window.png");
            Capture.Element(MainWindow).ToFile(capture);
            TestContext.AddTestAttachment(capture, "main window with the tunnelled tab");
            File.WriteAllText(Path.Combine(_evidenceDir, "tabs.txt"), string.Join(Environment.NewLine, captions));

            string caption = captions.First(IsSessionTab);
            Assert.That(caption, Does.Not.StartWith(LabTargets.LinuxUser + "@").IgnoreCase,
                        "the tunnel's shell title replaced the caption of the connection it carries");
            Assert.That(caption, Does.Contain(TargetName).IgnoreCase,
                        "the tab should show the name of the connection that was opened");
        }
    }
}

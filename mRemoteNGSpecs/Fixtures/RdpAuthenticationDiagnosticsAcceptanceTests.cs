using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using FlaUI.Core.Definitions;
using mRemoteNG.Connection.Protocol;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    [TestFixture]
    [NonParallelizable]
    [SupportedOSPlatform("windows")]
    public class RdpAuthenticationDiagnosticsAcceptanceTests : UiAcceptanceTestBase
    {
        protected override void SeedSettings()
        {
            Deployment.WriteConnectionsFile(new ConnectionsSeeder().Add("auth-diagnostic", LabTargets.WindowsTargetHost,
                ProtocolType.RDP, LabTargets.Rdp, LabTargets.WindowsUser, LabTargets.WindowsPassword,
                LabTargets.WindowsTargetName, c => { c.EnableRdsAadAuth = true; c.RedirectWebAuthn = true; }).Build());
            File.WriteAllText(Path.Combine(Deployment.Directory, "verbose.log.enable"), "");
        }

        [Test]
        [Touches("#196")]
        public void RequestedAuthenticationSettingsRecordTheControlsActualResponse()
        {
            var tree = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("ConnectionTree"), "connection tree");
            var row = tree.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                .First(e => e.Name.Contains("auth-diagnostic", StringComparison.Ordinal));
            Win32Mouse.DoubleClick(row);
            string path = Path.Combine(Deployment.Directory, "mRemoteNG-verbose.log");
            string trace = "";
            UiWait.Until(() =>
            {
                if (!File.Exists(path)) return false;
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                trace = reader.ReadToEnd();
                return trace.Contains("RDP authentication setting EnableRdsAadAuth: requested=True;", StringComparison.Ordinal);
            }, "the control to accept or reject the requested setting", TimeSpan.FromSeconds(30));
            var lines = trace.Split('\n').Where(l => l.Contains("RDP authentication setting", StringComparison.Ordinal)).ToArray();
            Assert.That(lines.Any(l => l.Contains("EnableRdsAadAuth", StringComparison.Ordinal)
                && (l.Contains("accepted by control", StringComparison.Ordinal) || l.Contains("rejected HRESULT=0x", StringComparison.Ordinal))), Is.True);
            string evidence = Path.Combine(AppContext.BaseDirectory, "_uiscenarios", "_evidence", "authentication-diagnostics");
            Directory.CreateDirectory(evidence);
            File.WriteAllLines(Path.Combine(evidence, "setting-results.txt"), lines);
            TestContext.Out.WriteLine(string.Join(Environment.NewLine, lines));
            TestContext.Out.WriteLine("This checks COM setting diagnostics, not Entra sign-in; the target is a local Windows account.");
        }
    }
}

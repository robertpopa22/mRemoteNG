using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using FlaUI.Core.AutomationElements;
using mRemoteNG.Connection.Protocol;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// #212: which automatic-reconnection (ARC) events the RDP control raises when the link drops.
    /// The announce-time stop lives in the OnAutoReconnecting (DISPID 17) handler, and the field log
    /// never shows it. This scenario drops the guest's network under a live session to the Windows
    /// target and records the [#212-diag] line for every ARC event, so the evidence does not wait
    /// for a laptop to sleep. A drop while awake must still reconnect.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class RdpAutoReconnectAcceptanceTests : UiAcceptanceTestBase
    {
        private const string ConnectionName = "lab-windows-arc";

        private string _evidenceDir = null!;

        protected override void SeedSettings()
        {
            ConnectionsSeeder seeder = new();
            seeder.Add(ConnectionName, LabTargets.WindowsTargetHost, ProtocolType.RDP, LabTargets.Rdp,
                       LabTargets.WindowsUser, LabTargets.WindowsPassword, LabTargets.WindowsTargetName);
            Deployment.WriteConnectionsFile(seeder.Build());
            Deployment.WriteSettings(new Dictionary<string, string>
            {
                ["ConfirmCloseConnection"] = "1",
                ["RdpReconnectionCount"] = "20",
            });
            _evidenceDir = Path.Combine(AppContext.BaseDirectory, "_uiscenarios", "_evidence",
                                        TestContext.CurrentContext.Test.MethodName ?? "arc");
            Directory.CreateDirectory(_evidenceDir);
        }

        [Test]
        public void ALinkDropNamesTheArcEventsItRaises()
        {
            ConnectToTarget();
            string names = UpAdapters();
            try
            {
                PowerShell($"Disable-NetAdapter -Name {names} -Confirm:$false");
                UiWait.Until(() => LogContains("phase=attempt"), "the control to announce an automatic reconnection", TimeSpan.FromSeconds(90));
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
            finally
            {
                PowerShell($"Enable-NetAdapter -Name {names} -Confirm:$false");
            }

            bool reconnected = UiWait.Happened(() => LogContains("phase=reconnected"), TimeSpan.FromSeconds(180));
            string lines = ArcLines("arc-events.txt");

            Assert.That(lines, Does.Contain("[#212-diag]"), "no ARC event reached the handlers");
            Assert.That(reconnected, Is.True, "a drop while awake did not reconnect");
            Assert.That(lines, Does.Not.Contain("auto_reconnect_stopped"), "a drop while awake was stopped as if after a sleep");
        }

        /// <summary>
        /// The stop itself. A paused VM does not reproduce a laptop's clocks, but the gate also reads
        /// the wall clock: with time synchronisation stopped, the guest's clock is moved ten minutes
        /// ahead while the network is down, which is what a resume looks like to it. The announced
        /// attempt must be stopped, the session must not come back, and the process must survive closing
        /// the session while the control is reconnecting.
        /// </summary>
        [Test]
        public void AnAttemptAfterAClockJumpIsStoppedAndNotReattached()
        {
            ConnectToTarget();
            string names = UpAdapters();
            PowerShell("Stop-Service vmictimesync,w32time -Force -ErrorAction SilentlyContinue");
            try
            {
                PowerShell($"Set-Date -Adjust ([TimeSpan]::FromMinutes(10)) | Out-Null; Disable-NetAdapter -Name {names} -Confirm:$false");
                UiWait.Until(() => LogContains("phase=attempt"), "the control to announce an automatic reconnection", TimeSpan.FromSeconds(90));
                UiWait.Until(() => LogContains("auto_reconnect_stopped_after_resume"), "the announced attempt to be stopped", TimeSpan.FromSeconds(30));
            }
            finally
            {
                PowerShell($"Set-Date -Adjust ([TimeSpan]::FromMinutes(-10)) | Out-Null; Enable-NetAdapter -Name {names} -Confirm:$false; " +
                           "Start-Service w32time,vmictimesync -ErrorAction SilentlyContinue");
            }

            // Long enough for a reconnection that was not really stopped to complete on a live link.
            Thread.Sleep(TimeSpan.FromSeconds(60));
            string lines = ArcLines("arc-stop-events.txt");

            Assert.That(lines, Does.Contain("auto_reconnect_stopped_after_resume_announced"),
                        "the stop did not come from the announcement");
            Assert.That(lines, Does.Not.Contain("phase=reconnected"), "the stopped session was attached again");
            Assert.That(Deployment.ReadAppLog() ?? string.Empty, Does.Not.Contain("RDP Disconnect failed"),
                        "stopping the reconnection showed a disconnect error");
            Assert.That(Deployment.ReadAppLog() ?? string.Empty, Does.Not.Contain("fatal=true"),
                        "the disconnect during the reconnection crashed the process");
            Assert.That(Driver.Application.HasExited, Is.False, "the application exited");
        }

        private void ConnectToTarget()
        {
            try
            {
                using System.Net.Sockets.TcpClient probe = new();
                if (!probe.ConnectAsync(LabTargets.WindowsTargetHost, LabTargets.Rdp).Wait(TimeSpan.FromSeconds(3)))
                    Assert.Ignore($"lab RDP target {LabTargets.WindowsTargetHost}:{LabTargets.Rdp} did not answer.");
            }
            catch (Exception ex) when (ex is not IgnoreException)
            {
                Assert.Ignore($"lab RDP target is not reachable: {ex.GetType().Name}");
            }
            Assert.That(LabTargets.WindowsPassword, Is.Not.Empty, "MRNG_LAB_WINDOWS_PASSWORD is not visible to the battery process");

            Win32Mouse.DoubleClick(Row(ConnectionName));
            UiWait.Settle(MainWindow);
            AnswerExpectedPrompts(TimeSpan.FromSeconds(20));
            UiWait.Until(() => LogContains("phase=login_complete"), "the RDP logon to complete", TimeSpan.FromSeconds(90));
            AnswerExpectedPrompts(TimeSpan.FromSeconds(5));
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }

        private static string UpAdapters()
        {
            string adapters = PowerShell("(Get-NetAdapter | Where-Object Status -eq 'Up' | Select-Object -ExpandProperty Name) -join '|'").Trim();
            TestContext.Out.WriteLine($"adapters taken down: {adapters}");
            Assert.That(adapters, Is.Not.Empty, "no adapter is up in the guest");
            return string.Join(",", adapters.Split('|').Select(n => $"'{n}'"));
        }

        private string ArcLines(string evidenceFile)
        {
            string log = Deployment.ReadAppLog() ?? string.Empty;
            string lines = string.Join(Environment.NewLine,
                log.Split('\n').Where(l => l.Contains("[#212-diag]", StringComparison.Ordinal)
                                           || l.Contains("event=rdp_reconnect", StringComparison.Ordinal)
                                           || l.Contains("auto_reconnect_stopped", StringComparison.Ordinal)));
            File.WriteAllText(Path.Combine(_evidenceDir, evidenceFile), lines);
            TestContext.Out.WriteLine(lines);
            return lines;
        }

        private AutomationElement Row(string name)
        {
            AutomationElement tree = UiWait.FindRequired(
                MainWindow, cf => cf.ByAutomationId("ConnectionTree"), "connection tree");
            return tree.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem))
                       .First(e =>
                       {
                           try { return (e.Name ?? "").Contains(name, StringComparison.OrdinalIgnoreCase); }
                           catch (Exception) { return false; }
                       });
        }

        private bool LogContains(string phrase) =>
            (Deployment.ReadAppLog() ?? string.Empty).Contains(phrase, StringComparison.Ordinal);

        private static string PowerShell(string command)
        {
            using Process p = Process.Start(new ProcessStartInfo("powershell.exe", $"-NoProfile -NonInteractive -Command \"{command}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            })!;
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(30000);
            return output;
        }
    }
}

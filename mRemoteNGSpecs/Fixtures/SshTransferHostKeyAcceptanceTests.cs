using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using mRemoteNGSpecs.Support;
using NUnit.Framework;
using Renci.SshNet;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// SSH File Transfer checks the server's host key before it sends the password.
    ///
    /// The window used to accept any host key, so a server in the middle received the password.
    /// The scenario drives Tools > SSH File Transfer... against the lab Linux target three times:
    /// an unknown key is offered for confirmation and the upload happens once it is trusted; the
    /// trusted key connects without a question; a key that differs from the trusted one is
    /// reported as changed, and "Do not connect" leaves the server untouched.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class SshTransferHostKeyAcceptanceTests : UiAcceptanceTestBase
    {
        private const string DialogTitle = "SSH host key verification";
        private readonly string _run = Guid.NewGuid().ToString("N")[..8];

        private string StorePath => Path.Combine(Deployment.SettingsPath, "ssh_known_hosts");

        private static string SafeName(AutomationElement e)
        {
            try { return e.Name; } catch (Exception) { return ""; }
        }

        private void OpenTransferWindow()
        {
            AutomationElement tools = UiWait.FindRequired(
                MainWindow, cf => cf.ByName("Tools").And(cf.ByControlType(ControlType.MenuItem)), "Tools menu");
            tools.Patterns.ExpandCollapse.Pattern.Expand();
            UiWait.Settle(MainWindow);
            UiWait.FindRequired(Driver.Automation.GetDesktop(),
                                cf => cf.ByName("SSH File Transfer...").And(cf.ByControlType(ControlType.MenuItem)),
                                "Tools menu entry 'SSH File Transfer...'").Click();
            UiWait.Settle(MainWindow);
        }

        private void Fill(string automationId, string value)
        {
            AutomationElement box = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId(automationId), automationId);
            box.Patterns.Value.Pattern.SetValue(value);
        }

        private string StartUpload(string tag)
        {
            string local = Path.Combine(Deployment.Directory, $"hostkey-{_run}-{tag}.txt");
            File.WriteAllText(local, $"mRemoteNG host key scenario {_run} {tag}");
            string remote = $"/tmp/mrng-hostkey-{_run}-{tag}.txt";

            Fill("txtHost", LabTargets.LinuxHost);
            Fill("txtPort", LabTargets.Ssh.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Fill("txtUser", LabTargets.LinuxUser);
            Fill("txtPassword", LabTargets.LinuxPassword);
            Fill("txtLocalFile", local);
            Fill("txtRemoteFile", remote);
            UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("radProtSFTP"), "SFTP option")
                  .Patterns.SelectionItem.Pattern.Select();
            UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("btnTransfer"), "Transfer button").Click();
            return remote;
        }

        private ModalDialogs.Dialog? WaitForHostKeyDialog(TimeSpan timeout)
        {
            ModalDialogs.Dialog? found = null;
            UiWait.Happened(() =>
            {
                found = ModalDialogs.Find(Driver).FirstOrDefault(d => string.Equals(d.Title, DialogTitle, StringComparison.Ordinal));
                return found is not null;
            }, timeout);
            return found;
        }

        private static void Choose(ModalDialogs.Dialog dialog, string command)
        {
            AutomationElement button = dialog.Element!
                .FindAllDescendants()
                .First(e => string.Equals(SafeName(e).Trim(), command, StringComparison.Ordinal));
            button.Click();
        }

        /// <summary>Reads the server through a client pinned to the key the application stored.</summary>
        private bool RemoteFileExists(string remote)
        {
            string[] entry = File.ReadAllLines(StorePath)
                .First(l => l.StartsWith(LabTargets.LinuxHost + " ", StringComparison.Ordinal))
                .Split(' ');
            byte[] pinned = Convert.FromBase64String(entry[2]);

            using SftpClient client = new(LabTargets.LinuxHost, LabTargets.Ssh, LabTargets.LinuxUser, LabTargets.LinuxPassword);
            client.HostKeyReceived += (_, e) => e.CanTrust = e.HostKey.AsSpan().SequenceEqual(pinned);
            client.Connect();
            bool exists = client.Exists(remote);
            if (exists)
                client.DeleteFile(remote);
            return exists;
        }

        [Test]
        public void SshFileTransferAsksAboutAnUnknownKeyAndRefusesAChangedOne()
        {
            if (!LabTargets.IsReachable(LabTargets.LinuxHost, LabTargets.Ssh))
                Assert.Ignore($"lab SSH target not reachable at {LabTargets.Describe(LabTargets.LinuxHost, LabTargets.Ssh)}.");
            if (string.IsNullOrEmpty(LabTargets.LinuxPassword))
                Assert.Ignore("MRNG_LAB_LINUX_PASSWORD is not set.");

            Assert.That(File.Exists(StorePath), Is.False, "the isolated deployment already trusts a host key");
            OpenTransferWindow();

            // 1. Unknown key: the question comes before any upload, and trusting it uploads.
            string first = StartUpload("first");
            ModalDialogs.Dialog? unknown = WaitForHostKeyDialog(TimeSpan.FromSeconds(30));
            Assert.That(unknown, Is.Not.Null, "no host key question for a server never seen before");
            TestContext.Out.WriteLine($"unknown-key dialog: {unknown!.Text}");
            Assert.That(unknown.Text, Does.Contain("cannot be established").And.Contain("SHA256:"));
            Choose(unknown, "Trust this key and connect");

            UiWait.Until(() => File.Exists(StorePath), "the trusted key written to ssh_known_hosts", TimeSpan.FromSeconds(15));
            UiWait.Until(() => RemoteFileExists(first), "the first upload on the server", TimeSpan.FromSeconds(30));
            AnswerExpectedPrompts(TimeSpan.FromSeconds(2));

            // 2. Trusted key: no question.
            string second = StartUpload("second");
            Assert.That(WaitForHostKeyDialog(TimeSpan.FromSeconds(10)), Is.Null, "a trusted key was questioned again");
            UiWait.Until(() => RemoteFileExists(second), "the second upload on the server", TimeSpan.FromSeconds(30));
            AnswerExpectedPrompts(TimeSpan.FromSeconds(2));

            // 3. Changed key: replace the trusted key with another one, then refuse.
            string realLine = File.ReadAllLines(StorePath).First(l => l.StartsWith(LabTargets.LinuxHost + " ", StringComparison.Ordinal));
            string[] parts = realLine.Split(' ');
            byte[] other = Convert.FromBase64String(parts[2]);
            other[^1] ^= 0xFF;
            string forgedLine = $"{parts[0]} {parts[1]} {Convert.ToBase64String(other)}";
            File.WriteAllLines(StorePath, File.ReadAllLines(StorePath).Select(l => string.Equals(l, realLine, StringComparison.Ordinal) ? forgedLine : l));

            string third = StartUpload("third");
            ModalDialogs.Dialog? changed = WaitForHostKeyDialog(TimeSpan.FromSeconds(30));
            Assert.That(changed, Is.Not.Null, "no warning for a key that differs from the trusted one");
            TestContext.Out.WriteLine($"changed-key dialog: {changed!.Text}");
            Assert.That(changed.Text, Does.Contain("has changed"));
            Choose(changed, "Do not connect");
            UiWait.Settle(MainWindow);
            AnswerExpectedPrompts(TimeSpan.FromSeconds(3));

            Assert.That(File.ReadAllLines(StorePath), Does.Contain(forgedLine), "refusing changed the trusted key");
            File.WriteAllLines(StorePath, File.ReadAllLines(StorePath).Select(l => string.Equals(l, forgedLine, StringComparison.Ordinal) ? realLine : l));
            Assert.That(RemoteFileExists(third), Is.False, "the upload happened although the changed key was refused");

            string log = Deployment.ReadAppLog() ?? "";
            Assert.That(log, Does.Contain("its SSH host key is not trusted"), "the refusal is not in the application log");
            AssertNoCrash("after the three transfers");
        }
    }
}

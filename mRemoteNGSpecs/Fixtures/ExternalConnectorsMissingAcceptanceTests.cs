using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNGSpecs.Drivers;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// A portable folder without ExternalConnectors.dll, the way #175, #191 and #192 reached it
    /// (on #192, Windows Defender had quarantined the file), and a real double-click on a
    /// connection.
    ///
    /// Up to v1.83.0 every connection crashed here: the connect methods referenced the vault types
    /// directly, so the runtime could not compile them without the file. Those calls now sit in
    /// methods of their own. What this proves, in the real process with real input, is the
    /// behaviour a user sees: a connection that uses no credential vault opens as usual, and one
    /// that does is refused with a message naming the missing file — no crash dialog either way.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class ExternalConnectorsMissingAcceptanceTests : UiAcceptanceTestBase
    {
        private const string PlainConnection = "plain";
        private const string VaultConnection = "vault";

        protected override void SeedSettings()
        {
            ConnectionsSeeder seeder = new();
            // Port 1 on loopback refuses at once: the tab opens and the attempt fails fast, with no
            // external dependency. Reaching the tab is what matters — it means the connect path ran.
            seeder.Add(PlainConnection, "127.0.0.1", ProtocolType.SSH2, 1);
            seeder.Add(VaultConnection, "127.0.0.1", ProtocolType.SSH2, 1,
                       configure: c => c.ExternalCredentialProvider = ExternalCredentialProvider.DelineaSecretServer);
            Deployment.WriteConnectionsFile(seeder.Build());

            // The reporters' folder: the shipped assembly is not beside the executable. The
            // scenario directory holds hard links, so this touches nothing but this scenario.
            File.Delete(Path.Combine(Deployment.Directory, "ExternalConnectors.dll"));
        }

        private AutomationElement Tree() =>
            UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("ConnectionTree"), "connection tree");

        private static string SafeName(AutomationElement e)
        {
            try { return e.Name; } catch (Exception) { return ""; }
        }

        private void DoubleClickConnection(string name)
        {
            UiWait.Until(() => Tree().FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                                     .Any(e => string.Equals(SafeName(e), name, StringComparison.Ordinal)),
                         $"'{name}' to appear in the tree", TimeSpan.FromSeconds(20));

            AutomationElement row = Tree()
                .FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                .First(e => string.Equals(SafeName(e), name, StringComparison.Ordinal));
            // Not row.DoubleClick(): on the lab guest FlaUI's two waited clicks arrive as two single
            // clicks and the tree starts a rename instead. One SendInput batch is a real double-click.
            Win32Mouse.DoubleClick(row);
        }

        private bool TabIsOpen(string name)
        {
            try
            {
                return MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
                                 .Any(t => string.Equals(SafeName(t), name, StringComparison.Ordinal));
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Test]
        [Issues("#175", "#191", "#192")]
        public void AConnectionWithoutACredentialVaultOpensWhenTheAssemblyIsMissing()
        {
            DoubleClickConnection(PlainConnection);

            UiWait.Until(() => TabIsOpen(PlainConnection) || CrashWatcher.Check(Driver, TimeSpan.FromMilliseconds(200)).Occurred,
                         "a tab for the plain connection, or the crash dialog", TimeSpan.FromSeconds(20));

            CrashWatcher.CrashResult crash = CrashWatcher.Check(Driver, TimeSpan.FromSeconds(3));
            string log = Deployment.ReadAppLog() ?? string.Empty;

            Assert.Multiple(() =>
            {
                Assert.That(crash.Occurred, Is.False,
                            "opening a connection that uses no vault crashed without ExternalConnectors.dll — "
                            + "the connect path still needs the assembly to compile");
                Assert.That(Driver.Application.HasExited, Is.False, "the application must still be running");
                Assert.That(TabIsOpen(PlainConnection), Is.True, "the connection must open a tab as usual");
                Assert.That(log, Does.Contain("ExternalConnectors.dll"),
                            "the missing file is still recorded in the log at startup");
            });
        }

        [Test]
        [Issues("#175", "#191", "#192")]
        public void AConnectionThatUsesACredentialVaultIsRefusedWithAMessageNamingTheFile()
        {
            DoubleClickConnection(VaultConnection);

            ModalDialogs.Dialog? message = null;
            UiWait.Until(() =>
                         {
                             message = ModalDialogs.Find(Driver)
                                                   .FirstOrDefault(d => d.Text.Contains("ExternalConnectors.dll", StringComparison.Ordinal));
                             return message is not null || CrashWatcher.Check(Driver, TimeSpan.FromMilliseconds(200)).Occurred;
                         },
                         "the message about the missing file, or the crash dialog", TimeSpan.FromSeconds(20));

            if (message?.Element is not null)
            {
                string shot = Path.Combine(Deployment.Directory, "missing-assembly-message.png");
                Capture.Element(message.Element).ToFile(shot);
                TestContext.AddTestAttachment(shot, "the message as shown");
            }
            TestContext.Out.WriteLine("message: " + message);

            Assert.Multiple(() =>
            {
                Assert.That(message, Is.Not.Null, "no message named the missing file");
                Assert.That(message?.Text, Does.Contain(Deployment.Directory), "the message says where the file was expected");
                Assert.That(CrashWatcher.Check(Driver, TimeSpan.FromMilliseconds(500)).Occurred, Is.False,
                            "the refusal must be a message, not the crash dialog");
                Assert.That(Driver.Application.HasExited, Is.False, "the application must still be running");
                Assert.That(TabIsOpen(VaultConnection), Is.False, "a refused connection opens no tab");
            });

            AutomationElement? ok = message?.Element?.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button));
            ok?.AsButton().Invoke();
        }
    }
}

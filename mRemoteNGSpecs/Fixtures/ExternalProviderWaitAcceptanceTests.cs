using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures;

[TestFixture]
[NonParallelizable]
[SupportedOSPlatform("windows")]
public class ExternalProviderWaitAcceptanceTests : UiAcceptanceTestBase
{
    private const string ConnectionName = "provider-wait";

    protected override void SeedSettings()
    {
        string mode = TestContext.CurrentContext.Test.Arguments.FirstOrDefault() as string ?? "rdp";
        if (TestContext.CurrentContext.Test.Name == nameof(APlainConnectionStillOpensWithoutTheProviderAssembly))
            mode = "missing-plain";
        var seeder = new ConnectionsSeeder();
        seeder.Add(ConnectionName, "127.0.0.1", mode.Contains("ssh", StringComparison.Ordinal) || mode == "missing-plain" ? ProtocolType.SSH2 : ProtocolType.RDP, 1, configure: c =>
        {
            if (mode == "missing-plain")
                c.Username = "test-user";
            else if (mode == "gateway")
            {
                c.Username = "test-user";
                c.RDGatewayUsageMethod = RDGatewayUsageMethod.Always;
                c.RDGatewayHostname = "127.0.0.1";
                c.RDGatewayUseConnectionCredentials = RDGatewayUseConnectionCredentials.ExternalCredentialProvider;
                c.RDGatewayExternalCredentialProvider = ExternalCredentialProvider.OnePassword;
                c.RDGatewayUserViaAPI = "op://Test/Wait";
            }
            else if (!mode.StartsWith("default", StringComparison.Ordinal))
            {
                c.ExternalCredentialProvider = ExternalCredentialProvider.OnePassword;
                c.UserViaAPI = "op://Test/Wait";
            }
        });
        Deployment.WriteConnectionsFile(seeder.Build());
        var settings = new Dictionary<string, string>
        {
            ["CheckForUpdatesAsked"] = "True"
        };
        if (mode.StartsWith("default", StringComparison.Ordinal))
        {
            settings["EmptyCredentials"] = "custom";
            settings["ExternalCredentialProviderDefault"] = "OnePassword";
            settings["UserViaAPIDefault"] = "op://Test/Wait";
        }
        Deployment.WriteSettings(settings);
        if (mode == "missing-plain")
        {
            File.Delete(Path.Combine(Deployment.Directory, "ExternalConnectors.dll"));
            return;
        }
        if (mode == "error")
            File.WriteAllText(Path.Combine(Deployment.Directory, "op-error"), "error");
        string source = Path.Combine(Deployment.Directory, "op-test.cs");
        File.WriteAllText(source, """
            using System;
            using System.IO;
            using System.Threading;
            class Program {
                static void Main() {
                    string dir = AppDomain.CurrentDomain.BaseDirectory;
                    File.WriteAllText(Path.Combine(dir, "op-started"), "started");
                    var deadline = DateTime.UtcNow.AddSeconds(45);
                    while (!File.Exists(Path.Combine(dir, "op-release")) && DateTime.UtcNow < deadline)
                        Thread.Sleep(50);
                    if (File.Exists(Path.Combine(dir, "op-error"))) {
                        Console.Error.WriteLine("synthetic authorization denial");
                        File.WriteAllText(Path.Combine(dir, "op-finished"), "finished");
                        Environment.Exit(1);
                    }
                    Console.WriteLine("{\"fields\":[{\"purpose\":\"USERNAME\",\"value\":\"test-user\"},{\"purpose\":\"PASSWORD\",\"value\":\"test-only\"}]}");
                    File.WriteAllText(Path.Combine(dir, "op-finished"), "finished");
                }
            }
            """);
        var start = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                                    "Microsoft.NET", "Framework64", "v4.0.30319", "csc.exe"),
            UseShellExecute = false,
            CreateNoWindow = true
        };
        start.ArgumentList.Add("/nologo");
        start.ArgumentList.Add("/out:" + Path.Combine(Deployment.Directory, "op.exe"));
        start.ArgumentList.Add(source);
        using var compiler = Process.Start(start)!;
        Assert.That(compiler.WaitForExit(30000), Is.True, "test CLI compiler must finish");
        Assert.That(compiler.ExitCode, Is.Zero, "test CLI must compile");
    }

    [TestCase("rdp")]
    [TestCase("ssh")]
    [TestCase("gateway")]
    [TestCase("default-rdp")]
    [TestCase("default-ssh")]
    [TestCase("error")]
    [TestCase("close")]
    [Issues("#217")]
    public void WaitingForAnExternalProviderKeepsTheMainWindowResponsive(string mode)
    {
        IntPtr hwnd = OpenSeededConnection();
        try
        {
            UiWait.Until(() => File.Exists(Path.Combine(Deployment.Directory, "op-started")),
                         "the test CLI entering its authorization wait", TimeSpan.FromSeconds(15));
            Thread.Sleep(300);
            bool responsive = SendMessageTimeout(hwnd, 0, UIntPtr.Zero, IntPtr.Zero, 2, 1000, out _) != IntPtr.Zero;
            string observation = mode + ": CLI waiting; main window answered WM_NULL: " + responsive;
            TestContext.Out.WriteLine(observation);
            string evidence = Path.Combine(Path.GetDirectoryName(Deployment.Directory)!, "_evidence", "ExternalProviderWait-" + mode);
            Directory.CreateDirectory(evidence);
            File.WriteAllText(Path.Combine(evidence, "observation.txt"), observation);
            Assert.That(responsive, Is.True, "authorization wait must not block the window message loop");
            if (mode == "close")
                Assert.That(CloseApplicationAndWaitForExit(), Is.True, "the user can close the app during authorization");
        }
        finally
        {
            File.WriteAllText(Path.Combine(Deployment.Directory, "op-release"), "release");
            if (File.Exists(Path.Combine(Deployment.Directory, "op-started")))
                UiWait.Until(() => File.Exists(Path.Combine(Deployment.Directory, "op-finished")),
                             "the test CLI finishing", TimeSpan.FromSeconds(15));
            Thread.Sleep(1000);
        }
        if (mode == "error")
            UiWait.Until(() => (Deployment.ReadAppLog() ?? "").Contains("synthetic authorization denial", StringComparison.Ordinal),
                         "the provider error being reported", TimeSpan.FromSeconds(10));
        if (mode != "close")
            AssertNoCrash("external provider authorization wait");
    }

    [Test]
    [Issues("#175", "#191", "#192", "#217")]
    public void APlainConnectionStillOpensWithoutTheProviderAssembly()
    {
        OpenSeededConnection();
        UiWait.Until(() => MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
                                    .Any(t => string.Equals(t.Name, ConnectionName, StringComparison.Ordinal)),
                     "the ordinary SSH connection tab", TimeSpan.FromSeconds(15));
        Assert.That(Driver.Application.HasExited, Is.False);
        AssertNoCrash("ordinary SSH without ExternalConnectors.dll");
    }

    private IntPtr OpenSeededConnection()
    {
        var picker = MainWindow.FindFirstDescendant(cf => cf.ByName("mRemoteNG — choose connections file"));
        if (picker is not null)
        {
            picker.FindFirstDescendant(cf => cf.ByName("Portable (exe\\Settings)"))
                  .Patterns.SelectionItem.Pattern.Select();
            picker.FindFirstDescendant(cf => cf.ByName("Load")).AsButton().Invoke();
        }
        var tree = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("ConnectionTree"), "connection tree");
        UiWait.Until(() => tree.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                              .Any(e => string.Equals(e.Name, ConnectionName, StringComparison.Ordinal)),
                     "the seeded connection", TimeSpan.FromSeconds(15));
        var row = tree.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                      .First(e => string.Equals(e.Name, ConnectionName, StringComparison.Ordinal));
        IntPtr hwnd = new(MainWindow.Properties.NativeWindowHandle.Value);
        Win32Mouse.DoubleClick(row);
        return hwnd;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(IntPtr window, uint message, UIntPtr wParam,
        IntPtr lParam, uint flags, uint timeout, out UIntPtr result);
}

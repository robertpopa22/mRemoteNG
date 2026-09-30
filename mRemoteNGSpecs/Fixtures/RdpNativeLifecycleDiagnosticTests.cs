using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.RDP;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures;

[TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
public class RdpNativeLifecycleDiagnosticTests
{
    [DllImport("user32.dll")]
    private static extern uint GetGuiResources(IntPtr process, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string className, string title);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")]
    private static extern IntPtr GetDlgItem(IntPtr window, int id);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr window, StringBuilder text, int length);
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    private sealed class NativeProtocol : RdpProtocol11
    {
        public AxHost Host => (AxHost)Control;
        public void DetachProtocolEvents() => RemoveEventHandlers();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CompareImmediateAndCompletedNativeDisconnectWithoutUiAutomation(bool waitForDisconnect)
    {
        Assert.That(LabTargets.WindowsPassword, Is.Not.Empty);
        string evidence = Path.Combine(AppContext.BaseDirectory, "_uiscenarios", "_evidence",
            $"native-disconnect-wait-{waitForDisconnect}");
        Directory.CreateDirectory(evidence);
        using Form pump = new() { Width = 1100, Height = 750, StartPosition = FormStartPosition.Manual,
            Location = new Point(30, 30), Text = "RDP native lifecycle diagnostic" };
        Exception? failure = null;
        using System.Windows.Forms.Timer certificatePrompt = new() { Interval = 300 };
        certificatePrompt.Tick += (_, _) =>
        {
            IntPtr dialog = FindWindow("#32770", "Remote Desktop Connection");
            GetWindowThreadProcessId(dialog, out uint owner);
            if (dialog != IntPtr.Zero && owner == Environment.ProcessId)
            {
                StringBuilder certificateName = new(256);
                GetWindowText(GetDlgItem(dialog, 13456), certificateName, certificateName.Capacity);
                IntPtr yes = GetDlgItem(dialog, 14004);
                if (yes != IntPtr.Zero && certificateName.ToString() == LabTargets.WindowsTargetName)
                    SendMessage(yes, 0x00F5, IntPtr.Zero, IntPtr.Zero);
            }
        };
        certificatePrompt.Start();
        pump.Shown += async (_, _) =>
        {
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            try
            {
                for (int cycle = 1; cycle <= 3; cycle++)
                {
                    Assert.That(Thread.CurrentThread.GetApartmentState(), Is.EqualTo(ApartmentState.STA));
                    using NativeProtocol protocol = new();
                    ConnectionInfo info = new() { Name = "native-lifecycle", Hostname = LabTargets.WindowsTargetHost,
                        Port = LabTargets.Rdp, Protocol = ProtocolType.RDP, Username = LabTargets.WindowsUser,
                        Password = LabTargets.WindowsPassword, Domain = LabTargets.WindowsTargetName,
                        RdpVersion = RdpVersion.Rdc11 };
                    using InterfaceControl parent = new(pump, protocol, info) { Dock = DockStyle.Fill };
                    protocol.InterfaceControl = parent;
                    Assert.That(protocol.Initialize(), Is.True);
                    AxHost host = protocol.Host;
                    var login = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    EventHandler loggedIn = (_, _) => login.TrySetResult();
                    var loginEvent = host.GetType().GetEvent("OnLoginComplete")!;
                    var endEvent = host.GetType().GetEvent("OnDisconnected")!;
                    var parameters = endEvent.EventHandlerType!.GetMethod("Invoke")!.GetParameters()
                        .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name)).ToArray();
                    Action onDisconnected = () => disconnected.TrySetResult();
                    Delegate endHandler = Expression.Lambda(endEvent.EventHandlerType,
                        Expression.Invoke(Expression.Constant(onDisconnected)), parameters).Compile();
                    loginEvent.AddEventHandler(host, loggedIn);
                    endEvent.AddEventHandler(host, endHandler);
                    dynamic native = host.GetOcx()!;
                    Assert.That(protocol.Connect(), Is.True);
                    await Task.Delay(3000);
                    using (Bitmap pending = new(pump.Width, pump.Height))
                    {
                        using Graphics graphics = Graphics.FromImage(pending);
                        graphics.CopyFromScreen(pump.Location, Point.Empty, pending.Size);
                        pending.Save(Path.Combine(evidence, $"connecting-{cycle}.png"));
                    }
                    TestContext.Out.WriteLine($"before login wait: connected={native.Connected}, login={login.Task.IsCompleted}, disconnected={disconnected.Task.IsCompleted}");
                    await login.Task.WaitAsync(TimeSpan.FromSeconds(60));
                    await Task.Delay(2000);
                    using (Bitmap desktop = new(pump.Width, pump.Height))
                    {
                        using Graphics graphics = Graphics.FromImage(desktop);
                        graphics.CopyFromScreen(pump.Location, Point.Empty, desktop.Size);
                        desktop.Save(Path.Combine(evidence, $"desktop-{cycle}.png"));
                    }
                    protocol.DetachProtocolEvents();
                    Assert.That(Thread.CurrentThread.GetApartmentState(), Is.EqualTo(ApartmentState.STA));
                    native.Disconnect();
                    if (waitForDisconnect) await disconnected.Task.WaitAsync(TimeSpan.FromSeconds(20));
                    loginEvent.RemoveEventHandler(host, loggedIn);
                    endEvent.RemoveEventHandler(host, endHandler);
                    parent.Dispose();
                    await Task.Delay(10000);
                    using var process = Process.GetCurrentProcess();
                    TestContext.Out.WriteLine($"cycle={cycle}, waited={waitForDisconnect}, nativeEvent={disconnected.Task.IsCompleted}, handles={process.HandleCount}, gdi={GetGuiResources(process.Handle, 0)}");
                    foreach (var message in Runtime.MessageCollector.Messages.Where(x => x.Text.Contains("native references", StringComparison.Ordinal)))
                        TestContext.Out.WriteLine(message.Text);
                    Runtime.MessageCollector.ClearMessages();
                }
            }
            catch (Exception ex) { failure = ex; }
            finally { pump.Close(); }
        };
        Application.Run(pump);
        if (failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}

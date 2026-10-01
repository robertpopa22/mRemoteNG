using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNG.Connection;
using NUnit.Framework;

namespace mRemoteNGTests.Connection.Protocol.RDP;

[TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
public class RdpActiveXOwnershipTests
{
    private sealed class TeardownProbe : RdpProtocol11
    {
        public bool UnsubscribedWhileParented { get; private set; }
        public void WireEvents() => SetEventHandlers();
        protected override void RemoveEventHandlers()
        {
            UnsubscribedWhileParented = Control is { IsDisposed: false, IsHandleCreated: true, Parent: not null };
            base.RemoveEventHandlers();
        }
    }

    [Test]
    public void ProtocolTeardownKeepsTheActiveXWindowParentedUntilItsEventsAreDetached()
    {
        using TeardownProbe protocol = new();
        using Form form = new() { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual,
            Location = new System.Drawing.Point(-10000, -10000) };
        using InterfaceControl parent = new(form, protocol, new ConnectionInfo { Name = "teardown test" });
        protocol.InterfaceControl = parent;
        form.Show();
        Assert.That(protocol.Initialize(), Is.True);
        protocol.WireEvents();
        parent.Dispose();
        Assert.That(protocol.UnsubscribedWhileParented, Is.True,
            "native teardown must begin before detaching the live ActiveX window");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndDisposeHost(int version, bool protocolEvents)
    {
        using RdpProtocol protocol = version switch
        {
            6 => new RdpProtocol(), 7 => new RdpProtocol7(), 8 => new RdpProtocol8(),
            9 => new RdpProtocol9(), 10 => new RdpProtocol10(), _ => new RdpProtocol11()
        };
        using Form form = new() { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual,
            Location = new System.Drawing.Point(-10000, -10000) };
        if (protocolEvents)
        {
            using InterfaceControl parent = new(form, protocol, new ConnectionInfo { Name = "ownership test" });
            protocol.InterfaceControl = parent;
            form.Show();
            Assert.That(protocol.Initialize(), Is.True);
            protocol.GetType().GetMethod("SetEventHandlers", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(protocol, null);
            var initializedHost = (AxHost)typeof(mRemoteNG.Connection.Protocol.ProtocolBase)
                .GetProperty("Control", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(protocol)!;
            parent.Dispose();
            Assert.That(initializedHost.IsDisposed, Is.True);
            return new WeakReference(initializedHost);
        }
        using AxHost control = (AxHost)protocol.GetType().GetMethod("CreateActiveXRdpClientControl",
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(protocol, null)!;
        form.Controls.Add(control);
        form.Show();
        Assert.That(control.IsHandleCreated, Is.True);
        Assert.That(control.GetOcx(), Is.Not.Null);
        control.Dispose();
        Assert.That(control.IsDisposed, Is.True);
        return new WeakReference(control);
    }

    [Test]
    public void DisposedActiveXHostsAreNotRootedByTheirComEventSink(
        [Values(6, 7, 8, 9, 10, 11)] int version, [Values] bool protocolEvents)
    {
        using Form pump = new() { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual,
            Location = new System.Drawing.Point(-10000, -10000) };
        Exception? failure = null;
        pump.Load += (_, _) =>
        {
            try
            {
                var hosts = Enumerable.Range(0, 3).Select(_ => CreateAndDisposeHost(version, protocolEvents)).ToArray();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Assert.That(hosts.Count(x => x.IsAlive), Is.Zero, "COM still owns a disposed RDP event sink");
            }
            catch (Exception ex) { failure = ex; }
            finally { Application.ExitThread(); }
        };
        Application.Run(pump);
        if (failure != null) throw failure;
    }
    [Test]
    public void DisposingAnInitializedProtocolReleasesTheNativeRdpObject([Values(6, 7, 8, 9, 10, 11)] int version)
    {
        using Form pump = new() { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual,
            Location = new System.Drawing.Point(-10000, -10000) };
        Exception? failure = null;
        pump.Load += (_, _) =>
        {
            try
            {
                using RdpProtocol protocol = version switch
                {
                    6 => new RdpProtocol(), 7 => new RdpProtocol7(), 8 => new RdpProtocol8(),
                    9 => new RdpProtocol9(), 10 => new RdpProtocol10(), _ => new RdpProtocol11()
                };
                using InterfaceControl parent = new(pump, protocol, new ConnectionInfo { Name = "native release test" });
                protocol.InterfaceControl = parent;
                Assert.That(protocol.Initialize(), Is.True);
                var host = (AxHost)typeof(mRemoteNG.Connection.Protocol.ProtocolBase)
                    .GetProperty("Control", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(protocol)!;
                // Our own counted reference outlives the teardown; whatever it reads when released
                // is what everyone else still holds.
                IntPtr native = Marshal.GetIUnknownForObject(host.GetOcx()!);
                parent.Dispose();
                Assert.That(host.IsDisposed, Is.True);
                Assert.That(Marshal.Release(native), Is.Zero, "something still holds the RDP ActiveX object after its protocol was disposed");
            }
            catch (Exception ex) { failure = ex; }
            finally { Application.ExitThread(); }
        };
        Application.Run(pump);
        if (failure != null) throw failure;
    }
}

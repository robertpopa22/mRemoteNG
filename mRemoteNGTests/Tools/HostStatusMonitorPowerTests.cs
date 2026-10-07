using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using mRemoteNG.Tools;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using NUnit.Framework;

namespace mRemoteNGTests.Tools;

/// <summary>
/// #210: in low-power mode only every 4th probe pass runs, and a pass runs promptly once low
/// power ends. Probes only ever touch a loopback listener owned by the test.
/// </summary>
[TestFixture]
public class HostStatusMonitorPowerTests
{
    private static (HostStatusMonitor monitor, ConnectionInfo connection, TcpListener listener, Func<bool> getLow, Action<bool> setLow)
        CreateMonitor(bool initialLowPower)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var connection = new ConnectionInfo { Hostname = "127.0.0.1", Port = port };
        var root = new RootNodeInfo(RootNodeType.Connection);
        root.AddChild(connection);
        var model = new ConnectionTreeModel();
        model.AddRootNode(root);

        bool lowPower = initialLowPower;
        var monitor = new HostStatusMonitor(model)
        {
            RequireIcmpEcho = false,
            StaggerDelayMilliseconds = 0,
            IsLowPowerMode = () => lowPower
        };
        return (monitor, connection, listener, () => lowPower, v => lowPower = v);
    }

    [Test]
    public async Task InLowPowerModeOnlyEveryFourthCycleProbes()
    {
        var (monitor, connection, listener, _, _) = CreateMonitor(initialLowPower: true);
        try
        {
            using (monitor)
            {
                for (int cycle = 1; cycle <= 3; cycle++)
                {
                    Assert.That(await monitor.RunCycleAsync(CancellationToken.None), Is.False, $"cycle {cycle} is skipped");
                    Assert.That(connection.HostReachabilityStatus, Is.EqualTo(HostReachabilityStatus.Unknown));
                }

                Assert.That(await monitor.RunCycleAsync(CancellationToken.None), Is.True, "4th cycle probes");
                Assert.That(connection.HostReachabilityStatus, Is.EqualTo(HostReachabilityStatus.Reachable));

                Assert.That(await monitor.RunCycleAsync(CancellationToken.None), Is.False, "the count starts over");
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    [Test]
    public async Task EveryCycleProbesWhenNotInLowPowerMode()
    {
        var (monitor, _, listener, _, _) = CreateMonitor(initialLowPower: false);
        try
        {
            using (monitor)
            {
                Assert.That(await monitor.RunCycleAsync(CancellationToken.None), Is.True);
                Assert.That(await monitor.RunCycleAsync(CancellationToken.None), Is.True);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    [Test]
    public async Task EndingLowPowerAfterSkippedCyclesRequestsAPromptPass()
    {
        var (monitor, connection, listener, _, setLow) = CreateMonitor(initialLowPower: true);
        try
        {
            using (monitor)
            {
                Assert.That(await monitor.RunCycleAsync(CancellationToken.None), Is.False);

                setLow(false);
                monitor.OnPowerStateChanged(null, EventArgs.Empty);

                Assert.That(monitor.WakePending, Is.True);
                Assert.That(await monitor.RunCycleAsync(CancellationToken.None), Is.True);
                Assert.That(connection.HostReachabilityStatus, Is.EqualTo(HostReachabilityStatus.Reachable));
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    [Test]
    public void PowerChangesWithoutSkippedCyclesDoNotWakeTheLoop()
    {
        var (monitor, _, listener, _, setLow) = CreateMonitor(initialLowPower: false);
        try
        {
            using (monitor)
            {
                setLow(false);
                monitor.OnPowerStateChanged(null, EventArgs.Empty);

                Assert.That(monitor.WakePending, Is.False);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    [Test]
    public async Task StillInLowPowerModeDoesNotWakeTheLoop()
    {
        var (monitor, _, listener, _, _) = CreateMonitor(initialLowPower: true);
        try
        {
            using (monitor)
            {
                await monitor.RunCycleAsync(CancellationToken.None);

                monitor.OnPowerStateChanged(null, EventArgs.Empty);

                Assert.That(monitor.WakePending, Is.False);
            }
        }
        finally
        {
            listener.Stop();
        }
    }
}

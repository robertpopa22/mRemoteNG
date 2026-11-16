using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using mRemoteNG.Connection.Protocol.VNC;
using NUnit.Framework;

namespace mRemoteNGTests.Connection.Protocol.VNC
{
    [TestFixture]
    public class VncProxyTunnelTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public async Task DisposeClosesTheTunnelWhetherItIsWaitingOrRelaying(bool connectViewer)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var server = new TcpListener(IPAddress.Loopback, 0);
            server.Start();
            using var proxiedClient = new TcpClient();
            await proxiedClient.ConnectAsync((IPEndPoint)server.LocalEndpoint, timeout.Token);
            using TcpClient peer = await server.AcceptTcpClientAsync(timeout.Token);
            using var tunnel = new ProtocolVNC.ProxyTunnel(proxiedClient);

            using var viewer = new TcpClient();
            await using NetworkStream peerStream = peer.GetStream();
            byte[] buffer = new byte[1];
            if (connectViewer)
            {
                await viewer.ConnectAsync(IPAddress.Loopback, tunnel.LocalPort, timeout.Token);
                await using NetworkStream viewerStream = viewer.GetStream();

                await viewerStream.WriteAsync(new byte[] { 0x52 }, timeout.Token);
                await peerStream.ReadExactlyAsync(buffer, timeout.Token);
                Assert.That(buffer[0], Is.EqualTo(0x52));

                await peerStream.WriteAsync(new byte[] { 0x46 }, timeout.Token);
                await viewerStream.ReadExactlyAsync(buffer, timeout.Token);
                Assert.That(buffer[0], Is.EqualTo(0x46));

                tunnel.Dispose();
                Assert.That(await viewerStream.ReadAsync(buffer, timeout.Token), Is.Zero);
            }
            else
            {
                tunnel.Dispose();
            }

            Assert.That(await peerStream.ReadAsync(buffer, timeout.Token), Is.Zero);
        }
    }
}

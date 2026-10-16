using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using mRemoteNG.Tools;
using NUnit.Framework;

namespace mRemoteNGTests.Tools
{
    /// <summary>
    /// What "reachable" means, pinned down.
    ///
    /// Everything here talks to loopback or to an address reserved for documentation, so the
    /// answers do not depend on the network the suite happens to run on.
    /// </summary>
    [TestFixture]
    public class HostReachabilityProbeTests
    {
        /// <summary>A real listener on an ephemeral loopback port, closed with the test.</summary>
        private sealed class Listener : System.IDisposable
        {
            private readonly TcpListener _listener = new(IPAddress.Loopback, 0);

            public Listener() => _listener.Start();

            public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

            public void Dispose() => _listener.Stop();
        }

        [Test]
        public async Task AListeningPortIsReachable()
        {
            using Listener listener = new();

            bool reachable = await HostReachabilityProbe.IsReachableAsync(
                "127.0.0.1", listener.Port, 2000, requireIcmpEcho: false, CancellationToken.None);

            Assert.That(reachable, Is.True);
        }

        [Test]
        public async Task AClosedPortIsNotReachableEvenThoughTheHostAnswersPing()
        {
            // A socket that is bound but never listens holds the port so nothing else can take
            // it, and refuses every connect - a closed port that cannot race with the test.
            using Socket reserved = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            reserved.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            int closedPort = ((IPEndPoint)reserved.LocalEndPoint!).Port;

            bool reachable = await HostReachabilityProbe.IsReachableAsync(
                "127.0.0.1", closedPort, 2000, requireIcmpEcho: true, CancellationToken.None);

            Assert.That(reachable, Is.False, "the port check comes first; a ping cannot rescue a closed port");
        }

        [Test]
        public async Task RequiringAnEchoStillPassesForAHostThatAnswersOne()
        {
            using Listener listener = new();

            bool reachable = await HostReachabilityProbe.IsReachableAsync(
                "127.0.0.1", listener.Port, 2000, requireIcmpEcho: true, CancellationToken.None);

            Assert.That(reachable, Is.True, "loopback answers ICMP without any privilege");
        }

        [Test]
        public async Task AnAddressNothingAnswersOnIsNotReachable()
        {
            // 192.0.2.1 is TEST-NET-1 (RFC 5737), reserved for documentation: the connect times
            // out rather than finding something that happens to listen on the suite's network.
            bool reachable = await HostReachabilityProbe.IsReachableAsync(
                "192.0.2.1", 3389, 200, requireIcmpEcho: false, CancellationToken.None);

            Assert.That(reachable, Is.False);
        }

        [TestCase(null, 22)]
        [TestCase("", 22)]
        [TestCase("   ", 22)]
        [TestCase("127.0.0.1", 0)]
        [TestCase("127.0.0.1", -1)]
        public async Task NothingToProbeIsNotReachable(string? hostname, int port)
        {
            bool reachable = await HostReachabilityProbe.IsReachableAsync(
                hostname, port, 200, requireIcmpEcho: false, CancellationToken.None);

            Assert.That(reachable, Is.False);
        }

        [Test]
        public async Task CancellationIsAnsweredAsNotReachableRatherThanThrown()
        {
            using CancellationTokenSource cancelled = new();
            cancelled.Cancel();

            bool reachable = await HostReachabilityProbe.IsReachableAsync(
                "192.0.2.1", 3389, 2000, requireIcmpEcho: false, cancelled.Token);

            Assert.That(reachable, Is.False);
        }

        [Test]
        public async Task AnEchoThatCannotBeSentCountsAsNoAnswer()
        {
            // The echo step is private; reach it directly with a name the resolver rejects, so the
            // failure arrives as an exception from the ping rather than as a status.
            var method = typeof(HostReachabilityProbe).GetMethod("AnswersEchoAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
            using CancellationTokenSource cancelled = new();
            cancelled.Cancel();

            var task = (Task<bool>)method.Invoke(null, new object[] { "no such host.invalid", 200, cancelled.Token })!;

            Assert.That(await task, Is.False);
        }
    }
}

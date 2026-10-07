using mRemoteNG.Connection;
using NUnit.Framework;
using mRemoteNG.Connection.Protocol.RDP;

namespace mRemoteNGTests.Connection
{
    public class WindowedFullscreenConnectionTests
    {
        [Test]
        public void WindowedFullscreenForce_IsAnIndependentFlag()
        {
            ConnectionInfo.Force value = ConnectionInfo.Force.WindowedFullscreen;

            Assert.That((int)value, Is.EqualTo(256));
            Assert.That(value.HasFlag(ConnectionInfo.Force.Fullscreen), Is.False);
            Assert.That(value.HasFlag(ConnectionInfo.Force.DoNotJump), Is.False);
        }

        [Test]
        public void InFlightGate_DeduplicatesOnlyTheSameConnection()
        {
            string first = "windowed-fullscreen-test-first";
            string second = "windowed-fullscreen-test-second";

            try
            {
                Assert.That(ConnectionInitiator.TryBeginWindowedFullscreen(first), Is.True);
                Assert.That(ConnectionInitiator.TryBeginWindowedFullscreen(first), Is.False);
                Assert.That(ConnectionInitiator.TryBeginWindowedFullscreen(second), Is.True);
            }
            finally
            {
                ConnectionInitiator.EndWindowedFullscreen(first);
                ConnectionInitiator.EndWindowedFullscreen(second);
            }

            Assert.That(ConnectionInitiator.TryBeginWindowedFullscreen(first), Is.True);
            ConnectionInitiator.EndWindowedFullscreen(first);
        }

        [Test]
        public void WindowedFullscreenResize_LegacyClientNeverAppliesAReconnectResize()
        {
            RdpProtocol8.SessionResizeDecision decision = RdpProtocol8.DecideWindowedFullscreenResize(
                supportsDynamicResize: false, loginComplete: true,
                client: new System.Drawing.Rectangle(0, 0, 1600, 900), padding: System.Windows.Forms.Padding.Empty);
            Assert.That(decision.Apply, Is.False);
        }

        [Test]
        public void WindowedFullscreenResize_PreLoginNeverApplies()
        {
            RdpProtocol8.SessionResizeDecision decision = RdpProtocol8.DecideWindowedFullscreenResize(
                supportsDynamicResize: true, loginComplete: false,
                client: new System.Drawing.Rectangle(0, 0, 1600, 900), padding: System.Windows.Forms.Padding.Empty);
            Assert.That(decision.Apply, Is.False);
        }

        [Test]
        public void WindowedFullscreenResize_DynamicClientUsesPaddedContentArea()
        {
            RdpProtocol8.SessionResizeDecision decision = RdpProtocol8.DecideWindowedFullscreenResize(
                supportsDynamicResize: true, loginComplete: true,
                client: new System.Drawing.Rectangle(0, 0, 1600, 900), padding: new System.Windows.Forms.Padding(4, 6, 8, 10));
            Assert.Multiple(() =>
            {
                Assert.That(decision.Apply, Is.True);
                Assert.That(decision.Size, Is.EqualTo(new System.Drawing.Size(1588, 884)));
            });
        }
    }
}

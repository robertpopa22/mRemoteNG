using System.Drawing;
using System.Windows.Forms;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol.RDP;
using NUnit.Framework;

namespace mRemoteNGTests.Connection.Protocol.RDP
{
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class RdpSessionResizeDecisionTests
    {
        [Test]
        public void ContentSizeSubtractsTheFramePadding()
        {
            Size size = RdpProtocol8.ContentSize(new Rectangle(0, 0, 200, 100), new Padding(8));

            Assert.That(size, Is.EqualTo(new Size(184, 84)));
        }

        [Test]
        public void Rdp8FitToWindowDoesNotApplyASessionResize()
        {
            var protocol = new Rdp8Probe();

            RdpProtocol8.SessionResizeDecision decision = RdpProtocol8.DecideSessionResize(
                protocol.Dynamic,
                RDPResolutions.FitToWindow,
                false,
                Size.Empty,
                new Rectangle(0, 0, 200, 100),
                new Padding(8));

            Assert.That(protocol.Dynamic, Is.False);
            Assert.That(decision.Apply, Is.False);
        }

        [Test]
        public void Rdp9FitToWindowAppliesThePanelContentSize()
        {
            var protocol = new Rdp9Probe();

            RdpProtocol8.SessionResizeDecision decision = RdpProtocol8.DecideSessionResize(
                protocol.Dynamic,
                RDPResolutions.FitToWindow,
                false,
                Size.Empty,
                new Rectangle(0, 0, 200, 100),
                new Padding(8));

            Assert.That(protocol.Dynamic, Is.True);
            Assert.That(decision.Apply, Is.True);
            Assert.That(decision.Size, Is.EqualTo(new Size(184, 84)));
        }

        [Test]
        public void FullscreenStillAppliesWhenTheClientCannotResizeInPlace()
        {
            RdpProtocol8.SessionResizeDecision decision = RdpProtocol8.DecideSessionResize(
                false,
                RDPResolutions.Fullscreen,
                true,
                new Size(1920, 1080),
                new Rectangle(0, 0, 200, 100),
                new Padding(8));

            Assert.That(decision.Apply, Is.True);
            Assert.That(decision.Size, Is.EqualTo(new Size(1920, 1080)));
        }

        [Test]
        public void APanelThatMovedDuringApplyNeedsAnotherPass()
        {
            bool moved = RdpProtocol8.NeedsAnotherPass(
                false,
                new Size(184, 84),
                new Rectangle(0, 0, 300, 200),
                new Padding(8));
            bool settled = RdpProtocol8.NeedsAnotherPass(
                false,
                new Size(184, 84),
                new Rectangle(0, 0, 200, 100),
                new Padding(8));
            bool fullscreen = RdpProtocol8.NeedsAnotherPass(
                true,
                new Size(1920, 1080),
                new Rectangle(0, 0, 300, 200),
                new Padding(8));

            Assert.That(moved, Is.True);
            Assert.That(settled, Is.False);
            Assert.That(fullscreen, Is.False);
        }

        private sealed class Rdp8Probe : RdpProtocol8
        {
            public bool Dynamic => SupportsDynamicResize;
        }

        private sealed class Rdp9Probe : RdpProtocol9
        {
            public bool Dynamic => SupportsDynamicResize;
        }
    }
}

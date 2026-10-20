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
        public void AdaptedSizeKeepsThePanelAtTheBaseScale()
        {
            Assert.That(RdpProtocol8.AdaptedSize(new Size(1610, 1019), 125, 125), Is.EqualTo(new Size(1610, 1019)));
        }

        [Test]
        public void AdaptedSizeShrinksByTheScaleRatioToEvenSides()
        {
            // 125 % panel rendered at remote 100 %: four fifths, stretched back by SmartSizing.
            Assert.That(RdpProtocol8.AdaptedSize(new Size(1610, 1019), 125, 100), Is.EqualTo(new Size(1288, 814)));
        }

        [Test]
        public void AdaptedSizeLeavesASmallPanelAlone()
        {
            Assert.That(RdpProtocol8.AdaptedSize(new Size(240, 180), 150, 100), Is.EqualTo(new Size(240, 180)));
        }

        [Test]
        public void FitZoomEnlargesASmallerSessionToTheTighterSide()
        {
            // Lab, 2026-10-20: 480x288 in 668x500 is limited by the width.
            Assert.That(RdpProtocol8.FitZoom(new Size(668, 500), new Size(480, 288)), Is.EqualTo(139u));
        }

        [Test]
        public void FitZoomBringsAnAdaptedSessionBackToItsPanel()
        {
            Size panel = new(1610, 1019);
            Size session = RdpProtocol8.AdaptedSize(panel, 125, 100);
            Assert.That(RdpProtocol8.FitZoom(panel, session), Is.EqualTo(125u));
        }

        [TestCase(1610, 1019, 1610, 1019)]
        [TestCase(1000, 700, 1288, 815)]
        [TestCase(0, 0, 640, 480)]
        public void FitZoomDoesNotZoomASessionThatAlreadyFills(int sw, int sh, int w, int h)
        {
            Assert.That(RdpProtocol8.FitZoom(new Size(sw, sh), new Size(w, h)), Is.EqualTo(100u));
        }

        [Test]
        public void FitZoomStopsAtTheControlsMaximum()
        {
            Assert.That(RdpProtocol8.FitZoom(new Size(3000, 2000), new Size(200, 200)), Is.EqualTo(300u));
        }

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
                RDPResolutions.FitToWindow,
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

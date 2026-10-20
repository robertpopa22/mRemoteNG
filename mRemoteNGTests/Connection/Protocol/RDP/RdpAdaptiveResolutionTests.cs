using mRemoteNG.Connection.Protocol.RDP;
using NUnit.Framework;

namespace mRemoteNGTests.Connection.Protocol.RDP
{
    [TestFixture]
    public class RdpAdaptiveResolutionTests
    {
        private static void Probes(RdpAdaptiveResolution c, long fromMs, long toMs, int rttMs)
        {
            for (long t = fromMs; t <= toMs; t += 15_000)
                c.Sample(t, rttMs);
        }

        [Test]
        public void AQuietRelayedLinkStaysAtTheFullPanel()
        {
            var c = new RdpAdaptiveResolution(3);
            Probes(c, 0, 1_800_000, 125); // the motorway median between tunnels
            Assert.That(c.Level, Is.EqualTo(0));
        }

        [Test]
        public void OneTunnelPeakDoesNotSwitch()
        {
            var c = new RdpAdaptiveResolution(3);
            Probes(c, 0, 60_000, 125);
            Assert.That(c.Sample(75_000, 1_580), Is.Null);
            Assert.That(c.Level, Is.EqualTo(0));
        }

        [Test]
        public void RepeatedPeaksStepDownOneLevel()
        {
            var c = new RdpAdaptiveResolution(3);
            Probes(c, 0, 60_000, 125);
            c.Sample(75_000, 1_297);
            RdpAdaptiveResolutionDecision? d = c.Sample(90_000, 900);

            Assert.That(d, Is.Not.Null);
            Assert.That(d!.FromLevel, Is.EqualTo(0));
            Assert.That(d.ToLevel, Is.EqualTo(1));
            Assert.That(d.Reason, Does.Contain("peaks"));
        }

        [Test]
        public void ItDoesNotStepDownAgainInsideTheDownDwell()
        {
            var c = new RdpAdaptiveResolution(3);
            c.Sample(0, 900);
            c.Sample(15_000, 900); // level 0 -> 1
            Assert.That(c.Sample(30_000, 900), Is.Null, "a second step inside 60 s would re-lay out twice");
            Assert.That(c.Level, Is.EqualTo(1));

            RdpAdaptiveResolutionDecision? d = c.Sample(75_000, 900);
            Assert.That(d?.ToLevel, Is.EqualTo(2));
        }

        [Test]
        public void ItNeverGoesBelowTheLowestLevel()
        {
            var c = new RdpAdaptiveResolution(3);
            for (long t = 0; t <= 600_000; t += 15_000)
                c.Sample(t, 1_500);
            Assert.That(c.Level, Is.EqualTo(2));
        }

        [Test]
        public void ItStepsBackUpOneLevelAfterFiveCalmMinutes()
        {
            var c = new RdpAdaptiveResolution(3);
            c.Sample(0, 900);
            c.Sample(15_000, 900); // -> level 1 at 15 s, last bad at 15 s
            Probes(c, 30_000, 300_000, 120);
            Assert.That(c.Level, Is.EqualTo(1), "not calm for 300 s yet");

            RdpAdaptiveResolutionDecision? d = c.Sample(315_000, 120);
            Assert.That(d, Is.Not.Null);
            Assert.That(d!.ToLevel, Is.EqualTo(0));
            Assert.That(d.Reason, Is.EqualTo("calm"));
        }

        [Test]
        public void AModeratePeakRestartsTheCalmClock()
        {
            var c = new RdpAdaptiveResolution(3);
            c.Sample(0, 900);
            c.Sample(15_000, 900); // -> level 1
            Probes(c, 30_000, 200_000, 120);
            c.Sample(210_000, 520); // a spike on its own, not enough to step down, and not calm
            Probes(c, 225_000, 500_000, 120);
            Assert.That(c.Level, Is.EqualTo(1));
            Probes(c, 515_000, 525_000, 120);
            Assert.That(c.Level, Is.EqualTo(0));
        }

        [Test]
        public void TwoLostProbesStepDown()
        {
            var c = new RdpAdaptiveResolution(3);
            Probes(c, 0, 30_000, 120);
            c.Loss(45_000);
            Assert.That(c.Loss(60_000)?.Reason, Is.EqualTo("loss"));
        }

        [Test]
        public void ADropStepsDown()
        {
            var c = new RdpAdaptiveResolution(3);
            Probes(c, 0, 30_000, 120);
            Assert.That(c.Drop(40_000)?.ToLevel, Is.EqualTo(1));
        }

        [Test]
        public void ASlowMedianStepsDownOnlyOverASpanOfAMinute()
        {
            var c = new RdpAdaptiveResolution(3);
            c.Sample(0, 300);
            c.Sample(15_000, 300);
            c.Sample(30_000, 300);
            Assert.That(c.Sample(45_000, 300), Is.Null, "four samples over 45 s are too short a span");
            Assert.That(c.Sample(60_000, 300)?.Reason, Is.EqualTo("rtt"));
        }

        [Test]
        public void PeaksBeforeAStepDoNotTakeTheNextStep()
        {
            var c = new RdpAdaptiveResolution(3);
            c.Sample(0, 900);
            c.Sample(15_000, 900); // level 0 -> 1
            Probes(c, 30_000, 120_000, 120);
            Assert.That(c.Level, Is.EqualTo(1));
        }

        [Test]
        public void OnePeakAtTheBatteryCadenceStepsDown()
        {
            var c = new RdpAdaptiveResolution(3);
            c.Sample(0, 120);
            Assert.That(c.Sample(60_000, 600)?.ToLevel, Is.EqualTo(1));
            Assert.That(c.Sample(120_000, 120), Is.Null);
        }

        [Test]
        public void ASinglePanelLevelNeverSwitches()
        {
            var c = new RdpAdaptiveResolution(1);
            for (long t = 0; t <= 300_000; t += 15_000)
                Assert.That(c.Sample(t, 1_500), Is.Null);
            Assert.That(c.Level, Is.EqualTo(0));
        }

        [TestCase(100u, new uint[] { 100 })]
        [TestCase(125u, new uint[] { 125, 100 })]
        [TestCase(150u, new uint[] { 150, 125, 100 })]
        [TestCase(200u, new uint[] { 200, 175, 150 })]
        [TestCase(120u, new uint[] { 120, 100 })]
        [TestCase(300u, new uint[] { 300, 250, 200 })]
        public void TheLadderStepsThroughStandardScalesAndStopsAt100(uint baseScale, uint[] expected)
        {
            Assert.That(RdpAdaptiveResolution.RemoteScaleLadder(baseScale), Is.EqualTo(expected));
        }

        [Test]
        public void ResetStartsAtTheFullPanel()
        {
            var c = new RdpAdaptiveResolution(3);
            c.Sample(0, 900);
            c.Sample(15_000, 900);
            c.Reset();
            Assert.That(c.Level, Is.EqualTo(0));
            Assert.That(c.Sample(20_000, 120), Is.Null);
        }
    }
}

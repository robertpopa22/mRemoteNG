using NUnit.Framework;
using mRemoteNG.Connection.Protocol.RDP;

namespace mRemoteNGTests.Connection.Protocol.RDP
{
    [TestFixture]
    public class RdpLinkStabilityMonitorTests
    {
        private static RdpAdaptDecision? FeedRtt(RdpLinkStabilityMonitor monitor, long fromMs, long toMs, long stepMs, int rttMs)
        {
            RdpAdaptDecision? last = null;
            for (long t = fromMs; t <= toMs; t += stepMs)
                last = monitor.Sample(t, rttMs) ?? last;
            return last;
        }

        [Test]
        public void ASlowLinkHeldForAMinuteWouldPrompt()
        {
            var monitor = new RdpLinkStabilityMonitor();

            RdpAdaptDecision? decision = FeedRtt(monitor, 0, 70_000, 10_000, 600);

            Assert.That(decision, Is.Not.Null);
            Assert.That(decision!.Decision, Is.EqualTo("would_prompt"));
            Assert.That(decision.Reason, Is.EqualTo("rtt"));
            Assert.That(decision.RttMedianMs, Is.EqualTo(600));
            Assert.That(monitor.IsUnstable, Is.True);
        }

        [Test]
        public void ABriefSpikeIsNotEnough()
        {
            var monitor = new RdpLinkStabilityMonitor();

            Assert.That(FeedRtt(monitor, 0, 30_000, 10_000, 900), Is.Null);
            Assert.That(monitor.IsUnstable, Is.False);
        }

        [Test]
        public void AFastLinkNeverPrompts()
        {
            var monitor = new RdpLinkStabilityMonitor();

            Assert.That(FeedRtt(monitor, 0, 300_000, 5_000, 40), Is.Null);
        }

        [Test]
        public void TwoDropsInTenMinutesWouldPrompt()
        {
            var monitor = new RdpLinkStabilityMonitor();

            Assert.That(monitor.Drop(0), Is.Null);
            RdpAdaptDecision? decision = monitor.Drop(300_000);

            Assert.That(decision!.Decision, Is.EqualTo("would_prompt"));
            Assert.That(decision.Reason, Is.EqualTo("drops"));
            Assert.That(decision.DropsInWindow, Is.EqualTo(2));
        }

        [Test]
        public void DropsFurtherApartThanTheWindowDoNotCount()
        {
            var monitor = new RdpLinkStabilityMonitor();

            monitor.Drop(0);

            Assert.That(monitor.Drop(RdpLinkStabilityMonitor.DropWindowMs + 1), Is.Null);
        }

        [Test]
        public void AFullQuarterHourOfCalmWouldOfferTheFullConfigurationBack()
        {
            var monitor = new RdpLinkStabilityMonitor();
            monitor.Drop(0);
            monitor.Drop(1_000);

            // The drops age out of the ten-minute window; the link then has to stay fast for
            // fifteen minutes more.
            RdpAdaptDecision? decision = FeedRtt(monitor, 610_000, 610_000 + RdpLinkStabilityMonitor.StableForMs + 120_000, 10_000, 40);

            Assert.That(decision!.Decision, Is.EqualTo("would_offer_restore"));
            Assert.That(monitor.IsUnstable, Is.False);
        }

        [Test]
        public void ADropDuringTheCalmPeriodRestartsIt()
        {
            var monitor = new RdpLinkStabilityMonitor();
            FeedRtt(monitor, 0, 70_000, 10_000, 600);
            FeedRtt(monitor, 80_000, 500_000, 10_000, 40);

            monitor.Drop(510_000);
            RdpAdaptDecision? decision = FeedRtt(monitor, 520_000, 1_200_000, 10_000, 40);

            Assert.That(decision, Is.Null);
            Assert.That(monitor.IsUnstable, Is.True);
        }

        [Test]
        public void BecomingUnstableAgainInsideTheCooldownIsSuppressed()
        {
            var monitor = new RdpLinkStabilityMonitor();
            Assert.That(FeedRtt(monitor, 0, 70_000, 10_000, 600)!.Decision, Is.EqualTo("would_prompt"));

            RdpAdaptDecision? restore = FeedRtt(monitor, 80_000, 1_100_000, 10_000, 40);
            Assert.That(restore!.Decision, Is.EqualTo("would_offer_restore"));

            monitor.Drop(1_200_000);
            RdpAdaptDecision? decision = monitor.Drop(1_201_000);

            Assert.That(decision!.Decision, Is.EqualTo("unstable_suppressed"));
            Assert.That(monitor.IsUnstable, Is.True);
        }

        [Test]
        public void BecomingUnstableAgainAfterTheCooldownPromptsAgain()
        {
            var monitor = new RdpLinkStabilityMonitor();
            FeedRtt(monitor, 0, 70_000, 10_000, 600);
            FeedRtt(monitor, 80_000, 1_100_000, 10_000, 40);

            long later = 70_000 + RdpLinkStabilityMonitor.PromptCooldownMs + 10_000;
            monitor.Drop(later);
            RdpAdaptDecision? decision = monitor.Drop(later + 1_000);

            Assert.That(decision!.Decision, Is.EqualTo("would_prompt"));
        }
    }
}

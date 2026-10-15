using System.Collections.Generic;
using System.Net;
using NUnit.Framework;
using mRemoteNG.Connection.Protocol.RDP;

namespace mRemoteNGTests.Connection.Protocol.RDP
{
    [TestFixture]
    public class RdpLinkProbeTests
    {
        private static readonly int[] RdpPort = [3389];
        private static readonly int[] RdpAndGatewayPorts = [3389, 443];

        [Test]
        public void ProbeWindowComputesLossMedianAndJitter()
        {
            RdpProbeWindow window = RdpNetworkStats.ComputeProbeWindow(new int?[] { 40, null, 60, 50, null });

            Assert.That(window.Samples, Is.EqualTo(5));
            Assert.That(window.Lost, Is.EqualTo(2));
            Assert.That(window.LossPct, Is.EqualTo(40));
            Assert.That(window.RttMinMs, Is.EqualTo(40));
            Assert.That(window.RttMedianMs, Is.EqualTo(50));
            Assert.That(window.RttMaxMs, Is.EqualTo(60));
            // answered in order 40, 60, 50: |60-40| + |50-60| = 30 over two steps
            Assert.That(window.JitterMs, Is.EqualTo(15));
        }

        [Test]
        public void AWindowWithNoAnswersHasNoRoundTrip()
        {
            RdpProbeWindow window = RdpNetworkStats.ComputeProbeWindow(new int?[] { null, null });

            Assert.That(window.LossPct, Is.EqualTo(100));
            Assert.That(window.RttMedianMs, Is.Null);
            Assert.That(window.JitterMs, Is.Null);
        }

        [Test]
        public void FirstProbeIsWrittenAndLaterOnesAreThrottled()
        {
            var stats = new RdpNetworkStats();

            Assert.That(stats.RecordProbe(0, 40)!.Reason, Is.EqualTo("first"));
            Assert.That(stats.RecordProbe(15_000, 42), Is.Null);

            RdpLinkProbeSample? interval = stats.RecordProbe(30_000, 44);
            Assert.That(interval, Is.Not.Null);
            Assert.That(interval!.Reason, Is.EqualTo("interval"));
            Assert.That(interval.Window.Samples, Is.EqualTo(3));
        }

        [Test]
        public void ALossBandChangeIsWrittenInsideTheThrottle()
        {
            var stats = new RdpNetworkStats();
            stats.RecordProbe(0, 40);

            RdpLinkProbeSample? sample = stats.RecordProbe(5_000, null);

            Assert.That(sample, Is.Not.Null);
            Assert.That(sample!.Reason, Is.EqualTo("loss_change"));
            Assert.That(sample.Window.LossPct, Is.EqualTo(50));
        }

        [Test]
        public void TheWindowKeepsTheLastTwentyProbes()
        {
            var stats = new RdpNetworkStats();
            for (int i = 0; i < 5; i++)
                stats.RecordProbe(i * 1_000, null);
            RdpLinkProbeSample? last = null;
            for (int i = 5; i < 25; i++)
                last = stats.RecordProbe(i * 1_000 + 30_000, 30) ?? last;

            Assert.That(last, Is.Not.Null);
            Assert.That(last!.Window.Samples, Is.EqualTo(20));
            Assert.That(last.Window.Lost, Is.EqualTo(0));
        }

        [Test]
        public void TheWindowDropsProbesOlderThanFiveMinutes()
        {
            var stats = new RdpNetworkStats();
            stats.RecordProbe(0, null);
            stats.RecordProbe(1_000, null);

            RdpLinkProbeSample? sample = stats.RecordProbe(400_000, 30);

            Assert.That(sample!.Window.Samples, Is.EqualTo(1));
            Assert.That(sample.Window.LossPct, Is.EqualTo(0));
        }

        [Test]
        public void TheSummaryCarriesTheWholeConnectionsProbes()
        {
            var stats = new RdpNetworkStats();
            stats.RecordProbe(0, 40);
            stats.RecordProbe(15_000, null);
            stats.RecordProbe(30_000, 60);
            stats.RecordProbe(45_000, 50);

            RdpNetworkSummary? summary = stats.Summary(60_000);

            Assert.That(summary, Is.Not.Null);
            Assert.That(summary!.Samples, Is.EqualTo(0));
            Assert.That(summary.ProbeSamples, Is.EqualTo(4));
            Assert.That(summary.ProbeRttAvgMs, Is.EqualTo(50));
            Assert.That(summary.ProbeLossPct, Is.EqualTo(25));
            Assert.That(summary.ProbeJitterMs, Is.EqualTo(15));
        }

        [TestCase(false, false, false, null, 15_000)]
        [TestCase(false, true, false, null, 60_000)]
        [TestCase(false, false, false, 100_000L, 5_000)]
        [TestCase(false, false, false, 10_000L, 15_000)]
        [TestCase(false, true, false, 100_000L, 60_000)]
        public void IntervalFollowsDropsAndPower(bool minimized, bool onBattery, bool stopped, long? lastDropMs, int expected) =>
            Assert.That(RdpLinkProbePolicy.IntervalMs(130_000, lastDropMs, minimized, onBattery, stopped), Is.EqualTo(expected));

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void ProbingPausesWhileMinimizedOrAfterReconnectionWasStopped(bool minimized, bool stopped) =>
            Assert.That(RdpLinkProbePolicy.IntervalMs(0, null, minimized, false, stopped), Is.Null);

        [Test]
        public void TcpFallbackNeverProbesMoreThanOncePerMinute()
        {
            Assert.That(RdpLinkProbePolicy.IntervalMs(0, null, false, false, false, tcpMethod: true), Is.EqualTo(RdpLinkProbePolicy.LowPowerIntervalMs));
            Assert.That(RdpLinkProbePolicy.IntervalMs(10_000, 5_000, false, false, false, tcpMethod: true), Is.EqualTo(RdpLinkProbePolicy.LowPowerIntervalMs));
        }

        [TestCase(15_000, 2_000)]
        [TestCase(1_000, 1_000)]
        public void ProbeTimeoutNeverExceedsTheInterval(int interval, int expected) =>
            Assert.That(RdpLinkProbePolicy.TimeoutMs(interval), Is.EqualTo(expected));

        [Test]
        public void IcmpThatNeverAnswersFallsBackToTcpAfterThreeHeldBackLosses()
        {
            RdpIcmpStep step = default;
            for (int i = 0; i < RdpLinkProbePolicy.TcpFallbackAfterLosses; i++)
            {
                Assert.That(step.SwitchToTcp, Is.False);
                step = RdpLinkProbePolicy.AfterIcmp(replied: false, step.Answered, step.LossRun, alive: true);
                Assert.That(step.Report, Is.False, "losses before the first reply are held back");
            }
            Assert.That(step.SwitchToTcp, Is.True);
        }

        [Test]
        public void HeldBackLossesDoNotCountWhileTheSessionIsDown()
        {
            RdpIcmpStep step = default;
            for (int i = 0; i < 5; i++)
                step = RdpLinkProbePolicy.AfterIcmp(replied: false, step.Answered, step.LossRun, alive: false);
            Assert.That(step.SwitchToTcp, Is.False);
            Assert.That(step.LossRun, Is.EqualTo(0));
        }

        [Test]
        public void LossyIcmpStaysIcmp()
        {
            RdpIcmpStep step = RdpLinkProbePolicy.AfterIcmp(replied: true, answeredBefore: false, lossRun: 0, alive: true);
            Assert.That(step.Answered, Is.True);
            Assert.That(step.Report, Is.True);

            for (int i = 0; i < 10; i++)
            {
                step = RdpLinkProbePolicy.AfterIcmp(replied: false, step.Answered, step.LossRun, alive: true);
                Assert.That(step.SwitchToTcp, Is.False, "an ICMP host that answered once is lossy, not filtered");
                Assert.That(step.Report, Is.True, "every lost ping after the first reply is a loss sample");
            }
            Assert.That(step.LossRun, Is.EqualTo(10));

            step = RdpLinkProbePolicy.AfterIcmp(replied: true, step.Answered, step.LossRun, alive: true);
            Assert.That(step.LossRun, Is.EqualTo(0));
        }

        [Test]
        public void TheTargetIsTheSingleEndpointOnTheSessionPort()
        {
            var remotes = new List<IPEndPoint>
            {
                new(IPAddress.Parse("192.0.2.10"), 3389),
                new(IPAddress.Parse("198.51.100.7"), 443),
                new(IPAddress.Parse("192.0.2.10"), 3389)
            };

            IPEndPoint? target = RdpLinkProbePolicy.SelectTarget(remotes, RdpPort, [], out int candidates);

            Assert.That(candidates, Is.EqualTo(1));
            Assert.That(target, Is.EqualTo(new IPEndPoint(IPAddress.Parse("192.0.2.10"), 3389)));
        }

        [Test]
        public void AnAmbiguousMatchSetsAsideEndpointsOtherSessionsProbe()
        {
            var first = new IPEndPoint(IPAddress.Parse("192.0.2.10"), 3389);
            var second = new IPEndPoint(IPAddress.Parse("192.0.2.11"), 3389);

            Assert.That(RdpLinkProbePolicy.SelectTarget([first, second], RdpPort, [], out int ambiguous), Is.Null);
            Assert.That(ambiguous, Is.EqualTo(2));
            Assert.That(RdpLinkProbePolicy.SelectTarget([first, second], RdpPort, [first], out _), Is.EqualTo(second));
        }

        [Test]
        public void TheEndpointThatAppearedOnConnectIsThisSessionsTarget()
        {
            // Laptop log 2026-10-15: three tabs on 3389 to different hosts; the second tab's probe
            // saw two unclaimed candidates and gave up with reason=no_target.
            var first = new IPEndPoint(IPAddress.Parse("192.0.2.10"), 3389);
            var second = new IPEndPoint(IPAddress.Parse("192.0.2.11"), 3389);
            var third = new IPEndPoint(IPAddress.Parse("192.0.2.12"), 3389);
            var unrelated = new IPEndPoint(IPAddress.Parse("192.0.2.50"), 443);

            IPEndPoint[] appeared = RdpLinkProbePolicy.AppearedEndpoints(
                [first, unrelated], [first, second, unrelated], RdpPort);
            Assert.That(appeared, Is.EqualTo(new[] { second }));

            Assert.That(RdpLinkProbePolicy.SelectTarget([first, second, third], RdpPort, [], out int candidates, appeared),
                Is.EqualTo(second));
            Assert.That(candidates, Is.EqualTo(1));
        }

        [Test]
        public void AnAmbiguousDiffFallsBackToClaimsAndCountsCandidates()
        {
            var first = new IPEndPoint(IPAddress.Parse("192.0.2.10"), 3389);
            var second = new IPEndPoint(IPAddress.Parse("192.0.2.11"), 3389);
            var third = new IPEndPoint(IPAddress.Parse("192.0.2.12"), 3389);

            // Another tab connected inside this session's window: two new endpoints.
            IPEndPoint[] appeared = RdpLinkProbePolicy.AppearedEndpoints([first], [first, second, third], RdpPort);
            Assert.That(appeared, Has.Length.EqualTo(2));

            Assert.That(RdpLinkProbePolicy.SelectTarget([first, second, third], RdpPort, [], out int ambiguous, appeared), Is.Null);
            Assert.That(ambiguous, Is.EqualTo(2));
            Assert.That(RdpLinkProbePolicy.SelectTarget([first, second, third], RdpPort, [third], out _, appeared), Is.EqualTo(second));
        }

        [Test]
        public void AnAppearedEndpointThatIsGoneFallsBackToTheWholeMatch()
        {
            var first = new IPEndPoint(IPAddress.Parse("192.0.2.10"), 3389);
            var reconnected = new IPEndPoint(IPAddress.Parse("192.0.2.20"), 3389);
            IPEndPoint[] appeared = [new IPEndPoint(IPAddress.Parse("192.0.2.11"), 3389)];

            Assert.That(RdpLinkProbePolicy.SelectTarget([first, reconnected], RdpPort, [first], out _, appeared),
                Is.EqualTo(reconnected));
        }

        [Test]
        public void NoMatchingConnectionMeansNoTarget()
        {
            IPEndPoint? target = RdpLinkProbePolicy.SelectTarget(
                [new IPEndPoint(IPAddress.Parse("192.0.2.10"), 22)], RdpAndGatewayPorts, [], out int candidates);

            Assert.That(target, Is.Null);
            Assert.That(candidates, Is.EqualTo(0));
        }

        [Test]
        public void ThreeLostOfTheLastFiveProbesIsADegradedLink()
        {
            var monitor = new RdpLinkStabilityMonitor();
            monitor.Sample(0, 40, probe: true);
            monitor.Sample(15_000, 40, probe: true);
            Assert.That(monitor.Loss(30_000), Is.Null);
            Assert.That(monitor.Loss(45_000), Is.Null);

            RdpAdaptDecision? decision = monitor.Loss(60_000);

            Assert.That(decision, Is.Not.Null);
            Assert.That(decision!.Decision, Is.EqualTo("would_prompt"));
            Assert.That(decision.Reason, Is.EqualTo("loss"));
            Assert.That(decision.DropsInWindow, Is.EqualTo(0));
        }

        [Test]
        public void ScatteredLossDoesNotDegradeTheLink()
        {
            var monitor = new RdpLinkStabilityMonitor();
            for (int i = 0; i < 20; i++)
            {
                RdpAdaptDecision? decision = i % 3 == 0 ? monitor.Loss(i * 15_000) : monitor.Sample(i * 15_000, 40, probe: true);
                Assert.That(decision, Is.Null);
            }
            Assert.That(monitor.IsUnstable, Is.False);
        }

        [Test]
        public void ALossyLinkIsNotCalmEvenWithAFastMedian()
        {
            var monitor = new RdpLinkStabilityMonitor();
            monitor.Loss(0);
            monitor.Loss(1_000);
            monitor.Loss(2_000);
            Assert.That(monitor.IsUnstable, Is.True);

            RdpAdaptDecision? last = null;
            for (long t = 3_000; t <= 1_200_000; t += 15_000)
            {
                last = (t / 15_000 % 2 == 0 ? monitor.Loss(t) : monitor.Sample(t, 20, probe: true)) ?? last;
            }

            Assert.That(last, Is.Null);
            Assert.That(monitor.IsUnstable, Is.True);
        }
    }
}

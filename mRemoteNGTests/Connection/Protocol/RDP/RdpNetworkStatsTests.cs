using System.Collections.Generic;
using NUnit.Framework;
using mRemoteNG.Connection.Protocol.RDP;

namespace mRemoteNGTests.Connection.Protocol.RDP
{
    [TestFixture]
    public class RdpNetworkStatsTests
    {
        [Test]
        public void FirstSampleIsAlwaysWritten()
        {
            var stats = new RdpNetworkStats();

            RdpNetworkSample? sample = stats.Record(1_000, 6, 5_000, 40);

            Assert.That(sample, Is.Not.Null);
            Assert.That(sample!.Reason, Is.EqualTo("first"));
            Assert.That(sample.PreviousQuality, Is.Null);
            Assert.That(sample.BandwidthKbps, Is.EqualTo(5_000));
            Assert.That(sample.RttMs, Is.EqualTo(40));
        }

        [Test]
        public void SamplesInsideTheThrottleWindowAreFoldedIntoTheNextLine()
        {
            var stats = new RdpNetworkStats();
            stats.Record(0, 4, 1_000, 100);

            Assert.That(stats.Record(5_000, 4, 300, 700), Is.Null);
            Assert.That(stats.Record(10_000, 4, 2_000, 60), Is.Null);
            RdpNetworkSample? line = stats.Record(RdpNetworkStats.ThrottleMs, 4, 900, 120);

            Assert.That(line, Is.Not.Null);
            Assert.That(line!.Reason, Is.EqualTo("interval"));
            Assert.That(line.WindowSamples, Is.EqualTo(3));
            Assert.That(line.WindowBandwidthMinKbps, Is.EqualTo(300));
            Assert.That(line.WindowBandwidthMaxKbps, Is.EqualTo(2_000));
            Assert.That(line.WindowRttMinMs, Is.EqualTo(60));
            Assert.That(line.WindowRttMaxMs, Is.EqualTo(700));
        }

        [Test]
        public void AChangeOfQualityIsWrittenAtOnce()
        {
            var stats = new RdpNetworkStats();
            stats.Record(0, 6, 10_000, 30);

            RdpNetworkSample? line = stats.Record(2_000, 2, 200, 900);

            Assert.That(line, Is.Not.Null);
            Assert.That(line!.Reason, Is.EqualTo("quality_change"));
            Assert.That(line.PreviousQuality, Is.EqualTo(6u));
            Assert.That(line.Quality, Is.EqualTo(2u));
        }

        [Test]
        public void SummaryCoversTheWholeConnection()
        {
            var stats = new RdpNetworkStats();
            stats.Record(0, 6, 4_000, 20);
            stats.Record(60_000, 3, 1_000, 200);
            stats.Record(90_000, 3, 1_000, 200);

            RdpNetworkSummary? summary = stats.Summary(120_000);

            Assert.That(summary, Is.Not.Null);
            Assert.That(summary!.Samples, Is.EqualTo(3));
            Assert.That(summary.BandwidthMinKbps, Is.EqualTo(1_000));
            Assert.That(summary.BandwidthAvgKbps, Is.EqualTo(2_000));
            Assert.That(summary.BandwidthMaxKbps, Is.EqualTo(4_000));
            Assert.That(summary.RttMinMs, Is.EqualTo(20));
            Assert.That(summary.RttAvgMs, Is.EqualTo(140));
            Assert.That(summary.RttMaxMs, Is.EqualTo(200));
            Assert.That(summary.QualitySeconds, Is.EqualTo("3:60,6:60"));
        }

        [Test]
        public void OutageRunsFromTheFirstAttemptToTheReconnect()
        {
            var stats = new RdpNetworkStats();
            stats.ReconnectAttempt(10_000);
            stats.ReconnectAttempt(15_000);

            long outage = stats.Reconnected(22_000);
            RdpNetworkSummary? summary = stats.Summary(30_000);

            Assert.That(outage, Is.EqualTo(12_000));
            Assert.That(summary!.ReconnectAttempts, Is.EqualTo(2));
            Assert.That(summary.Reconnects, Is.EqualTo(1));
            Assert.That(summary.OutageMs, Is.EqualTo(12_000));
            Assert.That(summary.OutageOpen, Is.False);
        }

        [Test]
        public void AnOutageStillOpenAtTheEndIsCountedAndFlagged()
        {
            var stats = new RdpNetworkStats();
            stats.Record(0, 5, 3_000, 50);
            stats.ReconnectAttempt(40_000);

            RdpNetworkSummary? summary = stats.Summary(55_000);

            Assert.That(summary!.OutageMs, Is.EqualTo(15_000));
            Assert.That(summary.OutageOpen, Is.True);
            Assert.That(summary.Reconnects, Is.EqualTo(0));
        }

        [Test]
        public void ReconnectedWithoutAnAttemptReportsNoOutage()
        {
            var stats = new RdpNetworkStats();

            Assert.That(stats.Reconnected(5_000), Is.EqualTo(0));
        }

        [Test]
        public void NothingRecordedMeansNoSummary()
        {
            Assert.That(new RdpNetworkStats().Summary(1_000), Is.Null);
        }

        [Test]
        public void NegativeReadingsFromTheControlAreClampedToZero()
        {
            var stats = new RdpNetworkStats();

            RdpNetworkSample? sample = stats.Record(0, 1, -1, -5);

            Assert.That(sample!.BandwidthKbps, Is.EqualTo(0));
            Assert.That(sample.RttMs, Is.EqualTo(0));
        }

        [Test]
        public void QualityTimeIsListedLowestLevelFirst()
        {
            var perQuality = new Dictionary<uint, long> { [6] = 3_600_000, [2] = 45_500 };

            Assert.That(RdpNetworkStats.FormatQualityTime(perQuality), Is.EqualTo("2:45,6:3600"));
            Assert.That(RdpNetworkStats.FormatQualityTime(new Dictionary<uint, long>()), Is.EqualTo("none"));
        }
    }
}

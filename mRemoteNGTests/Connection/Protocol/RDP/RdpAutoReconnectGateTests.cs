using System;
using mRemoteNG.Connection.Protocol.RDP;
using NUnit.Framework;

namespace mRemoteNGTests.Connection.Protocol.RDP
{
    [TestFixture]
    public class RdpAutoReconnectGateTests
    {
        private static readonly DateTime T0 = new(2026, 10, 7, 8, 25, 0, DateTimeKind.Utc);

        private static RdpAutoReconnectGate ConnectedAt(long ticks, DateTime utc)
        {
            var gate = new RdpAutoReconnectGate();
            gate.Reset(ticks, utc);
            return gate;
        }

        [Test]
        public void ShortNetworkDropKeepsAutoReconnect()
        {
            var gate = ConnectedAt(1_000, T0);
            gate.Touch(31_000, T0.AddSeconds(30));

            Assert.That(gate.ShouldContinue(40_000, T0.AddSeconds(39)), Is.True);
            Assert.That(gate.ShouldContinue(45_000, T0.AddSeconds(44)), Is.True);
            Assert.That(gate.StoppedAfterResume, Is.False);
        }

        [Test]
        public void AttemptAfterSleepIsStopped()
        {
            // Lid closed with the session alive; first ARC attempt only after resume an hour later.
            var gate = ConnectedAt(1_000, T0);

            Assert.That(gate.ShouldContinue(3_601_000, T0.AddHours(1)), Is.False);
            Assert.That(gate.StoppedAfterResume, Is.True);
        }

        [Test]
        public void AttemptBeforeHibernateThenAfterResumeIsStopped()
        {
            // #212 timeline: one attempt at 11:55:36, hibernate, next attempt at 12:59:15.
            var gate = ConnectedAt(1_000, T0);
            Assert.That(gate.ShouldContinue(5_000, T0.AddSeconds(4)), Is.True);

            Assert.That(gate.ShouldContinue(3_815_000, T0.AddMinutes(63.5)), Is.False);
        }

        [Test]
        public void LivenessTickAfterResumeDoesNotHideTheGap()
        {
            var gate = ConnectedAt(1_000, T0);
            gate.Touch(3_601_000, T0.AddHours(1));

            Assert.That(gate.ShouldContinue(3_610_000, T0.AddHours(1).AddSeconds(9)), Is.False);
        }

        [Test]
        public void StaleWallClockAfterHibernateIsCaughtByTickCount()
        {
            var gate = ConnectedAt(1_000, T0);

            Assert.That(gate.ShouldContinue(3_601_000, T0.AddSeconds(7)), Is.False);
        }

        [Test]
        public void WallClockJumpIsCaughtWhenTickCountMissedTheSleep()
        {
            var gate = ConnectedAt(1_000, T0);

            Assert.That(gate.ShouldContinue(8_000, T0.AddMinutes(64)), Is.False);
        }

        [Test]
        public void ResumeLongBeforeTheAttemptNoLongerBlocks()
        {
            // Session survived a short sleep; a genuine network blip happens much later.
            var gate = ConnectedAt(1_000, T0);
            gate.Touch(601_000, T0.AddMinutes(10));
            for (int i = 1; i <= 20; i++)
                gate.Touch(601_000 + i * 30_000, T0.AddMinutes(10).AddSeconds(i * 30));

            Assert.That(gate.ShouldContinue(1_210_000, T0.AddMinutes(20).AddSeconds(9)), Is.True);
        }

        [Test]
        public void ReconnectResetsTheGate()
        {
            var gate = ConnectedAt(1_000, T0);
            Assert.That(gate.ShouldContinue(3_601_000, T0.AddHours(1)), Is.False);

            gate.Reset(3_700_000, T0.AddHours(1).AddMinutes(2));

            Assert.That(gate.StoppedAfterResume, Is.False);
            Assert.That(gate.ShouldContinue(3_710_000, T0.AddHours(1).AddMinutes(2).AddSeconds(10)), Is.True);
        }
    }
}

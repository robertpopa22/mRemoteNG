using mRemoteNG.App;
using mRemoteNG.Connection.Protocol.RDP;
using NUnit.Framework;

namespace mRemoteNGTests.Connection.Protocol.RDP
{
    public class RdpPhaseClockTests
    {
        private long _now;
        private RdpPhaseClock NewClock() => new(() => _now);

        [Test]
        public void TheFirstLoginIsTimedFromConnect()
        {
            _now = 1_000;
            RdpPhaseClock clock = NewClock();
            clock.Connect();
            _now = 1_400;
            clock.Connected();
            _now = 3_000;

            Assert.That(clock.ElapsedMs, Is.EqualTo(2_000));
            Assert.That(clock.LoginComplete(), Is.False);
        }

        [Test]
        public void ALoginAfterAutoReconnectIsTimedFromTheOutageNotFromConnect()
        {
            _now = 0;
            RdpPhaseClock clock = NewClock();
            clock.Connect();
            _now = 2_000;
            clock.LoginComplete();

            _now = 295_000;
            Assert.That(clock.AutoReconnecting(1, out bool started), Is.True);
            Assert.That(started, Is.True);
            _now = 297_000;
            clock.Connected();
            _now = 298_964;

            Assert.That(clock.ElapsedMs, Is.EqualTo(3_964));
            Assert.That(clock.LoginComplete(), Is.True, "the second login is marked reconnect=true");
        }

        [Test]
        public void ASilentReconnectRestartsTheClockAtConnected()
        {
            _now = 0;
            RdpPhaseClock clock = NewClock();
            clock.Connect();
            _now = 2_000;
            clock.LoginComplete();

            _now = 290_000;
            clock.Connected();
            _now = 291_500;

            Assert.That(clock.ElapsedMs, Is.EqualTo(1_500));
            Assert.That(clock.LoginComplete(), Is.True);
        }

        [Test]
        public void BothArcEventsForOneAttemptAreCountedOnce()
        {
            RdpPhaseClock clock = NewClock();
            clock.Connect();
            clock.LoginComplete();

            Assert.That(clock.AutoReconnecting(1, out bool first), Is.True);
            Assert.That(clock.AutoReconnecting(1, out bool again), Is.False);
            Assert.That(clock.AutoReconnecting(2, out bool second), Is.True);
            Assert.That(first, Is.True);
            Assert.That(again, Is.False);
            Assert.That(second, Is.False);

            clock.LoginComplete();
            Assert.That(clock.AutoReconnecting(1, out bool nextOutage), Is.True, "a new outage counts attempt 1 again");
            Assert.That(nextOutage, Is.True);
        }

        [Test]
        public void ANewConnectForgetsThePreviousLogin()
        {
            RdpPhaseClock clock = NewClock();
            clock.Connect();
            clock.LoginComplete();
            clock.Connect();

            Assert.That(clock.LoginComplete(), Is.False);
        }

        [Test]
        public void OnlyAReconnectLoginCarriesTheReconnectField()
        {
            string first = RuntimeDiagnostics.FormatRdpPhase("0123456789ab", "login_complete", 1200, "10.0");
            string again = RuntimeDiagnostics.FormatRdpPhase("0123456789ab", "login_complete", 3964, "10.0", reconnect: true);

            Assert.That(first, Does.Not.Contain("reconnect="));
            Assert.That(again, Does.EndWith(" reconnect=true"));
            Assert.That(again, Does.Contain(" duration_ms=3964 "));
        }
    }
}

using mRemoteNG.Connection.Protocol.RDP;
using NUnit.Framework;

namespace mRemoteNGTests.Connection.Protocol.RDP
{
    public class RdpPhaseClockPerSessionTests
    {
        private long _now;
        private RdpPhaseClock NewClock() => new(() => _now);

        [Test]
        public void TabsOpenedInARowEachTimeTheirOwnLogin()
        {
            // Laptop log 2026-10-15, three tabs opened a few seconds apart: B connects at 20.5 s,
            // C at 26.8 s; B logs in at 30 s and C at 40 s. Each login is timed from its own connect.
            _now = 20_500;
            RdpPhaseClock b = NewClock();
            b.Connect();
            _now = 26_844;
            RdpPhaseClock c = NewClock();
            c.Connect();
            _now = 28_172;
            c.Connected();
            Assert.That(c.ElapsedMs, Is.EqualTo(1_328));

            _now = 30_000;
            Assert.That(b.ElapsedMs, Is.EqualTo(9_500));
            b.LoginComplete();

            _now = 40_000;
            Assert.That(c.ElapsedMs, Is.EqualTo(13_156));
            Assert.That(c.LoginComplete(), Is.False);
        }

        [Test]
        public void EveryProtocolInstanceOwnsItsPhaseClock()
        {
            System.Reflection.FieldInfo? field = typeof(RdpProtocol).GetField("_phaseClock",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            Assert.That(field!.IsStatic, Is.False);
            Assert.That(field.IsInitOnly, Is.True);

            // Constructing an RdpProtocol needs FrmMain on an STA thread; the field contract is enough:
            // an instance field, assigned once per protocol object, so no tab can read another's clock.
        }
    }
}

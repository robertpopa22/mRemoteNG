using System;
using mRemoteNG.App;
using NUnit.Framework;

namespace mRemoteNGTests.App;

/// <summary>#210: the diagnostics heartbeat slows down on battery or while minimized.</summary>
public class RuntimeDiagnosticsHeartbeatTests
{
    [Test]
    public void TheHeartbeatRunsEveryMinuteNormally() =>
        Assert.That(RuntimeDiagnostics.GetHeartbeatIntervalMs(lowPower: false), Is.EqualTo(60_000));

    [Test]
    public void TheHeartbeatRunsEveryFiveMinutesInLowPowerMode() =>
        Assert.That(RuntimeDiagnostics.GetHeartbeatIntervalMs(lowPower: true), Is.EqualTo(300_000));

    [Test]
    public void APeriodChangeKeepsTheRemainingDueTime()
    {
        var now = new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

        int due = RuntimeDiagnostics.ComputeHeartbeatDueMs(now.AddSeconds(20), now, 300_000);

        Assert.That(due, Is.EqualTo(20_000));
    }

    [Test]
    public void APeriodChangeCapsTheDueTimeAtTheNewPeriod()
    {
        var now = new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

        int due = RuntimeDiagnostics.ComputeHeartbeatDueMs(now.AddSeconds(250), now, 60_000);

        Assert.That(due, Is.EqualTo(60_000));
    }

    [Test]
    public void AnOverdueHeartbeatFiresImmediatelyAfterAPeriodChange()
    {
        var now = new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

        int due = RuntimeDiagnostics.ComputeHeartbeatDueMs(now.AddSeconds(-5), now, 300_000);

        Assert.That(due, Is.EqualTo(0));
    }
}

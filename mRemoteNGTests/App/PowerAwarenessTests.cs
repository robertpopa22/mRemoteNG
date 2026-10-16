using System;
using System.ComponentModel;
using System.Threading;
using mRemoteNG.App;
using NUnit.Framework;

namespace mRemoteNGTests.App;

/// <summary>
/// #210: idle background work backs off on battery or while minimized. The probe and the clock
/// are injected, so nothing here depends on the power state of the machine running the suite.
/// </summary>
[TestFixture]
[NonParallelizable]
public class PowerAwarenessTests
{
    private bool _onBattery;
    private long _now;
    private int _probeCalls;

    [SetUp]
    public void SetUp()
    {
        _onBattery = false;
        _now = 1_000;
        _probeCalls = 0;
        PowerAwareness.ResetForTests(() => { _probeCalls++; return _onBattery; }, () => _now);
    }

    [TearDown]
    public void TearDown() => PowerAwareness.ResetForTests();

    [Test]
    public void OnAcPowerWithAnActiveWindowLowPowerModeIsOff()
    {
        Assert.That(PowerAwareness.OnBattery, Is.False);
        Assert.That(PowerAwareness.IsMinimized, Is.False);
        Assert.That(PowerAwareness.LowPowerMode, Is.False);
    }

    [Test]
    public void OnBatteryLowPowerModeIsOn()
    {
        _onBattery = true;

        Assert.That(PowerAwareness.LowPowerMode, Is.True);
    }

    [Test]
    public void MinimizedAloneTurnsLowPowerModeOn()
    {
        PowerAwareness.IsMinimized = true;

        Assert.That(PowerAwareness.OnBattery, Is.False);
        Assert.That(PowerAwareness.LowPowerMode, Is.True);

        PowerAwareness.IsMinimized = false;

        Assert.That(PowerAwareness.LowPowerMode, Is.False);
    }

    [Test]
    public void TheBatteryIsNotReReadMoreOftenThanEveryThirtySeconds()
    {
        Assert.That(PowerAwareness.OnBattery, Is.False);
        _onBattery = true;
        _now += PowerAwareness.BatteryReadIntervalMs - 1;

        Assert.That(PowerAwareness.OnBattery, Is.False, "still inside the cache window");
        Assert.That(_probeCalls, Is.EqualTo(1));

        _now += 1;

        Assert.That(PowerAwareness.OnBattery, Is.True, "cache window elapsed");
        Assert.That(_probeCalls, Is.EqualTo(2));
    }

    [Test]
    public void ChangedIsRaisedWhenTheStateChangesAndNotOtherwise()
    {
        int raised = 0;
        EventHandler handler = (_, _) => raised++;
        PowerAwareness.Changed += handler;
        try
        {
            Assert.That(PowerAwareness.OnBattery, Is.False);
            Assert.That(raised, Is.EqualTo(0), "first read on AC power is not a change");

            PowerAwareness.IsMinimized = true;
            PowerAwareness.IsMinimized = true;
            Assert.That(raised, Is.EqualTo(1), "setting the same value again is not a change");

            _onBattery = true;
            _now += PowerAwareness.BatteryReadIntervalMs;
            Assert.That(PowerAwareness.OnBattery, Is.True);
            Assert.That(raised, Is.EqualTo(2));
        }
        finally
        {
            PowerAwareness.Changed -= handler;
        }
    }

    [Test]
    public void AFailingProbeIsTreatedAsMainsPower()
    {
        PowerAwareness.ResetForTests(() => throw new InvalidOperationException("no power API"), () => _now);

        Assert.That(PowerAwareness.OnBattery, Is.False);
        Assert.That(PowerAwareness.LowPowerMode, Is.False);
    }

    // #216: the subscription ran on the UI thread before any control existed, and
    // AsyncOperationManager left a plain SynchronizationContext there for the whole session.
    [Test]
    public void ReadingTheAsyncOperationContextOnABareThreadLeavesItBare()
    {
        SynchronizationContext? after = new();
        var thread = new Thread(() =>
        {
            PowerAwareness.WithoutInstallingContext(() => _ = AsyncOperationManager.SynchronizationContext);
            after = SynchronizationContext.Current;
        });
        thread.Start();
        thread.Join();

        Assert.That(after, Is.Null);
    }

    [Test]
    public void AnExistingContextIsKept()
    {
        var existing = new SynchronizationContext();
        SynchronizationContext? after = null;
        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(existing);
            PowerAwareness.WithoutInstallingContext(() => _ = AsyncOperationManager.SynchronizationContext);
            after = SynchronizationContext.Current;
        });
        thread.Start();
        thread.Join();

        Assert.That(after, Is.SameAs(existing));
    }

    private static void RaisePowerMode(Microsoft.Win32.PowerModes mode)
    {
        var handler = typeof(PowerAwareness).GetMethod("OnPowerModeChanged",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        handler.Invoke(null, new object?[] { null, new Microsoft.Win32.PowerModeChangedEventArgs(mode) });
    }

    [Test]
    public void AStatusChangeRereadsTheProbeAndRaisesChangedOnce()
    {
        int raised = 0;
        void Handler(object? s, EventArgs e) => raised++;
        PowerAwareness.Changed += Handler;
        try
        {
            _ = PowerAwareness.OnBattery;
            _onBattery = true;
            RaisePowerMode(Microsoft.Win32.PowerModes.StatusChange);

            Assert.That(raised, Is.EqualTo(1));
            Assert.That(PowerAwareness.OnBattery, Is.True);

            RaisePowerMode(Microsoft.Win32.PowerModes.Resume);
            Assert.That(raised, Is.EqualTo(1), "value did not change");
        }
        finally { PowerAwareness.Changed -= Handler; }
    }

    [Test]
    public void ASuspendNotificationIsIgnored()
    {
        int before = _probeCalls;
        RaisePowerMode(Microsoft.Win32.PowerModes.Suspend);

        Assert.That(_probeCalls, Is.EqualTo(before));
    }

    [Test]
    public void AThrowingSubscriberDoesNotBreakTheCaller()
    {
        void Bad(object? s, EventArgs e) => throw new InvalidOperationException("boom");
        PowerAwareness.Changed += Bad;
        try
        {
            Assert.DoesNotThrow(() => PowerAwareness.IsMinimized = true);
        }
        finally { PowerAwareness.Changed -= Bad; }
    }

    [Test]
    public void TheSystemProbeIsUsableAfterAFullReset()
    {
        PowerAwareness.ResetForTests();

        Assert.DoesNotThrow(() => _ = PowerAwareness.OnBattery);
    }

    [Test]
    public void InitializeIsIdempotentAndSubscribesWithoutLeavingAContext()
    {
        Exception? error = null;
        SynchronizationContext? after = null;
        var thread = new Thread(() =>
        {
            try
            {
                var flag = typeof(PowerAwareness).GetField("_subscribed",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
                flag.SetValue(null, false);
                PowerAwareness.Initialize();
                PowerAwareness.Initialize();
                after = SynchronizationContext.Current;
            }
            catch (Exception ex) { error = ex; }
        });
        thread.Start();
        thread.Join();

        Assert.That(error, Is.Null);
        Assert.That(after, Is.Null);
    }
}

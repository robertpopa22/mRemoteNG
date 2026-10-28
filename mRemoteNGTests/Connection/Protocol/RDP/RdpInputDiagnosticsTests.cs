using System;
using mRemoteNG.App;
using mRemoteNG.Connection.Protocol.RDP;
using NUnit.Framework;

namespace mRemoteNGTests.Connection.Protocol.RDP;

public class RdpInputDiagnosticsTests
{
    private static RdpInputSnapshot Active => new(RdpInputPhase.LoggedIn, RdpInputFocus.NativeChild,
        RdpInputForeground.Application, RdpInputActivation.HostRoot, true, true, true,
        false, false, true, false, true, false);

    private static RdpInputTrace Trace() => new(new RdpInputBudget(RdpInputTrace.ProcessLimitPerMinute));

    [Test]
    public void NativeChildrenAreRecognizedWithoutRelyingOnManagedFocus()
    {
        Assert.That(RdpInputTrace.ClassifyFocus(new IntPtr(1), new IntPtr(2), (parent, child) =>
            parent == new IntPtr(1) && child == new IntPtr(2)), Is.EqualTo(RdpInputFocus.NativeChild));
        string line = RuntimeDiagnostics.FormatRdpInput("0123456789ab", new(Active, RdpInputReason.Sample, 10, 0));
        Assert.That(line, Does.Contain("thread_focus=native_child "));
        Assert.That(line, Does.Contain("managed_focused=false managed_contains_focus=false"));
    }

    [Test]
    public void MissingFocusAndAnUnavailableHostRemainDistinct()
    {
        bool NeverCalled(IntPtr _, IntPtr __) => throw new AssertionException("must not inspect another window");
        Assert.That(RdpInputTrace.ClassifyFocus(IntPtr.Zero, new IntPtr(2), NeverCalled), Is.EqualTo(RdpInputFocus.Unavailable));
        Assert.That(RdpInputTrace.ClassifyFocus(new IntPtr(1), IntPtr.Zero, NeverCalled), Is.EqualTo(RdpInputFocus.None));
        Assert.That(RdpInputTrace.ClassifyFocus(new IntPtr(1), new IntPtr(1), NeverCalled), Is.EqualTo(RdpInputFocus.Host));
        Assert.That(RdpInputTrace.ClassifyFocus(new IntPtr(1), new IntPtr(2), (_, _) => false), Is.EqualTo(RdpInputFocus.Other));
    }

    [Test]
    public void StableActiveSamplesHaveOneMinuteHeartbeatAndInactiveOnesStaySilent()
    {
        RdpInputTrace trace = Trace();
        Assert.That(trace.Observe(0, 0, Active, RdpInputReason.Sample, false, out _), Is.True);
        Assert.That(trace.Observe(59_999, 59_999, Active, RdpInputReason.Sample, false, out _), Is.False);
        Assert.That(trace.Observe(60_000, 60_000, Active, RdpInputReason.Sample, false, out _), Is.True);
        RdpInputSnapshot inactive = Active with { Foreground = RdpInputForeground.Other };
        Assert.That(trace.Observe(65_000, 65_000, inactive, RdpInputReason.Sample, false, out _), Is.True, "record focus loss");
        Assert.That(trace.Observe(600_000, 600_000, inactive, RdpInputReason.Sample, false, out _), Is.False);
        Assert.That(Trace().Observe(0, 0, inactive, RdpInputReason.Sample, false, out _), Is.False);
    }

    [Test]
    public void LowPowerSamplingAndHeartbeatSlowDown()
    {
        Assert.That(RdpInputTrace.SampleIntervalMs(false), Is.EqualTo(5_000));
        Assert.That(RdpInputTrace.SampleIntervalMs(true), Is.EqualTo(30_000));
        RdpInputTrace trace = Trace();
        Assert.That(trace.Observe(0, 0, Active, RdpInputReason.Sample, true, out _), Is.True);
        Assert.That(trace.Observe(60_000, 60_000, Active, RdpInputReason.Sample, true, out _), Is.False);
        Assert.That(trace.Observe(300_000, 300_000, Active, RdpInputReason.Sample, true, out _), Is.True);
    }

    [Test]
    public void FocusAttemptAndReturnAreSeparateButDuplicateManagedEventsAreCoalesced()
    {
        RdpInputTrace trace = Trace();
        Assert.That(trace.Observe(0, 0, Active, RdpInputReason.FocusRequested, false, out _), Is.True);
        Assert.That(trace.Observe(1, 1, Active, RdpInputReason.FocusReturned, false, out _), Is.True);
        Assert.That(trace.Observe(2, 2, Active, RdpInputReason.ManagedFocus, false, out _), Is.True);
        Assert.That(trace.Observe(3, 3, Active, RdpInputReason.ManagedFocus, false, out _), Is.False);
        Assert.That(trace.Observe(4, 4, Active with { Phase = RdpInputPhase.Disconnected },
            RdpInputReason.Lifecycle, false, out _), Is.True);
    }

    [Test]
    public void SessionFloodIsBoundedAndNextRecordReportsSuppressionWithLatestState()
    {
        RdpInputTrace trace = Trace();
        for (int i = 0; i < RdpInputTrace.SessionLimitPerMinute; i++)
            Assert.That(trace.Observe(i * 1_000, i * 1_000, Active, RdpInputReason.ControlState, false, out _), Is.True);
        Assert.That(trace.Observe(30_000, 30_000, Active, RdpInputReason.ManagedBlur, false, out _), Is.False);
        RdpInputSnapshot latest = Active with { ThreadFocus = RdpInputFocus.Other };
        Assert.That(trace.Observe(60_000, 60_000, latest, RdpInputReason.Sample, false, out var sample), Is.True);
        Assert.That(sample.Suppressed, Is.EqualTo(1));
        Assert.That(sample.State, Is.EqualTo(latest));
    }

    [Test]
    public void ManySessionsShareAProcessCap()
    {
        RdpInputBudget shared = new(RdpInputTrace.ProcessLimitPerMinute);
        for (int i = 0; i < RdpInputTrace.ProcessLimitPerMinute; i++)
            Assert.That(new RdpInputTrace(shared).Observe(0, 0, Active, RdpInputReason.Lifecycle, false, out _), Is.True);
        RdpInputTrace delayed = new(shared);
        Assert.That(delayed.Observe(0, 0, Active, RdpInputReason.Lifecycle, false, out _), Is.False);
        Assert.That(delayed.Observe(60_000, 60_000, Active, RdpInputReason.Lifecycle, false, out var sample), Is.True);
        Assert.That(sample.Suppressed, Is.EqualTo(1));
    }

    [Test]
    public void FormatterAcceptsOnlyCorrelationIdAndFixedTypedValues()
    {
        RdpInputSnapshot invalid = Active with
        {
            Phase = (RdpInputPhase)99999, ThreadFocus = (RdpInputFocus)99999,
            Foreground = (RdpInputForeground)99999, ThreadActivation = (RdpInputActivation)99999
        };
        string line = RuntimeDiagnostics.FormatRdpInput("private-host\nuser=private-user path=C:\\private",
            new(invalid, (RdpInputReason)99999, -1, -1));
        Assert.That(line, Does.StartWith("rdp_session=invalid reason=unknown phase=unknown elapsed_ms=0 suppressed=0 "));
        Assert.That(line, Does.Contain("thread_focus=unknown foreground=unknown thread_activation=unknown"));
        Assert.That(line, Does.Not.Contain("private"));
        Assert.That(line, Does.Not.Contain("99999"));
        Assert.That(line, Does.Not.Contain("\n"));
        Assert.That(line.Length, Is.LessThan(600));
    }
}

using mRemoteNG.App;
using mRemoteNG.App.Diagnostics;
using NUnit.Framework;

namespace mRemoteNGTests.App;

/// <summary>The tree_click line splits the time from a tree click to OpenConnection into steps.</summary>
public class TreeClickTraceTests
{
    [Test]
    public void QueueWaitIsTheTickDifference()
    {
        Assert.That(TreeClickTrace.QueueWaitMs(1_000, 9_420), Is.EqualTo(8_420));
    }

    [Test]
    public void QueueWaitSurvivesTheTickCountWrap()
    {
        Assert.That(TreeClickTrace.QueueWaitMs(int.MaxValue - 99, unchecked(int.MaxValue + 101)), Is.EqualTo(200));
    }

    [Test]
    public void AStampFromTheFutureOrTooOldCountsAsZero()
    {
        Assert.That(TreeClickTrace.QueueWaitMs(5_000, 4_000), Is.EqualTo(0));
        Assert.That(TreeClickTrace.QueueWaitMs(0, 120_000), Is.EqualTo(0));
    }

    [Test]
    public void OtherIsWhatTheNamedStepsDoNotCover()
    {
        var steps = TreeClickTrace.Steps(totalMs: 9_000, downQueueMs: 10, selectMs: 20, configLoadMs: 30,
            saveUiMs: 0, clickQueueMs: 8_400, handlerMs: 40);

        Assert.That(steps[^1].Key, Is.EqualTo("other"));
        Assert.That(steps[^1].Value, Is.EqualTo(500));
    }

    [Test]
    public void OverlappingStepsNeverMakeOtherNegative()
    {
        var steps = TreeClickTrace.Steps(100, 50, 50, 50, 0, 50, 50);
        Assert.That(steps[^1].Value, Is.EqualTo(0));
    }

    [Test]
    public void TheLineNamesTheSlowStepWhenOneTookASecondOrMore()
    {
        var steps = TreeClickTrace.Steps(9_000, 10, 20, 30, 0, 8_400, 40);
        string line = RuntimeDiagnostics.FormatTreeClick("RDP", 2, 9_000, steps, 700);

        Assert.That(line, Is.EqualTo(
            "protocol=RDP clicks=2 total_ms=9000 down_queue_ms=10 select_ms=20 config_load_ms=30 save_ms=0 " +
            "click_queue_ms=8400 handler_ms=40 other_ms=500 save_bg_ms=700 slow_step=click_queue"));
    }

    [Test]
    public void NoSlowStepWhenEveryStepIsFast()
    {
        var steps = TreeClickTrace.Steps(300, 5, 20, 30, 0, 5, 40);
        Assert.That(RuntimeDiagnostics.FormatTreeClick("RDP", 2, 300, steps, 0), Does.Not.Contain("slow_step"));
    }

    [Test]
    public void AnOpenWithoutATracedClickWritesNothingAndDoesNotThrow()
    {
        Assert.DoesNotThrow(() => TreeClickTrace.ReachedOpenConnection("RDP"));
    }
}

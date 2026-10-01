using System.Linq;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGTests.Tools;

[TestFixture]
public class RetentionAssessmentTests
{
    private static readonly long[] Idle = [99, 100, 99, 101, 100, 99];
    private static RetentionAssessment.Result Assess(long[] closed, long[]? idle = null,
        int logins = 14, int settle = 30) =>
        RetentionAssessment.Assess(idle ?? Idle, closed, 32, logins, settle);

    [Test]
    public void ReporterSeriesCannotPassDespiteLargeFirstSession()
    {
        long[] series = [274, 376, 511, 525, 520, 651, 778, 774, 903, 899, 888, 1019, 1136, 1274];
        Assert.That(Assess(series).Verdict, Is.EqualTo("growth"));
        Assert.That(Assess(series).Growth, Is.EqualTo(1000));
    }

    [Test]
    public void SlowCumulativeGrowthCannotHideBehindSmallPerCycleDelta() =>
        Assert.That(Assess(Enumerable.Range(0, 14).Select(i => 200L + 3 * i).ToArray()).Verdict,
            Is.EqualTo("growth"));

    [Test]
    public void LargeInitialAllocationWithStablePlateauIsWithinFiniteBudget() =>
        Assert.That(Assess([800, 804, 799, 803, 800, 801, 799, 800, 802, 800, 799, 800, 801, 800]).Verdict,
            Is.EqualTo("within-budget"));

    [Test]
    public void LastSampleDipDoesNotHideGrowingTail() =>
        Assert.That(Assess([100, 101, 100, 120, 140, 160, 180, 200, 220, 240, 260, 280, 300, 100]).Verdict,
            Is.EqualTo("growth"));

    [Test]
    public void NoisyIdleControlCannotRelaxTheBudget() =>
        Assert.That(Assess(Enumerable.Repeat(200L, 14).ToArray(), [99, 100, 140, 120, 100, 99]).Verdict,
            Is.EqualTo("inconclusive"));

    [TestCase(3, 3, 30)]
    [TestCase(14, 13, 30)]
    [TestCase(14, 14, 4)]
    public void IncompleteEvidenceCannotPass(int cycles, int logins, int settle) =>
        Assert.That(Assess(Enumerable.Repeat(200L, cycles).ToArray(), logins: logins, settle: settle).Verdict,
            Is.EqualTo("inconclusive"));

    private static RetentionAssessment.ControlledResult Controlled(long[] application, long[] control, long[]? controlIdle = null) =>
        RetentionAssessment.AssessAgainstControl(Assess(application), Assess(control, controlIdle));

    private static long[] Rising(long start, long step) => Enumerable.Range(0, 14).Select(i => start + step * i).ToArray();

    [Test]
    public void GrowthMatchingMicrosoftsControlIsNotChargedToTheApplication()
    {
        var result = Controlled(Rising(1300, 75), Rising(1200, 75));
        Assert.That(result.Verdict, Is.EqualTo("within-budget"));
        Assert.That(result.Excess, Is.Zero);
    }

    [Test]
    public void GrowthBeyondTheControlByMoreThanTheBudgetFails() =>
        Assert.That(Controlled(Rising(1300, 78), Rising(1200, 75)).Verdict, Is.EqualTo("growth"));

    [Test]
    public void ReporterSeriesStillFailsAgainstAFlatControl() =>
        Assert.That(Controlled([274, 376, 511, 525, 520, 651, 778, 774, 903, 899, 888, 1019, 1136, 1274],
            Enumerable.Repeat(300L, 14).ToArray()).Verdict, Is.EqualTo("growth"));

    [Test]
    public void AShrinkingControlGivesNoAllowance()
    {
        var result = Controlled(Rising(200, 3), Rising(400, -3));
        Assert.That(result.ControlGrowth, Is.Zero);
        Assert.That(result.Verdict, Is.EqualTo("growth"));
    }

    [Test]
    public void AnInconclusiveControlBlocksAcceptance() =>
        Assert.That(Controlled(Enumerable.Repeat(200L, 14).ToArray(), Rising(200, 75), [99, 100, 140, 120, 100, 99]).Verdict,
            Is.EqualTo("inconclusive"));

    [Test]
    public void AnIncompleteControlBlocksAcceptance() =>
        Assert.That(RetentionAssessment.AssessAgainstControl(Assess(Enumerable.Repeat(200L, 14).ToArray()),
            Assess(Rising(200, 75).Take(13).ToArray(), logins: 13)).Verdict, Is.EqualTo("inconclusive"));
}

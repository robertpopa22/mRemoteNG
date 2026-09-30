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
}

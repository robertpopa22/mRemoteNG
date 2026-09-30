using System;
using System.Collections.Generic;
using System.Linq;

namespace mRemoteNGSpecs.Support;

/// <summary>A bounded regression budget, never a diagnosis or a promise of leak freedom.</summary>
public static class RetentionAssessment
{
    public const int RequiredCycles = 14;
    public const int RequiredSettleSeconds = 30;
    // Total growth across the run, independent of the first session's allocation.
    public const long PrivateBytesBudget = 32L * 1024 * 1024;
    public const long HandleBudget = 32;
    public const long GdiBudget = 16;

    public sealed record Result(string Verdict, long Growth, long IdleRange, long Budget);

    public static Result Assess(IReadOnlyList<long> idle, IReadOnlyList<long> closed,
        long budget, int completedLogins, int settleSeconds)
    {
        if (budget <= 0 || idle.Count < 6 || closed.Count < RequiredCycles ||
            completedLogins != closed.Count || settleSeconds < RequiredSettleSeconds ||
            idle.Any(x => x < 0) || closed.Any(x => x < 0))
            return new("inconclusive", 0, 0, budget);

        long noise = idle.Max() - idle.Min();
        // An unstable control cannot make a growing session series pass by widening its budget.
        if (noise > budget / 2)
            return new("inconclusive", 0, noise, budget);

        long growth = Median(closed.TakeLast(3)) - Median(closed.Take(3));
        // Also check first-to-last: a rising first window must not hide a slow accumulation.
        growth = Math.Max(growth, closed[^1] - closed[0]);
        return new(growth > budget ? "growth" : "within-budget", growth, noise, budget);
    }

    private static long Median(IEnumerable<long> values) => values.Order().ElementAt(1);
}

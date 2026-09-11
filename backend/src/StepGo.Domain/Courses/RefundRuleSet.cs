using StepGo.Domain.SharedKernel;

namespace StepGo.Domain.Courses;

/// <summary>One tier: courses cancelled at least <see cref="DaysBeforeCourseStart"/> days ahead refund at <see cref="RefundPercentage"/>.</summary>
public sealed record RefundTier
{
    public int DaysBeforeCourseStart { get; }
    public decimal RefundPercentage { get; }

    public RefundTier(int daysBeforeCourseStart, decimal refundPercentage)
    {
        if (refundPercentage < 0m || refundPercentage > 1m)
        {
            throw new DomainException("invalid_refund_percentage", "退費比例必須介於 0% 到 100% 之間。");
        }

        DaysBeforeCourseStart = daysBeforeCourseStart;
        RefundPercentage = refundPercentage;
    }
}

/// <summary>
/// Tiers sorted by <see cref="RefundTier.DaysBeforeCourseStart"/> descending. The tier applied for a
/// given "days before start" is the first tier whose threshold is &lt;= that value; below the smallest
/// threshold the refund percentage is implicitly 0%.
/// </summary>
public sealed class RefundRuleSet
{
    public IReadOnlyList<RefundTier> Tiers { get; }

    public RefundRuleSet(IEnumerable<RefundTier> tiers)
    {
        Tiers = tiers.OrderByDescending(t => t.DaysBeforeCourseStart).ToList();
        if (Tiers.Count == 0)
        {
            throw new DomainException("refund_rule_set_empty", "退費規則至少需要一個區間。");
        }
    }

    public decimal PercentageFor(int daysBeforeCourseStart)
    {
        foreach (var tier in Tiers)
        {
            if (daysBeforeCourseStart >= tier.DaysBeforeCourseStart)
            {
                return tier.RefundPercentage;
            }
        }

        return 0m;
    }

    /// <summary>
    /// Every tier's percentage must be &gt;= the platform floor's percentage for the same threshold.
    /// Throws naming the first violating threshold and the floor value, per spec scenario.
    /// </summary>
    public void ValidateAgainstFloor(RefundRuleSet platformFloor)
    {
        foreach (var tier in Tiers)
        {
            var floorPercentage = platformFloor.PercentageFor(tier.DaysBeforeCourseStart);
            if (tier.RefundPercentage < floorPercentage)
            {
                throw new DomainException(
                    "refund_rule_below_platform_floor",
                    $"開課前 {tier.DaysBeforeCourseStart} 日以上區間的退費比例（{tier.RefundPercentage:P0}）低於平台底線（{floorPercentage:P0}）。");
            }
        }
    }
}

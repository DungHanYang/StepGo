namespace StepGo.PricingRules;

/// <summary>Time-before-course-start tier used by the refund floor (design/design-handoff/README.md).</summary>
public enum RefundTier
{
    /// <summary>開課前 14 日以上.</summary>
    FourteenDaysOrMore,

    /// <summary>開課前 7–13 日.</summary>
    SevenToThirteenDays,

    /// <summary>開課前 1–6 日.</summary>
    OneToSixDays,

    /// <summary>開課後.</summary>
    AfterCourseStart,
}

/// <summary>
/// "平台底線 + 老師微調" refund rule (frontend-teacher-portal spec: "退費規則採平台底線加老師微調").
/// The floor values below are the platform default; in the real system these come from the
/// Admin fee-settings page (frontend-admin-panel) and may be adjusted with 30 days' notice —
/// the frontend still enforces "teacher rate SHALL NOT be lower than the floor" client-side.
/// </summary>
public static class RefundPolicy
{
    public static readonly IReadOnlyDictionary<RefundTier, decimal> DefaultPlatformFloor = new Dictionary<RefundTier, decimal>
    {
        [RefundTier.FourteenDaysOrMore] = 1.00m,
        [RefundTier.SevenToThirteenDays] = 0.50m,
        [RefundTier.OneToSixDays] = 0.15m,
        [RefundTier.AfterCourseStart] = 0.00m,
    };

    /// <summary>True when <paramref name="proposedRate"/> is at or above the platform floor for <paramref name="tier"/>.</summary>
    public static bool IsAllowed(RefundTier tier, decimal proposedRate, IReadOnlyDictionary<RefundTier, decimal>? platformFloor = null)
    {
        var floor = platformFloor ?? DefaultPlatformFloor;
        return proposedRate >= floor[tier];
    }
}

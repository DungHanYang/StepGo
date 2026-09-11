using StepGo.Domain.Courses;
using StepGo.Domain.SharedKernel;

namespace StepGo.Domain.Tests.Courses;

public class RefundRuleSetTests
{
    [Fact]
    public void ValidateAgainstFloor_WhenTierBelowFloor_Throws()
    {
        var platformFloor = new RefundRuleSet([new RefundTier(14, 1.0m)]);
        var teacherRules = new RefundRuleSet([new RefundTier(14, 0.8m)]);

        var ex = Assert.Throws<DomainException>(() => teacherRules.ValidateAgainstFloor(platformFloor));

        Assert.Equal("refund_rule_below_platform_floor", ex.Code);
        Assert.Contains("14", ex.Message);
    }

    [Fact]
    public void ValidateAgainstFloor_WhenTierAboveFloor_Passes()
    {
        var platformFloor = new RefundRuleSet([new RefundTier(14, 0.5m)]);
        var teacherRules = new RefundRuleSet([new RefundTier(14, 1.0m)]);

        teacherRules.ValidateAgainstFloor(platformFloor); // does not throw
    }

    [Fact]
    public void PercentageFor_BelowSmallestThreshold_IsZero()
    {
        var rules = new RefundRuleSet([new RefundTier(14, 1.0m), new RefundTier(7, 0.5m)]);

        Assert.Equal(0m, rules.PercentageFor(3));
        Assert.Equal(0.5m, rules.PercentageFor(7));
        Assert.Equal(1.0m, rules.PercentageFor(20));
    }
}

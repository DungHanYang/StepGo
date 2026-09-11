using StepGo.PricingRules;
using Xunit;

namespace StepGo.PricingRules.Tests;

public class RefundPolicyTests
{
    [Fact]
    public void Rate_Below_Floor_Is_Not_Allowed()
    {
        Assert.False(RefundPolicy.IsAllowed(RefundTier.FourteenDaysOrMore, 0.80m));
    }

    [Fact]
    public void Rate_Above_Floor_Is_Allowed()
    {
        Assert.True(RefundPolicy.IsAllowed(RefundTier.SevenToThirteenDays, 0.70m));
    }

    [Fact]
    public void Rate_Exactly_At_Floor_Is_Allowed()
    {
        Assert.True(RefundPolicy.IsAllowed(RefundTier.OneToSixDays, 0.15m));
    }
}

using StepGo.Domain.Courses;
using StepGo.Domain.Governance;
using StepGo.Domain.SharedKernel;

namespace StepGo.Domain.Tests.Governance;

public class PlatformFeeSettingTests
{
    private static readonly RefundRuleSet Floor = new([new RefundTier(14, 1.0m)]);

    [Fact]
    public void Propose_WithLessThan30DaysNotice_Throws()
    {
        var now = DateTimeOffset.UtcNow;

        var ex = Assert.Throws<DomainException>(() =>
            PlatformFeeSetting.Propose("v2", 0.10m, Money.FromWholeDollars(20), Floor, now.AddDays(20), Guid.NewGuid(), now));

        Assert.Equal("effective_date_notice_too_short", ex.Code);
    }

    [Fact]
    public void Propose_With30OrMoreDaysNotice_Succeeds()
    {
        var now = DateTimeOffset.UtcNow;

        var setting = PlatformFeeSetting.Propose("v2", 0.10m, Money.FromWholeDollars(20), Floor, now.AddDays(30), Guid.NewGuid(), now);

        Assert.Equal("v2", setting.Id);
        Assert.False(setting.IsEffectiveAt(now));
        Assert.True(setting.IsEffectiveAt(now.AddDays(31)));
    }
}

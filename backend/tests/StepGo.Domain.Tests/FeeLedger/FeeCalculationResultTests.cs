using StepGo.Domain.Courses;
using StepGo.Domain.FeeLedger;
using StepGo.Domain.SharedKernel;

namespace StepGo.Domain.Tests.FeeLedger;

public class FeeCalculationResultTests
{
    private static readonly FeeRateSchedule Schedule = new("v1", 0.0289m, Money.FromWholeDollars(15), 0.10m);

    [Fact]
    public void Calculate_ForCreditCardOrder_MatchesSpecExample()
    {
        // spec example: NT$3000, credit card 2.89%, platform 10% => gateway fee ~87, platform fee 300
        var result = FeeCalculationResult.Calculate(Money.FromWholeDollars(3000), PaymentMethod.CreditCard, Schedule);

        Assert.Equal(87, result.GatewayFee.Cents);
        Assert.Equal(300, result.PlatformServiceFee.Cents);
        Assert.Equal(3000 - 87 - 300, result.NetReceivable.Cents);
    }

    [Fact]
    public void Calculate_ForAtmOrder_UsesFlatGatewayFee()
    {
        var result = FeeCalculationResult.Calculate(Money.FromWholeDollars(2000), PaymentMethod.Atm, Schedule);

        Assert.Equal(15, result.GatewayFee.Cents);
        Assert.Equal(200, result.PlatformServiceFee.Cents);
    }

    [Fact]
    public void Calculate_BindsFeeScheduleVersionId()
    {
        var result = FeeCalculationResult.Calculate(Money.FromWholeDollars(1000), PaymentMethod.Atm, Schedule);

        Assert.Equal("v1", result.FeeScheduleVersionId);
    }

    [Fact]
    public void RateChangeAfterOrderCreated_DoesNotAffectAlreadyCalculatedResult()
    {
        var oldSchedule = new FeeRateSchedule("v1", 0.0289m, Money.FromWholeDollars(15), 0.10m);
        var newSchedule = new FeeRateSchedule("v2", 0.0289m, Money.FromWholeDollars(15), 0.15m);

        var oldResult = FeeCalculationResult.Calculate(Money.FromWholeDollars(3000), PaymentMethod.CreditCard, oldSchedule);
        _ = FeeCalculationResult.Calculate(Money.FromWholeDollars(3000), PaymentMethod.CreditCard, newSchedule);

        // the already-computed old result is a plain immutable record; recomputing under a new schedule never mutates it
        Assert.Equal(300, oldResult.PlatformServiceFee.Cents);
        Assert.Equal("v1", oldResult.FeeScheduleVersionId);
    }
}

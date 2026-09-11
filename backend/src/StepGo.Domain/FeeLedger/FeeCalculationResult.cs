using StepGo.Domain.SharedKernel;

namespace StepGo.Domain.FeeLedger;

public sealed record FeeCalculationResult(Money GrossAmount, Money GatewayFee, Money PlatformServiceFee, Money NetReceivable, string FeeScheduleVersionId)
{
    public static FeeCalculationResult Calculate(Money grossAmount, StepGo.Domain.Courses.PaymentMethod methodUsed, FeeRateSchedule schedule)
    {
        var gatewayFee = methodUsed == StepGo.Domain.Courses.PaymentMethod.CreditCard
            ? grossAmount.ApplyRate(schedule.CreditCardGatewayRate)
            : schedule.AtmGatewayFlatFee;

        var platformFee = grossAmount.ApplyRate(schedule.PlatformServiceFeeRate);
        var net = grossAmount - gatewayFee - platformFee;

        return new FeeCalculationResult(grossAmount, gatewayFee, platformFee, net, schedule.VersionId);
    }
}

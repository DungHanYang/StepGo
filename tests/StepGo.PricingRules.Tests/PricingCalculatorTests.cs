using StepGo.PricingRules;
using Xunit;

namespace StepGo.PricingRules.Tests;

public class PricingCalculatorTests
{
    // frontend-marketing-site spec: "此計算 SHALL NOT 呼叫任何後端 API" — PricingCalculator.Calculate
    // is a static method with only primitive parameters (no HttpClient/IHttpClientFactory
    // dependency anywhere in StepGo.PricingRules), so it is structurally incapable of making
    // a network call; these tests exercise it directly with no server/mock running.

    [Fact]
    public void Pure_Atm_Payment_Computes_Expected_Breakdown()
    {
        var result = PricingCalculator.Calculate(3000m, PaymentMethod.Atm, PayoutTransferType.SameBank);

        Assert.Equal(15m, result.GatewayFee);
        Assert.Equal(300m, result.PlatformFee); // 10% of 3000
        Assert.Equal(10m, result.PayoutTransferFee);
        Assert.Equal(2675m, result.TeacherNetAmount); // 3000 - 15 - 300 - 10
    }

    [Fact]
    public void Pure_CreditCard_Payment_Computes_Expected_Breakdown()
    {
        var result = PricingCalculator.Calculate(3000m, PaymentMethod.CreditCard, PayoutTransferType.SameBank);

        Assert.Equal(87m, result.GatewayFee); // round(3000 * 0.0289) = round(86.7) = 87
        Assert.Equal(300m, result.PlatformFee);
        Assert.Equal(10m, result.PayoutTransferFee);
        Assert.Equal(2603m, result.TeacherNetAmount); // 3000 - 87 - 300 - 10
    }

    [Fact]
    public void Switching_Payment_Method_Recomputes_Gateway_Fee_Only()
    {
        const decimal price = 3000m;

        var atm = PricingCalculator.Calculate(price, PaymentMethod.Atm);
        var creditCard = PricingCalculator.Calculate(price, PaymentMethod.CreditCard);

        Assert.NotEqual(atm.GatewayFee, creditCard.GatewayFee);
        Assert.Equal(atm.PlatformFee, creditCard.PlatformFee);
        Assert.Equal(atm.PayoutTransferFee, creditCard.PayoutTransferFee);
    }

    [Fact]
    public void CrossBank_Transfer_Uses_Higher_Payout_Fee()
    {
        var sameBank = PricingCalculator.Calculate(3000m, PaymentMethod.Atm, PayoutTransferType.SameBank);
        var crossBank = PricingCalculator.Calculate(3000m, PaymentMethod.Atm, PayoutTransferType.CrossBank);

        Assert.Equal(10m, sameBank.PayoutTransferFee);
        Assert.Equal(20m, crossBank.PayoutTransferFee);
        Assert.True(crossBank.TeacherNetAmount < sameBank.TeacherNetAmount);
    }

    [Fact]
    public void Negative_Price_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PricingCalculator.Calculate(-1m, PaymentMethod.Atm));
    }
}

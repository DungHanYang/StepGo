using StepGo.Shared.Domain.FeeLedger;
using StepGo.Shared.Domain;

namespace StepGo.Domain.Tests.FeeLedger;

public class TransferFeeCalculatorTests
{
    [Fact]
    public void Calculate_SameBank_IsZero()
    {
        var fee = TransferFeeCalculator.Calculate(isSameBankAsPlatform: true, Money.FromWholeDollars(20));

        Assert.Equal(0, fee.Cents);
    }

    [Fact]
    public void Calculate_CrossBank_ChargesConfiguredFee()
    {
        var fee = TransferFeeCalculator.Calculate(isSameBankAsPlatform: false, Money.FromWholeDollars(20));

        Assert.Equal(20, fee.Cents);
    }
}

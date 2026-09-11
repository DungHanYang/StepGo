using StepGo.Domain.SharedKernel;

namespace StepGo.Domain.FeeLedger;

/// <summary>Payout-batch-level transfer fee (charged once per batch, never allocated per order).</summary>
public static class TransferFeeCalculator
{
    public static Money Calculate(bool isSameBankAsPlatform, Money crossBankFee)
        => isSameBankAsPlatform ? Money.Zero : crossBankFee;
}

namespace StepGo.PricingRules;

/// <summary>
/// Pure, offline fee calculation — frontend-marketing-site spec: "費用試算工具為前端純計算"
/// (SHALL NOT call any backend API), and reused by the teacher course-creation wizard's
/// live pricing step (frontend-teacher-portal spec).
/// </summary>
public static class PricingCalculator
{
    public static PricingBreakdown Calculate(decimal price, PaymentMethod paymentMethod, PayoutTransferType transferType = PayoutTransferType.SameBank)
    {
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), price, "Price must not be negative.");
        }

        var gatewayFee = paymentMethod switch
        {
            PaymentMethod.CreditCard => Math.Round(price * PricingConstants.CreditCardFeeRate, 0, MidpointRounding.AwayFromZero),
            PaymentMethod.Atm => PricingConstants.AtmFeePerTransaction,
            _ => throw new ArgumentOutOfRangeException(nameof(paymentMethod), paymentMethod, null),
        };

        // Platform fee is computed on the original list price, not the net amount.
        var platformFee = Math.Round(price * PricingConstants.PlatformFeeRate, 0, MidpointRounding.AwayFromZero);

        var payoutTransferFee = transferType switch
        {
            PayoutTransferType.SameBank => PricingConstants.PayoutTransferFeeSameBank,
            PayoutTransferType.CrossBank => PricingConstants.PayoutTransferFeeCrossBank,
            _ => throw new ArgumentOutOfRangeException(nameof(transferType), transferType, null),
        };

        var teacherNetAmount = price - gatewayFee - platformFee - payoutTransferFee;

        return new PricingBreakdown(price, paymentMethod, transferType, gatewayFee, platformFee, payoutTransferFee, teacherNetAmount);
    }
}

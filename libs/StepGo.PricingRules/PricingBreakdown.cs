namespace StepGo.PricingRules;

/// <summary>Itemized result of <see cref="PricingCalculator.Calculate"/>.</summary>
public sealed record PricingBreakdown(
    decimal Price,
    PaymentMethod PaymentMethod,
    PayoutTransferType TransferType,
    decimal GatewayFee,
    decimal PlatformFee,
    decimal PayoutTransferFee,
    decimal TeacherNetAmount);

using StepGo.Domain.SharedKernel;

namespace StepGo.Domain.FeeLedger;

/// <summary>
/// The versioned rate schedule in effect when an order's fees are calculated. VersionId is persisted on the
/// order (see Order.FeeScheduleVersionId) so later rate changes never recompute historical orders.
/// </summary>
public sealed record FeeRateSchedule(string VersionId, decimal CreditCardGatewayRate, Money AtmGatewayFlatFee, decimal PlatformServiceFeeRate);

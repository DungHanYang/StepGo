using StepGo.PricingRules;

namespace StepGo.AdminPanel.Models;

/// <summary>Admin-editable mirror of <see cref="StepGo.PricingRules.PricingConstants"/> /
/// <see cref="RefundPolicy"/> — in the real system these values flow back to override those
/// frontend constants via the backend change; this scaffold just models the settings + audit log.</summary>
public sealed class AdminFeeSettings
{
    public decimal PlatformFeeRate { get; set; } = PricingConstants.PlatformFeeRate;
    public decimal PayoutTransferFeeSameBank { get; set; } = PricingConstants.PayoutTransferFeeSameBank;
    public decimal PayoutTransferFeeCrossBank { get; set; } = PricingConstants.PayoutTransferFeeCrossBank;
    public decimal RefundProcessingFee { get; set; } = PricingConstants.RefundProcessingFee;
    public Dictionary<RefundTier, decimal> RefundFloor { get; set; } = new(RefundPolicy.DefaultPlatformFloor);
}

public sealed record FeeChangeLogEntry(string Summary, DateOnly EffectiveDate, string ChangedBy, DateTimeOffset ChangedAt);

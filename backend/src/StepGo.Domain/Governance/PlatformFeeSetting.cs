using StepGo.Domain.Courses;
using StepGo.Domain.SharedKernel;

namespace StepGo.Domain.Governance;

/// <summary>
/// A versioned snapshot of platform-wide rates and the refund floor. Any change requires an effective
/// date at least 30 days out from when it is proposed; earlier orders keep referencing the version
/// active when they were created (see StepGo.Domain.FeeLedger.FeeRateSchedule.VersionId binding).
/// </summary>
public sealed class PlatformFeeSetting : AggregateRoot<string>
{
    public const int MinimumNoticeDays = 30;

    public decimal PlatformServiceFeeRate { get; }
    public Money CrossBankTransferFee { get; }
    public RefundRuleSet RefundFloor { get; }
    public DateTimeOffset EffectiveDate { get; }
    public Guid ProposedBy { get; }
    public DateTimeOffset ProposedAt { get; }

    private PlatformFeeSetting(
        string versionId, decimal platformServiceFeeRate, Money crossBankTransferFee, RefundRuleSet refundFloor,
        DateTimeOffset effectiveDate, Guid proposedBy, DateTimeOffset proposedAt)
        : base(versionId)
    {
        PlatformServiceFeeRate = platformServiceFeeRate;
        CrossBankTransferFee = crossBankTransferFee;
        RefundFloor = refundFloor;
        EffectiveDate = effectiveDate;
        ProposedBy = proposedBy;
        ProposedAt = proposedAt;
    }

    public static PlatformFeeSetting Propose(
        string versionId, decimal platformServiceFeeRate, Money crossBankTransferFee, RefundRuleSet refundFloor,
        DateTimeOffset effectiveDate, Guid proposedBy, DateTimeOffset now)
    {
        if (effectiveDate < now.AddDays(MinimumNoticeDays))
        {
            throw new DomainException("effective_date_notice_too_short", $"生效日必須在發布當下起算 {MinimumNoticeDays} 天後，不得提前生效。");
        }

        return new PlatformFeeSetting(versionId, platformServiceFeeRate, crossBankTransferFee, refundFloor, effectiveDate, proposedBy, now);
    }

    public static PlatformFeeSetting Rehydrate(
        string versionId, decimal platformServiceFeeRate, Money crossBankTransferFee, RefundRuleSet refundFloor,
        DateTimeOffset effectiveDate, Guid proposedBy, DateTimeOffset proposedAt)
        => new(versionId, platformServiceFeeRate, crossBankTransferFee, refundFloor, effectiveDate, proposedBy, proposedAt);

    public bool IsEffectiveAt(DateTimeOffset now) => now >= EffectiveDate;
}

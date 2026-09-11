using StepGo.Domain.SharedKernel;

namespace StepGo.Domain.Payouts;

public sealed class PayoutBatch : AggregateRoot<Guid>
{
    public static readonly Money MinimumPayoutThreshold = Money.FromWholeDollars(500);

    public Guid TeacherId { get; private set; }
    public string PeriodYyyyMm { get; private set; }
    public IReadOnlyList<PayoutBatchOrderLine> OrderLines { get; private set; }
    public Money NetReceivableTotal { get; private set; }
    public Money TransferFee { get; private set; }
    public Money NetPayout { get; private set; }
    public PayoutBatchStatus Status { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public string? TransferReference { get; private set; }
    public string? BankRejectionReason { get; private set; }

    private PayoutBatch(
        Guid id, Guid teacherId, string periodYyyyMm, IReadOnlyList<PayoutBatchOrderLine> orderLines,
        Money netReceivableTotal, Money transferFee)
        : base(id)
    {
        TeacherId = teacherId;
        PeriodYyyyMm = periodYyyyMm;
        OrderLines = orderLines;
        NetReceivableTotal = netReceivableTotal;
        TransferFee = transferFee;
        NetPayout = netReceivableTotal - transferFee;
        Status = PayoutBatchStatus.Draft;
    }

    /// <summary>
    /// Returns null when the teacher's total for the period is below the NT$500 minimum threshold —
    /// those orders stay pending and roll into the next period's calculation.
    /// </summary>
    public static PayoutBatch? TryDraft(
        Guid id, Guid teacherId, string periodYyyyMm, IReadOnlyList<PayoutBatchOrderLine> eligibleOrderLines, Money transferFee)
    {
        var total = eligibleOrderLines.Aggregate(Money.Zero, (sum, line) => sum + line.NetReceivableSnapshot);
        if (total < MinimumPayoutThreshold)
        {
            return null;
        }

        return new PayoutBatch(id, teacherId, periodYyyyMm, eligibleOrderLines, total, transferFee);
    }

    public static PayoutBatch Rehydrate(
        Guid id, Guid teacherId, string periodYyyyMm, IReadOnlyList<PayoutBatchOrderLine> orderLines,
        Money netReceivableTotal, Money transferFee, PayoutBatchStatus status, DateTimeOffset? paidAt,
        string? transferReference, string? bankRejectionReason)
    {
        var batch = new PayoutBatch(id, teacherId, periodYyyyMm, orderLines, netReceivableTotal, transferFee)
        {
            Status = status,
            PaidAt = paidAt,
            TransferReference = transferReference,
            BankRejectionReason = bankRejectionReason,
        };
        return batch;
    }

    /// <summary>Payout account holder name must match the teacher's verified real name before disbursing.</summary>
    public void GuardPayoutAccountNameMatches(string payoutAccountHolderName, string teacherRealName)
    {
        if (!string.Equals(payoutAccountHolderName.Trim(), teacherRealName.Trim(), StringComparison.Ordinal))
        {
            throw new DomainException("payout_account_name_mismatch", "撥款帳戶戶名與已驗證的真實姓名不一致，暫停本次撥款。");
        }
    }

    public void MarkPaid(string transferReference, DateTimeOffset paidAt)
    {
        Status = PayoutBatchStatus.Paid;
        TransferReference = transferReference;
        PaidAt = paidAt;
        BankRejectionReason = null;
    }

    public void MarkBankRejected(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("bank_rejection_reason_required", "標記銀行退回時必須記錄退回原因。");
        }

        Status = PayoutBatchStatus.BankRejected;
        BankRejectionReason = reason;
    }
}

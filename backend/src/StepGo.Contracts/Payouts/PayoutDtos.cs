namespace StepGo.Contracts.Payouts;

public enum PayoutBatchStatusDto
{
    Draft,
    Paid,
    BankRejected,
}

public sealed record PayoutBatchOrderLineDto(Guid OrderId, long NetReceivableSnapshot);

public sealed record PayoutBatchDto(
    Guid Id, Guid TeacherId, string PeriodYyyyMm, IReadOnlyList<PayoutBatchOrderLineDto> OrderLines,
    long NetReceivableTotal, long TransferFee, long NetPayout, PayoutBatchStatusDto Status,
    DateTimeOffset? PaidAt, string? TransferReference, string? BankRejectionReason);

public sealed record MarkBankRejectedRequestDto(string Reason);

public sealed record MarkPayoutBatchPaidRequestDto(string TransferReference);

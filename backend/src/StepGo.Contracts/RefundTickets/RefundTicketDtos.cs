namespace StepGo.Contracts.RefundTickets;

public enum RefundTicketStatusDto
{
    AutoRejected,
    TeacherReviewing,
    TeacherRejected,
    AdminArbitration,
    ApprovedTerminal,
    RejectedTerminal,
}

public enum DeductionSourceDto
{
    PlatformAccountDirect,
    NextTeacherPayout,
}

public sealed record SubmitRefundTicketRequestDto(Guid OrderId);

public sealed record ThreadMessageDto(string AuthorRole, Guid AuthorId, string Text, DateTimeOffset CreatedAt);

public sealed record InternalNoteDto(Guid AdminId, string Text, DateTimeOffset CreatedAt);

public sealed record RefundTicketDto(
    Guid Id, Guid OrderId, Guid StudentId, Guid TeacherId, long OriginalPaidAmount, long EligibleRefundAmount,
    RefundTicketStatusDto Status, DateTimeOffset? TeacherSlaDeadline, string? RejectionReason,
    long? DecidedAmount, DeductionSourceDto? DecidedDeductionSource, IReadOnlyList<ThreadMessageDto> Thread);

public sealed record TeacherRefundDecisionRequestDto(bool Approve, long? ApprovedAmount, string? RejectionReason);

public sealed record AdminArbitrationRequestDto(bool Approve, long? ApprovedAmount);

public sealed record AddThreadMessageRequestDto(string Text);

public sealed record AddInternalNoteRequestDto(string Text);

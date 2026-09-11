using StepGo.Domain.Identity;
using StepGo.Domain.SharedKernel;

namespace StepGo.Domain.RefundTickets;

public sealed class RefundTicket : AggregateRoot<Guid>
{
    /// <summary>Teacher SLA: 5 business days to respond once in TeacherReviewing.</summary>
    public const int TeacherResponseSlaBusinessDays = 5;

    public Guid OrderId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid TeacherId { get; private set; }
    public Money OriginalPaidAmount { get; private set; }
    public Money EligibleRefundAmount { get; private set; }
    public RefundTicketStatus Status { get; private set; }
    public DateTimeOffset? TeacherSlaDeadline { get; private set; }
    public string? RejectionReason { get; private set; }
    public Money? DecidedAmount { get; private set; }
    public DeductionSource? DecidedDeductionSource { get; private set; }
    public StepGo.Domain.Orders.RefundRoute? RefundExecutionRoute { get; private set; }
    public string? ManualTransferReference { get; private set; }
    public DateTimeOffset? ManualTransferCompletedAt { get; private set; }

    private readonly List<ThreadMessage> _thread = [];
    public IReadOnlyList<ThreadMessage> Thread => _thread;

    private readonly List<InternalNote> _internalNotes = [];
    public IReadOnlyList<InternalNote> InternalNotes => _internalNotes;

    private RefundTicket(Guid id, Guid orderId, Guid studentId, Guid teacherId, Money originalPaidAmount, Money eligibleRefundAmount)
        : base(id)
    {
        OrderId = orderId;
        StudentId = studentId;
        TeacherId = teacherId;
        OriginalPaidAmount = originalPaidAmount;
        EligibleRefundAmount = eligibleRefundAmount;
    }

    /// <summary>
    /// Auto-calculates the eligible amount from the refund percentage for "days before course start".
    /// A 0% result auto-rejects the ticket while still leaving the appeal-to-arbitration path open.
    /// </summary>
    public static RefundTicket Submit(
        Guid id, Guid orderId, Guid studentId, Guid teacherId, Money originalPaidAmount,
        decimal refundPercentage, IBusinessDayCalendar calendar, DateTimeOffset now)
    {
        var eligibleAmount = originalPaidAmount.ApplyRate(refundPercentage);
        var ticket = new RefundTicket(id, orderId, studentId, teacherId, originalPaidAmount, eligibleAmount);

        if (refundPercentage <= 0m)
        {
            ticket.Status = RefundTicketStatus.AutoRejected;
        }
        else
        {
            ticket.Status = RefundTicketStatus.TeacherReviewing;
            ticket.TeacherSlaDeadline = calendar.AddBusinessDays(now, TeacherResponseSlaBusinessDays);
        }

        return ticket;
    }

    public static RefundTicket Rehydrate(
        Guid id, Guid orderId, Guid studentId, Guid teacherId, Money originalPaidAmount, Money eligibleRefundAmount,
        RefundTicketStatus status, DateTimeOffset? teacherSlaDeadline, string? rejectionReason,
        Money? decidedAmount, DeductionSource? decidedDeductionSource, StepGo.Domain.Orders.RefundRoute? refundExecutionRoute,
        string? manualTransferReference, DateTimeOffset? manualTransferCompletedAt,
        IEnumerable<ThreadMessage> thread, IEnumerable<InternalNote> internalNotes)
    {
        var ticket = new RefundTicket(id, orderId, studentId, teacherId, originalPaidAmount, eligibleRefundAmount)
        {
            Status = status,
            TeacherSlaDeadline = teacherSlaDeadline,
            RejectionReason = rejectionReason,
            DecidedAmount = decidedAmount,
            DecidedDeductionSource = decidedDeductionSource,
            RefundExecutionRoute = refundExecutionRoute,
            ManualTransferReference = manualTransferReference,
            ManualTransferCompletedAt = manualTransferCompletedAt,
        };
        ticket._thread.AddRange(thread);
        ticket._internalNotes.AddRange(internalNotes);
        return ticket;
    }

    public void AddThreadMessage(Role authorRole, Guid authorId, string text, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DomainException("thread_message_required", "留言內容不得為空。");
        }

        _thread.Add(new ThreadMessage(authorRole, authorId, text.Trim(), now));
    }

    public void AddInternalNote(Guid adminId, string text, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DomainException("internal_note_required", "內部備註內容不得為空。");
        }

        _internalNotes.Add(new InternalNote(adminId, text.Trim(), now));
    }

    /// <summary>Approved amount is capped at the order's original payment — never above it.</summary>
    public void TeacherApprove(Money amount, Guid orderStudentId, Guid orderTeacherId, DateTimeOffset now)
    {
        GuardStatus(RefundTicketStatus.TeacherReviewing);

        if (amount > OriginalPaidAmount)
        {
            throw new DomainException("refund_amount_exceeds_original_payment", "核准金額不得超過訂單原始繳費金額。");
        }

        DecideApproved(amount, now, teacherResolved: true);
    }

    public void TeacherReject(string reason)
    {
        GuardStatus(RefundTicketStatus.TeacherReviewing);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("rejection_reason_required", "駁回退課申請時必須填寫理由。");
        }

        Status = RefundTicketStatus.TeacherRejected;
        RejectionReason = reason;
    }

    /// <summary>Called by the SLA Step Functions execution once the teacher-response window has elapsed unanswered.</summary>
    public void AutoEscalateOnSlaTimeout(DateTimeOffset now)
    {
        if (Status != RefundTicketStatus.TeacherReviewing)
        {
            return;
        }

        if (TeacherSlaDeadline is null || now < TeacherSlaDeadline)
        {
            throw new DomainException("sla_not_yet_expired", "尚未達到老師回應期限，不可自動升級。");
        }

        Status = RefundTicketStatus.AdminArbitration;
        Raise(new RefundTicketEscalatedToArbitrationEvent(Id, OrderId, now));
    }

    /// <summary>Student-initiated appeal, available from AutoRejected or TeacherRejected — never re-entrant once escalated.</summary>
    public void EscalateToArbitration(DateTimeOffset now)
    {
        if (Status != RefundTicketStatus.AutoRejected && Status != RefundTicketStatus.TeacherRejected)
        {
            throw new DomainException("cannot_escalate_from_current_status", "目前工單狀態不可申訴升級仲裁。");
        }

        Status = RefundTicketStatus.AdminArbitration;
        Raise(new RefundTicketEscalatedToArbitrationEvent(Id, OrderId, now));
    }

    /// <summary>Admin arbitration is final: no further escalation or appeal is possible afterward.</summary>
    public void AdminArbitrate(bool approve, Money amount, DateTimeOffset now)
    {
        GuardStatus(RefundTicketStatus.AdminArbitration);

        if (approve)
        {
            if (amount > OriginalPaidAmount)
            {
                throw new DomainException("refund_amount_exceeds_original_payment", "裁決核准金額不得超過訂單原始繳費金額。");
            }

            DecideApproved(amount, now, teacherResolved: false);
        }
        else
        {
            Status = RefundTicketStatus.RejectedTerminal;
        }
    }

    private void DecideApproved(Money amount, DateTimeOffset now, bool teacherResolved)
    {
        DecidedAmount = amount;
        Status = RefundTicketStatus.ApprovedTerminal;
        _pendingApprovalEventTimestamp = now;
    }

    private DateTimeOffset? _pendingApprovalEventTimestamp;

    /// <summary>
    /// Called by the application handler once it has looked up whether the order was already paid out —
    /// finalizes the deduction source and raises RefundApprovedEvent (deferred until this is known).
    /// </summary>
    public void SetDeductionSource(DeductionSource source)
    {
        DecidedDeductionSource = source;

        if (DecidedAmount is { } amount && _pendingApprovalEventTimestamp is { } occurredAt)
        {
            Raise(new RefundApprovedEvent(Id, OrderId, StudentId, TeacherId, amount, source, occurredAt));
            _pendingApprovalEventTimestamp = null;
        }
    }

    /// <summary>Records which route the refund must execute through, per Order.DetermineRefundRoute().</summary>
    public void SetRefundExecutionRoute(StepGo.Domain.Orders.RefundRoute route) => RefundExecutionRoute = route;

    /// <summary>ATM/manual-transfer refunds are completed only once an admin records the transfer proof.</summary>
    public void MarkManualTransferCompleted(string transferReference, DateTimeOffset now)
    {
        if (RefundExecutionRoute != StepGo.Domain.Orders.RefundRoute.ManualBankTransferPending)
        {
            throw new DomainException("not_a_manual_transfer_refund", "此工單的退款方式不是待人工轉帳。");
        }

        if (string.IsNullOrWhiteSpace(transferReference))
        {
            throw new DomainException("transfer_reference_required", "回填人工轉帳憑證時必須提供流水號。");
        }

        ManualTransferReference = transferReference;
        ManualTransferCompletedAt = now;
    }

    private void GuardStatus(RefundTicketStatus required)
    {
        if (Status != required)
        {
            throw new DomainException("invalid_ticket_status_transition", $"工單目前狀態（{Status}）不允許此操作。");
        }
    }
}

using StepGo.UI.Status;

namespace StepGo.TeacherPortal.Models;

/// <summary>
/// Mutable (not a record) because approving/rejecting updates it in place inside
/// <see cref="Services.TeacherDataStore"/> — frontend-teacher-portal spec: "退課工單審核與金額微調限制".
/// </summary>
public sealed class RefundTicket
{
    public required string Id { get; init; }
    public required string OrderId { get; init; }
    public required string StudentName { get; init; }
    public required string CourseName { get; init; }
    public required decimal OriginalPaymentAmount { get; init; }
    public required decimal SystemCalculatedRefundAmount { get; init; }
    public required DateTimeOffset SlaDeadline { get; init; }
    public required string Reason { get; init; }

    public RefundTicketStatus Status { get; set; } = RefundTicketStatus.PendingTeacherReview;
    public decimal? TeacherAdjustedAmount { get; set; }
    public string? TeacherDecisionReason { get; set; }

    public TimeSpan TimeRemaining(DateTimeOffset now) => SlaDeadline - now;
}

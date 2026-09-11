using StepGo.UI.Status;

namespace StepGo.ApiClient.Dtos;

/// <summary>frontend-teacher-portal spec: "退課工單審核與金額微調限制"; frontend-student-portal
/// spec: "退課申請與正式留言串".</summary>
public sealed record RefundTicketDto(
    string Id,
    string OrderId,
    string StudentName,
    string CourseName,
    decimal OriginalPaymentAmount,
    decimal SystemCalculatedRefundAmount,
    RefundTicketStatus Status,
    DateTimeOffset SlaDeadline,
    string Reason,
    decimal? TeacherAdjustedAmount,
    string? TeacherDecisionReason);

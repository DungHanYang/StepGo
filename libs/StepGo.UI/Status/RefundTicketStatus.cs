namespace StepGo.UI.Status;

/// <summary>
/// Refund ticket lifecycle — a multi-stage flow (frontend-teacher-portal /
/// frontend-student-portal specs), driven by the business rule that teachers
/// must respond within 5 business days or the system auto-approves the refund,
/// and unresolved disputes escalate to platform arbitration (final decision).
/// </summary>
public enum RefundTicketStatus
{
    /// <summary>老師審核中 — within the 5-business-day SLA window.</summary>
    PendingTeacherReview,

    /// <summary>老師已核准.</summary>
    TeacherApproved,

    /// <summary>老師已駁回.</summary>
    TeacherRejected,

    /// <summary>逾期系統自動核定退費.</summary>
    AutoApproved,

    /// <summary>已升級平台仲裁.</summary>
    Escalated,

    /// <summary>仲裁已裁決 — final decision recorded, payout/settlement pending.</summary>
    ArbitrationResolved,

    /// <summary>已完成 — refund settled or ticket closed.</summary>
    Completed,
}

public static class RefundTicketStatusExtensions
{
    public static (string Label, StatusTagVariant Variant) ToDisplay(this RefundTicketStatus status) => status switch
    {
        RefundTicketStatus.PendingTeacherReview => ("老師審核中", StatusTagVariant.Warning),
        RefundTicketStatus.TeacherApproved => ("老師已核准", StatusTagVariant.Positive),
        RefundTicketStatus.TeacherRejected => ("老師已駁回", StatusTagVariant.Negative),
        RefundTicketStatus.AutoApproved => ("逾期系統自動核定", StatusTagVariant.Info),
        RefundTicketStatus.Escalated => ("已升級平台仲裁", StatusTagVariant.Warning),
        RefundTicketStatus.ArbitrationResolved => ("仲裁已裁決", StatusTagVariant.Info),
        RefundTicketStatus.Completed => ("已完成", StatusTagVariant.Neutral),
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };
}

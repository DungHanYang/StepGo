using StepGo.PricingRules;
using StepGo.UI.Status;

namespace StepGo.ApiClient.Dtos;

/// <summary>
/// Shared order shape (task 8.2: "將 StepGo.ApiClient 的 DTO 對齊 specs 中列出的欄位"),
/// covering the fields used by both frontend-teacher-portal (revenue/roster) and
/// frontend-student-portal ("我的課程" tab classification, checkout results).
/// </summary>
public sealed record OrderDto(
    string Id,
    string StudentName,
    string CourseId,
    string CourseName,
    string TeacherName,
    decimal Amount,
    PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus,
    DateTimeOffset OrderedAt,
    DateTimeOffset? NextSessionAt,
    bool CourseEnded,
    RefundTicketStatus? RefundTicketStatus,
    string? AtmVirtualAccount,
    DateTimeOffset? AtmPaymentDeadline);

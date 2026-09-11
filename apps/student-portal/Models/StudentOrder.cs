using StepGo.PricingRules;
using StepGo.UI.Status;

namespace StepGo.StudentPortal.Models;

/// <summary>
/// frontend-student-portal spec: "我的課程」四分頁...純粹依訂單的付款狀態欄位與場次時間欄位
/// （及退款工單完成狀態）篩選呈現，SHALL NOT 依賴任何額外新增的狀態欄位" — no separate "tab"
/// field exists; <see cref="Services.MyCoursesTabClassifier"/> derives the tab from these.
/// </summary>
public sealed record StudentOrder(
    string Id,
    string CourseId,
    string CourseName,
    string TeacherName,
    decimal Amount,
    PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus,
    DateTimeOffset? NextSessionAt,
    bool CourseEnded,
    RefundTicketStatus? RefundTicketStatus,
    DateTimeOffset OrderedAt,
    string? AtmVirtualAccount = null,
    DateTimeOffset? AtmPaymentDeadline = null);

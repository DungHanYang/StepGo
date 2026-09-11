using StepGo.ApiClient.Dtos;
using StepGo.PricingRules;
using StepGo.UI.Status;

namespace StepGo.MockApi.Scenarios;

/// <summary>
/// One switchable mock scenario per state of the payment state machine (5 states) —
/// design.md decision 8: "可切換「審核中／未通過／通過」三種假資料情境，直接對應四個 UI 狀態"
/// generalized to all four state machines this Mock API covers (task 8.1).
/// </summary>
public static class PaymentScenarios
{
    public static readonly IReadOnlyDictionary<string, OrderDto> All = new Dictionary<string, OrderDto>(StringComparer.OrdinalIgnoreCase)
    {
        ["pending"] = new(
            "o-mock-pending", "王小明", "c-paint-kids", "兒童繪畫班", "陳老師", 3200m,
            PaymentMethod.Atm, PaymentStatus.Pending, DateTimeOffset.UtcNow.AddHours(-1),
            NextSessionAt: DateTimeOffset.UtcNow.AddDays(10), CourseEnded: false, RefundTicketStatus: null,
            AtmVirtualAccount: "9527-1234-5678", AtmPaymentDeadline: DateTimeOffset.UtcNow.AddDays(3)),

        ["processing"] = new(
            "o-mock-processing", "陳小美", "c-paint-kids", "兒童繪畫班", "陳老師", 3200m,
            PaymentMethod.CreditCard, PaymentStatus.Processing, DateTimeOffset.UtcNow.AddMinutes(-2),
            NextSessionAt: DateTimeOffset.UtcNow.AddDays(10), CourseEnded: false, RefundTicketStatus: null,
            AtmVirtualAccount: null, AtmPaymentDeadline: null),

        ["paid"] = new(
            "o-mock-paid", "李小華", "c-paint-kids", "兒童繪畫班", "陳老師", 3200m,
            PaymentMethod.CreditCard, PaymentStatus.Paid, DateTimeOffset.UtcNow.AddDays(-5),
            NextSessionAt: DateTimeOffset.UtcNow.AddDays(3), CourseEnded: false, RefundTicketStatus: null,
            AtmVirtualAccount: null, AtmPaymentDeadline: null),

        ["failed"] = new(
            "o-mock-failed", "張小芳", "c-paint-kids", "兒童繪畫班", "陳老師", 3200m,
            PaymentMethod.CreditCard, PaymentStatus.Failed, DateTimeOffset.UtcNow.AddMinutes(-5),
            NextSessionAt: null, CourseEnded: false, RefundTicketStatus: null,
            AtmVirtualAccount: null, AtmPaymentDeadline: null),

        ["refunded"] = new(
            "o-mock-refunded", "林小強", "c-paint-kids", "兒童繪畫班", "陳老師", 3200m,
            PaymentMethod.CreditCard, PaymentStatus.Refunded, DateTimeOffset.UtcNow.AddDays(-60),
            NextSessionAt: null, CourseEnded: true, RefundTicketStatus: StepGo.UI.Status.RefundTicketStatus.Completed,
            AtmVirtualAccount: null, AtmPaymentDeadline: null),
    };
}

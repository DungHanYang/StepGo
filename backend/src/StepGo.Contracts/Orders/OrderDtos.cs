using StepGo.Contracts.Courses;

namespace StepGo.Contracts.Orders;

public enum ChoosePaymentDto
{
    Atm,
    Credit,
    All,
}

public enum OrderPaymentStatusDto
{
    PendingPayment,
    Paid,
    Overdue,
    Refunded,
}

public enum OrderPayoutStatusDto
{
    NotEligible,
    PendingPayout,
    Batched,
    PaidOut,
}

public sealed record CreateOrderRequestDto(Guid CourseId, PaymentMethodDto RequestedPaymentMethod);

public sealed record OrderDto(
    Guid Id, Guid CourseId, Guid TeacherId, Guid StudentId, long CoursePriceAtOrder,
    PaymentMethodDto RequestedPaymentMethod, ChoosePaymentDto GatewayChoosePayment,
    PaymentMethodDto? PaymentMethodUsed, OrderPaymentStatusDto PaymentStatus, OrderPayoutStatusDto PayoutStatus,
    DateTimeOffset? AtmPaymentDueAt, DateTimeOffset CreatedAt);

/// <summary>Raw payload relayed from the payment gateway's background-notification webhook.</summary>
public sealed record PaymentGatewayNotificationDto(
    string Gateway, string MerchantTradeNo, string GatewayTransactionId, string RtnCode, IReadOnlyDictionary<string, string> RawFields);

using StepGo.Shared.Domain;

namespace StepGo.Orders.Domain;

public sealed record OrderPaymentConfirmedEvent(Guid OrderId, Guid StudentId, Guid TeacherId, Money AmountPaid, DateTimeOffset OccurredAt) : IDomainEvent
{
    public string EventBridgeDetailType => "OrderPaymentConfirmed";
}

public sealed record OrderPaymentOverdueEvent(Guid OrderId, Guid StudentId, DateTimeOffset OccurredAt) : IDomainEvent
{
    public string EventBridgeDetailType => "OrderPaymentOverdue";
}

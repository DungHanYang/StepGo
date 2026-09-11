using StepGo.Domain.SharedKernel;

namespace StepGo.Domain.RefundTickets;

public sealed record RefundApprovedEvent(Guid TicketId, Guid OrderId, Guid StudentId, Guid TeacherId, Money ApprovedAmount, DeductionSource DeductionSource, DateTimeOffset OccurredAt) : IDomainEvent
{
    public string EventBridgeDetailType => "RefundApproved";
}

public sealed record RefundTicketEscalatedToArbitrationEvent(Guid TicketId, Guid OrderId, DateTimeOffset OccurredAt) : IDomainEvent
{
    public string EventBridgeDetailType => "RefundTicketEscalatedToArbitration";
}

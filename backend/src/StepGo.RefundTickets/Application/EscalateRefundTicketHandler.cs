using StepGo.Shared.Application;
using StepGo.RefundTickets.Domain;
using StepGo.Shared.Domain;

namespace StepGo.RefundTickets.Application;

/// <summary>Student-initiated appeal (task 7.1's "保留申訴入口") and the SLA Step Functions callback (task 7.3) share the same repository plumbing.</summary>
public sealed class EscalateRefundTicketHandler(IRefundTicketRepository ticketRepository, IEventPublisher eventPublisher, IClock clock)
{
    public async Task<RefundTicket> EscalateByAppealAsync(Guid ticketId, CancellationToken ct)
    {
        var ticket = await ticketRepository.FindAsync(ticketId, ct)
            ?? throw new DomainException("refund_ticket_not_found", "找不到退課工單。");

        ticket.EscalateToArbitration(clock.UtcNow);
        await PersistAndPublishAsync(ticket, ct);
        return ticket;
    }

    /// <summary>Called by the refund-SLA Step Functions execution once the 5-business-day wait elapses.</summary>
    public async Task<RefundTicket> EscalateOnSlaTimeoutAsync(Guid ticketId, CancellationToken ct)
    {
        var ticket = await ticketRepository.FindAsync(ticketId, ct)
            ?? throw new DomainException("refund_ticket_not_found", "找不到退課工單。");

        ticket.AutoEscalateOnSlaTimeout(clock.UtcNow);
        await PersistAndPublishAsync(ticket, ct);
        return ticket;
    }

    private async Task PersistAndPublishAsync(RefundTicket ticket, CancellationToken ct)
    {
        await ticketRepository.SaveAsync(ticket, ct);
        await eventPublisher.PublishAsync(ticket.DomainEvents, ct);
        ticket.ClearDomainEvents();
    }
}

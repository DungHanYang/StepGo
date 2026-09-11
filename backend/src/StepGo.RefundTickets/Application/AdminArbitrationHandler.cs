using StepGo.RefundTickets.Domain;
using StepGo.Shared.Domain;

namespace StepGo.RefundTickets.Application;

public sealed record AdminArbitrationCommand(Guid TicketId, bool Approve, Money? ApprovedAmount);

/// <summary>Task 7.4: admin arbitration is final — RefundTicket.AdminArbitrate itself refuses re-entry from a terminal state.</summary>
public sealed class AdminArbitrationHandler(IRefundTicketRepository ticketRepository, RefundApprovalFinalizer finalizer, StepGo.Shared.Application.IClock clock)
{
    public async Task<RefundTicket> HandleAsync(AdminArbitrationCommand command, CancellationToken ct)
    {
        var ticket = await ticketRepository.FindAsync(command.TicketId, ct)
            ?? throw new DomainException("refund_ticket_not_found", "找不到退課工單。");

        ticket.AdminArbitrate(command.Approve, command.ApprovedAmount ?? Money.Zero, clock.UtcNow);

        if (command.Approve)
        {
            await finalizer.FinalizeAsync(ticket, ct);
        }

        await ticketRepository.SaveAsync(ticket, ct);
        return ticket;
    }
}

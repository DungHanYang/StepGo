using StepGo.Application.Common;
using StepGo.Domain.RefundTickets;
using StepGo.Domain.SharedKernel;

namespace StepGo.Application.RefundTickets;

public sealed record TeacherRefundDecisionCommand(Guid TicketId, Guid TeacherId, bool Approve, Money? ApprovedAmount, string? RejectionReason);

/// <summary>Task 7.2: approved amount is capped at the order's original payment; rejection requires a reason.</summary>
public sealed class TeacherRefundDecisionHandler(IRefundTicketRepository ticketRepository, RefundApprovalFinalizer finalizer, IClock clock)
{
    public async Task<RefundTicket> HandleAsync(TeacherRefundDecisionCommand command, CancellationToken ct)
    {
        var ticket = await ticketRepository.FindAsync(command.TicketId, ct)
            ?? throw new DomainException("refund_ticket_not_found", "找不到退課工單。");

        if (ticket.TeacherId != command.TeacherId)
        {
            throw new AuthorizationException("無權處理其他老師的退課工單。");
        }

        if (command.Approve)
        {
            ticket.TeacherApprove(command.ApprovedAmount ?? Money.Zero, ticket.StudentId, ticket.TeacherId, clock.UtcNow);
            await finalizer.FinalizeAsync(ticket, ct);
        }
        else
        {
            ticket.TeacherReject(command.RejectionReason ?? string.Empty);
        }

        await ticketRepository.SaveAsync(ticket, ct);
        return ticket;
    }
}

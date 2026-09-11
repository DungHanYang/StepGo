using StepGo.Application.Common;
using StepGo.Application.Identity;
using StepGo.Domain.Identity;
using StepGo.Domain.RefundTickets;
using StepGo.Domain.SharedKernel;

namespace StepGo.Application.RefundTickets;

/// <summary>Task 7.6: the shared thread is visible to both sides; internal notes are admin-only and never mixed in.</summary>
public sealed class ThreadAndNotesHandler(IRefundTicketRepository ticketRepository, ICurrentUserAccessor currentUser, IClock clock)
{
    public async Task<RefundTicket> AddThreadMessageAsync(Guid ticketId, string text, CancellationToken ct)
    {
        var ticket = await ticketRepository.FindAsync(ticketId, ct)
            ?? throw new DomainException("refund_ticket_not_found", "找不到退課工單。");

        if (currentUser.Role != Role.Admin)
        {
            if (currentUser.Role == Role.Student)
            {
                RowLevelAccessGuard.GuardOwnsStudentResource(currentUser, ticket.StudentId);
            }
            else
            {
                RowLevelAccessGuard.GuardOwnsTeacherResource(currentUser, ticket.TeacherId);
            }
        }

        ticket.AddThreadMessage(currentUser.Role, currentUser.UserId, text, clock.UtcNow);
        await ticketRepository.SaveAsync(ticket, ct);
        return ticket;
    }

    /// <summary>Admin-only. Never returned by any query that also returns the shared thread.</summary>
    public async Task<RefundTicket> AddInternalNoteAsync(Guid ticketId, string text, CancellationToken ct)
    {
        RowLevelAccessGuard.GuardIsAdmin(currentUser);

        var ticket = await ticketRepository.FindAsync(ticketId, ct)
            ?? throw new DomainException("refund_ticket_not_found", "找不到退課工單。");

        ticket.AddInternalNote(currentUser.UserId, text, clock.UtcNow);
        await ticketRepository.SaveAsync(ticket, ct);
        return ticket;
    }
}

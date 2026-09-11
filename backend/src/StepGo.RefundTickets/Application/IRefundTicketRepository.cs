using StepGo.RefundTickets.Domain;

namespace StepGo.RefundTickets.Application;

public interface IRefundTicketRepository
{
    Task<RefundTicket?> FindAsync(Guid ticketId, CancellationToken ct);
    Task<RefundTicket?> FindByOrderAsync(Guid orderId, CancellationToken ct);
    Task<IReadOnlyList<RefundTicket>> ListPendingByTeacherAsync(Guid teacherId, CancellationToken ct);
    Task<IReadOnlyList<RefundTicket>> ListByStatusAsync(RefundTicketStatus status, CancellationToken ct);
    Task SaveAsync(RefundTicket ticket, CancellationToken ct);
}

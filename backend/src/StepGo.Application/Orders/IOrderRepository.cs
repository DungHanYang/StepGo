using StepGo.Domain.Orders;

namespace StepGo.Application.Orders;

public interface IOrderRepository
{
    Task<Order?> FindAsync(Guid orderId, CancellationToken ct);
    Task<IReadOnlyList<Order>> ListByStudentAsync(Guid studentId, CancellationToken ct);
    Task<IReadOnlyList<Order>> ListByTeacherAsync(Guid teacherId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);

    /// <summary>
    /// Orders paid and payout-eligible but not yet batched, for a given teacher — the payout batch
    /// calculation's input set.
    /// </summary>
    Task<IReadOnlyList<Order>> ListPendingPayoutByTeacherAsync(Guid teacherId, CancellationToken ct);

    Task<IReadOnlyList<Guid>> ListTeacherIdsWithPendingPayoutAsync(CancellationToken ct);

    /// <summary>ATM orders still PendingPayment whose AtmPaymentDueAt has passed — the overdue-scan Lambda's input set (task 4.4).</summary>
    Task<IReadOnlyList<Guid>> ListOverdueCandidateOrderIdsAsync(DateTimeOffset now, CancellationToken ct);

    /// <summary>Persists the order. When the order just transitioned to Paid, also updates the teacher/platform summary aggregates in the same DynamoDB transaction (design.md decision 4).</summary>
    Task SaveAsync(Order order, CancellationToken ct);
}

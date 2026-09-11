using StepGo.Domain.Payouts;

namespace StepGo.Application.Payouts;

public interface IPayoutBatchRepository
{
    Task<PayoutBatch?> FindAsync(Guid payoutBatchId, CancellationToken ct);
    Task<IReadOnlyList<PayoutBatch>> ListByTeacherAsync(Guid teacherId, CancellationToken ct);
    Task SaveAsync(PayoutBatch payoutBatch, CancellationToken ct);
}

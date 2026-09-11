using StepGo.Payouts.Domain;

namespace StepGo.Payouts.Application;

public interface IPayoutBatchRepository
{
    Task<PayoutBatch?> FindAsync(Guid payoutBatchId, CancellationToken ct);
    Task<IReadOnlyList<PayoutBatch>> ListByTeacherAsync(Guid teacherId, CancellationToken ct);
    Task SaveAsync(PayoutBatch payoutBatch, CancellationToken ct);
}

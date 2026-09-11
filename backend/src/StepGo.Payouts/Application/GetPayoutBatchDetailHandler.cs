using StepGo.Shared.Application;
using StepGo.Payouts.Domain;
using StepGo.Shared.Domain;

namespace StepGo.Payouts.Application;

/// <summary>Task 6.3: full traceable detail — covered orders, net total, transfer fee, and final take-home amount.</summary>
public sealed class GetPayoutBatchDetailHandler(IPayoutBatchRepository payoutBatchRepository)
{
    public async Task<PayoutBatch> HandleAsync(Guid payoutBatchId, ICurrentUserAccessor currentUser, CancellationToken ct)
    {
        var batch = await payoutBatchRepository.FindAsync(payoutBatchId, ct)
            ?? throw new DomainException("payout_batch_not_found", "找不到撥款批次。");

        StepGo.Identity.Application.RowLevelAccessGuard.GuardOwnsTeacherResource(currentUser, batch.TeacherId);
        return batch;
    }
}

using StepGo.Application.Common;
using StepGo.Domain.Payouts;
using StepGo.Domain.SharedKernel;

namespace StepGo.Application.Payouts;

/// <summary>Task 6.3: full traceable detail — covered orders, net total, transfer fee, and final take-home amount.</summary>
public sealed class GetPayoutBatchDetailHandler(IPayoutBatchRepository payoutBatchRepository)
{
    public async Task<PayoutBatch> HandleAsync(Guid payoutBatchId, ICurrentUserAccessor currentUser, CancellationToken ct)
    {
        var batch = await payoutBatchRepository.FindAsync(payoutBatchId, ct)
            ?? throw new DomainException("payout_batch_not_found", "找不到撥款批次。");

        StepGo.Application.Identity.RowLevelAccessGuard.GuardOwnsTeacherResource(currentUser, batch.TeacherId);
        return batch;
    }
}

using StepGo.Shared.Application;
using StepGo.Orders.Application;
using StepGo.Shared.Domain;

namespace StepGo.Payouts.Application;

/// <summary>Task 6.2: payout account name check happens before marking paid; bank rejection reverts covered orders to the next period's pool.</summary>
public sealed class MarkPayoutBatchOutcomeHandler(
    IPayoutBatchRepository payoutBatchRepository, IOrderRepository orderRepository,
    StepGo.Identity.Application.ITeacherProfileRepository teacherRepository, IClock clock)
{
    public async Task MarkPaidAsync(Guid payoutBatchId, string transferReference, CancellationToken ct)
    {
        var batch = await payoutBatchRepository.FindAsync(payoutBatchId, ct)
            ?? throw new DomainException("payout_batch_not_found", "找不到撥款批次。");

        var teacher = await teacherRepository.FindAsync(batch.TeacherId, ct)
            ?? throw new DomainException("teacher_profile_not_found", "找不到老師資料。");

        batch.GuardPayoutAccountNameMatches(teacher.PayoutAccount.AccountHolderName, teacher.RealName);
        batch.MarkPaid(transferReference, clock.UtcNow);
        await payoutBatchRepository.SaveAsync(batch, ct);

        foreach (var line in batch.OrderLines)
        {
            var order = await orderRepository.FindAsync(line.OrderId, ct);
            if (order is null)
            {
                continue;
            }

            order.MarkPaidOut();
            await orderRepository.SaveAsync(order, ct);
        }
    }

    public async Task MarkBankRejectedAsync(Guid payoutBatchId, string reason, CancellationToken ct)
    {
        var batch = await payoutBatchRepository.FindAsync(payoutBatchId, ct)
            ?? throw new DomainException("payout_batch_not_found", "找不到撥款批次。");

        batch.MarkBankRejected(reason);
        await payoutBatchRepository.SaveAsync(batch, ct);

        foreach (var line in batch.OrderLines)
        {
            var order = await orderRepository.FindAsync(line.OrderId, ct);
            if (order is null)
            {
                continue;
            }

            order.RevertPayoutToPending();
            await orderRepository.SaveAsync(order, ct);
        }
    }
}

using StepGo.Governance.Application;
using StepGo.Identity.Application;
using StepGo.Orders.Application;
using StepGo.Shared.Domain.FeeLedger;
using StepGo.Payouts.Domain;
using StepGo.Shared.Domain;

namespace StepGo.Payouts.Application;

/// <summary>
/// Task 6.1: runs once per teacher with pending-payout orders (triggered monthly by EventBridge
/// Scheduler). Teachers whose period total is below the NT$500 threshold are skipped entirely — their
/// orders stay PendingPayout and roll into next period's calculation automatically (PayoutBatch.TryDraft
/// returns null and nothing here is persisted for that teacher).
/// </summary>
public sealed class CalculatePayoutBatchHandler(
    IOrderRepository orderRepository, IPayoutBatchRepository payoutBatchRepository,
    ITeacherProfileRepository teacherRepository, IPlatformFeeSettingRepository feeSettingRepository,
    IPlatformBankAccountProvider platformBankAccountProvider)
{
    public async Task<PayoutBatch?> HandleAsync(Guid teacherId, string periodYyyyMm, CancellationToken ct)
    {
        var pendingOrders = await orderRepository.ListPendingPayoutByTeacherAsync(teacherId, ct);
        if (pendingOrders.Count == 0)
        {
            return null;
        }

        var teacher = await teacherRepository.FindAsync(teacherId, ct)
            ?? throw new DomainException("teacher_profile_not_found", "找不到老師的撥款帳戶資料。");

        var platformBankCode = await platformBankAccountProvider.GetBankCodeAsync(ct);
        var isSameBank = teacher.PayoutAccount.IsSameBankAs(platformBankCode);

        var setting = await feeSettingRepository.GetCurrentAsync(ct)
            ?? throw new DomainException("platform_fee_setting_missing", "尚未設定平台轉帳費率。");
        var transferFee = TransferFeeCalculator.Calculate(isSameBank, setting.CrossBankTransferFee);

        var orderLines = pendingOrders
            .Select(o => new PayoutBatchOrderLine(o.Id, o.NetReceivable ?? Money.Zero))
            .ToList();

        var batch = PayoutBatch.TryDraft(Guid.NewGuid(), teacherId, periodYyyyMm, orderLines, transferFee);
        if (batch is null)
        {
            return null;
        }

        foreach (var order in pendingOrders)
        {
            order.AssignToPayoutBatch(batch.Id);
            await orderRepository.SaveAsync(order, ct);
        }

        await payoutBatchRepository.SaveAsync(batch, ct);
        return batch;
    }
}

using Moq;
using StepGo.Application.Common;
using StepGo.Application.Identity;
using StepGo.Application.Orders;
using StepGo.Application.Payouts;
using StepGo.Domain.Courses;
using StepGo.Domain.Orders;
using StepGo.Domain.Payouts;
using StepGo.Domain.SharedKernel;

namespace StepGo.Application.Tests.Payouts;

/// <summary>Task 6.2: after a bank-rejected transfer, the batch's covered orders roll back to PendingPayout so the next batch retries them.</summary>
public class MarkPayoutBatchOutcomeHandlerTests
{
    private static Order MakePendingPayoutOrder(Guid teacherId, DateTimeOffset now)
    {
        var course = Course.Create(
            Guid.NewGuid(), teacherId, "課程", Money.FromWholeDollars(3000), PaymentMethod.All,
            new RefundRuleSet([new RefundTier(14, 1.0m)]), new RefundRuleSet([new RefundTier(14, 1.0m)]), now.AddDays(30));
        var order = Order.PlaceForCourse(Guid.NewGuid(), course, Guid.NewGuid(), PaymentMethod.CreditCard, now);
        var schedule = new StepGo.Domain.FeeLedger.FeeRateSchedule("v1", 0.0289m, Money.FromWholeDollars(15), 0.10m);
        order.ConfirmPayment(PaymentMethod.CreditCard, StepGo.Domain.FeeLedger.FeeCalculationResult.Calculate(order.CoursePriceAtOrder, PaymentMethod.CreditCard, schedule), now);
        return order;
    }

    [Fact]
    public async Task MarkBankRejectedAsync_RevertsCoveredOrdersToPendingPayout()
    {
        var now = DateTimeOffset.UtcNow;
        var teacherId = Guid.NewGuid();
        var order = MakePendingPayoutOrder(teacherId, now);

        var batch = PayoutBatch.TryDraft(Guid.NewGuid(), teacherId, "2026-09", [new PayoutBatchOrderLine(order.Id, order.NetReceivable!.Value)], Money.Zero)!;
        order.AssignToPayoutBatch(batch.Id);

        var batchRepo = new Mock<IPayoutBatchRepository>();
        batchRepo.Setup(r => r.FindAsync(batch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(batch);
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.FindAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = new MarkPayoutBatchOutcomeHandler(batchRepo.Object, orderRepo.Object, new Mock<ITeacherProfileRepository>().Object, new FixedClock(now));
        await handler.MarkBankRejectedAsync(batch.Id, "銀行拒絕：戶名不符", CancellationToken.None);

        Assert.Equal(PayoutBatchStatus.BankRejected, batch.Status);
        Assert.Equal(OrderPayoutStatus.PendingPayout, order.PayoutStatus);
        Assert.Null(order.PayoutBatchId);
        orderRepo.Verify(r => r.SaveAsync(order, It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}

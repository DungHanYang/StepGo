using Moq;
using StepGo.Application.Common;
using StepGo.Application.FeeLedger;
using StepGo.Application.Orders;
using StepGo.Domain.Courses;
using StepGo.Domain.FeeLedger;
using StepGo.Domain.Orders;
using StepGo.Domain.SharedKernel;

namespace StepGo.Application.Tests.Orders;

/// <summary>Task 4.2: the webhook receiver only enqueues — it never touches order state.</summary>
public class ReceiveGatewayNotificationHandlerTests
{
    [Fact]
    public async Task HandleAsync_EnqueuesPayloadVerbatim()
    {
        var payload = new GatewayNotificationPayload("ECPay", "abc", "txn-1", "1", new Dictionary<string, string>());
        var queue = new Mock<IOrderNotificationQueue>();

        await new ReceiveGatewayNotificationHandler(queue.Object).HandleAsync(payload, CancellationToken.None);

        queue.Verify(q => q.EnqueueAsync(payload, It.IsAny<CancellationToken>()), Times.Once);
    }
}

/// <summary>Task 4.3: a redelivered notification (same gateway transaction id) is processed at most once.</summary>
public class ProcessGatewayNotificationHandlerTests
{
    private static Order MakeOrder(DateTimeOffset now)
    {
        var course = Course.Create(
            Guid.NewGuid(), Guid.NewGuid(), "課程", Money.FromWholeDollars(3000), PaymentMethod.All,
            new RefundRuleSet([new RefundTier(14, 1.0m)]), new RefundRuleSet([new RefundTier(14, 1.0m)]), now.AddDays(30));
        return Order.PlaceForCourse(Guid.NewGuid(), course, Guid.NewGuid(), PaymentMethod.CreditCard, now);
    }

    [Fact]
    public async Task HandleAsync_DuplicateNotification_IsIgnoredAndOrderUnchanged()
    {
        var now = DateTimeOffset.UtcNow;
        var order = MakeOrder(now);
        var notification = new GatewayNotificationPayload("ECPay", order.Id.ToString("N"), "txn-1", "1", new Dictionary<string, string>());

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.FindAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var idempotency = new Mock<IPaymentNotificationIdempotencyStore>();
        idempotency.SetupSequence(s => s.TryReserveAsync("txn-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)   // first delivery: reserved
            .ReturnsAsync(false); // redelivery: already processed
        var scheduleProvider = new Mock<IFeeRateScheduleProvider>();
        scheduleProvider.Setup(p => p.GetCurrentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeeRateSchedule("v1", 0.0289m, Money.FromWholeDollars(15), 0.10m));
        var eventPublisher = new Mock<IEventPublisher>();

        var handler = new ProcessGatewayNotificationHandler(orderRepo.Object, idempotency.Object, scheduleProvider.Object, eventPublisher.Object, new FixedClock(now));

        var firstResult = await handler.HandleAsync(order.Id, notification, PaymentMethod.CreditCard, CancellationToken.None);
        Assert.True(firstResult);
        Assert.Equal(OrderPaymentStatus.Paid, order.PaymentStatus);

        var secondResult = await handler.HandleAsync(order.Id, notification, PaymentMethod.CreditCard, CancellationToken.None);
        Assert.False(secondResult);

        orderRepo.Verify(r => r.SaveAsync(order, It.IsAny<CancellationToken>()), Times.Once); // only the first delivery persisted anything
        eventPublisher.Verify(p => p.PublishAsync(It.IsAny<IReadOnlyCollection<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}

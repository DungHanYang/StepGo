using StepGo.Shared.Application;
using StepGo.Shared.Application.FeeLedger;
using StepGo.Courses.Domain;
using StepGo.Shared.Domain.FeeLedger;
using StepGo.Shared.Domain;

namespace StepGo.Orders.Application;

/// <summary>
/// Task 4.3 + 5.1/5.2: the SQS consumer. Idempotency is enforced by a conditional write on the gateway's
/// unique transaction id before any order mutation happens — a duplicate delivery of the same notification
/// short-circuits here and touches nothing (order state, fees, or downstream notifications) a second time.
/// </summary>
public sealed class ProcessGatewayNotificationHandler(
    IOrderRepository orderRepository, IPaymentNotificationIdempotencyStore idempotencyStore,
    IFeeRateScheduleProvider feeScheduleProvider, IEventPublisher eventPublisher, IClock clock)
{
    /// <summary>Returns false when this transaction id had already been processed (safely ignored).</summary>
    public async Task<bool> HandleAsync(Guid orderId, GatewayNotificationPayload notification, PaymentMethod methodUsed, CancellationToken ct)
    {
        var reserved = await idempotencyStore.TryReserveAsync(notification.GatewayTransactionId, ct);
        if (!reserved)
        {
            return false;
        }

        var order = await orderRepository.FindAsync(orderId, ct)
            ?? throw new DomainException("order_not_found", "找不到訂單。");

        var schedule = await feeScheduleProvider.GetCurrentAsync(ct);
        var fees = FeeCalculationResult.Calculate(order.CoursePriceAtOrder, methodUsed, schedule);

        order.ConfirmPayment(methodUsed, fees, clock.UtcNow);

        await orderRepository.SaveAsync(order, ct);
        await eventPublisher.PublishAsync(order.DomainEvents, ct);
        order.ClearDomainEvents();

        return true;
    }
}

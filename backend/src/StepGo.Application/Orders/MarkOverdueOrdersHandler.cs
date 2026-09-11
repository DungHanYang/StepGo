using StepGo.Application.Common;

namespace StepGo.Application.Orders;

/// <summary>Task 4.4 (overdue half): scans ATM orders past their due date and flips them to Overdue.</summary>
public sealed class MarkOverdueOrdersHandler(IOrderRepository orderRepository, IClock clock, IEventPublisher eventPublisher)
{
    public async Task HandleAsync(IReadOnlyList<Guid> candidateOrderIds, CancellationToken ct)
    {
        foreach (var orderId in candidateOrderIds)
        {
            var order = await orderRepository.FindAsync(orderId, ct);
            if (order is null)
            {
                continue;
            }

            order.MarkOverdueIfAtmUnpaid(clock.UtcNow);
            if (order.DomainEvents.Count == 0)
            {
                continue;
            }

            await orderRepository.SaveAsync(order, ct);
            await eventPublisher.PublishAsync(order.DomainEvents, ct);
            order.ClearDomainEvents();
        }
    }
}

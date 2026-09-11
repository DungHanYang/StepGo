using StepGo.Shared.Application;
using StepGo.Orders.Application;
using StepGo.Orders.Domain;
using StepGo.RefundTickets.Domain;

namespace StepGo.RefundTickets.Application;

/// <summary>
/// Task 7.5: shared post-approval logic for both the teacher-approve and admin-arbitration paths.
/// Decides the deduction source from the order's current payout status, decides the refund execution
/// route from the payment method actually used, and (for credit-card orders only) calls the gateway's
/// automatic refund API — ATM orders are left for an admin to complete manually.
/// </summary>
public sealed class RefundApprovalFinalizer(IOrderRepository orderRepository, IPaymentGatewayClient gateway, IEventPublisher eventPublisher)
{
    public async Task FinalizeAsync(RefundTicket ticket, CancellationToken ct)
    {
        var order = await orderRepository.FindAsync(ticket.OrderId, ct)
            ?? throw new StepGo.Shared.Domain.DomainException("order_not_found", "找不到訂單。");

        var deductionSource = order.PayoutStatus == OrderPayoutStatus.PaidOut
            ? DeductionSource.NextTeacherPayout
            : DeductionSource.PlatformAccountDirect;
        ticket.SetDeductionSource(deductionSource);

        var route = order.DetermineRefundRoute();
        ticket.SetRefundExecutionRoute(route);

        if (route == RefundRoute.AutomaticGatewayApi && ticket.DecidedAmount is { } amount)
        {
            await gateway.RefundCreditCardAsync(order.Id.ToString(), amount, ct);
        }

        order.MarkRefunded();
        if (deductionSource == DeductionSource.PlatformAccountDirect)
        {
            order.ExcludeFromPayout();
        }

        await orderRepository.SaveAsync(order, ct);
        await eventPublisher.PublishAsync(ticket.DomainEvents, ct);
        ticket.ClearDomainEvents();
    }
}

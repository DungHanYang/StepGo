using Moq;
using StepGo.Orders.Application;
using StepGo.RefundTickets.Application;
using StepGo.Courses.Domain;
using StepGo.Shared.Domain.FeeLedger;
using StepGo.Orders.Domain;
using StepGo.RefundTickets.Domain;
using StepGo.Shared.Domain;

namespace StepGo.Application.Tests.RefundTickets;

/// <summary>Task 7.5: refund deduction source depends on whether the order was already paid out.</summary>
public class RefundApprovalFinalizerTests
{
    private static Order MakePaidOrder(PaymentMethod methodUsed, DateTimeOffset now)
    {
        var course = Course.Create(
            Guid.NewGuid(), Guid.NewGuid(), "課程", Money.FromWholeDollars(3000), PaymentMethod.All,
            new RefundRuleSet([new RefundTier(14, 1.0m)]), new RefundRuleSet([new RefundTier(14, 1.0m)]), now.AddDays(30));

        var order = Order.PlaceForCourse(Guid.NewGuid(), course, Guid.NewGuid(), methodUsed, now);
        var schedule = new FeeRateSchedule("v1", 0.0289m, Money.FromWholeDollars(15), 0.10m);
        order.ConfirmPayment(methodUsed, FeeCalculationResult.Calculate(order.CoursePriceAtOrder, methodUsed, schedule), now);
        return order;
    }

    private static RefundTicket MakeApprovedTicket(Order order, DateTimeOffset now)
    {
        var ticket = RefundTicket.Submit(order.Id, order.Id, order.StudentId, order.TeacherId, order.CoursePriceAtOrder, 1.0m, new AllowAllCalendar(), now);
        ticket.TeacherApprove(order.CoursePriceAtOrder, order.StudentId, order.TeacherId, now);
        return ticket;
    }

    private sealed class AllowAllCalendar : IBusinessDayCalendar
    {
        public DateTimeOffset AddBusinessDays(DateTimeOffset from, int businessDays) => from.AddDays(businessDays);
    }

    [Fact]
    public async Task FinalizeAsync_OrderNotYetPaidOut_DeductsFromPlatformAndExcludesFromPayout()
    {
        var now = DateTimeOffset.UtcNow;
        var order = MakePaidOrder(PaymentMethod.CreditCard, now); // PayoutStatus = PendingPayout after ConfirmPayment
        var ticket = MakeApprovedTicket(order, now);

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.FindAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var gateway = new Mock<IPaymentGatewayClient>();
        var eventPublisher = new Mock<Common.IEventPublisher>();

        var finalizer = new RefundApprovalFinalizer(orderRepo.Object, gateway.Object, eventPublisher.Object);
        await finalizer.FinalizeAsync(ticket, CancellationToken.None);

        Assert.Equal(DeductionSource.PlatformAccountDirect, ticket.DecidedDeductionSource);
        Assert.Equal(OrderPayoutStatus.NotEligible, order.PayoutStatus);
        gateway.Verify(g => g.RefundCreditCardAsync(It.IsAny<string>(), It.IsAny<Money>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FinalizeAsync_OrderAlreadyPaidOut_DeductsFromNextPayout()
    {
        var now = DateTimeOffset.UtcNow;
        var order = MakePaidOrder(PaymentMethod.Atm, now);
        order.AssignToPayoutBatch(Guid.NewGuid());
        order.MarkPaidOut(); // simulate the order already having gone through a completed payout batch

        var ticket = MakeApprovedTicket(order, now);

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.FindAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var gateway = new Mock<IPaymentGatewayClient>();
        var eventPublisher = new Mock<Common.IEventPublisher>();

        var finalizer = new RefundApprovalFinalizer(orderRepo.Object, gateway.Object, eventPublisher.Object);
        await finalizer.FinalizeAsync(ticket, CancellationToken.None);

        Assert.Equal(DeductionSource.NextTeacherPayout, ticket.DecidedDeductionSource);
        Assert.Equal(OrderPayoutStatus.PaidOut, order.PayoutStatus); // stays paid out; deduction happens against the *next* batch, not retroactively
        gateway.Verify(g => g.RefundCreditCardAsync(It.IsAny<string>(), It.IsAny<Money>(), It.IsAny<CancellationToken>()), Times.Never); // ATM route is manual, never automatic
    }
}

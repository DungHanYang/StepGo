using StepGo.Domain.Courses;
using StepGo.Domain.FeeLedger;
using StepGo.Domain.Orders;
using StepGo.Domain.SharedKernel;

namespace StepGo.Domain.Tests.Orders;

public class OrderTests
{
    private static Course MakeCourse(PaymentMethod accepted) => Course.Create(
        Guid.NewGuid(), Guid.NewGuid(), "課程", Money.FromWholeDollars(3000), accepted,
        new RefundRuleSet([new RefundTier(14, 1.0m)]), new RefundRuleSet([new RefundTier(14, 1.0m)]), DateTimeOffset.UtcNow.AddDays(30));

    [Fact]
    public void PlaceForCourse_RequestingUnsupportedMethod_Throws()
    {
        var course = MakeCourse(PaymentMethod.Atm);

        var ex = Assert.Throws<DomainException>(() =>
            Order.PlaceForCourse(Guid.NewGuid(), course, Guid.NewGuid(), PaymentMethod.CreditCard, DateTimeOffset.UtcNow));

        Assert.Equal("payment_method_not_supported", ex.Code);
    }

    [Fact]
    public void PlaceForCourse_WhenCourseAcceptsBoth_ChoosePaymentIsAll()
    {
        var course = MakeCourse(PaymentMethod.All);

        var order = Order.PlaceForCourse(Guid.NewGuid(), course, Guid.NewGuid(), PaymentMethod.Atm, DateTimeOffset.UtcNow);

        Assert.Equal(ChoosePayment.All, order.GatewayChoosePayment);
    }

    [Fact]
    public void PlaceForCourse_WhenCourseAcceptsAtmOnly_ChoosePaymentIsAtm()
    {
        var course = MakeCourse(PaymentMethod.Atm);

        var order = Order.PlaceForCourse(Guid.NewGuid(), course, Guid.NewGuid(), PaymentMethod.Atm, DateTimeOffset.UtcNow);

        Assert.Equal(ChoosePayment.Atm, order.GatewayChoosePayment);
    }

    [Fact]
    public void DetermineRefundRoute_ForCreditCard_IsAutomatic()
    {
        var course = MakeCourse(PaymentMethod.All);
        var order = Order.PlaceForCourse(Guid.NewGuid(), course, Guid.NewGuid(), PaymentMethod.CreditCard, DateTimeOffset.UtcNow);
        var schedule = new FeeRateSchedule("v1", 0.0289m, Money.FromWholeDollars(15), 0.10m);
        order.ConfirmPayment(PaymentMethod.CreditCard, FeeCalculationResult.Calculate(order.CoursePriceAtOrder, PaymentMethod.CreditCard, schedule), DateTimeOffset.UtcNow);

        Assert.Equal(RefundRoute.AutomaticGatewayApi, order.DetermineRefundRoute());
    }

    [Fact]
    public void DetermineRefundRoute_ForAtm_IsManualTransferPending()
    {
        var course = MakeCourse(PaymentMethod.All);
        var order = Order.PlaceForCourse(Guid.NewGuid(), course, Guid.NewGuid(), PaymentMethod.Atm, DateTimeOffset.UtcNow);
        var schedule = new FeeRateSchedule("v1", 0.0289m, Money.FromWholeDollars(15), 0.10m);
        order.ConfirmPayment(PaymentMethod.Atm, FeeCalculationResult.Calculate(order.CoursePriceAtOrder, PaymentMethod.Atm, schedule), DateTimeOffset.UtcNow);

        Assert.Equal(RefundRoute.ManualBankTransferPending, order.DetermineRefundRoute());
    }

    [Fact]
    public void MarkOverdueIfAtmUnpaid_BeforeDueDate_DoesNothing()
    {
        var course = MakeCourse(PaymentMethod.Atm);
        var order = Order.PlaceForCourse(Guid.NewGuid(), course, Guid.NewGuid(), PaymentMethod.Atm, DateTimeOffset.UtcNow, atmDueInDays: 3);

        order.MarkOverdueIfAtmUnpaid(DateTimeOffset.UtcNow.AddDays(1));

        Assert.Equal(OrderPaymentStatus.PendingPayment, order.PaymentStatus);
    }

    [Fact]
    public void MarkOverdueIfAtmUnpaid_AfterDueDate_MarksOverdue()
    {
        var course = MakeCourse(PaymentMethod.Atm);
        var now = DateTimeOffset.UtcNow;
        var order = Order.PlaceForCourse(Guid.NewGuid(), course, Guid.NewGuid(), PaymentMethod.Atm, now, atmDueInDays: 3);

        order.MarkOverdueIfAtmUnpaid(now.AddDays(4));

        Assert.Equal(OrderPaymentStatus.Overdue, order.PaymentStatus);
    }
}

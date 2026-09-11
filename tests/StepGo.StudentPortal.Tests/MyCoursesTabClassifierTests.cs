using StepGo.PricingRules;
using StepGo.StudentPortal.Models;
using StepGo.StudentPortal.Services;
using StepGo.UI.Status;
using Xunit;

namespace StepGo.StudentPortal.Tests;

public class MyCoursesTabClassifierTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private static StudentOrder MakeOrder(
        PaymentStatus paymentStatus,
        DateTimeOffset? nextSessionAt = null,
        bool courseEnded = false,
        RefundTicketStatus? refundTicketStatus = null) =>
        new("o-1", "c-1", "課程", "老師", 1000m, PaymentMethod.CreditCard, paymentStatus, nextSessionAt, courseEnded, refundTicketStatus, Now);

    [Fact]
    public void Pending_Payment_Goes_To_AwaitingPayment_Tab()
    {
        var order = MakeOrder(PaymentStatus.Pending);

        Assert.Equal(MyCoursesTab.AwaitingPayment, MyCoursesTabClassifier.Classify(order, Now));
    }

    [Fact]
    public void Paid_With_Future_Session_Goes_To_Upcoming_Tab()
    {
        var order = MakeOrder(PaymentStatus.Paid, nextSessionAt: Now.AddDays(5), courseEnded: false);

        Assert.Equal(MyCoursesTab.Upcoming, MyCoursesTabClassifier.Classify(order, Now));
    }

    [Fact]
    public void Paid_With_Ended_Course_Goes_To_Finished_Tab()
    {
        var order = MakeOrder(PaymentStatus.Paid, courseEnded: true);

        Assert.Equal(MyCoursesTab.Finished, MyCoursesTabClassifier.Classify(order, Now));
    }

    [Fact]
    public void Refunded_Order_Goes_To_CancelledRefunded_Tab()
    {
        var order = MakeOrder(PaymentStatus.Refunded);

        Assert.Equal(MyCoursesTab.CancelledRefunded, MyCoursesTabClassifier.Classify(order, Now));
    }

    [Fact]
    public void Order_With_Completed_Refund_Ticket_Moves_To_CancelledRefunded_Even_If_Still_Paid()
    {
        // frontend-student-portal spec: "退款工單狀態變為「已完成」...該訂單顯示於「已取消/已退款」分頁，
        // 並從原本所在分頁移除"
        var order = MakeOrder(PaymentStatus.Paid, refundTicketStatus: RefundTicketStatus.Completed);

        Assert.Equal(MyCoursesTab.CancelledRefunded, MyCoursesTabClassifier.Classify(order, Now));
    }

    [Fact]
    public void InTab_Only_Returns_Orders_Matching_That_Tab()
    {
        var pending = MakeOrder(PaymentStatus.Pending);
        var upcoming = MakeOrder(PaymentStatus.Paid, nextSessionAt: Now.AddDays(1));

        var result = MyCoursesTabClassifier.InTab([pending, upcoming], MyCoursesTab.AwaitingPayment, Now);

        Assert.Single(result);
    }
}

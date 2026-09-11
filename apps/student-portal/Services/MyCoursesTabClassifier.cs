using StepGo.StudentPortal.Models;
using StepGo.UI.Status;

namespace StepGo.StudentPortal.Services;

public enum MyCoursesTab
{
    AwaitingPayment,
    Upcoming,
    Finished,
    CancelledRefunded,
}

/// <summary>Pure classification, split out so it's directly unit-testable (frontend-student-portal
/// spec: "我的課程四分頁依既有欄位篩選").</summary>
public static class MyCoursesTabClassifier
{
    public static MyCoursesTab Classify(StudentOrder order, DateTimeOffset now)
    {
        if (order.PaymentStatus == PaymentStatus.Refunded || order.RefundTicketStatus == RefundTicketStatus.Completed)
        {
            return MyCoursesTab.CancelledRefunded;
        }

        if (order.PaymentStatus != PaymentStatus.Paid)
        {
            return MyCoursesTab.AwaitingPayment;
        }

        return order.CourseEnded ? MyCoursesTab.Finished : MyCoursesTab.Upcoming;
    }

    public static IReadOnlyList<StudentOrder> InTab(IEnumerable<StudentOrder> orders, MyCoursesTab tab, DateTimeOffset now) =>
        orders.Where(o => Classify(o, now) == tab).ToList();
}

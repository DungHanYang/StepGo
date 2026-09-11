using StepGo.TeacherPortal.Models;
using StepGo.TeacherPortal.Services;
using StepGo.UI.Status;
using Xunit;

namespace StepGo.TeacherPortal.Tests;

public class RefundTicketSortingTests
{
    [Fact]
    public void Most_Urgent_Sla_Sorts_First()
    {
        var now = DateTimeOffset.UtcNow;
        var urgent = MakeTicket("t-urgent", now.AddDays(1).AddHours(4));
        var lessUrgent = MakeTicket("t-less-urgent", now.AddDays(3));

        var sorted = RefundTicketSorting.ByUrgency([lessUrgent, urgent], now);

        Assert.Equal("t-urgent", sorted[0].Id);
        Assert.Equal("t-less-urgent", sorted[1].Id);
    }

    [Fact]
    public void Non_Pending_Tickets_Are_Excluded()
    {
        var now = DateTimeOffset.UtcNow;
        var pending = MakeTicket("t-pending", now.AddDays(1));
        var approved = MakeTicket("t-approved", now.AddHours(1));
        approved.Status = RefundTicketStatus.TeacherApproved;

        var sorted = RefundTicketSorting.ByUrgency([pending, approved], now);

        Assert.Single(sorted);
        Assert.Equal("t-pending", sorted[0].Id);
    }

    private static RefundTicket MakeTicket(string id, DateTimeOffset slaDeadline) => new()
    {
        Id = id,
        OrderId = $"{id}-order",
        StudentName = "測試學生",
        CourseName = "測試課程",
        OriginalPaymentAmount = 1000m,
        SystemCalculatedRefundAmount = 1000m,
        SlaDeadline = slaDeadline,
        Reason = "測試原因",
    };
}

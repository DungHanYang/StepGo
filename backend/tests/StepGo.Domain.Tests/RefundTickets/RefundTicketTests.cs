using StepGo.RefundTickets.Domain;
using StepGo.Shared.Domain;

namespace StepGo.Domain.Tests.RefundTickets;

/// <summary>Adds N calendar days — sufficient for these unit tests; the real business-day calendar lives in Infrastructure.</summary>
file sealed class CalendarDaysStub : IBusinessDayCalendar
{
    public DateTimeOffset AddBusinessDays(DateTimeOffset from, int businessDays) => from.AddDays(businessDays);
}

public class RefundTicketTests
{
    private static readonly IBusinessDayCalendar Calendar = new CalendarDaysStub();

    [Fact]
    public void Submit_WithZeroPercentEligibility_AutoRejectsButStaysAppealable()
    {
        var ticket = RefundTicket.Submit(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Money.FromWholeDollars(3000),
            refundPercentage: 0m, Calendar, DateTimeOffset.UtcNow);

        Assert.Equal(RefundTicketStatus.AutoRejected, ticket.Status);

        ticket.EscalateToArbitration(DateTimeOffset.UtcNow); // does not throw: appeal path stays open
        Assert.Equal(RefundTicketStatus.AdminArbitration, ticket.Status);
    }

    [Fact]
    public void Submit_WithPositivePercentage_EntersTeacherReviewingWithSlaDeadline()
    {
        var now = DateTimeOffset.UtcNow;
        var ticket = RefundTicket.Submit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Money.FromWholeDollars(3000), 1.0m, Calendar, now);

        Assert.Equal(RefundTicketStatus.TeacherReviewing, ticket.Status);
        Assert.Equal(now.AddDays(RefundTicket.TeacherResponseSlaBusinessDays), ticket.TeacherSlaDeadline);
    }

    [Fact]
    public void TeacherApprove_AmountAboveOriginalPayment_Throws()
    {
        var ticket = RefundTicket.Submit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Money.FromWholeDollars(3000), 1.0m, Calendar, DateTimeOffset.UtcNow);

        var ex = Assert.Throws<DomainException>(() =>
            ticket.TeacherApprove(Money.FromWholeDollars(3001), ticket.StudentId, ticket.TeacherId, DateTimeOffset.UtcNow));

        Assert.Equal("refund_amount_exceeds_original_payment", ex.Code);
    }

    [Fact]
    public void TeacherApprove_ValidAmount_TransitionsToApprovedTerminal()
    {
        var ticket = RefundTicket.Submit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Money.FromWholeDollars(3000), 1.0m, Calendar, DateTimeOffset.UtcNow);

        ticket.TeacherApprove(Money.FromWholeDollars(3000), ticket.StudentId, ticket.TeacherId, DateTimeOffset.UtcNow);

        Assert.Equal(RefundTicketStatus.ApprovedTerminal, ticket.Status);
    }

    [Fact]
    public void AutoEscalateOnSlaTimeout_BeforeDeadline_Throws()
    {
        var now = DateTimeOffset.UtcNow;
        var ticket = RefundTicket.Submit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Money.FromWholeDollars(3000), 1.0m, Calendar, now);

        Assert.Throws<DomainException>(() => ticket.AutoEscalateOnSlaTimeout(now.AddDays(1)));
    }

    [Fact]
    public void AutoEscalateOnSlaTimeout_AfterDeadline_TransitionsToAdminArbitration()
    {
        var now = DateTimeOffset.UtcNow;
        var ticket = RefundTicket.Submit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Money.FromWholeDollars(3000), 1.0m, Calendar, now);

        ticket.AutoEscalateOnSlaTimeout(now.AddDays(RefundTicket.TeacherResponseSlaBusinessDays + 1));

        Assert.Equal(RefundTicketStatus.AdminArbitration, ticket.Status);
        Assert.Contains(ticket.DomainEvents, e => e is RefundTicketEscalatedToArbitrationEvent);
    }

    [Fact]
    public void AdminArbitrate_IsFinal_CannotBeReEntered()
    {
        var now = DateTimeOffset.UtcNow;
        var ticket = RefundTicket.Submit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Money.FromWholeDollars(3000), 0m, Calendar, now);
        ticket.EscalateToArbitration(now);

        ticket.AdminArbitrate(approve: true, Money.FromWholeDollars(1500), now);
        Assert.Equal(RefundTicketStatus.ApprovedTerminal, ticket.Status);

        var ex = Assert.Throws<DomainException>(() => ticket.AdminArbitrate(approve: false, Money.Zero, now));
        Assert.Equal("invalid_ticket_status_transition", ex.Code);
    }

    [Fact]
    public void AddInternalNote_NeverAppearsInSharedThread()
    {
        var ticket = RefundTicket.Submit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Money.FromWholeDollars(3000), 1.0m, Calendar, DateTimeOffset.UtcNow);

        ticket.AddInternalNote(Guid.NewGuid(), "電話聯繫學生確認情況", DateTimeOffset.UtcNow);

        Assert.Single(ticket.InternalNotes);
        Assert.Empty(ticket.Thread);
    }
}

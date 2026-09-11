using StepGo.Application.Common;
using StepGo.Application.Courses;
using StepGo.Application.Orders;
using StepGo.Domain.RefundTickets;
using StepGo.Domain.SharedKernel;

namespace StepGo.Application.RefundTickets;

public sealed record SubmitRefundTicketCommand(Guid OrderId);

/// <summary>Task 7.1: auto-calculates the eligible amount from the course's refund tiers; a 0% result auto-rejects but stays appealable.</summary>
public sealed class SubmitRefundTicketHandler(
    IOrderRepository orderRepository, ICourseRepository courseRepository, IRefundTicketRepository ticketRepository,
    IBusinessDayCalendar calendar, IRefundSlaScheduler slaScheduler, IClock clock)
{
    public async Task<RefundTicket> HandleAsync(SubmitRefundTicketCommand command, CancellationToken ct)
    {
        var order = await orderRepository.FindAsync(command.OrderId, ct)
            ?? throw new DomainException("order_not_found", "找不到訂單。");

        var course = await courseRepository.FindAsync(order.CourseId, ct)
            ?? throw new DomainException("course_not_found", "找不到課程。");

        var daysBeforeStart = (int)Math.Floor((course.StartsAt - clock.UtcNow).TotalDays);
        var percentage = course.RefundRules.PercentageFor(daysBeforeStart);

        // The ticket's aggregate id intentionally equals the order id: DynamoDB stores it at
        // PK=ORDER#<orderId> SK=REFUNDTICKET (a fixed SK naturally enforces "one ticket per order"),
        // so looking a ticket up by its own id is the same GetItem as looking it up by order id.
        var ticket = RefundTicket.Submit(
            order.Id, order.Id, order.StudentId, order.TeacherId, order.CoursePriceAtOrder,
            percentage, calendar, clock.UtcNow);

        await ticketRepository.SaveAsync(ticket, ct);

        if (ticket.Status == RefundTicketStatus.TeacherReviewing)
        {
            await slaScheduler.StartTrackingAsync(ticket.Id, ct);
        }

        return ticket;
    }
}

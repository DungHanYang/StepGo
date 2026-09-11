using StepGo.Shared.Application;
using StepGo.Courses.Application;
using StepGo.Courses.Domain;
using StepGo.Orders.Domain;
using StepGo.Shared.Domain;

namespace StepGo.Orders.Application;

public sealed record PlaceOrderCommand(Guid CourseId, Guid StudentId, PaymentMethod RequestedPaymentMethod);

public sealed record PlaceOrderResult(Order Order, GatewayOrderPlacementResult GatewayPlacement);

/// <summary>Task 4.1: requested payment method must be one the course offers; ChoosePayment follows the course's full accepted set.</summary>
public sealed class PlaceOrderHandler(ICourseRepository courseRepository, IOrderRepository orderRepository, IPaymentGatewayClient gateway, IClock clock)
{
    public async Task<PlaceOrderResult> HandleAsync(PlaceOrderCommand command, CancellationToken ct)
    {
        var course = await courseRepository.FindAsync(command.CourseId, ct)
            ?? throw new DomainException("course_not_found", "找不到課程。");

        var order = Order.PlaceForCourse(Guid.NewGuid(), course, command.StudentId, command.RequestedPaymentMethod, clock.UtcNow);

        var placement = await gateway.PlaceOrderAsync(order.Id, order.CoursePriceAtOrder, order.GatewayChoosePayment, ct);

        await orderRepository.SaveAsync(order, ct);
        return new PlaceOrderResult(order, placement);
    }
}

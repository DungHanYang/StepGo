using StepGo.Api.Shared.Composition;
using StepGo.Api.Shared.Json;
using StepGo.Api.Shared.Routing;
using StepGo.Application.Orders;
using StepGo.Contracts.Courses;
using StepGo.Contracts.Json;
using StepGo.Contracts.Orders;
using StepGo.Domain.Courses;
using StepGo.Domain.Orders;
using StepGo.Domain.SharedKernel;

namespace StepGo.Api.Orders;

public static class OrdersRoutes
{
    public static MiniRouter Map(MiniRouter router, CompositionRoot root, StepGoJsonContext contractsJson) => router
        .MapHealthCheck("/orders/health")
        .MapPost("/orders", async (ctx, ct) =>
        {
            var request = JsonResponses.Deserialize(ctx.Request.Body, contractsJson.CreateOrderRequestDto)
                ?? throw new DomainException("invalid_request", "請求內容不正確。");

            var handler = new PlaceOrderHandler(root.CourseRepository, root.OrderRepository, root.PaymentGatewayClient, root.Clock);
            var result = await handler.HandleAsync(new PlaceOrderCommand(request.CourseId, ctx.CurrentUser.UserId, (PaymentMethod)request.RequestedPaymentMethod), ct);

            return JsonResponses.Created(ToDto(result.Order), contractsJson.OrderDto);
        })
        .MapGet("/orders/{orderId}", async (ctx, ct) =>
        {
            var orderId = Guid.Parse(ctx.PathParameters["orderId"]);
            var order = await root.OrderRepository.FindAsync(orderId, ct) ?? throw new DomainException("order_not_found", "找不到訂單。");

            if (ctx.CurrentUser.Role == StepGo.Domain.Identity.Role.Student)
            {
                StepGo.Application.Identity.RowLevelAccessGuard.GuardOwnsStudentResource(ctx.CurrentUser, order.StudentId);
            }
            else
            {
                StepGo.Application.Identity.RowLevelAccessGuard.GuardOwnsTeacherResource(ctx.CurrentUser, order.TeacherId);
            }

            return JsonResponses.Ok(ToDto(order), contractsJson.OrderDto);
        })
        .MapGet("/students/{studentId}/orders", async (ctx, ct) =>
        {
            var studentId = Guid.Parse(ctx.PathParameters["studentId"]);
            StepGo.Application.Identity.RowLevelAccessGuard.GuardOwnsStudentResource(ctx.CurrentUser, studentId);

            var orders = await root.OrderRepository.ListByStudentAsync(studentId, ct);
            return JsonResponses.Ok(new StepGo.Contracts.Common.PagedResultDto<OrderDto>([.. orders.Select(ToDto)], null), contractsJson.PagedResultDtoOrderDto);
        })
        .MapPost("/webhooks/payment-notifications", async (ctx, ct) =>
        {
            // Called directly by 綠界/藍新 — never authenticated with our own JWT (design.md decision 5).
            var request = JsonResponses.Deserialize(ctx.Request.Body, contractsJson.PaymentGatewayNotificationDto)
                ?? throw new DomainException("invalid_request", "無法解析金流商背景通知內容。");

            var handler = new ReceiveGatewayNotificationHandler(root.OrderNotificationQueue);
            await handler.HandleAsync(new GatewayNotificationPayload(request.Gateway, request.MerchantTradeNo, request.GatewayTransactionId, request.RtnCode, request.RawFields), ct);

            return JsonResponses.NoContent();
        }, requiresAuth: false);

    private static OrderDto ToDto(Order order) => new(
        order.Id, order.CourseId, order.TeacherId, order.StudentId, order.CoursePriceAtOrder.Cents,
        (PaymentMethodDto)order.RequestedPaymentMethod, (ChoosePaymentDto)order.GatewayChoosePayment,
        order.PaymentMethodUsed is null ? null : (PaymentMethodDto)order.PaymentMethodUsed,
        (OrderPaymentStatusDto)order.PaymentStatus, (OrderPayoutStatusDto)order.PayoutStatus, order.AtmPaymentDueAt, order.CreatedAt);
}

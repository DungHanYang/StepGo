using StepGo.Api.Shared.Composition;
using StepGo.Api.Shared.Json;
using StepGo.Api.Shared.Routing;
using StepGo.Application.RefundTickets;
using StepGo.Contracts.Json;
using StepGo.Contracts.RefundTickets;
using StepGo.Domain.RefundTickets;
using StepGo.Domain.SharedKernel;

namespace StepGo.Api.RefundTickets;

public static class RefundTicketsRoutes
{
    public static MiniRouter Map(MiniRouter router, CompositionRoot root, StepGoJsonContext contractsJson) => router
        .MapHealthCheck("/refund-tickets/health")
        .MapPost("/refund-tickets", async (ctx, ct) =>
        {
            var request = JsonResponses.Deserialize(ctx.Request.Body, contractsJson.SubmitRefundTicketRequestDto)
                ?? throw new DomainException("invalid_request", "請求內容不正確。");

            var handler = new SubmitRefundTicketHandler(root.OrderRepository, root.CourseRepository, root.RefundTicketRepository, root.BusinessDayCalendar, root.RefundSlaScheduler, root.Clock);
            var ticket = await handler.HandleAsync(new SubmitRefundTicketCommand(request.OrderId), ct);
            return JsonResponses.Created(ToDto(ticket), contractsJson.RefundTicketDto);
        })
        .MapGet("/refund-tickets/{ticketId}", async (ctx, ct) =>
        {
            var ticketId = Guid.Parse(ctx.PathParameters["ticketId"]);
            var ticket = await root.RefundTicketRepository.FindAsync(ticketId, ct) ?? throw new DomainException("refund_ticket_not_found", "找不到退課工單。");
            return JsonResponses.Ok(ToDto(ticket), contractsJson.RefundTicketDto);
        })
        .MapPost("/refund-tickets/{ticketId}/teacher-decision", async (ctx, ct) =>
        {
            var request = JsonResponses.Deserialize(ctx.Request.Body, contractsJson.TeacherRefundDecisionRequestDto)
                ?? throw new DomainException("invalid_request", "請求內容不正確。");
            var ticketId = Guid.Parse(ctx.PathParameters["ticketId"]);

            var finalizer = new RefundApprovalFinalizer(root.OrderRepository, root.PaymentGatewayClient, root.EventPublisher);
            var handler = new TeacherRefundDecisionHandler(root.RefundTicketRepository, finalizer, root.Clock);
            var ticket = await handler.HandleAsync(new TeacherRefundDecisionCommand(
                ticketId, ctx.CurrentUser.UserId, request.Approve,
                request.ApprovedAmount is { } amount ? Money.FromWholeDollars(amount) : null, request.RejectionReason), ct);

            return JsonResponses.Ok(ToDto(ticket), contractsJson.RefundTicketDto);
        })
        .MapPost("/refund-tickets/{ticketId}/escalate", async (ctx, ct) =>
        {
            var ticketId = Guid.Parse(ctx.PathParameters["ticketId"]);
            var handler = new EscalateRefundTicketHandler(root.RefundTicketRepository, root.EventPublisher, root.Clock);
            var ticket = await handler.EscalateByAppealAsync(ticketId, ct);
            return JsonResponses.Ok(ToDto(ticket), contractsJson.RefundTicketDto);
        })
        .MapPost("/refund-tickets/{ticketId}/admin-arbitration", async (ctx, ct) =>
        {
            var request = JsonResponses.Deserialize(ctx.Request.Body, contractsJson.AdminArbitrationRequestDto)
                ?? throw new DomainException("invalid_request", "請求內容不正確。");
            var ticketId = Guid.Parse(ctx.PathParameters["ticketId"]);

            StepGo.Application.Identity.RowLevelAccessGuard.GuardIsAdmin(ctx.CurrentUser);
            var finalizer = new RefundApprovalFinalizer(root.OrderRepository, root.PaymentGatewayClient, root.EventPublisher);
            var handler = new AdminArbitrationHandler(root.RefundTicketRepository, finalizer, root.Clock);
            var ticket = await handler.HandleAsync(new AdminArbitrationCommand(
                ticketId, request.Approve, request.ApprovedAmount is { } amount ? Money.FromWholeDollars(amount) : null), ct);

            return JsonResponses.Ok(ToDto(ticket), contractsJson.RefundTicketDto);
        })
        .MapPost("/refund-tickets/{ticketId}/thread", async (ctx, ct) =>
        {
            var request = JsonResponses.Deserialize(ctx.Request.Body, contractsJson.AddThreadMessageRequestDto)
                ?? throw new DomainException("invalid_request", "請求內容不正確。");
            var ticketId = Guid.Parse(ctx.PathParameters["ticketId"]);

            var handler = new ThreadAndNotesHandler(root.RefundTicketRepository, ctx.CurrentUser, root.Clock);
            var ticket = await handler.AddThreadMessageAsync(ticketId, request.Text, ct);
            return JsonResponses.Ok(ToDto(ticket), contractsJson.RefundTicketDto);
        })
        .MapPost("/refund-tickets/{ticketId}/internal-notes", async (ctx, ct) =>
        {
            var request = JsonResponses.Deserialize(ctx.Request.Body, contractsJson.AddInternalNoteRequestDto)
                ?? throw new DomainException("invalid_request", "請求內容不正確。");
            var ticketId = Guid.Parse(ctx.PathParameters["ticketId"]);

            var handler = new ThreadAndNotesHandler(root.RefundTicketRepository, ctx.CurrentUser, root.Clock);
            var ticket = await handler.AddInternalNoteAsync(ticketId, request.Text, ct);
            return JsonResponses.Ok(ToDto(ticket), contractsJson.RefundTicketDto);
        });

    private static RefundTicketDto ToDto(RefundTicket ticket) => new(
        ticket.Id, ticket.OrderId, ticket.StudentId, ticket.TeacherId, ticket.OriginalPaidAmount.Cents, ticket.EligibleRefundAmount.Cents,
        (RefundTicketStatusDto)ticket.Status, ticket.TeacherSlaDeadline, ticket.RejectionReason,
        ticket.DecidedAmount?.Cents, ticket.DecidedDeductionSource is null ? null : (DeductionSourceDto)ticket.DecidedDeductionSource,
        [.. ticket.Thread.Select(m => new ThreadMessageDto(m.AuthorRole.ToString(), m.AuthorId, m.Text, m.CreatedAt))]);
}

using StepGo.Api.Shared.Composition;
using StepGo.Api.Shared.Json;
using StepGo.Api.Shared.Routing;
using StepGo.Payouts.Application;
using StepGo.Contracts.Json;
using StepGo.Contracts.Payouts;
using StepGo.Payouts.Domain;
using StepGo.Shared.Domain;

namespace StepGo.Api.Payouts;

public static class PayoutsRoutes
{
    public static MiniRouter Map(MiniRouter router, CompositionRoot root, StepGoJsonContext contractsJson) => router
        .MapHealthCheck("/payouts/health")
        .MapGet("/payout-batches/{payoutBatchId}", async (ctx, ct) =>
        {
            var payoutBatchId = Guid.Parse(ctx.PathParameters["payoutBatchId"]);
            var handler = new GetPayoutBatchDetailHandler(root.PayoutBatchRepository);
            var batch = await handler.HandleAsync(payoutBatchId, ctx.CurrentUser, ct);
            return JsonResponses.Ok(ToDto(batch), contractsJson.PayoutBatchDto);
        })
        .MapGet("/teachers/{teacherId}/payout-batches", async (ctx, ct) =>
        {
            var teacherId = Guid.Parse(ctx.PathParameters["teacherId"]);
            StepGo.Identity.Application.RowLevelAccessGuard.GuardOwnsTeacherResource(ctx.CurrentUser, teacherId);

            var batches = await root.PayoutBatchRepository.ListByTeacherAsync(teacherId, ct);
            return JsonResponses.Ok(new StepGo.Contracts.Common.PagedResultDto<PayoutBatchDto>([.. batches.Select(ToDto)], null), contractsJson.PagedResultDtoPayoutBatchDto);
        })
        .MapPost("/payout-batches/{payoutBatchId}/mark-paid", async (ctx, ct) =>
        {
            var request = JsonResponses.Deserialize(ctx.Request.Body, contractsJson.MarkPayoutBatchPaidRequestDto)
                ?? throw new DomainException("invalid_request", "請求內容不正確。");
            var payoutBatchId = Guid.Parse(ctx.PathParameters["payoutBatchId"]);

            StepGo.Identity.Application.RowLevelAccessGuard.GuardIsAdmin(ctx.CurrentUser);
            var handler = new MarkPayoutBatchOutcomeHandler(root.PayoutBatchRepository, root.OrderRepository, root.TeacherProfileRepository, root.Clock);
            await handler.MarkPaidAsync(payoutBatchId, request.TransferReference, ct);
            return JsonResponses.NoContent();
        })
        .MapPost("/payout-batches/{payoutBatchId}/mark-bank-rejected", async (ctx, ct) =>
        {
            var request = JsonResponses.Deserialize(ctx.Request.Body, contractsJson.MarkBankRejectedRequestDto)
                ?? throw new DomainException("invalid_request", "請求內容不正確。");
            var payoutBatchId = Guid.Parse(ctx.PathParameters["payoutBatchId"]);

            StepGo.Identity.Application.RowLevelAccessGuard.GuardIsAdmin(ctx.CurrentUser);
            var handler = new MarkPayoutBatchOutcomeHandler(root.PayoutBatchRepository, root.OrderRepository, root.TeacherProfileRepository, root.Clock);
            await handler.MarkBankRejectedAsync(payoutBatchId, request.Reason, ct);
            return JsonResponses.NoContent();
        });

    private static PayoutBatchDto ToDto(PayoutBatch batch) => new(
        batch.Id, batch.TeacherId, batch.PeriodYyyyMm,
        [.. batch.OrderLines.Select(l => new PayoutBatchOrderLineDto(l.OrderId, l.NetReceivableSnapshot.Cents))],
        batch.NetReceivableTotal.Cents, batch.TransferFee.Cents, batch.NetPayout.Cents, (PayoutBatchStatusDto)batch.Status,
        batch.PaidAt, batch.TransferReference, batch.BankRejectionReason);
}

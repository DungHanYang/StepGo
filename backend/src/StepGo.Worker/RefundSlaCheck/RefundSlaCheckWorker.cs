using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using StepGo.Api.Shared.Composition;
using StepGo.Application.RefundTickets;

namespace StepGo.Worker.RefundSlaCheck;

/// <summary>
/// Task 7.3: the Step Functions state machine's Task state, invoked after its 5-business-day Wait.
/// AutoEscalateOnSlaTimeout is a no-op if the ticket already moved on (teacher approved/rejected in the
/// meantime) — that "interrupted while waiting" case is exactly why design.md chose Step Functions over
/// a bare EventBridge cron.
/// </summary>
public static class RefundSlaCheckWorker
{
    public static async Task<RefundSlaCheckOutput> HandleAsync(CompositionRoot root, RefundSlaCheckInput input, ILambdaContext context)
    {
        var handler = new EscalateRefundTicketHandler(root.RefundTicketRepository, root.EventPublisher, root.Clock);
        var ticket = await handler.EscalateOnSlaTimeoutAsync(input.TicketId, CancellationToken.None);
        return new RefundSlaCheckOutput(ticket.Status.ToString());
    }
}

public sealed record RefundSlaCheckInput(Guid TicketId);
public sealed record RefundSlaCheckOutput(string Status);

[JsonSerializable(typeof(RefundSlaCheckInput))]
[JsonSerializable(typeof(RefundSlaCheckOutput))]
public partial class RefundSlaCheckJsonContext : JsonSerializerContext;

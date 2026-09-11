using System.Text.Json.Serialization;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using StepGo.Api.Shared.Composition;
using StepGo.Application.RefundTickets;

var root = new CompositionRoot();

// Task 7.3: the Step Functions state machine's Task state, invoked after its 5-business-day Wait.
// AutoEscalateOnSlaTimeout is a no-op if the ticket already moved on (teacher approved/rejected in the
// meantime) — that "interrupted while waiting" case is exactly why design.md chose Step Functions over
// a bare EventBridge cron.
async Task<RefundSlaCheckOutput> HandleAsync(RefundSlaCheckInput input, Amazon.Lambda.Core.ILambdaContext context)
{
    var handler = new EscalateRefundTicketHandler(root.RefundTicketRepository, root.EventPublisher, root.Clock);
    var ticket = await handler.EscalateOnSlaTimeoutAsync(input.TicketId, CancellationToken.None);
    return new RefundSlaCheckOutput(ticket.Status.ToString());
}

await LambdaBootstrapBuilder.Create<RefundSlaCheckInput, RefundSlaCheckOutput>(HandleAsync, new SourceGeneratorLambdaJsonSerializer<RefundSlaCheckJsonContext>())
    .Build()
    .RunAsync();

public sealed record RefundSlaCheckInput(Guid TicketId);
public sealed record RefundSlaCheckOutput(string Status);

[JsonSerializable(typeof(RefundSlaCheckInput))]
[JsonSerializable(typeof(RefundSlaCheckOutput))]
public partial class RefundSlaCheckJsonContext : JsonSerializerContext;

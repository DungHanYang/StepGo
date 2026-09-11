using System.Text.Json.Serialization;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using StepGo.Application.RefundTickets;

namespace StepGo.Infrastructure.Aws;

public sealed record RefundSlaExecutionInput(Guid TicketId);

[JsonSerializable(typeof(RefundSlaExecutionInput))]
public partial class RefundSlaExecutionInputJsonContext : JsonSerializerContext;

/// <summary>Starts the refund-SLA Step Functions execution (task 7.3) — one execution per ticket, named by ticket id so a duplicate Submit is naturally idempotent (StartExecution rejects a reused name while the earlier execution is still running).</summary>
public sealed class StepFunctionsRefundSlaScheduler(IAmazonStepFunctions client, string stateMachineArn) : IRefundSlaScheduler
{
    private static readonly RefundSlaExecutionInputJsonContext JsonContext = new();

    public Task StartTrackingAsync(Guid ticketId, CancellationToken ct) => client.StartExecutionAsync(new StartExecutionRequest
    {
        StateMachineArn = stateMachineArn,
        Name = ticketId.ToString(),
        Input = System.Text.Json.JsonSerializer.Serialize(new RefundSlaExecutionInput(ticketId), JsonContext.RefundSlaExecutionInput),
    }, ct);
}

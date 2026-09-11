using Amazon.SQS;
using Amazon.SQS.Model;
using StepGo.Orders.Application;

namespace StepGo.Orders.Infrastructure;

/// <summary>The webhook receiver's durable hand-off to SQS (design.md decision 5; DLQ/redrive configured on the queue itself in CDK).</summary>
public sealed class SqsOrderNotificationQueue(IAmazonSQS client, string queueUrl) : IOrderNotificationQueue
{
    private static readonly InfrastructureJsonContext JsonContext = new();

    public Task EnqueueAsync(GatewayNotificationPayload notification, CancellationToken ct)
    {
        var body = System.Text.Json.JsonSerializer.Serialize(notification, JsonContext.GatewayNotificationPayload);
        return client.SendMessageAsync(new SendMessageRequest { QueueUrl = queueUrl, MessageBody = body }, ct);
    }
}

namespace StepGo.Orders.Application;

/// <summary>
/// Task 4.2: the API Gateway endpoint hit directly by 綠界/藍新's webhook. It never touches order state
/// itself — it only durably buffers the payload to SQS so a transient Lambda fault or traffic spike
/// can't drop a notification (design.md decision 5).
/// </summary>
public sealed class ReceiveGatewayNotificationHandler(IOrderNotificationQueue queue)
{
    public Task HandleAsync(GatewayNotificationPayload notification, CancellationToken ct)
        => queue.EnqueueAsync(notification, ct);
}

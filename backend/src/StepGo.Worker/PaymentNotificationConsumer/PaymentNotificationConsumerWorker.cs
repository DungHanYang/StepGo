using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using StepGo.Api.Shared.Composition;
using StepGo.Orders.Application;
using StepGo.Orders.Infrastructure;
using StepGo.Courses.Domain;

namespace StepGo.Worker.PaymentNotificationConsumer;

/// <summary>
/// Task 4.3: SQS-triggered consumer for the payment-gateway webhook queue. Idempotency is enforced inside
/// ProcessGatewayNotificationHandler before any order mutation — a redelivered message is a no-op here.
/// </summary>
public static class PaymentNotificationConsumerWorker
{
    private static readonly InfrastructureJsonContext InfraJson = new();

    public static async Task HandleBatchAsync(CompositionRoot root, SQSEvent sqsEvent, ILambdaContext context)
    {
        var handler = new ProcessGatewayNotificationHandler(root.OrderRepository, root.IdempotencyStore, root.FeeRateScheduleProvider, root.EventPublisher, root.Clock);

        foreach (var record in sqsEvent.Records)
        {
            var payload = System.Text.Json.JsonSerializer.Deserialize(record.Body, InfraJson.GatewayNotificationPayload)
                ?? throw new InvalidOperationException("無法解析金流通知訊息內容。");

            var orderId = Guid.ParseExact(payload.MerchantTradeNo, "N");
            var methodUsed = DeterminePaymentMethod(payload);

            await handler.HandleAsync(orderId, payload, methodUsed, CancellationToken.None);
        }
    }

    private static PaymentMethod DeterminePaymentMethod(GatewayNotificationPayload payload)
        => payload.RawFields.TryGetValue("PaymentType", out var paymentType) && paymentType.StartsWith("Credit", StringComparison.OrdinalIgnoreCase)
            ? PaymentMethod.CreditCard
            : PaymentMethod.Atm;
}

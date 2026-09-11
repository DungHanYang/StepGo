using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using StepGo.Api.Shared.Composition;
using StepGo.Api.Shared.Json;
using StepGo.Application.Orders;
using StepGo.Domain.Courses;

var root = new CompositionRoot();
var infraJson = new StepGo.Infrastructure.Aws.InfrastructureJsonContext();

// Task 4.3: SQS-triggered consumer for the payment-gateway webhook queue. Idempotency is enforced inside
// ProcessGatewayNotificationHandler before any order mutation — a redelivered message is a no-op here.
async Task HandleBatchAsync(SQSEvent sqsEvent, Amazon.Lambda.Core.ILambdaContext context)
{
    var handler = new ProcessGatewayNotificationHandler(root.OrderRepository, root.IdempotencyStore, root.FeeRateScheduleProvider, root.EventPublisher, root.Clock);

    foreach (var record in sqsEvent.Records)
    {
        var payload = System.Text.Json.JsonSerializer.Deserialize(record.Body, infraJson.GatewayNotificationPayload)
            ?? throw new InvalidOperationException("無法解析金流通知訊息內容。");

        var orderId = Guid.ParseExact(payload.MerchantTradeNo, "N");
        var methodUsed = DeterminePaymentMethod(payload);

        await handler.HandleAsync(orderId, payload, methodUsed, CancellationToken.None);
    }
}

static PaymentMethod DeterminePaymentMethod(GatewayNotificationPayload payload)
    => payload.RawFields.TryGetValue("PaymentType", out var paymentType) && paymentType.StartsWith("Credit", StringComparison.OrdinalIgnoreCase)
        ? PaymentMethod.CreditCard
        : PaymentMethod.Atm;

await LambdaBootstrapBuilder.Create<SQSEvent>(HandleBatchAsync, new SourceGeneratorLambdaJsonSerializer<ApiEventJsonContext>())
    .Build()
    .RunAsync();

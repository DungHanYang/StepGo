using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using StepGo.Api.Shared.Composition;
using StepGo.Application.Notifications;
using StepGo.Domain.Orders;
using StepGo.Domain.RefundTickets;
using StepGo.Infrastructure.Aws;

var root = new CompositionRoot();
var domainEventJson = new DomainEventJsonContext();

// Task 9.1: subscribed via an EventBridge rule matching every detail-type this capability cares about
// (design.md decision 5's per-event detail-type scheme). One rule -> one Lambda -> a switch here, rather
// than one Lambda per event type, keeps the notification-channel logic (task 9.2) in a single place.
async Task HandleAsync(NotificationEventEnvelope envelope, Amazon.Lambda.Core.ILambdaContext context)
{
    var dispatcher = new DispatchNotificationHandler(
        root.NotificationTemplateRepository, root.NotificationRecordRepository, root.RecipientContactLookup, root.EmailSender, root.LineMessenger, root.Clock);

    var detailJson = envelope.Detail.GetRawText();

    switch (envelope.DetailType)
    {
        case "OrderPaymentConfirmed":
        {
            var e = JsonSerializer.Deserialize(detailJson, domainEventJson.OrderPaymentConfirmedEvent)!;
            await dispatcher.HandleAsync(e.StudentId, "OrderPaymentConfirmed", new Dictionary<string, string> { ["amount"] = e.AmountPaid.Cents.ToString() }, CancellationToken.None);
            break;
        }
        case "OrderPaymentOverdue":
        {
            var e = JsonSerializer.Deserialize(detailJson, domainEventJson.OrderPaymentOverdueEvent)!;
            await dispatcher.HandleAsync(e.StudentId, "OrderPaymentOverdue", new Dictionary<string, string>(), CancellationToken.None);
            break;
        }
        case "RefundApproved":
        {
            var e = JsonSerializer.Deserialize(detailJson, domainEventJson.RefundApprovedEvent)!;
            var variables = new Dictionary<string, string> { ["amount"] = e.ApprovedAmount.Cents.ToString() };
            await dispatcher.HandleAsync(e.StudentId, "RefundApproved", variables, CancellationToken.None);

            // Task 7.5's notification requirement: a teacher whose already-paid-out order gets refunded
            // must be told why their next payout is smaller.
            if (e.DeductionSource == DeductionSource.NextTeacherPayout)
            {
                await dispatcher.HandleAsync(e.TeacherId, "RefundDeductedFromNextPayout", variables, CancellationToken.None);
            }
            break;
        }
        case "RefundTicketEscalatedToArbitration":
        {
            var e = JsonSerializer.Deserialize(detailJson, domainEventJson.RefundTicketEscalatedToArbitrationEvent)!;
            await dispatcher.HandleAsync(e.TicketId, "RefundTicketEscalatedToArbitration", new Dictionary<string, string>(), CancellationToken.None);
            break;
        }
        default:
            context.Logger.LogWarning($"No notification mapping registered for detail-type: {envelope.DetailType}");
            break;
    }
}

await LambdaBootstrapBuilder.Create<NotificationEventEnvelope>(HandleAsync, new SourceGeneratorLambdaJsonSerializer<NotificationEventEnvelopeJsonContext>())
    .Build()
    .RunAsync();

public sealed record NotificationEventEnvelope([property: JsonPropertyName("detail-type")] string DetailType, JsonElement Detail);

[JsonSerializable(typeof(NotificationEventEnvelope))]
public partial class NotificationEventEnvelopeJsonContext : JsonSerializerContext;

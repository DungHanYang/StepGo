using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using StepGo.Shared.Application;
using StepGo.Orders.Domain;
using StepGo.RefundTickets.Domain;
using StepGo.Shared.Domain;

namespace StepGo.Api.Shared.Infrastructure;

/// <summary>Publishes each domain event to the custom bus under its own EventBridge detail-type (design.md decision 5's event-schema decision).</summary>
public sealed class EventBridgePublisher(IAmazonEventBridge client, string eventBusName) : IEventPublisher
{
    private static readonly DomainEventJsonContext JsonContext = new();

    public async Task PublishAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct)
    {
        if (events.Count == 0)
        {
            return;
        }

        var entries = events.Select(e => new PutEventsRequestEntry
        {
            EventBusName = eventBusName,
            Source = "stepgo.backend",
            DetailType = e.EventBridgeDetailType,
            Detail = Serialize(e),
        }).ToList();

        await client.PutEventsAsync(new PutEventsRequest { Entries = entries }, ct);
    }

    private static string Serialize(IDomainEvent e) => e switch
    {
        OrderPaymentConfirmedEvent ev => System.Text.Json.JsonSerializer.Serialize(ev, JsonContext.OrderPaymentConfirmedEvent),
        OrderPaymentOverdueEvent ev => System.Text.Json.JsonSerializer.Serialize(ev, JsonContext.OrderPaymentOverdueEvent),
        RefundApprovedEvent ev => System.Text.Json.JsonSerializer.Serialize(ev, JsonContext.RefundApprovedEvent),
        RefundTicketEscalatedToArbitrationEvent ev => System.Text.Json.JsonSerializer.Serialize(ev, JsonContext.RefundTicketEscalatedToArbitrationEvent),
        _ => throw new NotSupportedException($"No AOT-safe serializer registered for domain event type {e.GetType().Name}."),
    };
}

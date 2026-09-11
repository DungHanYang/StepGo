using System.Text.Json.Serialization;
using StepGo.Orders.Domain;
using StepGo.RefundTickets.Domain;

namespace StepGo.Api.Shared.Infrastructure;

/// <summary>Source-generated serialization for every IDomainEvent published to EventBridge — reflection-based JsonSerializer.Serialize(object, Type) is not Native-AOT safe.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(OrderPaymentConfirmedEvent))]
[JsonSerializable(typeof(OrderPaymentOverdueEvent))]
[JsonSerializable(typeof(RefundApprovedEvent))]
[JsonSerializable(typeof(RefundTicketEscalatedToArbitrationEvent))]
public partial class DomainEventJsonContext : JsonSerializerContext;

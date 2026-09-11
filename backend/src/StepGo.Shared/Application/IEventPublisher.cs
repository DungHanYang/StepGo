using StepGo.Shared.Domain;

namespace StepGo.Shared.Application;

/// <summary>Publishes domain events to the EventBridge custom bus, one `detail-type` per event class.</summary>
public interface IEventPublisher
{
    Task PublishAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct);
}

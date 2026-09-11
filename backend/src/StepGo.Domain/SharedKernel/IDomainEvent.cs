namespace StepGo.Domain.SharedKernel;

/// <summary>
/// Marker for domain events. <see cref="EventBridgeDetailType"/> becomes the EventBridge
/// "detail-type" verbatim when StepGo.Infrastructure publishes it — one detail-type per event class.
/// </summary>
public interface IDomainEvent
{
    string EventBridgeDetailType { get; }
    DateTimeOffset OccurredAt { get; }
}

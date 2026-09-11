namespace StepGo.Shared.Domain;

/// <summary>
/// Marker for domain events. <see cref="EventBridgeDetailType"/> becomes the EventBridge
/// "detail-type" verbatim when EventBridgePublisher publishes it — one detail-type per event class.
/// </summary>
public interface IDomainEvent
{
    string EventBridgeDetailType { get; }
    DateTimeOffset OccurredAt { get; }
}

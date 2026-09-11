namespace StepGo.Application.Orders;

/// <summary>
/// Backs the webhook-consumer idempotency guard: a conditional write on the gateway's unique transaction id
/// (attribute_not_exists), per design.md decision 4. TryReserve returns false when this id was already processed.
/// </summary>
public interface IPaymentNotificationIdempotencyStore
{
    Task<bool> TryReserveAsync(string gatewayTransactionId, CancellationToken ct);
}

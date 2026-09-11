namespace StepGo.Application.Orders;

/// <summary>
/// The raw gateway callback payload, expressed purely in Application-owned terms (StepGo.Application
/// depends only on StepGo.Domain per design.md decision 1 — never on StepGo.Contracts). The
/// StepGo.Api project's Orders routes map the wire-format DTO into this shape.
/// </summary>
public sealed record GatewayNotificationPayload(string Gateway, string MerchantTradeNo, string GatewayTransactionId, string RtnCode, IReadOnlyDictionary<string, string> RawFields);

/// <summary>The webhook receiver's only job: durably hand the raw gateway payload to SQS (design.md decision 5).</summary>
public interface IOrderNotificationQueue
{
    Task EnqueueAsync(GatewayNotificationPayload notification, CancellationToken ct);
}

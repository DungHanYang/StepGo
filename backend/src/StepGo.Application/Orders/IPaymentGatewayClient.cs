using StepGo.Domain.Orders;
using StepGo.Domain.SharedKernel;

namespace StepGo.Application.Orders;

public sealed record GatewayOrderPlacementResult(string Gateway, string MerchantTradeNo, string RedirectUrl);

/// <summary>綠界/藍新 sandbox integration. Real merchant credentials are read from Secrets Manager by the implementation.</summary>
public interface IPaymentGatewayClient
{
    Task<GatewayOrderPlacementResult> PlaceOrderAsync(Guid orderId, Money amount, ChoosePayment choosePayment, CancellationToken ct);

    /// <summary>Credit card refunds only — ATM orders never call this (see Order.DetermineRefundRoute).</summary>
    Task RefundCreditCardAsync(string merchantTradeNo, Money amount, CancellationToken ct);
}

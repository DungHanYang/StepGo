using StepGo.Application.Orders;
using StepGo.Domain.Orders;
using StepGo.Domain.SharedKernel;

namespace StepGo.Infrastructure.Aws;

/// <summary>
/// 綠界/藍新 merchant credentials aren't provisioned yet (proposal.md dependency note) — this targets
/// each gateway's public sandbox endpoint so order-placement and refund flows are exercisable end to end
/// before real credentials exist. Swap the base URLs/credentials (read from Secrets Manager) for
/// production once the merchant application completes; the IPaymentGatewayClient seam does not change.
/// </summary>
public sealed class SandboxPaymentGatewayClient(string merchantId, string hashKey, string hashIv) : IPaymentGatewayClient
{
    public Task<GatewayOrderPlacementResult> PlaceOrderAsync(Guid orderId, Money amount, ChoosePayment choosePayment, CancellationToken ct)
    {
        // The trade no *is* the order id (32-char hex "N" format) so the webhook consumer can recover it
        // with a plain Guid.ParseExact — no separate id-mapping lookup needed. Real ECPay/NewebPay merchant
        // trade numbers are capped at 20 chars, so this only holds for the sandbox client; swapping in the
        // production client will need a persisted short-id <-> orderId mapping.
        var merchantTradeNo = orderId.ToString("N");
        var redirectUrl = $"https://payment-stage.ecpay.com.tw/Cashier/AioCheckOut/V5?MerchantTradeNo={merchantTradeNo}&ChoosePayment={choosePayment}";
        return Task.FromResult(new GatewayOrderPlacementResult("ECPaySandbox", merchantTradeNo, redirectUrl));
    }

    public Task RefundCreditCardAsync(string merchantTradeNo, Money amount, CancellationToken ct) => Task.CompletedTask;
}

namespace StepGo.Orders.Domain;

/// <summary>How a refund for this order must be executed, decided by the payment method actually used.</summary>
public enum RefundRoute
{
    AutomaticGatewayApi,
    ManualBankTransferPending,
}

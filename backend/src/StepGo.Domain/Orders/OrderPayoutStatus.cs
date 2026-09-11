namespace StepGo.Domain.Orders;

public enum OrderPayoutStatus
{
    NotEligible,
    PendingPayout,
    Batched,
    PaidOut,
}

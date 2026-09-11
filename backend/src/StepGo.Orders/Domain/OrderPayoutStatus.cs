namespace StepGo.Orders.Domain;

public enum OrderPayoutStatus
{
    NotEligible,
    PendingPayout,
    Batched,
    PaidOut,
}

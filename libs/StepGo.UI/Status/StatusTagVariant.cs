namespace StepGo.UI.Status;

/// <summary>
/// Color semantics for <see cref="Components.StatusTag"/>. Every domain status enum
/// (payment/verification/payout account/refund ticket) maps onto one of these —
/// StatusTag itself never receives a hex literal or a domain-specific color name.
/// </summary>
public enum StatusTagVariant
{
    Neutral,
    Positive,
    Warning,
    Negative,
    Info,
}

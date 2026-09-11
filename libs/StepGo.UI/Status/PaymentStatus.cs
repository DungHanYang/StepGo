namespace StepGo.UI.Status;

/// <summary>
/// Order payment status (5 states) — the state machine behind Checkout, "我的課程"
/// tab filtering, and teacher/admin ledgers. Shared by every app so a given order
/// always shows the same label/color everywhere (frontend-design-system spec).
/// </summary>
public enum PaymentStatus
{
    /// <summary>待付款 — ATM virtual account issued, awaiting bank transfer.</summary>
    Pending,

    /// <summary>付款處理中 — gateway callback not yet received.</summary>
    Processing,

    /// <summary>已付款.</summary>
    Paid,

    /// <summary>付款失敗.</summary>
    Failed,

    /// <summary>已退款.</summary>
    Refunded,
}

public static class PaymentStatusExtensions
{
    public static (string Label, StatusTagVariant Variant) ToDisplay(this PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => ("待付款", StatusTagVariant.Warning),
        PaymentStatus.Processing => ("處理中", StatusTagVariant.Info),
        PaymentStatus.Paid => ("已付款", StatusTagVariant.Positive),
        PaymentStatus.Failed => ("付款失敗", StatusTagVariant.Negative),
        PaymentStatus.Refunded => ("已退款", StatusTagVariant.Neutral),
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };
}

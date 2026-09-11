namespace StepGo.UI.Status;

/// <summary>
/// Teacher payout (bank) account status (4 states) —
/// frontend-teacher-portal spec: "撥款帳戶四狀態與戶名一致性提示".
/// </summary>
public enum PayoutAccountStatus
{
    /// <summary>檢視 — account on file, unchanged.</summary>
    Confirmed,

    /// <summary>變更中 — teacher is editing account details.</summary>
    Changing,

    /// <summary>核對中 — submitted change awaiting reconciliation.</summary>
    Reconciling,

    /// <summary>銀行退回 — name mismatch or other bank rejection; can resubmit.</summary>
    BankRejected,
}

public static class PayoutAccountStatusExtensions
{
    public static (string Label, StatusTagVariant Variant) ToDisplay(this PayoutAccountStatus status) => status switch
    {
        PayoutAccountStatus.Confirmed => ("檢視", StatusTagVariant.Positive),
        PayoutAccountStatus.Changing => ("變更中", StatusTagVariant.Info),
        PayoutAccountStatus.Reconciling => ("核對中", StatusTagVariant.Warning),
        PayoutAccountStatus.BankRejected => ("銀行退回", StatusTagVariant.Negative),
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };
}

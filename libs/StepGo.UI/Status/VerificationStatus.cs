namespace StepGo.UI.Status;

/// <summary>
/// Teacher identity verification status (4 states) — gates course creation
/// (frontend-teacher-portal spec: "身分驗證四狀態控管課程建立權限").
/// </summary>
public enum VerificationStatus
{
    /// <summary>填寫 — form not yet submitted.</summary>
    Filling,

    /// <summary>審核中.</summary>
    UnderReview,

    /// <summary>未通過 — can resubmit supporting documents.</summary>
    Rejected,

    /// <summary>通過.</summary>
    Verified,
}

public static class VerificationStatusExtensions
{
    public static (string Label, StatusTagVariant Variant) ToDisplay(this VerificationStatus status) => status switch
    {
        VerificationStatus.Filling => ("填寫中", StatusTagVariant.Neutral),
        VerificationStatus.UnderReview => ("審核中", StatusTagVariant.Warning),
        VerificationStatus.Rejected => ("未通過", StatusTagVariant.Negative),
        VerificationStatus.Verified => ("已通過", StatusTagVariant.Positive),
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };
}

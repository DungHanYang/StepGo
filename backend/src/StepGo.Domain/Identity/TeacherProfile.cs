using StepGo.Domain.SharedKernel;

namespace StepGo.Domain.Identity;

/// <summary>
/// Teacher is a 1:1 identity extension of a user (teacherId == userId), per design.md decision 4.
/// NationalId is stored encrypted by StepGo.Infrastructure; the Domain only carries the plaintext
/// value transiently within a single request and never exposes it to student-facing contracts.
/// </summary>
public sealed class TeacherProfile : AggregateRoot<Guid>
{
    public string RealName { get; private set; }
    public string NationalId { get; private set; }
    public string IdPhotoFrontObjectKey { get; private set; }
    public string IdPhotoBackObjectKey { get; private set; }
    public BankAccount PayoutAccount { get; private set; }
    public TeacherVerificationStatus VerificationStatus { get; private set; }
    public string? RejectionReason { get; private set; }

    private TeacherProfile(
        Guid teacherId,
        string realName,
        string nationalId,
        string idPhotoFrontObjectKey,
        string idPhotoBackObjectKey,
        BankAccount payoutAccount,
        TeacherVerificationStatus verificationStatus,
        string? rejectionReason)
        : base(teacherId)
    {
        RealName = realName;
        NationalId = nationalId;
        IdPhotoFrontObjectKey = idPhotoFrontObjectKey;
        IdPhotoBackObjectKey = idPhotoBackObjectKey;
        PayoutAccount = payoutAccount;
        VerificationStatus = verificationStatus;
        RejectionReason = rejectionReason;
    }

    /// <summary>Submits (or re-submits) a verification request. Payout account holder name must match the real name exactly.</summary>
    public static TeacherProfile Submit(
        Guid teacherId,
        string realName,
        string nationalId,
        string idPhotoFrontObjectKey,
        string idPhotoBackObjectKey,
        BankAccount payoutAccount)
    {
        if (!string.Equals(payoutAccount.AccountHolderName.Trim(), realName.Trim(), StringComparison.Ordinal))
        {
            throw new DomainException(
                "payout_account_name_mismatch",
                "撥款帳戶戶名與真實姓名不一致，請確認後重新送出。");
        }

        return new TeacherProfile(
            teacherId, realName.Trim(), nationalId, idPhotoFrontObjectKey, idPhotoBackObjectKey,
            payoutAccount, TeacherVerificationStatus.PendingReview, rejectionReason: null);
    }

    public static TeacherProfile Rehydrate(
        Guid teacherId, string realName, string nationalId, string idPhotoFrontObjectKey, string idPhotoBackObjectKey,
        BankAccount payoutAccount, TeacherVerificationStatus verificationStatus, string? rejectionReason)
        => new(teacherId, realName, nationalId, idPhotoFrontObjectKey, idPhotoBackObjectKey, payoutAccount, verificationStatus, rejectionReason);

    public void Approve()
    {
        VerificationStatus = TeacherVerificationStatus.Verified;
        RejectionReason = null;
    }

    public void Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("rejection_reason_required", "退回審核時必須填寫理由。");
        }

        VerificationStatus = TeacherVerificationStatus.Rejected;
        RejectionReason = reason;
    }

    public bool CanCreateOrPublishCourse => VerificationStatus == TeacherVerificationStatus.Verified;

    public void GuardCanCreateOrPublishCourse()
    {
        if (!CanCreateOrPublishCourse)
        {
            throw new DomainException("teacher_not_verified", "老師身分尚未完成驗證，無法建立或發佈課程。");
        }
    }
}

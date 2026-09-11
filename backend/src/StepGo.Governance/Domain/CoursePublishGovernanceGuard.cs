using StepGo.Shared.Domain;

namespace StepGo.Governance.Domain;

/// <summary>
/// Once a new terms version's effective date has passed, a teacher who has not re-consented to it may
/// not publish new courses — but their existing orders remain unaffected (they keep the fee-schedule
/// version bound at creation time; see FeeLedger).
/// </summary>
public static class CoursePublishGovernanceGuard
{
    public static void GuardCanPublish(TermsVersion latestTerms, TeacherConsent? teacherConsent, DateTimeOffset now, DateTimeOffset latestTermsEffectiveDate)
    {
        if (now < latestTermsEffectiveDate)
        {
            return;
        }

        if (teacherConsent is null || teacherConsent.TermsVersionId != latestTerms.Id)
        {
            throw new DomainException("terms_reconsent_required", "新條款已生效，尚未重新同意新版條款前無法發佈新課程。");
        }
    }
}

using StepGo.Domain.Governance;

namespace StepGo.Domain.Tests.Governance;

public class TermsVersionAndConsentTests
{
    [Fact]
    public void GuardCanPublish_BeforeEffectiveDate_AllowsEvenWithoutConsent()
    {
        var terms = new TermsVersion(Guid.NewGuid(), 2, "新條款內容", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        var effectiveDate = now.AddDays(10);

        CoursePublishGovernanceGuard.GuardCanPublish(terms, teacherConsent: null, now, effectiveDate); // no throw
    }

    [Fact]
    public void GuardCanPublish_AfterEffectiveDateWithoutConsent_Throws()
    {
        var terms = new TermsVersion(Guid.NewGuid(), 2, "新條款內容", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        var effectiveDate = now.AddDays(-1);

        var ex = Assert.Throws<StepGo.Domain.SharedKernel.DomainException>(() =>
            CoursePublishGovernanceGuard.GuardCanPublish(terms, teacherConsent: null, now, effectiveDate));

        Assert.Equal("terms_reconsent_required", ex.Code);
    }

    [Fact]
    public void GuardCanPublish_AfterEffectiveDateWithConsent_Allows()
    {
        var terms = new TermsVersion(Guid.NewGuid(), 2, "新條款內容", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        var effectiveDate = now.AddDays(-1);
        var consent = new TeacherConsent(Guid.NewGuid(), terms.Id, now);

        CoursePublishGovernanceGuard.GuardCanPublish(terms, consent, now, effectiveDate); // no throw
    }
}

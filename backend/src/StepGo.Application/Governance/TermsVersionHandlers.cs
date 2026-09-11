using StepGo.Application.Common;
using StepGo.Domain.Governance;
using StepGo.Domain.SharedKernel;

namespace StepGo.Application.Governance;

public sealed record PublishTermsVersionCommand(string Content);

/// <summary>Task 8.3: publishing always adds a new version; prior content stays queryable forever.</summary>
public sealed class PublishTermsVersionHandler(ITermsVersionRepository repository, ICurrentUserAccessor currentUser, IClock clock)
{
    public async Task<TermsVersion> HandleAsync(PublishTermsVersionCommand command, CancellationToken ct)
    {
        if (currentUser.Role != StepGo.Domain.Identity.Role.Admin)
        {
            throw new AuthorizationException("僅管理者可發佈合作條款。");
        }

        var latest = await repository.GetLatestAsync(ct);
        var nextVersionNumber = (latest?.VersionNumber ?? 0) + 1;

        var termsVersion = new TermsVersion(Guid.NewGuid(), nextVersionNumber, command.Content, currentUser.UserId, clock.UtcNow);
        await repository.SaveAsync(termsVersion, ct);
        return termsVersion;
    }
}

public sealed record RecordTeacherConsentCommand(Guid TeacherId, Guid TermsVersionId);

/// <summary>Task 8.4 (consent half): binds the teacher's agreement to a specific version id and timestamp.</summary>
public sealed class RecordTeacherConsentHandler(ITermsVersionRepository termsRepository, ITeacherConsentRepository consentRepository, IClock clock)
{
    public async Task<TeacherConsent> HandleAsync(RecordTeacherConsentCommand command, CancellationToken ct)
    {
        _ = await termsRepository.FindAsync(command.TermsVersionId, ct)
            ?? throw new DomainException("terms_version_not_found", "找不到該版本的合作條款。");

        var consent = new TeacherConsent(command.TeacherId, command.TermsVersionId, clock.UtcNow);
        await consentRepository.SaveAsync(consent, ct);
        return consent;
    }
}

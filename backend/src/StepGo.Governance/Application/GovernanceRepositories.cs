using StepGo.Governance.Domain;

namespace StepGo.Governance.Application;

public interface IPlatformFeeSettingRepository
{
    Task<PlatformFeeSetting?> GetCurrentAsync(CancellationToken ct);
    Task<PlatformFeeSetting?> GetLatestPublishedAsync(CancellationToken ct);
    Task<PlatformFeeSetting?> GetByVersionAsync(string versionId, CancellationToken ct);
    Task SaveAsync(PlatformFeeSetting setting, CancellationToken ct);
}

public interface ITermsVersionRepository
{
    Task<TermsVersion?> GetLatestAsync(CancellationToken ct);
    Task<TermsVersion?> FindAsync(Guid termsVersionId, CancellationToken ct);
    Task SaveAsync(TermsVersion termsVersion, CancellationToken ct);
}

public interface ITeacherConsentRepository
{
    Task<TeacherConsent?> FindLatestForTeacherAsync(Guid teacherId, CancellationToken ct);
    Task SaveAsync(TeacherConsent consent, CancellationToken ct);
}

public interface IChangeLogRepository
{
    Task AppendAsync(ChangeLogEntry entry, CancellationToken ct);
    Task<IReadOnlyList<ChangeLogEntry>> ListByCategoryAsync(ChangeLogCategory category, CancellationToken ct);
}

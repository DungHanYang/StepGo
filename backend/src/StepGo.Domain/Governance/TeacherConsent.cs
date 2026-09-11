namespace StepGo.Domain.Governance;

public sealed record TeacherConsent(Guid TeacherId, Guid TermsVersionId, DateTimeOffset ConsentedAt);

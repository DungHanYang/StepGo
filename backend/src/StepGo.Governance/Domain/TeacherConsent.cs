namespace StepGo.Governance.Domain;

public sealed record TeacherConsent(Guid TeacherId, Guid TermsVersionId, DateTimeOffset ConsentedAt);

using StepGo.Contracts.Courses;

namespace StepGo.Contracts.Governance;

public sealed record ProposeFeeSettingRequestDto(
    decimal PlatformServiceFeeRate, long CrossBankTransferFee, IReadOnlyList<RefundTierDto> RefundFloor, DateTimeOffset EffectiveDate);

public sealed record PlatformFeeSettingDto(
    string VersionId, decimal PlatformServiceFeeRate, long CrossBankTransferFee,
    IReadOnlyList<RefundTierDto> RefundFloor, DateTimeOffset EffectiveDate, Guid ProposedBy, DateTimeOffset ProposedAt);

public sealed record ChangeLogEntryDto(Guid Id, string Category, string Description, Guid OperatorId, DateTimeOffset OccurredAt);

public sealed record PublishTermsVersionRequestDto(string Content);

public sealed record TermsVersionDto(Guid Id, int VersionNumber, string Content, Guid PublishedBy, DateTimeOffset PublishedAt);

public sealed record RecordTeacherConsentRequestDto(Guid TermsVersionId);

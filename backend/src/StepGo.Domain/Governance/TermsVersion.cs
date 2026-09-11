using StepGo.Domain.SharedKernel;

namespace StepGo.Domain.Governance;

/// <summary>Publishing a new version never overwrites a prior one — old content stays queryable forever.</summary>
public sealed class TermsVersion(Guid id, int versionNumber, string content, Guid publishedBy, DateTimeOffset publishedAt)
    : Entity<Guid>(id)
{
    public int VersionNumber { get; } = versionNumber;
    public string Content { get; } = content;
    public Guid PublishedBy { get; } = publishedBy;
    public DateTimeOffset PublishedAt { get; } = publishedAt;
}

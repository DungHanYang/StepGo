namespace StepGo.Governance.Domain;

public sealed class ChangeLogEntry(Guid id, ChangeLogCategory category, string description, Guid operatorId, DateTimeOffset occurredAt)
    : StepGo.Shared.Domain.Entity<Guid>(id)
{
    public ChangeLogCategory Category { get; } = category;
    public string Description { get; } = description;
    public Guid OperatorId { get; } = operatorId;
    public DateTimeOffset OccurredAt { get; } = occurredAt;
}

using StepGo.Governance.Domain;

namespace StepGo.Governance.Application;

/// <summary>Task 8.2.</summary>
public sealed class ListChangeLogHandler(IChangeLogRepository changeLog)
{
    public Task<IReadOnlyList<ChangeLogEntry>> HandleAsync(ChangeLogCategory category, CancellationToken ct)
        => changeLog.ListByCategoryAsync(category, ct);
}

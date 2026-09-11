using StepGo.Domain.Governance;

namespace StepGo.Application.Governance;

/// <summary>Task 8.2.</summary>
public sealed class ListChangeLogHandler(IChangeLogRepository changeLog)
{
    public Task<IReadOnlyList<ChangeLogEntry>> HandleAsync(ChangeLogCategory category, CancellationToken ct)
        => changeLog.ListByCategoryAsync(category, ct);
}

using StepGo.AdminPanel.Models;

namespace StepGo.AdminPanel.Services;

public static class AuditLogFiltering
{
    public static IReadOnlyList<AuditLogEntry> ByCategory(IEnumerable<AuditLogEntry> entries, AuditLogCategory? category) =>
        category is null ? entries.ToList() : entries.Where(e => e.Category == category.Value).ToList();
}

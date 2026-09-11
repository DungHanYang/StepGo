using StepGo.AdminPanel.Models;
using StepGo.UI.Status;

namespace StepGo.AdminPanel.Services;

public static class DirectoryFiltering
{
    public static IReadOnlyList<DirectoryEntry> Filter(
        IEnumerable<DirectoryEntry> entries,
        VerificationStatus? verificationStatus,
        string? courseStatus,
        string? keyword)
    {
        var query = entries.AsEnumerable();

        if (verificationStatus.HasValue)
        {
            query = query.Where(e => e.VerificationStatus == verificationStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(courseStatus))
        {
            query = query.Where(e => e.CourseStatus == courseStatus);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(e =>
                e.TeacherName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                e.CourseName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        return query.ToList();
    }
}

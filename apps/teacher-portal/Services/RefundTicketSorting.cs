using StepGo.TeacherPortal.Models;
using StepGo.UI.Status;

namespace StepGo.TeacherPortal.Services;

/// <summary>Pure sorting logic, split out from <see cref="TeacherDataStore"/> so it's directly
/// unit-testable (frontend-teacher-portal spec: "SLA 倒數最急迫的工單排最前").</summary>
public static class RefundTicketSorting
{
    public static IReadOnlyList<RefundTicket> ByUrgency(IEnumerable<RefundTicket> tickets, DateTimeOffset now) =>
        tickets
            .Where(t => t.Status == RefundTicketStatus.PendingTeacherReview)
            .OrderBy(t => t.TimeRemaining(now))
            .ToList();
}

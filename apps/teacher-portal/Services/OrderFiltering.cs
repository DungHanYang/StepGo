using StepGo.TeacherPortal.Models;

namespace StepGo.TeacherPortal.Services;

/// <summary>frontend-teacher-portal spec: "可依日期區間篩選的訂單明細清單".</summary>
public static class OrderFiltering
{
    public static IReadOnlyList<Order> ByDateRange(IEnumerable<Order> orders, DateOnly? start, DateOnly? end) =>
        orders
            .Where(o => !start.HasValue || DateOnly.FromDateTime(o.OrderedAt.UtcDateTime) >= start.Value)
            .Where(o => !end.HasValue || DateOnly.FromDateTime(o.OrderedAt.UtcDateTime) <= end.Value)
            .ToList();
}

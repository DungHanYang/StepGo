namespace StepGo.AdminPanel.Models;

public sealed record RevenueStats(decimal GrossRevenue, decimal PlatformFeeCollected, int OrderCount);

public sealed record ReconciliationStatus(bool IsBalanced, decimal Discrepancy, DateTimeOffset LastCheckedAt);

public sealed record PayoutQueueBatch(DateOnly BatchDate, int TeacherCount, decimal TotalAmount);

public sealed record RefundOverview(int PendingCount, int CompletedThisMonth, decimal TotalRefundedThisMonth);

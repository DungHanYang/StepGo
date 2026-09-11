namespace StepGo.AdminPanel.Models;

public enum AuditLogCategory
{
    FeeRate,
    Verification,
    Arbitration,
    Permission,
    Payout,
}

public sealed record AuditLogEntry(DateTimeOffset OccurredAt, string Actor, AuditLogCategory Category, string Summary);

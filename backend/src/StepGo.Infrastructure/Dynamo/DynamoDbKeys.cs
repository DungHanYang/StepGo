namespace StepGo.Infrastructure.Dynamo;

/// <summary>
/// PK/SK assembly for the single-table design (design.md decision 4). Every key is a pure string
/// function so repositories never hand-roll key formatting and risk a typo splitting an entity across
/// two different partitions.
/// </summary>
public static class DynamoDbKeys
{
    public const string MetadataSk = "METADATA";
    public const string RefundTicketSk = "REFUNDTICKET";
    public const string PlatformAggregatePk = "AGGREGATE#PLATFORM";
    public const string PlatformSummarySk = "SUMMARY";

    public static string UserPk(Guid userId) => $"USER#{userId}";
    public static string TeacherPk(Guid teacherId) => $"TEACHER#{teacherId}";
    public static string CoursePk(Guid courseId) => $"COURSE#{courseId}";
    public static string OrderPk(Guid orderId) => $"ORDER#{orderId}";
    public static string PayoutBatchPk(Guid batchId) => $"PAYOUTBATCH#{batchId}";
    public static string StudentPk(Guid studentId) => $"STUDENT#{studentId}";

    public static string PayoutBatchOrderSk(Guid orderId) => $"ORDER#{orderId}";
    public static string TeacherSummarySk(string yyyyMm) => $"SUMMARY#{yyyyMm}";
    public static string AuditLogPk(string category) => $"AUDITLOG#{category}";
    public static string AuditLogSk(DateTimeOffset occurredAt, Guid logId) => $"{occurredAt:yyyyMMddHHmmssfff}#{logId}";

    // GSI1 (by teacher: courses / orders / payout batches sharing one partition, SK prefix disambiguates entity type)
    public static string Gsi1Pk(Guid teacherId) => $"TEACHER#{teacherId}";
    public static string Gsi1SkCourse(Guid courseId) => $"COURSE#{courseId}";
    public static string Gsi1SkOrder(DateTimeOffset createdAt, Guid orderId) => $"ORDER#{createdAt:yyyyMMddHHmmssfff}#{orderId}";
    public static string Gsi1SkPayoutBatch(DateTimeOffset createdAt) => $"PAYOUTBATCH#{createdAt:yyyyMMddHHmmssfff}";

    // GSI2 (refund tickets by status, SLA-ordered)
    public static string Gsi2Pk(string status) => $"REFUNDTICKET#STATUS#{status}";
    public static string SlaSortKey(DateTimeOffset? slaDeadline, Guid ticketId) => $"{slaDeadline:yyyyMMddHHmmssfff}#{ticketId}";

    // GSI3 (refund tickets pending for a teacher, SLA-ordered) — reuses Gsi1Pk's teacher partition value, different index.
    public static string Gsi3Pk(Guid teacherId) => $"TEACHER#{teacherId}";

    // GSI4 (orders by student)
    public static string Gsi4Pk(Guid studentId) => $"STUDENT#{studentId}";
    public static string Gsi4SkOrder(DateTimeOffset createdAt) => $"ORDER#{createdAt:yyyyMMddHHmmssfff}";
}

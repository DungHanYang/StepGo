using StepGo.AdminPanel.Models;
using StepGo.UI.Status;

namespace StepGo.AdminPanel.Services;

/// <summary>Mock-first in-memory "backend" for the admin panel (design.md decision 8),
/// replaced by real StepGo.ApiClient calls in task 8.1/8.2.</summary>
public sealed class AdminDataStore
{
    private readonly List<FeeChangeLogEntry> _feeChangeLog = [];
    private readonly List<AuditLogEntry> _auditLog;
    private readonly List<ArbitrationCase> _arbitrationCases;

    public AdminDataStore(TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        RevenueStats = new RevenueStats(GrossRevenue: 1_245_000m, PlatformFeeCollected: 124_500m, OrderCount: 386);
        ReconciliationStatus = new ReconciliationStatus(IsBalanced: true, Discrepancy: 0m, LastCheckedAt: now.AddHours(-3));
        PayoutQueue =
        [
            new PayoutQueueBatch(DateOnly.FromDateTime(now.AddDays(5).Date), TeacherCount: 42, TotalAmount: 386_200m),
        ];
        RefundOverview = new RefundOverview(PendingCount: 3, CompletedThisMonth: 18, TotalRefundedThisMonth: 52_400m);

        FeeSettings = new AdminFeeSettings();

        Directory =
        [
            new DirectoryEntry("陳老師", VerificationStatus.Verified, "兒童繪畫班", "招生中"),
            new DirectoryEntry("林老師", VerificationStatus.UnderReview, "親子瑜伽", "審核中"),
            new DirectoryEntry("張老師", VerificationStatus.Verified, "程式設計入門", "招生中"),
            new DirectoryEntry("王老師", VerificationStatus.Rejected, "親子烘焙", "已下架"),
        ];

        Members = [new AdminMember("平台管理員", "admin@stepgo.tw", AdminRole.Admin)];

        _auditLog =
        [
            new AuditLogEntry(now.AddDays(-1), "平台管理員", AuditLogCategory.Verification, "審核通過陳老師的身分驗證"),
            new AuditLogEntry(now.AddDays(-2), "平台管理員", AuditLogCategory.Payout, "確認 5 月撥款批次"),
            new AuditLogEntry(now.AddDays(-3), "平台管理員", AuditLogCategory.Arbitration, "裁決案件 arb-1：核准部分退款"),
            new AuditLogEntry(now.AddDays(-10), "平台管理員", AuditLogCategory.FeeRate, "調整平台服務費率"),
        ];

        _arbitrationCases =
        [
            new ArbitrationCase
            {
                Id = "arb-2",
                StudentName = "李小華",
                TeacherName = "林老師",
                CourseName = "親子瑜伽",
                DisputedAmount = 2400m,
                Statements =
                [
                    new ArbitrationStatement("學生", "課程時間與原公告不符，要求全額退費。"),
                    new ArbitrationStatement("老師", "已於報名頁面公告調整，學生報名時應已知悉。"),
                ],
            },
        ];
    }

    public RevenueStats RevenueStats { get; }

    public ReconciliationStatus ReconciliationStatus { get; }

    public IReadOnlyList<PayoutQueueBatch> PayoutQueue { get; }

    public RefundOverview RefundOverview { get; }

    public AdminFeeSettings FeeSettings { get; }

    public IReadOnlyList<FeeChangeLogEntry> FeeChangeLog => _feeChangeLog;

    public IReadOnlyList<DirectoryEntry> Directory { get; }

    public IReadOnlyList<AdminMember> Members { get; }

    public IReadOnlyList<AuditLogEntry> AuditLog => _auditLog;

    public IReadOnlyList<ArbitrationCase> ArbitrationCases => _arbitrationCases;

    public void RecordFeeChange(string summary, DateOnly effectiveDate, string changedBy, DateTimeOffset changedAt)
    {
        _feeChangeLog.Add(new FeeChangeLogEntry(summary, effectiveDate, changedBy, changedAt));
    }

    public ArbitrationCase? FindArbitrationCase(string id) => _arbitrationCases.FirstOrDefault(c => c.Id == id);

    public void RuleOnCase(string caseId, ArbitrationRuling ruling, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("A ruling reason is required.");
        }

        var arbitrationCase = FindArbitrationCase(caseId) ?? throw new InvalidOperationException($"Arbitration case {caseId} not found.");
        arbitrationCase.Ruling = ruling;
        arbitrationCase.RulingReason = reason;
        arbitrationCase.IsResolved = true;
    }
}

using StepGo.PricingRules;
using StepGo.TeacherPortal.Models;
using StepGo.UI.Status;

namespace StepGo.TeacherPortal.Services;

/// <summary>
/// Mock-first in-memory "backend" for the teacher portal (design.md decision 8), scoped per
/// app session. Replaced by real StepGo.ApiClient calls against the Mock API / backend in
/// task 8.1/8.2 — kept as a plain class (not behind an interface) since nothing here needs
/// substituting in tests; tests just construct a store with known seed state.
/// </summary>
public sealed class TeacherDataStore
{
    private readonly List<RefundTicket> _refundTickets;
    private readonly List<Order> _orders;
    private readonly List<string> _myCourseNames;

    public TeacherDataStore(TimeProvider timeProvider, IReadOnlyList<string>? myCourseNamesOverride = null)
    {
        var now = timeProvider.GetUtcNow();

        _orders =
        [
            new Order("o-1001", "王小明", "c-paint-kids", "兒童繪畫班", 3200m, PaymentMethod.CreditCard, PaymentStatus.Paid, now.AddDays(-10)),
            new Order("o-1002", "陳小美", "c-paint-kids", "兒童繪畫班", 3200m, PaymentMethod.Atm, PaymentStatus.Paid, now.AddDays(-8)),
            new Order("o-1003", "李小華", "c-yoga-parent", "親子瑜伽", 2400m, PaymentMethod.Atm, PaymentStatus.Pending, now.AddDays(-1)),
            new Order("o-1004", "張小芳", "c-paint-kids", "兒童繪畫班", 3200m, PaymentMethod.CreditCard, PaymentStatus.Refunded, now.AddDays(-20)),
        ];

        _refundTickets =
        [
            new RefundTicket
            {
                Id = "rt-1",
                OrderId = "o-1004",
                StudentName = "張小芳",
                CourseName = "兒童繪畫班",
                OriginalPaymentAmount = 3200m,
                SystemCalculatedRefundAmount = 3200m,
                SlaDeadline = now.AddHours(28),
                Reason = "臨時有事無法上課",
            },
            new RefundTicket
            {
                Id = "rt-2",
                OrderId = "o-1002",
                StudentName = "陳小美",
                CourseName = "兒童繪畫班",
                OriginalPaymentAmount = 3200m,
                SystemCalculatedRefundAmount = 1600m,
                SlaDeadline = now.AddDays(3),
                Reason = "課程時間衝突",
            },
        ];

        _myCourseNames = myCourseNamesOverride?.ToList() ?? ["兒童繪畫班", "親子瑜伽"];

        NextPayout = new PayoutBatch(DateOnly.FromDateTime(now.AddDays(5).Date), 3, 8_775m);
    }

    public IReadOnlyList<Order> Orders => _orders;

    public IReadOnlyList<RefundTicket> RefundTickets => _refundTickets;

    public IReadOnlyList<string> MyCourseNames => _myCourseNames;

    public PayoutBatch NextPayout { get; }

    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.UnderReview;

    public PayoutAccountStatus PayoutAccountStatus { get; set; } = PayoutAccountStatus.Confirmed;

    public string? PayoutAccountBankRejectionReason { get; set; } = "戶名與身分驗證姓名不一致";

    public void SubmitCourse(CourseDraft draft)
    {
        // Newly submitted courses go to "審核中" — deliberately NOT added to MyCourseNames,
        // since they must not appear in the public course list until approved (spec 5.5).
    }

    public RefundTicket? FindRefundTicket(string id) => _refundTickets.FirstOrDefault(t => t.Id == id);

    public void ApproveRefund(string ticketId, decimal approvedAmount, string? adjustmentReason)
    {
        var ticket = FindRefundTicket(ticketId) ?? throw new InvalidOperationException($"Refund ticket {ticketId} not found.");

        if (approvedAmount > ticket.OriginalPaymentAmount)
        {
            throw new InvalidOperationException("Approved amount cannot exceed the original payment amount.");
        }

        var isAdjusted = approvedAmount != ticket.SystemCalculatedRefundAmount;
        if (isAdjusted && string.IsNullOrWhiteSpace(adjustmentReason))
        {
            throw new InvalidOperationException("A reason is required when adjusting the system-calculated amount.");
        }

        ticket.TeacherAdjustedAmount = isAdjusted ? approvedAmount : null;
        ticket.TeacherDecisionReason = adjustmentReason;
        ticket.Status = RefundTicketStatus.TeacherApproved;
    }

    public void RejectRefund(string ticketId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("A reason is required to reject a refund ticket.");
        }

        var ticket = FindRefundTicket(ticketId) ?? throw new InvalidOperationException($"Refund ticket {ticketId} not found.");
        ticket.TeacherDecisionReason = reason;
        ticket.Status = RefundTicketStatus.TeacherRejected;
    }
}

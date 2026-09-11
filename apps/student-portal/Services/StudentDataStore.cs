using StepGo.PricingRules;
using StepGo.StudentPortal.Models;
using StepGo.UI.Status;

namespace StepGo.StudentPortal.Services;

/// <summary>Mock-first in-memory "backend" for the student portal (design.md decision 8),
/// replaced by real StepGo.ApiClient calls in task 8.1/8.2.</summary>
public sealed class StudentDataStore
{
    private readonly List<StudentOrder> _orders;
    private readonly Dictionary<string, List<ThreadMessage>> _threads = [];

    public StudentDataStore(TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        _orders =
        [
            new StudentOrder(
                "o-2001", "c-paint-kids", "兒童繪畫班", "陳老師", 3200m, PaymentMethod.CreditCard, PaymentStatus.Paid,
                NextSessionAt: now.AddDays(3), CourseEnded: false, RefundTicketStatus: null, OrderedAt: now.AddDays(-5)),
            new StudentOrder(
                "o-2002", "c-yoga-parent", "親子瑜伽", "林老師", 2400m, PaymentMethod.Atm, PaymentStatus.Pending,
                NextSessionAt: now.AddDays(10), CourseEnded: false, RefundTicketStatus: null, OrderedAt: now.AddHours(-2),
                AtmVirtualAccount: "9527-1234-5678", AtmPaymentDeadline: now.AddDays(3)),
            new StudentOrder(
                "o-2003", "c-code-intro", "程式設計入門", "張老師", 4500m, PaymentMethod.CreditCard, PaymentStatus.Paid,
                NextSessionAt: now.AddDays(-1), CourseEnded: true, RefundTicketStatus: null, OrderedAt: now.AddDays(-40)),
            new StudentOrder(
                "o-2004", "c-baking", "親子烘焙", "王老師", 1800m, PaymentMethod.CreditCard, PaymentStatus.Refunded,
                NextSessionAt: null, CourseEnded: true, RefundTicketStatus: StepGo.UI.Status.RefundTicketStatus.Completed, OrderedAt: now.AddDays(-60)),
        ];
    }

    public IReadOnlyList<StudentOrder> Orders => _orders;

    public StudentOrder? FindOrder(string id) => _orders.FirstOrDefault(o => o.Id == id);

    public static IReadOnlyList<CheckoutCourse> Courses { get; } =
    [
        new("c-paint-kids", "兒童繪畫班", "陳老師", 3200m, AllowCreditCard: true, AllowAtm: true),
        new("c-yoga-parent", "親子瑜伽", "林老師", 2400m, AllowCreditCard: false, AllowAtm: true),
        new("c-code-intro", "程式設計入門", "張老師", 4500m, AllowCreditCard: true, AllowAtm: false),
    ];

    public static CheckoutCourse? FindCourse(string id) => Courses.FirstOrDefault(c => c.Id == id);

    public string SubmitRefundRequest(string orderId, string reason)
    {
        var order = FindOrder(orderId) ?? throw new InvalidOperationException($"Order {orderId} not found.");

        var index = _orders.IndexOf(order);
        _orders[index] = order with { RefundTicketStatus = StepGo.UI.Status.RefundTicketStatus.PendingTeacherReview };

        _threads[orderId] =
        [
            new ThreadMessage("學生", reason, DateTime.UtcNow),
        ];

        return orderId;
    }

    public IReadOnlyList<ThreadMessage> GetMessages(string orderId) =>
        _threads.TryGetValue(orderId, out var messages) ? messages : [];

    public bool HasThread(string orderId) => _threads.ContainsKey(orderId);

    public void PostMessage(string orderId, string sender, string text)
    {
        if (!_threads.TryGetValue(orderId, out var messages))
        {
            messages = [];
            _threads[orderId] = messages;
        }

        messages.Add(new ThreadMessage(sender, text, DateTime.UtcNow));
    }
}

using StepGo.Shared.Domain;

namespace StepGo.Courses.Domain;

public sealed class Course : AggregateRoot<Guid>
{
    public Guid TeacherId { get; private set; }
    public string Title { get; private set; }
    public Money Price { get; private set; }
    public PaymentMethod AcceptedPaymentMethods { get; private set; }
    public RefundRuleSet RefundRules { get; private set; }
    public CourseStatus Status { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }

    private Course(
        Guid id, Guid teacherId, string title, Money price, PaymentMethod acceptedPaymentMethods,
        RefundRuleSet refundRules, CourseStatus status, DateTimeOffset startsAt)
        : base(id)
    {
        TeacherId = teacherId;
        Title = title;
        Price = price;
        AcceptedPaymentMethods = acceptedPaymentMethods;
        RefundRules = refundRules;
        Status = status;
        StartsAt = startsAt;
    }

    /// <summary>At least one payment method must be selected; refund rules must clear the platform floor.</summary>
    public static Course Create(
        Guid id, Guid teacherId, string title, Money price, PaymentMethod acceptedPaymentMethods,
        RefundRuleSet refundRules, RefundRuleSet platformFloor, DateTimeOffset startsAt)
    {
        if (acceptedPaymentMethods == PaymentMethod.None)
        {
            throw new DomainException("payment_method_required", "課程必須至少選擇一種付款方式。");
        }

        refundRules.ValidateAgainstFloor(platformFloor);

        return new Course(id, teacherId, title, price, acceptedPaymentMethods, refundRules, CourseStatus.Draft, startsAt);
    }

    public static Course Rehydrate(
        Guid id, Guid teacherId, string title, Money price, PaymentMethod acceptedPaymentMethods,
        RefundRuleSet refundRules, CourseStatus status, DateTimeOffset startsAt)
        => new(id, teacherId, title, price, acceptedPaymentMethods, refundRules, status, startsAt);

    /// <summary>Only a verified teacher's course may leave draft status. Caller supplies the guard result.</summary>
    public void Publish(bool teacherIsVerified)
    {
        if (!teacherIsVerified)
        {
            throw new DomainException("teacher_not_verified", "老師身分驗證狀態非「已認證」，課程無法發佈。");
        }

        Status = CourseStatus.Published;
    }
}

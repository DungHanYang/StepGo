using StepGo.Domain.Courses;
using StepGo.Domain.SharedKernel;

namespace StepGo.Domain.Orders;

public sealed class Order : AggregateRoot<Guid>
{
    public Guid CourseId { get; private set; }
    public Guid TeacherId { get; private set; }
    public Guid StudentId { get; private set; }
    public Money CoursePriceAtOrder { get; private set; }
    public PaymentMethod RequestedPaymentMethod { get; private set; }
    public ChoosePayment GatewayChoosePayment { get; private set; }
    public PaymentMethod? PaymentMethodUsed { get; private set; }
    public OrderPaymentStatus PaymentStatus { get; private set; }
    public OrderPayoutStatus PayoutStatus { get; private set; }
    public Guid? PayoutBatchId { get; private set; }
    public DateTimeOffset? AtmPaymentDueAt { get; private set; }
    public string? FeeScheduleVersionId { get; private set; }
    public Money? GatewayFee { get; private set; }
    public Money? PlatformServiceFee { get; private set; }
    public Money? NetReceivable { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Order(
        Guid id, Guid courseId, Guid teacherId, Guid studentId, Money coursePriceAtOrder,
        PaymentMethod requestedPaymentMethod, ChoosePayment gatewayChoosePayment, DateTimeOffset? atmPaymentDueAt,
        DateTimeOffset createdAt)
        : base(id)
    {
        CourseId = courseId;
        TeacherId = teacherId;
        StudentId = studentId;
        CoursePriceAtOrder = coursePriceAtOrder;
        RequestedPaymentMethod = requestedPaymentMethod;
        GatewayChoosePayment = gatewayChoosePayment;
        AtmPaymentDueAt = atmPaymentDueAt;
        PaymentStatus = OrderPaymentStatus.PendingPayment;
        PayoutStatus = OrderPayoutStatus.NotEligible;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// The requested payment method must be one the course accepts. The gateway `ChoosePayment` parameter is
    /// derived from the course's full accepted-method set (not just the student's single request) per
    /// backend-order-checkout spec: a course open to both methods always dispatches ChoosePayment=ALL.
    /// </summary>
    public static Order PlaceForCourse(
        Guid id, Course course, Guid studentId, PaymentMethod requestedPaymentMethod, DateTimeOffset now, int atmDueInDays = 3)
    {
        if ((course.AcceptedPaymentMethods & requestedPaymentMethod) != requestedPaymentMethod || requestedPaymentMethod == PaymentMethod.None)
        {
            throw new DomainException("payment_method_not_supported", "所選付款方式不是該課程開放的付款方式。");
        }

        var choosePayment = MapChoosePayment(course.AcceptedPaymentMethods);
        DateTimeOffset? atmDueAt = requestedPaymentMethod == PaymentMethod.Atm ? now.AddDays(atmDueInDays) : null;

        return new Order(id, course.Id, course.TeacherId, studentId, course.Price, requestedPaymentMethod, choosePayment, atmDueAt, now);
    }

    private static ChoosePayment MapChoosePayment(PaymentMethod accepted) => accepted switch
    {
        PaymentMethod.Atm => ChoosePayment.Atm,
        PaymentMethod.CreditCard => ChoosePayment.Credit,
        PaymentMethod.All => ChoosePayment.All,
        _ => throw new DomainException("payment_method_required", "課程必須至少開放一種付款方式。"),
    };

    public static Order Rehydrate(
        Guid id, Guid courseId, Guid teacherId, Guid studentId, Money coursePriceAtOrder,
        PaymentMethod requestedPaymentMethod, ChoosePayment gatewayChoosePayment, PaymentMethod? paymentMethodUsed,
        OrderPaymentStatus paymentStatus, OrderPayoutStatus payoutStatus, Guid? payoutBatchId,
        DateTimeOffset? atmPaymentDueAt, string? feeScheduleVersionId, Money? gatewayFee, Money? platformServiceFee,
        Money? netReceivable, DateTimeOffset createdAt)
    {
        var order = new Order(id, courseId, teacherId, studentId, coursePriceAtOrder, requestedPaymentMethod, gatewayChoosePayment, atmPaymentDueAt, createdAt)
        {
            PaymentMethodUsed = paymentMethodUsed,
            PaymentStatus = paymentStatus,
            PayoutStatus = payoutStatus,
            PayoutBatchId = payoutBatchId,
            FeeScheduleVersionId = feeScheduleVersionId,
            GatewayFee = gatewayFee,
            PlatformServiceFee = platformServiceFee,
            NetReceivable = netReceivable,
        };
        return order;
    }

    /// <summary>
    /// Called only from the webhook-consumer path after notification idempotency has been established there.
    /// The fee breakdown is computed by StepGo.Domain.FeeLedger.FeeCalculationResult against the rate
    /// schedule version in effect right now and persisted on the order permanently (fee-schedule version binding).
    /// </summary>
    public void ConfirmPayment(PaymentMethod methodUsed, StepGo.Domain.FeeLedger.FeeCalculationResult fees, DateTimeOffset now)
    {
        if (PaymentStatus == OrderPaymentStatus.Paid)
        {
            return;
        }

        PaymentMethodUsed = methodUsed;
        PaymentStatus = OrderPaymentStatus.Paid;
        PayoutStatus = OrderPayoutStatus.PendingPayout;
        FeeScheduleVersionId = fees.FeeScheduleVersionId;
        GatewayFee = fees.GatewayFee;
        PlatformServiceFee = fees.PlatformServiceFee;
        NetReceivable = fees.NetReceivable;

        Raise(new OrderPaymentConfirmedEvent(Id, StudentId, TeacherId, CoursePriceAtOrder, now));
    }

    public void MarkOverdueIfAtmUnpaid(DateTimeOffset now)
    {
        if (PaymentStatus != OrderPaymentStatus.PendingPayment || AtmPaymentDueAt is null || now < AtmPaymentDueAt)
        {
            return;
        }

        PaymentStatus = OrderPaymentStatus.Overdue;
        Raise(new OrderPaymentOverdueEvent(Id, StudentId, now));
    }

    /// <summary>Credit card refunds go through the gateway's automatic refund API; ATM refunds require manual transfer.</summary>
    public RefundRoute DetermineRefundRoute()
    {
        if (PaymentMethodUsed is null)
        {
            throw new DomainException("order_not_paid", "訂單尚未付款完成，無法判斷退款方式。");
        }

        return PaymentMethodUsed == PaymentMethod.CreditCard
            ? RefundRoute.AutomaticGatewayApi
            : RefundRoute.ManualBankTransferPending;
    }

    public void MarkRefunded()
    {
        PaymentStatus = OrderPaymentStatus.Refunded;
    }

    public void AssignToPayoutBatch(Guid payoutBatchId)
    {
        PayoutStatus = OrderPayoutStatus.Batched;
        PayoutBatchId = payoutBatchId;
    }

    public void MarkPaidOut()
    {
        PayoutStatus = OrderPayoutStatus.PaidOut;
    }

    /// <summary>Bank rejected the batch transfer: this order's payout reverts to pending so the next batch retries it.</summary>
    public void RevertPayoutToPending()
    {
        PayoutStatus = OrderPayoutStatus.PendingPayout;
        PayoutBatchId = null;
    }

    /// <summary>An approved refund on an order that hasn't been paid out yet excludes it from future payout batches.</summary>
    public void ExcludeFromPayout()
    {
        PayoutStatus = OrderPayoutStatus.NotEligible;
    }
}

namespace StepGo.Orders.Domain;

public enum OrderPaymentStatus
{
    PendingPayment,
    Paid,
    Overdue,
    Refunded,
}

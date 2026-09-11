namespace StepGo.Domain.RefundTickets;

public enum DeductionSource
{
    /// <summary>Order hasn't been paid out yet: refund comes straight from the platform account.</summary>
    PlatformAccountDirect,
    /// <summary>Order was already paid out: deduct from the teacher's next payout batch.</summary>
    NextTeacherPayout,
}

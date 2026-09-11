namespace StepGo.RefundTickets.Application;

/// <summary>Starts the Step Functions execution that waits out the teacher's 5-business-day SLA window (task 7.3).</summary>
public interface IRefundSlaScheduler
{
    Task StartTrackingAsync(Guid ticketId, CancellationToken ct);
}

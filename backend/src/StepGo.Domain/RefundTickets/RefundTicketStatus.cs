namespace StepGo.Domain.RefundTickets;

public enum RefundTicketStatus
{
    /// <summary>Beyond every refund tier (0% eligible); rejected automatically but still appealable.</summary>
    AutoRejected,
    TeacherReviewing,
    /// <summary>Teacher rejected; still appealable to arbitration.</summary>
    TeacherRejected,
    AdminArbitration,
    ApprovedTerminal,
    RejectedTerminal,
}

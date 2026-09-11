namespace StepGo.RefundTickets.Domain;

/// <summary>Abstracts "N business days from now" so Domain stays free of a concrete holiday-calendar dependency.</summary>
public interface IBusinessDayCalendar
{
    DateTimeOffset AddBusinessDays(DateTimeOffset from, int businessDays);
}

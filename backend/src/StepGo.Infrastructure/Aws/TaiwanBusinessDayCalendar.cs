using StepGo.Domain.RefundTickets;

namespace StepGo.Infrastructure.Aws;

/// <summary>Skips weekends only; national holidays are not modeled in the MVP scaffold.</summary>
public sealed class TaiwanBusinessDayCalendar : IBusinessDayCalendar
{
    public DateTimeOffset AddBusinessDays(DateTimeOffset from, int businessDays)
    {
        var date = from;
        var remaining = businessDays;
        while (remaining > 0)
        {
            date = date.AddDays(1);
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                remaining--;
            }
        }
        return date;
    }
}

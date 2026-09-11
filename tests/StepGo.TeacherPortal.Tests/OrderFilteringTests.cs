using StepGo.PricingRules;
using StepGo.TeacherPortal.Models;
using StepGo.TeacherPortal.Services;
using StepGo.UI.Status;
using Xunit;

namespace StepGo.TeacherPortal.Tests;

public class OrderFilteringTests
{
    private static Order MakeOrder(string id, DateTimeOffset orderedAt) =>
        new(id, "學生", "c-1", "課程", 1000m, PaymentMethod.CreditCard, PaymentStatus.Paid, orderedAt);

    [Fact]
    public void Date_Range_Filters_Out_Orders_Outside_The_Window()
    {
        var jan1 = MakeOrder("o-1", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var jan15 = MakeOrder("o-2", new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        var feb1 = MakeOrder("o-3", new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));

        var result = OrderFiltering.ByDateRange(
            [jan1, jan15, feb1],
            new DateOnly(2026, 1, 10),
            new DateOnly(2026, 1, 31));

        Assert.Single(result);
        Assert.Equal("o-2", result[0].Id);
    }

    [Fact]
    public void No_Range_Returns_All_Orders()
    {
        var orders = new[]
        {
            MakeOrder("o-1", DateTimeOffset.UtcNow),
            MakeOrder("o-2", DateTimeOffset.UtcNow.AddDays(-30)),
        };

        var result = OrderFiltering.ByDateRange(orders, null, null);

        Assert.Equal(2, result.Count);
    }
}

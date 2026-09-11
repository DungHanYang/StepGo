using StepGo.PricingRules;
using StepGo.TeacherPortal.Models;
using StepGo.TeacherPortal.Services;
using StepGo.UI.Status;
using Xunit;

namespace StepGo.TeacherPortal.Tests;

public class StudentRosterCsvExporterTests
{
    [Fact]
    public void Csv_Content_Matches_The_Order_List()
    {
        var orders = new[]
        {
            new Order("o-1", "王小明", "c-1", "兒童繪畫班", 3200m, PaymentMethod.CreditCard, PaymentStatus.Paid, new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero)),
            new Order("o-2", "陳小美", "c-2", "親子瑜伽", 2400m, PaymentMethod.Atm, PaymentStatus.Pending, new DateTimeOffset(2026, 1, 6, 0, 0, 0, TimeSpan.Zero)),
        };

        var csv = StudentRosterCsvExporter.ToCsv(orders);
        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(StudentRosterCsvExporter.Header, lines[0]);
        Assert.Equal("王小明,兒童繪畫班,3200,Paid,2026-01-05", lines[1]);
        Assert.Equal("陳小美,親子瑜伽,2400,Pending,2026-01-06", lines[2]);
        Assert.Equal(3, lines.Length);
    }
}

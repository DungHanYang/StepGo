using Bunit;
using Microsoft.Extensions.DependencyInjection;
using StepGo.TeacherPortal.Pages;
using StepGo.TeacherPortal.Services;
using Xunit;

namespace StepGo.TeacherPortal.Tests;

public class StudentRosterPageTests : TestContext
{
    [Fact]
    public void Export_Button_Shows_No_Paid_Upsell_Prompt()
    {
        Services.AddSingleton(new TeacherDataStore(TimeProvider.System));
        JSInterop.SetupVoid("stepGoDownloadFile", _ => true).SetVoidResult();

        var cut = RenderComponent<StudentRoster>();
        cut.FindAll("button").First(b => b.TextContent == "匯出 CSV").Click();

        Assert.DoesNotContain("付費", cut.Markup);
        Assert.DoesNotContain("升級方案", cut.Markup);
        Assert.DoesNotContain("Pro", cut.Markup);
    }

    [Fact]
    public void Roster_Table_Shows_All_Orders_By_Default()
    {
        var store = new TeacherDataStore(TimeProvider.System);
        Services.AddSingleton(store);

        var cut = RenderComponent<StudentRoster>();

        var rows = cut.FindAll("[data-testid='student-roster-table'] tbody tr");
        Assert.Equal(store.Orders.Count, rows.Count);
    }
}

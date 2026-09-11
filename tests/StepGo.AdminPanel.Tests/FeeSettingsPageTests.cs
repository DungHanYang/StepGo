using Bunit;
using Microsoft.Extensions.DependencyInjection;
using StepGo.AdminPanel.Pages;
using StepGo.AdminPanel.Services;
using Xunit;

namespace StepGo.AdminPanel.Tests;

public class FeeSettingsPageTests : TestContext
{
    [Fact]
    public void EffectiveDate_Under_30_Days_Blocks_Submission()
    {
        Services.AddSingleton(new AdminDataStore(TimeProvider.System));
        Services.AddSingleton(TimeProvider.System);

        var cut = RenderComponent<FeeSettings>();
        var dateInput = cut.Find("input[type=date]");
        dateInput.Change(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)).ToString("yyyy-MM-dd"));
        cut.Find("button").Click();

        Assert.Contains("生效日不得早於送出當下起算 30 天", cut.Markup);
    }

    [Fact]
    public void Successful_Change_Is_Written_To_The_Change_Log()
    {
        var store = new AdminDataStore(TimeProvider.System);
        Services.AddSingleton(store);
        Services.AddSingleton(TimeProvider.System);

        var cut = RenderComponent<FeeSettings>();
        var rateInput = cut.Find("input[type=number]");
        rateInput.Change("12.0");
        var dateInput = cut.Find("input[type=date]");
        dateInput.Change(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(35)).ToString("yyyy-MM-dd"));
        cut.Find("button").Click();

        Assert.Single(store.FeeChangeLog);
        Assert.Contains("12", store.FeeChangeLog[0].Summary);
        var logSection = cut.Find("[data-testid='fee-change-log']");
        Assert.Contains("平台管理員", logSection.TextContent);
    }
}

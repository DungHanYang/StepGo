using Bunit;
using Microsoft.Extensions.DependencyInjection;
using StepGo.AdminPanel.Pages;
using StepGo.AdminPanel.Services;
using Xunit;

namespace StepGo.AdminPanel.Tests;

public class ArbitrationPageTests : TestContext
{
    [Fact]
    public void Submitting_Ruling_Without_Reason_Is_Blocked()
    {
        var store = new AdminDataStore(TimeProvider.System);
        Services.AddSingleton(store);
        var caseId = store.ArbitrationCases[0].Id;

        var cut = RenderComponent<Arbitration>();
        cut.Find($"[data-testid='case-{caseId}'] button").Click();

        Assert.Contains("裁決理由為必填", cut.Markup);
        Assert.False(store.ArbitrationCases[0].IsResolved);
    }

    [Fact]
    public void Ruling_With_Reason_Removes_Case_From_The_Pending_Queue()
    {
        var store = new AdminDataStore(TimeProvider.System);
        Services.AddSingleton(store);
        var caseId = store.ArbitrationCases[0].Id;

        var cut = RenderComponent<Arbitration>();
        var caseCard = cut.Find($"[data-testid='case-{caseId}']");
        caseCard.QuerySelector("input")!.Change("雙方協議後同意部分退款");
        caseCard.QuerySelector("button")!.Click();

        Assert.True(store.ArbitrationCases[0].IsResolved);
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find($"[data-testid='case-{caseId}']"));
    }
}

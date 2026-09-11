using Bunit;
using Microsoft.Extensions.DependencyInjection;
using StepGo.TeacherPortal.Pages;
using StepGo.TeacherPortal.Services;
using Xunit;

namespace StepGo.TeacherPortal.Tests;

public class CourseWizardRefundRulesTests : TestContext
{
    private IRenderedComponent<CourseWizard> RenderAtRefundRulesStep()
    {
        Services.AddSingleton(new TeacherDataStore(TimeProvider.System));
        Services.AddSingleton(TimeProvider.System);

        var cut = RenderComponent<CourseWizard>();
        Next(cut); // basic info -> pricing
        cut.FindAll("input[type=checkbox]")[0].Change(true); // pick a payment method so we can advance
        Next(cut); // pricing -> refund rules
        return cut;
    }

    private static void Next(IRenderedComponent<CourseWizard> cut) =>
        cut.FindAll("button").First(b => b.TextContent == "下一步").Click();

    [Fact]
    public void Rate_Below_Platform_Floor_Is_Rejected()
    {
        var cut = RenderAtRefundRulesStep();

        // First tier is "開課前 14 日以上" with an 100% floor — 80% is below it.
        var firstRateInput = cut.Find("[data-testid='refund-rules-step'] input");
        firstRateInput.Change("80");

        Assert.Contains("不得低於平台底線", cut.Markup);
    }

    [Fact]
    public void Rate_Above_Platform_Floor_Is_Accepted()
    {
        var cut = RenderAtRefundRulesStep();

        // Second tier is "開課前 7–13 日" with a 50% floor — 70% is above it.
        var secondRateInput = cut.FindAll("[data-testid='refund-rules-step'] input")[1];
        secondRateInput.Change("70");

        Assert.DoesNotContain("不得低於平台底線", cut.Markup);
    }
}

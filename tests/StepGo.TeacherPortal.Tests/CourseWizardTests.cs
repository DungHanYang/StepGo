using Bunit;
using Microsoft.Extensions.DependencyInjection;
using StepGo.TeacherPortal.Pages;
using StepGo.TeacherPortal.Services;
using StepGo.UI.Status;
using Xunit;

namespace StepGo.TeacherPortal.Tests;

public class CourseWizardTests : TestContext
{
    private TeacherDataStore Seed(VerificationStatus status = VerificationStatus.Verified)
    {
        var store = new TeacherDataStore(TimeProvider.System) { VerificationStatus = status };
        Services.AddSingleton(store);
        Services.AddSingleton(TimeProvider.System);
        return store;
    }

    [Fact]
    public void Cannot_Advance_Past_Pricing_Step_Without_A_Payment_Method()
    {
        Seed();
        var cut = RenderComponent<CourseWizard>();

        GoToStep1(cut);

        cut.Find("[data-testid='pricing-step']"); // sanity: we're on the pricing step already
        var nextButton = cut.FindAll("button").First(b => b.TextContent == "下一步");
        nextButton.Click();

        Assert.Contains("需至少選擇一種付款方式", cut.Markup);
        cut.Find("[data-testid='pricing-step']"); // still on the pricing step — didn't advance
    }

    [Fact]
    public void Basic_Info_Entered_In_Step_One_Survives_Navigating_To_Later_Steps_And_Back()
    {
        Seed();
        var cut = RenderComponent<CourseWizard>();

        cut.Find("input").Input("兒童繪畫進階班");
        GoToStep1(cut);
        cut.FindAll("input[type=checkbox]")[0].Change(true); // allow credit card
        GoForward(cut); // -> refund rules
        GoForward(cut); // -> enrollment page
        GoBack(cut);
        GoBack(cut);
        GoBack(cut); // back to basic info

        Assert.Equal("兒童繪畫進階班", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Pricing_Breakdown_Updates_Live_As_Price_And_Payment_Method_Change()
    {
        // frontend-teacher-portal spec: "隨售價或付款方式勾選變動即時更新，不呼叫後端 API" —
        // PricingCalculator has no HttpClient dependency, so this is structurally offline.
        Seed();
        var cut = RenderComponent<CourseWizard>();
        GoToStep1(cut);

        cut.FindAll("input[type=checkbox]")[0].Change(true); // credit card
        Assert.Contains("信用卡：", cut.Markup);
        Assert.DoesNotContain("ATM：", cut.Markup);

        cut.FindAll("input[type=checkbox]")[1].Change(true); // + ATM
        Assert.Contains("ATM：", cut.Markup);

        var priceInput = cut.Find("[data-testid='pricing-step'] input[type=number]");
        priceInput.Input("6000");

        Assert.Contains("NT$600", cut.Markup); // 10% platform fee of 6000
    }

    [Fact]
    public void Non_Verified_Teacher_Is_Redirected_To_Verification_Page()
    {
        Seed(VerificationStatus.UnderReview);
        var nav = Services.GetRequiredService<Bunit.TestDoubles.FakeNavigationManager>();

        RenderComponent<CourseWizard>();

        Assert.EndsWith("/verification", nav.Uri);
    }

    private static void GoToStep1(IRenderedComponent<CourseWizard> cut)
    {
        GoForward(cut);
    }

    private static void GoForward(IRenderedComponent<CourseWizard> cut)
    {
        cut.FindAll("button").First(b => b.TextContent is "下一步").Click();
    }

    private static void GoBack(IRenderedComponent<CourseWizard> cut)
    {
        cut.FindAll("button").First(b => b.TextContent is "上一步").Click();
    }
}

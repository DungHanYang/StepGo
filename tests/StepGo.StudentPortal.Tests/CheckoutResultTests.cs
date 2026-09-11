using Bunit;
using Microsoft.Extensions.DependencyInjection;
using StepGo.StudentPortal.Pages;
using StepGo.StudentPortal.Services;
using Xunit;

namespace StepGo.StudentPortal.Tests;

public class CheckoutResultTests : TestContext
{
    public CheckoutResultTests()
    {
        Services.AddSingleton(new StudentDataStore(TimeProvider.System));
    }

    [Fact]
    public void CreditCard_Flow_Shows_Instant_Success_Result()
    {
        var cut = RenderComponent<Checkout>(parameters => parameters.Add(p => p.CourseId, "c-code-intro"));
        AdvanceToResult(cut);

        cut.Find("[data-testid='result-success']");
        Assert.Contains("報名成功", cut.Markup);
    }

    [Fact]
    public void Atm_Result_Shows_Virtual_Account_And_No_Self_Report_Button()
    {
        var cut = RenderComponent<Checkout>(parameters => parameters.Add(p => p.CourseId, "c-yoga-parent"));
        AdvanceToResult(cut);

        var resultPanel = cut.Find("[data-testid='result-atm-pending']");
        Assert.Contains("虛擬帳號", resultPanel.TextContent);

        // frontend-student-portal spec: "介面 SHALL NOT 提供任何『我已完成轉帳』之類的自我申報操作".
        var buttons = resultPanel.QuerySelectorAll("button");
        Assert.DoesNotContain(buttons, b => b.TextContent.Contains("已完成") || b.TextContent.Contains("已轉帳") || b.TextContent.Contains("已付款"));
    }

    private static void AdvanceToResult(IRenderedComponent<Checkout> cut)
    {
        Next(cut); // confirm -> payment
        Next(cut); // payment -> result
    }

    private static void Next(IRenderedComponent<Checkout> cut) =>
        cut.FindAll("button").First(b => b.TextContent == "下一步").Click();
}

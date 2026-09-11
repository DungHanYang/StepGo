using Bunit;
using Microsoft.Extensions.DependencyInjection;
using StepGo.StudentPortal.Pages;
using StepGo.StudentPortal.Services;
using Xunit;

namespace StepGo.StudentPortal.Tests;

public class CheckoutTests : TestContext
{
    public CheckoutTests()
    {
        Services.AddSingleton(new StudentDataStore(TimeProvider.System));
    }

    [Fact]
    public void Atm_Only_Course_Shows_Only_Atm_Option()
    {
        // c-yoga-parent is seeded with AllowCreditCard=false, AllowAtm=true.
        var cut = RenderComponent<Checkout>(parameters => parameters.Add(p => p.CourseId, "c-yoga-parent"));
        Next(cut); // confirm -> payment step

        Assert.DoesNotContain("信用卡", cut.Markup);
        Assert.Contains("ATM 虛擬帳號", cut.Markup);
    }

    [Fact]
    public void CreditCard_Only_Course_Shows_Only_CreditCard_Option()
    {
        // c-code-intro is seeded with AllowCreditCard=true, AllowAtm=false.
        var cut = RenderComponent<Checkout>(parameters => parameters.Add(p => p.CourseId, "c-code-intro"));
        Next(cut);

        Assert.Contains("信用卡", cut.Markup);
        Assert.DoesNotContain("ATM 虛擬帳號", cut.Markup);
    }

    [Fact]
    public void Both_Methods_Open_Shows_Both_Options()
    {
        // c-paint-kids is seeded with both open.
        var cut = RenderComponent<Checkout>(parameters => parameters.Add(p => p.CourseId, "c-paint-kids"));
        Next(cut);

        Assert.Contains("信用卡", cut.Markup);
        Assert.Contains("ATM 虛擬帳號", cut.Markup);
    }

    private static void Next(IRenderedComponent<Checkout> cut) =>
        cut.FindAll("button").First(b => b.TextContent == "下一步").Click();
}

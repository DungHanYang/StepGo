using Bunit;
using StepGo.Marketing.Client.Pages;
using Xunit;

namespace StepGo.Marketing.Tests;

public class FeeCalculatorTests : TestContext
{
    [Fact]
    public void Changing_Price_Updates_Breakdown_Live()
    {
        var cut = RenderComponent<FeeCalculator>();

        cut.Find("input[type=number]").Input("1000");

        var breakdown = cut.Find("[data-testid='breakdown']");
        Assert.Contains("NT$100", breakdown.TextContent); // 10% platform fee of 1000
    }

    [Fact]
    public void Switching_To_Atm_Updates_Gateway_Fee_To_Flat_Fifteen()
    {
        var cut = RenderComponent<FeeCalculator>();

        var atmRadio = cut.FindAll("input[type=radio][name=method]")[1];
        atmRadio.Change(true);

        var breakdown = cut.Find("[data-testid='breakdown']");
        Assert.Contains("金流手續費：NT$15", breakdown.TextContent);
    }
}

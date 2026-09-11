using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using StepGo.StudentPortal.Pages;
using StepGo.StudentPortal.Services;
using Xunit;

namespace StepGo.StudentPortal.Tests;

public class OrderDetailTests : TestContext
{
    [Fact]
    public void Submitting_Refund_Request_Navigates_To_Message_Thread()
    {
        Services.AddSingleton(new StudentDataStore(TimeProvider.System));
        var nav = Services.GetRequiredService<FakeNavigationManager>();

        var cut = RenderComponent<OrderDetail>(parameters => parameters.Add(p => p.OrderId, "o-2001"));
        cut.FindAll("button").First(b => b.TextContent == "申請退課").Click();
        cut.Find("input").Input("臨時有事");
        cut.FindAll("button").First(b => b.TextContent == "送出退課申請").Click();

        Assert.EndsWith("/orders/o-2001/messages", nav.Uri);
    }

    [Fact]
    public void Nonexistent_Order_Id_Shows_Order_Not_Found_Page()
    {
        Services.AddSingleton(new StudentDataStore(TimeProvider.System));

        var cut = RenderComponent<OrderDetail>(parameters => parameters.Add(p => p.OrderId, "o-does-not-exist"));

        cut.Find("[data-testid='order-not-found']");
        Assert.Contains("找不到訂單", cut.Markup);
        var backLink = cut.Find("a");
        Assert.Equal("/", backLink.GetAttribute("href"));
    }
}

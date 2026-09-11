using Bunit;
using Microsoft.Extensions.DependencyInjection;
using StepGo.StudentPortal.Layout;
using StepGo.StudentPortal.Pages;
using StepGo.StudentPortal.Services;
using Xunit;

namespace StepGo.StudentPortal.Tests;

public class MessageThreadTests : TestContext
{
    [Fact]
    public void Shows_Messages_Posted_After_A_Refund_Request()
    {
        var store = new StudentDataStore(TimeProvider.System);
        store.SubmitRefundRequest("o-2001", "臨時有事無法上課");
        Services.AddSingleton(store);

        var cut = RenderComponent<MessageThread>(parameters => parameters.Add(p => p.OrderId, "o-2001"));

        cut.Find("[data-testid='message-list']");
        Assert.Contains("臨時有事無法上課", cut.Markup);
    }

    [Fact]
    public void General_Question_Entry_Routes_To_Line_Not_A_New_Thread()
    {
        // frontend-student-portal spec: "一般問題頁面導引至 LINE 而非留言串".
        Services.AddSingleton(new StudentDataStore(TimeProvider.System));

        var cut = RenderComponent<MainLayout>();

        var lineLink = cut.FindAll("a").First(a => a.TextContent.Contains("一般問題聯繫"));
        Assert.Contains("line.me", lineLink.GetAttribute("href"));
        Assert.DoesNotContain("/orders/", lineLink.GetAttribute("href"));
    }
}

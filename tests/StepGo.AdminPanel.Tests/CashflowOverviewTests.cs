using Bunit;
using Microsoft.Extensions.DependencyInjection;
using StepGo.AdminPanel.Pages;
using StepGo.AdminPanel.Services;
using Xunit;

namespace StepGo.AdminPanel.Tests;

public class CashflowOverviewTests : TestContext
{
    [Fact]
    public void Shows_Payout_Queue_List()
    {
        var store = new AdminDataStore(TimeProvider.System);
        Services.AddSingleton(store);

        var cut = RenderComponent<CashflowOverview>();

        var queue = cut.Find("[data-testid='payout-queue']");
        Assert.Contains(store.PayoutQueue[0].TeacherCount.ToString(), queue.TextContent);
    }
}

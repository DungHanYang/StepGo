using Bunit;
using Microsoft.Extensions.DependencyInjection;
using StepGo.TeacherPortal.Pages;
using StepGo.TeacherPortal.Services;
using StepGo.UI.Status;
using Xunit;

namespace StepGo.TeacherPortal.Tests;

public class PayoutAccountPageTests : TestContext
{
    [Fact]
    public void BankRejected_Shows_Reason_And_Resubmit_Entry()
    {
        Services.AddSingleton(new TeacherDataStore(TimeProvider.System)
        {
            PayoutAccountStatus = PayoutAccountStatus.BankRejected,
            PayoutAccountBankRejectionReason = "戶名與身分驗證姓名不一致",
        });

        var cut = RenderComponent<PayoutAccount>();

        var reason = cut.Find("[data-testid='bank-rejection-reason']");
        Assert.Contains("戶名與身分驗證姓名不一致", reason.TextContent);
        Assert.Contains("修改後重新送出", cut.Markup);
    }
}

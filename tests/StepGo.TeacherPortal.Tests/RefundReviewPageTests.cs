using Bunit;
using Microsoft.Extensions.DependencyInjection;
using StepGo.TeacherPortal.Models;
using StepGo.TeacherPortal.Pages;
using StepGo.TeacherPortal.Services;
using StepGo.UI.Status;
using Xunit;

namespace StepGo.TeacherPortal.Tests;

public class RefundReviewPageTests : TestContext
{
    private TeacherDataStore Seed()
    {
        var store = new TeacherDataStore(TimeProvider.System);
        Services.AddSingleton(store);
        Services.AddSingleton(TimeProvider.System);
        return store;
    }

    [Fact]
    public void Adjusted_Amount_Above_Original_Payment_Is_Blocked()
    {
        var store = Seed();
        var ticket = store.RefundTickets[0];
        var cut = RenderComponent<RefundReview>();

        var amountInput = cut.Find($"[data-testid='ticket-{ticket.Id}'] input[type=number]");
        amountInput.Change((ticket.OriginalPaymentAmount + 100m).ToString());

        cut.FindAll($"[data-testid='ticket-{ticket.Id}'] button").First(b => b.TextContent == "核准").Click();

        Assert.Contains("核准金額不得超過原始繳費金額", cut.Markup);
        Assert.Equal(RefundTicketStatus.PendingTeacherReview, ticket.Status);
    }

    [Fact]
    public void Adjusted_Amount_Without_Reason_Is_Blocked()
    {
        var store = Seed();
        var ticket = store.RefundTickets[0];
        var cut = RenderComponent<RefundReview>();

        var amountInput = cut.Find($"[data-testid='ticket-{ticket.Id}'] input[type=number]");
        // Change to something different from the system-calculated amount, but leave reason blank.
        amountInput.Change((ticket.SystemCalculatedRefundAmount - 100m).ToString());

        cut.FindAll($"[data-testid='ticket-{ticket.Id}'] button").First(b => b.TextContent == "核准").Click();

        Assert.Contains("理由為必填", cut.Markup);
        Assert.Equal(RefundTicketStatus.PendingTeacherReview, ticket.Status);
    }

    [Fact]
    public void Approve_Without_Adjustment_Does_Not_Require_A_Reason()
    {
        var store = Seed();
        var ticket = store.RefundTickets[0];
        var cut = RenderComponent<RefundReview>();

        // Leave the amount at the system-calculated default (no adjustment) and approve directly.
        cut.FindAll($"[data-testid='ticket-{ticket.Id}'] button").First(b => b.TextContent == "核准").Click();

        Assert.Equal(RefundTicketStatus.TeacherApproved, ticket.Status);
    }
}

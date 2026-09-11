using Bunit;
using Microsoft.Extensions.DependencyInjection;
using StepGo.TeacherPortal.Pages;
using StepGo.TeacherPortal.Services;
using StepGo.UI.Status;
using Xunit;

namespace StepGo.TeacherPortal.Tests;

public class VerificationPageTests : TestContext
{
    [Theory]
    [InlineData(VerificationStatus.Filling, "上傳身分證明文件")]
    [InlineData(VerificationStatus.UnderReview, "審核中，請耐心等候")]
    [InlineData(VerificationStatus.Rejected, "未通過")]
    [InlineData(VerificationStatus.Verified, "已通過，你可以開始建立課程")]
    public void Renders_Correct_Content_For_Each_State(VerificationStatus status, string expectedText)
    {
        Services.AddSingleton(new TeacherDataStore(TimeProvider.System) { VerificationStatus = status });

        var cut = RenderComponent<Verification>();

        Assert.Contains(expectedText, cut.Markup);
    }

    [Fact]
    public void Rejected_Resubmit_Transitions_Status_To_UnderReview()
    {
        Services.AddSingleton(new TeacherDataStore(TimeProvider.System) { VerificationStatus = VerificationStatus.Rejected });

        var cut = RenderComponent<Verification>();
        cut.Find("button").Click(); // "重新上傳並送出"

        Assert.Contains("已重新送出，狀態已更新為審核中", cut.Markup);
    }
}

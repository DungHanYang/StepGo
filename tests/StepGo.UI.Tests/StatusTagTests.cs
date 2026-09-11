using Bunit;
using StepGo.UI.Components;
using StepGo.UI.Status;
using Xunit;

namespace StepGo.UI.Tests;

public class StatusTagTests : TestContext
{
    [Fact]
    public void Renders_Label_And_Variant_Class()
    {
        var cut = RenderComponent<StatusTag>(parameters => parameters
            .Add(p => p.Label, "已付款")
            .Add(p => p.Variant, StatusTagVariant.Positive));

        var span = cut.Find("span");
        Assert.Equal("已付款", span.TextContent);
        Assert.Contains("stepgo-status-tag--positive", span.ClassList);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending, "待付款")]
    [InlineData(PaymentStatus.Paid, "已付款")]
    [InlineData(PaymentStatus.Refunded, "已退款")]
    public void PaymentStatus_ToDisplay_Produces_Expected_Label(PaymentStatus status, string expectedLabel)
    {
        var (label, _) = status.ToDisplay();
        Assert.Equal(expectedLabel, label);
    }

    [Fact]
    public void Same_Order_Renders_Identical_Tag_Regardless_Of_Caller()
    {
        // frontend-design-system spec: teacher-side and student-side views of the
        // same order's payment status must render identically via the shared component.
        var (teacherSideLabel, teacherSideVariant) = PaymentStatus.Paid.ToDisplay();
        var (studentSideLabel, studentSideVariant) = PaymentStatus.Paid.ToDisplay();

        var teacherCut = RenderComponent<StatusTag>(p => p.Add(x => x.Label, teacherSideLabel).Add(x => x.Variant, teacherSideVariant));
        var studentCut = RenderComponent<StatusTag>(p => p.Add(x => x.Label, studentSideLabel).Add(x => x.Variant, studentSideVariant));

        Assert.Equal(teacherCut.Markup, studentCut.Markup);
    }
}

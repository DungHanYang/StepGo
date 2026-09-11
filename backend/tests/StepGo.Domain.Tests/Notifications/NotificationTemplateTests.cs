using StepGo.Domain.Notifications;
using StepGo.Domain.SharedKernel;

namespace StepGo.Domain.Tests.Notifications;

public class NotificationTemplateTests
{
    [Fact]
    public void Render_SubstitutesPlaceholders()
    {
        var template = NotificationTemplate.Create("OrderPaymentConfirmed", "您已成功報名「{courseName}」，金額 NT${amount}。", Guid.NewGuid(), DateTimeOffset.UtcNow);

        var rendered = template.Render(new Dictionary<string, string> { ["courseName"] = "瑜珈入門", ["amount"] = "3000" });

        Assert.Equal("您已成功報名「瑜珈入門」，金額 NT$3000。", rendered);
    }

    [Fact]
    public void UpdateBody_AppliesImmediatelyToNextRender()
    {
        var template = NotificationTemplate.Create("Key", "舊文案", Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.UpdateBody("新文案 {x}", Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal("新文案 1", template.Render(new Dictionary<string, string> { ["x"] = "1" }));
    }

    [Fact]
    public void UpdateBody_Empty_Throws()
    {
        var template = NotificationTemplate.Create("Key", "文案", Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() => template.UpdateBody("", Guid.NewGuid(), DateTimeOffset.UtcNow));
    }
}

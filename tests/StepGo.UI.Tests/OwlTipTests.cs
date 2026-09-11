using Bunit;
using StepGo.UI.Components;
using Xunit;

namespace StepGo.UI.Tests;

public class OwlTipTests : TestContext
{
    [Fact]
    public void Renders_Title_And_ChildContent_With_Note_Role()
    {
        var cut = RenderComponent<OwlTip>(parameters => parameters
            .Add(p => p.Title, "小提醒")
            .AddChildContent("填寫 Email 才能收到自動通知"));

        var root = cut.Find("[role='note']");
        Assert.Contains("小提醒", root.TextContent);
        Assert.Contains("填寫 Email 才能收到自動通知", root.TextContent);
    }
}
